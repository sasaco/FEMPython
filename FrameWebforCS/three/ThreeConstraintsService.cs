using FrameWebforCS.components.input;
using System.Globalization;
using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

internal readonly record struct ConstraintSelection(string Kind, int Row, string Axis);
internal readonly record struct ConstraintCases(string FixNode, string FixMember, string Joint);

/// <summary>
/// Owns the five JS constraint/auxiliary scene layers on the GL thread.
/// JS ThreeFixNode/FixMember/Joint/RigidZone/NoticePointsService each rebuild from
/// the active case and current node/member geometry; Rebuild performs the same projection.
/// C# groups their owners here because all five share one topology snapshot and GL lifetime.
/// ThreeComponent owns the WinForms fix-node/fix-member scale controls; this service
/// owns the GL glyphs and applies those scales on the render thread.
/// </summary>
internal sealed class ThreeConstraintsService : IDisposable
{
    private const float LegacyPointRadius = 0.018895766721676047f * 3;
    // Bound a single rebuild explicitly; the legacy repeat formula is otherwise unbounded.
    private const int MemberSpringVertexBudget = 250_000;
    private readonly Scene _scene;
    private readonly Dictionary<string, Group> _roots = new(StringComparer.Ordinal)
    {
        ["fix_node"] = new Group { Name = "fix_node" },
        ["fix_member"] = new Group { Name = "fix_member" },
        ["joint"] = new Group { Name = "joint" },
        ["rigid"] = new Group { Name = "rigid" },
        ["notice_points"] = new Group { Name = "notice_points" }
    };
    private readonly Dictionary<Object3D, ConstraintSelection> _selectionByObject = new();
    private readonly Dictionary<ConstraintSelection, Vector3> _positions = new();
    private readonly Dictionary<ConstraintSelection, int> _relatedMembers = new();
    private readonly Dictionary<Object3D, int> _baseColors = new();
    private readonly Dictionary<Object3D, Vector3> _initialScales = new();
    private readonly Dictionary<Line, (float[] Points, Vector3 Axis, string Direction)> _memberSpringGeometry = new();
    private readonly List<MeshBasicMaterial> _meshMaterials = new();
    private readonly List<LineBasicMaterial> _lineMaterials = new();
    private readonly List<BufferGeometry> _lineGeometries = new();
    private readonly SphereBufferGeometry _sphere = new(1, 8, 6);
    private readonly ConeBufferGeometry _cone = new(0.3f, 1, 12, 1, false);
    private readonly CylinderBufferGeometry _bar = new(0.04f, 0.04f, 1, 8);
    private readonly BoxBufferGeometry _box = new(1, 1, 1);
    private readonly PlaneBufferGeometry _plane = new(1, 2.5f);
    private readonly TorusBufferGeometry _ring = new(0.05f, 0.005f, 16, 64);
    private string? _mode;
    private float _fixNodeScale = 5;
    private float _fixMemberScale = 1;
    private bool _disposed;

    internal ThreeConstraintsService(Scene scene)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        foreach (var root in _roots.Values) _scene.Add(root);
    }

    internal ConstraintSelection? Selected { get; private set; }
    internal int? SelectedRelatedMemberId { get; private set; }
    internal float FixNodeScale => _fixNodeScale;
    internal float FixMemberScale => _fixMemberScale;
    internal int Count(string kind) => _positions.Keys.Count(item => item.Kind == kind);
    internal Vector3? PositionOf(string kind, int row, string axis) =>
        _positions.TryGetValue(new ConstraintSelection(kind, row, axis), out var value)
            ? new Vector3(value.X, value.Y, value.Z) : null;

    /// <param name="caseId">The one-based fix_node/fix_member/joint sheet key; rigid and notice points are not case keyed in JS.</param>
    internal void Rebuild(IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, (int Ni, int Nj)> members, string caseId,
        float baseScale = 1, int dimension = 3, double? memberSpringScale = null) =>
        Rebuild(nodes, members, new ConstraintCases(caseId, caseId, caseId),
            baseScale, dimension, memberSpringScale);

    /// <summary>Each JS constraint input has its own active one-based sheet.</summary>
    internal void Rebuild(IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, (int Ni, int Nj)> members, ConstraintCases cases,
        float baseScale = 1, int dimension = 3, double? memberSpringScale = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentException.ThrowIfNullOrWhiteSpace(cases.FixNode);
        ArgumentException.ThrowIfNullOrWhiteSpace(cases.FixMember);
        ArgumentException.ThrowIfNullOrWhiteSpace(cases.Joint);
        if (!float.IsFinite(baseScale) || baseScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseScale));
        if (dimension is not (2 or 3)) throw new ArgumentOutOfRangeException(nameof(dimension));
        var specs = new List<Glyph>();
        int memberSpringVerticesRemaining = MemberSpringVertexBudget;
        // JS geometry services read nodeThree.baseScale; the coordinator passes that value.
        var size = Math.Max(baseScale, 0.001f);
        float springSize = memberSpringScale is double distance && double.IsFinite(distance) && distance > 0
            ? (float)Math.Min(distance, float.MaxValue) : size;
        var modelCenter = ModelCenter(nodes.Values);

        foreach (var item in InputFixNodeService.Instance.GetDisplaySnapshot(cases.FixNode))
        {
            if (!TryNode(item.n, nodes, out var position)) continue;
            bool allFixed = dimension == 2
                ? item.tx == 1 && item.ty == 1 && item.rz == 1
                : item.tx == 1 && item.ty == 1 && item.tz == 1 &&
                    item.rx == 1 && item.ry == 1 && item.rz == 1;
            if (allFixed)
            {
                specs.Add(new Glyph("fix_node", item.row, "tp", position, size * 2, 0x303030, Shape.Box));
                continue;
            }
            AddFixNode(specs, item.row, position, modelCenter, "tx", item.tx, size);
            AddFixNode(specs, item.row, position, modelCenter, "ty", item.ty, size);
            AddFixNode(specs, item.row, position, modelCenter, "tz", item.tz, size);
            AddFixNode(specs, item.row, position, modelCenter, "rx", item.rx, size);
            AddFixNode(specs, item.row, position, modelCenter, "ry", item.ry, size);
            AddFixNode(specs, item.row, position, modelCenter, "rz", item.rz, size);
        }

        foreach (var item in InputFixMemberService.Instance.GetDisplaySnapshot(cases.FixMember))
        {
            if (!TryMember(item.m, nodes, members, out var i, out var j)) continue;
            var center = Midpoint(i, j);
            float length = i.DistanceTo(j);
            if (length < 0.001f) continue;
            var topology = members[int.Parse(item.m!, CultureInfo.InvariantCulture)];
            var display = InputMembersService.Instance.GetDisplayMember(int.Parse(item.m!, CultureInfo.InvariantCulture));
            float cg = display is { } data && data.Ni == topology.Ni && data.Nj == topology.Nj ? data.Cg : 0;
            var localAxis = ConstraintMemberLocalAxis.Get(i, j, cg);
            AddFixMember(specs, item.row, center, "x", item.tx, springSize, 0xff8888, localAxis, length, modelCenter, ref memberSpringVerticesRemaining);
            AddFixMember(specs, item.row, center, "y", item.ty, springSize, 0x88ff88, localAxis, length, modelCenter, ref memberSpringVerticesRemaining);
            AddFixMember(specs, item.row, center, "z", item.tz, springSize, 0x8888ff, localAxis, length, modelCenter, ref memberSpringVerticesRemaining);
            AddFixMember(specs, item.row, center, "r", item.tr, springSize, 0x808080, localAxis, length, modelCenter, ref memberSpringVerticesRemaining);
        }

        foreach (var item in InputJointService.Instance.GetDisplaySnapshot(cases.Joint))
        {
            if (item.row is not int row ||
                !int.TryParse(item.m, NumberStyles.None, CultureInfo.InvariantCulture, out int memberId) ||
                !TryMember(memberId, nodes, members, out var i, out var j)) continue;
            var along = new Vector3(j.X - i.X, j.Y - i.Y, j.Z - i.Z);
            float length = along.Length();
            if (length < 0.001f) continue;
            along.Normalize().MultiplyScalar(Math.Min(0.1f, length * 0.25f));
            var pi = new Vector3(i.X + along.X, i.Y + along.Y, i.Z + along.Z);
            var pj = new Vector3(j.X - along.X, j.Y - along.Y, j.Z - along.Z);
            // The coordinator's member topology contains Ni/Nj only. JS ThreeJointService
            // reads cg from the member input service when it projects the local axes.
            var member = members[memberId];
            var display = InputMembersService.Instance.GetDisplayMember(memberId);
            float cg = display is { } data && data.Ni == member.Ni && data.Nj == member.Nj
                ? data.Cg : 0;
            var localAxis = ConstraintMemberLocalAxis.Get(i, j, cg);
            AddJoint(specs, row, pi, "xi", item.xi, 0xff0000, localAxis.X);
            AddJoint(specs, row, pi, "yi", item.yi, 0x00ff00, localAxis.Y);
            AddJoint(specs, row, pi, "zi", item.zi, 0x0000ff, localAxis.Z);
            AddJoint(specs, row, pj, "xj", item.xj, 0xff0000, localAxis.X);
            AddJoint(specs, row, pj, "yj", item.yj, 0x00ff00, localAxis.Y);
            AddJoint(specs, row, pj, "zj", item.zj, 0x0000ff, localAxis.Z);
        }

        foreach (var item in InputRigidZoneService.Instance.GetDisplaySnapshot())
        {
            if (!TryMember(item.Row, nodes, members, out var i, out var j)) continue;
            float length = i.DistanceTo(j);
            if (length < 0.001f) continue;
            // JS ThreeRigidZoneService.changeData places each rigid end at a length along the member.
            // C# clamps invalid lengths to the member span to avoid a marker far outside the model.
            AddRigid(specs, item.Row, "i", i, j, Math.Clamp(item.ILength / length, 0, 1));
            AddRigid(specs, item.Row, "j", j, i, Math.Clamp(item.JLength / length, 0, 1));
        }

        foreach (var item in InputNoticePointsService.Instance.GetDisplaySnapshot())
        {
            if (!TryMember(item.Member, nodes, members, out var i, out var j)) continue;
            float length = i.DistanceTo(j);
            if (length < 0.001f) continue;
            for (int index = 0; index < item.Points.Length; index++)
            {
                float point = item.Points[index];
                if (!float.IsFinite(point)) continue;
                specs.Add(new Glyph("notice_points", item.Row, $"L{index + 1}",
                    Interpolate(i, j, point / length), LegacyPointRadius, 0x00a5ff, Shape.Sphere,
                    RelatedMemberId: int.TryParse(item.Member, NumberStyles.None, CultureInfo.InvariantCulture,
                        out int memberId) ? memberId : null));
            }
        }

        ClearObjects();
        foreach (var spec in specs) Create(spec);
        ApplyGlyphScales();
        ApplyVisibility();
    }

    internal void SetFixNodeScale(float value)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(value) || value < 5 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value));
        _fixNodeScale = value;
        ApplyGlyphScales();
    }

    internal void SetFixMemberScale(float value)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(value) || value < 0 || value > 5)
            throw new ArgumentOutOfRangeException(nameof(value));
        _fixMemberScale = value;
        ApplyGlyphScales();
    }

    private void ApplyGlyphScales()
    {
        foreach (var (item, initial) in _initialScales)
        {
            float factor = ReferenceEquals(_roots["fix_node"], item.Parent) ? _fixNodeScale / 5 :
                ReferenceEquals(_roots["fix_member"], item.Parent) && item is not Line ? _fixMemberScale : 1;
            item.Scale.Set(initial.X * factor, initial.Y * factor, initial.Z * factor);
        }
        foreach (var (line, (points, axis, direction)) in _memberSpringGeometry)
        {
            float radialScale = direction is "y" or "z"
                ? _fixMemberScale == 0 ? 0 : 1 + MathF.Log2(_fixMemberScale)
                : _fixMemberScale;
            float axialScale = direction == "x" ? 1 : _fixMemberScale;
            var attribute = (BufferAttribute<float>)((BufferGeometry)line.Geometry).GetAttribute<float>("position");
            for (int offset = 0; offset < points.Length; offset += 3)
            {
                float axial = points[offset] * axis.X + points[offset + 1] * axis.Y + points[offset + 2] * axis.Z;
                attribute.Array[offset] = (points[offset] - axis.X * axial) * radialScale + axis.X * axial * axialScale;
                attribute.Array[offset + 1] = (points[offset + 1] - axis.Y * axial) * radialScale + axis.Y * axial * axialScale;
                attribute.Array[offset + 2] = (points[offset + 2] - axis.Z * axial) * radialScale + axis.Z * axial * axialScale;
            }
            attribute.NeedsUpdate = true;
        }
    }

    internal void SetMode(string? routeKey)
    {
        ThrowIfDisposed();
        // JS ChangeMode: fix_nodes/joints/rigid_zone map to singular C# fix_node/joint/rigid.
        // JS ChangeMode("load_names") displays supports/joints; "load_values" hides them.
        // The coordinator maps the C# load sheet to these two JS mode names.
        _mode = routeKey;
        ApplyVisibility();
        if (Selected is { } selected && !IsPickable(selected.Kind)) Select(null);
    }

    internal void Select(ConstraintSelection? selection)
    {
        ThrowIfDisposed();
        if (selection is { } requested && requested.Kind == "fix_node" &&
            !_positions.ContainsKey(requested))
        {
            var fullFixity = requested with { Axis = "tp" };
            if (_positions.ContainsKey(fullFixity)) selection = fullFixity;
        }
        Selected = selection is { } value && IsPickable(value.Kind) && _positions.ContainsKey(value)
            ? value : null;
        SelectedRelatedMemberId = Selected is { } active && _relatedMembers.TryGetValue(active, out int memberId)
            ? memberId : null;
        foreach (var (item, baseColor) in _baseColors)
        {
            int color = baseColor;
            if (Selected is { } selected && _selectionByObject.TryGetValue(item, out var key) && key == selected)
                color = selected.Kind switch
                {
                    "fix_node" => 0xFF11FF,
                    "rigid" or "notice_points" => 0xFF0000,
                    _ => 0x00A5FF
                };
            else if (Selected is { Kind: "joint" or "fix_member" }) color = 0x000000;
            SetColor(item, color);
        }
    }

    internal void Select(string kind, int row, string? axis = null)
    {
        if (axis == null)
        {
            var first = _positions.Keys.FirstOrDefault(key => key.Kind == kind && key.Row == row);
            Select(first.Row == 0 ? null : first);
            return;
        }
        Select(new ConstraintSelection(kind, row, axis));
    }

    internal ConstraintSelection? Pick(Raycaster raycaster)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(raycaster);
        var objects = _selectionByObject.Keys.Where(item =>
            _selectionByObject.TryGetValue(item, out var key) && IsPickable(key.Kind)).ToList();
        if (objects.Count == 0) return null;
        foreach (var root in _roots.Values) root.UpdateWorldMatrix(true, true);
        // The port's Raycaster sorts with an integer distance comparator; choose the true
        // nearest hit explicitly, as ThreeNodesService.Pick does for JS detectObject.
        var nearest = raycaster.IntersectObjects(objects).MinBy(hit => hit.distance);
        return nearest != null && _selectionByObject.TryGetValue(nearest.object3D, out var selected)
            ? selected : null;
    }

    private bool IsPickable(string kind) => _mode == kind;

    private bool IsVisible(string kind) => IsPickable(kind) ||
        (_mode == "load_names" && kind is "fix_node" or "fix_member" or "joint");

    private void ApplyVisibility()
    {
        foreach (var (kind, root) in _roots) root.Visible = IsVisible(kind);
    }

    private void Create(Glyph spec)
    {
        Object3D item;
        if (spec.Shape == Shape.Line)
        {
            var geometry = new BufferGeometry().SetFromPoints(spec.Points!);
            var material = new LineBasicMaterial { Color = Color.Hex(spec.Color) };
            _lineGeometries.Add(geometry);
            _lineMaterials.Add(material);
            var line = new Line(geometry, material) { Name = $"{spec.Kind}{spec.Row}{spec.Axis}" };
            if (spec.Kind == "fix_member")
                _memberSpringGeometry.Add(line, ((float[])((BufferAttribute<float>)geometry.GetAttribute<float>("position")).Array.Clone(),
                    spec.Direction!, spec.Axis));
            item = line;
        }
        else
        {
            var geometry = spec.Shape switch
            {
                Shape.Cone => (BufferGeometry)_cone,
                Shape.Box => _box,
                Shape.Plane => _plane,
                Shape.Ring => _ring,
                Shape.Bar => _bar,
                _ => _sphere
            };
            var material = new MeshBasicMaterial { Color = Color.Hex(spec.Color), Side = Constants.DoubleSide };
            _meshMaterials.Add(material);
            var mesh = new Mesh(geometry, material)
            {
                Name = $"{spec.Kind}{spec.Row}{spec.Axis}"
            };
            if (spec.Shape == Shape.Bar) mesh.Scale.Set(1, spec.Size, 1);
            else mesh.Scale.Set(spec.Size, spec.Size, spec.Size);
            if (spec.Direction != null)
                mesh.Quaternion.SetFromUnitVectors(
                    spec.Shape is Shape.Ring or Shape.Plane ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0),
                    spec.Direction);
            item = mesh;
        }
        item.Position.Set(spec.Position.X, spec.Position.Y, spec.Position.Z);
        _roots[spec.Kind].Add(item);
        var key = new ConstraintSelection(spec.Kind, spec.Row, spec.Axis);
        if (spec.Pickable)
            _selectionByObject.Add(item, key);
        if (spec.Shape == Shape.Line && spec.Pickable)
        {
            // The ported THREE.Line has no Raycast implementation. Keep the visible legacy
            // helix as a line and use an invisible marker for viewport-to-grid selection.
            var pickMaterial = new MeshBasicMaterial
            {
                Color = Color.Hex(spec.Color), Transparent = true, Opacity = 0
            };
            _meshMaterials.Add(pickMaterial);
            var proxy = new Mesh(_sphere, pickMaterial)
            {
                Name = $"pick-{spec.Kind}{spec.Row}{spec.Axis}", Visible = false
            };
            proxy.Position.Set(spec.Position.X, spec.Position.Y, spec.Position.Z);
            float pickRadius = Math.Max(spec.Size * 0.75f, 0.05f);
            proxy.Scale.Set(pickRadius, pickRadius, pickRadius);
            _roots[spec.Kind].Add(proxy);
            _selectionByObject.Add(proxy, key);
            _initialScales.Add(proxy, new Vector3(proxy.Scale.X, proxy.Scale.Y, proxy.Scale.Z));
        }
        _positions.TryAdd(key, spec.SelectionPosition ?? spec.Position);
        if (spec.RelatedMemberId is int memberId) _relatedMembers[key] = memberId;
        _baseColors.Add(item, spec.Color);
        _initialScales.Add(item, new Vector3(item.Scale.X, item.Scale.Y, item.Scale.Z));
    }

    private static void SetColor(Object3D item, int color)
    {
        switch (item)
        {
            case Mesh mesh: mesh.Material.Color = Color.Hex(color); break;
            case Line line: line.Material.Color = Color.Hex(color); break;
        }
    }

    private void ClearObjects()
    {
        Select(null);
        foreach (var root in _roots.Values)
            foreach (var child in root.Children.ToArray())
            {
                root.Remove(child);
                child.Dispose();
            }
        _selectionByObject.Clear();
        _positions.Clear();
        _relatedMembers.Clear();
        _baseColors.Clear();
        _initialScales.Clear();
        _memberSpringGeometry.Clear();
        foreach (var material in _meshMaterials) material.Dispose();
        foreach (var material in _lineMaterials) material.Dispose();
        foreach (var geometry in _lineGeometries) geometry.Dispose();
        _meshMaterials.Clear(); _lineMaterials.Clear(); _lineGeometries.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        ClearObjects();
        _disposed = true;
        foreach (var root in _roots.Values) { _scene.Remove(root); root.Dispose(); }
        _sphere.Dispose(); _cone.Dispose(); _bar.Dispose(); _box.Dispose(); _plane.Dispose(); _ring.Dispose();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static bool TryNode(string? id, IReadOnlyDictionary<int, Vector3> nodes, out Vector3 position)
    {
        if (int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) &&
            nodes.TryGetValue(number, out var found)) { position = found; return true; }
        position = null!; return false;
    }

    private static bool TryMember(string? id, IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, (int Ni, int Nj)> members, out Vector3 i, out Vector3 j) =>
        TryMember(int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : 0,
            nodes, members, out i, out j);

    private static bool TryMember(int id, IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, (int Ni, int Nj)> members, out Vector3 i, out Vector3 j)
    {
        if (members.TryGetValue(id, out var member) && nodes.TryGetValue(member.Ni, out var ni) &&
            nodes.TryGetValue(member.Nj, out var nj)) { i = ni; j = nj; return true; }
        i = null!; j = null!; return false;
    }

    private static Vector3 Midpoint(Vector3 i, Vector3 j) =>
        new((i.X + j.X) / 2, (i.Y + j.Y) / 2, (i.Z + j.Z) / 2);

    private static Vector3 ModelCenter(IEnumerable<Vector3> nodes)
    {
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity, minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, maxZ = float.NegativeInfinity;
        foreach (var node in nodes)
        {
            minX = Math.Min(minX, node.X); minY = Math.Min(minY, node.Y); minZ = Math.Min(minZ, node.Z);
            maxX = Math.Max(maxX, node.X); maxY = Math.Max(maxY, node.Y); maxZ = Math.Max(maxZ, node.Z);
        }
        return float.IsPositiveInfinity(minX) ? new Vector3() :
            new Vector3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
    }

    private static Vector3 Interpolate(Vector3 i, Vector3 j, float fraction) =>
        new(i.X + (j.X - i.X) * fraction,
            i.Y + (j.Y - i.Y) * fraction,
            i.Z + (j.Z - i.Z) * fraction);

    private static void AddFixNode(List<Glyph> specs, int row, Vector3 position,
        Vector3 center, string axis, float? value, float size)
    {
        if (value is not float v || v is 0 or -1) return;
        int color = axis[1] switch { 'x' => 0xff0000, 'y' => 0x00ff00, _ => 0x0000ff };
        bool small = axis[1] switch
        {
            'x' => position.X <= center.X,
            'y' when axis[0] == 't' && v == 1 => position.Y < center.Y,
            'y' => position.Y <= center.Y,
            _ => position.Z <= center.Z
        };
        var outward = axis[1] switch
        {
            'x' => new Vector3(small ? -1 : 1, 0, 0),
            'y' => new Vector3(0, small ? -1 : 1, 0),
            _ => new Vector3(0, 0, small ? -1 : 1)
        };
        if (axis[0] == 'r')
        {
            if (v == 1)
                specs.Add(new Glyph("fix_node", row, axis, position, size * 2, color,
                    Shape.Plane, outward));
            else
                specs.Add(new Glyph("fix_node", row, axis, position, size * 2.5f, color,
                    Shape.Line, Points: NodeSpringPoints(axis[1], small, size, true)));
            return;
        }

        // The old node marker is a unit sphere scaled by baseScale; scaleNode is its radius
        // at the default nodeScale=100. Z pins and Y/Z coils use the reversed legacy side.
        var side = axis[1] == 'z' && v == 1 || axis[1] is 'y' or 'z' && v != 1
            ? new Vector3(-outward.X, -outward.Y, -outward.Z) : outward;
        var anchor = new Vector3(position.X + side.X * size,
            position.Y + side.Y * size, position.Z + side.Z * size);
        if (v == 1)
            specs.Add(new Glyph("fix_node", row, axis, anchor, size * 3, color,
                Shape.Cone, outward));
        else
            specs.Add(new Glyph("fix_node", row, axis, anchor, size * 2.5f, color,
                Shape.Line, Points: NodeSpringPoints(axis[1], small, size, false)));
    }

    private static Vector3[] NodeSpringPoints(char direction, bool small, float nodeRadius, bool rotating)
    {
        float sizeNode = nodeRadius * 2;
        int end = rotating ? 1530 : 1800;
        var points = new Vector3[end / 10 + 1];
        float increase = (small ? 1 : -1) * sizeNode / 500;
        for (int angle = 0; angle <= end; angle += 10)
        {
            float radians = angle * MathF.PI / 180;
            float c = MathF.Cos(radians), s = MathF.Sin(radians);
            float x, y, z;
            if (rotating)
            {
                float radius = sizeNode * 0.00015f * angle * 5;
                x = direction == 'x' ? 0 : radius * c;
                y = direction == 'y' ? 0 : direction == 'x' ? radius * c : radius * s;
                z = direction == 'z' ? 0 : radius * s;
            }
            else
            {
                float radius = sizeNode * 0.06f * 5;
                float depth = angle * increase * 0.06f * 5;
                x = direction == 'x' ? -depth : radius * c;
                y = direction == 'y' ? depth : direction == 'x' ? radius * c : radius * s;
                z = direction == 'z' ? depth : radius * s;
            }
            points[angle / 10] = new Vector3(x, y, z);
        }
        return points;
    }

    private static void AddFixMember(List<Glyph> specs, int row, Vector3 position,
        string axis, float? value, float size, int color,
        (Vector3 X, Vector3 Y, Vector3 Z) localAxis, float length, Vector3 modelCenter,
        ref int verticesRemaining)
    {
        if (value is not float v || v == 0) return;
        const float interval = 0.3f;
        double legacyCount = Math.Floor(length / (2 * interval) - interval);
        if (legacyCount > MemberSpringVertexBudget)
            throw new InvalidOperationException("Member spring geometry exceeds the vertex budget.");
        int count = Math.Max(0, (int)legacyCount);
        int turns = axis == "r" ? 3 : axis == "x" && count == 0
            ? (int)Math.Floor(length / 0.003 / 36) : 4;
        int pointsPerSpring = turns * 36 + (axis == "r" ? 9 : 0) + 1;
        long requestedVertices = (2L * count + 1) * pointsPerSpring;
        if (requestedVertices > verticesRemaining)
            throw new InvalidOperationException("Member spring geometry exceeds the vertex budget.");
        verticesRemaining -= (int)requestedVertices;
        var direction = axis switch
        {
            "x" or "r" => localAxis.X,
            "y" => localAxis.Y,
            _ => localAxis.Z
        };
        for (int k = -count; k <= count; k++)
        {
            var anchor = new Vector3(position.X + localAxis.X.X * k * interval,
                position.Y + localAxis.X.Y * k * interval,
                position.Z + localAxis.X.Z * k * interval);
            bool small = axis switch
            {
                "y" => position.Y <= modelCenter.Y,
                "z" => position.Z <= modelCenter.Z,
                _ => false
            };
            var points = MemberSpringPoints(axis, size, localAxis, length, count, small);
            specs.Add(new Glyph("fix_member", row, axis, anchor, size, color, Shape.Line,
                Direction: direction, Points: points, SelectionPosition: position));
        }
    }

    private static Vector3[] MemberSpringPoints(string axis, float size,
        (Vector3 X, Vector3 Y, Vector3 Z) localAxis, float length, int count, bool small)
    {
        int turns = axis == "r" ? 3 : axis == "x" && count == 0
            ? (int)Math.Floor(length / 0.003 / 36) : 4;
        int samples = turns * 36 + (axis == "r" ? 9 : 0);
        var result = new Vector3[samples + 1];
        var along = axis switch
        {
            "x" or "r" => localAxis.X,
            "y" => localAxis.Y,
            _ => localAxis.Z
        };
        var u = axis == "x" ? localAxis.Y : localAxis.X;
        var w = axis == "z" ? localAxis.Y : localAxis.Z;
        for (int i = 0; i <= samples; i++)
        {
            float theta = i * MathF.PI / 18;
            float degrees = i * 10;
            float radial = axis == "r" ? degrees * 0.0005f * size : 0.5f * size;
            float increase = small ? 0.003f : -0.003f;
            float axial = axis switch
            {
                "r" => 0,
                "x" => (turns * 180 - degrees) * increase * size,
                _ => -degrees * increase * 0.5f * size
            };
            result[i] = new Vector3(
                u.X * radial * MathF.Sin(theta) + w.X * radial * MathF.Cos(theta) + along.X * axial,
                u.Y * radial * MathF.Sin(theta) + w.Y * radial * MathF.Cos(theta) + along.Y * axial,
                u.Z * radial * MathF.Sin(theta) + w.Z * radial * MathF.Cos(theta) + along.Z * axial);
        }
        return result;
    }

    private static void AddJoint(List<Glyph> specs, int row, Vector3 position,
        string axis, float? value, int color, Vector3 direction)
    {
        // JS getJointJson(1) makes a missing field fixed; only explicit values other than 1 draw.
        if (value is not float v || v == 1) return;
        specs.Add(new Glyph("joint", row, axis, position, 1, color, Shape.Ring, direction));
    }

    private static void AddRigid(List<Glyph> specs, int row, string side,
        Vector3 end, Vector3 other, float fraction)
    {
        if (fraction == 0) return;
        var point = Interpolate(end, other, fraction);
        specs.Add(new Glyph("rigid", row, side, point, LegacyPointRadius, 0x00a5ff,
            Shape.Sphere, RelatedMemberId: row));
        var center = Midpoint(end, point);
        var direction = new Vector3(point.X - end.X, point.Y - end.Y, point.Z - end.Z);
        float length = direction.Length();
        if (length >= 0.001f)
            specs.Add(new Glyph("rigid", row, side + "bar", center, length,
                0x000000, Shape.Bar, direction.Normalize(), Pickable: false));
    }

    private enum Shape { Sphere, Cone, Box, Plane, Ring, Bar, Line }
    private sealed record Glyph(string Kind, int Row, string Axis, Vector3 Position,
        float Size, int Color, Shape Shape, Vector3? Direction = null,
        Vector3[]? Points = null, int? RelatedMemberId = null, bool Pickable = true,
        Vector3? SelectionPosition = null);
}

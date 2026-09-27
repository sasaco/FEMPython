using FrameWebforCS.components.input;
using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

/// <summary>Owns the C# counterpart of JS geometry/three-members.service.ts.</summary>
internal sealed class ThreeMembersService : IDisposable
{
    private readonly Scene _scene;
    private readonly Group _root = new() { Name = "members" };
    private readonly Dictionary<int, Mesh> _memberList = new();
    private readonly Dictionary<int, Group> _axisList = new();
    private readonly Dictionary<int, int> _elementById = new();
    private bool _disposed;
    private bool _gui;
    private int? _relatedMemberId;
    private float _nodeBaseScale = 1;
    private float _memberScale = 100;

    internal ThreeMembersService(Scene scene)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _scene.Add(_root);
    }

    internal int MemberCount => _memberList.Count;
    internal int? SelectedMemberId { get; private set; }
    internal bool LabelsVisible { get; private set; }
    internal bool GuiEnabled => _gui;
    internal float MemberScale => _memberScale;

    internal void HighlightRelated(int? memberId)
    {
        ThrowIfDisposed();
        _relatedMemberId = memberId.HasValue && _memberList.ContainsKey(memberId.Value)
            ? memberId : null;
        UpdateColors();
    }

    internal void SetMemberScale(float value)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(value) || value is < 0 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(value));
        _memberScale = value;
        foreach (var mesh in _memberList.Values)
            mesh.Scale.Set(RadiusScale, 1, RadiusScale);
        ScaleAxes();
    }

    internal void ReplaceAll(IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, DisplayMember> members, float nodeBaseScale)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(members);
        ValidateScale(nodeBaseScale);
        foreach (var (id, member) in members) Validate(id, member);

        ClearData();
        _nodeBaseScale = nodeBaseScale;
        foreach (var (id, member) in members)
            AddMember(id, member, nodes);
    }

    internal void UpdateMember(int id, DisplayMember? member,
        IReadOnlyDictionary<int, Vector3> nodes, float nodeBaseScale)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(nodes);
        ValidateScale(nodeBaseScale);
        if (member is { } value) Validate(id, value);
        _nodeBaseScale = nodeBaseScale;
        RemoveMember(id);
        if (member is { } next) AddMember(id, next, nodes);
        foreach (var mesh in _memberList.Values)
            mesh.Scale.Set(RadiusScale, 1, RadiusScale);
        ScaleAxes();
    }

    internal void SetMode(bool visible, bool text, bool gui)
    {
        ThrowIfDisposed();
        _root.Visible = visible;
        LabelsVisible = text;
        _gui = gui;
        if (!gui) Select(null);
        if (!visible || !text) HighlightRelated(null);
        // JS visibleChange(text, gui) also toggles CSS2D numbers, local-axis arrows,
        // and a dat.gui radius slider. Native label/GUI controls are still pending.
    }

    internal void Select(int? id, int? elementId = null)
    {
        ThrowIfDisposed();
        // JS selectChange(index, "elements") colors every member of that element.
        SelectedMemberId = _gui && id.HasValue && _memberList.ContainsKey(id.Value) ? id : null;
        foreach (var (memberId, mesh) in _memberList)
        {
            bool selected = elementId.HasValue
                ? _elementById[memberId] == elementId.Value
                : memberId == SelectedMemberId;
            mesh.Material.Color = Color.Hex(selected || memberId == _relatedMemberId
                ? 0xFF0000 : 0x000000);
            var axes = _axisList[memberId];
            if (_gui && selected && axes.Parent != _root) _root.Add(axes);
            else if ((!_gui || !selected) && axes.Parent == _root) _root.Remove(axes);
        }
    }

    internal int? Pick(Raycaster raycaster)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(raycaster);
        if (!_root.Visible || !_gui || _memberList.Count == 0) return null;
        _root.UpdateWorldMatrix(true, true);
        Intersection? nearest = null;
        int? selectedId = null;
        foreach (var (id, mesh) in _memberList)
            foreach (var hit in raycaster.IntersectObject(mesh))
                if (nearest is null || hit.distance < nearest.distance)
                {
                    nearest = hit;
                    selectedId = id;
                }
        return selectedId;
    }

    public void Dispose()
    {
        if (_disposed) return;
        ClearData();
        _disposed = true;
        _scene.Remove(_root);
        _root.Dispose();
    }

    private void ClearData()
    {
        foreach (var id in _memberList.Keys.ToArray()) RemoveMember(id);
        SelectedMemberId = null;
        _relatedMemberId = null;
    }

    private void RemoveMember(int id)
    {
        if (!_memberList.Remove(id, out var mesh)) return;
        if (_axisList.Remove(id, out var axes))
        {
            if (axes.Parent == _root) _root.Remove(axes);
            foreach (var arrow in axes.Children)
            {
                foreach (var child in arrow.Children)
                {
                    if (child is Line line)
                    {
                        line.Geometry.Dispose();
                        line.Material.Dispose();
                    }
                    else if (child is Mesh head)
                    {
                        head.Geometry.Dispose();
                        head.Material.Dispose();
                    }
                }
                arrow.Dispose();
            }
            axes.Dispose();
        }
        _elementById.Remove(id);
        if (SelectedMemberId == id) SelectedMemberId = null;
        if (_relatedMemberId == id) _relatedMemberId = null;
        _root.Remove(mesh);
        mesh.Geometry.Dispose();
        mesh.Material.Dispose();
        mesh.Dispose();
    }

    private void AddMember(int id, DisplayMember member, IReadOnlyDictionary<int, Vector3> nodes)
    {
        if (!nodes.TryGetValue(member.Ni, out var i) || !nodes.TryGetValue(member.Nj, out var j)) return;
        var direction = new Vector3().SubVectors(j, i);
        float length = direction.Length();
        // JS changeData skips missing endpoints and lengths below 0.001.
        if (!float.IsFinite(length) || length < 0.001f) return;
        var geometry = new CylinderBufferGeometry(1, 1, length, 12);
        var material = new MeshBasicMaterial { Color = Color.Hex(0x000000) };
        var mesh = new Mesh(geometry, material) { Name = "member" + id };
        mesh.Position.Set((i.X + j.X) / 2, (i.Y + j.Y) / 2, (i.Z + j.Z) / 2);
        // JS applies Euler rotation.z/rotation.y. Unit-vector alignment has the
        // same cylinder endpoints and remains well-defined for vertical members.
        mesh.Quaternion.SetFromUnitVectors(new Vector3(0, 1, 0), direction.Normalize());
        mesh.Scale.Set(RadiusScale, 1, RadiusScale);
        _memberList.Add(id, mesh);
        _elementById.Add(id, member.Element);
        _root.Add(mesh);

        // JS ThreeMembersService keeps axisList separate from memberList and shows
        // its local X/Y/Z arrows only for the selected member or element.
        var localAxis = ConstraintMemberLocalAxis.Get(i, j, member.Cg);
        var center = new Vector3((i.X + j.X) / 2, (i.Y + j.Y) / 2, (i.Z + j.Z) / 2);
        var axes = new Group { Name = "member" + id + "axis" };
        float arrowLength = length * 0.2f;
        axes.Add(new ArrowHelper(localAxis.X, center, arrowLength, Color.Hex(0xFF0000)) { Name = "x" });
        axes.Add(new ArrowHelper(localAxis.Y, center, arrowLength, Color.Hex(0x00FF00)) { Name = "y" });
        axes.Add(new ArrowHelper(localAxis.Z, center, arrowLength, Color.Hex(0x0000FF)) { Name = "z" });
        _axisList.Add(id, axes);
        ScaleAxes();
    }

    private void ScaleAxes()
    {
        // JS onResize multiplies the member radius by 50 for axis arrow size.
        float scale = RadiusScale * 50;
        foreach (var axes in _axisList.Values)
            foreach (var arrow in axes.Children)
                arrow.Scale.Set(scale, scale, scale);
    }

    private float RadiusScale => _nodeBaseScale * 0.3f * Math.Max(_memberScale / 100, 0.001f);

    private void UpdateColors()
    {
        foreach (var (id, mesh) in _memberList)
            mesh.Material.Color = Color.Hex(id == SelectedMemberId || id == _relatedMemberId
                ? 0xFF0000 : 0x000000);
    }

    private static void Validate(int id, DisplayMember member)
    {
        if (id <= 0 || member.Ni <= 0 || member.Nj <= 0 || !float.IsFinite(member.Cg))
            throw new ArgumentOutOfRangeException(nameof(member));
    }

    private static void ValidateScale(float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

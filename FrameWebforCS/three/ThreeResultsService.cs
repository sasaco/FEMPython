using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Globalization;
using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

internal enum SectionForceEnvelope { Single, Max, Min }

internal readonly record struct SectionForceSample(int MemberId, float Location, float Value,
    SectionForceEnvelope Envelope = SectionForceEnvelope.Single);

internal readonly record struct ResultValueRange(double Min, double Max,
    string MinEntityId, string MaxEntityId);

internal readonly record struct ResultViewportExtrema(string Mode, string CaseId,
    ResultValueRange Primary, ResultValueRange? Secondary);

/// <summary>GL-thread result layers corresponding to JS displacement/react/section-force services.</summary>
internal sealed class ThreeResultsService : IDisposable
{
    // Result controls publish only committed UI pages. The coordinator subscribes and
    // passes the chosen mode/page to SetMode on its GL flush thread.
    internal static event Action<string, string?, string?>? ResultPageChanged;
    internal static event Action<string, IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>>, long>? DerivedFsecChanged;
    internal static void PublishPage(string mode, string? caseId, string? component = null) =>
        ResultPageChanged?.Invoke(mode, caseId, component);
    internal static string DefaultSectionForceComponent(int dimension) =>
        dimension == 2 ? "momentZ" : "momentY";
    internal static void PublishDerivedFsec(string mode,
        IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>> cases, long sourceRevision) =>
        DerivedFsecChanged?.Invoke(mode, cases, sourceRevision);

    private readonly Scene _scene;
    private readonly Group _disgRoot = new() { Name = "disg" };
    private readonly Group _reacRoot = new() { Name = "reac" };
    private readonly Group _fsecRoot = new() { Name = "fsec" };
    private IReadOnlyDictionary<int, Vector3> _nodeData = new Dictionary<int, Vector3>();
    private IReadOnlyDictionary<int, DisplayMember> _memberData = new Dictionary<int, DisplayMember>();
    private IReadOnlyDictionary<int, DisplayPanel> _panelData = new Dictionary<int, DisplayPanel>();
    private Dictionary<string, Dictionary<string, clsDisg>> _disgData = new();
    private Dictionary<string, Dictionary<string, clsReac>> _reacData = new();
    private Dictionary<string, Dictionary<string, Dictionary<string, clsFsec>>> _fsecData = new();
    private readonly Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>>> _derivedFsec = new();
    private readonly Dictionary<string, long> _derivedRevision = new();
    private readonly Dictionary<string, string[]> _movingDisgCases = new(StringComparer.Ordinal);
    private string _mode = "";
    private string? _currentIndex;
    private string? _renderedDisgCase;
    private int _disgAnimationFrame;
    private int _disgAnimationIndex;
    private string _currentRadio = "momentY";
    private float _displacementScale = 0.5f;
    private float _reactionScale = 1;
    private float _sectionForceScale = 100;
    private float _nodeBaseScale = 1;
    private float _maxNodeDistance;
    private long _documentRevision = -1;
    private bool _disposed;

    internal ThreeResultsService(Scene scene)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _scene.Add(_disgRoot);
        _scene.Add(_reacRoot);
        _scene.Add(_fsecRoot);
        ApplyVisibility();
    }

    internal int DisplacementCount => _disgRoot.Children.Count;
    internal int ReactionCount => _reacRoot.Children.Count;
    internal int SectionForceCount => _fsecRoot.Children.Count;
    internal string Mode => _mode;
    internal string? CurrentIndex => _currentIndex;
    internal string? RenderedDisplacementCase => _renderedDisgCase;
    internal ResultViewportExtrema? CurrentExtrema { get; private set; }
    internal float DisplacementScale => _displacementScale;
    internal float ReactionScale => _reactionScale;
    internal float SectionForceScale => _sectionForceScale;

    internal void SetTopology(IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, DisplayMember> members,
        IReadOnlyDictionary<int, DisplayPanel> panels, float nodeBaseScale)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(panels);
        if (!float.IsFinite(nodeBaseScale) || nodeBaseScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(nodeBaseScale));
        _nodeData = nodes.ToDictionary(item => item.Key,
            item => new Vector3(item.Value.X, item.Value.Y, item.Value.Z));
        // Topology changes are less frequent than result page changes. Cache the
        // same distance estimate used by the node layer for every page redraw.
        _maxNodeDistance = (float)NodeDistanceExtrema.Find(_nodeData.Values.ToArray()).MaxDistance;
        _memberData = members.ToDictionary();
        _panelData = panels.ToDictionary();
        _nodeBaseScale = nodeBaseScale;
        Redraw();
    }

    internal void SetBaseResults(
        Dictionary<string, Dictionary<string, clsDisg>> disg,
        Dictionary<string, Dictionary<string, clsReac>> reac,
        Dictionary<string, Dictionary<string, Dictionary<string, clsFsec>>> fsec,
        long documentRevision,
        IReadOnlyCollection<string>? movingLoadParents = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(disg);
        ArgumentNullException.ThrowIfNull(reac);
        ArgumentNullException.ThrowIfNull(fsec);
        if (documentRevision < _documentRevision) return;
        bool replacement = documentRevision != _documentRevision;
        _documentRevision = documentRevision;
        // The service dictionaries are replaced, not mutated, by their Apply* methods.
        // Cache their committed generation; stale async derived output is checked separately.
        _disgData = disg;
        _reacData = reac;
        _fsecData = fsec;
        if (replacement) _derivedFsec.Clear();
        SetMovingDisplacementCases(movingLoadParents);
        ResetDisplacementAnimation();
        Redraw();
    }

    internal void SetDerivedFsec(string mode,
        IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>> cases, long sourceRevision)
    {
        ThrowIfDisposed();
        if (mode is not ("comb_fsec" or "pick_fsec")) throw new ArgumentOutOfRangeException(nameof(mode));
        ArgumentNullException.ThrowIfNull(cases);
        if (_derivedRevision.TryGetValue(mode, out long latest) && sourceRevision < latest) return;
        _derivedRevision[mode] = sourceRevision;
        _derivedFsec[mode] = cases.ToDictionary(item => item.Key,
            item => (IReadOnlyList<SectionForceSample>)item.Value.ToArray());
        if (_mode == mode) Redraw();
    }

    internal void SetMode(string jsMode, string? caseId, string? component = null)
    {
        ThrowIfDisposed();
        if (_mode == "disg" && jsMode != "disg")
            _displacementScale = 0.5f; // JS guiDisable resets dispScale on exit.
        _mode = jsMode ?? "";
        _currentIndex = caseId;
        ResetDisplacementAnimation();
        if (!string.IsNullOrWhiteSpace(component)) _currentRadio = component;
        Redraw();
    }

    internal void AdvanceDisplacementAnimation()
    {
        ThrowIfDisposed();
        if (_mode != "disg" || _currentIndex is null ||
            !_movingDisgCases.TryGetValue(_currentIndex, out var cases) ||
            cases.Length < 2) return;
        if (++_disgAnimationFrame < 10) return;
        _disgAnimationFrame = 0;
        _disgAnimationIndex = (_disgAnimationIndex + 1) % cases.Length;
        _renderedDisgCase = cases[_disgAnimationIndex];
        Redraw();
    }

    internal void SetDisplacementScale(float value)
    {
        ThrowIfDisposed();
        CheckScale(value, 2, nameof(value));
        _displacementScale = value;
        if (_mode == "disg") Redraw();
    }

    internal void SetReactionScale(float value)
    {
        ThrowIfDisposed();
        CheckScale(value, 10, nameof(value));
        _reactionScale = value;
        if (_mode == "reac") Redraw();
    }

    internal void SetSectionForceScale(float value)
    {
        ThrowIfDisposed();
        CheckScale(value, 1000, nameof(value));
        _sectionForceScale = value;
        if (_mode is "fsec" or "comb_fsec" or "pick_fsec") Redraw();
    }

    private static void CheckScale(float value, float maximum, string name)
    {
        if (!float.IsFinite(value) || value < 0 || value > maximum)
            throw new ArgumentOutOfRangeException(name);
    }

    private void SetMovingDisplacementCases(IReadOnlyCollection<string>? parents)
    {
        _movingDisgCases.Clear();
        // Legacy receives parent IDs explicitly. Infer only when the caller has
        // not yet supplied that metadata and the parent result itself exists.
        var parentIds = parents is null
            ? _disgData.Keys.ToHashSet(StringComparer.Ordinal)
            : parents.ToHashSet(StringComparer.Ordinal);
        var children = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var key in _disgData.Keys)
        {
            for (int dot = key.IndexOf('.'); dot >= 0;
                 dot = key.IndexOf('.', dot + 1))
            {
                string parent = key[..dot];
                if (!parentIds.Contains(parent) || !_disgData.ContainsKey(parent)) continue;
                if (!children.TryGetValue(parent, out var list))
                    children[parent] = list = new List<string>();
                list.Add(key);
            }
        }
        foreach (var (parent, list) in children)
            _movingDisgCases[parent] = [parent, .. list];
    }

    private void ResetDisplacementAnimation()
    {
        _renderedDisgCase = _mode == "disg" ? _currentIndex : null;
        _disgAnimationFrame = 0;
        _disgAnimationIndex = 0;
    }

    internal void ClearData()
    {
        ThrowIfDisposed();
        _disgData = new();
        _reacData = new();
        _fsecData = new();
        _derivedFsec.Clear();
        _movingDisgCases.Clear();
        _renderedDisgCase = null;
        _disgAnimationFrame = 0;
        _disgAnimationIndex = 0;
        _nodeData = new Dictionary<int, Vector3>();
        _maxNodeDistance = 0;
        _memberData = new Dictionary<int, DisplayMember>();
        _panelData = new Dictionary<int, DisplayPanel>();
        Redraw();
    }

    public void Dispose()
    {
        if (_disposed) return;
        RemoveChildren(_disgRoot);
        RemoveChildren(_reacRoot);
        RemoveChildren(_fsecRoot);
        _scene.Remove(_disgRoot);
        _scene.Remove(_reacRoot);
        _scene.Remove(_fsecRoot);
        _disgRoot.Dispose();
        _reacRoot.Dispose();
        _fsecRoot.Dispose();
        _disposed = true;
    }

    private void Redraw()
    {
        CurrentExtrema = null;
        RemoveChildren(_disgRoot);
        RemoveChildren(_reacRoot);
        RemoveChildren(_fsecRoot);
        ApplyVisibility();
        if (_currentIndex is null) return;
        if (_mode == "disg") DrawDisplacement();
        else if (_mode == "reac") DrawReaction();
        else if (_mode is "fsec" or "comb_fsec" or "pick_fsec") DrawSectionForce();
        // JS three.service.ts:512-556 intentionally hides comb/pick displacement
        // and reaction geometry; their table page changes show no result objects.
    }

    private void ApplyVisibility()
    {
        _disgRoot.Visible = _mode == "disg";
        _reacRoot.Visible = _mode == "reac";
        _fsecRoot.Visible = _mode is "fsec" or "comb_fsec" or "pick_fsec";
    }

    private void DrawDisplacement()
    {
        string caseId = _renderedDisgCase ?? _currentIndex!;
        if (!TryCase(_disgData, caseId, out var caseData)) return;
        CurrentExtrema = new ResultViewportExtrema("disg", caseId,
            ValueRange(caseData.SelectMany(item => new[]
            {
                (item.Key, item.Value.dx), (item.Key, item.Value.dy), (item.Key, item.Value.dz)
            })) ?? ZeroRange(),
            ValueRange(caseData.SelectMany(item => new[]
            {
                (item.Key, item.Value.rx), (item.Key, item.Value.ry), (item.Key, item.Value.rz)
            })));
        var displacements = new Dictionary<int, clsDisg>();
        foreach (var (id, value) in caseData)
            if (TryId(id, "node", out int nodeId)) displacements[nodeId] = value;
        if (displacements.Count == 0) return;
        float maxDistance = _maxNodeDistance;
        double maxValue = displacements.Values.SelectMany(v => new[] { Math.Abs(v.dx ?? 0), Math.Abs(v.dy ?? 0), Math.Abs(v.dz ?? 0) }).DefaultIfEmpty().Max();
        // JS changeDisg/onResize: node.maxDistance * 0.1 / maxValue,
        // then the default user scale 0.5 * 0.2.
        float scale = maxValue > 0
            ? (float)(maxDistance * 0.01 / maxValue) * (_displacementScale / 0.5f)
            : _displacementScale * 0.2f;
        // JS ThreeDisplacementService.setResultData adds a panel edge only when
        // InputMembersService.sameNodeMember finds no member in either direction.
        var memberNodePairs = new HashSet<(int Ni, int Nj)>();
        foreach (var (memberId, member) in _memberData)
        {
            memberNodePairs.Add(UndirectedNodePair(member.Ni, member.Nj));
            AddDisplacedEdge("member" + memberId, member.Ni, member.Nj, member.Cg, displacements, scale);
        }
        // JS setResultData adds only panel boundary edges absent from members
        // and from previously registered virtual panel members.
        foreach (var (panelId, panel) in _panelData)
            for (int i = 0; i < panel.Nodes.Length; i++)
            {
                int ni = panel.Nodes[i], nj = panel.Nodes[(i + 1) % panel.Nodes.Length];
                if (memberNodePairs.Add(UndirectedNodePair(ni, nj)))
                    AddDisplacedEdge($"panel{panelId}-{i}", ni, nj, 0, displacements, scale);
            }
    }

    private static (int Ni, int Nj) UndirectedNodePair(int ni, int nj) =>
        ni <= nj ? (ni, nj) : (nj, ni);

    private void AddDisplacedEdge(string name, int ni, int nj, float cg,
        IReadOnlyDictionary<int, clsDisg> data, float scale)
    {
        if (!_nodeData.TryGetValue(ni, out var i) || !_nodeData.TryGetValue(nj, out var j) ||
            !data.TryGetValue(ni, out var di) || !data.TryGetValue(nj, out var dj)) return;
        var start = new Vector3(i.X + (float)(di.dx ?? 0) * scale,
            i.Y + (float)(di.dy ?? 0) * scale, i.Z + (float)(di.dz ?? 0) * scale);
        var end = new Vector3(j.X + (float)(dj.dx ?? 0) * scale,
            j.Y + (float)(dj.dy ?? 0) * scale, j.Z + (float)(dj.dz ?? 0) * scale);
        var axis = new Vector3().SubVectors(end, start);
        float length = axis.Length();
        if (length < 0.001f) return;
        // JS ThreeDisplacementService.onResize:395-463 transforms the 12 end
        // displacements/rotations into member-local axes, then samples cubic
        // Hermite transverse displacement at 20 stations when rotations differ.
        var transform = MemberTransform(axis, cg);
        float[] global =
        [
            (float)(di.dx ?? 0), (float)(di.dy ?? 0), (float)(di.dz ?? 0),
            (float)(di.rx ?? 0), (float)(di.ry ?? 0), (float)(di.rz ?? 0),
            (float)(dj.dx ?? 0), (float)(dj.dy ?? 0), (float)(dj.dz ?? 0),
            (float)(dj.rx ?? 0), (float)(dj.ry ?? 0), (float)(dj.rz ?? 0)
        ];
        var local = new float[12];
        for (int block = 0; block < 4; block++)
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                    local[block * 3 + row] += transform[row, col] * global[block * 3 + col];
        int divisions = global[3] == global[9] && global[4] == global[10] && global[5] == global[11] ? 1 : 20;
        var positions = new Vector3[divisions + 1];
        for (int station = 0; station <= divisions; station++)
        {
            float n = (float)station / divisions;
            float n2 = n * n, n3 = n2 * n;
            float xhe = (1 - n) * local[0] + n * local[6];
            float yhe = (1 - 3 * n2 + 2 * n3) * local[1]
                + length * (n - 2 * n2 + n3) * local[5]
                + (3 * n2 - 2 * n3) * local[7]
                + length * (-n2 + n3) * local[11];
            float zhe = (1 - 3 * n2 + 2 * n3) * local[2]
                - length * (n - 2 * n2 + n3) * local[4]
                + (3 * n2 - 2 * n3) * local[8]
                - length * (n3 - n2) * local[10];
            float xhg = transform[0, 0] * xhe + transform[1, 0] * yhe + transform[2, 0] * zhe;
            float yhg = transform[0, 1] * xhe + transform[1, 1] * yhe + transform[2, 1] * zhe;
            float zhg = transform[0, 2] * xhe + transform[1, 2] * yhe + transform[2, 2] * zhe;
            // JS ThreeDisplacementService.onResize:385-389,445-447 starts from
            // already displaced endpoints, then adds de again. That doubles
            // endpoint translation. Keep its displaced tMatrix/L for bending,
            // but use the original endpoints here so a unit dx moves by scale.
            positions[station] = new Vector3(
                (1 - n) * i.X + n * j.X + xhg * scale,
                (1 - n) * i.Y + n * j.Y + yhg * scale,
                (1 - n) * i.Z + n * j.Z + zhg * scale);
        }
        AddLine(_disgRoot, name, positions, 0xFF0000);
    }

    private static float[,] MemberTransform(Vector3 axis, float cg)
    {
        float length = axis.Length();
        float ll = axis.X / length, mm = axis.Y / length, nn = axis.Z / length;
        float qq = MathF.Sqrt(ll * ll + mm * mm);
        var t2 = new float[3, 3];
        // Same vertical-member branch as JS ThreeMembersService.tMatrix.
        if (axis.X == 0 && axis.Y == 0)
        {
            t2[0, 2] = nn; t2[1, 0] = nn; t2[2, 1] = 1;
        }
        else
        {
            t2[0, 0] = ll; t2[0, 1] = mm; t2[0, 2] = nn;
            t2[1, 0] = -mm / qq; t2[1, 1] = ll / qq;
            t2[2, 0] = -ll * nn / qq; t2[2, 1] = -mm * nn / qq; t2[2, 2] = qq;
        }
        float angle = cg * MathF.PI / 180;
        float cos = MathF.Cos(angle), sin = MathF.Sin(angle);
        var result = new float[3, 3];
        for (int col = 0; col < 3; col++)
        {
            result[0, col] = t2[0, col];
            result[1, col] = cos * t2[1, col] + sin * t2[2, col];
            result[2, col] = -sin * t2[1, col] + cos * t2[2, col];
        }
        return result;
    }

    private void DrawReaction()
    {
        if (!TryCase(_reacData, _currentIndex!, out var caseData)) return;
        CurrentExtrema = new ResultViewportExtrema("reac", _currentIndex!,
            ValueRange(caseData.SelectMany(item => new[]
            {
                (item.Key, item.Value.tx), (item.Key, item.Value.ty), (item.Key, item.Value.tz)
            })) ?? ZeroRange(),
            ValueRange(caseData.SelectMany(item => new[]
            {
                (item.Key, item.Value.mx), (item.Key, item.Value.my), (item.Key, item.Value.mz)
            })));
        float maxForce = (float)caseData.Values.SelectMany(value => new[]
        {
            Math.Abs(value.tx ?? 0), Math.Abs(value.ty ?? 0), Math.Abs(value.tz ?? 0)
        }).DefaultIfEmpty().Max();
        float maxMoment = (float)caseData.Values.SelectMany(value => new[]
        {
            Math.Abs(value.mx ?? 0), Math.Abs(value.my ?? 0), Math.Abs(value.mz ?? 0)
        }).DefaultIfEmpty().Max();
        // JS ThreeReactService.maxLength is node.baseScale * 80; setPointReact
        // applies a further 0.2 and the nonlinear DataHelper.getCircleScale.
        float extent = _nodeBaseScale * 80 * 0.2f;
        foreach (var (id, value) in caseData)
        {
            if (!TryId(id, "node", out int nodeId) || !_nodeData.TryGetValue(nodeId, out var node)) continue;
            AddReactionArrow(nodeId, "tx", node, new Vector3(1, 0, 0), value.tx, maxForce, extent, 0xFF0000);
            AddReactionArrow(nodeId, "ty", node, new Vector3(0, 1, 0), value.ty, maxForce, extent, 0x00FF00);
            AddReactionArrow(nodeId, "tz", node, new Vector3(0, 0, 1), value.tz, maxForce, extent, 0x0000FF);
            AddReactionArrow(nodeId, "mx", node, new Vector3(1, 0, 0), value.mx, maxMoment, extent, 0xFF0000);
            AddReactionArrow(nodeId, "my", node, new Vector3(0, 1, 0), value.my, maxMoment, extent, 0x00FF00);
            AddReactionArrow(nodeId, "mz", node, new Vector3(0, 0, 1), value.mz, maxMoment, extent, 0x0000FF);
        }
    }

    private void AddReactionArrow(int nodeId, string component, Vector3 node,
        Vector3 axis, double? value, float max, float extent, int color)
    {
        if (!value.HasValue || value == 0 || max <= 0) return;
        if (component.StartsWith('m'))
        {
            AddMomentReaction(nodeId, component, node, value.Value, max, color);
            return;
        }
        float length = GetCircleScale(value.Value, max) * extent;
        float sign = Math.Sign(value.Value);
        // JS ThreeReactService.setPointReact draws from the node toward
        // (-length, 0, 0), (0, -length, 0), or (0, 0, -length).
        float signedLength = sign * length;
        var end = new Vector3(-axis.X * signedLength,
            -axis.Y * signedLength, -axis.Z * signedLength);
        float coneScale = signedLength * 0.3f;
        // JS ThreeReactService.setPointReact retains the sign in the cone size.
        var cone = new Mesh(new ConeBufferGeometry(0.1f * coneScale, coneScale, 3, 1, true),
            new MeshBasicMaterial { Color = Color.Hex(color) }) { Name = "cone" };
        switch (component)
        {
            case "tx":
                cone.Position.Set(-coneScale / 2, 0, 0);
                cone.Rotation.Z = 3 * MathF.PI / 2;
                break;
            case "ty":
                cone.Position.Set(0, -coneScale / 2, 0);
                break;
            case "tz":
                cone.Position.Set(0, 0, -coneScale / 2);
                cone.Rotation.X = MathF.PI / 2;
                break;
        }
        var group = new Group { Name = $"reac{nodeId}-{component}" };
        group.Scale.Set(_reactionScale, _reactionScale, _reactionScale);
        group.Add(cone);
        group.Add(new Line(new BufferGeometry().SetFromPoints(new[] { new Vector3(0, 0, 0), end }),
            new LineBasicMaterial { Color = Color.Hex(color) }) { Name = "line" });
        group.Position.Copy(node);
        _reacRoot.Add(group);
    }

    private void AddMomentReaction(int nodeId, string component, Vector3 node,
        double value, float max, int color)
    {
        // JS ThreeReactService.setMomentReact: a 270-degree radius-4 arc,
        // three-sided cone at (4, 0.5), and sign * getCircleScale / 5.
        var arc = new Vector3[21];
        for (int i = 0; i < arc.Length; i++)
        {
            float angle = i * 1.5f * MathF.PI / 20;
            arc[i] = new Vector3(4 * MathF.Cos(angle), 4 * MathF.Sin(angle), 0);
        }
        var group = new Group { Name = $"reac{nodeId}-{component}" };
        group.Add(new Line(new BufferGeometry().SetFromPoints(arc),
            new LineBasicMaterial { Color = Color.Hex(color) }) { Name = "ellipse" });
        var cone = new Mesh(new ConeBufferGeometry(0.3f, 3, 3, 1, true),
            new MeshBasicMaterial { Color = Color.Hex(color) }) { Name = "cone" };
        cone.Rotation.X = MathF.PI;
        cone.Position.Set(4, 0.5f, 0);
        group.Add(cone);
        group.Position.Copy(node);
        float momentScale = Math.Sign(value) * GetCircleScale(value, max) / 5 * _reactionScale;
        group.Scale.Set(momentScale, momentScale, momentScale);
        switch (component)
        {
            case "mx":
                group.Rotation.Set(value > 0 ? MathF.PI : 0, MathF.PI / 2, 0,
                    RotationOrder.YXZ);
                break;
            case "my":
                group.Rotation.Set(MathF.PI / 2, value < 0 ? MathF.PI : 0, 0,
                    RotationOrder.XYZ);
                break;
            case "mz":
                if (value > 0) group.Rotation.X = MathF.PI;
                break;
        }
        _reacRoot.Add(group);
    }

    private static float GetCircleScale(double value, float max)
    {
        // JS DataHelperModule.getCircleScale: sqrt(1 - (abs(value) / max - 1)^2).
        float ratio = (float)(Math.Abs(value) / max);
        return MathF.Sqrt(MathF.Max(0, 1 - (ratio - 1) * (ratio - 1)));
    }

    private void DrawSectionForce()
    {
        IReadOnlyList<SectionForceSample> samples;
        if (_mode == "fsec") samples = ProjectBaseFsec();
        else if (!_derivedFsec.TryGetValue(_mode, out var cases) ||
            !TryCase(cases, _currentIndex!, out samples)) return;
        if (samples.Count == 0) return;
        CurrentExtrema = new ResultViewportExtrema(_mode, _currentIndex!,
            ValueRange(samples.Select(item => ($"member{item.MemberId}", (double?)item.Value))) ?? ZeroRange(),
            null);
        float max = samples.Max(item => Math.Abs(item.Value));
        if (max <= 0) return;
        float scale = _sectionForceScale / 100 * _nodeBaseScale * 5 / max;
        string component = _currentRadio.EndsWith("_max", StringComparison.Ordinal) ||
            _currentRadio.EndsWith("_min", StringComparison.Ordinal)
                ? _currentRadio[..^4] : _currentRadio;
        int direction = component switch
        {
            "axialForce" or "fx" when InputDataService.Instance.dimension == 2 => 1,
            "axialForce" or "fx" or "torsionalMoment" or "mx" or
                "momentY" or "my" or "shearForceZ" or "fz" => 2,
            _ => 1
        };
        foreach (var byMember in samples.GroupBy(item => (item.MemberId, item.Envelope)))
        {
            if (!_memberData.TryGetValue(byMember.Key.MemberId, out var member) ||
                !_nodeData.TryGetValue(member.Ni, out var start) ||
                !_nodeData.TryGetValue(member.Nj, out var end)) continue;
            var axis = new Vector3().SubVectors(end, start);
            float length = axis.Length();
            if (length < 0.001f) continue;
            var transform = MemberTransform(axis, member.Cg);
            var normal = new Vector3(transform[direction, 0], transform[direction, 1],
                transform[direction, 2]);
            var diagram = new Group
            {
                Name = $"fsec{byMember.Key.MemberId}-{byMember.Key.Envelope.ToString().ToLowerInvariant()}"
            };
            var stations = byMember.OrderBy(item => item.Location).ToArray();
            for (int i = 1; i < stations.Length; i++)
            {
                var first = stations[i - 1];
                var last = stations[i];
                if (last.Location - first.Location <= 1e-6f) continue;
                var base1 = SectionStation(start, axis, length, first.Location);
                var base2 = SectionStation(start, axis, length, last.Location);
                var value1 = base1.Clone().AddScaledVector(normal, first.Value * scale);
                var value2 = base2.Clone().AddScaledVector(normal, last.Value * scale);
                var triangles = new List<Vector3>();
                if (Math.Sign(first.Value) != Math.Sign(last.Value) &&
                    first.Value != 0 && last.Value != 0)
                {
                    float fraction = Math.Abs(first.Value) /
                        (Math.Abs(first.Value) + Math.Abs(last.Value));
                    var zero = base1.Clone().AddScaledVector(
                        new Vector3().SubVectors(base2, base1), fraction);
                    triangles.AddRange([base1, value1, zero, zero, value2, base2]);
                }
                else
                {
                    triangles.AddRange([base1, value1, value2, base1, value2, base2]);
                }
                var geometry = new BufferGeometry().SetFromPoints(triangles.ToArray());
                diagram.Add(new Mesh(geometry, new MeshBasicMaterial
                {
                    Color = Color.Hex(0x0000FF), Transparent = true, Opacity = 0.1f,
                    Side = Constants.DoubleSide
                }) { Name = "face" });
                AddLine(diagram, "line", [base1, value1, value2, base2], 0x0000FF);
            }
            if (diagram.Children.Count > 0) _fsecRoot.Add(diagram);
        }
    }

    private static Vector3 SectionStation(Vector3 start, Vector3 axis, float length, float location) =>
        start.Clone().AddScaledVector(axis, Math.Clamp(location / length, 0, 1));

    private static ResultValueRange ZeroRange() => new(0, 0, "", "");

    private static ResultValueRange? ValueRange(IEnumerable<(string EntityId, double? Value)> values)
    {
        bool hasValue = false;
        double min = 0, max = 0;
        string minId = "", maxId = "";
        foreach (var (entityId, candidate) in values)
        {
            if (!candidate.HasValue || !double.IsFinite(candidate.Value)) continue;
            double value = candidate.Value;
            if (!hasValue || value < min) { min = value; minId = entityId; }
            if (!hasValue || value > max) { max = value; maxId = entityId; }
            hasValue = true;
        }
        return hasValue ? new ResultValueRange(min, max, minId, maxId) : null;
    }

    private IReadOnlyList<SectionForceSample> ProjectBaseFsec()
    {
        if (!TryCase(_fsecData, _currentIndex!, out var caseData)) return [];
        var result = new List<SectionForceSample>();
        foreach (var (id, points) in caseData)
        {
            if (!TryId(id, "member", out int memberId)) continue;
            float station = 0;
            foreach (var point in points.OrderBy(item => ParseStation(item.Key)))
            {
                var value = point.Value;
                result.Add(new SectionForceSample(memberId, station, PickComponent(value, true)));
                station += (float)(value.L ?? 0);
                result.Add(new SectionForceSample(memberId, station, PickComponent(value, false)));
            }
        }
        return result;
    }

    private float PickComponent(clsFsec value, bool isI) => _currentRadio switch
    {
        "axialForce" or "fx" => (float)((isI ? value.fxi : value.fxj) ?? 0),
        "shearForceY" or "fy" => (float)((isI ? value.fyi : value.fyj) ?? 0),
        "shearForceZ" or "fz" => (float)((isI ? value.fzi : value.fzj) ?? 0),
        "torsionalMoment" or "mx" => (float)((isI ? value.mxi : value.mxj) ?? 0),
        "momentZ" or "mz" => (float)((isI ? value.mzi : value.mzj) ?? 0),
        _ => (float)((isI ? value.myi : value.myj) ?? 0)
    };

    private static bool TryCase<T>(IReadOnlyDictionary<string, T> cases, string id, out T value)
    {
        if (cases.TryGetValue(id, out value!)) return true;
        if (id.StartsWith("Case", StringComparison.Ordinal) && cases.TryGetValue(id[4..], out value!)) return true;
        if (cases.TryGetValue("Case" + id, out value!)) return true;
        value = default!;
        return false;
    }

    private static bool TryId(string text, string prefix, out int id)
    {
        string value = text.StartsWith(prefix, StringComparison.Ordinal) ? text[prefix.Length..] : text;
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
    }

    private static int ParseStation(string key) =>
        int.TryParse(key.TrimStart('P'), NumberStyles.None, CultureInfo.InvariantCulture, out int station)
            ? station : int.MaxValue;

    private static void AddLine(Group root, string name, Vector3[] points, int color)
    {
        var geometry = new BufferGeometry().SetFromPoints(points);
        var material = new LineBasicMaterial { Color = Color.Hex(color) };
        root.Add(new Line(geometry, material) { Name = name });
    }

    private static void RemoveChildren(Group root)
    {
        foreach (var child in root.Children.ToArray())
        {
            root.Remove(child);
            DisposeTree(child);
        }
    }

    private static void DisposeTree(Object3D item)
    {
        foreach (var child in item.Children.ToArray())
        {
            item.Remove(child);
            DisposeTree(child);
        }
        item.Geometry?.Dispose();
        item.Material?.Dispose();
        item.Dispose();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

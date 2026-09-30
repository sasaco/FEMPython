using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.calculation;
using FrameWebforCS.providers;
using System.Globalization;
using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

internal enum SectionForceEnvelope { Single, Max, Min }

internal readonly record struct SectionForceSample(int MemberId, float Location, float Value,
    SectionForceEnvelope Envelope = SectionForceEnvelope.Single, bool Dummy = false);

internal readonly record struct PanelGradientLegendEntry(int NodeId, float Value, string Text,
    System.Drawing.Color Color);

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
    private readonly List<ViewportTextLabel> _visibleLabels = new();
    private readonly List<PanelGradientLegendEntry> _panelGradientLegend = new();
    private IReadOnlyDictionary<int, Vector3> _nodeData = new Dictionary<int, Vector3>();
    private IReadOnlyDictionary<int, DisplayMember> _memberData = new Dictionary<int, DisplayMember>();
    private IReadOnlyDictionary<int, DisplayPanel> _panelData = new Dictionary<int, DisplayPanel>();
    private Dictionary<string, Dictionary<string, clsDisg>> _disgData = new();
    private Dictionary<string, Dictionary<string, clsReac>> _reacData = new();
    private Dictionary<string, Dictionary<string, Dictionary<string, clsFsec>>> _fsecData = new();
    private readonly Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>>> _derivedFsec = new();
    private readonly Dictionary<string, long> _derivedRevision = new();
    private readonly Dictionary<string, string[]> _movingDisgCases = new(StringComparer.Ordinal);
    private CalculationResultPresentation? _canonical;
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
    internal string CurrentComponent => _currentRadio;

    internal sealed record PrintDerivedState(
        Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>>> Cases,
        Dictionary<string, long> Revisions);

    internal PrintDerivedState CapturePrintDerivedState() =>
        new(new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>>>(_derivedFsec),
            new Dictionary<string, long>(_derivedRevision));

    internal void SetPrintDerivedFsec(string mode, string caseId,
        IReadOnlyList<SectionForceSample> samples)
    {
        ThrowIfDisposed();
        if (mode is not ("comb_fsec" or "pick_fsec"))
            throw new ArgumentOutOfRangeException(nameof(mode));
        if (string.IsNullOrWhiteSpace(caseId) || samples.Count == 0)
            throw new ArgumentException("A derived print diagram needs a case and samples.");
        _derivedFsec[mode] = new Dictionary<string, IReadOnlyList<SectionForceSample>>
        {
            [caseId] = samples.ToArray()
        };
        if (_mode == mode) Redraw();
    }

    internal void RestorePrintDerivedState(PrintDerivedState state)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(state);
        _derivedFsec.Clear();
        foreach (var item in state.Cases) _derivedFsec.Add(item.Key, item.Value);
        _derivedRevision.Clear();
        foreach (var item in state.Revisions) _derivedRevision.Add(item.Key, item.Value);
        Redraw();
    }
    internal string? RenderedDisplacementCase => _renderedDisgCase;
    internal ResultViewportExtrema? CurrentExtrema { get; private set; }
    internal float DisplacementScale => _displacementScale;
    internal float ReactionScale => _reactionScale;
    internal float SectionForceScale => _sectionForceScale;
    internal IReadOnlyList<ViewportTextLabel> GetVisibleLabels() => _visibleLabels;
    internal IReadOnlyList<PanelGradientLegendEntry> GetPanelGradientLegend() => _panelGradientLegend;

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

    internal void SetCanonicalPresentation(CalculationResultPresentation? presentation)
    {
        ThrowIfDisposed();
        _canonical = presentation;
        if (presentation is not null && _mode is ("disg" or "reac" or "fsec") &&
            !presentation.Pages.Any(page => page.Key == _currentIndex &&
                (_mode == "disg" || page.Result is ForceAnalysisResult)))
            _currentIndex = presentation.Pages.FirstOrDefault(page =>
                _mode == "disg" || page.Result is ForceAnalysisResult)?.Key;
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
        if (_canonical is not null)
        {
            CalculationResultPage? page = _canonical.Pages.FirstOrDefault(item => item.Key == _currentIndex);
            if (_mode != "disg" || page is null || page.MovingChildren.Count == 0) return;
            if (++_disgAnimationFrame < 10) return;
            _disgAnimationFrame = 0;
            _disgAnimationIndex = (_disgAnimationIndex + 1) % page.MovingChildren.Count;
            _renderedDisgCase = page.MovingChildren[_disgAnimationIndex].CaseId;
            Redraw();
            return;
        }
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
        if (_mode is "disg" or "comb_disg" or "pik_disg") Redraw();
    }

    internal void SetReactionScale(float value)
    {
        ThrowIfDisposed();
        CheckScale(value, 10, nameof(value));
        _reactionScale = value;
        if (_mode is "reac" or "comb_reac" or "pik_reac") Redraw();
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
        _renderedDisgCase = _mode == "disg"
            ? _canonical?.Pages.FirstOrDefault(page => page.Key == _currentIndex)?.Result.CaseId
                ?? _currentIndex
            : null;
        _disgAnimationFrame = 0;
        _disgAnimationIndex = 0;
    }

    internal void ClearData()
    {
        ThrowIfDisposed();
        _disgData = new();
        _reacData = new();
        _fsecData = new();
        _canonical = null;
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
        _visibleLabels.Clear();
        _panelGradientLegend.Clear();
        RemoveChildren(_disgRoot);
        RemoveChildren(_reacRoot);
        RemoveChildren(_fsecRoot);
        ApplyVisibility();
        if (_currentIndex is null) return;
        if (_canonical is not null)
        {
            DrawCanonical();
            return;
        }
        if (_mode == "disg") DrawDisplacement();
        else if (_mode == "reac") DrawReaction();
        else if (_mode is "fsec" or "comb_fsec" or "pick_fsec") DrawSectionForce();
        // JS three.service.ts:512-556 intentionally hides comb/pick displacement
        // and reaction geometry; their table page changes show no result objects.
    }

    private void ApplyVisibility()
    {
        _disgRoot.Visible = _mode == "disg" || (_canonical is not null &&
            _mode is "comb_disg" or "pik_disg");
        _reacRoot.Visible = _mode == "reac" || (_canonical is not null &&
            _mode is "comb_reac" or "pik_reac");
        _fsecRoot.Visible = _mode is "fsec" or "comb_fsec" or "pick_fsec";
    }

    private void DrawCanonical()
    {
        if (_mode is "comb_disg" or "pik_disg" or "comb_reac" or "pik_reac" or
            "comb_fsec" or "pick_fsec")
        {
            DrawCanonicalDerived();
            return;
        }
        CalculationResultPage? page = _canonical!.Pages.FirstOrDefault(item => item.Key == _currentIndex);
        if (page is null) return;
        switch (_mode)
        {
            case "disg": DrawCanonicalDisplacement(page); break;
            case "reac" when page.Result is ForceAnalysisResult force:
                DrawCanonicalReaction(page, force); break;
            case "fsec" when page.Result is ForceAnalysisResult force:
                DrawCanonicalSectionForce(page, force); break;
        }
    }

    private void DrawCanonicalDerived()
    {
        CalculationDerivedPresentation? derived = _canonical!.Derived;
        if (derived is null || _currentIndex is null) return;
        IReadOnlyList<CalculationDerivedCase> cases = _mode.StartsWith("comb_", StringComparison.Ordinal)
            ? derived.Combines : derived.Pickups;
        CalculationDerivedCase? selected = cases.FirstOrDefault(item => item.Id == _currentIndex);
        if (selected is null) return;
        IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> modes = _mode switch
        {
            "comb_disg" or "pik_disg" => selected.Displacements,
            "comb_reac" or "pik_reac" => selected.Reactions,
            _ => selected.SectionForces
        };
        if (!modes.TryGetValue(_currentRadio, out IReadOnlyList<CalculationDerivedRow>? rows)) return;
        var displacements = new List<NodeDisplacement>();
        var reactions = new List<SupportReaction>();
        var sections = new List<MemberSectionForces>();
        if (_mode is "comb_disg" or "pik_disg")
            displacements.AddRange(rows.Select(row => new NodeDisplacement(row.EntityId,
                new DisplacementComponents(Component(row, "dx"), Component(row, "dy"),
                    Component(row, "dz"), Component(row, "rx"), Component(row, "ry"),
                    Component(row, "rz")))));
        else if (_mode is "comb_reac" or "pik_reac")
            reactions.AddRange(rows.Select(row => new SupportReaction(row.EntityId,
                DerivedForce(row))));
        else
        {
            foreach (TopologyMember member in _canonical.ResultSet.Topology.Members)
            {
                var stationRows = rows.Where(row => row.EntityId == member.MemberId &&
                    row.StationId is not null).ToDictionary(row => row.StationId!, StringComparer.Ordinal);
                var segments = new List<MemberSegmentResult>();
                for (int index = 1; index < member.Stations.Count; index++)
                {
                    MemberStation first = member.Stations[index - 1];
                    MemberStation last = member.Stations[index];
                    if (!stationRows.TryGetValue(first.StationId, out CalculationDerivedRow? rowI) ||
                        !stationRows.TryGetValue(last.StationId, out CalculationDerivedRow? rowJ)) continue;
                    segments.Add(new MemberSegmentResult($"{first.StationId}-{last.StationId}",
                        first.StationId, last.StationId, last.Position - first.Position,
                        DerivedForce(rowI), DerivedForce(rowJ)));
                }
                sections.Add(new MemberSectionForces(member.MemberId, segments));
            }
        }
        var synthetic = new StaticAnalysisResult(selected.Id, displacements, reactions,
            sections, [], [], new WarningDiagnostics([]));
        var page = new CalculationResultPage(selected.Id, selected.Name ?? selected.Id,
            _canonical.ResultSet.Cases[0], synthetic, []);
        switch (_mode)
        {
            case "comb_disg" or "pik_disg": DrawCanonicalDisplacement(page); break;
            case "comb_reac" or "pik_reac": DrawCanonicalReaction(page, synthetic); break;
            case "comb_fsec" or "pick_fsec": DrawCanonicalSectionForce(page, synthetic); break;
        }
    }

    private static double Component(CalculationDerivedRow row, string name) =>
        row.Components.TryGetValue(name, out double value) ? value : 0;

    private static ForceComponents DerivedForce(CalculationDerivedRow row) => new(
        Component(row, "fx"), Component(row, "fy"), Component(row, "fz"),
        Component(row, "mx"), Component(row, "my"), Component(row, "mz"));

    private Dictionary<string, Vector3> CanonicalNodes() => _canonical!.ResultSet.Topology.Nodes
        .ToDictionary(node => node.NodeId, node => new Vector3(
            (float)node.Coordinates.X, (float)node.Coordinates.Y,
            (float)node.Coordinates.Z), StringComparer.Ordinal);

    private void DrawCanonicalDisplacement(CalculationResultPage page)
    {
        IReadOnlyList<NodeDisplacement> rows = page.Result switch
        {
            ForceAnalysisResult force => force.NodeDisplacements,
            ModalAnalysisResult modal => modal.NodeModeShapes,
            _ => []
        };
        if (page.MovingChildren.Count > 0 && _renderedDisgCase is not null)
        {
            StaticAnalysisResult? child = page.MovingChildren.FirstOrDefault(
                item => item.CaseId == _renderedDisgCase);
            if (child is not null) rows = child.NodeDisplacements;
        }
        var displacement = rows.ToDictionary(row => row.NodeId, row => row.Components,
            StringComparer.Ordinal);
        var nodes = CanonicalNodes();
        CurrentExtrema = new ResultViewportExtrema("disg", _renderedDisgCase ?? page.Result.CaseId,
            ValueRange(rows.SelectMany(row => new[]
            {
                (row.NodeId, (double?)row.Components.Dx),
                (row.NodeId, (double?)row.Components.Dy),
                (row.NodeId, (double?)row.Components.Dz)
            })) ?? ZeroRange(),
            ValueRange(rows.SelectMany(row => new[]
            {
                (row.NodeId, (double?)row.Components.Rx),
                (row.NodeId, (double?)row.Components.Ry),
                (row.NodeId, (double?)row.Components.Rz)
            })));
        float maxDistance = (float)NodeDistanceExtrema.Find(nodes.Values.ToArray()).MaxDistance;
        double maxValue = rows.SelectMany(row => new[] { Math.Abs(row.Components.Dx),
            Math.Abs(row.Components.Dy), Math.Abs(row.Components.Dz) }).DefaultIfEmpty().Max();
        float scale = maxValue > 0
            ? (float)(maxDistance * 0.01 / maxValue) * (_displacementScale / 0.5f)
            : _displacementScale * 0.2f;
        var drawnEdges = new HashSet<(string, string)>();
        foreach (TopologyMember member in _canonical.ResultSet.Topology.Members)
        {
            drawnEdges.Add(OrderedEdge(member.NodeI, member.NodeJ));
            AddCanonicalDisplacedMember(member, nodes, displacement, scale);
        }
        foreach (TopologyShellElement shell in _canonical.ResultSet.Topology.ShellElements)
            for (int index = 0; index < shell.NodeIds.Count; index++)
            {
                string first = shell.NodeIds[index];
                string last = shell.NodeIds[(index + 1) % shell.NodeIds.Count];
                if (drawnEdges.Add(OrderedEdge(first, last)))
                    AddCanonicalDisplacedEdge($"shell{shell.ElementId}-{index}", first, last,
                        nodes, displacement, scale);
            }
    }

    private static (string, string) OrderedEdge(string first, string last) =>
        StringComparer.Ordinal.Compare(first, last) <= 0 ? (first, last) : (last, first);

    private void AddCanonicalDisplacedMember(TopologyMember member,
        IReadOnlyDictionary<string, Vector3> nodes,
        IReadOnlyDictionary<string, DisplacementComponents> displacement, float scale)
    {
        if (!nodes.TryGetValue(member.NodeI, out Vector3? start) ||
            !nodes.TryGetValue(member.NodeJ, out Vector3? end) ||
            !displacement.TryGetValue(member.NodeI, out DisplacementComponents? di) ||
            !displacement.TryGetValue(member.NodeJ, out DisplacementComponents? dj)) return;
        Vector3 axis = new Vector3().SubVectors(end, start);
        float length = axis.Length();
        if (length <= 0) return;
        Vector3Value[] basis = [member.LocalFrame.XAxis, member.LocalFrame.YAxis,
            member.LocalFrame.ZAxis];
        float[] global = [(float)di.Dx, (float)di.Dy, (float)di.Dz,
            (float)di.Rx, (float)di.Ry, (float)di.Rz,
            (float)dj.Dx, (float)dj.Dy, (float)dj.Dz,
            (float)dj.Rx, (float)dj.Ry, (float)dj.Rz];
        var local = new float[12];
        for (int block = 0; block < 4; block++)
            for (int row = 0; row < 3; row++)
                local[block * 3 + row] = (float)(basis[row].X * global[block * 3] +
                    basis[row].Y * global[block * 3 + 1] +
                    basis[row].Z * global[block * 3 + 2]);
        int divisions = di.Rx == dj.Rx && di.Ry == dj.Ry && di.Rz == dj.Rz ? 1 : 20;
        var positions = new Vector3[divisions + 1];
        for (int index = 0; index <= divisions; index++)
        {
            float n = (float)index / divisions;
            float n2 = n * n, n3 = n2 * n;
            float x = (1 - n) * local[0] + n * local[6];
            float y = (1 - 3 * n2 + 2 * n3) * local[1] +
                length * (n - 2 * n2 + n3) * local[5] +
                (3 * n2 - 2 * n3) * local[7] + length * (-n2 + n3) * local[11];
            float z = (1 - 3 * n2 + 2 * n3) * local[2] -
                length * (n - 2 * n2 + n3) * local[4] +
                (3 * n2 - 2 * n3) * local[8] - length * (n3 - n2) * local[10];
            positions[index] = new Vector3(
                (1 - n) * start.X + n * end.X +
                (float)(basis[0].X * x + basis[1].X * y + basis[2].X * z) * scale,
                (1 - n) * start.Y + n * end.Y +
                (float)(basis[0].Y * x + basis[1].Y * y + basis[2].Y * z) * scale,
                (1 - n) * start.Z + n * end.Z +
                (float)(basis[0].Z * x + basis[1].Z * y + basis[2].Z * z) * scale);
        }
        AddLine(_disgRoot, $"member{member.MemberId}", positions, 0xFF0000);
    }

    private void AddCanonicalDisplacedEdge(string name, string nodeI, string nodeJ,
        IReadOnlyDictionary<string, Vector3> nodes,
        IReadOnlyDictionary<string, DisplacementComponents> displacement, float scale)
    {
        if (!nodes.TryGetValue(nodeI, out Vector3? start) ||
            !nodes.TryGetValue(nodeJ, out Vector3? end) ||
            !displacement.TryGetValue(nodeI, out DisplacementComponents? di) ||
            !displacement.TryGetValue(nodeJ, out DisplacementComponents? dj)) return;
        AddLine(_disgRoot, name,
        [
            new Vector3(start.X + (float)di.Dx * scale,
                start.Y + (float)di.Dy * scale, start.Z + (float)di.Dz * scale),
            new Vector3(end.X + (float)dj.Dx * scale,
                end.Y + (float)dj.Dy * scale, end.Z + (float)dj.Dz * scale)
        ], 0xFF0000);
    }

    private void DrawCanonicalReaction(CalculationResultPage page, ForceAnalysisResult force)
    {
        // Moving-load table envelopes use signed extrema. The 3D arrows use
        // each child's greatest absolute component, excluding the parent.
        IEnumerable<SupportReaction> source = page.MovingChildren.Count > 0
            ? page.MovingChildren.SelectMany(child => child.SupportReactions)
            : force.SupportReactions;
        SupportReaction[] reactions = source.GroupBy(row => row.NodeId, StringComparer.Ordinal)
            .Select(group => new SupportReaction(group.Key, new ForceComponents(
                MaxAbsolute(group.Select(row => row.Components.Fx)),
                MaxAbsolute(group.Select(row => row.Components.Fy)),
                MaxAbsolute(group.Select(row => row.Components.Fz)),
                MaxAbsolute(group.Select(row => row.Components.Mx)),
                MaxAbsolute(group.Select(row => row.Components.My)),
                MaxAbsolute(group.Select(row => row.Components.Mz)))))
            .ToArray();
        var nodes = CanonicalNodes();
        CurrentExtrema = new ResultViewportExtrema("reac", page.Result.CaseId,
            ValueRange(reactions.SelectMany(row => new[]
            {
                (row.NodeId, (double?)row.Components.Fx),
                (row.NodeId, (double?)row.Components.Fy),
                (row.NodeId, (double?)row.Components.Fz)
            })) ?? ZeroRange(),
            ValueRange(reactions.SelectMany(row => new[]
            {
                (row.NodeId, (double?)row.Components.Mx),
                (row.NodeId, (double?)row.Components.My),
                (row.NodeId, (double?)row.Components.Mz)
            })));
        float maxForce = (float)reactions.SelectMany(row => new[] {
            Math.Abs(row.Components.Fx), Math.Abs(row.Components.Fy), Math.Abs(row.Components.Fz)
        }).DefaultIfEmpty().Max();
        float maxMoment = (float)reactions.SelectMany(row => new[] {
            Math.Abs(row.Components.Mx), Math.Abs(row.Components.My), Math.Abs(row.Components.Mz)
        }).DefaultIfEmpty().Max();
        float extent = _nodeBaseScale * 80 * 0.2f;
        foreach (SupportReaction row in reactions)
        {
            if (!nodes.TryGetValue(row.NodeId, out Vector3? node)) continue;
            ForceComponents value = row.Components;
            AddReactionArrow(row.NodeId, "tx", node, new Vector3(1, 0, 0), value.Fx, maxForce, extent, 0xFF0000);
            AddReactionArrow(row.NodeId, "ty", node, new Vector3(0, 1, 0), value.Fy, maxForce, extent, 0x00FF00);
            AddReactionArrow(row.NodeId, "tz", node, new Vector3(0, 0, 1), value.Fz, maxForce, extent, 0x0000FF);
            AddReactionArrow(row.NodeId, "mx", node, new Vector3(1, 0, 0), value.Mx, maxMoment, extent, 0xFF0000);
            AddReactionArrow(row.NodeId, "my", node, new Vector3(0, 1, 0), value.My, maxMoment, extent, 0x00FF00);
            AddReactionArrow(row.NodeId, "mz", node, new Vector3(0, 0, 1), value.Mz, maxMoment, extent, 0x0000FF);
        }
    }

    private static double MaxAbsolute(IEnumerable<double> values) => values
        .OrderByDescending(Math.Abs).FirstOrDefault();

    private void DrawCanonicalSectionForce(CalculationResultPage page, ForceAnalysisResult force)
    {
        var nodes = CanonicalNodes();
        var members = _canonical!.ResultSet.Topology.Members.ToDictionary(
            item => item.MemberId, StringComparer.Ordinal);
        var values = force.MemberSectionForces.SelectMany(member => member.Segments.Select(segment =>
            (member.MemberId, Segment: segment,
                I: CanonicalComponent(segment.IEnd), J: CanonicalComponent(segment.JEnd)))).ToArray();
        CurrentExtrema = new ResultViewportExtrema("fsec", page.Result.CaseId,
            ValueRange(values.SelectMany(item => new[]
            {
                (item.MemberId, (double?)item.I), (item.MemberId, (double?)item.J)
            })) ?? ZeroRange(), null);
        double max = values.SelectMany(item => new[] { Math.Abs(item.I), Math.Abs(item.J) })
            .DefaultIfEmpty().Max();
        if (max <= 0) return;
        float scale = (float)(_sectionForceScale / 100 * _nodeBaseScale * 5 / max);
        foreach (var item in values)
        {
            if (!members.TryGetValue(item.MemberId, out TopologyMember? member) ||
                !nodes.TryGetValue(member.NodeI, out Vector3? start) ||
                !nodes.TryGetValue(member.NodeJ, out Vector3? end)) continue;
            var stations = member.Stations.ToDictionary(station => station.StationId,
                StringComparer.Ordinal);
            if (!stations.TryGetValue(item.Segment.StationI, out MemberStation? stationI) ||
                !stations.TryGetValue(item.Segment.StationJ, out MemberStation? stationJ)) continue;
            Vector3 axis = new Vector3().SubVectors(end, start);
            float length = axis.Length();
            if (length <= 0) continue;
            Vector3 first = SectionStation(start, axis, length, (float)stationI.Position);
            Vector3 last = SectionStation(start, axis, length, (float)stationJ.Position);
            string selectedComponent = _currentRadio.EndsWith("_max", StringComparison.Ordinal) ||
                _currentRadio.EndsWith("_min", StringComparison.Ordinal)
                ? _currentRadio[..^4] : _currentRadio;
            Vector3Value direction = selectedComponent is "axialForce" or "fx" or
                "torsionalMoment" or "mx" or "momentY" or "my" or "shearForceZ" or "fz"
                ? member.LocalFrame.ZAxis : member.LocalFrame.YAxis;
            Vector3 normal = new((float)direction.X, (float)direction.Y, (float)direction.Z);
            Vector3 firstValue = first.Clone().AddScaledVector(normal, (float)item.I * scale);
            Vector3 lastValue = last.Clone().AddScaledVector(normal, (float)item.J * scale);
            AddLine(_fsecRoot, $"fsec{item.MemberId}-{item.Segment.SegmentId}",
                [first, firstValue, lastValue, last], 0x0000FF);
        }
    }

    private double CanonicalComponent(ForceComponents value) =>
        (_currentRadio.EndsWith("_max", StringComparison.Ordinal) ||
         _currentRadio.EndsWith("_min", StringComparison.Ordinal)
            ? _currentRadio[..^4] : _currentRadio) switch
    {
        "axialForce" or "fx" => value.Fx,
        "shearForceY" or "fy" => value.Fy,
        "shearForceZ" or "fz" => value.Fz,
        "torsionalMoment" or "mx" => value.Mx,
        "momentZ" or "mz" => value.Mz,
        _ => value.My
    };

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
            string nodeKey = nodeId.ToString(CultureInfo.InvariantCulture);
            AddReactionArrow(nodeKey, "tx", node, new Vector3(1, 0, 0), value.tx, maxForce, extent, 0xFF0000);
            AddReactionArrow(nodeKey, "ty", node, new Vector3(0, 1, 0), value.ty, maxForce, extent, 0x00FF00);
            AddReactionArrow(nodeKey, "tz", node, new Vector3(0, 0, 1), value.tz, maxForce, extent, 0x0000FF);
            AddReactionArrow(nodeKey, "mx", node, new Vector3(1, 0, 0), value.mx, maxMoment, extent, 0xFF0000);
            AddReactionArrow(nodeKey, "my", node, new Vector3(0, 1, 0), value.my, maxMoment, extent, 0x00FF00);
            AddReactionArrow(nodeKey, "mz", node, new Vector3(0, 0, 1), value.mz, maxMoment, extent, 0x0000FF);
        }
    }

    private void AddReactionArrow(string nodeId, string component, Vector3 node,
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

    private void AddMomentReaction(string nodeId, string component, Vector3 node,
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
        DrawGradientPanel(samples);
        float max = samples.Max(item => Math.Abs(item.Value));
        if (max <= 0) return;
        var textValues = samples.Where(item => !item.Dummy && float.IsFinite(item.Value))
            .Select(item => item.Value).ToArray();
        var targetValues = textValues.Distinct().OrderByDescending(MathF.Abs).ToArray();
        int textCount = Math.Max(1, (int)Math.Floor(textValues.Length * 0.15));
        var targetList = targetValues.Take(Math.Min(textCount, targetValues.Length)).ToHashSet();
        var labelCandidates = new Dictionary<(double X, double Y, double Z), (Vector3 Position, float Value)>();
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
                if (!first.Dummy && targetList.Contains(first.Value))
                    AddLabelCandidate(labelCandidates, base1, value1, first.Value);
                if (!last.Dummy && targetList.Contains(last.Value))
                    AddLabelCandidate(labelCandidates, base2, value2, last.Value);
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
        foreach (var candidate in labelCandidates.Values
            .OrderByDescending(item => MathF.Abs(item.Value))
            .Take(ViewportTextLabels.MaximumVisibleLabels))
        {
            float rounded = MathF.Floor(candidate.Value * 100 + 0.5f) / 100;
            if (rounded == 0) continue;
            _visibleLabels.Add(new ViewportTextLabel(
                rounded.ToString("F2", CultureInfo.InvariantCulture), candidate.Position));
        }
    }

    private static void AddLabelCandidate(
        Dictionary<(double X, double Y, double Z), (Vector3 Position, float Value)> candidates,
        Vector3 basePosition, Vector3 labelPosition, float value)
    {
        // JS compares baseline positions, then keeps the larger absolute value.
        var key = (Math.Round((double)basePosition.X, 4), Math.Round((double)basePosition.Y, 4),
            Math.Round((double)basePosition.Z, 4));
        if (!candidates.TryGetValue(key, out var current) || MathF.Abs(value) > MathF.Abs(current.Value))
            candidates[key] = (labelPosition, value);
    }

    // JS drawGradientPanel/GetValueTable chooses the greatest signed value at
    // each panel vertex when several member ends meet there.
    private void DrawGradientPanel(IReadOnlyList<SectionForceSample> samples)
    {
        if (_panelData.Count == 0) return;
        var valueTable = GetValueTable(samples);
        var panels = _panelData.Where(item => item.Value.Nodes is { Length: 3 or 4 } &&
            item.Value.Nodes.All(id => _nodeData.ContainsKey(id) && valueTable.ContainsKey(id))).ToArray();
        if (panels.Length == 0) return;
        var values = panels.SelectMany(item => item.Value.Nodes).Select(id => valueTable[id]).ToArray();
        float max = values.Max(), min = values.Min();
        foreach (var (key, panel) in panels)
            CreatePanel(key, panel, valueTable, min, max);
        // JS colorList keeps the first entry for either a repeated node or
        // repeated two-decimal value, then publishes descending values.
        var usedValues = new HashSet<string>();
        foreach (int nodeId in panels.SelectMany(item => item.Value.Nodes).Distinct())
        {
            float value = valueTable[nodeId];
            string text = value.ToString("F2", CultureInfo.InvariantCulture);
            if (!usedValues.Add(text)) continue;
            var rgb = PanelColor(value, min, max);
            _panelGradientLegend.Add(new PanelGradientLegendEntry(nodeId, value, text,
                System.Drawing.Color.FromArgb((int)rgb.R, (int)rgb.G, (int)rgb.B)));
        }
        _panelGradientLegend.Sort((a, b) => b.Value.CompareTo(a.Value));
        if (_panelGradientLegend.Count > ViewportTextLabels.MaximumVisibleLabels)
            _panelGradientLegend.RemoveRange(ViewportTextLabels.MaximumVisibleLabels,
                _panelGradientLegend.Count - ViewportTextLabels.MaximumVisibleLabels);
    }

    private Dictionary<int, float> GetValueTable(IReadOnlyList<SectionForceSample> samples)
    {
        var valueTable = new Dictionary<int, float>();
        foreach (var sample in samples)
        {
            if (!_memberData.TryGetValue(sample.MemberId, out var member) ||
                !_nodeData.TryGetValue(member.Ni, out var ni) ||
                !_nodeData.TryGetValue(member.Nj, out var nj) ||
                !float.IsFinite(sample.Value)) continue;
            float length = new Vector3().SubVectors(nj, ni).Length();
            int nodeId;
            if (MathF.Abs(sample.Location) <= 1e-4f) nodeId = member.Ni;
            else if (MathF.Abs(sample.Location - length) <= 1e-4f) nodeId = member.Nj;
            else continue;
            if (!valueTable.TryGetValue(nodeId, out float current) || sample.Value > current)
                valueTable[nodeId] = sample.Value;
        }
        return valueTable;
    }

    private void CreatePanel(int key, DisplayPanel panel, IReadOnlyDictionary<int, float> valueTable,
        float min, float max)
    {
        var vertices = panel.Nodes.Select(id => _nodeData[id]).ToArray();
        var colors = panel.Nodes.Select(id => PanelColor(valueTable[id], min, max)).ToArray();
        int[] indices = panel.Nodes.Length == 3 ? [0, 1, 2, 0, 2, 1] :
            [0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0];
        var positions = new float[indices.Length * 3];
        var vertexColors = new float[indices.Length * 3];
        for (int i = 0; i < indices.Length; i++)
        {
            int source = indices[i];
            positions[i * 3] = vertices[source].X;
            positions[i * 3 + 1] = vertices[source].Y;
            positions[i * 3 + 2] = vertices[source].Z;
            vertexColors[i * 3] = colors[source].R / 255f;
            vertexColors[i * 3 + 1] = colors[source].G / 255f;
            vertexColors[i * 3 + 2] = colors[source].B / 255f;
        }
        var geometry = new BufferGeometry();
        geometry.SetAttribute("position", new BufferAttribute<float>(positions, 3));
        geometry.SetAttribute("color", new BufferAttribute<float>(vertexColors, 3));
        geometry.ComputeVertexNormals();
        _fsecRoot.Add(new Mesh(geometry, new MeshPhongMaterial
        {
            VertexColors = true, Side = Constants.DoubleSide, FlatShading = true,
            PolygonOffset = true, PolygonOffsetFactor = -1, PolygonOffsetUnits = -1
        }) { Name = "panelGradient-" + key });
    }

    private static (float R, float G, float B) PanelColor(float value, float min, float max)
    {
        // JS arrColors and its max/min/midpoint branches. Avoid a zero divisor
        // in the middle branches when one endpoint of the range is zero.
        if (value == max) return (255, 0, 0);
        if (value == min) return (74, 160, 183);
        float mid = (min + max) / 2;
        if (mid < value)
        {
            float step = max == 0 ? 0 : MathF.Floor(MathF.Abs((max - value) / max) * 50 + 0.5f);
            return (228, Math.Clamp(100 + step, 0, 255), 97);
        }
        else
        {
            float step = min == 0 ? 0 : MathF.Floor((MathF.Abs(value) - MathF.Abs(min)) / MathF.Abs(min) * 50 + 0.5f);
            return (229, Math.Clamp(226 + step, 0, 255), 171);
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
                result.Add(new SectionForceSample(memberId, station, PickComponent(value, true),
                    Dummy: value.dummyi == true));
                station += (float)(value.L ?? 0);
                result.Add(new SectionForceSample(memberId, station, PickComponent(value, false),
                    Dummy: value.dummyj == true));
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

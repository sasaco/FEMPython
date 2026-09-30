using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.calculation;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using SingleFormsDemo;
using System.Diagnostics;
using System.Globalization;
using THREE;

namespace FrameWebforCS.three;

/// <summary>
/// C# counterpart of JS three.service.ts. JS calls fileload/changeData/ChangeMode
/// directly; WinForms receives file, grid, and sidebar events and applies one coalesced
/// batch on the GL-owning thread before render. SceneService remains camera/renderer owner.
/// </summary>
internal sealed class ThreeService : IDisposable
{
    private readonly SceneService _scene;
    private readonly InputNodesService _inputNodes;
    private readonly InputMembersService _inputMembers;
    private readonly InputPanelService _inputPanels;
    private readonly InputLoadService _inputLoads;
    private readonly InputDataService _inputData;
    private readonly AppRoutingModule _routing;
    private readonly ThreeNodesService _nodes;
    private readonly ThreeMembersService _members;
    private readonly ThreePanelsService _panels;
    private readonly ThreeConstraintsService _constraints;
    private readonly ThreeLoadsService _loads;
    private readonly ThreeResultsService _results;
    private readonly object _pendingLock = new();
    private readonly HashSet<int> _pendingIds = new();
    private long _pendingRevision;
    private bool _replacePending = true;
    private PendingEntity _pendingEntities = PendingEntity.All;
    private string? _pendingCase;
    private (string Mode, string? CaseId, string? Component)? _pendingResultPage;
    private readonly Dictionary<string,
        (IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>> Cases,
            long SourceRevision)> _pendingDerivedFsec = new();
    private (string Kind, int? Id, string? Axis)? _pendingSelection;
    private (string Kind, float Value)? _pendingScale;
    private readonly HashSet<UserControl> _boundGridComponents = new();
    private string _loadDisplayMode = "load_names";
    private string _memberDisplayMode = "member";
    private string? _visibleMode;
    private long _lastFrameStamp = Stopwatch.GetTimestamp();
    private bool _disposed;

    [Flags]
    private enum PendingEntity { None = 0, Members = 1, Panels = 2, Constraints = 4, Loads = 8,
        Results = 16, All = Members | Panels | Constraints | Loads | Results }

    internal ThreeService(SceneService scene)
    {
        _scene = scene;
        _inputNodes = InputNodesService.Instance;
        _inputMembers = InputMembersService.Instance;
        _inputPanels = InputPanelService.Instance;
        _inputLoads = InputLoadService.Instance;
        _inputData = InputDataService.Instance;
        _routing = AppRoutingModule.Instance;
        _pendingRevision = _inputData.DocumentRevision;
        _nodes = new ThreeNodesService(scene.scene);
        _members = new ThreeMembersService(scene.scene);
        _panels = new ThreePanelsService(scene.scene);
        _constraints = new ThreeConstraintsService(scene.scene);
        _loads = new ThreeLoadsService(scene.scene);
        _results = new ThreeResultsService(scene.scene);
        _pendingMode = _routing.ActiveModeKey;
        ApplyMode(_routing.ActiveModeKey);
        _inputNodes.NodeEdited += OnNodeEdited;
        _inputMembers.MemberEdited += OnMemberEdited;
        _inputPanels.PanelEdited += OnPanelEdited;
        InputFixNodeService.Instance.Changed += OnConstraintEdited;
        InputFixMemberService.Instance.Changed += OnConstraintEdited;
        InputJointService.Instance.Changed += OnConstraintEdited;
        InputRigidZoneService.Instance.Changed += OnConstraintEdited;
        InputNoticePointsService.Instance.Changed += OnConstraintEdited;
        _inputLoads.LoadsEdited += OnLoadsEdited;
        _inputLoads.SelectedCaseChanged += OnCaseChanged;
        ResultDisgService.Instance.Changed += OnResultEdited;
        ResultReacService.Instance.Changed += OnResultEdited;
        ResultFsecService.Instance.Changed += OnResultEdited;
        CalculationResultStore.Instance.Changed += OnResultEdited;
        ThreeResultsService.ResultPageChanged += OnResultPageChanged;
        ThreeResultsService.DerivedFsecChanged += OnDerivedFsecChanged;
        _inputData.FileReplaced += OnFileReplaced;
        _inputData.DimensionChanged += OnDimensionChanged;
        _routing.InputModeChanged += OnInputModeChanged;
        BindGridComponents();
    }

    internal int? SelectedNodeId => _nodes.SelectedNodeId;
    internal int NodeCount => _nodes.NodeCount;
    internal int MemberCount => _members.MemberCount;
    internal int PanelCount => _panels.PanelCount;
    internal int? SelectedPanelId => _panels.SelectedPanelId;
    internal int ConstraintCount(string kind) => _constraints.Count(kind);
    internal int LoadGlyphCount => _loads.GlyphCount;
    internal int DisplacementCount => _results.DisplacementCount;
    internal int ReactionCount => _results.ReactionCount;
    internal int SectionForceCount => _results.SectionForceCount;
    internal string? ResultCase => _results.CurrentIndex;
    internal ResultViewportExtrema? CurrentResultExtrema => _results.CurrentExtrema;
    internal string? SelectedKind { get; private set; }

    internal readonly record struct PrintState(string? VisibleMode, string LoadDisplayMode,
        string? LoadCase, string ResultMode, string? ResultCase, string ResultComponent,
        float DisplacementScale, float ReactionScale, float SectionForceScale,
        int? NodeId, int? MemberId, int? ElementId, int? PanelId,
        ConstraintSelection? Constraint, (int Row, string Column)? LoadSelection,
        string? SelectedKind, ThreeResultsService.PrintDerivedState DerivedState);

    internal PrintState CapturePrintState()
    {
        FlushPending();
        return new PrintState(_visibleMode, _loadDisplayMode, _loads.CurrentCaseId,
            _results.Mode, _results.CurrentIndex, _results.CurrentComponent,
            _results.DisplacementScale, _results.ReactionScale,
            _results.SectionForceScale, _nodes.SelectedNodeId,
            _members.SelectedMemberId, _members.SelectedElementId,
            _panels.SelectedPanelId, _constraints.Selected, _loads.Selection, SelectedKind,
            _results.CapturePrintDerivedState());
    }

    internal void ApplyPrintView(PrintDiagramRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string mode = request.Mode;
        string caseId = request.CaseId;
        string output = request.Output;
        if (string.IsNullOrWhiteSpace(mode) || string.IsNullOrWhiteSpace(caseId))
            throw new ArgumentException("A print diagram needs a mode and case.");
        string route = mode switch
        {
            "PrintLoad" or "print_load" => "load",
            "disg" => "disg",
            "reac" => "reac",
            "fsec" => "fsec",
            "comb_disg" => "combdisg",
            "pik_disg" => "pickdisg",
            "comb_reac" => "combreac",
            "pik_reac" => "pickreac",
            "comb_fsec" => "combfsec",
            "pick_fsec" => "pickfsec",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode,
                "The print diagram mode is unsupported.")
        };
        if (route == "load") _loadDisplayMode = "load_values";
        ApplyMode(route);
        if (route == "load")
        {
            _loads.SetCase(caseId);
            if (_loads.VisibleGlyphCount == 0)
                throw new InvalidOperationException($"Load case '{caseId}' has no visible diagram.");
            return;
        }

        string resultMode = ResultMode(route);
        if (request.DerivedSamples is { } points)
        {
            if (resultMode is not ("comb_fsec" or "pick_fsec") || points.Count is 0 or > 100_000 ||
                points.Any(point => point.MemberId <= 0 || !float.IsFinite(point.Location) ||
                    !float.IsFinite(point.Value)))
                throw new ArgumentException("Invalid derived section-force print samples.", nameof(request));
            _results.SetPrintDerivedFsec(resultMode, caseId, points.Select(point =>
                new SectionForceSample(point.MemberId, point.Location, point.Value,
                    point.IsMaximum ? SectionForceEnvelope.Max : SectionForceEnvelope.Min)).ToArray());
        }
        _results.SetMode(resultMode, caseId,
            resultMode is "fsec" or "comb_fsec" or "pick_fsec" ? output : null);
        int count = resultMode switch
        {
            "disg" or "comb_disg" or "pik_disg" => _results.DisplacementCount,
            "reac" or "comb_reac" or "pik_reac" => _results.ReactionCount,
            _ => _results.SectionForceCount
        };
        if (count == 0)
            throw new InvalidOperationException($"Result case '{caseId}' has no '{mode}' diagram.");
    }

    internal void RestorePrintState(PrintState state)
    {
        _results.RestorePrintDerivedState(state.DerivedState);
        _loadDisplayMode = state.LoadDisplayMode;
        ApplyMode(state.VisibleMode);
        _loads.SetCase(state.LoadCase);
        _results.SetMode(state.ResultMode, state.ResultCase, state.ResultComponent);
        _results.SetDisplacementScale(state.DisplacementScale);
        _results.SetReactionScale(state.ReactionScale);
        _results.SetSectionForceScale(state.SectionForceScale);
        _nodes.Select(state.NodeId);
        _members.Select(state.MemberId, state.ElementId);
        _panels.Select(state.PanelId);
        _constraints.Select(state.Constraint);
        _members.HighlightRelated(_constraints.SelectedRelatedMemberId);
        if (state.LoadSelection is { } load)
            _loads.Select(load.Row, load.Column);
        SelectedKind = state.SelectedKind;
    }

    internal IEnumerable<ViewportTextLabel> GetVisibleLabels()
    {
        // Other scene owners can contribute their labels here without creating
        // another overlay or another renderer.
        return _results.GetVisibleLabels().Concat(_loads.GetVisibleLabels())
            .Concat(_members.GetVisibleLabels());
    }

    internal IReadOnlyList<PanelGradientLegendEntry> GetPanelGradientLegend() =>
        _results.GetPanelGradientLegend();

    internal (string Kind, string Label, float Value, float Minimum, float Maximum, float Step)?
        GetScaleControl() => EffectiveMode(_visibleMode) switch
        {
            "member" or "element" => ("member", "部材倍率", _members.MemberScale, 0, 1000, 1),
            "fix_node" => ("fix_node", "節点拘束倍率", _constraints.FixNodeScale, 5, 100, 1),
            "fix_member" => ("fix_member", "部材拘束倍率", _constraints.FixMemberScale, 0, 5, 0.1f),
            "load_values" => ("load", "荷重倍率 (%)", _loads.LoadScale, 0, 400, 1),
            _ => ResultMode(_visibleMode) switch
            {
                "disg" or "comb_disg" or "pik_disg" =>
                    ("disg", "変位倍率", _results.DisplacementScale, 0, 2, 0.1f),
                "reac" or "comb_reac" or "pik_reac" =>
                    ("reac", "反力倍率", _results.ReactionScale, 0, 10, 0.1f),
                "fsec" or "comb_fsec" or "pick_fsec" =>
                    ("fsec", "断面力倍率", _results.SectionForceScale, 0, 1000, 1),
                _ => null
            }
        };

    internal void QueueScale(string kind, float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        lock (_pendingLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pendingScale = (kind, value);
        }
    }

    internal void SelectNode(int? id)
    {
        FlushPending();
        _nodes.Select(id);
    }

    private void OnNodeEdited(int id)
    {
        // JS input-nodes.component calls three.changeData("nodes") after an edit.
        // The WinForms BindingList can raise several edits before one frame, so keep IDs once.
        lock (_pendingLock)
        {
            _pendingIds.Add(id);
            _pendingEntities |= PendingEntity.All;
        }
    }

    private void OnMemberEdited(int id)
    {
        // JS changeData("members") refreshes members and load member positions.
        // Also refresh dependent rigid, joint and result geometry after topology edits.
        lock (_pendingLock) _pendingEntities |= PendingEntity.Members | PendingEntity.Constraints |
            PendingEntity.Loads | PendingEntity.Results;
    }

    private void OnPanelEdited(int id)
    {
        lock (_pendingLock) _pendingEntities |= PendingEntity.Panels | PendingEntity.Results;
    }

    private void OnConstraintEdited(object? sender, EventArgs e)
    {
        lock (_pendingLock) _pendingEntities |= PendingEntity.Constraints;
    }

    private void OnLoadsEdited()
    {
        lock (_pendingLock) _pendingEntities |= PendingEntity.Loads;
    }

    private void OnCaseChanged(string caseId)
    {
        lock (_pendingLock)
        {
            _pendingCase = caseId;
            _pendingEntities |= PendingEntity.Constraints;
        }
    }

    private void OnResultEdited(object? sender, EventArgs e)
    {
        lock (_pendingLock) _pendingEntities |= PendingEntity.Results;
    }

    private void OnResultPageChanged(string mode, string? caseId, string? component)
    {
        // JS ChangePage and result service setResultData are direct; C# result controls
        // publish only committed pages, then GL thread applies the latest page once.
        lock (_pendingLock) _pendingResultPage = (mode, caseId, component);
    }

    private void OnDerivedFsecChanged(string mode,
        IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>> cases,
        long sourceRevision)
    {
        lock (_pendingLock) _pendingDerivedFsec[mode] = (cases, sourceRevision);
    }

    private void OnFileReplaced(long revision)
    {
        // JS menu calls three.fileload() after loadInputData(). The revision drops edits
        // queued for the previous file and also covers files opened before GL initialization.
        lock (_pendingLock)
        {
            _pendingRevision = revision;
            _pendingIds.Clear();
            _pendingEntities = PendingEntity.All;
            _pendingCase = _inputLoads.SelectedCaseId;
            _pendingDerivedFsec.Clear();
            _pendingResultPage = null;
            _pendingSelection = null;
            _pendingMode = _routing.ActiveModeKey;
            _replacePending = true;
        }
        SelectedKind = null;
    }

    private void OnDimensionChanged(int _)
    {
        // GL objects are only rebuilt by FlushPending on the viewport owner thread.
        lock (_pendingLock)
        {
            if (_disposed) return;
            _pendingEntities |= PendingEntity.Constraints | PendingEntity.Loads |
                PendingEntity.Results;
            _pendingMode = _routing.ActiveModeKey;
            _pendingResultPage = null;
            _pendingDerivedFsec.Clear();
        }
    }

    private void OnInputModeChanged(string modeKey)
    {
        BindGridComponents();
        // Scene changes are made only from FlushPending on the GL-owning thread.
        lock (_pendingLock)
            _pendingMode = modeKey;
    }

    private void BindGridComponents()
    {
        foreach (var component in _routing.myComponents)
        {
            if (component.IsDisposed || !_boundGridComponents.Add(component)) continue;
            component.Disposed += OnGridComponentDisposed;
            switch (component)
            {
                case InputElementsComponent elements:
                    elements.GridSelectionChanged += OnElementGridSelection;
                    break;
                case InputMembersComponent members:
                    members.GridSelectionChanged += OnMemberGridSelection;
                    members.ActiveMemberDisplayModeChanged += OnMemberDisplayModeChanged;
                    _memberDisplayMode = members.ActiveMemberDisplayMode;
                    break;
                case InputPanelComponent panel:
                    panel.PanelSelected += OnPanelGridSelection;
                    break;
                case InputFixNodeComponent fixNode:
                    fixNode.GridSelectionChanged += OnFixNodeGridSelection;
                    break;
                case InputFixMemberComponent fixMember:
                    fixMember.GridSelectionChanged += OnFixMemberGridSelection;
                    break;
                case InputJointComponent joint:
                    joint.GridSelectionChanged += OnJointGridSelection;
                    break;
                case InputNoticePointsComponent notice:
                    notice.GridSelectionChanged += OnNoticeGridSelection;
                    break;
                case InputLoadComponent load:
                    load.GridSelectionChanged += OnLoadGridSelection;
                    load.ActiveLoadDisplayModeChanged += OnLoadDisplayModeChanged;
                    _loadDisplayMode = load.ActiveLoadDisplayMode;
                    break;
            }
        }
    }

    private void OnGridComponentDisposed(object? sender, EventArgs e)
    {
        if (sender is UserControl component) UnbindGridComponent(component);
    }

    private void UnbindGridComponent(UserControl component)
    {
        if (!_boundGridComponents.Remove(component)) return;
        component.Disposed -= OnGridComponentDisposed;
        switch (component)
        {
            case InputElementsComponent elements:
                elements.GridSelectionChanged -= OnElementGridSelection;
                break;
            case InputMembersComponent members:
                members.GridSelectionChanged -= OnMemberGridSelection;
                members.ActiveMemberDisplayModeChanged -= OnMemberDisplayModeChanged;
                break;
            case InputPanelComponent panel:
                panel.PanelSelected -= OnPanelGridSelection;
                break;
            case InputFixNodeComponent fixNode:
                fixNode.GridSelectionChanged -= OnFixNodeGridSelection;
                break;
            case InputFixMemberComponent fixMember:
                fixMember.GridSelectionChanged -= OnFixMemberGridSelection;
                break;
            case InputJointComponent joint:
                joint.GridSelectionChanged -= OnJointGridSelection;
                break;
            case InputNoticePointsComponent notice:
                notice.GridSelectionChanged -= OnNoticeGridSelection;
                break;
            case InputLoadComponent load:
                load.GridSelectionChanged -= OnLoadGridSelection;
                load.ActiveLoadDisplayModeChanged -= OnLoadDisplayModeChanged;
                break;
        }
    }

    internal void QueueSelection(string kind, int? id, string? axis)
    {
        lock (_pendingLock) _pendingSelection = (kind, id, axis);
    }

    private void OnMemberGridSelection(string kind, int row, string axis) =>
        QueueSelection(kind == "rigid_zone" ? "rigid" : "member", row, axis);
    private void OnElementGridSelection(int row) => QueueSelection("element", row, null);
    private void OnMemberDisplayModeChanged(string mode)
    {
        lock (_pendingLock)
        {
            _memberDisplayMode = mode;
            if (_routing.ActiveModeKey is "member" or "rigid")
                _pendingMode = _routing.ActiveModeKey;
        }
    }
    private void OnPanelGridSelection(int? row) => QueueSelection("shell", row, null);
    private void OnFixNodeGridSelection(int row, string axis) => QueueSelection("fix_node", row, axis);
    private void OnFixMemberGridSelection(int row, string axis) => QueueSelection("fix_member", row, axis);
    private void OnJointGridSelection(int row, string axis) => QueueSelection("joint", row, axis);
    private void OnNoticeGridSelection(int row, string axis) => QueueSelection("notice_points", row, axis);
    private void OnLoadGridSelection(int row, string column) => QueueSelection("load", row, column);
    private void OnLoadDisplayModeChanged(string mode)
    {
        lock (_pendingLock)
        {
            _loadDisplayMode = mode;
            if (_routing.ActiveModeKey == "load") _pendingMode = "load";
        }
    }

    private string? _pendingMode;

    internal void FlushPending()
    {
        bool replace;
        int[] ids;
        string? mode;
        string? caseId;
        PendingEntity entities;
        (string Mode, string? CaseId, string? Component)? resultPage;
        (string Mode, IReadOnlyDictionary<string, IReadOnlyList<SectionForceSample>> Cases,
            long SourceRevision)[] derivedFsec;
        (string Kind, int? Id, string? Axis)? selection;
        (string Kind, float Value)? scale;
        long revision;
        lock (_pendingLock)
        {
            if (_disposed)
                return;
            replace = _replacePending;
            _replacePending = false;
            ids = _pendingIds.ToArray();
            _pendingIds.Clear();
            mode = _pendingMode;
            _pendingMode = null;
            caseId = _pendingCase;
            _pendingCase = null;
            entities = _pendingEntities;
            _pendingEntities = PendingEntity.None;
            resultPage = _pendingResultPage;
            _pendingResultPage = null;
            derivedFsec = _pendingDerivedFsec.Select(item =>
                (item.Key, item.Value.Cases, item.Value.SourceRevision)).ToArray();
            _pendingDerivedFsec.Clear();
            selection = _pendingSelection;
            _pendingSelection = null;
            scale = _pendingScale;
            _pendingScale = null;
            revision = _pendingRevision;
        }

        if (revision != _inputData.DocumentRevision)
        {
            replace = true;
            entities = PendingEntity.All;
        }

        // Load glyphs bake their display scale into geometry, so apply it before rebuilding.
        if (scale is { Kind: "load" } loadScale)
        {
            _loads.SetLoadScale(loadScale.Value);
            entities |= PendingEntity.Loads;
        }

        Dictionary<int, Vector3>? displayNodes = null;
        if (replace)
        {
            displayNodes = GetDisplayNodes();
            _nodes.ReplaceAll(displayNodes);
            entities = PendingEntity.All;
        }
        else
        {
            foreach (int id in ids)
                _nodes.UpdateNode(id, _inputNodes.GetDisplayNode(id), deferScale: true);
            if (ids.Length > 0)
                _nodes.FinishNodeUpdates();
        }

        if (entities != PendingEntity.None)
        {
            displayNodes ??= GetDisplayNodes();
            var displayMembers = _inputMembers.GetDisplayMembers();
            if (entities.HasFlag(PendingEntity.Members))
                _members.ReplaceAll(displayNodes, displayMembers, _nodes.BaseScale);
            if (entities.HasFlag(PendingEntity.Panels))
                _panels.ReplaceAll(displayNodes, _inputPanels.GetDisplayPanels());
            if (entities.HasFlag(PendingEntity.Constraints))
                _constraints.Rebuild(displayNodes,
                    displayMembers.ToDictionary(item => item.Key,
                        item => (item.Value.Ni, item.Value.Nj)),
                    new ConstraintCases(InputFixNodeService.Instance.SelectedCaseId,
                        InputFixMemberService.Instance.SelectedCaseId,
                        InputJointService.Instance.SelectedCaseId),
                    _nodes.BaseScale, _inputData.dimension,
                    memberSpringScale: _nodes.MinDistance);
            if (entities.HasFlag(PendingEntity.Loads))
                _loads.ReplaceAll(_inputLoads.GetDisplaySnapshot(), displayNodes,
                    displayMembers.ToDictionary(item => item.Key,
                        item => new LoadMemberFrame(item.Value.Ni, item.Value.Nj, item.Value.Cg)),
                    _nodes.BaseScale, _inputData.dimension);
            if (replace || ids.Length > 0 || entities.HasFlag(PendingEntity.Members) ||
                entities.HasFlag(PendingEntity.Panels))
                _results.SetTopology(displayNodes, displayMembers,
                    _inputPanels.GetDisplayPanels(), _nodes.BaseScale);
            if (entities.HasFlag(PendingEntity.Results))
            {
                _results.SetBaseResults(ResultsMatchDimension ? ResultDisgService.Instance.getDisg() : new(),
                    ResultsMatchDimension ? ResultReacService.Instance.getReac() : new(),
                    ResultsMatchDimension ? ResultFsecService.Instance.getFsec() : new(),
                    _inputData.DocumentRevision,
                    _inputLoads.GetDisplaySnapshot()
                        .Where(item => item.Value.Symbol == "LL")
                        .Select(item => item.Key).ToArray());
                _results.SetCanonicalPresentation(ResultsMatchDimension
                    ? CalculationResultStore.Instance.Current : null);
            }
        }

        if (caseId != null)
            _loads.SetCase(caseId);
        else if (replace)
            _loads.SetCase(_inputLoads.SelectedCaseId);
        if (mode != null)
            ApplyMode(mode);
        if (ResultsMatchDimension)
            foreach (var derived in derivedFsec)
                _results.SetDerivedFsec(derived.Mode, derived.Cases, derived.SourceRevision);
        if (ResultsMatchDimension && resultPage is { } page &&
            page.Mode == ResultMode(_routing.ActiveModeKey))
            _results.SetMode(page.Mode, page.CaseId, page.Component);
        if (selection is { } selected)
            ApplyGridSelection(selected.Kind, selected.Id, selected.Axis);
        if (scale is { } requestedScale && requestedScale.Kind != "load")
            ApplyScale(requestedScale.Kind, requestedScale.Value);
        long frameStamp = Stopwatch.GetTimestamp();
        _loads.AdvanceAnimation(Math.Clamp((frameStamp - _lastFrameStamp) /
            (float)Stopwatch.Frequency, 0, 1));
        _results.AdvanceDisplacementAnimation();
        _lastFrameStamp = frameStamp;
    }

    private Dictionary<int, Vector3> GetDisplayNodes()
    {
        var displayNodes = new Dictionary<int, Vector3>();
        foreach (var (id, node) in _inputNodes.getNodeJson(0))
            displayNodes.Add(int.Parse(id, CultureInfo.InvariantCulture),
                new Vector3(node.X!.Value, node.Y!.Value, node.Z!.Value));
        return displayNodes;
    }

    private void ApplyScale(string kind, float value)
    {
        switch (kind)
        {
            case "member": _members.SetMemberScale(value); break;
            case "fix_node": _constraints.SetFixNodeScale(value); break;
            case "fix_member": _constraints.SetFixMemberScale(value); break;
            case "disg": _results.SetDisplacementScale(value); break;
            case "reac": _results.SetReactionScale(value); break;
            case "fsec": _results.SetSectionForceScale(value); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private void ApplyMode(string? mode)
    {
        _visibleMode = mode;
        // JS ChangeMode uses plural "nodes"/"members" and "panel"; sidebar keys
        // are singular and "shell". This map also clears hidden selection on route exit.
        _nodes.SetNodeMode(mode == "node");
        string? displayMode = EffectiveMode(mode);
        bool memberText = displayMode is "member" or "element" or "rigid" or "notice_points" or "joint" or "fix_member" or "shell" or "load_values" or
            "fsec" or "basefsec" or "combfsec" or "pickfsec";
        _members.SetMode(true, memberText, displayMode is "member" or "element");
        bool hidePanels = mode is "reac" or "basereac" or "combreac" or "pickreac" or
            "fsec" or "basefsec" or "combfsec" or "pickfsec" or "combdisg" or "pickdisg";
        _panels.SetMode(!hidePanels, mode == "shell" ? 0.7f : 0.3f);
        _constraints.SetMode(displayMode);
        _members.HighlightRelated(null);
        _loads.SetVisible(mode == "load");
        string resultMode = ResultsMatchDimension ? ResultMode(mode) : "";
        string? defaultCase = resultMode switch
        {
            "disg" => ResultDisgService.Instance.getDisg().Keys.FirstOrDefault(),
            "reac" => ResultReacService.Instance.getReac().Keys.FirstOrDefault(),
            "fsec" => ResultFsecService.Instance.getFsec().Keys.FirstOrDefault(),
            _ => null
        };
        if (resultMode is "disg" or "reac" or "fsec" &&
            CalculationResultStore.Instance.Current is { } canonical && ResultsMatchDimension)
            defaultCase = canonical.Pages.FirstOrDefault(page => resultMode == "disg" ||
                page.Result is ForceAnalysisResult)?.Key;
        _results.SetMode(resultMode, defaultCase);
        SelectedKind = null;
        // JS mode branches also set number labels and dat.gui controls. Native controls
        // remain pending; individual entity owners record their related JS symbols.
    }

    private static string ResultMode(string? routeKey) => routeKey switch
    {
        "disg" or "basedisg" => "disg",
        "combdisg" => "comb_disg",
        "pickdisg" => "pik_disg",
        "reac" or "basereac" => "reac",
        "combreac" => "comb_reac",
        "pickreac" => "pik_reac",
        "fsec" or "basefsec" => "fsec",
        "combfsec" => "comb_fsec",
        "pickfsec" => "pick_fsec",
        _ => ""
    };

    private bool ResultsMatchDimension =>
        CalculationResultStore.Instance.Current is not null ||
        _inputData.ResultDimension is not int resultDimension ||
        resultDimension == _inputData.dimension;

    private string? EffectiveMode(string? routeKey) => routeKey switch
    {
        "load" => _loadDisplayMode,
        "member" or "rigid" => _memberDisplayMode,
        _ => routeKey
    };

    private void ApplyGridSelection(string kind, int? id, string? axis)
    {
        // JS selectChange is invoked directly by each input grid. C# grids publish row/column
        // events and the coordinator applies highlights on the GL thread before the next frame.
        if (kind is not ("fix_node" or "fix_member" or "joint" or "rigid" or "notice_points"))
            _members.HighlightRelated(null);
        switch (kind)
        {
            case "node": _nodes.Select(id); break;
            case "member": _members.Select(id); break;
            case "element": _members.Select(null, id); break;
            case "shell": _panels.Select(id); break;
            case "load": _loads.Select(id, axis); break;
            case "fix_node":
            case "fix_member":
            case "joint":
            case "rigid":
            case "notice_points":
                _constraints.Select(id.HasValue ? new ConstraintSelection(kind, id.Value, axis ?? "") : null);
                _members.HighlightRelated(_constraints.SelectedRelatedMemberId);
                break;
        }
        SelectedKind = id.HasValue ? kind : null;
    }

    internal int? SelectAt(int x, int y, int width, int height)
    {
        // JS ThreeComponent handles pointerdown. WinForms waits for a short MouseUp click
        // so TrackballControls drags do not also select a geometry object.
        if (width <= 0 || height <= 0)
            return null;

        FlushPending();
        _scene.scene.UpdateMatrixWorld(true);
        var camera = _scene.CurrentCamera;
        camera.UpdateMatrixWorld(true);
        var raycaster = new Raycaster();
        raycaster.SetFromCamera(new Vector2(2f * x / width - 1f, 1f - 2f * y / height), camera);
        string? mode = EffectiveMode(_routing.ActiveModeKey);
        int? id = null;
        switch (mode)
        {
            case "node":
                id = _nodes.Pick(raycaster);
                if (id.HasValue) { _nodes.Select(id); SelectedKind = "node"; }
                break;
            case "member":
            case "element":
                id = _members.Pick(raycaster);
                if (id.HasValue)
                {
                    _members.Select(id);
                    SelectedKind = "member";
                    ShowMemberSelectionDetail(mode, id.Value);
                }
                break;
            case "shell":
                id = _panels.Pick(raycaster);
                if (id.HasValue)
                {
                    // JS ThreePanelService.detectObject selects red on pointer hits;
                    // selectChange from the grid keeps the default cyan highlight.
                    _panels.Select(id, 0xFF0000);
                    SelectedKind = "shell";
                    foreach (var panel in _routing.myComponents.OfType<InputPanelComponent>())
                        panel.SelectPanel(id);
                }
                break;
            case "fix_node":
            case "fix_member":
            case "joint":
            case "rigid":
            case "notice_points":
                var constraint = _constraints.Pick(raycaster);
                id = constraint?.Row;
                if (constraint is { } hit)
                {
                    _constraints.Select(hit);
                    _members.HighlightRelated(_constraints.SelectedRelatedMemberId);
                    SelectedKind = hit.Kind;
                    foreach (var component in _routing.myComponents)
                        switch (component)
                        {
                            case InputMembersComponent members when hit.Kind == "rigid":
                                members.SelectGridRow("rigid_zone", hit.Row, hit.Axis);
                                break;
                            case InputFixNodeComponent fixNode when hit.Kind == "fix_node":
                                fixNode.SelectGridRow(hit.Row, hit.Axis,
                                    InputFixNodeService.Instance.SelectedCaseId);
                                break;
                            case InputFixMemberComponent fixMember when hit.Kind == "fix_member":
                                fixMember.SelectGridRow(hit.Row, hit.Axis,
                                    InputFixMemberService.Instance.SelectedCaseId);
                                break;
                            case InputJointComponent joint when hit.Kind == "joint":
                                joint.SelectGridRow(hit.Row, hit.Axis,
                                    InputJointService.Instance.SelectedCaseId);
                                break;
                            case InputNoticePointsComponent notice when hit.Kind == "notice_points":
                                notice.SelectGridRow(hit.Row, hit.Axis);
                                break;
                        }
                }
                break;
            // JS three-load.service.ts detectObject() returns immediately; load picking
            // remains disabled here. Grid-driven ThreeLoadsService.Select is supported.
        }
        // JS detectObject returns on a ray miss; retain the previous highlight.
        return id;
    }

    internal void ShowMemberSelectionDetail(string mode, int memberId)
    {
        // The element route can be opened before the member grid is constructed.
        // Keep its detail in the active element component without changing routes.
        if (mode == "element")
        {
            foreach (var elements in _routing.myComponents.OfType<InputElementsComponent>())
                if (!elements.IsDisposed) elements.ShowMemberDetail(memberId);
            return;
        }
        foreach (var members in _routing.myComponents.OfType<InputMembersComponent>())
            if (!members.IsDisposed)
            {
                members.SelectGridRow("members", memberId);
                members.ShowMemberDetail(memberId);
            }
    }

    internal void HoverAt(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0 ||
            EffectiveMode(_routing.ActiveModeKey) is not ("member" or "element"))
        {
            _members.Hover(null);
            return;
        }
        _scene.scene.UpdateMatrixWorld(true);
        var camera = _scene.CurrentCamera;
        camera.UpdateMatrixWorld(true);
        var raycaster = new Raycaster();
        raycaster.SetFromCamera(new Vector2(2f * x / width - 1f,
            1f - 2f * y / height), camera);
        _members.Hover(_members.Pick(raycaster));
    }

    internal void ClearHover() => _members.Hover(null);

    public void Dispose()
    {
        lock (_pendingLock)
        {
            if (_disposed)
                return;
            _disposed = true;
            _pendingIds.Clear();
        }
        _inputNodes.NodeEdited -= OnNodeEdited;
        _inputMembers.MemberEdited -= OnMemberEdited;
        _inputPanels.PanelEdited -= OnPanelEdited;
        InputFixNodeService.Instance.Changed -= OnConstraintEdited;
        InputFixMemberService.Instance.Changed -= OnConstraintEdited;
        InputJointService.Instance.Changed -= OnConstraintEdited;
        InputRigidZoneService.Instance.Changed -= OnConstraintEdited;
        InputNoticePointsService.Instance.Changed -= OnConstraintEdited;
        _inputLoads.LoadsEdited -= OnLoadsEdited;
        _inputLoads.SelectedCaseChanged -= OnCaseChanged;
        ResultDisgService.Instance.Changed -= OnResultEdited;
        ResultReacService.Instance.Changed -= OnResultEdited;
        ResultFsecService.Instance.Changed -= OnResultEdited;
        CalculationResultStore.Instance.Changed -= OnResultEdited;
        ThreeResultsService.ResultPageChanged -= OnResultPageChanged;
        ThreeResultsService.DerivedFsecChanged -= OnDerivedFsecChanged;
        _inputData.FileReplaced -= OnFileReplaced;
        _inputData.DimensionChanged -= OnDimensionChanged;
        _routing.InputModeChanged -= OnInputModeChanged;
        foreach (var component in _boundGridComponents.ToArray())
            UnbindGridComponent(component);
        _results.Dispose();
        _loads.Dispose();
        _constraints.Dispose();
        _panels.Dispose();
        _members.Dispose();
        _nodes.Dispose();
    }
}

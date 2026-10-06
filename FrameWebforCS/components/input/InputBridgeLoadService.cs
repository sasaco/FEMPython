using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FrameWebforCS.providers;

namespace FrameWebforCS.components.input;

internal readonly record struct BridgePoint(double X, double Y, double Z);
internal sealed record BridgePlane(BridgePoint Origin, BridgePoint AxisU, BridgePoint AxisV);
internal sealed record BridgeMeshNode(int Id, BridgePoint Point);
internal sealed record BridgePanel(int Id, string Name, int[] Nodes, int[] Elements,
    int[][] Triangles, int[][] Holes, BridgePlane? Plane, double AbsoluteTolerance,
    double RelativeTolerance, BridgeMeshNode[] LoadingNodes, int[][] LoadingTriangles);
internal sealed record BridgePath(int Id, string Name, BridgePoint[] Points);
internal sealed record BridgeDirection(string Mode, BridgePoint? Vector);
internal sealed record BridgeLoad(int Id, int PanelId, int[] PathIds, double[][] EndIntensities,
    BridgeDirection Direction, string Name);
internal sealed record BridgeLoadSnapshot(IReadOnlyList<BridgePanel> Panels,
    IReadOnlyList<BridgePath> Paths, IReadOnlyDictionary<string, IReadOnlyList<BridgeLoad>> Cases,
    IReadOnlyList<string> Errors);
internal sealed record BridgeSelection(string Kind, int Id, string? CaseId = null, int? PointIndex = null);

// Strings preserve unfinished edits verbatim. A detached, validated snapshot is the
// only input to render/projection; invalid rows remain in save-file draft metadata.
internal abstract class BridgeEditRow : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _values = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    protected string Get(string key) => _values.GetValueOrDefault(key, "");
    protected void Set(string key, string? value)
    {
        value ??= "";
        if (Get(key) == value) return;
        _values[key] = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
    }
    internal bool IsEmpty => _values.Values.All(string.IsNullOrWhiteSpace);
}

internal sealed class BridgePanelRow : BridgeEditRow
{
    public string Id { get => Get(nameof(Id)); set => Set(nameof(Id), value); }
    public string Name { get => Get(nameof(Name)); set => Set(nameof(Name), value); }
    public string Nodes { get => Get(nameof(Nodes)); set => Set(nameof(Nodes), value); }
    public string Triangles { get => Get(nameof(Triangles)); set => Set(nameof(Triangles), value); }
    public string Elements { get => Get(nameof(Elements)); set => Set(nameof(Elements), value); }
    public string Holes { get => Get(nameof(Holes)); set => Set(nameof(Holes), value); }
    public string Origin { get => Get(nameof(Origin)); set => Set(nameof(Origin), value); }
    public string AxisU { get => Get(nameof(AxisU)); set => Set(nameof(AxisU), value); }
    public string AxisV { get => Get(nameof(AxisV)); set => Set(nameof(AxisV), value); }
    public string AbsoluteTolerance { get => Get(nameof(AbsoluteTolerance)); set => Set(nameof(AbsoluteTolerance), value); }
    public string RelativeTolerance { get => Get(nameof(RelativeTolerance)); set => Set(nameof(RelativeTolerance), value); }
    public string LoadingNodes { get => Get(nameof(LoadingNodes)); set => Set(nameof(LoadingNodes), value); }
    public string LoadingTriangles { get => Get(nameof(LoadingTriangles)); set => Set(nameof(LoadingTriangles), value); }
}

internal sealed class BridgePathRow : BridgeEditRow
{
    public string Id { get => Get(nameof(Id)); set => Set(nameof(Id), value); }
    public string Name { get => Get(nameof(Name)); set => Set(nameof(Name), value); }
    public string Order { get => Get(nameof(Order)); set => Set(nameof(Order), value); }
    public string X { get => Get(nameof(X)); set => Set(nameof(X), value); }
    public string Y { get => Get(nameof(Y)); set => Set(nameof(Y), value); }
    public string Z { get => Get(nameof(Z)); set => Set(nameof(Z), value); }
}

internal sealed class BridgeLoadRow : BridgeEditRow
{
    public string CaseId { get => Get(nameof(CaseId)); set => Set(nameof(CaseId), value); }
    public string Id { get => Get(nameof(Id)); set => Set(nameof(Id), value); }
    public string Name { get => Get(nameof(Name)); set => Set(nameof(Name), value); }
    public string PanelId { get => Get(nameof(PanelId)); set => Set(nameof(PanelId), value); }
    public string Path1 { get => Get(nameof(Path1)); set => Set(nameof(Path1), value); }
    public string Path2 { get => Get(nameof(Path2)); set => Set(nameof(Path2), value); }
    public string P11 { get => Get(nameof(P11)); set => Set(nameof(P11), value); }
    public string P12 { get => Get(nameof(P12)); set => Set(nameof(P12), value); }
    public string P21 { get => Get(nameof(P21)); set => Set(nameof(P21), value); }
    public string P22 { get => Get(nameof(P22)); set => Set(nameof(P22), value); }
    public string Direction { get => Get(nameof(Direction)); set => Set(nameof(Direction), value); }
    public string Vx { get => Get(nameof(Vx)); set => Set(nameof(Vx), value); }
    public string Vy { get => Get(nameof(Vy)); set => Set(nameof(Vy), value); }
    public string Vz { get => Get(nameof(Vz)); set => Set(nameof(Vz), value); }
    public string Units => string.IsNullOrWhiteSpace(Path2) ? "kN/m" : "kN/m²";
}

internal sealed class InputBridgeLoadService
{
    private static readonly Lazy<InputBridgeLoadService> _instance = new(() => new());
    internal static InputBridgeLoadService Instance => _instance.Value;
    private bool _applying;
    private BridgeLoadSnapshot? _snapshot;
    internal BindingList<BridgePanelRow> Panels { get; } = new();
    internal BindingList<BridgePathRow> Paths { get; } = new();
    internal BindingList<BridgeLoadRow> Loads { get; } = new();
    internal event Action? Changed;
    internal event Action? DisplayChanged;
    internal event Action<BridgeSelection?>? SelectionChanged;
    internal event Action<string>? ViewRequested;
    internal BridgeSelection? Selection { get; private set; }
    private bool _showEquivalentLoads, _showMesh = true, _showLabels = true;
    internal bool ShowEquivalentLoads { get => _showEquivalentLoads; set { _showEquivalentLoads = value; DisplayChanged?.Invoke(); } }
    internal bool ShowMesh { get => _showMesh; set { _showMesh = value; DisplayChanged?.Invoke(); } }
    internal bool ShowLabels { get => _showLabels; set { _showLabels = value; DisplayChanged?.Invoke(); } }
    internal bool HasData => Panels.Any(r => !r.IsEmpty) || Paths.Any(r => !r.IsEmpty) || Loads.Any(r => !r.IsEmpty);
    internal IEnumerable<string> CaseIds => Loads.Where(r => !r.IsEmpty && TryId(r.CaseId, out _))
        .Select(r => Id(r.CaseId).ToString(CultureInfo.InvariantCulture)).Distinct();
    internal bool HasCase(string id) => CaseIds.Contains(id);

    private InputBridgeLoadService()
    {
        Panels.Add(new()); Paths.Add(new()); Loads.Add(new());
        Panels.ListChanged += Edited; Paths.ListChanged += Edited; Loads.ListChanged += Edited;
    }
    private void Edited(object? sender, ListChangedEventArgs e)
    {
        if (_applying) return;
        _snapshot = null;
        DocumentReplacementNotifications.Publish(Changed);
    }
    internal void SelectEntity(BridgeSelection? selection)
    {
        if (Selection == selection) return;
        Selection = selection;
        if (selection?.CaseId is { } id) InputLoadService.Instance.SelectCase(id);
        DocumentReplacementNotifications.Publish(SelectionChanged, selection);
    }
    internal void RequestView(string view)
    {
        if (view is not ("plan" or "iso")) throw new ArgumentOutOfRangeException(nameof(view));
        ViewRequested?.Invoke(view);
    }
    internal void Clear() => Apply(new([], [], []));
    internal sealed record Prepared(BridgePanelRow[] Panels, BridgePathRow[] Paths, BridgeLoadRow[] Loads);

    internal void Apply(Prepared prepared)
    {
        _applying = true;
        try
        {
            Replace(Panels, prepared.Panels); Replace(Paths, prepared.Paths); Replace(Loads, prepared.Loads);
            _snapshot = null; Selection = null;
        }
        finally { _applying = false; }
        DocumentReplacementNotifications.Publish(Changed);
        DocumentReplacementNotifications.Publish(SelectionChanged, (BridgeSelection?)null);
    }
    private static void Replace<T>(BindingList<T> list, IEnumerable<T> rows) where T : new()
    {
        list.RaiseListChangedEvents = false;
        list.Clear(); foreach (var row in rows) list.Add(row);
        list.Add(new T());
        list.RaiseListChangedEvents = true;
        DocumentReplacementNotifications.Defer(list.ResetBindings);
    }

    internal BridgeLoadSnapshot GetSnapshot()
    {
        // Build every time to keep callers' mutable arrays detached from service state.
        return Build(Panels, Paths, Loads);
    }
    internal IReadOnlyList<string> Errors => (_snapshot ??= GetSnapshot()).Errors;
    internal JsonObject GetSaveJson()
    {
        var snapshot = GetSnapshot();
        var result = ToJson(snapshot, includeNames: true);
        result["version"] = 1;
        if (snapshot.Errors.Count != 0)
            result["drafts"] = JsonSerializer.SerializeToNode(new Prepared(
                Panels.Where(r => !r.IsEmpty).ToArray(), Paths.Where(r => !r.IsEmpty).ToArray(),
                Loads.Where(r => !r.IsEmpty).ToArray()));
        return result;
    }

    internal static Prepared Parse(JsonElement root)
    {
        try
        {
            var source = JsonNode.Parse(root.GetRawText()) as JsonObject ?? throw new FormatException("Input must be an object.");
            JsonObject data;
            if (source["bridge_loads"] != null)
            {
                if (HasSolverBridge(source)) throw new FormatException("bridge_loadsと解析用の橋面荷重が重複しています。");
                data = ReadObject(source["bridge_loads"]);
            }
            else data = MigrateSolverBridge(source);
            Known(data, "version", "panels", "paths", "cases", "drafts");
            if (data["version"] != null && Int(data["version"]) != 1) throw new FormatException("Unsupported bridge_loads version.");
            var prepared = ReadCanonical(data);
            var snapshot = Build(prepared.Panels, prepared.Paths, prepared.Loads);
            if (snapshot.Errors.Count > 0) throw new FormatException(string.Join("\n", snapshot.Errors));
            if (data["drafts"] != null)
            {
                prepared = data["drafts"]!.Deserialize<Prepared>() ?? throw new FormatException("Invalid bridge drafts.");
                if (prepared.Panels == null || prepared.Paths == null || prepared.Loads == null ||
                    prepared.Panels.Any(r => r == null) || prepared.Paths.Any(r => r == null) || prepared.Loads.Any(r => r == null))
                    throw new FormatException("Invalid bridge drafts.");
                if (prepared.Panels.Length + prepared.Paths.Length + prepared.Loads.Length > 100_000)
                    throw new FormatException("Too many bridge draft rows.");
            }
            return prepared;
        }
        catch (Exception e) when (e is FormatException or InvalidOperationException or OverflowException or ArgumentException)
        { throw new JsonException("橋面荷重: " + e.Message, e); }
    }

    private static bool HasSolverBridge(JsonObject root) => root.ContainsKey("spatial_loads") ||
        root.ContainsKey("inf_panel") || root.ContainsKey("line") || (root["load"] is JsonObject cases && cases.Any(c => c.Value is JsonObject value &&
            (value.ContainsKey("load_inf") || value.ContainsKey("spatial_loads"))));

    // Only this import boundary handles legacy XY and case-local solver records.
    // Conflicting per-case definition IDs are reallocated while preserving references.
    private static JsonObject MigrateSolverBridge(JsonObject root)
    {
        var panels = new JsonArray(); var paths = new JsonArray(); var cases = new JsonObject();
        var result = new JsonObject { ["panels"] = panels, ["paths"] = paths, ["cases"] = cases };
        if (!HasSolverBridge(root)) return result;
        var sourceCases = root["load"] as JsonObject ?? new JsonObject();
        if (root.ContainsKey("spatial_loads") && sourceCases.Count != 1)
            throw new FormatException("共通spatial_loadsの取込には実荷重ケースを1件にしてください。複数ケースはケース別spatial_loadsで指定します。");
        foreach (var (caseId, value) in sourceCases)
        {
            var loadCase = ReadObject(value);
            var legacy = ReadArray(loadCase["load_inf"]);
            var canonical = loadCase["spatial_loads"] ?? root["spatial_loads"];
            if (loadCase.ContainsKey("spatial_loads") && root.ContainsKey("spatial_loads") || canonical != null && legacy.Count > 0)
                throw new FormatException($"実荷重 {caseId}: 橋面荷重が重複しています。");
            if (canonical != null)
            {
                var definition = ReadObject(canonical); Known(definition, "panels", "paths", "loads");
                bool fromRoot = loadCase["spatial_loads"] == null;
                var shellIds = fromRoot ? RootShellIds(root) : null;
                var panelMap = ReadArray(definition["panels"]).Select(p => ReadObject(p))
                    .ToDictionary(p => Int(p["id"]), p => MergeDefinition(panels,
                        shellIds == null ? p : TranslateRootShellReferences(p, shellIds)));
                var pathMap = ReadArray(definition["paths"]).Select(p => ReadObject(p))
                    .ToDictionary(p => Int(p["id"]), p => MergeDefinition(paths, p));
                var records = new JsonArray();
                foreach (var entry in ReadArray(definition["loads"]))
                {
                    var record = (JsonObject)ReadObject(entry).DeepClone();
                    int panelId = Int(record["panel_id"]);
                    if (!panelMap.TryGetValue(panelId, out int mappedPanel)) throw new FormatException($"面 {panelId} が未定義です。");
                    record["panel_id"] = mappedPanel;
                    record["path_ids"] = new JsonArray(ReadArray(record["path_ids"], true).Select(p =>
                        (JsonNode?)JsonValue.Create(pathMap.TryGetValue(Int(p), out int mapped) ? mapped : throw new FormatException($"線 {p} が未定義です。"))).ToArray());
                    records.Add(record);
                }
                cases[caseId] = records;
                continue;
            }
            if (legacy.Count == 0) continue;
            int panel = loadCase["inf_panel"] == null ? 1 : Int(loadCase["inf_panel"]);
            var definitions = ReadObject(root["inf_panel"]);
            var sourcePanel = (JsonObject)Lookup(definitions, panel).DeepClone();
            Known(sourcePanel, "nodes", "elements", "triangles", "holes", "tolerance", "name");
            sourcePanel["id"] = panel;
            if (ReadArray(sourcePanel["elements"]).Count == 0 && ReadArray(sourcePanel["triangles"]).Count == 0)
            {
                var ids = ReadArray(sourcePanel["nodes"], true).Select(Int).ToHashSet();
                var shells = root["shell"] as JsonObject;
                var references = shells?.Where(pair => pair.Value is JsonObject shell &&
                    ReadArray(shell["nodes"], true).Select(Int).All(ids.Contains)).Select(pair => Id(pair.Key)).ToArray() ?? [];
                if (references.Length == 0) throw new FormatException($"面 {panel}: 三角形または既存面要素の指定が必要です。");
                sourcePanel["elements"] = Array(references);
            }
            int mapped = MergeDefinition(panels, sourcePanel);
            var recordsLegacy = new JsonArray();
            foreach (var entry in legacy)
            {
                var record = ReadObject(entry); Known(record, "L1", "P11", "P12", "L2", "P21", "P22");
                bool area = record.ContainsKey("L2");
                if (!area && (record.ContainsKey("P21") || record.ContainsKey("P22"))) throw new FormatException("P21/P22にはL2が必要です。");
                var pathIds = new JsonArray(); var intensities = new JsonArray();
                foreach (int side in area ? new[] { 1, 2 } : new[] { 1 })
                {
                    int pathId = Int(record["L" + side]);
                    var path = Lookup(ReadObject(root["line"]), pathId);
                    Known(path, "position", "name");
                    var points = new JsonArray();
                    foreach (var point in ReadArray(path["position"], true))
                    {
                        var xy = ReadObject(point); Known(xy, "x", "y");
                        points.Add(new JsonArray(Number(xy["x"]?.ToString() ?? ""), Number(xy["y"]?.ToString() ?? ""), 0d));
                    }
                    var normalized = new JsonObject { ["id"] = pathId, ["points"] = points };
                    if (path["name"] != null) normalized["name"] = path["name"]!.DeepClone();
                    pathIds.Add(MergeDefinition(paths, normalized));
                    intensities.Add(new JsonArray(Number(record[$"P{side}1"]?.ToString() ?? ""), Number(record[$"P{side}2"]?.ToString() ?? "")));
                }
                recordsLegacy.Add(new JsonObject { ["id"] = recordsLegacy.Count + 1, ["panel_id"] = mapped,
                    ["path_ids"] = pathIds, ["end_intensities"] = intensities });
            }
            cases[caseId] = recordsLegacy;
        }
        // Keep unreferenced legacy definitions too: they may still be under editing.
        if (root["inf_panel"] is JsonObject legacyPanels)
            foreach (var (key, value) in legacyPanels)
            {
                int id = Id(key);
                if (panels.Any(p => Int(p!["id"]) == id)) continue;
                var panel = (JsonObject)ReadObject(value).DeepClone(); panel["id"] = id;
                MergeDefinition(panels, panel);
            }
        if (root["line"] is JsonObject legacyPaths)
            foreach (var (key, value) in legacyPaths)
            {
                int id = Id(key); if (paths.Any(p => Int(p!["id"]) == id)) continue;
                var path = ReadObject(value); Known(path, "position", "name");
                var points = new JsonArray();
                foreach (var point in ReadArray(path["position"], true))
                {
                    var xy = ReadObject(point); Known(xy, "x", "y");
                    points.Add(new JsonArray(Number(xy["x"]?.ToString() ?? ""), Number(xy["y"]?.ToString() ?? ""), 0d));
                }
                var normalized = new JsonObject { ["id"] = id, ["points"] = points };
                if (path["name"] != null) normalized["name"] = path["name"]!.DeepClone();
                MergeDefinition(paths, normalized);
            }
        return result;
    }
    private static int MergeDefinition(JsonArray definitions, JsonObject source)
    {
        int id = Int(source["id"]);
        var match = definitions.FirstOrDefault(p => Int(p!["id"]) == id);
        if (match != null && JsonNode.DeepEquals(match, source)) return id;
        if (match != null)
        {
            var used = definitions.Select(p => Int(p!["id"])).ToHashSet();
            id = Enumerable.Range(1, 100_000).FirstOrDefault(candidate => !used.Contains(candidate));
            if (id == 0) throw new FormatException("面・線の番号が上限を超えています。");
        }
        var clone = (JsonObject)source.DeepClone(); clone["id"] = id; definitions.Add(clone); return id;
    }
    private static JsonObject Lookup(JsonObject definitions, int id)
    {
        var matches = definitions.Where(p => Id(p.Key) == id).ToArray();
        if (matches.Length != 1) throw new FormatException($"定義 {id} が未定義または重複しています。");
        return ReadObject(matches[0].Value);
    }
    private static Dictionary<int, int> RootShellIds(JsonObject root)
    {
        // file_io._read_legacy_json_model allocates collisions above every public
        // member/shell/solid ID, in shell insertion order, before member subdivision.
        int[] allIds = new[] { "member", "shell", "solid" }.SelectMany(key =>
            (root[key] as JsonObject ?? new JsonObject()).Select(p => Id(p.Key))).ToArray();
        int next = allIds.DefaultIfEmpty(0).Max() + 1;
        var used = (root["member"] as JsonObject ?? new JsonObject()).Select(p => Id(p.Key)).ToHashSet();
        var result = new Dictionary<int, int>();
        var publicShellIds = new HashSet<int>();
        foreach (var (key, _) in root["shell"] as JsonObject ?? new JsonObject())
        {
            int publicId = Id(key);
            if (!publicShellIds.Add(publicId)) throw new FormatException("面要素Noが重複しています。");
            int unifiedId = used.Contains(publicId) ? next++ : publicId;
            used.Add(unifiedId); result.Add(unifiedId, publicId);
        }
        return result;
    }
    private static JsonObject TranslateRootShellReferences(JsonObject panel, Dictionary<int, int> shellIds)
    {
        var result = (JsonObject)panel.DeepClone();
        if (result["elements"] == null) return result;
        var elements = new JsonArray();
        foreach (var value in ReadArray(result["elements"], true))
        {
            if (!int.TryParse(value?.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out int unifiedId) ||
                !shellIds.TryGetValue(unifiedId, out int publicId))
                throw new FormatException($"共通spatial_loadsの要素 {value} に対応する面要素がありません。");
            elements.Add(publicId);
        }
        result["elements"] = elements;
        return result;
    }

    internal static IReadOnlyDictionary<string, JsonObject> ProjectCases(JsonElement root)
    {
        var prepared = Parse(root);
        var snapshot = Build(prepared.Panels, prepared.Paths, prepared.Loads);
        if (snapshot.Errors.Count > 0) throw new JsonException("橋面荷重の入力を確認してください: " + string.Join("\n", snapshot.Errors));
        if (root.TryGetProperty("dimension", out var dimension) && dimension.GetInt32() == 2 &&
            (prepared.Panels.Length + prepared.Paths.Length + prepared.Loads.Length != 0))
            throw new JsonException("橋面荷重は3D解析専用です。入力を保持したまま3Dへ戻してください。");
        var result = new Dictionary<string, JsonObject>();
        foreach (var (caseId, loads) in snapshot.Cases)
        {
            if (loads.Count == 0) continue;
            var panels = loads.Select(l => l.PanelId).ToHashSet();
            var paths = loads.SelectMany(l => l.PathIds).ToHashSet();
            result.Add(caseId, new JsonObject
            {
                ["panels"] = new JsonArray(snapshot.Panels.Where(p => panels.Contains(p.Id)).Select(p => (JsonNode)PanelJson(p, false)).ToArray()),
                ["paths"] = new JsonArray(snapshot.Paths.Where(p => paths.Contains(p.Id)).Select(p => (JsonNode)PathJson(p, false)).ToArray()),
                ["loads"] = new JsonArray(loads.Select(l => (JsonNode)LoadJson(l, false)).ToArray())
            });
        }
        return result;
    }

    private static BridgeLoadSnapshot Build(IEnumerable<BridgePanelRow> panelRows,
        IEnumerable<BridgePathRow> pathRows, IEnumerable<BridgeLoadRow> loadRows)
    {
        var errors = new List<string>(); var panels = new List<BridgePanel>(); var paths = new List<BridgePath>();
        var cases = new Dictionary<string, IReadOnlyList<BridgeLoad>>();
        foreach (var row in panelRows.Where(r => !r.IsEmpty))
            Try("面 " + row.Id, () =>
            {
                int id = Id(row.Id); int[] nodes = Ids(row.Nodes); int[] elements = Ids(row.Elements);
                int[][] triangles = Rings(row.Triangles); int[][] holes = Rings(row.Holes);
                if (panels.Any(p => p.Id == id)) throw new FormatException("面Noが重複しています。");
                if (nodes.Length < 3 || nodes.Distinct().Count() != nodes.Length) throw new FormatException("3個以上の異なる節点が必要です。");
                if ((elements.Length == 0) == (triangles.Length == 0)) throw new FormatException("三角形または面要素Noの一方を指定してください。");
                if (elements.Distinct().Count() != elements.Length) throw new FormatException("面要素Noが重複しています。");
                if (triangles.Any(t => t.Length != 3 || t.Distinct().Count() != 3 || t.Any(n => !nodes.Contains(n))))
                    throw new FormatException("三角形は面内の異なる3節点で指定してください。");
                BridgePlane? plane = null;
                if (new[] { row.Origin, row.AxisU, row.AxisV }.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    plane = new(Point(row.Origin), Point(row.AxisU), Point(row.AxisV));
                    var u = plane.AxisU; var v = plane.AxisV;
                    double cross = Math.Pow(u.Y * v.Z - u.Z * v.Y, 2) + Math.Pow(u.Z * v.X - u.X * v.Z, 2) + Math.Pow(u.X * v.Y - u.Y * v.X, 2);
                    double norm = Math.Sqrt((u.X*u.X + u.Y*u.Y + u.Z*u.Z) * (v.X*v.X + v.Y*v.Y + v.Z*v.Z));
                    if (!double.IsFinite(cross) || cross <= 1e-24 || !double.IsFinite(norm) ||
                        Math.Abs(u.X*v.X + u.Y*v.Y + u.Z*v.Z) > 1e-12 * norm)
                        throw new FormatException("平面U/V軸はゼロでない直交方向を指定してください。");
                }
                double absolute = string.IsNullOrWhiteSpace(row.AbsoluteTolerance) ? 1e-9 : Number(row.AbsoluteTolerance);
                double relative = string.IsNullOrWhiteSpace(row.RelativeTolerance) ? 1e-9 : Number(row.RelativeTolerance);
                if (absolute < 0 || relative < 0 || absolute == 0 && relative == 0)
                    throw new FormatException("許容差は非負で、少なくとも一方を正にしてください。");
                var loading = string.IsNullOrWhiteSpace(row.LoadingNodes) ? [] : ReadLoadingNodes(JsonNode.Parse(row.LoadingNodes));
                int[][] loadingTriangles = Rings(row.LoadingTriangles);
                if ((loading.Length == 0) != (loadingTriangles.Length == 0) || loading.Select(n => n.Id).Distinct().Count() != loading.Length ||
                    loadingTriangles.Any(t => t.Length != 3 || t.Distinct().Count() != 3 || t.Any(n => !loading.Any(p => p.Id == n))))
                    throw new FormatException("独立載荷メッシュの節点と三角形を確認してください。");
                var holeNodes = loading.Length > 0 ? loading.Select(n => n.Id).ToHashSet() : nodes.ToHashSet();
                if (holes.Any(h => h.Length < 3 || h.Distinct().Count() != h.Length || h.Any(n => !holeNodes.Contains(n))))
                    throw new FormatException(loading.Length > 0
                        ? "孔は独立載荷メッシュ内の異なる3節点以上で指定してください。"
                        : "孔は面内の異なる3節点以上で指定してください。");
                panels.Add(new(id, row.Name, nodes, elements, triangles, holes, plane, absolute, relative, loading, loadingTriangles));
            }, errors);
        foreach (var group in pathRows.Where(r => !r.IsEmpty).GroupBy(r => r.Id))
            Try("線 " + group.Key, () =>
            {
                int id = Id(group.Key);
                var rows = group.Select(r => (Order: Id(r.Order), Row: r)).OrderBy(r => r.Order).ToArray();
                if (paths.Any(p => p.Id == id) || rows.Length < 2 || !rows.Select(r => r.Order).SequenceEqual(Enumerable.Range(1, rows.Length)))
                    throw new FormatException("点順を1から連続で指定し、2点以上入力してください。");
                var points = rows.Select(r => new BridgePoint(Number(r.Row.X), Number(r.Row.Y), Number(r.Row.Z))).ToArray();
                if (points.Zip(points.Skip(1)).Any(pair => pair.First == pair.Second)) throw new FormatException("連続点が同じ座標です。");
                paths.Add(new(id, rows[0].Row.Name, points));
            }, errors);
        foreach (var group in loadRows.Where(r => !r.IsEmpty).GroupBy(r => r.CaseId))
        {
            var values = new List<BridgeLoad>();
            string? caseId = null;
            Try("実荷重 " + group.Key, () => caseId = Id(group.Key).ToString(CultureInfo.InvariantCulture), errors);
            if (caseId == null) continue;
            foreach (var row in group)
                Try($"実荷重 {caseId} / 荷重 {row.Id}", () =>
                {
                    int id = Id(row.Id), panel = Id(row.PanelId), path1 = Id(row.Path1);
                    bool area = !string.IsNullOrWhiteSpace(row.Path2);
                    int[] pathIds = area ? [path1, Id(row.Path2)] : [path1];
                    if (values.Any(l => l.Id == id)) throw new FormatException("荷重Noが重複しています。");
                    if (!panels.Any(p => p.Id == panel) || pathIds.Any(p => !paths.Any(path => path.Id == p)))
                        throw new FormatException("参照する面または線が未完成・未定義です。");
                    if (area && pathIds[0] == pathIds[1]) throw new FormatException("面荷重の線1と線2は異なる線を指定してください。");
                    if (!area && (!string.IsNullOrWhiteSpace(row.P21) || !string.IsNullOrWhiteSpace(row.P22)))
                        throw new FormatException("P21/P22には線2が必要です。");
                    double[][] intensities = area ? [[Number(row.P11), Number(row.P12)], [Number(row.P21), Number(row.P22)]] : [[Number(row.P11), Number(row.P12)]];
                    string mode = string.IsNullOrWhiteSpace(row.Direction) ? "global" : row.Direction.Trim().ToLowerInvariant();
                    BridgePoint? vector = null;
                    if (mode == "global")
                    {
                        vector = new BridgePoint(string.IsNullOrWhiteSpace(row.Vx) ? 0 : Number(row.Vx),
                            string.IsNullOrWhiteSpace(row.Vy) ? 0 : Number(row.Vy), string.IsNullOrWhiteSpace(row.Vz) ? 1 : Number(row.Vz));
                        if (vector.Value == new BridgePoint(0, 0, 0)) throw new FormatException("荷重方向ベクトルはゼロにできません。");
                    }
                    else if (mode != "normal") throw new FormatException("方向はglobalまたはnormalです。");
                    else if (new[] { row.Vx, row.Vy, row.Vz }.Any(s => !string.IsNullOrWhiteSpace(s)))
                        throw new FormatException("normal方向ではVx/Vy/Vzを空欄にしてください。");
                    values.Add(new(id, panel, pathIds, intensities, new(mode, vector), row.Name));
                }, errors);
            if (cases.ContainsKey(caseId)) errors.Add($"実荷重 {caseId}: 番号が重複しています。");
            else cases.Add(caseId, values);
        }
        return new(panels, paths, cases, errors);
    }

    private static void Try(string label, Action action, List<string> errors)
    {
        try { action(); }
        catch (Exception e) when (e is FormatException or JsonException or InvalidOperationException or OverflowException)
        { errors.Add(label + ": " + e.Message); }
    }
    private static bool TryId(string text, out int value) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value is > 0 and <= 100_000;
    private static int Id(string text) => TryId(text, out int value) ? value : throw new FormatException("番号は1～100000の整数で入力してください。");
    private static int Int(JsonNode? node) => Id(node?.ToString() ?? "");
    private static double Number(string text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value)
        ? value : throw new FormatException("有限の数値を入力してください。");
    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static int[] Ids(string text) => text.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Select(Id).ToArray();
    private static int[][] Rings(string text) => text.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Ids).ToArray();
    private static BridgePoint Point(string text)
    {
        var values = text.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Select(Number).ToArray();
        if (values.Length != 3) throw new FormatException("X,Y,Zの3成分で入力してください。");
        return new(values[0], values[1], values[2]);
    }
    private static JsonArray Array(IEnumerable<int> values) => new(values.Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());
    private static JsonArray PointJson(BridgePoint point) => new(point.X, point.Y, point.Z);
    private static JsonArray RingsJson(int[][] values) => new(values.Select(v => (JsonNode)Array(v)).ToArray());
    private static JsonObject PanelJson(BridgePanel p, bool names)
    {
        var result = new JsonObject { ["id"] = p.Id, ["nodes"] = Array(p.Nodes), ["elements"] = Array(p.Elements),
            ["triangles"] = RingsJson(p.Triangles), ["holes"] = RingsJson(p.Holes),
            ["tolerance"] = new JsonObject { ["absolute_length"] = p.AbsoluteTolerance, ["relative_length"] = p.RelativeTolerance } };
        if (names && p.Name.Length > 0) result["name"] = p.Name;
        if (p.Plane is { } plane) result["plane"] = new JsonObject { ["origin"] = PointJson(plane.Origin), ["axis_u"] = PointJson(plane.AxisU), ["axis_v"] = PointJson(plane.AxisV) };
        if (p.LoadingNodes.Length > 0)
        {
            result["loading_nodes"] = new JsonArray(p.LoadingNodes.Select(n => (JsonNode)new JsonObject { ["id"] = n.Id, ["point"] = PointJson(n.Point) }).ToArray());
            result["loading_triangles"] = RingsJson(p.LoadingTriangles);
        }
        return result;
    }
    private static JsonObject PathJson(BridgePath p, bool names)
    {
        var result = new JsonObject { ["id"] = p.Id, ["points"] = new JsonArray(p.Points.Select(p => (JsonNode)PointJson(p)).ToArray()) };
        if (names && p.Name.Length > 0) result["name"] = p.Name;
        return result;
    }
    private static JsonObject LoadJson(BridgeLoad load, bool names)
    {
        var direction = new JsonObject { ["mode"] = load.Direction.Mode };
        if (load.Direction.Vector is { } vector) direction["vector"] = PointJson(vector);
        var result = new JsonObject { ["id"] = load.Id, ["panel_id"] = load.PanelId, ["path_ids"] = Array(load.PathIds),
            ["end_intensities"] = new JsonArray(load.EndIntensities.Select(p => (JsonNode)new JsonArray(p[0], p[1])).ToArray()), ["direction"] = direction };
        if (names && load.Name.Length > 0) result["name"] = load.Name;
        return result;
    }
    private static JsonObject ToJson(BridgeLoadSnapshot snapshot, bool includeNames)
    {
        var cases = new JsonObject();
        foreach (var (id, loads) in snapshot.Cases) cases[id] = new JsonArray(loads.Select(l => (JsonNode)LoadJson(l, includeNames)).ToArray());
        return new JsonObject { ["panels"] = new JsonArray(snapshot.Panels.Select(p => (JsonNode)PanelJson(p, includeNames)).ToArray()),
            ["paths"] = new JsonArray(snapshot.Paths.Select(p => (JsonNode)PathJson(p, includeNames)).ToArray()), ["cases"] = cases };
    }

    private static JsonArray ReadArray(JsonNode? node, bool required = false) => node as JsonArray ??
        (node == null && !required ? new JsonArray() : throw new FormatException("配列が必要です。"));
    private static JsonObject ReadObject(JsonNode? node) => node as JsonObject ?? throw new FormatException("オブジェクトが必要です。");
    private static void Known(JsonObject value, params string[] keys)
    {
        var unknown = value.Select(p => p.Key).Except(keys).ToArray();
        if (unknown.Length != 0) throw new FormatException("未対応の入力項目: " + string.Join(", ", unknown));
    }
    private static string IdList(JsonNode? node, bool required = false) => string.Join(",", ReadArray(node, required).Select(Int));
    private static string RingList(JsonNode? node) => string.Join(";", ReadArray(node).Select(n => IdList(n, true)));
    private static string PointText(JsonNode? node)
    {
        var values = ReadArray(node, true);
        if (values.Count != 3) throw new FormatException("座標は3成分が必要です。");
        return string.Join(",", values.Select(n => F(Number(n?.ToString() ?? ""))));
    }
    private static BridgeMeshNode[] ReadLoadingNodes(JsonNode? node) => ReadArray(node).Select(n =>
    {
        var value = ReadObject(n); Known(value, "id", "point");
        return new BridgeMeshNode(Int(value["id"]), Point(PointText(value["point"])));
    }).ToArray();
    private static Prepared ReadCanonical(JsonObject data)
    {
        var panels = new List<BridgePanelRow>(); var paths = new List<BridgePathRow>(); var loads = new List<BridgeLoadRow>();
        foreach (var value in ReadArray(data["panels"]))
        {
            var p = ReadObject(value); var plane = p["plane"] == null ? null : ReadObject(p["plane"]);
            Known(p, "id", "name", "nodes", "elements", "triangles", "holes", "plane", "tolerance", "loading_nodes", "loading_triangles");
            if (plane != null) Known(plane, "origin", "axis_u", "axis_v");
            var tolerance = p["tolerance"] == null ? null : ReadObject(p["tolerance"]);
            if (tolerance != null) Known(tolerance, "absolute_length", "relative_length");
            panels.Add(new() { Id = Int(p["id"]).ToString(), Name = p["name"]?.ToString() ?? "", Nodes = IdList(p["nodes"], true),
                Elements = IdList(p["elements"]), Triangles = RingList(p["triangles"]), Holes = RingList(p["holes"]),
                Origin = plane == null ? "" : PointText(plane["origin"]), AxisU = plane == null ? "" : PointText(plane["axis_u"]), AxisV = plane == null ? "" : PointText(plane["axis_v"]),
                AbsoluteTolerance = tolerance?["absolute_length"]?.ToString() ?? "", RelativeTolerance = tolerance?["relative_length"]?.ToString() ?? "",
                LoadingNodes = p["loading_nodes"]?.ToJsonString() ?? "", LoadingTriangles = RingList(p["loading_triangles"]) });
        }
        foreach (var value in ReadArray(data["paths"]))
        {
            var p = ReadObject(value); int id = Int(p["id"]); var points = ReadArray(p["points"], true);
            Known(p, "id", "name", "points");
            if (points.Count < 2) throw new FormatException($"線 {id}: 2点以上が必要です。");
            for (int i = 0; i < points.Count; i++)
            {
                var point = Point(PointText(points[i]));
                paths.Add(new() { Id = id.ToString(), Name = p["name"]?.ToString() ?? "", Order = (i + 1).ToString(), X = F(point.X), Y = F(point.Y), Z = F(point.Z) });
            }
        }
        var cases = data["cases"] == null ? new JsonObject() : ReadObject(data["cases"]);
        var caseIds = new HashSet<int>();
        foreach (var (caseId, records) in cases)
        {
            int normalizedCaseId = Id(caseId);
            if (!caseIds.Add(normalizedCaseId)) throw new FormatException("実荷重番号が重複しています。");
            foreach (var value in ReadArray(records, true))
            {
                var p = ReadObject(value); var ids = ReadArray(p["path_ids"], true); var intensities = ReadArray(p["end_intensities"], true);
                Known(p, "id", "name", "panel_id", "path_ids", "end_intensities", "direction");
                if (ids.Count is not (1 or 2) || intensities.Count != ids.Count || intensities.Any(pair => ReadArray(pair, true).Count != 2))
                    throw new FormatException("荷重には1本または2本の線と各線の端部強度が必要です。");
                var direction = p["direction"] == null ? null : ReadObject(p["direction"]);
                if (direction != null) Known(direction, "mode", "vector");
                string mode = direction?["mode"]?.ToString() ?? "global";
                BridgePoint? vector = direction?["vector"] == null ? null : Point(PointText(direction["vector"]));
                loads.Add(new() { CaseId = normalizedCaseId.ToString(CultureInfo.InvariantCulture), Id = Int(p["id"]).ToString(), Name = p["name"]?.ToString() ?? "", PanelId = Int(p["panel_id"]).ToString(),
                    Path1 = Int(ids[0]).ToString(), Path2 = ids.Count == 2 ? Int(ids[1]).ToString() : "",
                    P11 = F(Number(intensities[0]![0]!.ToString())), P12 = F(Number(intensities[0]![1]!.ToString())),
                    P21 = ids.Count == 2 ? F(Number(intensities[1]![0]!.ToString())) : "", P22 = ids.Count == 2 ? F(Number(intensities[1]![1]!.ToString())) : "",
                    Direction = mode, Vx = vector.HasValue ? F(vector.Value.X) : "", Vy = vector.HasValue ? F(vector.Value.Y) : "", Vz = vector.HasValue ? F(vector.Value.Z) : "" });
            }
        }
        if (panels.Count + paths.Count + loads.Count > 100_000) throw new FormatException("橋面荷重の行数が上限を超えています。");
        return new(panels.ToArray(), paths.ToArray(), loads.ToArray());
    }
}

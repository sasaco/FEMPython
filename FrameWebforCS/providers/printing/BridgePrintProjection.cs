using FrameWebforCS.calculation;
using System.Globalization;
using System.Text.Json.Nodes;

namespace FrameWebforCS.providers.printing;

/// <summary>Projects bridge input and actual solver audits into bounded, printable tables.</summary>
internal static class BridgePrintProjection
{
    internal static bool IsBridgeOption(PrintOption option) => option is
        PrintOption.BridgeDefinitions or PrintOption.BridgeLoads or
        PrintOption.BridgeLoadDiagram or PrintOption.BridgeAudit;

    internal static bool HasDefinitions(JsonObject? saved) =>
        saved?["bridge_loads"] is JsonObject bridge &&
        ((bridge["panels"] as JsonArray)?.Count > 0 || (bridge["paths"] as JsonArray)?.Count > 0);

    internal static bool HasLoads(JsonObject? saved) =>
        (saved?["bridge_loads"]?["cases"] as JsonObject)?.Any(item =>
            item.Value is JsonArray { Count: > 0 }) == true;

    internal static bool HasAudit(CalculationResultPresentation? result) =>
        result?.Pages.Any(page => page.Result is StaticAnalysisResult value &&
            value.Diagnostics.SpatialLoads is not null) == true;

    internal static void ValidateDimension(JsonObject saved, PrintSelection selection)
    {
        if (selection.Options.Any(IsBridgeOption) &&
            (saved["dimension"]?.GetValue<int>() ?? 3) != 3)
            throw new PrintProjectionException("橋面荷重の印刷は3次元モデル専用です。");
    }

    internal static IReadOnlyList<string> InputCases(JsonObject saved, PrintSelection selection)
    {
        var cases = saved["bridge_loads"]?["cases"] as JsonObject;
        string[] available = cases?.Where(item => item.Value is JsonArray { Count: > 0 })
            .Select(item => item.Key).ToArray() ?? [];
        if (selection.InputCaseIds is { } filter)
        {
            var known = ((saved["load"] as JsonObject)?.Select(item => item.Key) ?? [])
                .Concat(available).ToHashSet(StringComparer.Ordinal);
            if (filter.Any(id => !known.Contains(id)))
                throw new PrintProjectionException("選択した入力荷重ケースがありません。");
            available = available.Where(filter.Contains).ToArray();
        }
        return available;
    }

    private static JsonObject Bridge(JsonObject saved)
    {
        var bridge = saved["bridge_loads"] as JsonObject ??
            throw new PrintProjectionException("橋面荷重の入力がありません。");
        if (bridge["drafts"] is JsonObject { Count: > 0 } or JsonArray { Count: > 0 })
            throw new PrintProjectionException("橋面荷重に未完成の入力があります。入力を修正してから印刷してください。");
        return bridge;
    }

    internal static void AddDiagrams(List<PrintDiagramRequest> requests, JsonObject saved,
        PrintSelection selection)
    {
        if (!selection.Options.Contains(PrintOption.BridgeLoadDiagram)) return;
        Bridge(saved);
        var ids = InputCases(saved, selection);
        if (ids.Count == 0) throw new PrintProjectionException("選択したケースに橋面荷重がありません。");
        IReadOnlyList<string> views = selection.BridgeDiagramViews ?? ["plan"];
        if (views.Count == 0 || views.Any(view => view is not ("plan" or "iso")))
            throw new PrintProjectionException("橋面荷重図の表示方向を選択してください。");
        foreach (string id in ids)
            foreach (string view in views.Distinct())
                requests.Add(new(requests.Count, PrintOption.BridgeLoadDiagram, "print_bridge_load",
                    id, "bridge_load", selection.Language == "en"
                        ? $"Bridge deck load - Case {id} ({view})"
                        : $"橋面荷重図 - Case {id} ({(view == "plan" ? "平面" : "鳥瞰")})",
                    View: view, ShowMesh: selection.BridgeShowMesh,
                    ShowLabels: selection.BridgeShowLabels));
    }

    internal static void AddTables(JsonObject root, JsonObject saved, PrintSnapshot snapshot)
    {
        PrintSelection selection = snapshot.Selection;
        bool definitions = selection.Options.Contains(PrintOption.BridgeDefinitions);
        bool loads = selection.Options.Contains(PrintOption.BridgeLoads);
        bool audit = selection.Options.Contains(PrintOption.BridgeAudit);
        if (!definitions && !loads && !audit) return;
        ValidateDimension(saved, selection);
        var reports = new JsonArray();
        bool en = selection.Language == "en";
        string T(string ja, string english) => en ? english : ja;
        if (definitions || loads)
        {
            var bridge = Bridge(saved);
            IReadOnlyList<string> ids = InputCases(saved, selection);
            if (loads && ids.Count == 0)
                throw new PrintProjectionException("選択したケースに橋面荷重がありません。");
            var caseLoads = ids.SelectMany(id => bridge["cases"]![id]!.AsArray()
                .Select(value => (Case: id, Load: value!.AsObject()))).ToArray();
            var panelIds = caseLoads.Select(item => Text(item.Load["panel_id"])).ToHashSet();
            var pathIds = caseLoads.SelectMany(item => item.Load["path_ids"]!.AsArray())
                .Select(Text).ToHashSet();
            bool allDefinitions = selection.InputCaseIds is null;
            if (definitions)
            {
                var panelRows = new List<string[]>();
                foreach (JsonObject panel in (bridge["panels"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    string id = Text(panel["id"]);
                    if (!allDefinitions && !panelIds.Contains(id)) continue;
                    string name = Text(panel["name"]);
                    foreach (var (key, label) in new[] {
                        ("nodes", T("分配節点", "Transfer nodes")),
                        ("elements", T("参照シェル", "Shell references")),
                        ("triangles", T("分配三角形", "Transfer triangles")),
                        ("holes", T("開口", "Holes")),
                        ("loading_nodes", T("載荷節点", "Loading nodes")),
                        ("loading_triangles", T("載荷三角形", "Loading triangles")),
                        ("plane", T("載荷平面", "Loading plane")),
                        ("tolerance", T("幾何許容差", "Geometry tolerance")) })
                    {
                        if (panel[key] is not { } value) continue;
                        if (value is JsonArray values && values.Any(v => v is JsonArray or JsonObject))
                            panelRows.AddRange(values.Select((item, index) => new[] {
                                id, name, label + " " + (index + 1), Text(item) }));
                        else panelRows.Add([id, name, label, Text(value)]);
                    }
                }
                Add(reports, T("橋面定義（荷重分配用・剛性なし）", "Bridge surfaces (load transfer only; no stiffness)"),
                    [T("橋面ID", "Panel"), T("名称", "Name"), T("項目", "Definition"), T("内容", "Value")],
                    [45, 105, 100, 240], panelRows);
                var pathRows = new List<string[]>();
                foreach (JsonObject path in (bridge["paths"] as JsonArray ?? []).OfType<JsonObject>())
                {
                    string id = Text(path["id"]);
                    if (!allDefinitions && !pathIds.Contains(id)) continue;
                    int order = 0;
                    foreach (JsonArray point in path["points"]!.AsArray().OfType<JsonArray>())
                        pathRows.Add([id, Text(path["name"]), (++order).ToString(CultureInfo.InvariantCulture),
                            Text(point[0]), Text(point[1]), Text(point[2])]);
                }
                Add(reports, T("載荷ライン座標（点順が載荷方向）", "Load paths (ordered from start to end)"),
                    [T("ライン", "Path"), T("名称", "Name"), T("点順", "Order"), "X (m)", "Y (m)", "Z (m)"],
                    [45, 100, 40, 100, 100, 100], pathRows);
                if (panelRows.Count + pathRows.Count == 0)
                    throw new PrintProjectionException("選択したケースに印刷できる橋面定義がありません。");
            }
            if (loads)
                foreach (string id in ids)
                {
                    var rows = new List<string[]>();
                    var directions = new List<string[]>();
                    foreach (var item in caseLoads.Where(item => item.Case == id))
                    {
                        JsonObject load = item.Load;
                        var paths = load["path_ids"]!.AsArray();
                        var intensities = load["end_intensities"]!.AsArray();
                        bool area = paths.Count == 2;
                        for (int index = 0; index < paths.Count; index++)
                            rows.Add([Text(load["id"]), Text(load["panel_id"]),
                                area ? T("面", "Area") : T("線", "Line"), Text(paths[index]),
                                Text(intensities[index]![0]), Text(intensities[index]![1]), area ? "kN/m²" : "kN/m"]);
                        JsonObject? direction = load["direction"] as JsonObject;
                        string value = Text(direction?["mode"]) == "normal"
                            ? T("面の正法線", "Positive panel normal")
                            : "Global " + (direction?["vector"] is { } vector ? Text(vector) : "[0,0,1]");
                        directions.Add([Text(load["id"]), Text(load["name"]), value]);
                    }
                    string caseName = Text(saved["load"]?[id]?["name"]);
                    Add(reports, T($"橋面荷重 Case {id} {caseName}（入力強度）", $"Bridge loads - Case {id} {caseName} (entered intensities)"),
                        [T("荷重ID", "Load"), T("橋面ID", "Panel"), T("種別", "Kind"), T("ライン", "Path"),
                         T("始点強度", "Start"), T("終点強度", "End"), T("単位", "Unit")],
                        [45, 45, 40, 50, 110, 110, 65], rows);
                    Add(reports, T($"荷重方向 Case {id}（符号は指定方向を正）", $"Load directions - Case {id} (positive along specified direction)"),
                        [T("荷重ID", "Load"), T("名称", "Name"), T("方向", "Direction")], [45, 180, 240], directions);
                }
        }
        if (audit) AddAudit(reports, snapshot, T);
        root["bridge_reports"] = reports;
    }

    private static void AddAudit(JsonArray reports, PrintSnapshot snapshot, Func<string, string, string> t)
    {
        if (snapshot.ResultInputRevision != snapshot.InputRevision)
            throw new PrintProjectionException("入力が変更されています。再解析してから荷重分配結果を印刷してください。");
        var result = snapshot.Result ?? throw new PrintProjectionException("橋面荷重の解析結果がありません。");
        int count = 0;
        string unknownUnit = t("単位未指定", "unspecified");
        bool KnownUnit(string value) => !string.IsNullOrWhiteSpace(value) &&
            !value.Equals("unspecified", StringComparison.OrdinalIgnoreCase);
        string forceUnit = KnownUnit(result.ResultSet.Units.Force) ? result.ResultSet.Units.Force : unknownUnit;
        string lengthUnit = KnownUnit(result.ResultSet.Units.Length) ? result.ResultSet.Units.Length : unknownUnit;
        string momentUnit = KnownUnit(result.ResultSet.Units.Force) && KnownUnit(result.ResultSet.Units.Length)
            ? forceUnit + " " + lengthUnit : unknownUnit;
        string areaUnit = KnownUnit(result.ResultSet.Units.Length) ? lengthUnit + "²" : unknownUnit;
        foreach (var page in result.Pages)
        {
            if (snapshot.Selection.InputCaseIds is { } ids && !ids.Contains(page.Case.CaseId)) continue;
            if (page.Result is not StaticAnalysisResult value || value.Diagnostics.SpatialLoads is not { } audit) continue;
            count++;
            string title = t($"荷重分配確認 Case {page.Case.CaseId}（解析時の実荷重）", $"Load transfer audit - Case {page.Case.CaseId} (solver assembly)");
            Add(reports, title, [t("項目", "Quantity"), "X", "Y", "Z"], [185, 100, 100, 100],
                new[] { VectorRow(t($"入力合力 ({forceUnit})", $"Applied force ({forceUnit})"), audit.Resultant),
                    VectorRow(t($"節点合力 ({forceUnit})", $"Nodal force ({forceUnit})"), audit.NodalResultant),
                    VectorRow(t($"入力M・原点回り ({momentUnit})", $"Applied M at origin ({momentUnit})"), audit.Moment),
                    VectorRow(t($"節点M・原点回り ({momentUnit})", $"Nodal M at origin ({momentUnit})"), audit.NodalMoment) });
            Add(reports, t("ケース全体の保存誤差", "Case conservation errors"),
                [t($"合力誤差 ({forceUnit})", $"Force error ({forceUnit})"),
                 t($"モーメント誤差 ({momentUnit})", $"Moment error ({momentUnit})")],
                [240, 240], new[] { new[] { Number(audit.ForceError), Number(audit.MomentError) } });
            Add(reports, t("荷重別積分・保存誤差", "Integration and conservation errors"),
                [t("荷重ID", "Load"), t("橋面ID", "Panel"), t($"長さ {lengthUnit}", $"Length {lengthUnit}"), t($"面積 {areaUnit}", $"Area {areaUnit}"),
                 t($"合力誤差 {forceUnit}", $"Force error {forceUnit}"), t($"M誤差 {momentUnit}", $"M error {momentUnit}")],
                [45, 45, 90, 90, 110, 110], audit.Loads.Select(load => new[] { Number(load.LoadId), Number(load.PanelId),
                    load.Feature == "spatial_line" ? Number(load.IntegratedLength) : "-",
                    load.Feature == "spatial_area" ? Number(load.ClippedArea) : "-", Number(load.ForceError), Number(load.MomentError) }));
            Add(reports, t("等価節点荷重（入力へ再加算しない）", "Equivalent nodal loads (do not add again to input)"),
                [t("節点ID", "Node"), $"Fx ({forceUnit})", $"Fy ({forceUnit})", $"Fz ({forceUnit})"], [80, 135, 135, 135],
                audit.NodeLoads.Select(load => VectorRow(load.NodeId, load.Force)));
        }
        if (count == 0) throw new PrintProjectionException("選択したケースに橋面荷重の分配結果がありません。");
    }

    private static string[] VectorRow(string label, Vector3Value vector) =>
        [label, Number(vector.X), Number(vector.Y), Number(vector.Z)];
    private static string Number(double value) => value.ToString("G10", CultureInfo.InvariantCulture);
    private static string Text(JsonNode? value) => value is JsonValue scalar && scalar.TryGetValue<double>(out double number)
        ? Number(number) : value is JsonArray or JsonObject ? value.ToJsonString() : value?.ToString() ?? "";

    private static void Add(JsonArray reports, string title, string[] headers, double[] widths, IEnumerable<string[]> values)
    {
        var rows = new JsonArray();
        foreach (string[] row in values)
        {
            if (rows.Count >= 100_000) throw new PrintProjectionException("橋面荷重帳票の行数が多すぎます。");
            rows.Add(new JsonArray(row.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()));
        }
        if (rows.Count == 0) return;
        reports.Add(new JsonObject { ["title"] = title,
            ["headers"] = new JsonArray(headers.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()),
            ["widths"] = new JsonArray(widths.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray()), ["rows"] = rows });
    }
}

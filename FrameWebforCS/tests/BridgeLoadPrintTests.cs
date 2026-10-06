using FrameWebforCS.calculation;
using FrameWebforCS.providers.printing;
using FrameWebforCS.three;
using PDF_Manager;
using PdfSharpCore.Pdf.IO;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class BridgeLoadPrintTests
{
    private const string Input = """
        {"dimension":3,"node":{},"member":{},"shell":{},"element":{},"rigid":[],
         "joint":{},"fix_node":{},"fix_member":{},"notice_points":[],
         "load":{"1":{"name":"歩道線荷重"},"2":{"name":"車道面荷重"}},
         "bridge_loads":{"version":1,
           "panels":[{"id":1,"name":"左橋面","nodes":[1,2,3],"triangles":[[1,2,3]]},
                     {"id":2,"name":"右橋面","nodes":[4,5,6],"triangles":[[4,5,6]],"holes":[[7,8,9]]}],
           "paths":[{"id":1,"name":"左縁","points":[[0,0,1],[10,0,1]]},
                    {"id":2,"name":"右縁","points":[[0,5,1],[10,5,2]]},
                    {"id":3,"name":"外縁","points":[[0,6,1],[10,6,2]]}],
           "cases":{"1":[{"id":1,"panel_id":1,"path_ids":[1],"end_intensities":[[-2,-4]]}],
                    "2":[{"id":2,"panel_id":2,"path_ids":[2,3],"end_intensities":[[-3.125,-4.25],[-5.5,-6.75]],
                           "direction":{"mode":"normal"}}]}}}
        """;

    [Fact]
    public void InputOnlyBridgeReportsSelectCaseAndOnlyReferencedGeometry()
    {
        var choice = new PrintSelection([PrintOption.BridgeDefinitions, PrintOption.BridgeLoads], InputCaseIds: ["2"]);
        var root = JsonNode.Parse(PrintProjection.Build(Snapshot(choice)))!.AsObject();
        var reports = root["bridge_reports"]!.AsArray();
        string text = string.Join("\n", reports.SelectMany(report => report!["rows"]!.AsArray())
            .SelectMany(row => row!.AsArray()).Select(cell => cell!.GetValue<string>()));
        Assert.Contains("右橋面", text);
        Assert.DoesNotContain("左橋面", text);
        Assert.Contains("-3.125", text);
        Assert.Contains("-6.75", text);
        Assert.Contains("kN/m²", text);
        Assert.DoesNotContain("kN/m\n", text);
        Assert.False(root["hasPrintCalculation"]!.GetValue<bool>());
        Assert.Null(root["result"]);
        Assert.Null(root["PrintLoad"]);
    }

    [Fact]
    public void DiagramSelectionBeforeSolveProducesPlanAndIsoWithoutLegacyVectorRoute()
    {
        var selection = new PrintSelection([PrintOption.BridgeLoadDiagram], InputCaseIds: ["2"],
            BridgeDiagramViews: ["plan", "iso"], BridgeShowMesh: false, BridgeShowLabels: true);
        var requests = PrintProjection.MakeDiagramRequests(selection, null, JsonNode.Parse(Input)!.AsObject());
        Assert.Equal(2, requests.Count);
        Assert.Equal(new[] { "plan", "iso" }, requests.Select(item => item.View));
        Assert.All(requests, request =>
        {
            Assert.Equal("2", request.CaseId);
            Assert.Equal("print_bridge_load", request.Mode);
            Assert.False(request.ShowMesh);
            Assert.True(request.ShowLabels);
        });
        byte[] png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aGFAAAAAASUVORK5CYII=");
        var snapshot = new PrintSnapshot(selection, Input, 1, 1, null, null, requests);
        JsonObject root = JsonNode.Parse(PrintProjection.Build(snapshot,
            requests.Select(request => new PrintDiagramImage(request, png)).ToArray()))!.AsObject();
        Assert.Equal(2, root["PrintScreenData"]!.AsArray().Count);
        Assert.Null(root["PrintLoad"]);
        Assert.Null(root["PrintDiagram"]);
    }

    [Theory]
    [InlineData((int)PrintOption.BridgeDefinitions)]
    [InlineData((int)PrintOption.BridgeLoads)]
    [InlineData((int)PrintOption.BridgeLoadDiagram)]
    [InlineData((int)PrintOption.BridgeAudit)]
    public void EveryBridgeOptionRejectsTwoDimensions(int option)
    {
        string input = Input.Replace("\"dimension\":3", "\"dimension\":2");
        var choice = new PrintSelection([(PrintOption)option]);
        Assert.Throws<PrintProjectionException>(() => PrintProjection.MakeDiagramRequests(choice, null, JsonNode.Parse(input)!.AsObject()));
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(new(choice, input, 1, 1, null, null, [])));
    }

    [Fact]
    public void InvalidCaseAndUnfinishedRowsFailInsteadOfPrintingIncompleteInput()
    {
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(Snapshot(
            new PrintSelection([PrintOption.BridgeLoads], InputCaseIds: ["missing"]))));
        var input = JsonNode.Parse(Input)!.AsObject();
        input["bridge_loads"]!["drafts"] = new JsonObject { ["Loads"] = new JsonArray("unfinished") };
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(new(
            new PrintSelection([PrintOption.BridgeLoads]), input.ToJsonString(), 1, 1, null, null, [])));
    }

    [Fact]
    public void BridgeTablesGenerateJapanesePdfAndPaginateLongCoordinateLists()
    {
        var input = JsonNode.Parse(Input)!.AsObject();
        var points = input["bridge_loads"]!["paths"]![0]!["points"]!.AsArray();
        for (int i = 2; i < 130; i++) points.Add(new JsonArray(i, 0, 1));
        var snapshot = new PrintSnapshot(new([PrintOption.BridgeDefinitions, PrintOption.BridgeLoads], InputCaseIds: ["1"]),
            input.ToJsonString(), 1, 1, null, null, []);
        byte[] bytes = DirectPdfGenerator.Generate(PrintProjection.Build(snapshot));
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount >= 3);
        Assert.All(pdf.Pages.Cast<PdfSharpCore.Pdf.PdfPage>(), page => Assert.True(page.Contents.Elements.Count > 0));
        string? artifact = Environment.GetEnvironmentVariable("FRAMEWEB_BRIDGE_PRINT_ARTIFACT");
        if (!string.IsNullOrEmpty(artifact)) File.WriteAllBytes(artifact, bytes);
    }

    [Fact]
    public void OversizedSingleBridgeCellIsRejectedBeforeWrapping()
    {
        var input = JsonNode.Parse(Input)!.AsObject();
        input["bridge_loads"]!["panels"]![0]!["name"] = new string('x', 10_000_000);
        var snapshot = new PrintSnapshot(new([PrintOption.BridgeDefinitions], InputCaseIds: ["1"]),
            input.ToJsonString(), 1, 1, null, null, []);
        string json = PrintProjection.Build(snapshot);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(json) < DirectPdfGenerator.MaxRequestBytes);
        var error = Assert.Throws<InvalidOperationException>(() => DirectPdfGenerator.Generate(json));
        Assert.Contains("text exceeds the layout budget", error.Message);
    }

    [Fact]
    public void ActualSolverAuditFixtureReachesPdfWithoutRecomputingLoadDistribution()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb"))) directory = directory.Parent;
        var result = new CalculationResultPresentation(AnalysisResultSetJson.Deserialize(File.ReadAllText(Path.Combine(directory!.FullName,
            "FrameWeb", "tests", "data", "contracts", "positive", "bridge-static.json"))));
        var snapshot = new PrintSnapshot(new([PrintOption.BridgeAudit], InputCaseIds: ["1"]), Input, 1, 1,
            result, null, [], resultInputRevision: 1);
        string json = PrintProjection.Build(snapshot);
        JsonArray reports = JsonNode.Parse(json)!["bridge_reports"]!.AsArray();
        Assert.Contains("単位未指定", reports.ToJsonString(new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        Assert.DoesNotContain("unspecified", reports.ToJsonString());
        Assert.Equal("50", reports[0]!["rows"]![0]![3]!.GetValue<string>());
        Assert.Equal("53.33333333", reports[0]!["rows"]![2]![1]!.GetValue<string>());
        using var pdf = PdfReader.Open(new MemoryStream(DirectPdfGenerator.Generate(json)), PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 0);
    }

    [Fact]
    public void AuditPrintRequiresMatchingInputRevisionAndUsesActualNodalForces()
    {
        var result = PresentationWithAudit();
        var selection = new PrintSelection([PrintOption.BridgeAudit], InputCaseIds: ["D"]);
        var current = new PrintSnapshot(selection, Input, 4, 5, result, null, [], resultInputRevision: 5);
        var root = JsonNode.Parse(PrintProjection.Build(current))!.AsObject();
        string text = string.Join("\n", root["bridge_reports"]!.AsArray()
            .SelectMany(report => report!["rows"]!.AsArray()).SelectMany(row => row!.AsArray())
            .Select(value => value!.GetValue<string>()));
        Assert.Contains("-7.125", text);
        Assert.Equal("1", root["bridge_reports"]!.AsArray().Last()!["rows"]![0]![0]!.GetValue<string>());
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(new(selection,
            Input, 4, 6, result, null, [], resultInputRevision: 5)));
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(new(
            selection with { InputCaseIds = ["other"] }, Input, 4, 5, result, null, [], resultInputRevision: 5)));
    }

    private static PrintSnapshot Snapshot(PrintSelection selection) => new(selection, Input, 1, 1, null, null, []);

    private static CalculationResultPresentation PresentationWithAudit()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb"))) directory = directory.Parent;
        var original = AnalysisResultSetJson.Deserialize(File.ReadAllText(Path.Combine(directory!.FullName,
            "FrameWeb", "tests", "data", "contracts", "positive", "single-static.json")));
        var source = (StaticAnalysisResult)original.Results[0];
        var force = new Vector3Value(0, 0, -7.125);
        var zero = new Vector3Value(0, 0, 0);
        var audit = new SpatialLoadAudit(force, zero, force, zero, 0, 0,
            [new SpatialNodalLoad("1", force)],
            [new SpatialLoadItemAudit(7, 2, "spatial_line", force, zero, force, zero, 0, 0, 2.375, 0)]);
        var changed = new StaticAnalysisResult(source.CaseId, source.NodeDisplacements, source.SupportReactions,
            source.MemberSectionForces, source.ShellResults, source.SolidResults, new WarningDiagnostics([], audit));
        return new CalculationResultPresentation(new AnalysisResultSet(original.Kind, original.SchemaVersion, original.Units,
            original.CoordinateSystem, original.Cases, original.Topology, [changed]));
    }
}

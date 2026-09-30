using FrameWebforCS.calculation;
using FrameWebforCS.providers.printing;
using PDF_Manager;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class PrintProjectionTests
{
    private const string Input = """
        {"dimension":3,"node":{},"member":{},"shell":{},"element":{},
         "rigid":[],"joint":{},"fix_node":{},"fix_member":{},
         "notice_points":[],"load":{},"define":{},"combine":{},"pickup":{},
         "result":{"1":{"disg":{"1":{"dx":999}}}}}
        """;

    [Fact]
    public void LegacyOptionNumbersAreStableAndInactiveControlsAreReserved()
    {
        Assert.Equal(0, (int)PrintOption.Input);
        Assert.Equal(9, (int)PrintOption.PickupSectionForce);
        Assert.Equal(11, (int)PrintOption.SectionDiagram);
        Assert.Equal(15, (int)PrintOption.LoadDiagram);
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Capture(
            new PrintSelection([PrintOption.ReservedScreen])));
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Capture(
            new PrintSelection([PrintOption.ReservedReactionDiagram])));
    }

    [Fact]
    public void InputOnlyUsesLegacyRootAndDoesNotLeakSavedResult()
    {
        PrintSnapshot snapshot = Snapshot(new PrintSelection([PrintOption.Input]));
        JsonObject root = JsonNode.Parse(PrintProjection.Build(snapshot))!.AsObject();
        Assert.True(root["hasPrintInputData"]!.GetValue<bool>());
        Assert.False(root["hasPrintCalculation"]!.GetValue<bool>());
        Assert.Null(root["result"]);
        Assert.Null(root["disg"]);
        Assert.Equal("A4", root["pageSize"]!.GetValue<string>());
        Assert.NotNull(root["node"]);
    }

    [Fact]
    public void InputProjectionProducesParseablePdfThroughDirectLibrary()
    {
        string json = PrintProjection.Build(Snapshot(new PrintSelection([PrintOption.Input])));
        byte[] pdf = DirectPdfGenerator.Generate(json);
        Assert.True(pdf.Length > 100);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Contains("%%EOF", System.Text.Encoding.ASCII.GetString(pdf[^32..]));
    }

    [Theory]
    [InlineData((int)PrintPaper.A4, (int)PrintOrientation.Vertical, false)]
    [InlineData((int)PrintPaper.A3, (int)PrintOrientation.Horizontal, true)]
    public void GeneratedPdfUsesSelectedPaperAndOrientation(
        int paper, int orientation, bool landscape)
    {
        string json = PrintProjection.Build(Snapshot(new PrintSelection(
            [PrintOption.Input], Paper: (PrintPaper)paper,
            Orientation: (PrintOrientation)orientation)));
        using var pdf = PdfReader.Open(new MemoryStream(DirectPdfGenerator.Generate(json)),
            PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 0);
        double width = pdf.Pages[0].Width.Point;
        double height = pdf.Pages[0].Height.Point;
        Assert.Equal(landscape, width > height);
        Assert.InRange(Math.Max(width, height), landscape ? 1190 : 841,
            landscape ? 1192 : 843);
    }

    [Theory]
    [InlineData("single-static")]
    [InlineData("nonlinear-steps")]
    [InlineData("modal")]
    public void DisplacementProjectionReadsCanonicalResultFamilies(string fixture)
    {
        CalculationResultPresentation presentation = Presentation(fixture);
        PrintSnapshot snapshot = Snapshot(new PrintSelection([PrintOption.Displacement]), presentation);
        JsonObject root = JsonNode.Parse(PrintProjection.Build(snapshot))!.AsObject();
        JsonObject rows = root["disg"]!.AsObject();
        Assert.Equal(presentation.Pages.Count, rows.Count);
        Assert.Equal(rows.Count, root["disgName"]!.AsArray().Count);
        Assert.All(rows, entry => Assert.NotEmpty(entry.Value!.AsArray()));
        Assert.Null(root["result"]);
    }

    [Fact]
    public void StaticForceAndReactionRowsUsePrintInputFieldNames()
    {
        CalculationResultPresentation presentation = Presentation("single-static");
        PrintSnapshot snapshot = Snapshot(new PrintSelection(
            [PrintOption.Displacement, PrintOption.Reaction, PrintOption.SectionForce]), presentation);
        JsonObject root = JsonNode.Parse(PrintProjection.Build(snapshot))!.AsObject();
        Assert.NotNull(root["reac"]);
        Assert.NotNull(root["fsec"]);
        foreach (JsonNode? row in root["reac"]!.AsObject().First().Value!.AsArray())
        {
            Assert.NotNull(row!["tx"]);
            Assert.NotNull(row["mz"]);
        }
        foreach (JsonNode? row in root["fsec"]!.AsObject().First().Value!.AsArray())
        {
            Assert.NotNull(row!["m"]);
            Assert.NotNull(row["n"]);
            Assert.NotNull(row["l"]);
        }
    }

    [Fact]
    public void CanonicalStaticReportsReachDirectPdfGenerator()
    {
        var presentation = Presentation("single-static");
        string json = PrintProjection.Build(Snapshot(new PrintSelection(
            [PrintOption.Displacement, PrintOption.Reaction, PrintOption.SectionForce]), presentation));
        byte[] pdf = DirectPdfGenerator.Generate(json);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.True(pdf.Length > 1000);
    }

    [Fact]
    public void MovingChildrenProjectAsLegacySignedEnvelopesWithSourceCase()
    {
        JsonObject fixture = JsonNode.Parse(Fixture("single-static"))!.AsObject();
        JsonObject baseCase = fixture["cases"]!.AsArray()[0]!.AsObject();
        JsonObject baseResult = fixture["results"]!.AsArray()[0]!.AsObject();
        var cases = new JsonArray();
        var results = new JsonArray();
        foreach (var (id, displacement) in new[] { ("L", 0.001), ("L.1", 0.004), ("L.2", -0.003) })
        {
            JsonObject itemCase = (JsonObject)baseCase.DeepClone();
            JsonObject itemResult = (JsonObject)baseResult.DeepClone();
            itemCase["case_id"] = id;
            itemCase["symbol"] = "LL";
            itemResult["case_id"] = id;
            itemResult["node_displacements"]!.AsArray()[1]!["components"]!["dx"] = displacement;
            cases.Add(itemCase);
            results.Add(itemResult);
        }
        fixture["cases"] = cases;
        fixture["results"] = results;
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()));
        Assert.Single(presentation.Pages);
        string json = PrintProjection.Build(Snapshot(new PrintSelection(
            [PrintOption.Displacement, PrintOption.Reaction, PrintOption.SectionForce]), presentation));
        JsonObject root = JsonNode.Parse(json)!.AsObject();
        JsonObject parent = root["disg"]!.AsObject().Single().Value!.AsObject();
        Assert.Equal("L.1", parent["dx_max"]!["2"]!["case"]!.GetValue<string>());
        Assert.Equal("L.2", parent["dx_min"]!["2"]!["case"]!.GetValue<string>());
        Assert.NotNull(root["reac"]!.AsObject().Single().Value!["tx_max"]);
        Assert.NotNull(root["fsec"]!.AsObject().Single().Value!["fx_max"]);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(
            DirectPdfGenerator.Generate(json), 0, 4));
    }

    [Fact]
    public void DerivedFamiliesKeepModeNamesProvenanceAndStationPosition()
    {
        AnalysisResultSet resultSet = AnalysisResultSetJson.Deserialize(Fixture("single-static"));
        IReadOnlyDictionary<string, double> displacement = new Dictionary<string, double>
        {
            ["dx"] = 0.02, ["dy"] = 0, ["dz"] = 0,
            ["rx"] = 0, ["ry"] = 0, ["rz"] = 0,
        };
        IReadOnlyDictionary<string, double> force = new Dictionary<string, double>
        {
            ["fx"] = 12, ["fy"] = 0, ["fz"] = 0,
            ["mx"] = 0, ["my"] = 0, ["mz"] = 0,
        };
        var defined = new CalculationDerivedCase("C1", "Combination One",
            Modes("dx_max", new CalculationDerivedRow("2", null, displacement, "D", "D")),
            Modes("fx_max", new CalculationDerivedRow("1", null, force, "D", "D")),
            Modes("fx_max", new CalculationDerivedRow("M1", "S1", force, "D", "D")));
        var derived = new CalculationDerivedPresentation([], [defined], [defined]);
        var presentation = new CalculationResultPresentation(resultSet, derived);
        PrintSnapshot snapshot = Snapshot(new PrintSelection(
            [PrintOption.CombinedDisplacement, PrintOption.CombinedReaction,
             PrintOption.CombinedSectionForce, PrintOption.PickupSectionForce],
            Components: ["load"],
            DisplacementComponents: ["dx_max"], ReactionComponents: ["tx_max"],
            SectionForceComponents: ["fx_max"]), presentation);
        JsonObject root = JsonNode.Parse(PrintProjection.Build(snapshot))!.AsObject();
        Assert.Equal(0.02, root["disgCombine"]!["C1"]!["dx_max"]!["2"]!["dx"]!.GetValue<double>());
        Assert.Equal(12, root["reacCombine"]!["C1"]!["tx_max"]!["1"]!["tx"]!.GetValue<double>());
        Assert.Equal(1, root["fsecCombine"]!["C1"]!["fx_max"]![0]!["l"]!.GetValue<double>());
        Assert.Equal("D", root["fsecPickup"]!["C1"]!["fx_max"]![0]!["case"]!.GetValue<string>());
        Assert.Single(root["disgCombine"]!["C1"]!.AsObject());
        Assert.Single(root["reacCombine"]!["C1"]!.AsObject());
        Assert.Single(root["fsecCombine"]!["C1"]!.AsObject());
    }

    [Fact]
    public void LoadAndResultDiagramFiltersAreIndependent()
    {
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(Fixture("single-static")), dimension: 2);
        string input = Input.Replace("\"dimension\":3", "\"dimension\":2");
        var selection = new PrintSelection([PrintOption.LoadDiagram, PrintOption.SectionDiagram],
            DiagramComponents: ["fy"], LoadDiagramComponents: ["axis"]);
        PrintSnapshot snapshot = new(selection, input, 2, 3, presentation, null, []);
        JsonObject root = JsonNode.Parse(PrintProjection.Build(snapshot))!.AsObject();
        Assert.Equal("axis", root["PrintLoad"]!["diagramInput"]!["output"]![0]!.GetValue<string>());
        Assert.Single(root["PrintLoad"]!["diagramInput"]!["output"]!.AsArray());
        Assert.Equal("fy", root["PrintDiagram"]!["diagramResult"]!["output"]![0]!.GetValue<string>());
        Assert.Single(root["PrintDiagram"]!["diagramResult"]!["output"]!.AsArray());
        Assert.Null(root["PrintDiagram"]!["disg"]);
    }

    [Theory]
    [InlineData("disg", "disg")]
    [InlineData("fx,disg", "fx,disg")]
    [InlineData("fx,fy,mz,disg", "mz,fy,fx,disg")]
    public void TwoDimensionalBaseDiagramRetainsDisplacementOutputInLegacyOrder(
        string selectedText, string expectedText)
    {
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(Fixture("single-static")), dimension: 2);
        string input = Input.Replace("\"dimension\":3", "\"dimension\":2");
        var selection = new PrintSelection([PrintOption.SectionDiagram],
            DiagramComponents: selectedText.Split(','));
        PrintSnapshot snapshot = new(selection, input, 2, 3, presentation, null, []);
        JsonObject root = JsonNode.Parse(PrintProjection.Build(snapshot))!.AsObject();
        string[] outputs = root["PrintDiagram"]!["diagramResult"]!["output"]!.AsArray()
            .Select(value => value!.GetValue<string>()).ToArray();
        Assert.Equal(expectedText.Split(','), outputs);
        Assert.NotNull(root["PrintDiagram"]!["disg"]);
        Assert.NotNull(root["PrintDiagram"]!["disgName"]);
    }

    [Fact]
    public void MixedStaticAndModalCasesProjectOnlyEligiblePagesPerFamily()
    {
        CalculationResultPresentation presentation = MixedPresentation();
        Assert.Equal(3, presentation.Pages.Count);
        string modalKey = presentation.Pages.Single(page => page.Result is ModalAnalysisResult).Key;
        string staticKey = presentation.Pages.Single(page => page.Case.CaseId == "D").Key;
        var options = new[] { PrintOption.Displacement, PrintOption.Reaction, PrintOption.SectionForce };

        JsonObject all = JsonNode.Parse(PrintProjection.Build(Snapshot(
            new PrintSelection(options), presentation)))!.AsObject();
        Assert.Equal(3, all["disg"]!.AsObject().Count);
        Assert.Single(all["reac"]!.AsObject());
        Assert.Equal(2, all["fsec"]!.AsObject().Count);
        Assert.NotNull(all["disg"]![modalKey]);
        Assert.Null(all["reac"]![modalKey]);

        JsonObject explicitCases = JsonNode.Parse(PrintProjection.Build(Snapshot(
            new PrintSelection(options, CaseIds: [staticKey, modalKey]), presentation)))!.AsObject();
        Assert.Equal(2, explicitCases["disg"]!.AsObject().Count);
        Assert.Single(explicitCases["reac"]!.AsObject());
        Assert.Single(explicitCases["fsec"]!.AsObject());
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(Snapshot(
            new PrintSelection([PrintOption.Reaction], CaseIds: [modalKey]), presentation)));
    }

    [Fact]
    public void FamilyFilterRejectsComponentsFromOtherFamilies()
    {
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(Snapshot(
            new PrintSelection([PrintOption.Input], ReactionComponents: ["dx_max"]))));
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(Snapshot(
            new PrintSelection([PrintOption.Input], LoadDiagramComponents: ["fy"]))));
    }

    [Fact]
    public void MissingOrInvalidResultsFailWithoutPublishingPayload()
    {
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(
            Snapshot(new PrintSelection([PrintOption.Displacement]))));
        CalculationResultPresentation modal = Presentation("modal");
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(
            Snapshot(new PrintSelection([PrintOption.Reaction]), modal)));
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(
            Snapshot(new PrintSelection([PrintOption.Displacement], CaseIds: ["not-a-case"]), modal)));
    }

    [Fact]
    public void StaleSnapshotIsRejectedBeforeAndAfterProjection()
    {
        PrintSnapshot snapshot = Snapshot(new PrintSelection([PrintOption.Input]));
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(snapshot,
            stillCurrent: () => false));
        int checks = 0;
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(snapshot,
            stillCurrent: () => ++checks == 1));
        Assert.Equal(2, checks);
    }

    [Fact]
    public void InvalidScaleAndAbsentDiagramCaptureFailClosed()
    {
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Capture(
            new PrintSelection([PrintOption.Input], ScaleX: -1)));
        var selection = new PrintSelection([PrintOption.LoadDiagram]);
        PrintSnapshot snapshot = new(selection, Input, 2, 3, null, null,
            [new PrintDiagramRequest(0, PrintOption.LoadDiagram, "print_load", "1", "load", "Load")]);
        Assert.Throws<PrintProjectionException>(() => PrintProjection.Build(snapshot));
    }

    private static PrintSnapshot Snapshot(PrintSelection selection,
        CalculationResultPresentation? presentation = null) => new(
        selection, Input, 2, 3, presentation, presentation?.Derived, []);

    private static IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> Modes(
        string name, CalculationDerivedRow row) =>
        new Dictionary<string, IReadOnlyList<CalculationDerivedRow>> { [name] = [row] };

    private static CalculationResultPresentation Presentation(string fixture)
        => new(AnalysisResultSetJson.Deserialize(Fixture(fixture)));

    private static CalculationResultPresentation MixedPresentation()
    {
        JsonObject staticFixture = JsonNode.Parse(Fixture("single-static"))!.AsObject();
        JsonObject modalFixture = JsonNode.Parse(Fixture("modal"))!.AsObject();
        JsonObject modalCase = (JsonObject)modalFixture["cases"]![0]!.DeepClone();
        JsonObject modalResult = (JsonObject)modalFixture["results"]![0]!.DeepClone();
        modalCase["case_id"] = "M";
        modalCase["name"] = "Mode";
        modalResult["case_id"] = "M";
        JsonObject secondShape = (JsonObject)modalResult["node_mode_shapes"]![0]!.DeepClone();
        secondShape["node_id"] = "2";
        secondShape["components"]!["dx"] = 0;
        modalResult["node_mode_shapes"]!.AsArray().Add(secondShape);
        staticFixture["cases"]!.AsArray().Add(modalCase);
        staticFixture["results"]!.AsArray().Add(modalResult);

        JsonObject emptyReactionCase = (JsonObject)staticFixture["cases"]![0]!.DeepClone();
        JsonObject emptyReactionResult = (JsonObject)staticFixture["results"]![0]!.DeepClone();
        emptyReactionCase["case_id"] = "Z";
        emptyReactionCase["name"] = "No Supports";
        emptyReactionCase["support_node_ids"] = new JsonArray();
        emptyReactionResult["case_id"] = "Z";
        emptyReactionResult["support_reactions"] = new JsonArray();
        staticFixture["cases"]!.AsArray().Add(emptyReactionCase);
        staticFixture["results"]!.AsArray().Add(emptyReactionResult);
        return new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(staticFixture.ToJsonString()));
    }

    private static string Fixture(string fixture)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException();
        return File.ReadAllText(Path.Combine(root, "FrameWeb", "tests", "data",
            "contracts", "positive", fixture + ".json"));
    }
}

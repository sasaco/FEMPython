using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers.printing;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class SectionDiagramProjectionTests
{
    [Fact]
    public void DiagramContractPreservesTableStationsAndAddsRawGeometricDisplacements()
    {
        JsonObject fixture = Fixture();
        JsonObject saved = Saved();
        CalculationResultPresentation presentation = Presentation(fixture, saved);
        JsonObject tables = Project(presentation, saved, new(
            [PrintOption.Displacement, PrintOption.SectionForce,
             PrintOption.CombinedSectionForce, PrintOption.PickupSectionForce]));
        JsonObject mixed = Project(presentation, saved, new(
            [PrintOption.Displacement, PrintOption.SectionForce,
             PrintOption.CombinedSectionForce, PrintOption.PickupSectionForce,
             PrintOption.SectionDiagram, PrintOption.CombinedSectionDiagram, PrintOption.PickupSectionDiagram],
            Layout: PrintLayout.SplitHorizontal, ScaleX: 20, ScaleY: 100));

        foreach (string key in new[] { "fsec", "fsecName", "fsecCombine", "fsecPickup", "disg", "disgName" })
            Assert.True(JsonNode.DeepEquals(tables[key], mixed[key]), key);
        JsonArray stations = mixed["fsec"]!["1/Static/0"]!.AsArray();
        Assert.Equal(new[] { "S0", "S1" }, stations.Select(row => row!["n"]!.GetValue<string>()));
        Assert.All(stations, row => Assert.Equal("M1", row!["m"]!.GetValue<string>()));
        Assert.Equal(new[] { 0.0, 1.0 }, stations.Select(row => row!["l"]!.GetValue<double>()));

        foreach (var (key, caseId, label) in new[]
        {
            ("PrintDiagram", "1/Static/0", "Case 1 Dead load"),
            ("CombPrintDiagram", "1", "Combine 1 Combination One"),
            ("PickPrintDiagram", "1", "Pickup Table 1 Envelope One"),
        })
        {
            JsonNode details = mixed[key]!["diagramResult"]!;
            Assert.NotNull(details["canonicalStations"]);
            Assert.True(details["canonicalStations"]!.GetValue<bool>());
            Assert.True(details["yUp"]!.GetValue<bool>());
            Assert.True(details["referenceLayout"]!.GetValue<bool>());
            Assert.Equal(label, details["caseLabels"]![caseId]!.GetValue<string>());
            Assert.Equal("splitHorizontal", details["layout"]!.GetValue<string>());
            Assert.Equal(0.05, details["scaleX"]!.GetValue<double>());
            Assert.Equal(0.01, details["scaleY"]!.GetValue<double>());
        }

        JsonNode diagram = mixed["PrintDiagram"]!;
        Assert.Equal(1000, diagram["diagramResult"]!["displacementLabelScale"]!.GetValue<double>());
        JsonNode raw = diagram["disg"]!["1/Static/0"]![1]!;
        Assert.Equal(0.001, raw["dx"]!.GetValue<double>());
        Assert.Equal(-0.002, raw["dy"]!.GetValue<double>());
        Assert.Equal(0.003, raw["rz"]!.GetValue<double>());
        Assert.Equal(1, mixed["disg"]!["1/Static/0"]![1]!["dx"]!.GetValue<double>());
        Assert.Equal(-3, mixed["disg"]!["1/Static/0"]![1]!["rz"]!.GetValue<double>());
        Assert.Equal(0.003, ((ForceAnalysisResult)presentation.Pages[0].Result).NodeDisplacements[1].Components.Rz);
    }

    [Theory]
    [InlineData("m", 1000.0)]
    [InlineData("mm", 1.0)]
    [InlineData("cm", 10.0)]
    [InlineData("unspecified", 1000.0)]
    public void RawDiagramDisplacementsKeepModelUnitsAndSelectedNodes(string length, double scale)
    {
        JsonObject fixture = Fixture();
        fixture["units"]!["length"] = length;
        JsonObject saved = Saved();
        CalculationResultPresentation presentation = Presentation(fixture, saved);
        JsonObject root = Project(presentation, saved, new([PrintOption.SectionDiagram],
            CaseIds: ["1"], NodeIds: ["2"], MemberIds: ["M1"], DiagramComponents: ["disg"]));
        JsonNode details = root["PrintDiagram"]!["diagramResult"]!;
        Assert.NotNull(details["displacementLabelScale"]);
        Assert.Equal(scale, details["displacementLabelScale"]!.GetValue<double>());
        Assert.Equal(new[] { "disg" }, details["output"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Single(details["caseLabels"]!.AsObject());
        JsonArray geometry = root["PrintDiagram"]!["disg"]!["1/Static/0"]!.AsArray();
        Assert.Equal(2, geometry.Count);
        JsonNode raw = geometry.Single(row => row!["id"]!.GetValue<string>() == "2")!;
        Assert.Equal("2", raw["id"]!.GetValue<string>());
        Assert.Equal(0.001, raw["dx"]!.GetValue<double>());
        Assert.Equal(-0.002, raw["dy"]!.GetValue<double>());
        Assert.Equal(0.003, raw["rz"]!.GetValue<double>());
        Assert.Single(root["disg"]!["1/Static/0"]!.AsArray());
        Assert.Equal(new[] { "2" }, details["displacementLabelNodeIds"]!.AsArray()
            .Select(node => node!.GetValue<string>()));
        Assert.Equal(0.001 * scale, root["disg"]!["1/Static/0"]![0]!["dx"]!.GetValue<double>());
    }

    [Fact]
    public void ArbitraryCaseIdsAndMultipleStatesKeepDistinctIdentityAndLabelsAfterFiltering()
    {
        JsonObject fixture = Fixture();
        fixture["cases"]![0]!["case_id"] = "train/A";
        fixture["cases"]![0]!["analysis_type"] = "material_nonlinear";
        JsonNode first = fixture["results"]![0]!;
        first["case_id"] = "train/A";
        first["state"] = JsonNode.Parse("""{"kind":"load_step","index":0,"load_factor":0.5,"is_final":false}""");
        first["diagnostics"]!["iterations"] = JsonNode.Parse("""[{"index":0,"residual_norm":0,"correction_norm":0,"converged":true}]""");
        JsonNode second = first.DeepClone();
        second["state"] = JsonNode.Parse("""{"kind":"load_step","index":1,"load_factor":1,"is_final":true}""");
        second["node_displacements"]![1]!["components"]!["dx"] = 0.002;
        fixture["results"]!.AsArray().Add(second);
        JsonObject saved = Saved();
        saved["load"] = JsonNode.Parse("""{"train/A":{"name":"Train A"}}""");
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        JsonObject root = Project(presentation, saved, new([PrintOption.SectionDiagram]));
        JsonNode details = root["PrintDiagram"]!["diagramResult"]!;
        Assert.NotNull(details["caseLabels"]);
        Assert.Equal(new[] { "train/A/LoadStep/0", "train/A/LoadStep/1" },
            details["caseLabels"]!.AsObject().Select(pair => pair.Key));
        Assert.Equal("Case train/A Train A (Step 0, 0.5)",
            details["caseLabels"]!["train/A/LoadStep/0"]!.GetValue<string>());
        Assert.Equal("Case train/A Train A (Step 1, 1)",
            details["caseLabels"]!["train/A/LoadStep/1"]!.GetValue<string>());
        Assert.Equal(0.002, root["PrintDiagram"]!["disg"]!["train/A/LoadStep/1"]![1]!["dx"]!.GetValue<double>());

        JsonObject filtered = Project(presentation, saved, new([PrintOption.SectionDiagram],
            CaseIds: ["train/A/LoadStep/1"], NodeIds: ["2"]));
        Assert.Single(filtered["PrintDiagram"]!["diagramResult"]!["caseLabels"]!.AsObject());
        Assert.Null(filtered["PrintDiagram"]!["diagramResult"]!["caseLabels"]!["train/A/LoadStep/0"]);
        Assert.Single(filtered["fsec"]!.AsObject());
        Assert.Single(filtered["PrintDiagram"]!["disg"]!.AsObject());
    }

    [Fact]
    public void DiagramMetadataDoesNotLeakToTablesLoadsOrThreeDimensionalCaptures()
    {
        JsonObject fixture = Fixture();
        JsonObject saved = Saved();
        var presentation = Presentation(fixture, saved);
        JsonObject table = Project(presentation, saved, new([PrintOption.SectionForce]));
        Assert.Null(table["PrintDiagram"]);
        Assert.Null(table["canonicalStations"]);
        JsonObject load = Project(null, saved, new([PrintOption.LoadDiagram]));
        Assert.Null(load["PrintLoad"]!["diagramInput"]!["canonicalStations"]);
        Assert.Null(load["PrintLoad"]!["diagramInput"]!["referenceLayout"]);
        saved["dimension"] = 3;
        JsonObject threeD = Project(Presentation(fixture, saved, 3), saved,
            new([PrintOption.SectionDiagram]));
        Assert.Null(threeD["PrintDiagram"]);
        Assert.Null(threeD["canonicalStations"]);
        Assert.Throws<PrintProjectionException>(() => Project(null, Saved(), new([PrintOption.SectionDiagram])));
    }

    [Theory]
    [InlineData((int)PrintOption.SectionDiagram, "m", 1000.0)]
    [InlineData((int)PrintOption.SectionDiagram, "mm", 1.0)]
    [InlineData((int)PrintOption.SectionDiagram, "cm", 10.0)]
    [InlineData((int)PrintOption.SectionDiagram, "unspecified", 1000.0)]
    [InlineData((int)PrintOption.CombinedSectionDiagram, "m", 1000.0)]
    [InlineData((int)PrintOption.CombinedSectionDiagram, "mm", 1.0)]
    [InlineData((int)PrintOption.CombinedSectionDiagram, "cm", 10.0)]
    [InlineData((int)PrintOption.CombinedSectionDiagram, "unspecified", 1000.0)]
    [InlineData((int)PrintOption.PickupSectionDiagram, "m", 1000.0)]
    [InlineData((int)PrintOption.PickupSectionDiagram, "mm", 1.0)]
    [InlineData((int)PrintOption.PickupSectionDiagram, "cm", 10.0)]
    [InlineData((int)PrintOption.PickupSectionDiagram, "unspecified", 1000.0)]
    public void ForceOnlyDiagramsCarryModelLengthScaleWithoutDisplacementRows(int option, string length, double scale)
    {
        JsonObject fixture = Fixture();
        fixture["units"]!["length"] = length;
        JsonObject saved = Saved();
        JsonObject root = Project(Presentation(fixture, saved), saved,
            new([(PrintOption)option], DiagramComponents: ["mz"]));
        string key = (PrintOption)option switch
        {
            PrintOption.CombinedSectionDiagram => "CombPrintDiagram",
            PrintOption.PickupSectionDiagram => "PickPrintDiagram",
            _ => "PrintDiagram",
        };
        JsonNode diagram = root[key]!;
        Assert.Null(diagram["disg"]);
        Assert.NotNull(diagram["diagramResult"]!["displacementLabelScale"]);
        Assert.Equal(scale, diagram["diagramResult"]!["displacementLabelScale"]!.GetValue<double>());
        Assert.Null(root["displacementLabelScale"]);
    }

    [Fact]
    public void CaseIdFallbackNameUsesSymbolAndPreservesStateSuffix()
    {
        JsonObject fixture = Fixture();
        fixture["cases"]![0]!["name"] = "1";
        fixture["cases"]![0]!["symbol"] = "NL";
        fixture["cases"]![0]!["analysis_type"] = "material_nonlinear";
        fixture["results"]![0]!["state"] = JsonNode.Parse("""{"kind":"load_step","index":0,"load_factor":1,"is_final":true}""");
        fixture["results"]![0]!["diagnostics"]!["iterations"] = JsonNode.Parse("""[{"index":0,"residual_norm":0,"correction_norm":0,"converged":true}]""");
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        JsonObject saved = Saved();
        saved["load"] = new JsonObject();
        JsonObject root = Project(presentation, saved, new([PrintOption.SectionDiagram]));
        Assert.Equal("Case 1 NL (Step 0, 1)",
            root["PrintDiagram"]!["diagramResult"]!["caseLabels"]!["1/LoadStep/0"]!.GetValue<string>());
        Assert.Equal("1 (Step 0, 1)", root["fsecName"]![0]![1]!.GetValue<string>());
    }

    [Fact]
    public void MovingDisplacementDiagramRetainsSelectedCorrelatedRawSourceAndTableValues()
    {
        JsonObject fixture = Fixture();
        fixture["cases"]![0]!["symbol"] = "LL";
        JsonNode childCase = fixture["cases"]![0]!.DeepClone();
        childCase["case_id"] = "1.1";
        fixture["cases"]!.AsArray().Add(childCase);
        JsonNode childResult = fixture["results"]![0]!.DeepClone();
        childResult["case_id"] = "1.1";
        childResult["node_displacements"]![1]!["components"]!["dx"] = 0.0023;
        childResult["node_displacements"]![1]!["components"]!["rz"] = -0.004;
        fixture["results"]!.AsArray().Add(childResult);
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        JsonObject saved = Saved();
        JsonObject tables = Project(presentation, saved, new([PrintOption.Displacement],
            NodeIds: ["2"], DisplacementComponents: ["rz_max"]));
        JsonObject root = Project(presentation, saved, new([PrintOption.SectionDiagram],
            NodeIds: ["2"], DisplacementComponents: ["rz_max"], DiagramComponents: ["disg"]));
        Assert.True(JsonNode.DeepEquals(tables["disg"], root["disg"]));
        JsonNode rawModes = root["PrintDiagram"]!["disg"]!["1/Static/0"]!;
        Assert.Equal(6, rawModes.AsObject().Count);
        Assert.Single(root["disg"]!["1/Static/0"]!.AsObject());
        Assert.Equal(2, rawModes["rz_max"]!.AsObject().Count);
        JsonNode raw = rawModes["rz_max"]!["2"]!;
        Assert.Equal("1.1", raw["case"]!.GetValue<string>());
        Assert.Equal(0.0023, raw["dx"]!.GetValue<double>());
        Assert.Equal(-0.004, raw["rz"]!.GetValue<double>());
        Assert.Equal(4, root["disg"]!["1/Static/0"]!["rz_max"]!["2"]!["rz"]!.GetValue<double>());
        Assert.Equal(-0.004, presentation.Pages[0].MovingChildren[0].NodeDisplacements[1].Components.Rz);
        Assert.Equal(new[] { "2" }, root["PrintDiagram"]!["diagramResult"]!["displacementLabelNodeIds"]!.AsArray()
            .Select(node => node!.GetValue<string>()));
    }

    [Fact]
    public void DiagramForceRowsRetainBothSidesAtAStationWithoutChangingTables()
    {
        JsonObject fixture = SplitFixture("opaque-mid", true);
        JsonObject saved = SavedForFixture(fixture);
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        JsonObject table = Project(presentation, saved, new([PrintOption.SectionForce]));
        JsonObject root = Project(presentation, saved,
            new([PrintOption.SectionForce, PrintOption.SectionDiagram], DiagramComponents: ["mz"]));
        Assert.True(JsonNode.DeepEquals(table["fsec"], root["fsec"]));
        Assert.Equal(3, root["fsec"]!["1/Static/0"]!.AsArray().Count);
        Assert.NotNull(root["PrintDiagram"]!["fsec"]);
        JsonArray rows = root["PrintDiagram"]!["fsec"]!["1/Static/0"]!.AsArray();
        Assert.Equal(new[] { "S0", "S1", "S1", "S2" }, rows.Select(row => row!["n"]!.GetValue<string>()));
        Assert.Equal(new[] { 0.0, 1.0, 1.0, 2.0 }, rows.Select(row => row!["l"]!.GetValue<double>()));
        Assert.Equal(new[] { 0.0, 10.0, 10.0, 0.0 }, rows.Select(row => row!["mz"]!.GetValue<double>()));
        Assert.Equal(new[] { 10.0, 10.0, -10.0, -10.0 }, rows.Select(row => row!["fy"]!.GetValue<double>()));
        Assert.All(rows, row => Assert.Equal("M1", row!["m"]!.GetValue<string>()));
    }

    [Fact]
    public void MovingDiagramEnvelopesSelectEachSideOfStationWithoutChangingTableEnvelope()
    {
        JsonObject fixture = SplitFixture("opaque-mid", true);
        fixture["cases"]![0]!["symbol"] = "LL";
        JsonNode childCase = fixture["cases"]![0]!.DeepClone();
        childCase["case_id"] = "1.1";
        fixture["cases"]!.AsArray().Add(childCase);
        JsonNode child = fixture["results"]![0]!.DeepClone();
        child["case_id"] = "1.1";
        child["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["fy"] = 20;
        child["member_section_forces"]![0]!["segments"]![0]!["j_end"]!["fy"] = 20;
        child["member_section_forces"]![0]!["segments"]![1]!["i_end"]!["fy"] = -30;
        child["member_section_forces"]![0]!["segments"]![1]!["j_end"]!["fy"] = -30;
        fixture["results"]!.AsArray().Add(child);
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        JsonObject saved = SavedForFixture(fixture);
        JsonObject tables = Project(presentation, saved, new([PrintOption.SectionForce],
            SectionForceComponents: ["fy_max", "fy_min"]));
        JsonObject root = Project(presentation, saved,
            new([PrintOption.SectionForce, PrintOption.SectionDiagram],
                SectionForceComponents: ["fy_max", "fy_min"], DiagramComponents: ["fy"]));
        Assert.True(JsonNode.DeepEquals(tables["fsec"], root["fsec"]));
        Assert.NotNull(root["PrintDiagram"]!["fsec"]);
        JsonNode envelope = root["PrintDiagram"]!["fsec"]!["1/Static/0"]!;
        Assert.Equal(new[] { 20.0, 20.0, -10.0, -10.0 },
            envelope["fy_max"]!.AsArray().Select(row => row!["fy"]!.GetValue<double>()));
        Assert.Equal(new[] { "1.1", "1.1", "1", "1" },
            envelope["fy_max"]!.AsArray().Select(row => row!["case"]!.GetValue<string>()));
        Assert.Equal(new[] { 10.0, 10.0, -30.0, -30.0 },
            envelope["fy_min"]!.AsArray().Select(row => row!["fy"]!.GetValue<double>()));
        Assert.Equal(new[] { "1", "1", "1.1", "1.1" },
            envelope["fy_min"]!.AsArray().Select(row => row!["case"]!.GetValue<string>()));
        Assert.Equal(new[] { 0.0, 1.0, 1.0, 2.0 },
            envelope["fy_max"]!.AsArray().Select(row => row!["l"]!.GetValue<double>()));
    }

    [Theory]
    [InlineData("opaque-mid", true)]
    [InlineData("interior-original", false)]
    [InlineData("generated:other-member:S99", true)]
    public void DisplacementStationsMatchActualRotatedGeometryWithoutParsingNodeIds(string middleId, bool generated)
    {
        JsonObject fixture = SplitFixture(middleId, generated);
        fixture["topology"]!["nodes"]![1]!["coordinates"] = JsonNode.Parse("""{"x":1.2,"y":1.6,"z":0}""");
        fixture["topology"]!["nodes"]![2]!["coordinates"] = JsonNode.Parse("""{"x":0.6,"y":0.8,"z":0}""");
        fixture["topology"]!["members"]![0]!["local_frame"]!["x_axis"] = JsonNode.Parse("""{"x":0.6,"y":0.8,"z":0}""");
        fixture["topology"]!["members"]![0]!["local_frame"]!["y_axis"] = JsonNode.Parse("""{"x":-0.8,"y":0.6,"z":0}""");
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        JsonObject root = Project(presentation, SavedForFixture(fixture), new([PrintOption.SectionDiagram],
            NodeIds: [middleId], DiagramComponents: ["disg"]));
        JsonNode details = root["PrintDiagram"]!["diagramResult"]!;
        Assert.NotNull(details["displacementStations"]);
        JsonArray stations = details["displacementStations"]!["M1"]!.AsArray();
        Assert.Equal(new[] { "1", middleId, "2" }, stations.Select(row => row!["nodeId"]!.GetValue<string>()));
        Assert.Equal(new[] { 0.0, 1.0, 2.0 }, stations.Select(row => row!["position"]!.GetValue<double>()));
        Assert.Equal(3, root["PrintDiagram"]!["disg"]!["1/Static/0"]!.AsArray().Count);
        Assert.Single(root["disg"]!["1/Static/0"]!.AsArray());
        Assert.Equal(middleId, Assert.Single(details["displacementLabelNodeIds"]!.AsArray())!.GetValue<string>());
        JsonNode raw = root["PrintDiagram"]!["disg"]!["1/Static/0"]!.AsArray()
            .Single(row => row!["id"]!.GetValue<string>() == middleId)!;
        Assert.Equal(0.004, raw["dx"]!.GetValue<double>());
        Assert.Equal(0.006, raw["dy"]!.GetValue<double>());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("coincident")]
    [InlineData("frame")]
    [InlineData("length")]
    public void UnprovedDisplacementStationAssociationFailsExplicitly(string scenario)
    {
        JsonObject fixture = SplitFixture("opaque-mid", true);
        if (scenario == "missing")
        {
            fixture["topology"]!["nodes"]!.AsArray().RemoveAt(2);
            fixture["results"]![0]!["node_displacements"]!.AsArray().RemoveAt(2);
        }
        else if (scenario == "coincident")
        {
            JsonNode node = fixture["topology"]!["nodes"]![2]!.DeepClone();
            node["node_id"] = "coincident";
            fixture["topology"]!["nodes"]!.AsArray().Add(node);
            JsonNode displacement = fixture["results"]![0]!["node_displacements"]![2]!.DeepClone();
            displacement["node_id"] = "coincident";
            fixture["results"]![0]!["node_displacements"]!.AsArray().Add(displacement);
        }
        else if (scenario == "frame")
            fixture["topology"]!["members"]![0]!["local_frame"]!["origin"]!["x"] = 1;
        else
            fixture["topology"]!["nodes"]![1]!["coordinates"]!["x"] = 3;
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        Assert.Throws<PrintProjectionException>(() => Project(presentation, SavedForFixture(fixture),
            new([PrintOption.SectionDiagram], DiagramComponents: ["disg"])));
    }

    [Fact]
    public void ExplicitEndpointIdentitySurvivesUnrelatedCoincidentNodes()
    {
        JsonObject fixture = SplitFixture("opaque-mid", true);
        JsonNode node = fixture["topology"]!["nodes"]![0]!.DeepClone();
        node["node_id"] = "unconnected-at-start";
        node["source_node_id"] = "unconnected-at-start";
        fixture["topology"]!["nodes"]!.AsArray().Add(node);
        JsonNode displacement = fixture["results"]![0]!["node_displacements"]![0]!.DeepClone();
        displacement["node_id"] = "unconnected-at-start";
        fixture["results"]![0]!["node_displacements"]!.AsArray().Add(displacement);
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: 2);
        JsonObject root = Project(presentation, SavedForFixture(fixture), new([PrintOption.SectionDiagram]));
        JsonNode details = root["PrintDiagram"]!["diagramResult"]!;
        Assert.NotNull(details["displacementStations"]);
        Assert.Equal("1", details["displacementStations"]!["M1"]![0]!["nodeId"]!.GetValue<string>());
        Assert.Null(details["displacementLabelNodeIds"]);
    }

    [Theory]
    [InlineData((int)PrintOption.SectionDiagram, (int)PrintOption.SectionForce, "PrintDiagram", "fsec", "1/Static/0")]
    [InlineData((int)PrintOption.CombinedSectionDiagram, (int)PrintOption.CombinedSectionForce, "CombPrintDiagram", "fsecCombine", "1")]
    [InlineData((int)PrintOption.PickupSectionDiagram, (int)PrintOption.PickupSectionForce, "PickPrintDiagram", "fsecPickup", "1")]
    public void DiagramForceFocusIsIndependentOfTableModeSelection(
        int diagramOption, int tableOption, string diagramKey, string family, string caseKey)
    {
        JsonObject fixture = MovingFixture();
        JsonObject saved = SavedForFixture(fixture);
        var presentation = Presentation(fixture, saved);
        JsonObject table = Project(presentation, saved, new([(PrintOption)tableOption],
            SectionForceComponents: ["fx_max"]));
        JsonObject mixed = Project(presentation, saved,
            new([(PrintOption)tableOption, (PrintOption)diagramOption],
                SectionForceComponents: ["fx_max"], DiagramComponents: ["fy"]));
        Assert.True(JsonNode.DeepEquals(table[family], mixed[family]));
        Assert.Single(mixed[family]![caseKey]!.AsObject());
        Assert.NotNull(mixed[family]![caseKey]!["fx_max"]);
        Assert.NotNull(mixed[diagramKey]![family]);
        JsonNode modes = mixed[diagramKey]![family]![caseKey]!;
        Assert.NotNull(modes["fy_max"]);
        Assert.NotNull(modes["fy_min"]);
        Assert.Equal(20, modes["fy_max"]![0]!["fy"]!.GetValue<double>());
        Assert.Equal(10, modes["fy_min"]![0]!["fy"]!.GetValue<double>());
    }

    [Fact]
    public void MovingDiagramDisplacementFocusIsIndependentOfTableModeSelection()
    {
        JsonObject fixture = MovingFixture();
        JsonObject saved = SavedForFixture(fixture);
        var presentation = Presentation(fixture, saved);
        JsonObject table = Project(presentation, saved, new([PrintOption.Displacement],
            NodeIds: ["2"], DisplacementComponents: ["dx_max"]));
        JsonObject mixed = Project(presentation, saved,
            new([PrintOption.Displacement, PrintOption.SectionDiagram], NodeIds: ["2"],
                DisplacementComponents: ["dx_max"], DiagramComponents: ["disg"]));
        Assert.True(JsonNode.DeepEquals(table["disg"], mixed["disg"]));
        Assert.Single(mixed["disg"]!["1/Static/0"]!.AsObject());
        JsonNode raw = mixed["PrintDiagram"]!["disg"]!["1/Static/0"]!;
        Assert.NotNull(raw["dy_max"]);
        Assert.NotNull(raw["dy_min"]);
        Assert.Equal(6, raw.AsObject().Count);
        Assert.Equal(-0.002, raw["dy_max"]!["2"]!["dy"]!.GetValue<double>());
        Assert.Equal(-0.006, raw["dy_min"]!["2"]!["dy"]!.GetValue<double>());
        Assert.Equal("1.1", raw["dy_min"]!["2"]!["case"]!.GetValue<string>());
    }

    private static JsonObject Project(CalculationResultPresentation? presentation, JsonObject saved,
        PrintSelection selection) => JsonNode.Parse(PrintProjection.Build(new PrintSnapshot(
            selection, saved.ToJsonString(), 1, 1, presentation, presentation?.Derived, [])))!.AsObject();

    private static CalculationResultPresentation Presentation(JsonObject fixture, JsonObject saved, int dimension = 2)
    {
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(fixture.ToJsonString());
        using JsonDocument doc = JsonDocument.Parse(saved.ToJsonString());
        var parsed = InputCombineService.ParseCombineJson(doc.RootElement);
        var derived = CalculationDerivedPresenter.Build(result, dimension,
            CalculationDerivedInputSnapshot.Capture(parsed.Define, parsed.Combine, parsed.Pickup));
        return new(result, derived, dimension);
    }

    private static JsonObject Saved() => JsonNode.Parse("""
        {"dimension":2,"node":{"1":{"x":0,"y":0,"z":0},"2":{"x":1,"y":0,"z":0}},
         "member":{"M1":{"ni":"1","nj":"2","name":"Beam"}},"shell":{},"element":{},
         "load":{"1":{"name":"Dead load"}},"notice_points":[],"rigid":[],
         "define":{"1":{"row":1,"C1":1}},
         "combine":{"1":{"row":1,"name":"Combination One","C1":1}},
         "pickup":{"1":{"row":1,"name":"Envelope One","C1":1}}}
        """)!.AsObject();

    private static JsonObject Fixture()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        JsonObject root = JsonNode.Parse(File.ReadAllText(Path.Combine(directory!.FullName,
            "FrameWeb", "tests", "data", "contracts", "positive", "single-static.json")))!.AsObject();
        root["cases"]![0]!["case_id"] = "1";
        root["results"]![0]!["case_id"] = "1";
        root["results"]![0]!["node_displacements"]![1]!["components"]!["dy"] = -0.002;
        root["results"]![0]!["node_displacements"]![1]!["components"]!["rz"] = 0.003;
        return root;
    }

    private static JsonObject SplitFixture(string middleId, bool generated)
    {
        JsonObject root = Fixture();
        root["topology"]!["nodes"]![1]!["coordinates"]!["x"] = 2;
        root["topology"]!["nodes"]!.AsArray().Add(new JsonObject
        {
            ["node_id"] = middleId, ["source_node_id"] = generated ? null : JsonValue.Create(middleId),
            ["generated"] = generated,
            ["coordinates"] = JsonNode.Parse("""{"x":1,"y":0,"z":0}"""),
        });
        root["results"]![0]!["node_displacements"]!.AsArray().Add(new JsonObject
        {
            ["node_id"] = middleId,
            ["components"] = JsonNode.Parse("""{"dx":0.004,"dy":0.006,"dz":0,"rx":0,"ry":0,"rz":0.002}"""),
        });
        root["topology"]!["members"]![0]!["stations"] = JsonNode.Parse("""
            [{"station_id":"S0","position":0},{"station_id":"S1","position":1},{"station_id":"S2","position":2}]
            """);
        JsonNode first = root["results"]![0]!["member_section_forces"]![0]!["segments"]![0]!;
        first["i_end"]!["fy"] = 10;
        first["j_end"]!["fy"] = 10;
        first["j_end"]!["mz"] = -10;
        JsonNode second = first.DeepClone();
        second["segment_id"] = "S1-S2";
        second["station_i"] = "S1";
        second["station_j"] = "S2";
        second["i_end"]!["fy"] = -10;
        second["i_end"]!["mz"] = -10;
        second["j_end"]!["fy"] = -10;
        second["j_end"]!["mz"] = 0;
        root["results"]![0]!["member_section_forces"]![0]!["segments"]!.AsArray().Add(second);
        return root;
    }

    private static JsonObject SavedForFixture(JsonObject fixture)
    {
        JsonObject saved = Saved();
        foreach (JsonNode node in fixture["topology"]!["nodes"]!.AsArray().OfType<JsonNode>()
            .Where(node => !node["generated"]!.GetValue<bool>()))
            saved["node"]![node["node_id"]!.GetValue<string>()] = node["coordinates"]!.DeepClone();
        return saved;
    }

    private static JsonObject MovingFixture()
    {
        JsonObject fixture = SplitFixture("opaque-mid", true);
        fixture["cases"]![0]!["symbol"] = "LL";
        JsonNode childCase = fixture["cases"]![0]!.DeepClone();
        childCase["case_id"] = "1.1";
        fixture["cases"]!.AsArray().Add(childCase);
        JsonNode child = fixture["results"]![0]!.DeepClone();
        child["case_id"] = "1.1";
        child["node_displacements"]![1]!["components"]!["dx"] = 0.0023;
        child["node_displacements"]![1]!["components"]!["dy"] = -0.006;
        child["node_displacements"]![1]!["components"]!["rz"] = -0.004;
        child["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["fy"] = 20;
        child["member_section_forces"]![0]!["segments"]![0]!["j_end"]!["fy"] = 20;
        child["member_section_forces"]![0]!["segments"]![1]!["i_end"]!["fy"] = -30;
        child["member_section_forces"]![0]!["segments"]![1]!["j_end"]!["fy"] = -30;
        fixture["results"]!.AsArray().Add(child);
        return fixture;
    }
}

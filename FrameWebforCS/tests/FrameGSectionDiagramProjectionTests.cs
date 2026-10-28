using FrameWebforCS.providers.printing;
using System.Text.Json.Nodes;
using Xunit;
using static FrameWebforCS.Tests.SectionDiagramProjectionTests;

namespace FrameWebforCS.Tests;

/// <summary>Data the FrameG 断面力図 layout needs from the saved model (原宿ボックス 0404).</summary>
public sealed class FrameGSectionDiagramProjectionTests
{
    [Fact]
    public void RigidMembersZonesAndSpringMembersReachTheDiagram()
    {
        JsonObject saved = Saved();
        // M1 uses the rigid zones' material; a FrameG 剛域 member converts to exactly that.
        saved["element"] = JsonNode.Parse("""
            {"1":{"1":{"E":2.65e7,"Xp":1e-5,"A":1000,"Iz":1000,"n":"剛域"},
                  "2":{"E":2.65e7,"Xp":1e-5,"A":0.35,"Iz":0.00357,"n":"上床版"}}}
            """);
        saved["member"]!["M1"]!["e"] = "1";
        saved["rigid"] = JsonNode.Parse("""[{"m":"M1","Ilength":0,"Jlength":0.138,"e":1}]""");
        saved["fix_member"] = JsonNode.Parse("""{"1":[{"row":1,"m":"M1","tx":1646,"ty":4937}],"2":[]}""");
        JsonObject root = Project(Presentation(Fixture(), saved), saved, new([PrintOption.SectionDiagram]));
        JsonNode details = root["PrintDiagram"]!["diagramResult"]!;
        Assert.Equal(new[] { "M1" }, details["rigidMembers"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal(new[] { 0, 0.138 }, details["rigidZones"]!["M1"]!.AsArray().Select(node => node!.GetValue<double>()));
        Assert.Equal(new[] { "M1" }, details["springMembers"]!.AsArray().Select(node => node!.GetValue<string>()));
    }

    [Fact]
    public void ModelWithoutRigidZonesOrSpringsGivesEmptyLists()
    {
        JsonObject saved = Saved();
        JsonObject root = Project(Presentation(Fixture(), saved), saved, new([PrintOption.CombinedSectionDiagram]));
        JsonNode details = root["CombPrintDiagram"]!["diagramResult"]!;
        Assert.Empty(details["rigidMembers"]!.AsArray());
        Assert.Empty(details["rigidZones"]!.AsObject());
        Assert.Empty(details["springMembers"]!.AsArray());
    }

    [Fact]
    public void CaptionAddsTheEnteredSymbolOnly()
    {
        // The solver names a case without a symbol "LoadSymbol"; the caption shows only an entered symbol.
        JsonObject fixture = Fixture();
        fixture["cases"]![0]!["symbol"] = "LoadSymbol";
        JsonObject saved = Saved();
        JsonObject root = Project(Presentation(fixture, saved), saved, new([PrintOption.SectionDiagram]));
        Assert.Equal("Case 1 Dead load",
            root["PrintDiagram"]!["diagramResult"]!["caseLabels"]!["1/Static/0"]!.GetValue<string>());
        saved["load"]!["1"]!["symbol"] = "D1";
        root = Project(Presentation(fixture, saved), saved, new([PrintOption.SectionDiagram]));
        Assert.Equal("Case 1 Dead load:D1",
            root["PrintDiagram"]!["diagramResult"]!["caseLabels"]!["1/Static/0"]!.GetValue<string>());
    }
}

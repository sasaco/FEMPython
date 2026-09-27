using FrameWebforCS.calculation;
using FrameWebforCS.three;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class CalculationThreeResultsTests
{
    [Fact]
    public void CanonicalStringIdsRenderDisplacementReactionAndSectionForce()
    {
        var scene = new Scene();
        using var renderer = new ThreeResultsService(scene);
        CalculationResultPresentation presentation = Presentation();
        CalculationResultPage page = presentation.Pages[0];
        renderer.SetCanonicalPresentation(presentation);

        renderer.SetMode("disg", page.Key);
        Assert.Contains(scene.Children[0].Children, item => item.Name == "memberbeam/A");
        Assert.Equal("alpha", renderer.CurrentExtrema!.Value.Primary.MaxEntityId);

        renderer.SetMode("reac", page.Key);
        Assert.Contains(scene.Children[1].Children, item => item.Name == "reacalpha-tx");
        Assert.Equal(8, renderer.CurrentExtrema!.Value.Primary.Max);

        renderer.SetMode("fsec", page.Key, "axialForce");
        Assert.Contains(scene.Children[2].Children, item => item.Name == "fsecbeam/A-S0-S1");
        Assert.Equal("beam/A", renderer.CurrentExtrema!.Value.Primary.MaxEntityId);
    }

    [Fact]
    public void CanonicalDerivedModesUseDerivedRowsWithStringIds()
    {
        var scene = new Scene();
        using var renderer = new ThreeResultsService(scene);
        CalculationResultPresentation baseline = Presentation();
        CalculationDerivedCase combined = DerivedCase("mix");
        CalculationDerivedCase picked = DerivedCase("pick");
        var derived = new CalculationDerivedPresentation([], [combined], [picked]);
        renderer.SetCanonicalPresentation(new CalculationResultPresentation(baseline.ResultSet, derived));

        renderer.SetMode("comb_disg", "mix", "dx_max");
        Assert.Contains(scene.Children[0].Children, item => item.Name == "memberbeam/A");
        renderer.SetMode("comb_reac", "mix", "fx_max");
        Assert.Contains(scene.Children[1].Children, item => item.Name == "reacalpha-tx");
        renderer.SetMode("comb_fsec", "mix", "fx_max");
        Assert.Contains(scene.Children[2].Children, item => item.Name == "fsecbeam/A-S0-S1");
        renderer.SetMode("pick_fsec", "pick", "fx_max");
        Assert.Contains(scene.Children[2].Children, item => item.Name == "fsecbeam/A-S0-S1");
    }

    private static CalculationDerivedCase DerivedCase(string id)
    {
        CalculationDerivedRow Row(string entity, string? station, string name, double value) =>
            new(entity, station, new Dictionary<string, double> { [name] = value }, id, id);
        var displacements = CalculationDerivedPresentation.ReadOnlyModes(
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>
            {
                ["dx_max"] = [Row("alpha", null, "dx", 1), Row("beta", null, "dx", 0)]
            });
        var reactions = CalculationDerivedPresentation.ReadOnlyModes(
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>
            {
                ["fx_max"] = [Row("alpha", null, "fx", 4)]
            });
        var sections = CalculationDerivedPresentation.ReadOnlyModes(
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>
            {
                ["fx_max"] = [Row("beam/A", "S0", "fx", 5),
                    Row("beam/A", "S1", "fx", 6)]
            });
        return new CalculationDerivedCase(id, null, displacements, reactions, sections);
    }

    private static CalculationResultPresentation Presentation()
    {
        var zero = new Vector3Value(0, 0, 0);
        var frame = new CoordinateFrame(zero, new(1, 0, 0), new(0, 1, 0), new(0, 0, 1));
        var topology = new AnalysisTopology(
        [
            new TopologyNode("alpha", zero, null, false),
            new TopologyNode("beta", new(2, 0, 0), null, false)
        ],
        [
            new TopologyMember("beam/A", "alpha", "beta", frame,
                [new MemberStation("S0", 0), new MemberStation("S1", 2)])
        ], [], []);
        var cases = new[]
        {
            new AnalysisCase("travel", "Travel", "LL", AnalysisType.Static, ["alpha"]),
            new AnalysisCase("travel.1", "Position 1", "LL", AnalysisType.Static, ["alpha"]),
            new AnalysisCase("travel.2", "Position 2", "LL", AnalysisType.Static, ["alpha"])
        };
        var resultSet = new AnalysisResultSet("analysis_result_set", "1.0",
            new AnalysisUnits("SI", "m", "N", "kg", "s"),
            new CoordinateSystem("global_cartesian", "right", ["x", "y", "z"]),
            cases, topology,
            [Result("travel", 100), Result("travel.1", -5), Result("travel.2", 8)]);
        return new CalculationResultPresentation(resultSet);
    }

    private static StaticAnalysisResult Result(string caseId, double reaction) => new(
        caseId,
        [
            new NodeDisplacement("alpha", new(1, 0, 0, 0, 0, 0)),
            new NodeDisplacement("beta", new(0, 0, 0, 0, 0, 0))
        ],
        [new SupportReaction("alpha", new(reaction, 0, 0, 0, 0, 0))],
        [new MemberSectionForces("beam/A",
            [new MemberSegmentResult("S0-S1", "S0", "S1", 2,
                new(3, 0, 0, 0, 0, 0), new(-2, 0, 0, 0, 0, 0))])],
        [], [], new WarningDiagnostics([]));
}

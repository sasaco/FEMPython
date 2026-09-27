using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.three;
using THREE;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class ThreeResultsServiceTests
{
    [Fact]
    public void DisplacementDrawsMemberAndPanelEdgesForSelectedCaseAndNodeMove()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        var nodes = Nodes();
        results.SetTopology(nodes, Members(), Panels(), 0.2f);
        results.SetBaseResults(new()
        {
            ["Case1"] = Disg(1), ["Case2"] = Disg(2)
        }, new(), new(), 1);
        results.SetMode("disg", "1");
        Assert.Equal(3, results.DisplacementCount); // member plus two distinct panel edges
        var first = Assert.IsType<Line>(scene.Children[0].Children[0]);
        Assert.Equal("member10", first.Name);
        Assert.Equal(MathF.Sqrt(200) * 0.01f, Positions(first)[0], 5);
        Assert.Equal(10f, Positions(first)[3], 5);

        results.SetMode("disg", "2");
        Assert.Equal(MathF.Sqrt(200) * 0.01f, Positions(Assert.IsType<Line>(scene.Children[0].Children[0]))[0], 5);
        nodes[1] = new Vector3(5, 0, 0);
        results.SetTopology(nodes, Members(), Panels(), 0.2f);
        Assert.Equal(5f + MathF.Sqrt(200) * 0.01f,
            Positions(Assert.IsType<Line>(scene.Children[0].Children[0]))[0], 5);
    }

    [Fact]
    public void DisplacementOmitsPanelEdgesAlreadyRepresentedByMembersInEitherDirection()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), new Dictionary<int, DisplayMember>
        {
            [10] = new(3, 1, 1, 0)
        }, Panels(), 0.2f);
        results.SetBaseResults(new() { ["1"] = Disg(1) }, new(), new(), 1);
        results.SetMode("disg", "1");

        Assert.Equal(3, results.DisplacementCount);
        Assert.Equal(1, scene.Children[0].Children.Count(item => item.Name == "member10"));
        Assert.DoesNotContain(scene.Children[0].Children, item => item.Name == "panel20-0");
    }

    [Fact]
    public void ReactionForceUsesLegacyCircleScaleNegativeAxisAndOneFifthExtent()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 0.2f);
        results.SetBaseResults(new(), new()
        {
            ["1"] = new() { ["node1"] = new clsReac { tx = 10, ty = 5 } }
        }, new(), 1);
        results.SetMode("reac", "1");

        var xGroup = Assert.IsType<Group>(scene.Children[1].Children.Single(item => item.Name == "reac1-tx"));
        var yGroup = Assert.IsType<Group>(scene.Children[1].Children.Single(item => item.Name == "reac1-ty"));
        Assert.IsType<Mesh>(xGroup.Children[0]);
        var x = Positions(Assert.IsType<Line>(xGroup.Children[1]));
        var y = Positions(Assert.IsType<Line>(yGroup.Children[1]));
        Assert.Equal(-3.2f, x[3], 5); // baseScale * 80 * 0.2
        Assert.Equal(-3.2f * MathF.Sqrt(0.75f), y[4], 5);
    }

    [Fact]
    public void LargeTopologyDisplacementRetainsLineScaleAcrossPageChanges()
    {
        const int count = 100_000;
        var nodes = Enumerable.Range(0, count)
            .ToDictionary(i => i + 1, i => new Vector3(i, 0, 0));
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(nodes,
            new Dictionary<int, DisplayMember> { [1] = new(1, count, 1, 0) },
            new Dictionary<int, DisplayPanel>(), 1);
        results.SetBaseResults(new()
        {
            ["1"] = new()
            {
                ["node1"] = new clsDisg { dx = 1 },
                [$"node{count}"] = new clsDisg { dx = 0 }
            }
        }, new(), new(), 1);
        results.SetMode("disg", "1");
        float expected = (count - 1) * 0.01f;
        Assert.Equal(expected, Positions(Assert.IsType<Line>(scene.Children[0].Children[0]))[0], 4);
        results.SetMode("disg", "1");
        Assert.Equal(expected, Positions(Assert.IsType<Line>(scene.Children[0].Children[0]))[0], 4);
    }

    [Fact]
    public void MovingDisplacementCyclesParentAndChildrenEveryTenFramesAndStopsOnModeChange()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 1);
        results.SetBaseResults(new()
        {
            ["1"] = Disg(1), ["1.1"] = Disg(2), ["1.2"] = Disg(3),
            ["10.1"] = Disg(4)
        }, new(), new(), 1, ["1"]);
        results.SetMode("disg", "1");
        Assert.Equal("1", results.RenderedDisplacementCase);
        for (int i = 0; i < 9; i++) results.AdvanceDisplacementAnimation();
        Assert.Equal("1", results.RenderedDisplacementCase);
        results.AdvanceDisplacementAnimation();
        Assert.Equal("1.1", results.RenderedDisplacementCase);
        Assert.Equal("1", results.CurrentIndex);
        Assert.Equal("1.1", results.CurrentExtrema?.CaseId);
        for (int i = 0; i < 20; i++) results.AdvanceDisplacementAnimation();
        Assert.Equal("1", results.RenderedDisplacementCase);
        results.SetMode("reac", "1");
        Assert.Null(results.RenderedDisplacementCase);
        for (int i = 0; i < 20; i++) results.AdvanceDisplacementAnimation();
        results.SetMode("disg", "1");
        Assert.Equal("1", results.RenderedDisplacementCase);
        results.SetBaseResults(new() { ["1"] = Disg(5) }, new(), new(), 2);
        for (int i = 0; i < 20; i++) results.AdvanceDisplacementAnimation();
        Assert.Equal("1", results.RenderedDisplacementCase);
    }

    [Fact]
    public void ResultScalesAreBoundedAndCurrentExtremaTrackSignedCases()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 1);
        results.SetBaseResults(new()
        {
            ["1"] = new()
            {
                ["node1"] = new clsDisg { dx = -2, rz = 0.5 },
                ["node3"] = new clsDisg { dy = 3, ry = -0.2 }
            }
        }, new()
        {
            ["1"] = new() { ["node1"] = new clsReac { tx = -4, my = 7 } }
        }, new()
        {
            ["1"] = new() { ["member10"] = new()
            {
                ["P1"] = new clsFsec { L = 10, myi = -2, myj = 5 }
            } }
        }, 1);
        results.SetMode("disg", "1");
        var displacement = results.CurrentExtrema!.Value;
        Assert.Equal((-2d, 3d, "node1", "node3"),
            (displacement.Primary.Min, displacement.Primary.Max,
                displacement.Primary.MinEntityId, displacement.Primary.MaxEntityId));
        Assert.Equal(-0.2, displacement.Secondary!.Value.Min);
        float before = Positions(Assert.IsType<Line>(scene.Children[0].Children[0]))[0];
        results.SetDisplacementScale(1);
        Assert.Equal(before * 2, Positions(Assert.IsType<Line>(scene.Children[0].Children[0]))[0], 4);
        results.SetMode("reac", "1");
        Assert.Equal(-4, results.CurrentExtrema?.Primary.Min);
        Assert.Equal(7, results.CurrentExtrema?.Secondary?.Max);
        results.SetReactionScale(2);
        Assert.Equal(2f, Assert.IsType<Group>(scene.Children[1].Children[0]).Scale.X);
        results.SetMode("fsec", "1", "momentY");
        Assert.Equal((-2d, 5d),
            (results.CurrentExtrema!.Value.Primary.Min, results.CurrentExtrema!.Value.Primary.Max));
        var diagram = Assert.IsType<Group>(scene.Children[2].Children[0]);
        float beforeFace = Positions(Assert.IsType<Mesh>(diagram.Children[0]))[4];
        results.SetSectionForceScale(200);
        diagram = Assert.IsType<Group>(scene.Children[2].Children[0]);
        Assert.Equal(beforeFace * 2, Positions(Assert.IsType<Mesh>(diagram.Children[0]))[4], 4);
        Assert.Throws<ArgumentOutOfRangeException>(() => results.SetDisplacementScale(2.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => results.SetReactionScale(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => results.SetSectionForceScale(float.NaN));
        results.SetMode("", null);
        Assert.Null(results.CurrentExtrema);
        Assert.Equal(0.5f, results.DisplacementScale);
    }

    [Fact]
    public void ReactionPageAndModeHideWithoutStaleShapes()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 0.2f);
        results.SetBaseResults(new(), new()
        {
            ["1"] = new() { ["node1"] = new clsReac { tx = 10, my = -5 } },
            ["2"] = new() { ["node3"] = new clsReac { tz = 20 } }
        }, new(), 1);
        results.SetMode("reac", "1");
        Assert.Equal(2, results.ReactionCount);
        Assert.All(scene.Children[1].Children, shape => Assert.StartsWith("reac1-", shape.Name));
        var moment = Assert.IsType<Group>(scene.Children[1].Children[1]);
        Assert.Equal(-0.2f, moment.Scale.X, 5);
        Assert.Equal(21 * 3, Positions(Assert.IsType<Line>(moment.Children[0])).Length);
        Assert.IsType<Mesh>(moment.Children[1]);
        results.SetMode("reac", "2");
        Assert.Single(scene.Children[1].Children);
        Assert.Equal("reac3-tz", scene.Children[1].Children[0].Name);
        results.SetMode("comb_reac", "1");
        Assert.Equal(0, results.ReactionCount); // JS hides combined-reaction glyphs.
        Assert.False(scene.Children[1].Visible);
    }

    [Fact]
    public void DifferentEndRotationsUseTwentyHermiteStations()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 1);
        results.SetBaseResults(new()
        {
            ["1"] = new()
            {
                ["node1"] = new clsDisg { rz = 0.1 },
                ["node3"] = new clsDisg { rz = -0.1 }
            }
        }, new(), new(), 1);
        results.SetMode("disg", "1");
        var positions = Positions(Assert.IsType<Line>(scene.Children[0].Children[0]));
        Assert.Equal(21 * 3, positions.Length);
        Assert.NotEqual(0f, positions[10 * 3 + 1]);
    }

    [Fact]
    public void BaseAndDerivedSectionForceUseCaseComponentAndRejectOlderGeneration()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 0.2f);
        results.SetBaseResults(new(), new(), new()
        {
            ["1"] = new() { ["member10"] = new()
            {
                ["P1"] = new clsFsec { L = 10, myi = 2, myj = 4 }
            } }
        }, 1);
        results.SetMode("fsec", "1", "momentY");
        Assert.Equal(1, results.SectionForceCount);
        var baseDiagram = Assert.IsType<Group>(scene.Children[2].Children[0]);
        Assert.Equal("fsec10-single", baseDiagram.Name);
        Assert.IsType<Mesh>(baseDiagram.Children[0]);
        Assert.Equal(12, Positions(Assert.IsType<Line>(baseDiagram.Children[1])).Length);

        results.SetMode("comb_fsec", "C1", "my_max");
        results.SetDerivedFsec("comb_fsec", new Dictionary<string, IReadOnlyList<SectionForceSample>>
        {
            ["C1"] = [new(10, 0, 3), new(10, 10, 6)]
        }, 5);
        Assert.Equal(1, results.SectionForceCount);
        results.SetDerivedFsec("comb_fsec", new Dictionary<string, IReadOnlyList<SectionForceSample>>(), 4);
        Assert.Equal(1, results.SectionForceCount);
        results.SetBaseResults(new(), new(), new(), 2);
        Assert.Equal(0, results.SectionForceCount);
        results.SetDerivedFsec("comb_fsec", new Dictionary<string, IReadOnlyList<SectionForceSample>>
        {
            ["C1"] = [new(10, 0, 9), new(10, 10, 9)]
        }, 4);
        Assert.Equal(0, results.SectionForceCount);
        results.SetDerivedFsec("comb_fsec", new Dictionary<string, IReadOnlyList<SectionForceSample>>
        {
            ["C1"] = [new(10, 0, 9), new(10, 10, 9)]
        }, 6);
        Assert.Equal(1, results.SectionForceCount);
        results.SetMode("pick_fsec", "P1");
        results.SetDerivedFsec("pick_fsec", new Dictionary<string, IReadOnlyList<SectionForceSample>>
        {
            ["P1"] = [new(10, 0, 1), new(10, 10, 2)]
        }, 6);
        Assert.Equal(1, results.SectionForceCount);
    }

    [Fact]
    public void SectionForceUsesLegacyTwoDimensionalDefaultComponent()
    {
        Assert.Equal("momentZ", ThreeResultsService.DefaultSectionForceComponent(2));
        Assert.Equal("momentY", ThreeResultsService.DefaultSectionForceComponent(3));
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 1);
        results.SetBaseResults(new(), new(), new()
        {
            ["1"] = new() { ["member10"] = new()
            {
                ["P1"] = new clsFsec { L = 10, myi = 9, myj = 9, mzi = 2, mzj = 4 }
            } }
        }, 1);
        results.SetMode("fsec", "1", ThreeResultsService.DefaultSectionForceComponent(2));
        var diagram = Assert.IsType<Group>(Assert.Single(scene.Children[2].Children));
        var face = Positions(Assert.IsType<Mesh>(diagram.Children[0]));
        Assert.Equal(2.5f, face[4], 4); // mz i-end: 2 * (5 / 4), along local Y
    }

    [Fact]
    public void SectionForceUsesRolledMemberAxisAndSplitsSignCrossing()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), new Dictionary<int, DisplayMember>
        {
            [10] = new(1, 3, 1, 90)
        }, new Dictionary<int, DisplayPanel>(), 1);
        results.SetBaseResults(new(), new(), new()
        {
            ["1"] = new() { ["member10"] = new()
            {
                ["P1"] = new clsFsec { L = 10, myi = 2, myj = -2 }
            } }
        }, 1);
        results.SetMode("fsec", "1", "momentY");
        var diagram = Assert.IsType<Group>(Assert.Single(scene.Children[2].Children));
        var face = Positions(Assert.IsType<Mesh>(diagram.Children[0]));
        Assert.Equal(18, face.Length); // two triangles meeting at the zero crossing
        Assert.Equal(-5f, face[4], 4); // rolled local Z points toward global -Y
        Assert.Equal(5f, face[6], 4); // sign crossing at half the member length
        Assert.Equal(0f, face[7], 4);
    }

    [Fact]
    public void SectionForceDisplaysBothEnvelopeFaces()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 1);
        results.SetMode("comb_fsec", "C1", "my_max");
        results.SetDerivedFsec("comb_fsec", new Dictionary<string, IReadOnlyList<SectionForceSample>>
        {
            ["C1"] =
            [
                new(10, 0, 3, SectionForceEnvelope.Max),
                new(10, 10, 4, SectionForceEnvelope.Max),
                new(10, 0, -2, SectionForceEnvelope.Min),
                new(10, 10, -5, SectionForceEnvelope.Min)
            ]
        }, 1);
        var diagrams = scene.Children[2].Children.Cast<Group>().ToArray();
        Assert.Equal(["fsec10-max", "fsec10-min"], diagrams.Select(item => item.Name));
        Assert.All(diagrams, diagram =>
        {
            Assert.IsType<Mesh>(diagram.Children[0]);
            Assert.IsType<Line>(diagram.Children[1]);
        });
    }

    [Fact]
    public void ReplacementClearsPriorResultsAndOlderDocumentIsIgnored()
    {
        var scene = new Scene();
        using var results = new ThreeResultsService(scene);
        results.SetTopology(Nodes(), Members(), new Dictionary<int, DisplayPanel>(), 1);
        results.SetBaseResults(new() { ["1"] = Disg(1) }, new(), new(), 3);
        results.SetMode("disg", "1");
        Assert.Single(scene.Children[0].Children);
        results.SetBaseResults(new(), new(), new(), 4);
        Assert.Equal(0, results.DisplacementCount);
        results.SetBaseResults(new() { ["1"] = Disg(1) }, new(), new(), 3);
        Assert.Equal(0, results.DisplacementCount);
    }

    [Fact]
    public void DisposeReleasesRootsAndRejectsFurtherUpdates()
    {
        var scene = new Scene();
        var results = new ThreeResultsService(scene);
        results.Dispose();
        results.Dispose();
        Assert.Empty(scene.Children);
        Assert.Throws<ObjectDisposedException>(() => results.SetMode("disg", "1"));
    }

    private static Dictionary<int, Vector3> Nodes() => new()
    {
        [1] = new(0, 0, 0), [3] = new(10, 0, 0), [9] = new(0, 10, 0)
    };

    private static Dictionary<int, DisplayMember> Members() => new() { [10] = new(1, 3, 1, 0) };
    private static Dictionary<int, DisplayPanel> Panels() => new() { [20] = new(1, [1, 3, 9]) };

    private static Dictionary<string, clsDisg> Disg(double dx) => new()
    {
        ["node1"] = new clsDisg { dx = dx },
        ["node3"] = new clsDisg { dx = 0 },
        ["node9"] = new clsDisg { dx = 0 }
    };

    private static float[] Positions(Line line) =>
        Assert.IsType<BufferAttribute<float>>(
            Assert.IsType<BufferGeometry>(line.Geometry).GetAttribute<float>("position")).Array;

    private static float[] Positions(Mesh mesh) =>
        Assert.IsType<BufferAttribute<float>>(
            Assert.IsType<BufferGeometry>(mesh.Geometry).GetAttribute<float>("position")).Array;
}

[Collection("DisplacementSingletons")]
public sealed class ResultPageActivationTests
{
    [Fact]
    public void CombinedPagePublishesWhenInitiallyShownAfterRouteSelection()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var coordinator = ResultCombineDisgCoordinator.Instance;
                coordinator.BeginLoad();
                using var definitions = JsonDocument.Parse("""
                    {"define":{"5":{"row":1,"C1":1}},
                     "combine":{"7":{"row":1,"name":"Main","C5":1}}}
                    """);
                InputCombineService.Instance.setCombineJson(definitions.RootElement);
                using var displacements = JsonDocument.Parse("""
                    {"result":{"1":{"disg":{"11":{"dx":1,"dy":0,"rz":0}}}}}
                    """);
                ResultDisgService.Instance.setDisgJson(displacements.RootElement);
                coordinator.CompleteLoad(2);

                using var form = new Form();
                using var view = new ResultCombineDisgComponent();
                form.Controls.Add(view);
                // A hidden view starts calculation only after HandleCreated.
                // Force its handle while the form remains hidden so this test
                // exercises an already materialized page becoming visible.
                _ = view.Handle;
                var pages = new List<(string Mode, string? CaseId)>();
                void Observe(string mode, string? id, string? _) => pages.Add((mode, id));
                ThreeResultsService.ResultPageChanged += Observe;
                try
                {
                    view.setActiveSheet(1); // The route calls this before Show.
                    var timer = Stopwatch.StartNew();
                    while (view.Controls.Find("fpSpread1", true).OfType<FarPoint.Win.Spread.FpSpread>()
                               .Single().Sheets.Count == 0 && timer.Elapsed < TimeSpan.FromSeconds(10))
                    {
                        Application.DoEvents();
                        Thread.Sleep(10);
                    }
                    Assert.NotEmpty(view.Controls.Find("fpSpread1", true)
                        .OfType<FarPoint.Win.Spread.FpSpread>().Single().Sheets);
                    Assert.DoesNotContain(pages, page => page.Mode == "comb_disg");
                    form.Show();
                    timer.Restart();
                    while (!pages.Any(page => page == ("comb_disg", "7")) &&
                           timer.Elapsed < TimeSpan.FromSeconds(10))
                    {
                        Application.DoEvents();
                        Thread.Sleep(10);
                    }
                    Assert.Contains(("comb_disg", "7"), pages);
                }
                finally
                {
                    ThreeResultsService.ResultPageChanged -= Observe;
                    coordinator.FailLoad();
                    ResultDisgService.Instance.clear();
                }
            }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "STA result activation test timed out.");
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}

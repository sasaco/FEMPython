using FrameWebforCS.components.input;
using FrameWebforCS.three;
using System.Text.Json;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ThreePanelsServiceTests
{
    [Fact]
    public void SparseTriangleAndQuadrilateralCreateOneMeshPerTriangleWithStablePanelSelection()
    {
        var scene = new Scene();
        using var panels = new ThreePanelsService(scene);
        panels.ReplaceAll(new Dictionary<int, Vector3>
        {
            [7] = new(0, 0, 0), [42] = new(10, 0, 0),
            [81] = new(10, 10, 0), [99] = new(0, 10, 0)
        }, new Dictionary<int, DisplayPanel>
        {
            [12] = new(1, [7, 42, 81]), [25] = new(2, [7, 42, 81, 99])
        });
        Assert.Equal(2, panels.PanelCount);
        var root = Assert.Single(scene.Children);
        var triangle = Assert.IsType<Mesh>(root.Children[0]);
        var quadFirst = Assert.IsType<Mesh>(root.Children[1]);
        var quadSecond = Assert.IsType<Mesh>(root.Children[2]);
        Assert.Equal("panel-12", triangle.Name);
        Assert.Equal("panel-25", quadFirst.Name);
        Assert.Equal("panel-25", quadSecond.Name);
        Assert.Equal(9, PositionCount(triangle));
        Assert.Equal(9, PositionCount(quadFirst));
        Assert.Equal(9, PositionCount(quadSecond));
        Assert.Same(quadFirst.Material, quadSecond.Material);
        Assert.Equal(Constants.DoubleSide, quadFirst.Material.Side);

        panels.SetMode(true, 0.7f);
        Assert.Equal(25, panels.Pick(new Raycaster(new Vector3(1, 9, 10), new Vector3(0, 0, -1))));
        panels.Select(25);
        Assert.Equal(25, panels.SelectedPanelId);
        Assert.Equal(0x00AFAF, quadFirst.Material.Color!.Value.GetHex());
        Assert.Equal(0x00AFAF, quadSecond.Material.Color!.Value.GetHex());
        panels.Select(25, 0xFF0000);
        Assert.Equal(0xFF0000, quadFirst.Material.Color!.Value.GetHex());
        Assert.Equal(0xFF0000, quadSecond.Material.Color!.Value.GetHex());
        panels.Select(25);
        Assert.Equal(0x00AFAF, quadFirst.Material.Color!.Value.GetHex());
    }

    [Fact]
    public void ContextOpacitySelectionPickingEditAndNodeMove()
    {
        var scene = new Scene();
        using var panels = new ThreePanelsService(scene);
        var nodes = new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0), [3] = new(10, 0, 0), [8] = new(0, 10, 0)
        };
        var data = new Dictionary<int, DisplayPanel> { [88] = new(2, [1, 3, 8]) };
        panels.ReplaceAll(nodes, data);
        panels.SetMode(true, 0.3f);
        panels.Select(88);
        Assert.Null(panels.SelectedPanelId);
        Assert.Equal(0.3f, panels.Opacity);

        panels.SetMode(true, 0.7f);
        panels.Select(88);
        Assert.Equal(88, panels.SelectedPanelId);
        Assert.Equal(88, panels.Pick(new Raycaster(new Vector3(2, 2, 10), new Vector3(0, 0, -1))));

        nodes[3] = new Vector3(20, 0, 0);
        panels.ReplaceAll(nodes, data);
        Assert.Null(panels.SelectedPanelId);
        var mesh = Assert.IsType<Mesh>(Assert.Single(Assert.Single(scene.Children).Children));
        var points = Assert.IsType<BufferAttribute<float>>(Assert.IsType<BufferGeometry>(mesh.Geometry).GetAttribute<float>("position"));
        Assert.Contains(20f, points.Array);

        panels.UpdatePanel(88, null, nodes);
        Assert.Equal(0, panels.PanelCount);
    }

    [Fact]
    public void ParserAcceptsSparseIdsAndRejectsInvalidReplacement()
    {
        using var valid = JsonDocument.Parse("""{"shell":{"19":{"e":2,"nodes":[7,42,81,99]}}}""");
        var parsed = InputPanelService.ParsePanelJson(valid.RootElement);
        Assert.Equal("2", parsed["19"].E);
        Assert.Equal("99", parsed["19"].Point4);

        using var bad = JsonDocument.Parse("""{"shell":{"19":{"e":2,"nodes":[7,42,42]}}}""");
        Assert.Throws<JsonException>(() => InputPanelService.ParsePanelJson(bad.RootElement));
    }

    [Fact]
    public void PartialRowsSurviveSaveAndReloadWithoutCreatingGeometry()
    {
        var input = InputPanelService.Instance;
        try
        {
            input.ApplyPanels(new Dictionary<string, clsPanel>
            {
                ["10"] = new() { E = "2", Point1 = "7" },
                ["20"] = new() { E = "3", Point1 = "7", Point2 = "42" },
                ["30"] = new() { E = "4", Point1 = "7", Point2 = "42", Point3 = "81" },
                ["40"] = new() { E = "5" }
            });

            var saved = input.getPanelJson();
            Assert.Contains("10", saved.Keys);
            Assert.Contains("20", saved.Keys);
            Assert.Contains("30", saved.Keys);
            Assert.DoesNotContain("40", saved.Keys);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(new { shell = saved }));
            var parsed = InputPanelService.ParsePanelJson(document.RootElement);
            input.ApplyPanels(parsed);

            Assert.Equal("7", input.Panels[9].Point1);
            Assert.Null(input.Panels[9].Point2);
            Assert.Equal("42", input.Panels[19].Point2);
            Assert.Null(input.Panels[19].Point3);
            Assert.Null(input.GetDisplayPanel(10));
            Assert.Null(input.GetDisplayPanel(20));
            Assert.Equal(new[] { 7, 42, 81 }, input.GetDisplayPanel(30)?.Nodes);
            Assert.Equal(new[] { 30 }, input.GetDisplayPanels().Keys);
        }
        finally
        {
            input.clear();
        }
    }

    [Fact]
    public void MissingReferenceInvalidInputAndDisposeDoNotLeaveGeometry()
    {
        var scene = new Scene();
        var panels = new ThreePanelsService(scene);
        var nodes = new Dictionary<int, Vector3> { [1] = new(0, 0, 0), [2] = new(1, 0, 0) };
        panels.ReplaceAll(nodes, new Dictionary<int, DisplayPanel> { [5] = new(1, [1, 2, 9]) });
        Assert.Equal(0, panels.PanelCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => panels.ReplaceAll(nodes,
            new Dictionary<int, DisplayPanel> { [5] = new(1, [1, 2, 2]) }));
        Assert.Equal(0, panels.PanelCount);
        panels.Dispose();
        panels.Dispose();
        Assert.Empty(scene.Children);
        Assert.Throws<ObjectDisposedException>(() => panels.SetMode(true, 0.7f));
    }

    private static int PositionCount(Mesh mesh)
    {
        var geometry = Assert.IsType<BufferGeometry>(mesh.Geometry);
        var positions = Assert.IsType<BufferAttribute<float>>(geometry.GetAttribute<float>("position"));
        return positions.Array.Length;
    }
}

using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.three;
using SingleFormsDemo;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ThreeCoordinatorTests
{
    [Fact]
    public void FileReplacementBeforeAndAfterViewportInitializationClearsOldMarkers()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        var nodes = InputNodesService.Instance;
        try
        {
            routing.NotifyInputMode("node");
            Open("""{"node":{"1":{"x":1},"9":{"z":3}}}""");
            using var coordinator = new ThreeService(new SceneService());
            coordinator.FlushPending();
            Assert.Equal(2, coordinator.NodeCount);
            coordinator.SelectNode(9);
            Assert.Equal(9, coordinator.SelectedNodeId);

            Open("""{"node":{"5":{"y":2}}}""");
            coordinator.FlushPending();
            Assert.Equal(1, coordinator.NodeCount);
            Assert.Null(coordinator.SelectedNodeId);

            Open("{}");
            coordinator.FlushPending();
            Assert.Equal(0, coordinator.NodeCount);
        }
        finally
        {
            routing.NotifyInputMode("member");
            nodes.clear();
        }

        void Open(string json)
        {
            using var document = JsonDocument.Parse(json);
            input.JsonDataOpen(document.RootElement);
        }
    }

    [Fact]
    public void EditsCoalesceAndFailedReplacementPreservesCurrentScene()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        var nodes = InputNodesService.Instance;
        try
        {
            routing.NotifyInputMode("node");
            using var coordinator = new ThreeService(new SceneService());
            using (var document = JsonDocument.Parse("""{"node":{"7":{"x":1}}}"""))
                input.JsonDataOpen(document.RootElement);
            coordinator.FlushPending();
            Assert.Equal(1, coordinator.NodeCount);

            nodes.Nodes[6].X = 2;
            nodes.Nodes[6].Y = 3;
            nodes.Nodes[6].X = 4;
            nodes.Nodes[6].Y = null;
            nodes.Nodes[6].X = null;
            coordinator.FlushPending();
            Assert.Equal(0, coordinator.NodeCount);

            using (var valid = JsonDocument.Parse("""{"node":{"8":{"z":6}}}"""))
                input.JsonDataOpen(valid.RootElement);
            coordinator.FlushPending();
            coordinator.SelectNode(8);
            using (var invalid = JsonDocument.Parse("""{"node":{"0":{"x":1}}}"""))
                Assert.Throws<JsonException>(() => input.JsonDataOpen(invalid.RootElement));
            coordinator.FlushPending();
            Assert.Equal(1, coordinator.NodeCount);
            Assert.Equal(8, coordinator.SelectedNodeId);

            routing.NotifyInputMode("member");
            coordinator.FlushPending();
            Assert.Null(coordinator.SelectedNodeId);
            Assert.Equal(1, coordinator.NodeCount);
        }
        finally
        {
            routing.NotifyInputMode("member");
            nodes.clear();
        }
    }
}

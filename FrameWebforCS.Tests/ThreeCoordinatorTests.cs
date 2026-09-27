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
    public void ViewportScaleControlFollowsModeAndAppliesOnFlush()
    {
        var routing = AppRoutingModule.Instance;
        try
        {
            routing.NotifyInputMode("member");
            using var viewport = new ThreeService(new SceneService());
            viewport.FlushPending();
            Assert.Equal("member", viewport.GetScaleControl()?.Kind);
            viewport.QueueScale("member", 200);
            viewport.FlushPending();
            Assert.Equal(200f, viewport.GetScaleControl()?.Value);

            routing.NotifyInputMode("fix_node");
            viewport.FlushPending();
            Assert.Equal("fix_node", viewport.GetScaleControl()?.Kind);
            viewport.QueueScale("fix_node", 10);
            viewport.FlushPending();
            Assert.Equal(10f, viewport.GetScaleControl()?.Value);

            routing.NotifyInputMode("basedisg");
            viewport.FlushPending();
            Assert.Equal("disg", viewport.GetScaleControl()?.Kind);
        }
        finally { routing.NotifyInputMode("node"); }
    }

    [Fact]
    public void ResultPageResetsToNewDocumentCaseAfterReplacement()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        try
        {
            routing.NotifyInputMode("basedisg");
            using var viewport = new ThreeService(new SceneService());
            using (var first = JsonDocument.Parse("""
                {"node":{"1":{"x":0},"2":{"x":1}},
                 "member":{"1":{"ni":"1","nj":"2"}},
                 "result":{"Case5":{"disg":{"1":{"dx":0.1},"2":{"dx":0.2}}}}}
                """)) input.JsonDataOpen(first.RootElement);
            viewport.FlushPending();
            Assert.Equal("Case5", viewport.ResultCase);
            Assert.True(viewport.DisplacementCount > 0);

            using (var second = JsonDocument.Parse("""
                {"node":{"3":{"x":0},"4":{"x":2}},
                 "member":{"2":{"ni":"3","nj":"4"}},
                 "result":{"Case9":{"disg":{"3":{"dy":0.1},"4":{"dy":0.2}}}}}
                """)) input.JsonDataOpen(second.RootElement);
            viewport.FlushPending();
            Assert.Equal("Case9", viewport.ResultCase);
            Assert.True(viewport.DisplacementCount > 0);
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
            routing.NotifyInputMode("node");
        }
    }

    [Fact]
    public void FileReplacementDropsPendingGridSelectionFromPreviousDocument()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        try
        {
            routing.NotifyInputMode("shell");
            using var viewport = new ThreeService(new SceneService());
            using (var first = JsonDocument.Parse("""
                {"node":{"1":{"x":0},"2":{"x":1},"3":{"y":1}},
                 "shell":{"5":{"e":1,"nodes":[1,2,3]}}}
                """)) input.JsonDataOpen(first.RootElement);
            viewport.FlushPending();
            viewport.QueueSelection("shell", 5, null);
            using (var second = JsonDocument.Parse("""
                {"node":{"4":{"x":0},"6":{"x":2},"7":{"y":2}},
                 "shell":{"5":{"e":2,"nodes":[4,6,7]}}}
                """)) input.JsonDataOpen(second.RootElement);
            viewport.FlushPending();
            Assert.Null(viewport.SelectedPanelId);
            Assert.Null(viewport.SelectedKind);
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
            routing.NotifyInputMode("node");
        }
    }

    [Fact]
    public void NonNodeLayersFollowTopologyModeAndSuccessfulReplacement()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        var fixNode = InputFixNodeService.Instance;
        string previousFixNodeCase = fixNode.SelectedCaseId;
        try
        {
            fixNode.SelectCase("1");
            routing.NotifyInputMode("member");
            using var viewport = new ThreeService(new SceneService());
            using (var first = JsonDocument.Parse("""
                {"node":{"1":{"x":0},"2":{"x":10},"3":{"y":10}},
                 "member":{"4":{"ni":"1","nj":"2","e":"1"}},
                 "shell":{"5":{"e":1,"nodes":[1,2,3]}},
                 "fix_node":{"1":[{"row":1,"n":"1","tx":1}]},
                 "load":{"1":{"load_node":[{"row":1,"n":"2","tx":3}]}},
                 "result":{"Case1":{"disg":{"1":{"dx":0.1},"2":{"dx":0.2}},
                                    "reac":{"1":{"tx":4}}}}}
                """))
                input.JsonDataOpen(first.RootElement);
            viewport.FlushPending();
            Assert.Equal(3, viewport.NodeCount);
            Assert.Equal(1, viewport.MemberCount);
            Assert.Equal(1, viewport.PanelCount);
            Assert.True(viewport.ConstraintCount("fix_node") > 0,
                $"Active fix_node sheet={InputFixNodeService.Instance.SelectedCaseId}; " +
                $"rows={InputFixNodeService.Instance.GetDisplaySnapshot(InputFixNodeService.Instance.SelectedCaseId).Count}");
            Assert.True(viewport.LoadGlyphCount > 0);

            InputNodesService.Instance.Nodes[1].X = 11;
            viewport.FlushPending();
            Assert.Equal(1, viewport.MemberCount);
            Assert.Equal(1, viewport.PanelCount);

            routing.NotifyInputMode("basedisg");
            viewport.FlushPending();
            Assert.True(viewport.DisplacementCount > 0);
            routing.NotifyInputMode("disg");
            viewport.FlushPending();
            Assert.True(viewport.DisplacementCount > 0);
            routing.NotifyInputMode("reac");
            viewport.FlushPending();
            Assert.True(viewport.ReactionCount > 0);

            using (var replacement = JsonDocument.Parse("{}"))
                input.JsonDataOpen(replacement.RootElement);
            viewport.FlushPending();
            Assert.Equal(0, viewport.NodeCount);
            Assert.Equal(0, viewport.MemberCount);
            Assert.Equal(0, viewport.PanelCount);
            Assert.Equal(0, viewport.LoadGlyphCount);
            Assert.Equal(0, viewport.DisplacementCount);
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
            routing.NotifyInputMode("node");
            fixNode.SelectCase(previousFixNodeCase);
        }
    }

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

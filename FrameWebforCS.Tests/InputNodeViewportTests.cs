using FrameWebforCS.components.input;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputNodeViewportTests
{
    [Fact]
    public void PartialCoordinatesAreProjectedWithoutChangingStoredValues()
    {
        var nodes = InputNodesService.Instance;
        try
        {
            using var document = JsonDocument.Parse("""{"node":{"7":{"x":2.5},"42":{"z":-3}}}""");
            nodes.setNodeJson(document.RootElement);

            var snapshot = nodes.getNodeJson(0);
            Assert.Equal(2, snapshot.Count);
            Assert.Equal((2.5f, 0f, 0f), (snapshot["7"].X, snapshot["7"].Y, snapshot["7"].Z));
            Assert.Equal((0f, 0f, -3f), (snapshot["42"].X, snapshot["42"].Y, snapshot["42"].Z));
            Assert.Null(nodes.getNodeJson()["7"].Y);
            Assert.Null(nodes.Nodes[6].Y);
            Assert.Null(nodes.Nodes[41].X);
        }
        finally { nodes.clear(); }
    }

    [Fact]
    public void MissingNodeSectionClearsRowsAndInvalidNodeLeavesThemUnchanged()
    {
        var nodes = InputNodesService.Instance;
        try
        {
            using var first = JsonDocument.Parse("""{"node":{"11":{"y":4}}}""");
            nodes.setNodeJson(first.RootElement);
            using var invalid = JsonDocument.Parse("""{"node":{"0":{"x":1}}}""");
            Assert.Throws<JsonException>(() => nodes.setNodeJson(invalid.RootElement));
            Assert.Single(nodes.getNodeJson(0));

            using var empty = JsonDocument.Parse("{}");
            nodes.setNodeJson(empty.RootElement);
            Assert.Empty(nodes.getNodeJson(0));
        }
        finally { nodes.clear(); }
    }

    [Fact]
    public void GridCoordinateEditsPublishStableIdsForAddMoveAndDelete()
    {
        var nodes = InputNodesService.Instance;
        var edited = new List<int>();
        nodes.NodeEdited += OnEdited;
        try
        {
            nodes.clear();
            nodes.Nodes[22].X = 1;
            nodes.Nodes[22].Y = 2;
            nodes.Nodes[22].X = null;
            nodes.Nodes[22].Y = null;
            Assert.Equal(new[] { 23, 23, 23, 23 }, edited);
            Assert.Empty(nodes.getNodeJson(0));
        }
        finally
        {
            nodes.NodeEdited -= OnEdited;
            nodes.clear();
        }

        void OnEdited(int id) => edited.Add(id);
    }
}

using FrameWebforCS.three;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class ThreeNodesServiceTests
{
    [Fact]
    public void BaseScaleMatchesLegacyMinimumAndMaximumDistanceFormula()
    {
        var scene = new Scene();
        using var nodes = new ThreeNodesService(scene);
        nodes.ReplaceAll(new Dictionary<int, Vector3>());
        Assert.Equal(1f, nodes.BaseScale);

        nodes.ReplaceAll(new Dictionary<int, Vector3> { [1] = new(0, 0, 0) });
        Assert.Equal(1f, nodes.BaseScale);

        nodes.ReplaceAll(new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0),
            [2] = new(0, 0, 0), // JS ignores this zero-distance pair.
            [3] = new(10, 0, 0)
        });
        Assert.Equal(0.2f, nodes.BaseScale, 5);
        Assert.Equal(0.2f, MatrixScale(scene, 0), 5);

        nodes.ReplaceAll(new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0),
            [2] = new(1, 0, 0),
            [3] = new(100_000, 0, 0)
        });
        Assert.Equal(200f, nodes.BaseScale, 5); // No old 100-unit clamp.
    }

    [Fact]
    public void EditAddDeleteAndBatchRecalculateTheLegacyScale()
    {
        var scene = new Scene();
        using var nodes = new ThreeNodesService(scene);
        nodes.SetNodeMode(true);
        nodes.ReplaceAll(new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0),
            [2] = new(10, 0, 0)
        });
        nodes.Select(1);
        Assert.Equal(0.2f, nodes.BaseScale, 5);

        nodes.UpdateNode(2, new Vector3(100, 0, 0));
        Assert.Equal(2f, nodes.BaseScale, 5);
        Assert.Equal(2.6f, scene.Children[0].Children[1].Scale.X, 5);

        nodes.UpdateNode(3, new Vector3(101, 0, 0), deferScale: true);
        nodes.UpdateNode(2, null, deferScale: true);
        Assert.Equal(2f, nodes.BaseScale, 5);
        nodes.FinishNodeUpdates();
        Assert.Equal(2.02f, nodes.BaseScale, 5);
        Assert.Equal(2.02f, MatrixScale(scene, 0), 5);
    }

    [Fact]
    public void ReplaceAllUsesStableIdsAndClearsPreviousSelection()
    {
        var scene = new Scene();
        using var nodes = new ThreeNodesService(scene);
        nodes.SetNodeMode(true);
        nodes.ReplaceAll(new Dictionary<int, Vector3>
        {
            [42] = new(1, 2, 3),
            [7] = new(4, 5, 6)
        });
        nodes.Select(42);
        Assert.Equal(2, nodes.NodeCount);
        Assert.Equal(42, nodes.SelectedNodeId);
        Assert.Single(scene.Children);
        Assert.Equal(2, scene.Children[0].Children.Count);

        nodes.ReplaceAll(new Dictionary<int, Vector3> { [99] = new(0, 0, 0) });
        Assert.Equal(1, nodes.NodeCount);
        Assert.Null(nodes.SelectedNodeId);
        nodes.Select(42);
        Assert.Null(nodes.SelectedNodeId);
        nodes.Select(99);
        Assert.Equal(99, nodes.SelectedNodeId);

        nodes.ReplaceAll(new Dictionary<int, Vector3>());
        Assert.Equal(0, nodes.NodeCount);
        Assert.Null(nodes.SelectedNodeId);
    }

    [Fact]
    public void UpdateMovesAddsAndDeletesWithoutDuplicateInstances()
    {
        var scene = new Scene();
        using var nodes = new ThreeNodesService(scene);
        nodes.SetNodeMode(true);
        nodes.ReplaceAll(new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0),
            [2] = new(10, 0, 0),
            [3] = new(20, 0, 0)
        });
        nodes.UpdateNode(2, new Vector3(11, 12, 13));
        nodes.UpdateNode(2, new Vector3(14, 15, 16));
        Assert.Equal(3, nodes.NodeCount);
        Assert.Equal((14f, 15f, 16f), MatrixPosition(scene, 1));

        nodes.Select(3);
        nodes.UpdateNode(1, null); // swap the last instance into the removed slot
        Assert.Equal(2, nodes.NodeCount);
        Assert.Equal((20f, 0f, 0f), MatrixPosition(scene, 0));
        Assert.Equal(3, nodes.SelectedNodeId);
        nodes.UpdateNode(3, null);
        Assert.Null(nodes.SelectedNodeId);
        nodes.UpdateNode(10, new Vector3(30, 0, 0));
        Assert.Equal(2, nodes.NodeCount);
    }

    [Fact]
    public void MarkersStayVisibleWhileSelectionRequiresNodeMode()
    {
        var scene = new Scene();
        using var nodes = new ThreeNodesService(scene);
        nodes.ReplaceAll(new Dictionary<int, Vector3> { [17] = new(1, 0, 0) });
        var root = scene.Children[0];
        var marker = Assert.IsType<InstancedMesh>(root.Children[0]);
        var selected = Assert.IsType<Mesh>(root.Children[1]);

        nodes.Select(17);
        Assert.Null(nodes.SelectedNodeId);
        nodes.SetNodeMode(true);
        nodes.Select(17);
        Assert.Equal(17, nodes.SelectedNodeId);
        Assert.True(selected.Visible);
        nodes.SetNodeMode(false);
        Assert.True(root.Visible);
        Assert.Equal(1, marker.InstanceCount);
        Assert.False(selected.Visible);
        Assert.Null(nodes.SelectedNodeId);
    }

    [Fact]
    public void PickReturnsStableIdOnlyInNodeMode()
    {
        var scene = new Scene();
        using var nodes = new ThreeNodesService(scene);
        nodes.ReplaceAll(new Dictionary<int, Vector3>
        {
            [42] = new(0, 0, 0),
            [7] = new(0, 0, -3)
        });
        var ray = new Raycaster(new Vector3(0, 0, 10), new Vector3(0, 0, -1));
        Assert.Null(nodes.Pick(ray));
        nodes.SetNodeMode(true);
        Assert.Equal(42, nodes.Pick(ray));
        nodes.UpdateNode(42, null);
        Assert.Equal(7, nodes.Pick(ray));
    }

    [Fact]
    public void InvalidReplacementKeepsExistingMarkers()
    {
        var scene = new Scene();
        using var nodes = new ThreeNodesService(scene);
        nodes.ReplaceAll(new Dictionary<int, Vector3> { [1] = new(3, 4, 5) });
        Assert.Throws<ArgumentOutOfRangeException>(() => nodes.ReplaceAll(
            new Dictionary<int, Vector3> { [2] = new(float.NaN, 0, 0) }));
        Assert.Equal(1, nodes.NodeCount);
        Assert.Equal((3f, 4f, 5f), MatrixPosition(scene, 0));
    }

    [Fact]
    public void DisposeDetachesRootAndRejectsFurtherChanges()
    {
        var scene = new Scene();
        var nodes = new ThreeNodesService(scene);
        nodes.ReplaceAll(new Dictionary<int, Vector3> { [1] = new(0, 0, 0) });
        nodes.Dispose();
        nodes.Dispose();
        Assert.Empty(scene.Children);
        Assert.Throws<ObjectDisposedException>(() => nodes.UpdateNode(2, new Vector3()));
    }

    private static (float X, float Y, float Z) MatrixPosition(Scene scene, int index)
    {
        var markers = Assert.IsType<InstancedMesh>(scene.Children[0].Children[0]);
        var matrix = markers.InstanceMatrix.Array;
        var offset = index * 16;
        return (matrix[offset + 12], matrix[offset + 13], matrix[offset + 14]);
    }

    private static float MatrixScale(Scene scene, int index)
    {
        var markers = Assert.IsType<InstancedMesh>(scene.Children[0].Children[0]);
        return markers.InstanceMatrix.Array[index * 16];
    }
}

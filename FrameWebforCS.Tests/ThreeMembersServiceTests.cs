using FrameWebforCS.components.input;
using FrameWebforCS.three;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class ThreeMembersServiceTests
{
    [Fact]
    public void SparseIdsProduceMidpointLengthAndLegacyRadius()
    {
        var scene = new Scene();
        using var members = new ThreeMembersService(scene);
        members.ReplaceAll(new Dictionary<int, Vector3>
        {
            [7] = new(0, 0, 0), [42] = new(10, 0, 0)
        }, new Dictionary<int, DisplayMember>
        {
            [91] = new(7, 42, 3, 0)
        }, nodeBaseScale: 0.2f);

        Assert.Equal(1, members.MemberCount);
        var mesh = Assert.IsType<Mesh>(Assert.Single(Assert.Single(scene.Children).Children));
        Assert.Equal("member91", mesh.Name);
        Assert.Equal(5f, mesh.Position.X);
        Assert.Equal(10f, Assert.IsType<CylinderBufferGeometry>(mesh.Geometry).Height);
        Assert.Equal(0.06f, mesh.Scale.X, 5);
        Assert.Equal(0.06f, mesh.Scale.Z, 5);
    }

    [Fact]
    public void MissingEndpointsAndShortMembersAreSkippedAndNodeMoveRebuilds()
    {
        var scene = new Scene();
        using var members = new ThreeMembersService(scene);
        var data = new Dictionary<int, DisplayMember>
        {
            [10] = new(2, 8, 1, 0), [11] = new(2, 9, 1, 0)
        };
        members.ReplaceAll(new Dictionary<int, Vector3> { [2] = new(0, 0, 0), [8] = new(0.0005f, 0, 0) }, data, 1);
        Assert.Equal(0, members.MemberCount);

        members.ReplaceAll(new Dictionary<int, Vector3> { [2] = new(0, 0, 0), [8] = new(10, 0, 0) }, data, 1);
        Assert.Equal(1, members.MemberCount);
        members.ReplaceAll(new Dictionary<int, Vector3> { [2] = new(5, 0, 0), [8] = new(10, 0, 0) }, data, 1);
        var mesh = Assert.IsType<Mesh>(Assert.Single(Assert.Single(scene.Children).Children));
        Assert.Equal(7.5f, mesh.Position.X);
        Assert.Equal(5f, Assert.IsType<CylinderBufferGeometry>(mesh.Geometry).Height);
    }

    [Fact]
    public void EditSelectPickAndModeClearUseStableMemberId()
    {
        var scene = new Scene();
        using var members = new ThreeMembersService(scene);
        var nodes = new Dictionary<int, Vector3> { [1] = new(0, 0, 0), [2] = new(10, 0, 0) };
        members.ReplaceAll(nodes, new Dictionary<int, DisplayMember> { [77] = new(1, 2, 5, 0) }, 1);
        members.SetMode(true, true, true);
        members.Select(77);
        Assert.Equal(77, members.SelectedMemberId);
        Assert.True(members.LabelsVisible);
        Assert.Equal(77, members.Pick(new Raycaster(new Vector3(5, 0, 10), new Vector3(0, 0, -1))));

        members.UpdateMember(77, new DisplayMember(1, 2, 5, 0), nodes, 2);
        Assert.Equal(1, members.MemberCount);
        members.UpdateMember(77, null, nodes, 2);
        Assert.Equal(0, members.MemberCount);
        Assert.Null(members.SelectedMemberId);
        members.SetMode(true, false, false);
        Assert.Null(members.Pick(new Raycaster(new Vector3(5, 0, 10), new Vector3(0, 0, -1))));
    }

    [Fact]
    public void SelectionShowsLocalAxisArrowsAndLeavingEditModeHidesThem()
    {
        var scene = new Scene();
        using var members = new ThreeMembersService(scene);
        members.ReplaceAll(new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0), [2] = new(10, 0, 0)
        }, new Dictionary<int, DisplayMember>
        {
            [7] = new(1, 2, 4, 90)
        }, 0.2f);

        members.SetMode(true, true, true);
        members.Select(7);
        var root = Assert.Single(scene.Children);
        var axes = Assert.Single(root.Children.OfType<Group>());
        Assert.Equal("member7axis", axes.Name);
        Assert.Equal(["x", "y", "z"], axes.Children.Select(child => child.Name));
        Assert.All(axes.Children, arrow => Assert.IsType<ArrowHelper>(arrow));

        members.SetMode(true, true, false);
        Assert.DoesNotContain(root.Children, child => child.Name == "member7axis");
    }

    [Fact]
    public void MemberScaleChangesCylinderRadiusWithoutMovingEndpoints()
    {
        var scene = new Scene();
        using var members = new ThreeMembersService(scene);
        members.ReplaceAll(new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0), [2] = new(10, 0, 0)
        }, new Dictionary<int, DisplayMember> { [7] = new(1, 2, 1, 0) }, 0.2f);
        var mesh = Assert.IsType<Mesh>(Assert.Single(Assert.Single(scene.Children).Children));

        members.SetMemberScale(200);

        Assert.Equal(0.12f, mesh.Scale.X, 5);
        Assert.Equal(10f, Assert.IsType<CylinderBufferGeometry>(mesh.Geometry).Height);
        Assert.Equal(5f, mesh.Position.X);
        Assert.Throws<ArgumentOutOfRangeException>(() => members.SetMemberScale(1001));
    }

    [Fact]
    public void RelatedMemberHighlightWorksOutsideMemberEditMode()
    {
        var scene = new Scene();
        using var members = new ThreeMembersService(scene);
        members.ReplaceAll(new Dictionary<int, Vector3>
        {
            [1] = new(0, 0, 0), [2] = new(10, 0, 0)
        }, new Dictionary<int, DisplayMember> { [7] = new(1, 2, 1, 0) }, 1);
        members.SetMode(true, true, false);
        var mesh = Assert.IsType<Mesh>(Assert.Single(Assert.Single(scene.Children).Children));

        members.HighlightRelated(7);
        Assert.Equal(0xFF0000, mesh.Material.Color!.Value.GetHex());
        members.HighlightRelated(null);
        Assert.Equal(0x000000, mesh.Material.Color!.Value.GetHex());
    }

    [Fact]
    public void DisposeRemovesRootAndRejectsChanges()
    {
        var scene = new Scene();
        var members = new ThreeMembersService(scene);
        members.Dispose();
        members.Dispose();
        Assert.Empty(scene.Children);
        Assert.Throws<ObjectDisposedException>(() => members.SetMode(true, true, true));
    }
}

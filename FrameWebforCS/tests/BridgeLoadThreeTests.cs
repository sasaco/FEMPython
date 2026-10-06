using FrameWebforCS.components.input;
using FrameWebforCS.three;
using FrameWebforCS.calculation;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class BridgeLoadThreeTests
{
    private static BridgePanel Panel() => new(7,"deck",[1,2,3,4],[],[[1,2,3],[1,3,4]],[],null,1e-9,1e-9,[],[]);
    private static Dictionary<int,Vector3> Nodes() => new()
    { [1]=new(0,0,0),[2]=new(10,0,0),[3]=new(10,4,0),[4]=new(0,4,0) };
    private static BridgeLoadSnapshot Snapshot() => new([Panel()],
        [new(1,"first",[new(0,1,0),new(10,1,0)]),new(2,"second",[new(0,3,0),new(10,3,0)])],
        new Dictionary<string,IReadOnlyList<BridgeLoad>> {
            ["1"]=[new(10,7,[1,2],[[-2,-4],[-6,-8]],new("global",new(0,0,1)),"area")],
            ["2"]=[new(20,7,[1],[[-5,-5]],new("global",new(0,0,1)),"line")]
        },[]);

    [Fact]
    public void CaseSelectionUsesStableIdsAndOnlyThreeDimensionsCreateGeometry()
    {
        var scene=new Scene();using var view=new ThreeBridgeLoadsService(scene);
        view.ReplaceAll(Snapshot(),Nodes(),new Dictionary<int,DisplayPanel>(),3,"1");
        view.SetView(true,true,true,false);
        Assert.Equal(1,view.VisibleLoadCount);Assert.Empty(view.Errors);
        Assert.Contains(view.GetVisibleLabels(),x=>x.Text.Contains("kN/m²"));
        var hit=view.Pick(new Raycaster(new Vector3(5,2,10),new Vector3(0,0,-1)),"load");
        Assert.Equal(new BridgeSelection("load",10,"1"),hit);
        view.SetCase("2");
        Assert.Equal(1,view.VisibleLoadCount);
        Assert.DoesNotContain(view.GetVisibleLabels(),x=>x.Text.Contains("kN/m²"));
        view.ReplaceAll(Snapshot(),Nodes(),new Dictionary<int,DisplayPanel>(),2,"1");
        Assert.Equal(0,view.VisibleLoadCount);Assert.Empty(Assert.Single(scene.Children).Children);
    }

    [Fact]
    public void ReversedSecondPathSwapsEndpointValuesAndUsesNormalizedArcLength()
    {
        var first=new BridgePath(1,"",[new(0,0,0),new(2,0,0),new(10,0,0)]);
        var second=new BridgePath(2,"",[new(10,4,0),new(0,4,0)]);
        var load=new BridgeLoad(1,7,[1,2],[[10,20],[40,30]],new("global",null),"");
        var cells=BridgeLoadGeometry.BuildStrips(first,second,load);
        Assert.Equal(2,cells.Count);Assert.Equal(.2,cells[0].S1,12);
        Assert.Equal(new BridgePoint(2,4,0),cells[0].Corners[2]);
        Assert.Equal(30,cells[0].Value(0,1));Assert.Equal(40,cells[1].Value(1,1));
        Assert.Equal(20,cells[0].Value(0,.5));
    }

    [Fact]
    public void ClippingPreservesActualTransferCellsOnInclinedPlane()
    {
        BridgePoint[] subject=[new(-1,-1,-1),new(3,-1,3),new(3,3,3),new(-1,3,-1)];
        BridgePoint[] cell=[new(0,0,0),new(2,0,2),new(0,2,0)];
        var polygon=BridgeLoadGeometry.ClipToTriangle(subject,cell);
        Assert.True(polygon.Length>=3);
        Assert.All(polygon,p=>Assert.True(BridgeLoadGeometry.Contains(cell,p)));
        Assert.False(BridgeLoadGeometry.Contains(cell,new(1,1,2)));
    }

    [Fact]
    public void MissingReferencesProduceVisibleErrorAndDoNotLeaveOldLoads()
    {
        var scene=new Scene();using var view=new ThreeBridgeLoadsService(scene);
        var nodes=Nodes();view.ReplaceAll(Snapshot(),nodes,new Dictionary<int,DisplayPanel>(),3,"1");
        view.SetView(true,true,true,false);nodes.Remove(3);
        view.ReplaceAll(Snapshot(),nodes,new Dictionary<int,DisplayPanel>(),3,"1");
        Assert.Equal(0,view.LoadCount);Assert.NotEmpty(view.Errors);
        Assert.Contains(view.GetVisibleLabels(),x=>x.Text.StartsWith("入力エラー"));
    }

    [Fact]
    public void EquivalentForcesComeOnlyFromAuditAndDisposalRemovesOwnedObjects()
    {
        var scene=new Scene();var view=new ThreeBridgeLoadsService(scene);
        var z=new Vector3Value(0,0,0);
        var audit=new SpatialLoadAudit(new(0,0,-10),z,new(0,0,-10),z,0,0,
            [new("2",new(0,0,-10))],[]);
        view.ReplaceAll(Snapshot(),Nodes(),new Dictionary<int,DisplayPanel>(),3,"1",audit);
        view.SetView(true,false,true,true);Assert.Equal(1,view.VisibleLoadCount);
        Assert.Contains(view.GetVisibleLabels(),x=>x.Text.Contains("節点 2"));
        view.SetCase("2");Assert.Equal(0,view.LoadCount);
        Assert.Contains(view.Errors,x=>x.Contains("解析完了後"));
        view.Dispose();view.Dispose();Assert.Empty(scene.Children);
    }

    [Fact]
    public void PlanFrameFollowsInclinedTransferPlaneAndSelectionRetainsIdentity()
    {
        var nodes=Nodes();foreach(var p in nodes.Values)p.Z=p.X;
        var s=Snapshot();
        s=s with {Panels=[Panel() with {Plane=new(new(0,0,0),new(Math.Sqrt(.5),0,Math.Sqrt(.5)),new(0,1,0))}]};
        var paths=s.Paths.Select(p=>p with {Points=p.Points.Select(v=>v with{Z=v.X}).ToArray()}).ToArray();
        using var view=new ThreeBridgeLoadsService(new Scene());
        view.ReplaceAll(s with{Paths=paths},nodes,new Dictionary<int,DisplayPanel>(),3,"1");
        view.SetView(true,false,false,false);view.Select(new("load",10,"1"));
        Assert.Equal(new BridgeSelection("load",10,"1"),view.Selection);
        Assert.Empty(view.GetVisibleLabels());
        var frame=view.ViewFrame();Assert.True(frame.Normal.X<0);Assert.True(frame.Normal.Z>0);
        Assert.Equal(frame.Center.X,frame.Center.Z,5);
    }

    [Fact]
    public void NormalDirectionUsesStructuralWindingWithReversedLoadingMesh()
    {
        var panel=Panel() with {
            LoadingNodes=[new(101,new(0,0,0)),new(102,new(10,0,0)),new(103,new(10,4,0)),new(104,new(0,4,0))],
            LoadingTriangles=[[101,103,102],[101,104,103]]
        };
        var original=Snapshot();
        var snapshot=original with {Panels=[panel], Cases=new Dictionary<string,IReadOnlyList<BridgeLoad>> {
            ["1"]=[original.Cases["1"][0] with {Direction=new("normal",null)}]
        }};
        var scene=new Scene();using var view=new ThreeBridgeLoadsService(scene);
        view.ReplaceAll(snapshot,Nodes(),new Dictionary<int,DisplayPanel>(),3,"1");
        view.SetView(true,true,true,false);
        Assert.Empty(view.Errors);
        var arrows=Assert.Single(scene.Children).Children.OfType<ArrowHelper>()
            .Where(item=>item.Name.StartsWith("bridge-load-")).ToArray();
        Assert.NotEmpty(arrows);
        Assert.All(arrows,arrow=>Assert.True(arrow.Position.Z>0));
    }

    [Theory]
    [InlineData(0,1)]
    [InlineData(5,0)]
    public void InvalidAreaDoesNotLeaveRenderableLoadGeometry(double xOffset,double zOffset)
    {
        var source=Snapshot();
        var paths=source.Paths.Select(p=>p with {Points=p.Points.Select(v=>new BridgePoint(v.X+xOffset,v.Y,v.Z+zOffset)).ToArray()}).ToArray();
        var scene=new Scene();using var view=new ThreeBridgeLoadsService(scene);
        view.ReplaceAll(source with {Paths=paths},Nodes(),new Dictionary<int,DisplayPanel>(),3,"1");
        view.SetView(true,true,true,false);
        Assert.Equal(0,view.VisibleLoadCount);Assert.NotEmpty(view.Errors);
        Assert.DoesNotContain(Assert.Single(scene.Children).Children,item=>item.Name.StartsWith("bridge-load-"));
    }
}

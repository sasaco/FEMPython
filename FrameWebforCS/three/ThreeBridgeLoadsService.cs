using FrameWebforCS.components.input;
using FrameWebforCS.calculation;
using System.Globalization;
using THREE;
using Color = THREE.Color;
using static FrameWebforCS.three.BridgeLoadGeometry;

namespace FrameWebforCS.three;

/// <summary>Owns only bridge loading geometry; it never adds structural stiffness or input forces.</summary>
internal sealed class ThreeBridgeLoadsService : IDisposable
{
    private readonly Scene _scene;
    private readonly Group _root = new() { Name="bridge-loads", Visible=false };
    private readonly List<(Object3D Item, BridgeSelection Selection, int Color, string Layer)> _items=[];
    private readonly List<ViewportTextLabel> _labels=[];
    private readonly List<string> _errors=[];
    private bool _disposed;
    private bool _visible;
    private int _dimension=3;
    private bool _showMesh=true, _showLabels=true, _equivalent;
    private readonly Dictionary<int,BridgePoint[]> _panelTriangles=[];
    private IReadOnlyDictionary<int,Vector3> _nodes=new Dictionary<int,Vector3>();
    private BridgeLoadSnapshot _snapshot=new([],[],new Dictionary<string,IReadOnlyList<BridgeLoad>>(),[]);
    private IReadOnlyDictionary<int,DisplayPanel> _shells=new Dictionary<int,DisplayPanel>();
    private SpatialLoadAudit? _audit;
    private double _scale=1;

    internal ThreeBridgeLoadsService(Scene scene) { _scene=scene; scene.Add(_root); }
    internal string? CaseId { get; private set; }
    internal BridgeSelection? Selection { get; private set; }
    internal int LoadCount { get; private set; }
    internal int VisibleLoadCount => _root.Visible ? LoadCount : 0;
    internal IReadOnlyList<string> Errors=>_errors;
    internal bool ShowMesh=>_showMesh;
    internal bool ShowLabels=>_showLabels;
    internal bool Equivalent=>_equivalent;
    internal IReadOnlyList<ViewportTextLabel> GetVisibleLabels()=>_root.Visible && _showLabels ? _labels : [];

    internal void ReplaceAll(BridgeLoadSnapshot snapshot,IReadOnlyDictionary<int,Vector3> nodes,
        IReadOnlyDictionary<int,DisplayPanel> shells,int dimension,string? caseId,SpatialLoadAudit? audit=null)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        _snapshot=snapshot;_nodes=nodes;_shells=shells;_dimension=dimension;CaseId=caseId;_audit=audit;
        Rebuild();
    }

    internal void SetView(bool visible,bool mesh,bool labels,bool equivalent)
    {
        bool changed=_showMesh!=mesh || _showLabels!=labels || _equivalent!=equivalent;
        _visible=visible;_showMesh=mesh;_showLabels=labels;_equivalent=equivalent;
        if(changed)Rebuild();
        _root.Visible=visible && _dimension==3;
    }

    internal void SetCase(string? caseId,SpatialLoadAudit? audit=null)
    {
        if(CaseId==caseId && ReferenceEquals(_audit,audit))return;
        CaseId=caseId;_audit=audit;Rebuild();
    }

    private void Rebuild()
    {
        Clear();_errors.AddRange(_snapshot.Errors);
        _root.Visible=_visible && _dimension==3;
        if(_dimension!=3)return;
        var points=_nodes.Values.ToArray();
        if(points.Length>0)
            _scale=Math.Max(0.05,Length(new(points.Max(p=>p.X)-points.Min(p=>p.X),
                points.Max(p=>p.Y)-points.Min(p=>p.Y),points.Max(p=>p.Z)-points.Min(p=>p.Z)))*0.08);
        var loads=CaseId!=null && _snapshot.Cases.TryGetValue(CaseId,out var found) ? found : [];
        var panels=_snapshot.Panels.ToDictionary(p=>p.Id);
        var paths=_snapshot.Paths.ToDictionary(p=>p.Id);
        var relevant=loads.Select(l=>l.PanelId).ToHashSet();
        foreach(var panel in _snapshot.Panels)
        {
            if(relevant.Count>0 && !relevant.Contains(panel.Id) && Selection?.Kind!="panel")continue;
            try
            {
                var cells=GetTriangles(panel);
                _panelTriangles[panel.Id]=cells.SelectMany(x=>x).ToArray();
                foreach(var t in cells)
                {
                    AddTriangle(t,new("panel",panel.Id),0x86A8B4,0.13f,"panel");
                    if(_showMesh)AddLine([t[0],t[1],t[2],t[0]],new("panel",panel.Id),0x779198,"mesh");
                }
                if(cells.Count>0)Label($"橋面 {panel.Id}",cells[0][0]);
            }
            catch(ArgumentException error) { _errors.Add($"橋面 {panel.Id}: {error.Message}"); }
        }
        var pathIds=loads.SelectMany(l=>l.PathIds).ToHashSet();
        foreach(var path in _snapshot.Paths)
        {
            if(pathIds.Count>0 && !pathIds.Contains(path.Id) && Selection?.Kind!="path")continue;
            try
            {
                Parameters(path.Points);
                AddLine(path.Points,new("path",path.Id),0x287E99,"path");
                if (!pathIds.Contains(path.Id) || _equivalent || Selection?.Kind == "path")
                { Label($"L{path.Id} 始",path.Points[0]);Label($"L{path.Id} 終",path.Points[^1]); }
                for(int i=0;i<path.Points.Length;i++)
                {
                    var marker=new Mesh(new SphereGeometry((float)(_scale*0.06),8,6),
                        new MeshBasicMaterial { Color=Color.Hex(0x287E99) });
                    marker.Position.Copy(Vector(path.Points[i]));
                    Add(marker,new("path",path.Id,null,i),0x287E99,"path");
                }
                var midpoint=At(path.Points,Parameters(path.Points),0.5);
                AddArrow(midpoint,Sub(path.Points[^1],path.Points[0]),_scale*.5,new("path",path.Id),0x287E99,"path");
            }
            catch(ArgumentException error) { _errors.Add($"ライン {path.Id}: {error.Message}"); }
        }
        if(_equivalent) DrawAudit();
        else foreach(var load in loads)
        {
            int itemStart = _items.Count, labelStart = _labels.Count;
            try
            {
                if(!panels.TryGetValue(load.PanelId,out var panel))throw new ArgumentException("橋面がありません。");
                var triangles=GetTriangles(panel);
                var structural = GetTriangles(panel, structural: true);
                var loadPaths = load.PathIds.Select(id => paths[id]).ToArray();
                var holeNodes = panel.LoadingNodes.Length > 0
                    ? panel.LoadingNodes.ToDictionary(node => node.Id, node => node.Point)
                    : _nodes.ToDictionary(node => node.Key, node => Point(node.Value));
                var holes = panel.Holes.Select(ring => ring.Select(id => holeNodes[id]).ToArray()).ToArray();
                var strips = BridgeLoadValidation.Validate(panel, structural, triangles, holes, loadPaths, load);
                var direction=load.Direction.Mode=="normal"
                    ? Unit(Cross(Sub(structural[0][1],structural[0][0]),Sub(structural[0][2],structural[0][0])))
                    : Unit(load.Direction.Vector ?? new(0,0,1));
                var first=loadPaths[0];
                double maximum=Math.Max(1e-100,loads.SelectMany(l=>l.EndIntensities.SelectMany(x=>x)).Select(Math.Abs).DefaultIfEmpty(1).Max());
                if(load.PathIds.Length==1)DrawLineLoad(first,load,direction,triangles,maximum);
                else DrawAreaLoad(first,paths[load.PathIds[1]],load,direction,triangles,maximum,strips);
                LoadCount++;
            }
            catch(Exception error) when(error is ArgumentException or KeyNotFoundException or InvalidOperationException)
            {
                foreach(var item in _items.Skip(itemStart)) { _root.Remove(item.Item);DisposeTree(item.Item); }
                _items.RemoveRange(itemStart,_items.Count-itemStart);
                _labels.RemoveRange(labelStart,_labels.Count-labelStart);
                _errors.Add($"荷重 {load.Id}: {error.Message}");
            }
        }
        Select(Selection);
        if(_errors.Count>0)Label("入力エラー: "+_errors[0],points.Length>0?Point(points[0]):new(0,0,0),System.Drawing.Color.Red);
    }

    private List<BridgePoint[]> GetTriangles(BridgePanel panel, bool structural = false)
    {
        var source=_nodes.ToDictionary(n=>n.Key,n=>Point(n.Value));
        int[][] indices=panel.Triangles;
        if(panel.LoadingNodes.Length>0 && !structural)
        { source=panel.LoadingNodes.ToDictionary(n=>n.Id,n=>n.Point);indices=panel.LoadingTriangles; }
        else if(panel.Elements.Length>0)
        {
            var result=new List<int[]>();
            foreach(int id in panel.Elements)
            {
                if(!_shells.TryGetValue(id,out var shell))throw new ArgumentException($"シェル {id} がありません。");
                var n=shell.Nodes;result.Add([n[0],n[1],n[2]]);
                if(n.Length==4)result.Add([n[0],n[2],n[3]]);
            }
            indices=result.ToArray();
        }
        var triangles=new List<BridgePoint[]>();
        foreach(var ids in indices)
        {
            if(ids.Length!=3 || ids.Any(n=>!source.ContainsKey(n)))throw new ArgumentException("三角形の節点参照が不正です。");
            var t=ids.Select(n=>source[n]).ToArray();Unit(Cross(Sub(t[1],t[0]),Sub(t[2],t[0])));triangles.Add(t);
        }
        if(triangles.Count==0)throw new ArgumentException("荷重伝達面の接続がありません。");
        return triangles;
    }

    private void DrawLineLoad(BridgePath path,BridgeLoad load,BridgePoint direction,List<BridgePoint[]> cells,double max)
    {
        var parameters=Parameters(path.Points);var selection=new BridgeSelection("load",load.Id,CaseId);
        var tops=new List<BridgePoint>();
        foreach(double s in parameters.Concat(Enumerable.Range(0,17).Select(i=>i/16d)).Distinct().Order())
        {
            var p=At(path.Points,parameters,s);
            if(!cells.Any(t=>Contains(t,p)))throw new ArgumentException("載荷ラインが橋面外にあります。");
            double value=load.EndIntensities[0][0]+s*(load.EndIntensities[0][1]-load.EndIntensities[0][0]);
            double length=_scale*Math.Abs(value)/max;
            var force=Scale(direction,Math.Sign(value));
            var top=Sub(p,Scale(force,length));tops.Add(top);
            if(length>0)AddArrow(top,force,length,selection,0xB14633,"load");
        }
        AddLine(tops,selection,0xB14633,"load");
        Label($"荷重 {load.Id}: {Format(load.EndIntensities[0][0])} → {Format(load.EndIntensities[0][1])} kN/m",tops[0]);
    }

    private void DrawAreaLoad(BridgePath first,BridgePath second,BridgeLoad load,BridgePoint direction,List<BridgePoint[]> cells,double max,
        IReadOnlyList<Strip> strips)
    {
        var selection=new BridgeSelection("load",load.Id,CaseId);
        foreach(var strip in strips)
        {
            foreach(var cell in cells)
            {
                var polygon=ClipToTriangle(strip.Corners,cell);
                for(int i=1;i+1<polygon.Length;i++)
                    AddTriangle([polygon[0],polygon[i],polygon[i+1]],selection,0xE4A259,.32f,"load");
            }
            for(int i=0;i<=4;i++)for(int j=0;j<=2;j++)
            {
                double u=i/4d,v=j/2d,s=strip.S0+(strip.S1-strip.S0)*u;
                var p=Lerp(Lerp(strip.Corners[0],strip.Corners[1],u),Lerp(strip.Corners[3],strip.Corners[2],u),v);
                if(!cells.Any(t=>Contains(t,p)))continue;
                double value=strip.Value(s,v),length=_scale*Math.Abs(value)/max;
                var force=Scale(direction,Math.Sign(value));
                if(length>0)AddArrow(Sub(p,Scale(force,length)),force,length,selection,0xB14633,"load");
            }
        }
        for(int i=0;i<2;i++)
        {
            var path=i==0?first:second;
            Label($"荷重 {load.Id} L{path.Id}始 {Format(load.EndIntensities[i][0])} kN/m²",path.Points[0]);
            Label($"L{path.Id}終 {Format(load.EndIntensities[i][1])} kN/m²",path.Points[^1]);
        }
    }

    private void DrawAudit()
    {
        if(_audit==null) { _errors.Add("等価節点荷重は最新の解析完了後に表示できます。");return; }
        double maximum=_audit.NodeLoads.Select(n=>Math.Sqrt(n.Force.X*n.Force.X+n.Force.Y*n.Force.Y+n.Force.Z*n.Force.Z)).DefaultIfEmpty(1).Max();
        foreach(var node in _audit.NodeLoads)
        {
            if(!int.TryParse(node.NodeId,out int id) || !_nodes.TryGetValue(id,out var position))continue;
            var force=new BridgePoint(node.Force.X,node.Force.Y,node.Force.Z);double norm=Length(force);
            if(norm<=0)continue;
            var p=Point(position);double size=_scale*norm/Math.Max(maximum,1e-100);
            AddArrow(Sub(p,Scale(Unit(force),size)),force,size,new("equivalent",id,CaseId),0x75459A,"load");
            Label($"節点 {id}: ({Format(force.X)}, {Format(force.Y)}, {Format(force.Z)}) kN",p);
            LoadCount++;
        }
    }

    internal void Select(BridgeSelection? selection)
    {
        Selection=selection;
        foreach(var item in _items)
        {
            bool selected=selection!=null && item.Selection.Kind==selection.Kind && item.Selection.Id==selection.Id &&
                (selection.CaseId==null || selection.CaseId==item.Selection.CaseId) &&
                (selection.PointIndex==null || selection.PointIndex==item.Selection.PointIndex);
            SetColor(item.Item,selected?0xDB2424:item.Color);
        }
    }

    internal BridgeSelection? Pick(Raycaster raycaster,string? preferredKind=null)
    {
        if(!_root.Visible)return null;
        _root.UpdateWorldMatrix(true,true);
        float nearest=float.PositiveInfinity;BridgeSelection? result=null;
        foreach(var item in _items)
        {
            if(item.Selection.Kind=="equivalent" || preferredKind!=null && item.Selection.Kind!=preferredKind)continue;
            foreach(var hit in raycaster.IntersectObject(item.Item,true))
                if(hit.distance<nearest) { nearest=hit.distance;result=item.Selection; }
        }
        return result;
    }

    internal (Vector3 Center,Vector3 Normal,Vector3 Up,float Radius,Vector3[] Points) ViewFrame()
    {
        var panels=_snapshot.Panels.Where(p=>Selection?.Kind!="panel" || p.Id==Selection.Id).ToArray();
        var relevant=CaseId!=null && _snapshot.Cases.TryGetValue(CaseId,out var loads)?loads.Select(l=>l.PanelId).ToHashSet():[];
        if(relevant.Count>0 && Selection?.Kind!="panel")panels=panels.Where(p=>relevant.Contains(p.Id)).ToArray();
        var panel=panels.FirstOrDefault() ?? throw new InvalidOperationException("表示する橋面がありません。");
        var cells=panels.SelectMany(panel => GetTriangles(panel)).ToArray();
        var points=cells.SelectMany(t=>t).ToArray();
        var center=Scale(points.Aggregate(new BridgePoint(0,0,0),BridgeLoadGeometry.Add),1d/points.Length);
        var normal=Unit(Cross(Sub(cells[0][1],cells[0][0]),Sub(cells[0][2],cells[0][0])));
        var up=panel.Plane?.AxisV ?? Unit(Cross(normal,Sub(cells[0][1],cells[0][0])));
        return(Vector(center),Vector(normal),Vector(Unit(up)),(float)Math.Max(.1,points.Max(p=>Length(Sub(p,center)))+_scale),points.Select(Vector).ToArray());
    }

    private void AddTriangle(BridgePoint[] p,BridgeSelection selection,int color,float opacity,string layer)
    {
        if(Length(Cross(Sub(p[1],p[0]),Sub(p[2],p[0])))<1e-18)return;
        var geometry=Geometry(p);geometry.ComputeVertexNormals();
        Add(new Mesh(geometry,new MeshBasicMaterial {Color=Color.Hex(color),Side=Constants.DoubleSide,
            Transparent=true,Opacity=opacity,DepthWrite=false}),selection,color,layer);
    }
    private void AddLine(IEnumerable<BridgePoint> points,BridgeSelection selection,int color,string layer)=>
        Add(new Line(Geometry(points),new LineBasicMaterial {Color=Color.Hex(color)}),selection,color,layer);
    private void AddArrow(BridgePoint origin,BridgePoint direction,double length,BridgeSelection selection,int color,string layer)
    {
        if(length<=0)return;
        Add(new ArrowHelper(Vector(Unit(direction)),Vector(origin),(float)length,Color.Hex(color),
            (float)Math.Min(length*.3,_scale*.18),(float)Math.Min(length*.15,_scale*.09)),selection,color,layer);
    }
    private void Add(Object3D item,BridgeSelection selection,int color,string layer)
    {
        item.Name=$"bridge-{selection.Kind}-{selection.CaseId}-{selection.Id}";
        _root.Add(item);_items.Add((item,selection,color,layer));
    }
    private static BufferGeometry Geometry(IEnumerable<BridgePoint> points)
    {
        var result=new BufferGeometry();result.SetAttribute("position",new BufferAttribute<float>(
            points.Select(Vector).SelectMany(p=>new[]{p.X,p.Y,p.Z}).ToArray(),3));return result;
    }
    private void Label(string text,BridgePoint point,System.Drawing.Color? color=null)
    { if(_labels.Count<ViewportTextLabels.MaximumCandidateLabels)_labels.Add(new(text,Vector(point),color,AvoidOverlap:true)); }
    private static string Format(double value)=>value.ToString("G5",CultureInfo.InvariantCulture);
    private static BridgePoint Point(Vector3 p)=>new(p.X,p.Y,p.Z);
    private static Vector3 Vector(BridgePoint p)
    {
        if(!float.IsFinite((float)p.X)||!float.IsFinite((float)p.Y)||!float.IsFinite((float)p.Z))throw new ArgumentException("座標が表示範囲を超えています。");
        return new((float)p.X,(float)p.Y,(float)p.Z);
    }
    private static void SetColor(Object3D item,int color)
    {
        if(item.Material!=null)item.Material.Color=Color.Hex(color);
        foreach(var child in item.Children)SetColor(child,color);
    }
    private void Clear()
    {
        foreach(var item in _root.Children.ToArray()) { _root.Remove(item);DisposeTree(item); }
        _items.Clear();_labels.Clear();_errors.Clear();_panelTriangles.Clear();LoadCount=0;
    }
    private static void DisposeTree(Object3D item)
    {
        foreach(var child in item.Children.ToArray()) { item.Remove(child);DisposeTree(child); }
        item.Geometry?.Dispose();item.Material?.Dispose();item.Dispose();
    }
    public void Dispose()
    { if(_disposed)return;Clear();_scene.Remove(_root);_root.Dispose();_disposed=true; }
}

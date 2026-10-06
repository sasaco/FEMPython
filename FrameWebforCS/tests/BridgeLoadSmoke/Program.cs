using System.Drawing.Imaging;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FrameWebforCS;
using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using FrameWebforCS.three;
using OpenTK.WinForms;
using PDF_Manager;

internal static class Program
{
    private const string Fixture = """
    {"dimension":3,"node":{"1":{"x":0,"y":0,"z":0},"2":{"x":10,"y":0,"z":0},
      "3":{"x":10,"y":4,"z":0},"4":{"x":0,"y":4,"z":0}},
     "member":{"1":{"ni":"1","nj":"2","e":"1"},"2":{"ni":"4","nj":"3","e":"1"}},
     "element":{"1":{"1":{"E":200000000,"G":80000000,"A":1,"J":1,"Iy":1,"Iz":1}}},
     "fix_node":{"1":[{"row":1,"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1},
                         {"row":2,"n":"4","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]},
     "load":{"1":{"name":"橋面の面荷重"},"2":{"name":"橋面の線荷重"},
             "3":{"name":"通常荷重","load_node":[{"row":1,"n":"2","tz":-7}]}},
     "bridge_loads":{"version":1,
       "panels":[{"id":7,"name":"橋面A","nodes":[1,2,3,4],"triangles":[[1,2,3],[1,3,4]]}],
       "paths":[{"id":1,"name":"左側","points":[[0,1,0],[10,1,0]]},
                {"id":2,"name":"右側","points":[[0,3,0],[10,3,0]]}],
       "cases":{"1":[{"id":1,"panel_id":7,"path_ids":[1,2],"end_intensities":[[-2,-2],[-2,-2]]}],
                "2":[{"id":1,"panel_id":7,"path_ids":[1],"end_intensities":[[-3,-3]]}]}}}
    """;

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string root=FindRoot(), output=args.FirstOrDefault() ?? Path.Combine(root,".tmp","bridge-deck-smoke");
        Directory.CreateDirectory(output);
        Exception? failure=null;
        using var owner=new Form { Text="Bridge loads acceptance",Location=new(-20000,-20000),
            StartPosition=FormStartPosition.Manual,ClientSize=new(1200,800),ShowInTaskbar=false };
        using var gl=new GLControl { Dock=DockStyle.Fill };
        owner.Controls.Add(gl);
        using var viewport=new ThreeComponent(gl);
        owner.Shown+=(_,_)=>owner.BeginInvoke(async () =>
        {
            try { await Run(root,output,owner,viewport); }
            catch(Exception error) { failure=error; }
            finally { owner.Close(); }
        });
        Application.Run(owner);
        if(failure!=null) { Console.Error.WriteLine(failure);return 1; }
        Console.WriteLine("PASS: desktop save/reload -> pythonnet cases -> audit -> live GL plan/iso -> print snapshot; 2D gated.");
        return 0;
    }

    private static async Task Run(string root,string output,Form owner,ThreeComponent viewport)
    {
        var input=InputDataService.Instance;
        using(var document=JsonDocument.Parse(Fixture))input.JsonDataOpen(document.RootElement);
        string saved=JsonSerializer.Serialize(input.GetSaveJson());
        File.WriteAllText(Path.Combine(output,"input.json"),saved);
        using(var document=JsonDocument.Parse(saved))input.JsonDataOpen(document.RootElement);
        var service=(ThreeService)typeof(ThreeComponent).GetField("_threeService",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(viewport)!;
        AppRoutingModule.Instance.NotifyInputMode("bridge_load");
        InputLoadService.Instance.SelectCase("1");
        service.FlushPending();
        var before=service.CapturePrintState();
        var images=viewport.CapturePrintDiagrams([
            new(0,PrintOption.BridgeLoadDiagram,"print_bridge_load","1","bridge_load","面荷重 平面図",View:"plan"),
            new(1,PrintOption.BridgeLoadDiagram,"print_bridge_load","1","bridge_load","面荷重 立体図",View:"iso"),
            new(2,PrintOption.BridgeLoadDiagram,"print_bridge_load","2","bridge_load","線荷重 立体図",View:"iso")]);
        for(int i=0;i<images.Count;i++)File.WriteAllBytes(Path.Combine(output,$"load-{i}.png"),images[i].PngBytes);
        var after=service.CapturePrintState();
        Require(before.BridgeCase==after.BridgeCase && before.VisibleMode==after.VisibleMode,"print mode restoration");
        Require(before.CameraState!.Position.Equals(after.CameraState!.Position),"print camera restoration");

        await using(var runtime=new PythonCalculationRuntime(root))
        {
            var request=input.CreateCalculationRequest();
            string json=await runtime.CalculateAsync(request.Json).WaitAsync(TimeSpan.FromSeconds(60));
            File.WriteAllText(Path.Combine(output,"results.json"),json);
            var results=AnalysisResultSetJson.Deserialize(Encoding.UTF8.GetBytes(json));
            var expected=new Dictionary<string,double>{{"1",40},{"2",30},{"3",7}};
            foreach(var result in results.Results.Cast<StaticAnalysisResult>())
            {
                Require(Math.Abs(result.SupportReactions.Sum(n=>n.Components.Fz)-expected[result.CaseId])<1e-7,"case force equilibrium "+result.CaseId);
                Require((result.Diagnostics.SpatialLoads!=null)==(result.CaseId!="3"),"audit case isolation "+result.CaseId);
            }
            var presentation = new CalculationResultPresentation(results,dimension:3);
            CalculationResultStore.Instance.Commit(presentation);
            service.FlushPending();
            InputBridgeLoadService.Instance.ShowEquivalentLoads=true;service.FlushPending();
            Require(service.BridgeLoadCount>0,"equivalent nodal glyphs");
            InputBridgeLoadService.Instance.ShowEquivalentLoads=false;
            var choice = new PrintSelection([PrintOption.BridgeDefinitions,PrintOption.BridgeLoads,
                PrintOption.BridgeLoadDiagram,PrintOption.BridgeAudit], InputCaseIds:["1","2"],
                BridgeDiagramViews:["plan","iso"], Title:"橋面荷重 3D 通し確認");
            var requests = PrintProjection.MakeDiagramRequests(choice,presentation,JsonNode.Parse(saved)!.AsObject());
            var printImages = viewport.CapturePrintDiagrams(requests);
            var snapshot = new PrintSnapshot(choice,saved,input.DocumentRevision,input.CalculationInputRevision,
                presentation,null,requests,input.CalculationInputRevision);
            File.WriteAllBytes(Path.Combine(output,"bridge-load-complete.pdf"),
                DirectPdfGenerator.Generate(PrintProjection.Build(snapshot,printImages)));
        }

        using(var dialog=new Form { ClientSize=new(1250,520),Location=owner.Location,ShowInTaskbar=false })
        using(var editor=new InputBridgeLoadComponent { Dock=DockStyle.Fill })
        {
            dialog.Controls.Add(editor);dialog.Show(owner);Application.DoEvents();
            var spread=(FarPoint.Win.Spread.FpSpread)typeof(InputBridgeLoadComponent)
                .GetField("_spread",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(editor)!;
            for(int i=0;i<3;i++)
            {
                spread.ActiveSheetIndex=i;Application.DoEvents();
                using var bitmap=new Bitmap(dialog.ClientSize.Width,dialog.ClientSize.Height);
                dialog.DrawToBitmap(bitmap,dialog.ClientRectangle);
                bitmap.Save(Path.Combine(output,$"input-tab-{i}.png"),ImageFormat.Png);
            }
            dialog.Close();
        }
        // Empty 2D models never display retained bridge layers.
        using(var empty=JsonDocument.Parse("{\"dimension\":2}"))input.JsonDataOpen(empty.RootElement);
        service.FlushPending();Require(service.BridgeLoadCount==0,"2D bridge visibility");
        try
        {
            service.ApplyPrintView(new(0,PrintOption.BridgeLoadDiagram,"print_bridge_load","1","bridge_load","invalid"));
            throw new Exception("2D bridge print unexpectedly accepted");
        }
        catch(InvalidOperationException error) when(error.Message.Contains("3D")) { }
    }

    private static void Require(bool condition,string label)
    { if(!condition)throw new InvalidOperationException("Acceptance failed: "+label); }
    private static string FindRoot()
    {
        for(var directory=new DirectoryInfo(AppContext.BaseDirectory);directory!=null;directory=directory.Parent)
            if(File.Exists(Path.Combine(directory.FullName,"FrameWeb.sln")))return directory.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }
}

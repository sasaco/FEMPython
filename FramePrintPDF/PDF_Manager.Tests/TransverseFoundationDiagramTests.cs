using Newtonsoft.Json.Linq;
using PDF_Manager;
using PDF_Manager.Printing;
using System.Reflection;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class TransverseFoundationDiagramTests
{
    [Theory]
    [InlineData("base")]
    [InlineData("combine")]
    [InlineData("pickup")]
    [InlineData("moving")]
    public void DrawingRowsIgnoreOnlyMarkedDisplayClonesAndKeepTableRowsAndEnvelopeSides(string route)
    {
        JArray rows = JArray.Parse("""
            [{"m":"1","n":"1","l":0,"mz":1},
             {"m":"1","n":"S1","l":0.4,"mz":12,"occurrence":1},
             {"m":"1","n":"S1","l":0.4,"mz":12,"occurrence":2},
             {"m":"1","n":"S1","l":0.4,"mz":99},
             {"m":"1","n":"S1","l":0.4,"mz":77},
             {"m":"1","n":"2","l":1,"mz":3}]
            """);
        JArray minimum = (JArray)rows.DeepClone();
        foreach (JObject row in minimum) row["mz"] = -(double)row["mz"]!;
        var modes = new JObject { ["mz_max"] = rows, ["mz_min"] = minimum };
        string key = route == "combine" ? "fsecCombine" : route == "pickup" ? "fsecPickup" : "fsec";
        var root = new Dictionary<string, object>
        {
            ["dimension"] = 2, ["language"] = "en", ["pageSize"] = "A4", ["pageOrientation"] = "Horizontal",
            [key] = new JObject { ["1"] = route == "base" ? rows.DeepClone() : modes },
            [key + "Name"] = JArray.Parse("[[\"1\",\"Test\"]]"),
        };
        var data = new PrintData(root);
        object parsed = data.printDatas[key];
        List<Fsec> tableRows;
        if (route == "base")
            tableRows = (List<Fsec>)((Dictionary<string, object>)parsed.GetType().GetField("fsecs")!.GetValue(parsed)!)["1"];
        else
        {
            FsecCombine envelope;
            if (route == "moving")
            {
                var moving = (Dictionary<string, object>)((Dictionary<string, object>)parsed.GetType()
                    .GetField("fsecs")!.GetValue(parsed)!)["1"];
                Type parser = typeof(Fsec).Assembly.GetType("PDF_Manager.Printing.ResultFsecCombine")!;
                envelope = (FsecCombine)parser.GetMethod("getFsecCombine", BindingFlags.Static | BindingFlags.Public)!
                    .Invoke(null, [moving])!;
            }
            else envelope = ((Dictionary<string, FsecCombine>)parsed.GetType().GetField("Fsecs")!.GetValue(parsed)!)["1"];
            tableRows = envelope.mz_min.Concat(envelope.mz_max).ToList();
        }
        Fsec[] before = tableRows.ToArray();
        Type renderer = typeof(Fsec).Assembly.GetType("PDF_Manager.Printing.DiagramResult")!;
        MethodInfo? boundary = renderer.GetMethod("DrawingRows", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(boundary);
        var drawing = Assert.IsType<List<Fsec>>(boundary.Invoke(null, [tableRows]));
        int sides = route == "base" ? 1 : 2;
        Assert.Equal(6 * sides, tableRows.Count);
        Assert.Equal(5 * sides, drawing.Count);
        Assert.Equal(before, tableRows);
        Assert.Single(drawing.Where(row => row.mz == 12));
        Assert.Single(drawing.Where(row => row.mz == 99));
        Assert.Single(drawing.Where(row => row.mz == 77));
        if (route != "base")
        {
            Assert.Single(drawing.Where(row => row.mz == -12));
            Assert.Single(drawing.Where(row => row.mz == -99));
        }
        Assert.All(drawing, row => Assert.Contains(row, before));
    }
}

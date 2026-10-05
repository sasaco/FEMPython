using Newtonsoft.Json.Linq;
using PDF_Manager;
using PDF_Manager.Printing;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class DisplacementNodeIdLayoutTests
{
    private const string LongId = "generated:11:S9";

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void BasicDisplacementPreservesLongIdInsideItsColumn(int dimension)
    {
        var root = Root(dimension);
        root["disg"] = JObject.Parse("""
            {"1":[{"id":"generated:11:S9","dx":0.1,"dy":0.2,"dz":0.3,"rx":0.4,"ry":0.5,"rz":0.6}]}
            """);
        root["disgName"] = JArray.Parse("""[["1","DL"]]""");
        var data = new PrintData(root);
        int page = 1;
        using var document = new PdfDocument(data, ref page);
        var (table, panels) = new InspectDisg(root).TableFor(document, data);
        Assert.Equal(LongId, table[4, 0]);
        Assert.Equal(dimension == 2 ? 4 : 7, table.Columns);
        Assert.Equal(1, panels);
        AssertFits(document, table, LongId);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void PickupDisplacementPreservesLongIdInsideItsColumn(int dimension)
    {
        var root = Root(dimension);
        root["disgPickup"] = JObject.Parse("""
            {"1":{"dx_max":{"generated:11:S9":{"dx":0.1,"dy":0.2,"dz":0.3,"rx":0.4,"ry":0.5,"rz":0.6,"case":"1"}}}}
            """);
        root["disgPickupName"] = JArray.Parse("""[["1","PICKUP"]]""");
        var data = new PrintData(root);
        int page = 1;
        using var document = new PdfDocument(data, ref page);
        var table = new InspectPickup(root).TableFor(document, data);
        Assert.Equal(LongId, table[5, 0]);
        AssertFits(document, table, LongId);
    }

    [Fact]
    public void ShortNumericIdsKeepThePairedTwoDimensionalLayout()
    {
        var root = Root(2);
        root["disg"] = JObject.Parse("""{"1":[{"id":"1","dx":0,"dy":0,"rz":0}]}""");
        root["disgName"] = JArray.Parse("""[["1","DL"]]""");
        var data = new PrintData(root);
        int page = 1;
        using var document = new PdfDocument(data, ref page);
        var (table, panels) = new InspectDisg(root).TableFor(document, data);
        Assert.Equal(8, table.Columns);
        Assert.Equal(2, panels);
    }

    private static void AssertFits(PdfDocument document, Table table, string id)
    {
        Assert.True(table.ColWidth[0] >= document.MeasureString(id).Width + 5);
        Assert.True(table.GetTableWidth() <= document.currentPageSize.Width + 0.001);
    }

    private static Dictionary<string, object> Root(int dimension) => new()
    {
        ["dimension"] = dimension, ["language"] = "ja", ["pageSize"] = "A4", ["pageOrientation"] = "Vertical",
    };

    private sealed class InspectDisg(Dictionary<string, object> root) : ResultDisg(root)
    {
        internal (Table Table, int Panels) TableFor(PdfDocument document, PrintData data)
        {
            PrintInit(document, data, out _, out _, out var panels);
            return (Assert.Single(GetTables(document, data, 1)), panels.Length);
        }
    }

    private sealed class InspectPickup(Dictionary<string, object> root) : ResultDisgCombine(root, "disgPickup")
    {
        internal Table TableFor(PdfDocument document, PrintData data)
        {
            PrintInit(document, data, out _, out _, out _);
            return Assert.Single(Assert.Single(GetContexts()).GetTables());
        }
    }
}

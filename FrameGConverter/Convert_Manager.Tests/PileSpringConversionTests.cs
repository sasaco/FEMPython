using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Convert_Manager.Tests;

public class PileSpringConversionTests
{
    private static readonly double[] IntervalLengths = [2.861, 5.499, 2.2, 0.7, 2.1, 2.5, 3.6, 3.1, 0.99, 0.4];

    [Fact]
    public void PileArchiveConvertsEveryIntervalForAllThreeTypes()
    {
        var converter = ConvertArchive("バネ連衡あり.frd");
        var json = JObject.Parse(converter.getJsonString());
        var piles = PileMembers(json);
        Assert.Equal(4, piles.Length);
        var springs = Assert.IsType<JObject>(json["fix_member"]);
        Assert.Equal(new[] { "1", "2", "3" }, springs.Properties().Select(p => p.Name));

        double[][] transverse =
        [
            [2220, 2220, 4187, 27532, 27532, 46564, 44787, 29816, 105688, 105688],
            [4441, 4441, 8374, 55064, 55064, 93127, 89575, 59632, 211376, 211376],
            [4441, 4441, 8374, 55064, 55064, 93127, 89575, 59632, 211376, 211376]
        ];
        double[][] axial =
        [
            [0, 0, 829, 5454, 5454, 9224, 8872, 5906, 20936, 0],
            [880, 880, 1659, 10908, 10908, 18447, 17744, 11812, 41871, 0],
            [0, 880, 1659, 10908, 10908, 18447, 17744, 11812, 41871, 0]
        ];

        for (int type = 1; type <= 3; type++)
        {
            var rows = Assert.IsType<JArray>(springs[type.ToString()]);
            Assert.Equal(40, rows.Count);
            foreach (var pile in piles)
            {
                var intervals = rows.Where(r => r.Value<string>("m") == pile.Name).ToArray();
                Assert.Equal(10, intervals.Length);
                for (int index = 0; index < intervals.Length; index++)
                {
                    Assert.Equal(IntervalLengths[index], intervals[index].Value<double>("length"), 8);
                    Assert.Equal(transverse[type - 1][index], intervals[index].Value<double>("ty"));
                    Assert.Equal(axial[type - 1][index], intervals[index].Value<double>("tx"));
                }
            }
            Assert.Equal(rows.Count, rows.Select(r => r.Value<int>("row")).Distinct().Count());
        }
        Assert.Equal("1", json["load"]!["1"]!.Value<string>("fix_member"));
        Assert.Empty(converter._member.message);
    }

    [Fact]
    public void PileArchiveAppliesTipSupportToJEndForEachType()
    {
        var json = JObject.Parse(ConvertArchive("バネ連衡あり.frd").getJsonString());
        var piles = PileMembers(json);
        Assert.Equal(4, piles.Length);
        for (int type = 1; type <= 3; type++)
        {
            var supports = Assert.IsType<JArray>(json["fix_node"]![type.ToString()]);
            foreach (var pile in piles)
            {
                var support = Assert.Single(supports, r => r.Value<string>("n") == pile.Value.Value<string>("nj"));
                Assert.Equal(type == 1 ? 210719 : 421438, support.Value<double>("ty"));
                Assert.Equal(0, support.Value<double>("tx"));
                Assert.Equal(0, support.Value<double>("rz"));
            }
            Assert.Equal(supports.Count, supports.Select(r => r.Value<int>("row")).Distinct().Count());
        }
    }

    [Fact]
    public void PileArchiveWithoutSpringFilesDoesNotAddSprings()
    {
        var converter = ConvertArchive("バネ連衡なし.frd");
        Assert.DoesNotContain("$4.txt", converter.wdata.Keys);
        Assert.DoesNotContain("$5.txt", converter.wdata.Keys);
        Assert.DoesNotContain("$6.txt", converter.wdata.Keys);
        var json = JObject.Parse(converter.getJsonString());
        var piles = PileMembers(json).Select(p => p.Name).ToHashSet();
        Assert.Equal(4, piles.Count);
        var springs = Assert.IsType<JObject>(json["fix_member"]);
        Assert.Empty(springs.Properties());
        Assert.Empty(converter._member.message);
    }

    [Fact]
    public void NonPileArchiveKeepsLegacySpringSerialization()
    {
        var converter = ConvertArchive("4_2-FrameG.frd");
        var json = JObject.Parse(converter.getJsonString());
        Assert.Empty(PileMembers(json));
        Assert.NotEmpty(Assert.IsType<JObject>(json["member"]).Properties());
        Assert.NotEmpty(Assert.IsType<JObject>(json["load"]).Properties());
        Assert.All(json["fix_member"]!.Children<JProperty>().SelectMany(p => p.Value), row => Assert.Null(row["length"]));
        Assert.Empty(converter._member.message);
    }

    private static ConvertManager ConvertArchive(string filename)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", filename));
        return new ConvertManager(stream);
    }

    private static JProperty[] PileMembers(JObject json)
    {
        var elements = Assert.IsType<JObject>(json["element"]!["1"]);
        return Assert.IsType<JObject>(json["member"]).Properties()
            .Where(p => elements[p.Value.Value<string>("e")!]!.Value<string>("name")!.Contains('杭'))
            .ToArray();
    }
}

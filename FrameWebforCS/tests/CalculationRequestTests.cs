using FrameWebforCS.calculation;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class CalculationRequestTests
{
    // Captured by executing the Angular 15 InputDataService.getInputJson(0) with
    // real node/member/element/fix-node/load services and empty optional sections.
    [Fact]
    public void CompleteTwoDimensionalRequestMatchesAngularFixture()
    {
        const string saved = """
            {"dimension":2,
             "node":{"1":{"x":0,"y":0,"z":null},"2":{"x":1,"y":0,"z":null},"9":{"x":9,"y":0,"z":null}},
             "member":{"1":{"ni":"1","nj":"2","e":"1","cg":null}},
             "element":{"1":{"1":{"E":200000,"G":null,"Xp":null,"A":1,"J":null,"Iy":null,"Iz":1,"n":""}}},
             "fix_node":{"1":[{"row":1,"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]},
             "load":{"1":{"symbol":"DL","name":"Dead","fix_node":1,"fix_member":1,"element":1,"joint":1,"LL_pitch":0.1,
                 "load_node":[{"row":1,"n":"-2","tx":12,"ty":null,"tz":null,"rx":null,"ry":null,"rz":5}]}},
             "three":{"camera":{"x":1}},"result":{"old":{}},"define":{"1":{}}}
            """;
        const string angular = """
            {"ver":"2.5.12","node":{"1":{"x":0,"y":0,"z":0},"2":{"x":1,"y":0,"z":0}},
             "fix_node":{"1":[{"row":1,"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1},
                 {"n":"2","tz":1,"rx":1,"ry":1}]},
             "member":{"1":{"ni":1,"nj":2,"e":1,"cg":0}},
             "element":{"1":{"1":{"E":200000,"G":1,"Xp":0,"A":1,"J":1,"Iy":1,"Iz":1,"n":""}}},
             "rigid":[],
             "load":{"1":{"fix_node":1,"fix_member":1,"element":1,"joint":1,"symbol":"DL","LL_pitch":0.1,
                 "load_node":[{"n":"2","dx":0.012,"dy":0,"dz":0,"ax":0,"ay":0,"az":0.005,
                     "row":1,"tz":0,"rx":0,"ry":0}]}},"dimension":2}
            """;
        var actual = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved).Json);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(angular), actual), actual?.ToJsonString());
    }

    // Expected load rows captured from the real Angular InputLoadService.getLoadJson(0),
    // with member lengths of 1 m. The parent LL case has no effective load at L1=0.
    [Fact]
    public void MemberRangesAndMovingChildrenMatchAngular()
    {
        string saved = BaseSnapshot("""
            "1":{"symbol":"DL","fix_node":1,"fix_member":1,"element":1,"joint":1,"LL_pitch":0.5,
              "load_member":[{"row":1,"m1":"1","m2":"2","direction":"y","mark":"2","L1":"0.2","L2":"0.2","P1":10,"P2":20}]},
            "2":{"symbol":"LL","fix_node":1,"fix_member":1,"element":1,"joint":1,"LL_pitch":0.5,
              "load_member":[{"row":1,"m1":"1","m2":"2","direction":"y","mark":"1","L1":"0","L2":"0","P1":10,"P2":0}]}
            """);
        string original = saved;
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(saved).Json);
        Assert.Equal(original, saved);
        var root = result.RootElement;
        Assert.False(root.TryGetProperty("three", out _));
        Assert.False(root.TryGetProperty("result", out _));
        Assert.False(root.TryGetProperty("define", out _));
        Assert.Equal(new[] { "1", "2.1", "2.2" }, root.GetProperty("load").EnumerateObject().Select(item => item.Name));
        var first = root.GetProperty("load").GetProperty("1").GetProperty("load_member");
        Assert.Equal(new[] { 1, 2 }, first.EnumerateArray().Select(item => item.GetProperty("m").GetInt32()));
        Assert.All(first.EnumerateArray(), item =>
        {
            Assert.Equal(0.2, item.GetProperty("L1").GetDouble(), 3);
            Assert.Equal(0.2, item.GetProperty("L2").GetDouble(), 3);
            Assert.Equal(10, item.GetProperty("P1").GetDouble());
            Assert.Equal(20, item.GetProperty("P2").GetDouble());
        });
        Assert.All(root.GetProperty("load").GetProperty("2.1").GetProperty("load_member").EnumerateArray(),
            item => Assert.Equal(0.5, item.GetProperty("L1").GetDouble(), 3));
        Assert.All(root.GetProperty("load").GetProperty("2.2").GetProperty("load_member").EnumerateArray(),
            item => Assert.Equal(1, item.GetProperty("L1").GetDouble()));
    }

    [Fact]
    public void NegativeNodeBecomesPrescribedDisplacementAndTwoDimensionalFieldsAreCompleted()
    {
        string saved = BaseSnapshot("""
            "1":{"symbol":"DL","fix_node":1,"element":1,
              "load_node":[{"row":1,"n":"-2","tx":12,"rz":5}]}
            """, 2);
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(saved).Json);
        var root = result.RootElement;
        var load = root.GetProperty("load").GetProperty("1").GetProperty("load_node")[0];
        Assert.Equal("2", load.GetProperty("n").GetString());
        Assert.Equal(0.012, load.GetProperty("dx").GetDouble(), 5);
        Assert.Equal(0.005, load.GetProperty("az").GetDouble(), 5);
        Assert.False(load.TryGetProperty("tx", out _));
        Assert.Equal(0, root.GetProperty("node").GetProperty("2").GetProperty("z").GetDouble());
        Assert.Equal(1, root.GetProperty("fix_node").GetProperty("1")[1].GetProperty("tz").GetInt32());
    }

    [Theory]
    [InlineData("-0.2", "-0.3", 0, 0.9, 16.67)]
    [InlineData("0.2", "-0.3", 0.2, 0.5, 10)]
    [InlineData("-0.2", "0.3", 0, 0.7, 14)]
    public void RelativeMemberPositionsMatchAngular(string l1, string l2,
        double expectedL1, double expectedL2, double expectedP1)
    {
        string saved = BaseSnapshot($$"""
            "1":{"symbol":"DL","fix_node":1,"element":1,
              "load_member":[{"row":1,"m1":"1","m2":"1","direction":"y","mark":"2",
              "L1":"{{l1}}","L2":"{{l2}}","P1":10,"P2":20}]}
            """);
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(saved).Json);
        var row = result.RootElement.GetProperty("load").GetProperty("1").GetProperty("load_member")[0];
        Assert.Equal(expectedL1, row.GetProperty("L1").GetDouble(), 3);
        Assert.Equal(expectedL2, row.GetProperty("L2").GetDouble(), 3);
        Assert.Equal(expectedP1, row.GetProperty("P1").GetDouble(), 2);
    }

    [Fact]
    public void MoreThan256CasesHaveNoCountCeiling()
    {
        var cases = Enumerable.Range(1, 257).Select(id =>
            $"\"{id}\":{{\"fix_node\":1,\"element\":1,\"load_node\":[{{\"row\":1,\"n\":\"2\",\"tx\":1}}]}}");
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(BaseSnapshot(string.Join(',', cases))).Json);
        Assert.Equal(257, result.RootElement.GetProperty("load").EnumerateObject().Count());
    }

    [Fact]
    public void SweepRequiresExactLlSymbol()
    {
        string saved = BaseSnapshot("""
            "1":{"symbol":"XLL","fix_node":1,"element":1,"LL_pitch":0.5,
              "load_member":[{"row":1,"m1":"1","m2":"-2","direction":"y","mark":"1","L1":"0.2","L2":"0.3","P1":10,"P2":20}]}
            """);
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(saved).Json);
        var loads = result.RootElement.GetProperty("load");
        Assert.Equal(new[] { "1" }, loads.EnumerateObject().Select(item => item.Name));
        var members = loads.GetProperty("1").GetProperty("load_member");
        Assert.Single(members.EnumerateArray());
        Assert.Equal(1, members[0].GetProperty("m").GetInt32());
    }

    [Fact]
    public void RelativeRangeClipsFirstMemberOnlyLikeAngular()
    {
        string saved = BaseSnapshot("""
            "1":{"symbol":"DL","fix_node":1,"element":1,
              "load_member":[{"row":1,"m1":"1","m2":"2","direction":"y","mark":"2",
              "L1":"-0.2","L2":"-0.3","P1":10,"P2":20}]}
            """);
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(saved).Json);
        var rows = result.RootElement.GetProperty("load").GetProperty("1").GetProperty("load_member");
        Assert.Equal(2, rows.GetArrayLength());
        Assert.Equal(16.67, rows[0].GetProperty("P1").GetDouble(), 2);
        Assert.Equal(10, rows[1].GetProperty("P1").GetDouble());
        Assert.All(rows.EnumerateArray(), row => Assert.Equal(0.9, row.GetProperty("L2").GetDouble(), 2));
    }

    [Fact]
    public void ContiguousRelativeMemberRowsUsePreviousDistributedLoadEnd()
    {
        string saved = BaseSnapshot("""
            "1":{"symbol":"DL","fix_node":1,"element":1,
              "load_member":[
                {"row":1,"m1":"1","m2":"1","direction":"y","mark":"2","L1":"0.2","L2":"0.3","P1":10,"P2":20},
                {"row":2,"m1":"1","m2":"1","direction":"y","mark":"2","L1":"-0.1","L2":"-0.2","P1":10,"P2":20}]}
            """);
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(saved).Json);
        var rows = result.RootElement.GetProperty("load").GetProperty("1").GetProperty("load_member");
        Assert.Equal(0.2, rows[0].GetProperty("L1").GetDouble(), 3);
        Assert.Equal(0.3, rows[0].GetProperty("L2").GetDouble(), 3);
        Assert.Equal(0.8, rows[1].GetProperty("L1").GetDouble(), 3);
        Assert.Equal(0, rows[1].GetProperty("L2").GetDouble());
    }

    [Fact]
    public void InvalidMemberReferenceReportsTheCase()
    {
        string saved = BaseSnapshot("""
            "1":{"fix_node":1,"element":1,"load_member":[{"row":1,"m1":"9","direction":"y","mark":"1","L1":"0.2","P1":1}]}
            """);
        var error = Assert.Throws<CalculationRequestException>(() => CalculationRequestBuilder.FromSavedJson(saved));
        Assert.Contains("load.1: member 9", error.Message);
    }

    private static string BaseSnapshot(string cases, int dimension = 3) =>
        """ 
        {"dimension":
        """ + dimension + """
        ,"node":{"1":{"x":0,"y":0,"z":0},"2":{"x":1,"y":0,"z":0},"3":{"x":2,"y":0,"z":0},"9":{"x":9,"y":0,"z":0}},
         "member":{"1":{"ni":"1","nj":"2","e":"1"},"2":{"ni":"2","nj":"3","e":"1"}},
         "element":{"1":{"1":{"E":200000,"A":1,"Iz":1}}},
         "fix_node":{"1":[{"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]},
         "load":{
        """ + cases + """
        },"three":{"camera":{"x":1}},"result":{"old":{}},"define":{"1":{}}}
        """;
}

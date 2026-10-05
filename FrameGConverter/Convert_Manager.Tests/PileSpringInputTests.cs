using System.Globalization;
using System.Reflection;
using System.Text;
using Convert_Manager.FrameWebForJS;
using Xunit;

namespace Convert_Manager.Tests;

public class PileSpringInputTests
{
    [Fact]
    public void FourTypesPreserveZeroIntervalsAndMapTransverseAndAxialColumns()
    {
        var input = ReadFixture();
        input["$4.txt"] = "2\n1\t1\n2\t22.95\n";
        input["$5.txt"] = "2\n1\t0\t0\t20\t30\t40\t50\t60\t70\n2\t80\t90\t100\t110\t120\t130\t140\t150\n";
        input.Remove("$6.txt");
        var model = new SpringModel(input);

        model.Apply();

        Assert.Equal(new[] { "1", "2", "3", "4" }, model.Springs.GetFixMember().Keys);
        foreach (var sheet in model.Springs.GetFixMember())
        {
            Assert.Equal(8, sheet.Value.Count);
            int type = int.Parse(sheet.Key, CultureInfo.InvariantCulture);
            foreach (string id in new[] { "20", "21", "22", "23" })
            {
                var intervals = sheet.Value.Where(r => r.m == id).ToArray();
                Assert.Equal(2, intervals.Length);
                Assert.Equal(1, intervals[0].length);
                Assert.Equal(22.95, intervals[1].length);
                Assert.Equal(type == 1 ? 0 : 20 * (type - 1), intervals[0].ty);
                Assert.Equal(type == 1 ? 0 : 20 * (type - 1) + 10, intervals[0].tx);
                Assert.Equal(80 + 20 * (type - 1), intervals[1].ty);
                Assert.Equal(90 + 20 * (type - 1), intervals[1].tx);
            }
        }
    }

    [Fact]
    public void ExistingSpringsAndSupportsArePreservedAndPileValuesAreAdded()
    {
        var model = new SpringModel(ReadFixture());
        var nonPileSpring = new FixMember { row = 1, m = "1", tx = 7, ty = 11 };
        model.Springs.GetFixMember().Add("1", [nonPileSpring, new FixMember { row = 2, m = "20", tx = 13, ty = 17 }]);
        var nonPileSupport = new FixNode { row = 1, n = "1", tx = 1, ty = 23 };
        model.Supports.GetFixNode()["1"] = [nonPileSupport, new FixNode { row = 2, n = "18", tx = 1, ty = 29, rz = 31 }];

        model.Apply();

        Assert.Same(nonPileSpring, Assert.Single(model.Springs.GetFixMember()["1"], r => r.m == "1"));
        Assert.Null(nonPileSpring.length);
        var firstPileInterval = model.Springs.GetFixMember()["1"].First(r => r.m == "20");
        Assert.Equal(2237, firstPileInterval.ty);
        Assert.Equal(13, firstPileInterval.tx);
        Assert.Same(nonPileSupport, Assert.Single(model.Supports.GetFixNode()["1"], r => r.n == "1"));
        var tip = Assert.Single(model.Supports.GetFixNode()["1"], r => r.n == "18");
        Assert.Equal(1, tip.tx);
        Assert.Equal(210748, tip.ty);
        Assert.Equal(31, tip.rz);
    }

    [Fact]
    public void PileTipSupportCanBeSpecifiedWithoutDistributedSprings()
    {
        var input = ReadFixture();
        input.Remove("$4.txt");
        input.Remove("$5.txt");
        input["$6.txt"] = "2\r\n1\t5\t7\t9\r\n4\t11\t13\t15\r\n";
        var model = new SpringModel(input);

        model.Apply();

        Assert.Empty(model.Springs.GetFixMember());
        Assert.All(model.Supports.GetFixNode()["1"], r =>
        {
            Assert.Equal(5, r.tx);
            Assert.Equal(7, r.ty);
            Assert.Equal(9, r.rz);
        });
        Assert.Equal(new[] { "18", "19", "20", "21" }, model.Supports.GetFixNode()["4"].Select(r => r.n));
        Assert.All(model.Supports.GetFixNode()["4"], r =>
        {
            Assert.Equal(11, r.tx);
            Assert.Equal(13, r.ty);
            Assert.Equal(15, r.rz);
        });
    }

    [Theory]
    [InlineData("$4.txt", null)]
    [InlineData("$5.txt", null)]
    [InlineData("$4.txt", "2\n1\t1\n")]
    [InlineData("$4.txt", "10\n1\t-1\n2\t5.499\n3\t2.2\n4\t0.7\n5\t2.1\n6\t2.5\n7\t3.6\n8\t3.1\n9\t0.99\n10\t0.4\n")]
    [InlineData("$5.txt", "1\n1\t2\t3\n")]
    [InlineData("$6.txt", "1\n1\tNaN\t0\t0\n")]
    [InlineData("$6.txt", "1\n1\t2\t3\n")]
    public void MalformedSpringInputIsRejected(string filename, string? replacement)
    {
        var input = ReadFixture();
        if (replacement == null)
            input.Remove(filename);
        else
            input[filename] = replacement;
        var model = new SpringModel(input);

        Assert.Throws<InvalidDataException>(model.Apply);
    }

    [Fact]
    public void IntervalsLongerThanPileAreRejected()
    {
        var input = ReadFixture();
        input["$4.txt"] = "1\n1\t24\n";
        input["$5.txt"] = "1\n1\t2220\t0\n";
        var model = new SpringModel(input);

        Assert.Throws<InvalidDataException>(model.Apply);
    }

    [Fact]
    public void NumbersUseArchiveDecimalAndThousandsSeparatorsIndependentOfCurrentCulture()
    {
        var model = new SpringModel(ReadFixture());
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            model.Apply();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        var first = model.Springs.GetFixMember()["1"].First(r => r.m == "20");
        Assert.Equal(2.861, first.length);
        Assert.Equal(2220, first.ty);
        Assert.Equal(210719, Assert.Single(model.Supports.GetFixNode()["1"], r => r.n == "18").ty);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0.5, false)]
    [InlineData(0.5, true)]
    public void RigidZoneSplitPartitionsPileIntervalsAndKeepsSupportAtOriginalTip(double jRigidLength, bool sloping)
    {
        var input = ReadFixture();
        // Isolate rigid zones on original pile member 20.
        input["Buzai_G.tmp"] = string.Concat(Enumerable.Range(1, 23).Select(id =>
            string.Concat((id == 20 ? jRigidLength.ToString(CultureInfo.InvariantCulture) : "0").PadRight(14),
                (id == 20 ? "1" : "0").PadRight(14), "1".PadRight(14), "1".PadRight(14))));
        var model = new SpringModel(input);
        var originalTip = model.Nodes.GetNode("18");
        if (sloping)
        {
            var pileHead = model.Nodes.GetNode("14");
            originalTip.x = pileHead.x + 23.95 / Math.Sqrt(2);
            originalTip.y = pileHead.y - 23.95 / Math.Sqrt(2);
        }
        var tipCoordinates = (originalTip.x, originalTip.y);
        model.Apply();
        var springIntegrals = model.Springs.GetFixMember().ToDictionary(sheet => sheet.Key,
            sheet => (Tx: sheet.Value.Sum(r => r.tx * r.length!.Value), Ty: sheet.Value.Sum(r => r.ty * r.length!.Value)));
        var splitter = new gouiki(input);
        var split = typeof(gouiki).GetMethod("exChange", BindingFlags.Instance | BindingFlags.NonPublic)!;

        split.Invoke(splitter, [model.Nodes, model.Members, model.Supports, model.Springs,
            new joint(input), new notice_points(input), new load(input)]);

        var firstSegment = model.Members.getMember("20");
        var remainingSegment = model.Members.getMember("21");
        Assert.Equal(1, firstSegment.Length(model.Nodes), 8);
        Assert.Equal(22.95 - jRigidLength, remainingSegment.Length(model.Nodes), 8);
        var firstSpring = Assert.Single(model.Springs.GetFixMember()["1"], r => r.m == "20");
        Assert.Equal(1, firstSpring.length);
        Assert.Equal(2220, firstSpring.ty);
        var remainder = model.Springs.GetFixMember()["1"].Where(r => r.m == "21").ToArray();
        Assert.Equal(jRigidLength == 0 ? 10 : 9, remainder.Length);
        Assert.Equal(1.861, remainder[0].length!.Value, 8);
        Assert.Equal(22.95 - jRigidLength, remainder.Sum(r => r.length!.Value), 8);
        Assert.Equal(23.95 * 4, model.Springs.GetFixMember()["1"].Sum(r => r.length!.Value), 8);
        var tipSegment = jRigidLength == 0 ? remainingSegment : model.Members.getMember("22");
        if (jRigidLength != 0)
        {
            var tipSprings = model.Springs.GetFixMember()["1"].Where(r => r.m == "22").ToArray();
            Assert.Equal(2, tipSprings.Length);
            Assert.Equal(0.1, tipSprings[0].length!.Value, 8);
            Assert.Equal(0.4, tipSprings[1].length!.Value, 8);
            Assert.Equal(20936, tipSprings[0].tx);
            Assert.Equal(0, tipSprings[1].tx);
        }
        Assert.Same(originalTip, model.Nodes.GetNode(tipSegment.nj));
        Assert.Equal(tipCoordinates, (originalTip.x, originalTip.y));
        var tipSupport = Assert.Single(model.Supports.GetFixNode()["1"], r => r.n == tipSegment.nj);
        Assert.Equal(210719, tipSupport.ty);
        Assert.DoesNotContain(model.Supports.GetFixNode()["1"], r => r.n == firstSegment.nj);
        foreach (var sheet in model.Springs.GetFixMember())
        {
            Assert.Equal(springIntegrals[sheet.Key].Tx, sheet.Value.Sum(r => r.tx * r.length!.Value), 6);
            Assert.Equal(springIntegrals[sheet.Key].Ty, sheet.Value.Sum(r => r.ty * r.length!.Value), 6);
            foreach (var group in sheet.Value.GroupBy(r => r.m))
            {
                double geometricLength = model.Members.getMember(group.Key).Length(model.Nodes);
                Assert.InRange(group.Sum(r => r.length!.Value), geometricLength - 1e-6 * Math.Max(1, geometricLength),
                    geometricLength + 1e-6 * Math.Max(1, geometricLength));
            }
        }
    }

    private static Dictionary<string, string> ReadFixture()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "TestData", "バネ連衡あり"))
            .ToDictionary(path => Path.GetFileName(path), path => File.ReadAllText(path, Encoding.GetEncoding(932)));
    }

    private sealed class SpringModel(Dictionary<string, string> input)
    {
        public node Nodes { get; } = new(input);
        public member Members { get; } = new(input);
        public fix_node Supports { get; } = new(input);
        public fix_member Springs { get; } = new(input);

        public void Apply() => pile_spring.Apply(input, Nodes, Members, Supports, Springs);
    }
}

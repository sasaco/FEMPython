using Newtonsoft.Json.Linq;
using PDF_Manager;
using PDF_Manager.Printing;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class PartialMemberSpringPdfTests
{
    [Theory]
    [InlineData("ja", "部材長")]
    [InlineData("en", "Length")]
    [InlineData("cn", "构件长度")]
    public void ThreeDimensionalInputTablePrintsEnteredLengthsAndBlankLegacyRows(string language, string heading)
    {
        var root = Root(3, language, """
            {"1":[{"row":2,"m":"7","length":null,"ty":4},
                  {"row":1,"m":"7","length":2,"tx":3}]}
            """);
        var table = new InspectInputFixMember(root).TableFor(new PrintData(root));

        Assert.Equal(6, table.Columns);
        Assert.Equal(heading, table[1, 1]);
        Assert.Equal("(m)", table[3, 1]);
        Assert.Equal("7", table[4, 0]);
        Assert.Equal("2.000", table[4, 1]);
        Assert.Equal("3.000", table[4, 2]);
        Assert.Equal("7", table[5, 0]);
        Assert.Equal("", table[5, 1]);
    }

    [Theory]
    [InlineData("ja", "部材長")]
    [InlineData("en", "Length")]
    [InlineData("cn", "构件长度")]
    public void TwoDimensionalPairedTableKeepsBothLengthColumnsAndOriginalRows(string language, string heading)
    {
        var root = Root(2, language, """
            {"1":[{"row":3,"m":"8","length":1,"tx":8},
                  {"row":1,"m":"7","length":2,"tx":3},
                  {"row":2,"m":"7","ty":4}]}
            """);
        var table = new InspectInputFixMember(root).TableFor(new PrintData(root));

        Assert.Equal(8, table.Columns);
        Assert.Equal(heading, table[1, 1]);
        Assert.Equal(heading, table[1, 5]);
        Assert.Equal("(m)", table[3, 1]);
        Assert.Equal("(m)", table[3, 5]);
        Assert.Equal("7", table[4, 0]);
        Assert.Equal("2.000", table[4, 1]);
        Assert.Equal("", table[5, 1]);
        Assert.Equal("8", table[4, 4]);
        Assert.Equal("1.000", table[4, 5]);
        Assert.Equal("8.000", table[4, 6]);
    }

    [Fact]
    public void TwoDimensionalSingleRowUsesFourColumns()
    {
        var root = Root(2, "ja", """{"1":[{"m":"7","length":2,"tx":3}]}""");
        var table = new InspectInputFixMember(root).TableFor(new PrintData(root));

        Assert.Equal(4, table.Columns);
        Assert.Equal("2.000", table[4, 1]);
    }

    [Fact]
    public void LongTwoDimensionalSpringInputPaginates()
    {
        var rows = string.Join(',', Enumerable.Range(1, 300)
            .Select(row => $"{{\"row\":{row},\"m\":\"{row}\",\"length\":2,\"tx\":3}}"));
        var root = Root(2, "en", $"{{\"1\":[{rows}]}}");
        root["hasPrintInputData"] = true;
        var bytes = DirectPdfGenerator.Generate(JObject.FromObject(root).ToString());

        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1);
    }

    [Theory]
    [InlineData(0, 0, 5, 0)]
    [InlineData(5, 0, 0, 0)]
    [InlineData(0, 5, 0, 0)]
    [InlineData(0, 0, 0, 5)]
    [InlineData(5, 0, 5, 5)]
    [InlineData(0, 0, 3, 4)]
    [InlineData(3, 4, 0, 0)]
    public void DiagramOnlyDrawsActiveIntervalsAndLeavesGapAndTrailingRemainderEmpty(
        double xi, double yi, double xj, double yj)
    {
        var input = new InputFixMember(Root(2, "ja", """
            {"1":[{"row":1,"m":"7","length":2,"tx":3},
                  {"row":2,"m":"7","length":1,"tx":0,"ty":0},
                  {"row":3,"m":"7","length":1,"ty":4}]}
            """));
        var member = Member(xi, yi, xj, yj);
        _ = new DiagramSpring(input, 1, new Dictionary<string, XMember> { ["7"] = member },
            new XPoint(2.5, 2.5), new XPoint(-1, 6), new XPoint(6, -1));

        var springs = member.Springs.ToArray();
        Assert.Equal(2, springs.Length);
        AssertSpan(springs[0], member, 0, 2);
        AssertSpan(springs[1], member, 3, 4);
    }

    [Fact]
    public void TerminalBlankCoversRemainderAndLegacyBlankRowsEachCoverWholeMember()
    {
        var member = Member(0, 0, 5, 0);
        var partial = new InputFixMember(Root(2, "ja", """
            {"1":[{"m":"7","length":2,"tx":3},{"m":"7","ty":4}]}
            """));
        _ = new DiagramSpring(partial, 1, new Dictionary<string, XMember> { ["7"] = member },
            new XPoint(2.5, 0), new XPoint(0, 1), new XPoint(5, -1));
        var partialSprings = member.Springs.ToArray();
        Assert.Equal(2, partialSprings.Length);
        AssertSpan(partialSprings[1], member, 2, 5);

        var legacy = new InputFixMember(Root(2, "ja", """
            {"1":[{"m":"7","tx":3},{"m":"7","ty":4}]}
            """));
        _ = new DiagramSpring(legacy, 1, new Dictionary<string, XMember> { ["7"] = member },
            new XPoint(2.5, 0), new XPoint(0, 1), new XPoint(5, -1));
        var legacySprings = member.Springs.ToArray();
        Assert.Equal(2, legacySprings.Length);
        AssertSpan(legacySprings[0], member, 0, 5);
        AssertSpan(legacySprings[1], member, 0, 5);
    }

    [Fact]
    public void GlyphFitsInsideAnIntervalShorterThanOneLegacyPitch()
    {
        var member = Member(0, 0, 5, 0);
        var input = new InputFixMember(Root(2, "ja", """
            {"1":[{"m":"7","length":2,"tx":0},{"m":"7","length":0.01,"tx":3}]}
            """));
        _ = new DiagramSpring(input, 1, new Dictionary<string, XMember> { ["7"] = member },
            new XPoint(2.5, 0), new XPoint(0, 1), new XPoint(5, -1));

        AssertSpan(Assert.Single(member.Springs), member, 2, 2.01);
    }

    [Fact]
    public void ThreeConsecutiveLengthsPartitionTheWholeMemberFromTheIEnd()
    {
        var member = Member(5, 0, 0, 0);
        var input = new InputFixMember(Root(2, "ja", """
            {"1":[{"row":3,"m":"7","length":1,"tx":3},
                  {"row":1,"m":"7","length":2,"tx":3},
                  {"row":2,"m":"7","length":2,"tx":3}]}
            """));
        _ = new DiagramSpring(input, 1, new Dictionary<string, XMember> { ["7"] = member },
            new XPoint(2.5, 0), new XPoint(0, 1), new XPoint(5, -1));

        var springs = member.Springs.ToArray();
        Assert.Equal(3, springs.Length);
        AssertSpan(springs[0], member, 0, 2);
        AssertSpan(springs[1], member, 2, 4);
        AssertSpan(springs[2], member, 4, 5);
    }

    [Theory]
    [InlineData("[{\"m\":\"7\",\"tx\":3},{\"m\":\"7\",\"length\":2,\"tx\":3}]")]
    [InlineData("[{\"m\":\"7\",\"length\":3,\"tx\":3},{\"m\":\"7\",\"length\":3,\"tx\":3}]")]
    public void InvalidIntervalRowsAreRejected(string rows)
    {
        var member = Member(0, 0, 5, 0);
        var input = new InputFixMember(Root(2, "ja", $"{{\"1\":{rows}}}"));

        Assert.Throws<FormatException>(() => new DiagramSpring(input, 1,
            new Dictionary<string, XMember> { ["7"] = member },
            new XPoint(2.5, 0), new XPoint(0, 1), new XPoint(5, -1)));
    }

    private static Dictionary<string, object> Root(int dimension, string language, string springJson) =>
        new()
        {
            ["dimension"] = dimension,
            ["language"] = language,
            ["fix_member"] = JObject.Parse(springJson)
        };

    private static XMember Member(double xi, double yi, double xj, double yj)
    {
        var raw = new Dictionary<string, object>
        {
            ["node"] = new Dictionary<string, object>
            {
                ["1"] = new { x = xi, y = yi },
                ["2"] = new { x = xj, y = yj }
            }
        };
        var input = new InputNode(raw);
        var nodes = new Dictionary<string, XNode>
        {
            ["1"] = new XNode("1", input, 1),
            ["2"] = new XNode("2", input, 1)
        };
        return new XMember("7", new Member { ni = "1", nj = "2", e = "1" }, nodes);
    }

    private static void AssertSpan(XSpring spring, XMember member, double start, double end)
    {
        var canvas = new CapturingCanvas();
        spring.Print(canvas);
        Assert.NotEmpty(canvas.Points);
        double dx = (member.Nj.Pos.X - member.Ni.Pos.X) / member.Lenngth;
        double dy = (member.Nj.Pos.Y - member.Ni.Pos.Y) / member.Lenngth;
        foreach (var point in canvas.Points)
        {
            double axial = (point.X - member.Ni.Pos.X) * dx + (point.Y - member.Ni.Pos.Y) * dy;
            Assert.InRange(axial, start - 1e-8, end + 1e-8);
        }
    }

    private sealed class InspectInputFixMember(Dictionary<string, object> root) : InputFixMember(root)
    {
        internal Table TableFor(PrintData data)
        {
            base.PrintInit(null!, data, out _, out _, out _);
            return Assert.Single(base.GetTables(null!, data, 1));
        }
    }

    private sealed class CapturingCanvas : ICanvas
    {
        internal List<XPoint> Points { get; } = new();
        public void LineBetween(XPoint pi, XPoint pj) { Points.Add(pi); Points.Add(pj); }
        public void DashedLineBetween(XPoint pi, XPoint pj, double[] dashPattern) => LineBetween(pi, pj);
        public void TextAt(string text, XPoint pos, double angle = 0, XStringFormat align = null!) { }
        public void ArcAt(XPoint center, double radius, double startAngle, double sweepAngle) { }
    }
}

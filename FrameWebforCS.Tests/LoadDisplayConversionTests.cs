using FrameWebforCS.components.input;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class LoadDisplayConversionTests
{
    private static readonly IReadOnlyDictionary<int, float> Lengths = new Dictionary<int, float>
    {
        [7] = 10, [8] = 20, [9] = 10
    };

    [Fact]
    public void PositiveRangeRepeatsPointRowsOnEachMemberAndKeepsGridRow()
    {
        var cases = LoadDisplayConversion.ExpandMemberLoads("1", [
            new LoadMemberDisplay(4, 7, 8, "y", 1, 2, 6, 3, 5)
        ], Lengths);

        var rows = cases["1"];
        Assert.Equal(2, rows.Count);
        Assert.Equal([7, 8], rows.Select(row => row.MemberStart));
        Assert.All(rows, row =>
        {
            Assert.Equal(row.MemberStart, row.MemberEnd);
            Assert.Equal(4, row.Row);
            Assert.Equal(3, row.P1);
            Assert.Equal(5, row.P2);
        });
    }

    [Fact]
    public void NegativeEndMemberTreatsDistributedLoadAsContinuousAndInterpolatesIntensity()
    {
        var cases = LoadDisplayConversion.ExpandMemberLoads("2", [
            new LoadMemberDisplay(5, 7, -8, "gy", 2, 2, 3, 10, 20)
        ], Lengths);

        var rows = cases["2"];
        Assert.Equal(2, rows.Count);
        Assert.Equal((7, 2f, 0f, 10f),
            (rows[0].MemberStart, rows[0].L1, rows[0].L2, rows[0].P1));
        Assert.Equal(13.2f, rows[0].P2, 5);
        Assert.Equal((8, 0f, 3f, 20f),
            (rows[1].MemberStart, rows[1].L1, rows[1].L2, rows[1].P2));
        Assert.Equal(13.2f, rows[1].P1, 5);
        Assert.All(rows, row => Assert.Equal(5, row.Row));
    }

    [Fact]
    public void ChainedNegativeL1UsesPreviousEndAndBlankRowResetsCarry()
    {
        var cases = LoadDisplayConversion.ExpandMemberLoads("3", [
            new LoadMemberDisplay(1, 7, 7, "y", 1, 2, 4, 2, 3),
            new LoadMemberDisplay(2, 7, 7, "y", 1, -1, 7, 5, 6),
            new LoadMemberDisplay(4, 7, 7, "y", 1, -1, 7, 7, 8)
        ], Lengths);

        var rows = cases["3"];
        Assert.Equal(3, rows.Count);
        Assert.Equal(5, rows[1].L1); // previous L2=4 plus one
        Assert.Equal(0, rows[2].P1); // blank row breaks the negative-L1 chain
        Assert.Equal(8, rows[2].P2);
    }

    [Fact]
    public void ContinuousPointChainCarriesActualSecondStationMember()
    {
        // JS getL2Position carries m7/L2=3 into the next adjacent row.
        var cases = LoadDisplayConversion.ExpandMemberLoads("3", [
            new LoadMemberDisplay(1, 7, -8, "y", 1, 2, 3, 5, 6),
            new LoadMemberDisplay(2, 7, -8, "y", 1, -1, 0, 7, 0)
        ], Lengths);

        var first = Assert.Single(cases["3"].Where(row => row.Row == 1));
        Assert.Equal((7, 2f, 3f, 5f, 6f),
            (first.MemberStart, first.L1, first.L2, first.P1, first.P2));
        Assert.Contains(cases["3"], row => row.Row == 2 &&
            row.MemberStart == 7 && row.L1 == 4 && row.P1 == 7);
        Assert.DoesNotContain(cases["3"], row => row.Row == 2 && row.MemberStart == 8);
    }

    [Fact]
    public void NegativeDistributedStartInterpolatesIntensityAtMemberOrigin()
    {
        // JS checkIntoMemberL1 computes P3 before checkMember2 splits the group.
        var cases = LoadDisplayConversion.ExpandMemberLoads("3", [
            new LoadMemberDisplay(1, 7, -8, "y", 2, -2, 0, 0, 30)
        ], Lengths);

        Assert.All(cases["3"], row => Assert.Equal(30, row.P1));
        Assert.All(cases["3"], row => Assert.Equal(30, row.P2));
    }

    [Fact]
    public void ChainedRowAfterDistributionUsesRawL2CarryBeforeMemberPositionCarry()
    {
        // checkIntoMemberL1 carries raw L2=20 m, so -1 becomes 21 m
        // from m7's start; checkMember2 then resolves that point to m8/L1=11.
        var cases = LoadDisplayConversion.ExpandMemberLoads("3", [
            new LoadMemberDisplay(1, 7, -8, "y", 2, 0, 20, 1, 1),
            new LoadMemberDisplay(2, 7, -8, "y", 1, -1, 0, 5, 0)
        ], Lengths);

        var chained = Assert.Single(cases["3"].Where(row => row.Row == 2));
        Assert.Equal(8, chained.MemberStart);
        Assert.Equal(11, chained.L1);
        Assert.Equal(5, chained.P1);
    }

    [Fact]
    public void ZeroValueInputRowStillAdvancesLegacyDistanceCarry()
    {
        // getEnableLoad keeps a complete row even when both P values are zero;
        // final checkIntoMember removes it only after the next row uses L2.
        var cases = LoadDisplayConversion.ExpandMemberLoads("3", [
            new LoadMemberDisplay(1, 7, 7, "y", 1, 0, 4, 0, 0),
            new LoadMemberDisplay(2, 7, 7, "y", 1, -1, 0, 5, 0)
        ], Lengths);

        var drawn = Assert.Single(cases["3"]);
        Assert.Equal(2, drawn.Row);
        Assert.Equal(5, drawn.L1);
    }

    [Fact]
    public void ConvertedDistancesAndIntensitiesUseLegacyMillimetreAndCentRounding()
    {
        var lengths = new Dictionary<int, float> { [7] = 10.0004f, [8] = 20 };
        var cases = LoadDisplayConversion.ExpandMemberLoads("3", [
            new LoadMemberDisplay(1, 7, 7, "y", 1, 10.0004f, 0, 1.234f, 0),
            new LoadMemberDisplay(2, 7, -8, "y", 2, 0, 0, 0, 1)
        ], lengths);

        Assert.Equal(10, Assert.Single(cases["3"].Where(row => row.Row == 1)).L1);
        Assert.Equal(1.23f, Assert.Single(cases["3"].Where(row => row.Row == 1)).P1);
        Assert.Equal(0.33f, Assert.Single(cases["3"].Where(row =>
            row.Row == 2 && row.MemberStart == 7)).P2);
    }

    [Fact]
    public void MovingLoadProducesFractionalDisplayCasesWithoutMutatingInput()
    {
        var source = new LoadMemberDisplay(1, 7, 7, "y", 1, 1, 0, 5, 0);
        var cases = LoadDisplayConversion.ExpandMemberLoads("1", [source], Lengths,
            symbol: "LL", llPitch: 5);

        Assert.Contains("1", cases.Keys);
        // JS checkIntoMemberL1 discards the L1=L2=0 point row. The first
        // surviving 1.1 case is then promoted to the integral key.
        Assert.DoesNotContain("1.1", cases.Keys);
        Assert.Contains("1.2", cases.Keys);
        Assert.Equal(1, source.L1);
        Assert.Equal(5, Assert.Single(cases["1"]).L1);
        Assert.Equal(10, Assert.Single(cases["1.2"]).L1);
        Assert.All(cases.Values.SelectMany(rows => rows), row => Assert.Equal(1, row.Row));
    }

    [Fact]
    public void MovingLoadClearsTheFirstL1BeforeCalculatingItsStartPosition()
    {
        var source = new LoadMemberDisplay(1, 7, 7, "y", 1, -5, 0, 5, 0);

        var cases = LoadDisplayConversion.ExpandMemberLoads("1", [source], Lengths,
            symbol: "LL", llPitch: 5);

        Assert.Equal(5, Assert.Single(cases["1"]).L1);
        Assert.DoesNotContain("1.1", cases.Keys);
        Assert.Equal(10, Assert.Single(cases["1.2"]).L1);
        Assert.Equal(-5, source.L1);
    }

    [Fact]
    public void ReversedSignedEndStillTraversesMemberChain()
    {
        var cases = LoadDisplayConversion.ExpandMemberLoads("4", [
            new LoadMemberDisplay(8, 8, -7, "y", 2, 0, 0, 2, 4)
        ], Lengths);

        var rows = cases["4"];
        Assert.Equal([7, 8], rows.Select(row => row.MemberStart));
        Assert.Equal(8, rows[0].Row);
        Assert.Equal(8, rows[1].Row);
    }

    [Fact]
    public void OutsideMemberAndUnsupportedRowsAreOmitted()
    {
        var cases = LoadDisplayConversion.ExpandMemberLoads("1", [
            new LoadMemberDisplay(1, 7, 7, "y", 1, 12, 13, 2, 3),
            new LoadMemberDisplay(2, 7, 7, "y", 2, 9, 2, 1, 2),
            new LoadMemberDisplay(3, 7, 7, "y", 99, 0, 0, 1, 1)
        ], Lengths);

        Assert.Empty(cases);
    }
}

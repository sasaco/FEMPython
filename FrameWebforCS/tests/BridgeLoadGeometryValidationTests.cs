using FrameWebforCS.components.input;
using FrameWebforCS.three;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class BridgeLoadGeometryValidationTests
{
    private static BridgePoint P(double x, double y, double z = 0) => new(x, y, z);
    private static BridgePoint[][] Square(double size = 4) =>
        [[P(0, 0), P(size, 0), P(size, size)], [P(0, 0), P(size, size), P(0, size)]];
    private static BridgePanel Panel(BridgePlane? plane = null) =>
        new(7, "deck", [1, 2, 3, 4], [], [[1, 2, 3], [1, 3, 4]], [], plane, 1e-9, 1e-9, [], []);
    private static BridgeLoad Load(bool area = true) => new(1, 7, area ? [1, 2] : [1],
        area ? [[1, 2], [3, 4]] : [[1, 2]], new("global", null), "");
    private static BridgePath[] Paths(double size = 4, double z = 0) =>
        [new(1, "", [P(0, 0, z), P(size, 0, z)]), new(2, "", [P(0, size, z), P(size, size, z)])];

    [Fact]
    public void OffPlaneAreaAndInclinedSurfaceWithoutExplicitPlaneAreRejected()
    {
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), Square(), Square(), [], Paths(z: 1), Load()));
        var tilted = Square().Select(t => t.Select(p => p with { Z = p.X }).ToArray()).ToArray();
        var paths = Paths().Select(p => p with { Points = p.Points.Select(v => v with { Z = v.X }).ToArray() }).ToArray();
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), tilted, tilted, [], paths, Load()));
        var plane = new BridgePlane(P(0, 0), P(1, 0, 1), P(0, 1));
        Assert.Single(BridgeLoadValidation.Validate(Panel(plane), tilted, tilted, [], paths, Load()));
    }

    [Fact]
    public void AreaOutsideOuterBoundaryIsRejectedRatherThanSilentlyClipped()
    {
        var paths = Paths();
        paths[1] = paths[1] with { Points = [P(0, 5), P(4, 5)] };
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), Square(), Square(), [], paths, Load()));
    }

    private static (BridgePoint[][] Cells, BridgePoint[][] Holes) Ring(double left = 1, double right = 2)
    {
        BridgePoint[] outer = [P(0, 0), P(4, 0), P(4, 4), P(0, 4)];
        BridgePoint[] inner = [P(left, 1), P(right, 1), P(right, 2), P(left, 2)];
        var cells = new List<BridgePoint[]>();
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            cells.Add([outer[i], outer[j], inner[j]]);
            cells.Add([outer[i], inner[j], inner[i]]);
        }
        return (cells.ToArray(), [inner.Reverse().ToArray()]);
    }

    [Fact]
    public void AreaSpansDeclaredHoleButLineCannotCrossEvenANarrowHole()
    {
        var (cells, holes) = Ring(1.01, 1.02);
        Assert.Single(BridgeLoadValidation.Validate(Panel(), cells, cells, holes, Paths(), Load()));
        BridgePath[] crossing = [new(1, "", [P(0, 1.5), P(4, 1.5)])];
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), cells, cells, holes, crossing, Load(false)));
        BridgePath[] boundary = [new(1, "", [P(0, 1), P(4, 1)])];
        Assert.Empty(BridgeLoadValidation.Validate(Panel(), cells, cells, holes, boundary, Load(false)));
    }

    [Fact]
    public void UndeclaredOrWronglyOrientedHoleAndAreaEntirelyInsideHoleAreRejected()
    {
        var (cells, holes) = Ring();
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), cells, cells, [], Paths(), Load()));
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), cells, cells,
            [holes[0].Reverse().ToArray()], Paths(), Load()));
        BridgePath[] inner = [new(1, "", [P(1.1, 1.1), P(1.9, 1.1)]), new(2, "", [P(1.1, 1.9), P(1.9, 1.9)])];
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), cells, cells, holes, inner, Load()));
    }

    [Fact]
    public void ConcaveExteriorNotchCannotBeBridgedByAreaOrLine()
    {
        double[] x = [0, 1.01, 1.02, 4]; double[] y = [0, 1, 4];
        var cells = new List<BridgePoint[]>();
        for (int i = 0; i + 1 < x.Length; i++)
            for (int j = 0; j + 1 < y.Length; j++)
            {
                if (i == 1 && j == 1) continue;
                var a = P(x[i], y[j]); var b = P(x[i + 1], y[j]);
                var c = P(x[i + 1], y[j + 1]); var d = P(x[i], y[j + 1]);
                cells.Add([a, b, c]); cells.Add([a, c, d]);
            }
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), cells, cells, [], Paths(), Load()));
        BridgePath[] crossing = [new(1, "", [P(0, 2), P(4, 2)])];
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), cells, cells, [], crossing, Load(false)));
        BridgePath[] inside = [new(1, "", [P(0, .5), P(4, .5)])];
        Assert.Empty(BridgeLoadValidation.Validate(Panel(), cells, cells, [], inside, Load(false)));
    }

    [Fact]
    public void IndependentMeshOppositeWindingAndHoleUseStructuralTargetWithoutChangingLoads()
    {
        var reversed = Square().Select(t => t.Reverse().ToArray()).ToArray();
        Assert.Single(BridgeLoadValidation.Validate(Panel(), Square(), reversed, [], Paths(), Load()));
        var (cells, holes) = Ring();
        Assert.Single(BridgeLoadValidation.Validate(Panel(), Square(), cells, holes, Paths(), Load()));
    }

    [Fact]
    public void SelfCrossingPathAndNonConvexStripAreRejected()
    {
        BridgePath[] crossing = [new(1, "", [P(0, 0), P(4, 4), P(0, 4), P(4, 0)])];
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), Square(), Square(), [], crossing, Load(false)));
        BridgePath[] strip = [new(1, "", [P(0, 0), P(4, 0)]), new(2, "", [P(0, 4), P(1, .1)])];
        Assert.Throws<ArgumentException>(() => BridgeLoadValidation.Validate(Panel(), Square(), Square(), [], strip, Load()));
    }

    [Fact]
    public void TinyValidAreaUsesLengthScaledCorrespondenceAndClippingTolerances()
    {
        const double size = 1e-5;
        Assert.Single(BridgeLoadValidation.Validate(Panel(), Square(size), Square(size), [], Paths(size), Load()));
    }
}

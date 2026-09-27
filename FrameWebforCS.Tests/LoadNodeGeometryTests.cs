using FrameWebforCS.three;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class LoadNodeGeometryTests
{
    [Theory]
    [InlineData("tx", 5, 1, "gx-", 0xFF0000)]
    [InlineData("ty", -5, -1, "gy+", 0x00FF00)]
    [InlineData("tz", 5, 1, "gz-", 0x0000FF)]
    public void PointMatchesLegacyDirectionScaleOffsetAndSelection(
        string direction, float value, float expectedSign, string offsetDirection, int color)
    {
        var anchor = new Vector3(3, 4, 5);
        using var glyph = LoadNodeGeometry.CreatePoint(anchor, direction, value,
            pMax: 10, baseScale: 2, loadScale: 50, offset: 0.25f)!;

        Assert.Equal("PointLoad", glyph.Root.Name);
        Assert.Equal(1.25f, glyph.Size, 5); // |5|/10 * 2.5 * (2 * 50/100)
        Assert.Equal(1.5f, glyph.EndOffset, 5);
        Assert.Equal(offsetDirection, glyph.OffsetDirection);
        Assert.Equal((3f, 4f, 5f), (glyph.Root.Position.X, glyph.Root.Position.Y, glyph.Root.Position.Z));
        var arrow = Assert.IsType<ArrowHelper>(glyph.Root.GetObjectByName("arrow"));
        var axis = direction[1];
        float coordinate = axis == 'x' ? arrow.Position.X : axis == 'y' ? arrow.Position.Y : arrow.Position.Z;
        Assert.Equal(-expectedSign * 1.5f, coordinate, 5);
        var cone = Assert.IsType<Mesh>(arrow.Children.Single(c => c is Mesh));
        Assert.Equal(color, cone.Material.Color!.Value.GetHex());
        glyph.Highlight(true);
        Assert.Equal(LoadNodeGeometry.SelectedColor, cone.Material.Color!.Value.GetHex());
        glyph.Highlight(false);
        Assert.Equal(color, cone.Material.Color!.Value.GetHex());
        Assert.Equal((3f, 4f, 5f), (anchor.X, anchor.Y, anchor.Z));
    }

    [Theory]
    [InlineData("rx", 5, 0xFF0000)]
    [InlineData("ry", -5, 0x00FF00)]
    [InlineData("rz", 5, 0x0000FF)]
    public void MomentMatchesLegacyArcConeOrientationAndSelection(string direction, float value, int color)
    {
        using var glyph = LoadNodeGeometry.CreateMoment(new Vector3(3, 4, 5), direction,
            value, mMax: 10, baseScale: 2)!;

        Assert.Equal("MomentLoad", glyph.Root.Name);
        Assert.Equal(1f, glyph.Size, 5);
        Assert.Equal($"rg{direction[1]}", glyph.OffsetDirection);
        var child = Assert.IsType<Group>(glyph.Root.GetObjectByName("child"));
        Assert.Equal(1f, child.Scale.X, 5);
        var line = Assert.IsType<Line>(glyph.Root.GetObjectByName("line"));
        var positions = Assert.IsType<BufferAttribute<float>>(((BufferGeometry)line.Geometry).Attributes["position"]);
        Assert.Equal(13, positions.count);
        Assert.Equal(MathF.Cos(MathF.PI / 6), positions.Array[0], 5);
        Assert.Equal(MathF.Sin(MathF.PI / 6), positions.Array[1], 5);
        Assert.Equal(color, line.Material.Color!.Value.GetHex());
        var cone = Assert.IsType<Mesh>(glyph.Root.GetObjectByName("arrow"));
        Assert.Equal(3, Assert.IsType<ConeBufferGeometry>(cone.Geometry).RadialSegments);
        glyph.Highlight(true);
        Assert.Equal(LoadNodeGeometry.SelectedColor, line.Material.Color!.Value.GetHex());
        Assert.Equal(LoadNodeGeometry.SelectedColor, cone.Material.Color!.Value.GetHex());
        glyph.Highlight(false);
        Assert.Equal(color, line.Material.Color!.Value.GetHex());
        Assert.Equal(color, cone.Material.Color!.Value.GetHex());
    }

    [Fact]
    public void ZeroOrUnsupportedNodeLoadsProduceNoGeometryAndInvalidScaleFails()
    {
        var point = new Vector3();
        Assert.Null(LoadNodeGeometry.CreatePoint(point, "tx", 0, 1, 1));
        Assert.Null(LoadNodeGeometry.CreatePoint(point, "rx", 1, 1, 1));
        Assert.Null(LoadNodeGeometry.CreateMoment(point, "tx", 1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoadNodeGeometry.CreatePoint(point, "tx", 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoadNodeGeometry.CreateMoment(point, "rx", 1, 1, 1, 401));
    }
}

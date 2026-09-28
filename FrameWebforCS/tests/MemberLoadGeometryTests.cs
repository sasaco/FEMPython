using FrameWebforCS.three;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class MemberLoadGeometryTests
{
    private static MemberLoadGeometryInput Input(int mark, string direction,
        float l1 = 1, float l2 = 2, float p1 = 3, float p2 = 4,
        float loadScale = 100) =>
        new(new Vector3(0, 0, 0), new Vector3(10, 0, 0), mark,
            direction, l1, l2, p1, p2, 0, 0.2f, loadScale);

    [Fact]
    public void CreatesDistinctLegacyMemberLoadFamilyShapes()
    {
        var cases = new[]
        {
            (1, "y", "member-point", 4),
            (11, "z", "member-moment", 4),
            (2, "y", "member-distributed", 2),
            (2, "x", "member-axial", 2),
            (2, "r", "member-torsion", 4),
            (9, "", "member-temperature", 1)
        };
        foreach (var (mark, direction, family, minimumPrimitives) in cases)
        {
            using var glyph = Assert.IsType<MemberLoadGeometry>(
                MemberLoadGeometryFactory.Create(Input(mark, direction)));
            Assert.Equal(family, glyph.Family);
            Assert.True(glyph.PrimitiveCount >= minimumPrimitives, family);
            Assert.Equal(5, glyph.Anchor.X);
            glyph.SetSelected(true);
            Assert.All(glyph.Root.Children, child =>
                Assert.Equal(family == "member-torsion" && child.Name.StartsWith("torsion-face")
                    ? 0xFF00FF : 0xAFEEEE, child.Material!.Color!.Value.GetHex()));
            glyph.SetSelected(false);
        }
    }

    [Fact]
    public void PointStationsUseIEndWhileDistributedSecondStationUsesJEnd()
    {
        using var points = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(1, "y", 2, 8)));
        var pointShafts = points.Root.Children.OfType<Line>()
            .Where(line => line.Name.EndsWith("-shaft"))
            .OrderBy(line => line.Name).ToArray();
        Assert.Equal(2, pointShafts.Length);
        Assert.Equal(2, Positions(pointShafts[0])[^3]);
        Assert.Equal(8, Positions(pointShafts[1])[^3]);

        using var distributed = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(2, "y", 2, 3)));
        var outline = Assert.Single(distributed.Root.Children.OfType<Line>());
        var positions = Positions(outline);
        Assert.Equal(2, positions[0]);
        Assert.Equal(7, positions[^3]);
    }

    [Fact]
    public void ScalesPointArrowAndRejectsInvalidLegacyRanges()
    {
        using var normal = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(1, "y", 1, 2, 4, 0)));
        using var larger = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(1, "y", 1, 2, 4, 0, 200)));
        var normalLine = Assert.Single(normal.Root.Children.OfType<Line>());
        var largerLine = Assert.Single(larger.Root.Children.OfType<Line>());
        Assert.Equal(2 * ShaftLength(Positions(normalLine)),
            ShaftLength(Positions(largerLine)), 4);
        Assert.Null(MemberLoadGeometryFactory.Create(Input(2, "y", 8, 3)));
        Assert.Null(MemberLoadGeometryFactory.Create(Input(1, "y", 1, 11)));
        Assert.Null(MemberLoadGeometryFactory.Create(Input(1, "unknown")));
        Assert.Null(MemberLoadGeometryFactory.Create(Input(1, "y", loadScale: 401)));
        Assert.Null(MemberLoadGeometryFactory.Create(Input(9, "", p1: 0, p2: 4)));
    }

    [Fact]
    public void SplitsOppositeSignedDistributionAndHonorsTwoDimensionalFamilies()
    {
        using var distributed = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(2, "y", 1, 1, 2, -2)));
        var face = Assert.Single(distributed.Root.Children.OfType<Mesh>());
        var vertices = Assert.IsType<BufferAttribute<float>>(
            Assert.IsType<BufferGeometry>(face.Geometry).GetAttribute<float>("position")).Array;
        Assert.Equal(18, vertices.Length); // two triangles meet at zero intensity

        Assert.Null(MemberLoadGeometryFactory.Create(Input(2, "r") with { Is3D = false }));
        Assert.Null(MemberLoadGeometryFactory.Create(Input(11, "x") with { Is3D = false }));
        using var planarMoment = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(11, "z") with { Is3D = false }));
        Assert.Equal("member-moment", planarMoment.Family);
    }

    [Fact]
    public void SteepMemberUsesLegacyGlobalZReferenceForLocalY()
    {
        var steep = Input(2, "y", 0, 0, 1, 1) with
        {
            Nj = new Vector3(1, 0, 10)
        };
        using var glyph = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(steep));
        var face = Assert.Single(glyph.Root.Children.OfType<Mesh>());
        var positions = Assert.IsType<BufferAttribute<float>>(
            Assert.IsType<BufferGeometry>(face.Geometry).GetAttribute<float>("position")).Array;
        // JS ThreeMembersService.tMatrix: for DX != 0, local y is global +Y.
        Assert.Equal(positions[0], positions[3], 5);
        Assert.True(positions[4] < positions[1]);
    }

    [Fact]
    public void TemperatureLineUsesNegativeLocalYAndMomentArcHasLegacySamples()
    {
        using var temperature = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(9, "", p1: 20)));
        var line = Assert.Single(temperature.Root.Children.OfType<Line>());
        Assert.Equal(-0.2f, Positions(line)[1], 5);

        using var moment = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(11, "z", p1: 3, p2: 0)));
        var arc = Assert.Single(moment.Root.Children.OfType<Line>());
        Assert.Equal(13 * 3, Positions(arc).Length);
    }

    [Fact]
    public void TorsionUsesOpenFiveSixthsCylinderSectorsAndMagentaSelectedFaces()
    {
        using var glyph = Assert.IsType<MemberLoadGeometry>(
            MemberLoadGeometryFactory.Create(Input(2, "r", p1: 2, p2: -3)));
        var faces = glyph.Root.Children.OfType<Mesh>()
            .Where(mesh => mesh.Name.StartsWith("torsion-face")).ToArray();
        Assert.Equal(2, faces.Length);
        foreach (var face in faces)
        {
            var geometry = Assert.IsType<CylinderBufferGeometry>(face.Geometry);
            Assert.True(geometry.OpenEnded);
            Assert.Equal(-MathF.PI / 3, geometry.ThetaStart, 5);
            Assert.Equal(5 * MathF.PI / 3, geometry.ThetaLength, 5);
        }
        Assert.True(((CylinderBufferGeometry)faces[0].Geometry!).RadiusTop > 0);
        Assert.Equal(0, ((CylinderBufferGeometry)faces[0].Geometry!).RadiusBottom);
        Assert.Equal(0, ((CylinderBufferGeometry)faces[1].Geometry!).RadiusTop);
        Assert.True(((CylinderBufferGeometry)faces[1].Geometry!).RadiusBottom > 0);
        glyph.SetSelected(true);
        Assert.All(faces, face => Assert.Equal(0xFF00FF, face.Material!.Color!.Value.GetHex()));
    }

    private static float[] Positions(Line line) =>
        Assert.IsType<BufferAttribute<float>>(
            Assert.IsType<BufferGeometry>(line.Geometry).GetAttribute<float>("position")).Array;

    private static float ShaftLength(float[] positions) =>
        MathF.Sqrt(MathF.Pow(positions[3] - positions[0], 2) +
                   MathF.Pow(positions[4] - positions[1], 2) +
                   MathF.Pow(positions[5] - positions[2], 2));
}

using FrameWebforCS.components.input;
using FrameWebforCS.three;
using System.Text.Json;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ThreeConstraintsServiceTests
{
    private static readonly Dictionary<int, (int Ni, int Nj)> Members = new() { [7] = (1, 9) };

    [Fact]
    public void ActiveCaseProjectsDefaultsAndSparseStableRows()
    {
        ClearInputs();
        try
        {
            Load("""
                {"fix_node":{"1":[{"row":4,"n":"9","tx":1},{"row":8,"n":"1","ty":2}]},
                 "fix_member":{"1":[{"row":3,"m":"7","tx":2}]},
                 "joint":{"1":[{"row":6,"m":"7","xi":0,"yj":1}]}}
                """);
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.SetMode("fix_node");
            layer.Rebuild(Nodes(), Members, "1", 1);
            Assert.Equal(2, layer.Count("fix_node"));
            Assert.Equal(1, layer.Count("fix_member"));
            Assert.Equal(1, layer.Count("joint")); // null defaults to fixed; explicit zero is free.
            Assert.Equal(13f, layer.PositionOf("fix_node", 4, "tx")!.X, 4);
            layer.Select("fix_node", 4, "tx");
            Assert.Equal(new ConstraintSelection("fix_node", 4, "tx"), layer.Selected);
            var ray = new Raycaster(new Vector3(13f, 0, 10), new Vector3(0, 0, -1));
            Assert.Equal(new ConstraintSelection("fix_node", 4, "tx"), layer.Pick(ray));

            layer.Rebuild(Nodes(), Members, "2", 1);
            Assert.Equal(0, layer.Count("fix_node"));
            Assert.Equal(0, layer.Count("fix_member"));
            Assert.Equal(0, layer.Count("joint"));
            Assert.Null(layer.Selected);
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void NodeMovementReplacementAndMissingReferencesRemoveDependentGlyphs()
    {
        ClearInputs();
        try
        {
            Load("""
                {"fix_member":{"1":[{"row":3,"m":"7","tx":2}]},
                 "joint":{"1":[{"row":6,"m":"7","xi":0}]},
                 "rigid":[{"m":"7","Ilength":2,"Jlength":3,"e":1}],
                 "notice_points":[{"row":11,"m":"7","Points":[4]}]}
                """);
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.SetMode("notice_points");
            layer.Rebuild(Nodes(), Members, "1");
            Assert.Equal(4f, layer.PositionOf("notice_points", 11, "L1")!.X);
            Assert.Equal(4, layer.Count("rigid")); // two end bars and two markers

            var moved = Nodes();
            moved[9] = new Vector3(22, 0, 0);
            layer.Rebuild(moved, Members, "1");
            Assert.Equal(4f, layer.PositionOf("notice_points", 11, "L1")!.X);
            Assert.Equal(11f, layer.PositionOf("fix_member", 3, "x")!.X);

            layer.Rebuild(new Dictionary<int, Vector3> { [1] = new(0, 0, 0) }, Members, "1");
            Assert.Equal(0, layer.Count("fix_member"));
            Assert.Equal(0, layer.Count("joint"));
            Assert.Equal(0, layer.Count("notice_points"));
            Assert.Equal(0, layer.Count("rigid"));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void IndependentConstraintCasesFollowTheirOwnSheets()
    {
        ClearInputs();
        try
        {
            Load("""
                {"fix_node":{"1":[{"row":4,"n":"9","tx":1}]},
                 "fix_member":{"2":[{"row":3,"m":"7","tx":2}]},
                 "joint":{"3":[{"row":6,"m":"7","xi":0}]}}
                """);
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, new ConstraintCases("1", "2", "3"));
            Assert.Equal(1, layer.Count("fix_node"));
            Assert.Equal(1, layer.Count("fix_member"));
            Assert.Equal(1, layer.Count("joint"));

            layer.Rebuild(Nodes(), Members, new ConstraintCases("2", "1", "1"));
            Assert.Equal(0, layer.Count("fix_node"));
            Assert.Equal(0, layer.Count("fix_member"));
            Assert.Equal(0, layer.Count("joint"));
            InputFixNodeService.Instance.SelectCase("2");
            Assert.Equal("2", InputFixNodeService.Instance.SelectedCaseId);
            Assert.Throws<ArgumentOutOfRangeException>(() => InputFixNodeService.Instance.SelectCase("7"));
        }
        finally
        {
            InputFixNodeService.Instance.SelectCase("1");
            ClearInputs();
        }
    }

    [Fact]
    public void ModesSelectionEventsAndDisposeAreDeterministic()
    {
        ClearInputs();
        try
        {
            int changed = 0;
            EventHandler handler = (_, _) => changed++;
            InputFixNodeService.Instance.Changed += handler;
            try
            {
                Load("""{"fix_node":{"1":[{"row":4,"n":"9","tx":1}]}}""");
                Assert.True(changed > 0);
                var scene = new Scene();
                var layer = new ThreeConstraintsService(scene);
                layer.Rebuild(Nodes(), Members, "1");
                layer.SetMode("fix_node");
                layer.Select("fix_node", 4);
                Assert.Equal(4, layer.Selected?.Row);
                layer.SetMode("member");
                Assert.Null(layer.Selected);
                Assert.False(((Group)scene.GetObjectByName("fix_node")!).Visible);
                layer.SetMode("load_names");
                Assert.True(((Group)scene.GetObjectByName("fix_node")!).Visible);
                layer.SetMode("load_values");
                Assert.False(((Group)scene.GetObjectByName("fix_node")!).Visible);
                Assert.Null(layer.Pick(new Raycaster(new Vector3(13f, 0, 10),
                    new Vector3(0, 0, -1))));
                layer.Dispose();
                layer.Dispose();
                Assert.Null(scene.GetObjectByName("fix_node"));
                Assert.Throws<ObjectDisposedException>(() => layer.Rebuild(Nodes(), Members, "1"));
            }
            finally { InputFixNodeService.Instance.Changed -= handler; }
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void StageParsersRejectInvalidRowsWithoutChangingCommittedData()
    {
        ClearInputs();
        try
        {
            Load("""{"fix_node":{"1":[{"row":4,"n":"9","tx":1}]}}""");
            using var bad = JsonDocument.Parse("""{"fix_node":{"1":[{"row":0,"n":"9"}]}}""");
            Assert.Throws<JsonException>(() => InputFixNodeService.ParseFixNodeJson(bad.RootElement));
            Assert.Single(InputFixNodeService.Instance.GetDisplaySnapshot("1"));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void JointRingsFollowMemberLocalAxesIncludingCg()
    {
        ClearInputs();
        try
        {
            Load("""
                {"member":{"7":{"ni":"1","nj":"9","cg":90}},
                 "joint":{"1":[{"row":6,"m":"7","xi":0,"yi":0,"zi":0}]}}
                """);
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1");

            var joint = (Group)scene.GetObjectByName("joint")!;
            var x = (Mesh)joint.GetObjectByName("joint6xi")!;
            var y = (Mesh)joint.GetObjectByName("joint6yi")!;
            var z = (Mesh)joint.GetObjectByName("joint6zi")!;
            Assert.Equal(0.1f, layer.PositionOf("joint", 6, "xi")!.X, 4);
            AssertAxis(x, new Vector3(1, 0, 0));
            AssertAxis(y, new Vector3(0, 0, 1));
            AssertAxis(z, new Vector3(0, -1, 0));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void FullyFixedNodeHighlightsItsCubeFromAnyConstraintColumn()
    {
        ClearInputs();
        try
        {
            Load("""{"fix_node":{"1":[{"row":4,"n":"9","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]}}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.SetMode("fix_node");
            layer.Rebuild(Nodes(), Members, "1");
            var cube = (Mesh)((Group)scene.GetObjectByName("fix_node")!).GetObjectByName("fix_node4tp")!;

            layer.Select("fix_node", 4, "tx");
            Assert.Equal(new ConstraintSelection("fix_node", 4, "tp"), layer.Selected);
            Assert.Equal(0xFF11FF, cube.Material.Color!.Value.GetHex());
            layer.Select("fix_node", 4, "rz");
            Assert.Equal(new ConstraintSelection("fix_node", 4, "tp"), layer.Selected);
            layer.Select(null);
            Assert.Equal(0x303030, cube.Material.Color!.Value.GetHex());

            layer.Rebuild(Nodes(), Members, "1", dimension: 2);
            layer.Select("fix_node", 4, "rz");
            Assert.Equal(new ConstraintSelection("fix_node", 4, "tp"), layer.Selected);
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void NegativeOneIsNotDrawnAsAStiffnessSpring()
    {
        ClearInputs();
        try
        {
            Load("""{"fix_node":{"1":[{"row":4,"n":"9","tx":-1,"rx":-1,"ty":2,"ry":2}]}}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1");
            var root = (Group)scene.GetObjectByName("fix_node")!;
            Assert.Null(root.GetObjectByName("fix_node4tx"));
            Assert.Null(root.GetObjectByName("fix_node4rx"));
            Assert.IsType<Line>(root.GetObjectByName("fix_node4ty"));
            Assert.IsType<Line>(root.GetObjectByName("fix_node4ry"));
            layer.SetMode("fix_node");
            Assert.Equal(new ConstraintSelection("fix_node", 4, "ty"),
                layer.Pick(new Raycaster(new Vector3(12, 1, 10), new Vector3(0, 0, -1))));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void MemberSpringsRepeatAlongLocalMemberAxisAndRetainCgDirection()
    {
        ClearInputs();
        try
        {
            Load("""{"member":{"7":{"ni":"1","nj":"9","cg":90}},"fix_member":{"1":[{"row":3,"m":"7","ty":2}]}}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1");
            var root = (Group)scene.GetObjectByName("fix_member")!;
            var marker = root.Children.OfType<Line>().Where(item => item.Name == "fix_member3y").ToArray();
            Assert.True(marker.Length > 1);
            Assert.Equal(1, layer.Count("fix_member"));
            Assert.Contains(marker, item => item.Position.X < 6);
            Assert.Contains(marker, item => item.Position.X > 6);
            Assert.All(marker, item => Assert.Equal(0x88FF88, item.Material.Color!.Value.GetHex()));
            var vertices = Assert.IsType<BufferAttribute<float>>(
                Assert.IsType<BufferGeometry>(marker[0].Geometry).GetAttribute<float>("position")).Array;
            Assert.True(Math.Abs(vertices[^1] - vertices[2]) > 1); // cg=90 turns local Y into global Z.
            Assert.Equal(vertices[1], vertices[^2], 4);
            layer.SetMode("fix_member");
            layer.Select("fix_member", 3, "y");
            Assert.All(marker, item => Assert.Equal(0x00A5FF, item.Material.Color!.Value.GetHex()));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void MemberSpringIntervalsFollowSourceRowsAndPreservePickIdentity()
    {
        ClearInputs();
        try
        {
            Load("""
                {"fix_member":{"1":[{"row":7,"m":"7","length":1,"tz":2},
                                    {"row":1,"m":"7","length":2,"tx":2},
                                    {"row":4,"m":"7","length":2,"ty":2}]}}
                """);
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.SetMode("fix_member");
            layer.Rebuild(FiveMeterNodes(), Members, "1");

            Assert.Equal(3, layer.Count("fix_member"));
            Assert.Equal(1f, layer.PositionOf("fix_member", 1, "x")!.X, 4);
            Assert.Equal(3f, layer.PositionOf("fix_member", 4, "y")!.X, 4);
            Assert.Equal(4.5f, layer.PositionOf("fix_member", 7, "z")!.X, 4);
            AssertMemberSpringBounds(scene, 1, 0, 2, new Vector3(1, 0, 0));
            AssertMemberSpringBounds(scene, 4, 2, 4, new Vector3(1, 0, 0));
            AssertMemberSpringBounds(scene, 7, 4, 5, new Vector3(1, 0, 0));
            Assert.Equal(new ConstraintSelection("fix_member", 4, "y"),
                layer.Pick(new Raycaster(new Vector3(3, 0, 10), new Vector3(0, 0, -1))));

            layer.Select("fix_member", 7, "z");
            Assert.Equal(new ConstraintSelection("fix_member", 7, "z"), layer.Selected);
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void MemberSpringGapTrailingRemainderAndTerminalBlankHaveDistinctSpans()
    {
        ClearInputs();
        try
        {
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.SetMode("fix_member");
            Load("""
                {"fix_member":{"1":[{"row":1,"m":"7","length":2,"tx":2},
                                    {"row":2,"m":"7","length":2},
                                    {"row":3,"m":"7","length":1,"ty":2}]}}
                """);
            layer.Rebuild(FiveMeterNodes(), Members, "1");
            Assert.Equal(2, layer.Count("fix_member"));
            Assert.Null(layer.PositionOf("fix_member", 2, "x"));
            Assert.Equal(4.5f, layer.PositionOf("fix_member", 3, "y")!.X, 4);
            AssertMemberSpringBounds(scene, 3, 4, 5, new Vector3(1, 0, 0));
            Assert.Null(layer.Pick(new Raycaster(new Vector3(3, 0, 10), new Vector3(0, 0, -1))));

            Load("""{"fix_member":{"1":[{"row":1,"m":"7","length":2,"tx":2}]}}""");
            layer.Rebuild(FiveMeterNodes(), Members, "1");
            Assert.Equal(1, layer.Count("fix_member"));
            AssertMemberSpringBounds(scene, 1, 0, 2, new Vector3(1, 0, 0));
            Assert.Null(layer.PositionOf("fix_member", 2, "x"));
            Assert.Null(layer.Pick(new Raycaster(new Vector3(3.5f, 0, 10), new Vector3(0, 0, -1))));

            Load("""
                {"fix_member":{"1":[{"row":1,"m":"7","length":2,"tx":2},
                                    {"row":2,"m":"7","ty":2}]}}
                """);
            layer.Rebuild(FiveMeterNodes(), Members, "1");
            Assert.Equal(3.5f, layer.PositionOf("fix_member", 2, "y")!.X, 4);
            AssertMemberSpringBounds(scene, 2, 2, 5, new Vector3(1, 0, 0));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void ShortSlopedMemberSpringStaysInsideIntervalAfterScaleChanges()
    {
        ClearInputs();
        try
        {
            Load("""
                {"member":{"7":{"ni":"1","nj":"9","cg":90}},
                 "fix_member":{"1":[{"row":1,"m":"7","length":0.05,
                                      "tx":2,"ty":2,"tz":2,"tr":2}]}}
                """);
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            var nodes = new Dictionary<int, Vector3>
            {
                [1] = new(0, 0, 0), [9] = new(3, 4, 0)
            };
            layer.Rebuild(nodes, Members, "1");
            var memberAxis = new Vector3(0.6f, 0.8f, 0);
            foreach (float scale in new[] { 0f, 1f, 5f })
            {
                layer.SetFixMemberScale(scale);
                AssertMemberSpringBounds(scene, 1, 0, 0.05f, memberAxis);
            }
            Assert.Equal(0.025f * memberAxis.X,
                layer.PositionOf("fix_member", 1, "y")!.X, 4);
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void LegacyBlankLengthRowsRemainFullMemberSprings()
    {
        ClearInputs();
        try
        {
            Load("""
                {"fix_member":{"1":[{"row":2,"m":"7","tx":2},
                                    {"row":5,"m":"7","ty":3}]}}
                """);
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(FiveMeterNodes(), Members, "1");
            Assert.Equal(2, layer.Count("fix_member"));
            Assert.Equal(2.5f, layer.PositionOf("fix_member", 2, "x")!.X, 4);
            Assert.Equal(2.5f, layer.PositionOf("fix_member", 5, "y")!.X, 4);
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void JointAndPointMarkersKeepLegacyFixedSizesAcrossModelScaleChanges()
    {
        ClearInputs();
        try
        {
            Load("""{"joint":{"1":[{"row":6,"m":"7","xi":0}]},"rigid":[{"m":"7","Ilength":2}],"notice_points":[{"row":11,"m":"7","Points":[4]}]}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1", baseScale: 1);
            var joint = (Mesh)((Group)scene.GetObjectByName("joint")!).GetObjectByName("joint6xi")!;
            var rigid = (Mesh)((Group)scene.GetObjectByName("rigid")!).GetObjectByName("rigid7i")!;
            var notice = (Mesh)((Group)scene.GetObjectByName("notice_points")!).GetObjectByName("notice_points11L1")!;
            Assert.Equal(1, joint.Scale.X);
            Assert.Equal(0.0188957667f * 3, rigid.Scale.X, 5);
            Assert.Equal(rigid.Scale.X, notice.Scale.X);

            layer.Rebuild(Nodes(), Members, "1", baseScale: 20);
            joint = (Mesh)((Group)scene.GetObjectByName("joint")!).GetObjectByName("joint6xi")!;
            rigid = (Mesh)((Group)scene.GetObjectByName("rigid")!).GetObjectByName("rigid7i")!;
            Assert.Equal(1, joint.Scale.X);
            Assert.Equal(0.0188957667f * 3, rigid.Scale.X, 5);
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void RigidAndNoticeSelectionExposesRelatedMemberAndColorsMarker()
    {
        ClearInputs();
        try
        {
            Load("""{"rigid":[{"m":"7","Ilength":2}],"notice_points":[{"row":11,"m":"7","Points":[4]}]}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1");
            var rigid = (Mesh)((Group)scene.GetObjectByName("rigid")!).GetObjectByName("rigid7i")!;
            layer.SetMode("rigid");
            layer.Select("rigid", 7, "i");
            Assert.Equal(7, layer.SelectedRelatedMemberId);
            Assert.Equal(0xFF0000, rigid.Material.Color!.Value.GetHex());
            Assert.Null(layer.Pick(new Raycaster(new Vector3(1, 0, 10), new Vector3(0, 0, -1))));

            layer.SetMode("notice_points");
            layer.Select("notice_points", 11, "L1");
            var notice = (Mesh)((Group)scene.GetObjectByName("notice_points")!).GetObjectByName("notice_points11L1")!;
            Assert.Equal(7, layer.SelectedRelatedMemberId);
            Assert.Equal(0xFF0000, notice.Material.Color!.Value.GetHex());
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void ScaleControlsApplyToExistingAndRebuiltGlyphs()
    {
        ClearInputs();
        try
        {
            Load("""{"fix_node":{"1":[{"row":4,"n":"9","tx":1}]},"fix_member":{"1":[{"row":3,"m":"7","ty":2}]}}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1");
            var node = (Mesh)((Group)scene.GetObjectByName("fix_node")!).GetObjectByName("fix_node4tx")!;
            var member = ((Group)scene.GetObjectByName("fix_member")!).Children.OfType<Line>()
                .First(item => item.Name == "fix_member3y");
            float nodeBefore = node.Scale.X;
            var memberBefore = SpringVertices(member).ToArray();
            layer.SetFixNodeScale(10);
            layer.SetFixMemberScale(2);
            Assert.Equal(nodeBefore * 2, node.Scale.X);
            var memberAfter = SpringVertices(member);
            Assert.Equal(memberBefore[2] * 2, memberAfter[2], 4);
            layer.Rebuild(Nodes(), Members, "1");
            node = (Mesh)((Group)scene.GetObjectByName("fix_node")!).GetObjectByName("fix_node4tx")!;
            Assert.Equal(nodeBefore * 2, node.Scale.X);
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetFixNodeScale(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetFixMemberScale(6));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void MemberSpringResizeUsesLegacyAxisSpecificDimensions()
    {
        ClearInputs();
        try
        {
            Load("""{"fix_member":{"1":[{"row":3,"m":"7","tx":2,"ty":2,"tz":2,"tr":2}]}}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1");
            var root = (Group)scene.GetObjectByName("fix_member")!;
            var springs = new[] { "x", "y", "z", "r" }.ToDictionary(axis => axis,
                axis => root.Children.OfType<Line>().First(line => line.Name == $"fix_member3{axis}"));
            var before = springs.ToDictionary(item => item.Key, item => SpringVertices(item.Value).ToArray());

            layer.SetFixMemberScale(4);
            foreach (var axis in new[] { "x", "y", "z", "r" })
            {
                var oldPoint = Point(before[axis], 10);
                var resized = Point(SpringVertices(springs[axis]), 10);
                var along = axis switch
                {
                    "x" or "r" => new Vector3(1, 0, 0),
                    "y" => new Vector3(0, 1, 0),
                    _ => new Vector3(0, 0, 1)
                };
                float axialBefore = oldPoint.Dot(along);
                float axialAfter = resized.Dot(along);
                float radialBefore = new Vector3(oldPoint.X - axialBefore * along.X,
                    oldPoint.Y - axialBefore * along.Y, oldPoint.Z - axialBefore * along.Z).Length();
                float radialAfter = new Vector3(resized.X - axialAfter * along.X,
                    resized.Y - axialAfter * along.Y, resized.Z - axialAfter * along.Z).Length();
                Assert.Equal(axialBefore * (axis == "x" ? 1 : 4), axialAfter, 3);
                Assert.Equal(radialBefore * (axis is "y" or "z" ? 3 : 4), radialAfter, 3);
            }

            layer.SetFixMemberScale(1);
            foreach (var axis in springs.Keys)
                Assert.Equal(before[axis], SpringVertices(springs[axis]));
        }
        finally { ClearInputs(); }
    }

    [Fact]
    public void MemberSpringLegacyRepeatCountBeyond32AndExplicitLongMemberBudget()
    {
        ClearInputs();
        try
        {
            Load("""{"fix_member":{"1":[{"row":3,"m":"7","tx":2}]}}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            var nodes = Nodes();
            nodes[9] = new Vector3(24, 0, 0);
            layer.Rebuild(nodes, Members, "1");
            var root = (Group)scene.GetObjectByName("fix_member")!;
            var springs = root.Children.OfType<Line>().Where(line => line.Name == "fix_member3x").ToArray();
            Assert.Equal(79, springs.Length); // floor(24 / 0.6 - 0.3) = 39, from -39 through +39.
            layer.SetMode("fix_member");
            layer.Select("fix_member", 3, "x");
            Assert.All(springs, line => Assert.Equal(0x00A5FF, line.Material.Color!.Value.GetHex()));

            nodes[9] = new Vector3(2_000, 0, 0);
            var error = Assert.Throws<InvalidOperationException>(() => layer.Rebuild(nodes, Members, "1"));
            Assert.Contains("vertex budget", error.Message);
            Assert.Equal(79, root.Children.OfType<Line>().Count()); // failed staging preserves the scene.
            layer.Dispose();
            Assert.Null(scene.GetObjectByName("fix_member"));
        }
        finally { ClearInputs(); }
    }

    private static float[] SpringVertices(Line line) =>
        Assert.IsType<BufferAttribute<float>>(
            Assert.IsType<BufferGeometry>(line.Geometry).GetAttribute<float>("position")).Array;

    private static void AssertMemberSpringBounds(Scene scene, int row, float start,
        float end, Vector3 memberAxis)
    {
        var root = (Group)scene.GetObjectByName("fix_member")!;
        var lines = root.Children.OfType<Line>()
            .Where(line => line.Name.StartsWith($"fix_member{row}", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(lines);
        foreach (var line in lines)
        {
            var vertices = SpringVertices(line);
            for (int offset = 0; offset < vertices.Length; offset += 3)
            {
                var point = new Vector3(line.Position.X + vertices[offset],
                    line.Position.Y + vertices[offset + 1], line.Position.Z + vertices[offset + 2]);
                float distance = point.Dot(memberAxis);
                Assert.InRange(distance, start - 0.0001f, end + 0.0001f);
            }
        }
    }

    private static Vector3 Point(float[] vertices, int index) =>
        new(vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);

    [Fact]
    public void MemberSpringUsesNearestNodeDistanceInsteadOfNodeMarkerScale()
    {
        ClearInputs();
        try
        {
            Load("""{"fix_member":{"1":[{"row":3,"m":"7","ty":2}]}}""");
            var scene = new Scene();
            using var layer = new ThreeConstraintsService(scene);
            layer.Rebuild(Nodes(), Members, "1", baseScale: 1, memberSpringScale: 12);
            var line = ((Group)scene.GetObjectByName("fix_member")!).Children.OfType<Line>()
                .First(item => item.Name == "fix_member3y");
            var vertices = Assert.IsType<BufferAttribute<float>>(
                Assert.IsType<BufferGeometry>(line.Geometry).GetAttribute<float>("position")).Array;
            Assert.Equal(6, vertices[2], 4); // legacy coil radius = 0.5 * minDistance.
        }
        finally { ClearInputs(); }
    }

    private static void AssertAxis(Mesh mesh, Vector3 expected)
    {
        var q = mesh.Quaternion;
        var normal = new Vector3(2 * (q.X * q.Z + q.W * q.Y),
            2 * (q.Y * q.Z - q.W * q.X), 1 - 2 * (q.X * q.X + q.Y * q.Y));
        Assert.Equal(expected.X, normal.X, 4);
        Assert.Equal(expected.Y, normal.Y, 4);
        Assert.Equal(expected.Z, normal.Z, 4);
    }

    private static Dictionary<int, Vector3> Nodes() => new()
    {
        [1] = new Vector3(0, 0, 0), [9] = new Vector3(12, 0, 0)
    };

    private static Dictionary<int, Vector3> FiveMeterNodes() => new()
    {
        [1] = new Vector3(0, 0, 0), [9] = new Vector3(5, 0, 0)
    };

    private static void Load(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        InputMembersService.Instance.ApplyMembers(InputMembersService.ParseMemberJson(root));
        InputFixNodeService.Instance.ApplyFixNode(InputFixNodeService.ParseFixNodeJson(root));
        InputFixMemberService.Instance.ApplyFixMember(InputFixMemberService.ParseFixMemberJson(root));
        InputJointService.Instance.ApplyJoint(InputJointService.ParseJointJson(root));
        InputRigidZoneService.Instance.ApplyRigid(InputRigidZoneService.ParseRigidJson(root));
        InputNoticePointsService.Instance.ApplyNoticePoints(InputNoticePointsService.ParseNoticePointsJson(root));
    }

    private static void ClearInputs()
    {
        InputMembersService.Instance.clear();
        InputFixNodeService.Instance.clear();
        InputFixMemberService.Instance.clear();
        InputJointService.Instance.clear();
        InputRigidZoneService.Instance.clear();
        InputNoticePointsService.Instance.clear();
    }
}

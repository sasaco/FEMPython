using FrameWebforCS.components.input;
using FrameWebforCS.three;
using System.Text.Json;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ThreeLoadsServiceTests
{
    private static readonly Dictionary<int, Vector3> Nodes = new()
    {
        [1] = new(0, 0, 0), [2] = new(10, 0, 0), [3] = new(20, 0, 0)
    };
    private static readonly Dictionary<int, LoadMemberFrame> Members = new()
    {
        [7] = new(1, 2), [8] = new(2, 3)
    };

    [Fact]
    public void DrawsEverySupportedNodeAndMemberLoadFamily()
    {
        var scene = new Scene();
        using var loads = new ThreeLoadsService(scene);
        loads.ReplaceAll(new Dictionary<string, LoadCaseDisplay>
        {
            ["1"] = new("", new[]
            {
                new LoadNodeDisplay(1, 1, false, 5, 0, 0, 0, -2, 0),
                new LoadNodeDisplay(2, 2, true, 0.002f, 0, 0, 0, 0, 0)
            }, new[]
            {
                new LoadMemberDisplay(3, 7, 7, "gy", 1, 2, 8, 3, 0),
                new LoadMemberDisplay(4, 7, 7, "y", 2, 1, 1, 2, 3),
                new LoadMemberDisplay(5, 7, 7, "r", 2, 1, 1, 4, 0),
                new LoadMemberDisplay(6, 7, 7, "", 9, 0, 0, 20, 0),
                new LoadMemberDisplay(7, 7, 7, "x", 11, 1, 9, 2, 3),
                new LoadMemberDisplay(8, 7, 7, "x", 2, 1, 1, 2, 0)
            })
        }, Nodes, Members);
        loads.SetCase("1");
        loads.SetVisible(true);

        // JS ThreeLoadService stores one LoadData per member-load row even when
        // that object contains two arrows. The former shared-cone renderer counted
        // the two arrowheads separately.
        Assert.Equal(9, loads.GlyphCount);
        Assert.Equal(9, loads.VisibleGlyphCount);
        Assert.Contains(loads.Glyphs, g => g.Family == "node-force" && g.SourceId == 1);
        Assert.Contains(loads.Glyphs, g => g.Family == "node-moment" && g.Column == "ry");
        Assert.Contains(loads.Glyphs, g => g.Family == "displacement" && g.SourceId == 2);
        foreach (string family in new[] { "member-point", "member-distributed",
                     "member-axial", "member-torsion", "member-temperature", "member-moment" })
            Assert.Contains(loads.Glyphs, g => g.Family == family);
        Assert.Single(scene.Children);
    }

    [Fact]
    public void CaseSwitchAndGridSelectionOnlyHighlightTheActiveCase()
    {
        var scene = new Scene();
        using var loads = new ThreeLoadsService(scene);
        var cases = new Dictionary<string, LoadCaseDisplay>
        {
            ["1"] = new("", new[] { new LoadNodeDisplay(1, 1, false, 1, 0, 0, 0, 0, 0) },
                Array.Empty<LoadMemberDisplay>()),
            ["2"] = new("", Array.Empty<LoadNodeDisplay>(),
                new[] { new LoadMemberDisplay(1, 7, 7, "y", 1, 1, 1, 1, 0) })
        };
        loads.ReplaceAll(cases, Nodes, Members);
        loads.SetVisible(true);
        loads.SetCase("1");
        loads.Select(1, "tx");
        Assert.Equal((1, "tx"), loads.Selection);
        Assert.Equal(1, loads.VisibleGlyphCount);

        loads.SetCase("2");
        Assert.Null(loads.Selection);
        Assert.Equal(1, loads.VisibleGlyphCount);
        loads.Select(1, "P1"); // JS maps member cells to `m`.
        Assert.Equal((1, "m"), loads.Selection);
        loads.SetVisible(false);
        Assert.Equal(0, loads.VisibleGlyphCount);
        Assert.Null(loads.Selection);
    }

    [Fact]
    public void CaseChildrenFollowLegacyRankThenGridRow()
    {
        var scene = new Scene();
        using var loads = new ThreeLoadsService(scene);
        loads.ReplaceAll(new Dictionary<string, LoadCaseDisplay>
        {
            ["1"] = new("", [
                new LoadNodeDisplay(1, 1, false, 2, 0, 0, 0, 0, 0),
                new LoadNodeDisplay(2, 1, false, 0, 0, 0, 0, 3, 0)
            ], [
                new LoadMemberDisplay(3, 7, 7, "y", 1, 1, 0, 1, 0),
                new LoadMemberDisplay(4, 7, 7, "r", 2, 0, 0, 1, 1)
            ])
        }, Nodes, Members);

        var root = Assert.IsType<Group>(Assert.Single(scene.Children));
        var caseRoot = Assert.IsType<Group>(Assert.Single(root.Children));
        Assert.Equal(["load-node-moment-2-ry-1", "load-member-torsion-4-m-7",
            "load-member-point-3-m-7", "load-node-force-1-tx-1"],
            caseRoot.Children.Select(child => child.Name));
    }

    [Fact]
    public void MovingLoadCyclesExpandedDisplayCasesAndStopsWhenHidden()
    {
        using var loads = new ThreeLoadsService(new Scene());
        loads.ReplaceAll(new Dictionary<string, LoadCaseDisplay>
        {
            ["1"] = new("LL", Array.Empty<LoadNodeDisplay>(),
                new[] { new LoadMemberDisplay(1, 7, 8, "y", 1, 0, 0, 5, 0) }, 2)
        }, Nodes, Members);
        loads.SetCase("1");
        loads.SetVisible(true);
        string? initial = loads.DisplayCaseId;
        Assert.Equal("1", loads.CurrentCaseId);
        Assert.True(loads.VisibleGlyphCount > 0);

        loads.AdvanceAnimation(0.5f);
        Assert.NotEqual(initial, loads.DisplayCaseId);
        Assert.True(loads.VisibleGlyphCount > 0);
        string? paused = loads.DisplayCaseId;
        loads.SetVisible(false);
        loads.AdvanceAnimation(1);
        Assert.Equal(paused, loads.DisplayCaseId);
    }

    [Fact]
    public void CaseMaximumsKeepTheFiveLegacyLoadFamiliesSeparate()
    {
        using var loads = new ThreeLoadsService(new Scene());
        loads.ReplaceAll(new Dictionary<string, LoadCaseDisplay>
        {
            ["1"] = new("", new[]
            {
                new LoadNodeDisplay(1, 1, false, 5, 0, 0, 0, -2, 0)
            }, new[]
            {
                new LoadMemberDisplay(2, 7, 7, "y", 1, 1, 2, -10, 0),
                new LoadMemberDisplay(3, 7, 7, "z", 11, 1, 2, 8, 0),
                new LoadMemberDisplay(4, 7, 7, "y", 2, 1, 1, 20, 0),
                new LoadMemberDisplay(5, 7, 7, "r", 2, 1, 1, 30, 0),
                new LoadMemberDisplay(6, 7, 7, "x", 2, 1, 1, 40, 0)
            }),
            ["2"] = new("", new[]
            {
                new LoadNodeDisplay(1, 1, false, 1, 0, 0, 0, 0, 0)
            }, Array.Empty<LoadMemberDisplay>())
        }, Nodes, Members);

        Assert.Equal(new MaxLoadDict(10, 8, 20, 30, 40), loads.MaxLoadDicts["1"]);
        Assert.Equal(new MaxLoadDict(1, 0, 0, 0, 0), loads.MaxLoadDicts["2"]);
    }

    [Fact]
    public void ReplaceAfterTopologyEditsMovesAndDeletesDependentGlyphs()
    {
        var scene = new Scene();
        using var loads = new ThreeLoadsService(scene);
        var cases = new Dictionary<string, LoadCaseDisplay>
        {
            ["1"] = new("", new[] { new LoadNodeDisplay(1, 1, false, 5, 0, 0, 0, 0, 0) },
                new[] { new LoadMemberDisplay(2, 7, 7, "z", 9, 0, 0, 10, 0) })
        };
        loads.ReplaceAll(cases, Nodes, Members);
        var prior = loads.Glyphs.Single(g => g.Family == "member-temperature").Anchor;
        var moved = new Dictionary<int, Vector3>(Nodes) { [2] = new(10, 8, 0) };
        loads.ReplaceAll(cases, moved, Members);
        var next = loads.Glyphs.Single(g => g.Family == "member-temperature").Anchor;
        Assert.NotEqual(prior.Y, next.Y);
        Assert.Equal(2, loads.GlyphCount);

        moved.Remove(1);
        loads.ReplaceAll(cases, moved, Members);
        Assert.Equal(0, loads.GlyphCount);
        Assert.Single(scene.Children); // no duplicated scene roots
    }

    [Fact]
    public void SnapshotPreservesNegativeNodeDisplacementAndCaseNotification()
    {
        var service = InputLoadService.Instance;
        using var document = JsonDocument.Parse("""
            {"load":{"1":{"load_node":[{"row":1,"n":"-3","tx":2,"rz":-4}],
                           "load_member":[{"row":2,"m1":"7","direction":"GY","mark":"2",
                                           "L1":"1","L2":"2","P1":3}]},
                     "2":{"name":"second"}}}
            """);
        int edits = 0;
        string? changedCase = null;
        void OnEdit() => edits++;
        void OnCase(string id) => changedCase = id;
        service.LoadsEdited += OnEdit;
        service.SelectedCaseChanged += OnCase;
        try
        {
            var staged = InputLoadService.ParseLoadJson(document.RootElement);
            Assert.NotNull(staged);
            service.ApplyLoads(staged);
            var snapshot = service.GetDisplaySnapshot();
            var node = Assert.Single(snapshot["1"].NodeLoads);
            Assert.True(node.IsDisplacement);
            Assert.Equal(3, node.NodeId);
            Assert.Equal(0.002f, node.Tx, 6);
            Assert.Equal(-0.004f, node.Rz, 6);
            var member = Assert.Single(snapshot["1"].MemberLoads);
            Assert.Equal("gy", member.Direction);
            Assert.Equal(7, member.MemberEnd); // absent m2 defaults to m1
            int before = edits;
            service.IntensityRows[0].tx = 6;
            Assert.True(edits > before);
            Assert.Equal(0.006f, Assert.Single(service.GetDisplaySnapshot()["1"].NodeLoads).Tx, 6);
            service.IntensityRows[0].n = null;
            Assert.Empty(service.GetDisplaySnapshot()["1"].NodeLoads);
            service.SelectCase("2");
            Assert.Equal("2", changedCase);
            Assert.True(edits > 0);
        }
        finally
        {
            service.LoadsEdited -= OnEdit;
            service.SelectedCaseChanged -= OnCase;
            service.clear();
            service.SelectCase("1");
        }
    }

    [Fact]
    public void DisposeRemovesRootAndRejectsFurtherUpdates()
    {
        var scene = new Scene();
        var loads = new ThreeLoadsService(scene);
        loads.ReplaceAll(new Dictionary<string, LoadCaseDisplay> { ["1"] =
            new("", new[] { new LoadNodeDisplay(1, 1, false, 1, 0, 0, 0, 0, 0) },
                Array.Empty<LoadMemberDisplay>()) }, Nodes, Members);
        loads.Dispose();
        loads.Dispose();
        Assert.Empty(scene.Children);
        Assert.Throws<ObjectDisposedException>(() => loads.SetVisible(true));
    }

    [Fact]
    public void ParseLoadStagesMissingAndRejectsMalformedSections()
    {
        using var missing = JsonDocument.Parse("""{"node":{}}""");
        Assert.Empty(InputLoadService.ParseLoadJson(missing.RootElement)!);
        using var malformed = JsonDocument.Parse("""{"load":{"1":{"load_node":{}}}}""");
        Assert.Throws<JsonException>(() => InputLoadService.ParseLoadJson(malformed.RootElement));
    }

    [Fact]
    public void ReplacingFileNormalizesMissingSelectedCaseAndPublishesChange()
    {
        var service = InputLoadService.Instance;
        using var oldFile = JsonDocument.Parse("""{"load":{"1":{"symbol":"A"},"2":{"symbol":"B"}}}""");
        using var newFile = JsonDocument.Parse("""{"load":{"1":{"symbol":"C"}}}""");
        var selected = new List<string>();
        void OnSelected(string id) => selected.Add(id);
        service.SelectedCaseChanged += OnSelected;
        try
        {
            service.ApplyLoads(InputLoadService.ParseLoadJson(oldFile.RootElement));
            service.SelectCase("2");
            selected.Clear();

            service.ApplyLoads(InputLoadService.ParseLoadJson(newFile.RootElement));

            Assert.Equal("1", service.SelectedCaseId);
            Assert.Equal(["1"], selected);
            Assert.Equal("C", service.GetDisplaySnapshot()["1"].Symbol);
            Assert.Equal("1", service.IntensityRows[0].LoadId);

            selected.Clear();
            service.ApplyLoads(InputLoadService.ParseLoadJson(newFile.RootElement));
            Assert.Empty(selected);
        }
        finally
        {
            service.SelectedCaseChanged -= OnSelected;
            service.clear();
            service.SelectCase("1");
        }
    }
}

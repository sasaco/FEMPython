using FarPoint.Win.Spread;
using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class PartialMemberSpringTests
{
    [Fact]
    public void IntervalsFollowRowOrderAndPreserveUnsprungTailAndZeroGap()
    {
        var rows = new[]
        {
            new clsFixMember { row = 9, m = "1", length = 1, tx = 30 },
            new clsFixMember { row = 2, m = "1", length = 2, tx = 10 },
            new clsFixMember { row = 5, m = "1", length = 2, tx = 0 }
        };
        var spans = MemberSpringIntervals.Resolve(rows, 6);
        Assert.Equal(new[] { (0d, 2d), (2d, 4d), (4d, 5d) },
            spans.Select(span => (span.Start, span.End)));
        Assert.Equal(new float?[] { 10, 0, 30 }, spans.Select(span => span.Row.tx));
    }

    [Fact]
    public void BlankLengthUsesWholeMemberOrTerminalRemainder()
    {
        var full = MemberSpringIntervals.Resolve([
            new clsFixMember { row = 1, m = "1", tx = 5 },
            new clsFixMember { row = 2, m = "1", tx = 6 }], 5);
        Assert.All(full, span => { Assert.Equal(0, span.Start); Assert.Equal(5, span.End); });
        var remainder = MemberSpringIntervals.Resolve([
            new clsFixMember { row = 1, m = "1", length = 2 },
            new clsFixMember { row = 2, m = "1" }], 5);
        Assert.Equal((2d, 5d), (remainder[1].Start, remainder[1].End));
    }

    [Fact]
    public void InvalidMixedOrExcessLengthsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => MemberSpringIntervals.Resolve([
            new clsFixMember { row = 1, m = "1" },
            new clsFixMember { row = 2, m = "1", length = 2 }], 5));
        Assert.Throws<ArgumentException>(() => MemberSpringIntervals.Resolve([
            new clsFixMember { row = 1, m = "1", length = 4 },
            new clsFixMember { row = 2, m = "1", length = 2 }], 5));
        Assert.Throws<ArgumentException>(() => MemberSpringIntervals.Resolve([
            new clsFixMember { row = 1, m = "1", length = 5 },
            new clsFixMember { row = 2, m = "1" }], 5));
    }

    [Fact]
    public void EditorRowsInsertDeleteAndSaveStaySynchronized()
    {
        var service = InputFixMemberService.Instance;
        service.clear();
        try
        {
            var editor = service.GetEditorRows("1");
            editor[0].M = "1";
            editor[0].Length = 2;
            editor[0].Tx = 10;
            Assert.Equal(2, editor.Count);
            Assert.Equal(2, service.GetRows("1")[0].length);
            Assert.True(service.InsertEditorRow("1", 2, "1"));
            Assert.Equal(new[] { 1, 2, 3 }, editor.Select(row => row.row));
            editor[1].Length = 2;
            editor[1].Tx = 20;
            Assert.True(service.DeleteEditorRows("1", [1]));
            Assert.Equal(1, editor[0].row);
            Assert.Equal(20, editor[0].tx);
            Assert.Equal(2, service.GetRows("1")[0].length);
            Assert.Equal(1, service.GetDisplaySnapshot("1").Single().row);
            var saved = service.getFixMemberJson();
            Assert.Single((System.Collections.IList)saved["1"]);
        }
        finally { service.clear(); }
    }

    [Fact]
    public void CalculationRequestKeepsSegmentLengthsAndExplicitZeroGap()
    {
        using var result = JsonDocument.Parse(CalculationRequestBuilder.FromSavedJson(SavedInput(
            """
            [{"row":1,"m":"1","length":2,"tx":10},
             {"row":2,"m":"1","length":2,"tx":0},
             {"row":3,"m":"1","length":1,"tx":30}]
            """)).Json);
        var rows = result.RootElement.GetProperty("fix_member").GetProperty("1");
        Assert.Equal(new[] { 2d, 2d, 1d }, rows.EnumerateArray()
            .Select(row => row.GetProperty("length").GetDouble()));
        Assert.Equal(0, rows[1].GetProperty("tx").GetDouble());
    }

    [Fact]
    public void InvalidSavedIntervalsFailBeforeReplacingCurrentDocument()
    {
        var input = InputDataService.Instance;
        using var valid = JsonDocument.Parse(SavedInput("""[{"row":1,"m":"1","tx":10}]"""));
        using var invalid = JsonDocument.Parse(SavedInput(
            """
            [{"row":1,"m":"1","length":4,"tx":10},
             {"row":2,"m":"1","length":2,"tx":20}]
            """));
        try
        {
            input.JsonDataOpen(valid.RootElement);
            Assert.Throws<JsonException>(() => input.JsonDataOpen(invalid.RootElement));
            Assert.Equal(10, InputFixMemberService.Instance.GetDisplaySnapshot("1").Single().tx);
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
        }
    }

    [Fact]
    public void SpringSheetsKeepFixedCapacityWithoutMemberOutlines()
    {
        RunSta(() =>
        {
            var service = InputFixMemberService.Instance;
            try
            {
                service.clear();
                using var component = new InputFixMemberComponent();
                var spread = component.Controls.OfType<FpSpread>().Single();
                Assert.All(spread.Sheets.Cast<SheetView>(), sheet =>
                {
                    Assert.Equal(100_000, sheet.RowCount);
                    Assert.Empty(sheet.GetRangeGroupInfo(1, true) ?? []);
                });
                var first = spread.Sheets[0];
                first.SetValue(99_999, 0, "1");
                var row = Assert.Single(service.GetDisplaySnapshot("1"));
                Assert.Equal(100_000, row.row);
                Assert.Equal("1", row.m);
                Assert.Equal(100_000, first.RowCount);
                Assert.Empty(first.GetRangeGroupInfo(1, true) ?? []);
                Assert.True(component.SelectGridRow(100_000, "tx", "1"));
                Assert.Equal(0, first.ActiveRowIndex);
                first.SetValue(1, 0, "1");
                var outline = Assert.Single(first.GetRangeGroupInfo(1, true));
                Assert.Equal(1, outline.Start);
                Assert.Equal(1, outline.Length);
                Assert.True(component.SelectGridRow(100_000, "tx", "1"));
                Assert.Equal(1, first.ActiveRowIndex);
            }
            finally { service.clear(); }
        });
    }

    [Fact]
    public void RowOutlineShowsWholeMemberAndMasksSprings()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            using var document = JsonDocument.Parse(SavedInput(
                """
                [{"row":1,"m":"1","length":2,"tx":10},
                 {"row":2,"m":"1","length":2,"tx":20}]
                """));
            try
            {
                input.JsonDataOpen(document.RootElement);
                using var component = new InputFixMemberComponent();
                var sheet = component.Controls.OfType<FpSpread>().Single().Sheets[0];
                Assert.NotNull(sheet.DataSource);
                Assert.Equal(100_000, sheet.RowCount);
                var outline = Assert.Single(sheet.GetRangeGroupInfo(1, true));
                Assert.Equal(1, outline.Start);
                Assert.Equal(1, outline.Length);
                Assert.Equal("1", sheet.GetValue(0, 0));
                Assert.Equal(2f, sheet.GetValue(0, 1));
                Assert.Equal(10f, sheet.GetValue(0, 2));
                Assert.False(sheet.Rows[0].Locked);
                sheet.SetValue(0, 1, 1.5f);
                Assert.Equal(1.5f, InputFixMemberService.Instance.GetDisplaySnapshot("1")[0].length);
                Assert.True(component.SelectGridRow(2, "tx", "1"));
                Assert.Equal(1, sheet.ActiveRowIndex);
                sheet.ExpandRangeGroup(outline, true, false);
                Assert.Equal(GroupState.Collapsed, Assert.Single(sheet.GetRangeGroupInfo(1, true)).State);
                Assert.Equal("5.00", sheet.GetValue(0, 1));
                Assert.Equal("***", sheet.GetValue(0, 2));
                Assert.True(sheet.Rows[0].Locked);
                Assert.Equal(1.5f, InputFixMemberService.Instance.GetDisplaySnapshot("1")[0].length);
                InputFixMemberService.Instance.GetRows("1")[0].M = "1";
                Assert.Equal(GroupState.Collapsed, Assert.Single(sheet.GetRangeGroupInfo(1, true)).State);
                outline = Assert.Single(sheet.GetRangeGroupInfo(1, true));
                sheet.ExpandRangeGroup(outline, true, true);
                Assert.Equal(1.5f, sheet.GetValue(0, 1));
                sheet.ExpandRangeGroup(outline, true, false);
                Assert.Equal("5.00", sheet.GetValue(0, 1));
                Assert.True(component.SelectGridRow(2, "tx", "1"));
                Assert.Equal(GroupState.Expanded, Assert.Single(sheet.GetRangeGroupInfo(1, true)).State);
                Assert.Equal(1.5f, sheet.GetValue(0, 1));
                Assert.Equal(10f, sheet.GetValue(0, 2));
                Assert.False(sheet.Rows[0].Locked);
            }
            finally
            {
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
            }
        });
    }

    [Fact]
    public void GroupedEditorMovesDistantMemberRowsAndAcceptsBackslashInsert()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            using var document = JsonDocument.Parse(SavedInput(
                """
                [{"row":1,"m":"1","tx":10},
                 {"row":2,"m":"2","tx":20},
                 {"row":3,"m":"1","tx":30}]
                """));
            try
            {
                input.JsonDataOpen(document.RootElement);
                using var form = new System.Windows.Forms.Form { Width = 700, Height = 350 };
                using var component = new InputFixMemberComponent();
                form.Controls.Add(component);
                form.Show();
                System.Windows.Forms.Application.DoEvents();
                var spread = Assert.IsType<FrameWebforCS.components.myFpSpread>(
                    component.Controls.OfType<FpSpread>().Single());
                var sheet = spread.Sheets[0];
                Assert.NotNull(sheet.DataSource);
                Assert.Equal(100_000, sheet.RowCount);
                Assert.Single(sheet.GetRangeGroupInfo(1, true));
                Assert.Equal(30f, sheet.GetValue(1, 2));
                Assert.Equal(20f, sheet.GetValue(2, 2));
                Assert.Equal(new[] { 1, 2, 3 }, InputFixMemberService.Instance
                    .GetDisplaySnapshot("1").Select(row => row.row));

                sheet.SetActiveCell(1, 0);
                Assert.True(LoadSheetTest.Key(spread, System.Windows.Forms.Keys.Oem5).SuppressKeyPress);
                Assert.Equal(new[] { 1, 2, 3, 4, 5 }, InputFixMemberService.Instance
                    .GetEditorRows("1").Select(row => row.row));
                Assert.Equal("1", InputFixMemberService.Instance.GetEditorRows("1")[2].m);

                LoadSheetTest.MarkRowHeader(spread);
                sheet.ClearSelection();
                sheet.AddSelection(1, -1, 1, -1);
                Assert.True(LoadSheetTest.Key(spread, System.Windows.Forms.Keys.Delete).SuppressKeyPress);
                Assert.Equal(new[] { 1, 2, 3 }, InputFixMemberService.Instance
                    .GetDisplaySnapshot("1").Select(row => row.row));

                sheet.SetValue(2, 0, "1");
                Assert.All(InputFixMemberService.Instance.GetDisplaySnapshot("1"),
                    row => Assert.Equal("1", row.m));
            }
            finally
            {
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
            }
        });
    }

    [Fact]
    public void DeletingOutlineSummaryRemovesOnlyThatMembersRows()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            using var document = JsonDocument.Parse(SavedInput(
                """
                [{"row":1,"m":"1","tx":10},
                 {"row":2,"m":"2","tx":20},
                 {"row":3,"m":"1","tx":30}]
                """));
            try
            {
                input.JsonDataOpen(document.RootElement);
                using var form = new System.Windows.Forms.Form { Width = 700, Height = 350 };
                using var component = new InputFixMemberComponent();
                form.Controls.Add(component);
                form.Show();
                System.Windows.Forms.Application.DoEvents();
                var spread = Assert.IsType<FrameWebforCS.components.myFpSpread>(
                    component.Controls.OfType<FpSpread>().Single());
                var sheet = spread.Sheets[0];
                var outline = Assert.Single(sheet.GetRangeGroupInfo(1, true));
                sheet.ExpandRangeGroup(outline, true, false);
                sheet.SetActiveCell(0, 0);
                Assert.True(LoadSheetTest.Key(spread, System.Windows.Forms.Keys.Oem5).SuppressKeyPress);
                Assert.Equal("1", InputFixMemberService.Instance.GetEditorRows("1")
                    .Single(row => row.row == 4).m);
                outline = Assert.Single(sheet.GetRangeGroupInfo(1, true));
                sheet.ExpandRangeGroup(outline, true, false);
                LoadSheetTest.MarkRowHeader(spread);
                sheet.ClearSelection();
                sheet.AddSelection(0, -1, 1, -1);
                Assert.True(LoadSheetTest.Key(spread, System.Windows.Forms.Keys.Delete).SuppressKeyPress);
                var remaining = Assert.Single(InputFixMemberService.Instance.GetDisplaySnapshot("1"));
                Assert.Equal("2", remaining.m);
                Assert.Equal(1, remaining.row);
                Assert.Equal(20f, remaining.tx);
                Assert.True(component.SelectGridRow(1, "tx", "1"));
            }
            finally
            {
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
            }
        });
    }

    private static string SavedInput(string springRows) => """
        {"dimension":3,
         "node":{"1":{"x":0,"y":0,"z":0},"2":{"x":5,"y":0,"z":0},"3":{"x":10,"y":0,"z":0}},
         "member":{"1":{"ni":"1","nj":"2","e":"1"},"2":{"ni":"2","nj":"3","e":"1"}},
         "element":{"1":{"1":{"E":200000,"A":1,"Iz":1}}},
         "fix_node":{"1":[{"row":1,"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]},
         "fix_member":{"1":
        """ + springRows + """
         },
         "load":{"1":{"symbol":"DL","fix_node":1,"fix_member":1,"element":1,
            "load_node":[{"row":1,"n":"2","tx":1}]}}}
        """;

    private static void RunSta(System.Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "Spring grid test timed out.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}

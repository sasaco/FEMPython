using FarPoint.Win.Spread;
using FrameWebforCS.components;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.three;
using SingleFormsDemo;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputLoadFixedRowsTests
{
    [Fact]
    public void EmptyClearReloadAndCaseNameChangesPreserveFixedListAndAnonymousRows()
    {
        LoadSheetTest.WithLoads(service =>
        {
            var rows = service.IntensityRows;
            void AssertFixed()
            {
                Assert.Same(rows, service.IntensityRows);
                Assert.Equal(100000, rows.Count);
                Assert.All(rows, row =>
                {
                    Assert.False(row.IsAssigned);
                    Assert.Equal("", row.LoadId);
                    Assert.Null(row.n);
                    Assert.Null(row.tx);
                    Assert.Null(row.m1);
                    Assert.Null(row.P1);
                });
            }
            AssertFixed();
            LoadSheetTest.Load(service, """{"load":{"2":{"name":"named only"}}}""");
            AssertFixed();
            service.SelectCase("2");
            service.LoadNames[1].name = "renamed";
            AssertFixed();
            service.clear();
            AssertFixed();
            LoadSheetTest.Load(service, "{}");
            AssertFixed();
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(99999)]
    public void InsertAtFirstMiddleGapOrLastShiftsDisplayAndKeepsCaseLocalIdentity(int index)
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
            var rows = service.IntensityRows;
            var before = rows.Take(6).Select(LoadSheetTest.Payload).ToArray();
            int notifications = 0;
            int edits = 0;
            void Changed(object? _, ListChangedEventArgs e)
            {
                Assert.Same(rows, service.IntensityRows);
                Assert.Equal(100000, rows.Count);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                notifications++;
            }
            void Edited() => edits++;
            rows.ListChanged += Changed;
            service.LoadsEdited += Edited;
            long revision = InputDataService.Instance.CalculationInputRevision;
            try
            {
                Assert.True(service.InsertIntensityRowAt(index));
                Assert.False(rows[index].IsAssigned);
                Assert.Equal("", rows[index].LoadId);
                for (int i = 0; i < 6; i++)
                    Assert.Equal(before[i], LoadSheetTest.Payload(rows[i < index ? i : i + 1]));
                Assert.Equal(1, notifications);
                Assert.Equal(1, edits);
                Assert.Equal(revision + 1, InputDataService.Instance.CalculationInputRevision);
            }
            finally { rows.ListChanged -= Changed; service.LoadsEdited -= Edited; }
        });
    }

    [Fact]
    public void DisjointBulkDeleteRemovesBothFamiliesOnceAndKeepsSurvivorOrder()
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, """
                {"load":{"1":{"input_rows":[1,3,4],"load_node":[{"row":1,"n":"11"},{"row":3,"n":"13"},{"row":4,"n":"14"}],
                  "load_member":[{"row":3,"m1":"23","P1":3}]},
                  "2":{"input_rows":[1,2],"load_node":[{"row":1,"n":"21"},{"row":2,"n":"22"}]}},
                  "load_intensity_layout":{"version":1,"rows":[{"slot":1,"case_id":"1","row":1},
                    {"slot":2,"case_id":"2","row":1},{"slot":4,"case_id":"1","row":3},
                    {"slot":5,"case_id":"2","row":2},{"slot":6,"case_id":"1","row":4}]}}
                """);
            int edits = 0;
            void Edited() => edits++;
            service.LoadsEdited += Edited;
            try
            {
                Assert.True(service.DeleteIntensityRowsAt([1, 3, 3]));
                Assert.Equal("11", service.IntensityRows[0].n);
                Assert.False(service.IntensityRows[1].IsAssigned); // Surviving anonymous gap.
                Assert.Equal("22", service.IntensityRows[2].n);
                Assert.Equal(("2", 1), (service.IntensityRows[2].CaseId, service.IntensityRows[2].Row));
                Assert.Equal("14", service.IntensityRows[3].n);
                Assert.Equal(("1", 3), (service.IntensityRows[3].CaseId, service.IntensityRows[3].Row));
                Assert.Equal(new[] { 0, 2, 3 }, service.AssignedIntensityRowIndices.Order().ToArray());
                Assert.Empty(service.GetDisplaySnapshot()["1"].MemberLoads);
                Assert.All(service.IntensityRows.Skip(99998), row => Assert.False(row.IsAssigned));
                Assert.Equal(100000, service.IntensityRows.Count);
                Assert.Equal(1, edits);
            }
            finally { service.LoadsEdited -= Edited; }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OccupiedTailRejectsInsertionWithoutAnyPublishedChange(bool withValue)
    {
        LoadSheetTest.WithLoads(service =>
        {
            string json = JsonSerializer.Serialize(new
            {
                load = new Dictionary<string, object> { ["2"] = withValue
                    ? new { input_rows = new[] { 7 }, load_node = new[] { new { row = 7, n = "9", tx = 0f } } }
                    : (object)new { input_rows = new[] { 7 } } },
                load_intensity_layout = new { version = 1, rows = new[] { new { slot = 100000, case_id = "2", row = 7 } } }
            });
            LoadSheetTest.Load(service, json);
            service.SelectCase("2");
            string before = LoadSheetTest.Save(service);
            var rows = service.IntensityRows;
            long revision = InputDataService.Instance.CalculationInputRevision;
            int notifications = 0;
            int edits = 0;
            void Changed(object? _, ListChangedEventArgs __) => notifications++;
            void Edited() => edits++;
            rows.ListChanged += Changed;
            service.LoadsEdited += Edited;
            try
            {
                Assert.False(service.InsertIntensityRowAt(0));
                Assert.False(service.InsertIntensityRowAt(99999));
                Assert.Equal(before, LoadSheetTest.Save(service));
                Assert.Equal("2", service.SelectedCaseId);
                Assert.Same(rows, service.IntensityRows);
                Assert.Equal(revision, InputDataService.Instance.CalculationInputRevision);
                Assert.Equal(0, notifications);
                Assert.Equal(0, edits);
            }
            finally { rows.ListChanged -= Changed; service.LoadsEdited -= Edited; }
        });
    }

    [Fact]
    public void EditingAnonymousRowsAllocatesOnlyOnValidInputAndUsesFreeSparseNumberAtLimit()
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, """{"load":{"2":{"input_rows":[2,100000]}}}""");
            service.SelectCase("2");
            var blank = service.IntensityRows[4];
            blank.LoadId = "0";
            Assert.False(blank.IsAssigned);
            blank.tx = 0;
            Assert.True(blank.IsAssigned);
            Assert.Equal("2", blank.CaseId);
            Assert.Equal(1, blank.Row);
            Assert.Equal(0f, blank.tx);
            Assert.Equal(4, service.FindIntensityRowIndex("2", 1));
            service.IntensityRows[7].LoadId = "3";
            Assert.True(service.IntensityRows[7].IsAssigned);
            Assert.Equal(("3", 1), (service.IntensityRows[7].CaseId, service.IntensityRows[7].Row));
            Assert.Null(service.IntensityRows[7].tx);
        });
    }

    [Fact]
    public void CaseMoveRenumbersReferencesWithoutReorderingDisplaySlots()
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, """
                {"load":{"1":{"input_rows":[3,4],"load_node":[{"row":3,"n":"13"},{"row":4,"n":"14"}],
                  "load_member":[{"row":3,"m1":"23","P1":4}]},
                  "2":{"load_node":[{"row":3,"n":"33"}]}},
                  "load_intensity_layout":{"version":1,"rows":[{"slot":2,"case_id":"2","row":3},
                    {"slot":4,"case_id":"1","row":3},{"slot":7,"case_id":"1","row":4}]}}
                """);
            Assert.True(service.MoveIntensityRow(service.IntensityRows[3], "2"));
            Assert.Equal("33", service.IntensityRows[1].n);
            Assert.Equal(4, service.IntensityRows[1].Row);
            Assert.Equal("13", service.IntensityRows[3].n);
            Assert.Equal("23", service.IntensityRows[3].m1);
            Assert.Equal(("2", 3), (service.IntensityRows[3].CaseId, service.IntensityRows[3].Row));
            Assert.Equal("14", service.IntensityRows[6].n);
            Assert.Equal(3, service.IntensityRows[6].Row);
            Assert.Equal(1, service.FindIntensityRowIndex("2", 4));
            Assert.Equal(6, service.FindIntensityRowIndex("1", 3));
            Assert.False(service.IntensityRows[2].IsAssigned);
        });
    }

    [Fact]
    public void RealKeyPipelineShiftsRowsAcrossCasesAndAnonymousSelectionPublishesNoLoad()
    {
        LoadSheetTest.RunSta(() => LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
            using var form = LoadSheetTest.ShowSheet(out var component, out var spread);
            using (component)
            {
                var sheet = spread.ActiveSheet;
                sheet.SetActiveCell(0, 10);
                var selected = new List<(int, string)>();
                component.GridSelectionChanged += (row, field) => selected.Add((row, field));
                string selectedCase = service.SelectedCaseId;
                float[] widths = Enumerable.Range(0, sheet.ColumnCount).Select(c => sheet.Columns[c].Width).ToArray();
                Assert.True(LoadSheetTest.Key(spread, Keys.Oem5).SuppressKeyPress);
                Application.DoEvents();
                Assert.Equal(0, sheet.ActiveRowIndex);
                Assert.Equal(10, sheet.ActiveColumnIndex);
                Assert.False(service.IntensityRows[0].IsAssigned);
                Assert.Equal("2", sheet.Cells[1, 0].Text);
                Assert.Equal("1", sheet.Cells[3, 0].Text);
                Assert.Equal(100000, sheet.RowCount);
                Assert.Equal(widths, Enumerable.Range(0, sheet.ColumnCount).Select(c => sheet.Columns[c].Width).ToArray());
                selected.Clear();
                LoadSheetTest.EnterCell(spread, 0, 10);
                Assert.Empty(selected);
                Assert.Equal(selectedCase, service.SelectedCaseId);

                LoadSheetTest.MarkRowHeader(component, spread);
                sheet.ClearSelection();
                sheet.AddSelection(1, -1, 1, -1);
                sheet.AddSelection(3, -1, 1, -1);
                Assert.True(LoadSheetTest.Key(spread, Keys.Delete).SuppressKeyPress);
                Application.DoEvents();
                Assert.Equal(1, sheet.ActiveRowIndex);
                Assert.False(service.IntensityRows[0].IsAssigned);
                Assert.False(service.IntensityRows[1].IsAssigned);
                Assert.Equal(("2", 6), (service.IntensityRows[3].CaseId, service.IntensityRows[3].Row));
                Assert.Null(service.IntensityRows[3].n);
                Assert.Equal(100000, sheet.RowCount);
                Assert.Null(service.IntensityRows[^1].n);
            }
        }));
    }

    [Fact]
    public void RealEditorCancelAndModifiedKeysDoNotAssignOrShiftAnonymousRows()
    {
        LoadSheetTest.RunSta(() => LoadSheetTest.WithLoads(service =>
        {
            using var form = LoadSheetTest.ShowSheet(out var component, out var spread);
            using (component)
            {
                var sheet = spread.ActiveSheet;
                sheet.SetActiveCell(5, 9);
                spread.Focus();
                spread.StartCellEditing(EventArgs.Empty, false);
                Assert.True(spread.EditMode);
                spread.EditingControl.Text = "9";
                Assert.False(LoadSheetTest.Key(spread, Keys.Oem5).SuppressKeyPress);
                Assert.True(spread.EditMode);
                Assert.False(service.IntensityRows[5].IsAssigned);
                spread.CancelCellEditing();
                Assert.False(spread.EditMode);
                Assert.False(service.IntensityRows[5].IsAssigned);
                Assert.False(LoadSheetTest.Key(spread, Keys.Control | Keys.Oem5).SuppressKeyPress);
                Assert.False(LoadSheetTest.Key(spread, Keys.Shift | Keys.Oem102).SuppressKeyPress);
                Assert.Empty(service.AssignedIntensityRowIndices);
                Assert.True(LoadSheetTest.Key(spread, Keys.Oem102).SuppressKeyPress);
                Assert.Equal(100000, sheet.RowCount);
            }
        }));
    }

    [Fact]
    public void CellDeleteAndSheetOneKeysNeverDeleteIntensityRows()
    {
        LoadSheetTest.RunSta(() => LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
            using var form = LoadSheetTest.ShowSheet(out var component, out var spread);
            using (component)
            {
                var sheet = spread.ActiveSheet;
                sheet.SetActiveCell(0, 10);
                sheet.ClearSelection();
                sheet.AddSelection(0, 0, 1, sheet.ColumnCount);
                Assert.True(LoadSheetTest.Key(spread, Keys.Delete).SuppressKeyPress);
                Assert.True(service.IntensityRows[0].IsAssigned);
                Assert.Null(service.IntensityRows[0].tx);
                Assert.Equal("21", service.IntensityRows[0].n);
                Assert.Equal(100000, sheet.RowCount);
                sheet.Columns[9].Locked = true;
                sheet.Protect = true;
                sheet.SetActiveCell(0, 9);
                LoadSheetTest.Key(spread, Keys.Delete);
                Assert.Equal("21", service.IntensityRows[0].n);
                string before = LoadSheetTest.Save(service);
                spread.ActiveSheetIndex = 0;
                Assert.False(LoadSheetTest.Key(spread, Keys.Oem5).SuppressKeyPress);
                Assert.Equal(before, LoadSheetTest.Save(service));
            }
        }));
    }

    [Fact]
    public void TailRefusalThroughRealKeyPipelineKeepsActiveCellSelectionsAndSave()
    {
        LoadSheetTest.RunSta(() => LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, """
                {"load":{"2":{"input_rows":[1]}},"load_intensity_layout":{"version":1,
                  "rows":[{"slot":100000,"case_id":"2","row":1}]}}
                """);
            using var form = LoadSheetTest.ShowSheet(out var component, out var spread);
            using (component)
            {
                var sheet = spread.ActiveSheet;
                sheet.SetActiveCell(6, 10);
                sheet.AddSelection(6, 10, 2, 1);
                var beforeSelection = sheet.GetSelections().Select(r => (r.Row, r.Column, r.RowCount, r.ColumnCount)).ToArray();
                string before = LoadSheetTest.Save(service);
                string caseId = service.SelectedCaseId;
                LoadSheetTest.Key(spread, Keys.Oem5);
                Application.DoEvents();
                Assert.Equal((6, 10), (sheet.ActiveRowIndex, sheet.ActiveColumnIndex));
                Assert.Equal(beforeSelection, sheet.GetSelections().Select(r => (r.Row, r.Column, r.RowCount, r.ColumnCount)).ToArray());
                Assert.Equal(before, LoadSheetTest.Save(service));
                Assert.Equal(caseId, service.SelectedCaseId);
            }
        }));
    }

    [Fact]
    public void ShiftDimensionReloadAndDisposedFormClearStaleBlueColors()
    {
        LoadSheetTest.RunSta(() => LoadSheetTest.WithLoads(service =>
        {
            int priorDimension = InputDataService.Instance.dimension;
            try
            {
                LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
                using (var form = LoadSheetTest.ShowSheet(out var component, out var spread))
                using (component)
                {
                    var sheet = spread.ActiveSheet;
                    Assert.Equal(System.Drawing.Color.AliceBlue, sheet.Rows[0].BackColor);
                    service.InsertIntensityRowAt(0);
                    Application.DoEvents();
                    Assert.NotEqual(System.Drawing.Color.AliceBlue, sheet.Rows[0].BackColor);
                    Assert.Equal(System.Drawing.Color.AliceBlue, sheet.Rows[1].BackColor);
                    InputDataService.Instance.SetDimension(2);
                    component.RefreshDimension();
                    Assert.Equal(100000, sheet.RowCount);
                    InputDataService.Instance.SetDimension(3);
                    component.RefreshDimension();
                    Assert.Equal(100000, sheet.RowCount);
                    LoadSheetTest.Load(service, "{}");
                    Application.DoEvents();
                    Assert.NotEqual(System.Drawing.Color.AliceBlue, sheet.Rows[1].BackColor);
                }
                LoadSheetTest.Load(service, LoadSheetTest.MixedLayout); // Detached disposed Spread must not receive Reset.
                using var second = LoadSheetTest.ShowSheet(out var recreated, out var nextSpread);
                using (recreated) Assert.Equal(100000, nextSpread.ActiveSheet.RowCount);
            }
            finally { InputDataService.Instance.SetDimension(priorDimension); }
        }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeferredCaseMoveSelectsMovedIdentityButDoesNotOverrideNewerNavigation(bool navigate)
    {
        LoadSheetTest.RunSta(() => LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
            using var form = LoadSheetTest.ShowSheet(out var component, out var spread);
            using (component)
            {
                var sheet = spread.ActiveSheet;
                sheet.SetActiveCell(2, 0);
                spread.Focus();
                spread.StartCellEditing(EventArgs.Empty, false);
                Assert.True(spread.EditMode);
                spread.EditingControl.Text = "2";
                spread.StopCellEditing();
                Assert.Equal(("2", 4), (service.IntensityRows[2].CaseId, service.IntensityRows[2].Row));
                if (navigate)
                {
                    sheet.SetActiveCell(6, 9);
                    LoadSheetTest.EnterCell(spread, 6, 9);
                }
                Application.DoEvents();
                Assert.Equal(navigate ? (6, 9) : (2, 0), (sheet.ActiveRowIndex, sheet.ActiveColumnIndex));
                Assert.Equal(100000, sheet.RowCount);
            }
        }));
    }

    [Fact]
    public void ThreeDSelectionUsesCaseLocalRowAfterDisplayShiftAndBlankSelectionDoesNotPublish()
    {
        LoadSheetTest.RunSta(() =>
        {
            var input = InputDataService.Instance;
            var routing = AppRoutingModule.Instance;
            using var form = LoadSheetTest.ShowSheet(out var component, out var spread);
            routing.myComponents.Add(component);
            try
            {
                routing.NotifyInputMode("load");
                using var viewport = new ThreeService(new SceneService());
                using var document = JsonDocument.Parse("""
                    {"node":{"1":{"x":0}},"load":{"1":{"load_node":[{"row":1,"n":"1","tx":3}]},
                      "2":{"load_node":[{"row":7,"n":"1","tx":9}]}},
                     "load_intensity_layout":{"version":1,"rows":[{"slot":3,"case_id":"1","row":1},
                       {"slot":6,"case_id":"2","row":7}]}}
                    """);
                input.JsonDataOpen(document.RootElement);
                viewport.FlushPending();
                var loads = Assert.IsType<ThreeLoadsService>(typeof(ThreeService)
                    .GetField("_loads", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(viewport));
                LoadSheetTest.EnterCell(spread, 5, 10);
                viewport.FlushPending();
                Assert.Equal("2", InputLoadService.Instance.SelectedCaseId);
                Assert.Equal((7, "tx"), loads.Selection);
                Assert.Equal("load", viewport.SelectedKind);
                Assert.True(InputLoadService.Instance.InsertIntensityRowAt(0));
                viewport.FlushPending();
                Assert.True(component.SelectGridRow(7, "tx", "2"));
                Assert.Equal(6, spread.ActiveSheet.ActiveRowIndex);
                LoadSheetTest.EnterCell(spread, 6, 10);
                viewport.FlushPending();
                Assert.Equal((7, "tx"), loads.Selection);
                LoadSheetTest.EnterCell(spread, 0, 10);
                viewport.FlushPending();
                Assert.Equal((7, "tx"), loads.Selection);
                Assert.Equal("2", InputLoadService.Instance.SelectedCaseId);
            }
            finally
            {
                routing.myComponents.Remove(component);
                component.Dispose();
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
                routing.NotifyInputMode("node");
            }
        });
    }
}

internal static class LoadSheetTest
{
    internal const string MixedLayout = """
        {"load":{"1":{"load_node":[{"row":4,"n":"14","tx":4}]},
          "2":{"input_rows":[1,7],"load_node":[{"row":1,"n":"21","tx":1,"ty":2,"tz":3,"rx":4,"ry":5,"rz":6}],
            "load_member":[{"row":1,"m1":"31","m2":"32","direction":"GY","mark":"2","L1":"1","L2":"2","P1":3,"P2":4}]}},
          "load_intensity_layout":{"version":1,"rows":[{"slot":1,"case_id":"2","row":1},
            {"slot":3,"case_id":"1","row":4},{"slot":5,"case_id":"2","row":7}]}}
        """;

    internal static string Payload(clsLoadIntensityRow row) => JsonSerializer.Serialize(new
    {
        row.CaseId, row.Row, row.IsAssigned, row.n, row.tx, row.ty, row.tz, row.rx, row.ry, row.rz,
        row.m1, row.m2, row.direction, row.mark, row.L1, row.L2, row.P1, row.P2
    });
    internal static void Load(InputLoadService service, string json)
    {
        using var document = JsonDocument.Parse(json);
        service.setLoadJson(document.RootElement);
    }
    internal static string Save(InputLoadService service) => JsonSerializer.Serialize(new
    {
        load = service.getLoadJson(), load_intensity_layout = service.GetIntensityLayoutJson()
    });
    internal static void WithLoads(System.Action<InputLoadService> test)
    {
        var service = InputLoadService.Instance;
        service.clear();
        service.SelectCase("1");
        try { test(service); }
        finally { service.clear(); service.SelectCase("1"); }
    }
    internal static void RunSta(System.Action test)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { test(); } catch (Exception exception) { error = exception; } })
            { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(90)), "Load sheet STA test timed out.");
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
    internal static Form ShowSheet(out InputLoadComponent component, out myFpSpread spread)
    {
        var form = new Form { Width = 1100, Height = 500, ShowInTaskbar = false };
        component = new InputLoadComponent { Dock = DockStyle.Fill };
        form.Controls.Add(component);
        form.Show();
        spread = Assert.IsType<myFpSpread>(component.Controls.OfType<FpSpread>().Single());
        spread.ActiveSheetIndex = 1;
        Application.DoEvents();
        return form;
    }
    internal static KeyEventArgs Key(myFpSpread spread, Keys key)
    {
        var args = new KeyEventArgs(key);
        typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(spread, [args]);
        return args;
    }
    internal static void EnterCell(FpSpread spread, int row, int column) =>
        typeof(FpSpread).GetMethod("OnEnterCell", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(spread, [new EnterCellEventArgs(new SpreadView(spread), row, column)]);
    internal static void MarkRowHeader(InputLoadComponent component, FpSpread spread)
    {
        var point = (from y in Enumerable.Range(0, Math.Min(spread.Height, 250))
                     from x in Enumerable.Range(0, Math.Min(spread.Width, 70))
                     where spread.HitTest(x, y).Type == HitTestType.RowHeader
                     select (X: x, Y: y)).First();
        typeof(InputLoadComponent).GetMethod("OnSpreadMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(component, [spread, new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0)]);
    }
}

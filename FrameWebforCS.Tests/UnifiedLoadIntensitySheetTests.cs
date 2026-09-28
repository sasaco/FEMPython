using FarPoint.Win.Spread;
using FrameWebforCS.components;
using FrameWebforCS.components.input;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class UnifiedLoadIntensitySheetTests
{
    [Fact]
    public void LegacySparseRowsFromAllCasesShareOneListAndRoundTripWithEmptyRows()
    {
        WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"name":"first","load_node":[{"row":1,"n":"7","tx":3}],
                               "load_member":[{"row":4,"m1":"8","mark":"2","P1":9}]},
                         "2":{"name":"second","load_node":[{"row":1,"n":"9","ty":4}]}}}
                """);

            AssertRow(service, "1", 1, node: "7");
            AssertRow(service, "1", 4, member: "8");
            AssertRow(service, "2", 1, node: "9");
            service.SelectCase("2");
            AssertRow(service, "1", 1, node: "7");
            AssertRow(service, "2", 1, node: "9");

            Assert.True(service.InsertIntensityRow("2", 2));
            AssertRow(service, "2", 2);
            string saved = Save(service);
            using (var document = JsonDocument.Parse(saved))
            {
                var cases = document.RootElement.GetProperty("load");
                // A legacy case with no blank rows keeps its legacy save shape.
                Assert.False(cases.GetProperty("1").TryGetProperty("input_rows", out _));
                Assert.Equal(new[] { 1, 4 }, SavedLoadRows(cases.GetProperty("1")));
                Assert.Equal(new[] { 1, 2 }, RowNumbers(cases.GetProperty("2")));
                Assert.Single(cases.GetProperty("2").GetProperty("load_node").EnumerateArray());
            }

            Load(service, saved);
            AssertRow(service, "1", 4, member: "8");
            AssertRow(service, "2", 1, node: "9");
            AssertRow(service, "2", 2);
            Assert.Empty(service.GetDisplaySnapshot()["2"].MemberLoads);
        });
    }

    [Fact]
    public void CaseMoveCarriesBothLoadFamiliesAndInsertsAtOriginalRowNumber()
    {
        WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"input_rows":[1,3,4],
                               "load_node":[{"row":3,"n":"13","tx":3},{"row":4,"n":"14","tx":4}],
                               "load_member":[{"row":3,"m1":"23","mark":"2","P1":5}]},
                         "2":{"input_rows":[1,3,4],
                               "load_node":[{"row":3,"n":"33","tx":7},{"row":4,"n":"34","tx":8}]}}}
                """);
            var source = AssertRow(service, "1", 3, node: "13", member: "23");

            Assert.True(service.MoveIntensityRow(source, "2"));

            AssertRow(service, "1", 3, node: "14");
            AssertRow(service, "1", 4); // Input starter, not the former row-four load.
            AssertRow(service, "2", 3, node: "13", member: "23");
            AssertRow(service, "2", 4, node: "33");
            AssertRow(service, "2", 5, node: "34");

            string saved = Save(service);
            Load(service, saved);
            AssertRow(service, "2", 3, node: "13", member: "23");
            AssertRow(service, "2", 4, node: "33");
            AssertRow(service, "1", 3, node: "14");
        });
    }

    [Fact]
    public void InsertionAndDisjointBulkDeletionShiftEachCaseOnlyOnce()
    {
        WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"load_node":[{"row":1,"n":"11","tx":1},
                                            {"row":2,"n":"12","tx":2},
                                            {"row":3,"n":"13","tx":3},
                                            {"row":4,"n":"14","tx":4}]},
                         "2":{"load_node":[{"row":1,"n":"21","tx":1},
                                            {"row":2,"n":"22","tx":2},
                                            {"row":3,"n":"23","tx":3}]}}}
                """);

            Assert.True(service.InsertIntensityRow("1", 2));
            AssertRow(service, "1", 2);
            AssertRow(service, "1", 3, node: "12");
            AssertRow(service, "1", 5, node: "14");
            AssertRow(service, "2", 2, node: "22");

            Assert.True(service.DeleteIntensityRows([
                ("1", 4), ("2", 1), ("1", 2), ("1", 4)
            ]));

            AssertRow(service, "1", 1, node: "11");
            AssertRow(service, "1", 2, node: "12");
            AssertRow(service, "1", 3, node: "14");
            AssertRow(service, "1", 4); // Unsaved input starter.
            AssertRow(service, "2", 1, node: "22");
            AssertRow(service, "2", 2, node: "23");
            AssertRow(service, "2", 3); // Unsaved input starter.
        });
    }

    [Fact]
    public void InvalidSavedRowMetadataRejectsTheFileWithoutReplacingCurrentLoads()
    {
        WithLoads(service =>
        {
            Load(service, """{"load":{"1":{"load_node":[{"row":1,"n":"7","tx":1}]}}}""");
            string before = Save(service);
            foreach (string invalid in new[]
            {
                """{"load":{"2":{"input_rows":{}}}}""",
                """{"load":{"2":{"input_rows":[1,1]}}}""",
                """{"load":{"2":{"input_rows":[0]}}}""",
                """{"load":{"2":{"input_rows":[100001]}}}""",
                """{"load":{"2":{"input_rows":[1.5]}}}""",
                """{"load":{"2":{"input_rows":[2],"load_node":[{"row":1,"n":"8"}]}}}"""
            })
            {
                using var document = JsonDocument.Parse(invalid);
                Assert.Throws<JsonException>(() => service.setLoadJson(document.RootElement));
                Assert.Equal(before, Save(service));
            }
        });
    }

    [Fact]
    public void NullRowMetadataUsesLegacyLoadRowsAndPreservesSparsePosition()
    {
        WithLoads(service =>
        {
            Load(service, """
                {"load":{"3":{"input_rows":null,
                               "load_node":[{"row":7,"n":"31","tx":2}]}}}
                """);
            AssertRow(service, "3", 7, node: "31");
            string saved = Save(service);
            using (var document = JsonDocument.Parse(saved))
            {
                var loadCase = document.RootElement.GetProperty("load").GetProperty("3");
                Assert.False(loadCase.TryGetProperty("input_rows", out _));
                Assert.Equal(7, Assert.Single(loadCase.GetProperty("load_node").EnumerateArray())
                    .GetProperty("row").GetInt32());
            }
            Load(service, saved);
            AssertRow(service, "3", 7, node: "31");
        });
    }

    [Fact]
    public void InvalidRowOperationsDoNotPartiallyShiftOrOverwriteLoads()
    {
        WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"input_rows":[1,100000],
                               "load_node":[{"row":1,"n":"11","tx":1},
                                            {"row":100000,"n":"12","tx":2}]},
                         "2":{"input_rows":[100000],
                               "load_node":[{"row":100000,"n":"21","tx":3}]}}}
                """);
            string before = Save(service);
            var source = AssertRow(service, "1", 100000, node: "12");
            source.LoadId = "0";
            Assert.Equal("1", source.CaseId);
            Assert.Equal("1", source.LoadId);
            source.LoadId = "2"; // Destination row 100000 cannot be shifted.
            Assert.Equal("1", source.CaseId);
            Assert.Equal("1", source.LoadId);
            Assert.False(service.InsertIntensityRow("1", 1));
            Assert.False(service.InsertIntensityRow("1", 0));
            Assert.False(service.InsertIntensityRow("0", 1));
            Assert.False(service.DeleteIntensityRows([("1", 1), ("2", 99999)]));
            Assert.False(service.MoveIntensityRow(AssertRow(service, "1", 100000, node: "12"), "2"));
            Assert.Equal(before, Save(service));
            AssertRow(service, "1", 1, node: "11");
            AssertRow(service, "1", 100000, node: "12");
            AssertRow(service, "2", 100000, node: "21");
        });
    }

    [Fact]
    public void OneIntensitySheetDisplaysBothCaseIdsAndFindsTheCorrectCaseLocalRow()
    {
        RunSta(() => WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"load_node":[{"row":1,"n":"11","tx":1}]},
                         "2":{"load_node":[{"row":1,"n":"21","tx":2}]}}}
                """);
            using var component = new InputLoadComponent();
            Assert.Empty(component.Controls.OfType<ComboBox>());
            var spread = component.Controls.OfType<FpSpread>().Single();
            component.CreateControl();
            spread.ActiveSheetIndex = 1;
            var sheet = spread.ActiveSheet;
            int first = service.FindIntensityRowIndex("1", 1);
            int second = service.FindIntensityRowIndex("2", 1);
            Assert.True(first >= 0 && second > first);
            Assert.Equal("荷重強度", sheet.SheetName);
            Assert.Equal("1", sheet.Cells[first, 0].Text);
            Assert.Equal("2", sheet.Cells[second, 0].Text);

            Assert.True(component.SelectGridRow(1, "ty", "2"));
            Assert.Equal(second, sheet.ActiveRowIndex);
            Assert.Equal(11, sheet.ActiveColumnIndex);
            Assert.Equal("2", service.SelectedCaseId);
            Assert.Equal("1", sheet.Cells[first, 0].Text);

            var selected = new List<(int Row, string Column)>();
            component.GridSelectionChanged += (row, column) => selected.Add((row, column));
            service.SelectCase("1");
            var cases = new List<string>();
            void OnCaseChanged(string caseId) => cases.Add(caseId);
            service.SelectedCaseChanged += OnCaseChanged;
            try
            {
                typeof(FpSpread).GetMethod("OnEnterCell", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(spread, [new EnterCellEventArgs(new SpreadView(spread), second, 10)]);
                Assert.Contains((1, "tx"), selected);
                Assert.Equal("2", service.SelectedCaseId);
                Assert.Equal(new[] { "2" }, cases);

                spread.ActiveSheetIndex = 0;
                service.SelectCase("1");
                cases.Clear();
                spread.ActiveSheetIndex = 1;
                Assert.Equal("2", service.SelectedCaseId);
                Assert.Equal(new[] { "2" }, cases);
            }
            finally { service.SelectedCaseChanged -= OnCaseChanged; }
        }));
    }

    [Fact]
    public void EditingTheBoundCaseIdCellMovesTheWholeRowWithoutOverwritingDestination()
    {
        RunSta(() => WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"name":"source","load_node":[{"row":1,"n":"11","tx":3}],
                               "load_member":[{"row":1,"m1":"31","mark":"2","P1":5}]},
                         "2":{"load_node":[{"row":1,"n":"21","tx":4}]}}}
                """);
            using var component = new InputLoadComponent();
            var spread = Assert.IsType<myFpSpread>(component.Controls.OfType<FpSpread>().Single());
            component.CreateControl();
            spread.ActiveSheetIndex = 1;
            var sheet = spread.ActiveSheet;
            Assert.False(sheet.Columns[0].Locked);

            sheet.Cells[service.FindIntensityRowIndex("1", 1), 0].Value = "2";

            AssertRow(service, "2", 1, node: "11", member: "31");
            AssertRow(service, "2", 2, node: "21");
            Assert.Equal("2", service.SelectedCaseId);
            Assert.Equal("2", sheet.Cells[service.FindIntensityRowIndex("2", 1), 0].Text);
            using var document = JsonDocument.Parse(Save(service));
            var cases = document.RootElement.GetProperty("load");
            Assert.Equal("source", cases.GetProperty("1").GetProperty("name").GetString());
            Assert.Equal(new[] { 1, 2 }, SavedLoadRows(cases.GetProperty("2")));
        }));
    }

    [Fact]
    public void RowHeaderMultiSelectionDeleteRemovesDisjointRowsAcrossCases()
    {
        RunSta(() => WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"load_node":[{"row":1,"n":"11","tx":1},
                                            {"row":2,"n":"12","tx":2},
                                            {"row":3,"n":"13","tx":3}]},
                         "2":{"load_node":[{"row":1,"n":"21","tx":1},
                                            {"row":2,"n":"22","tx":2}]}}}
                """);
            using var form = new Form { Width = 1000, Height = 500 };
            using var component = new InputLoadComponent { Dock = DockStyle.Fill };
            form.Controls.Add(component);
            form.Show();
            var spread = Assert.IsType<myFpSpread>(component.Controls.OfType<FpSpread>().Single());
            spread.ActiveSheetIndex = 1;
            var sheet = spread.ActiveSheet;
            int first = service.FindIntensityRowIndex("1", 1);
            int third = service.FindIntensityRowIndex("1", 3);
            int otherCase = service.FindIntensityRowIndex("2", 1);
            sheet.ClearSelection();
            sheet.AddSelection(first, -1, 1, -1);
            sheet.AddSelection(third, -1, 1, -1);
            sheet.AddSelection(otherCase, -1, 1, -1);
            Assert.All(sheet.GetSelections(), range =>
            {
                Assert.Equal(-1, range.Column);
                Assert.Equal(-1, range.ColumnCount);
            });

            var headerPoint = (from y in Enumerable.Range(0, Math.Min(spread.Height, 250))
                               from x in Enumerable.Range(0, Math.Min(spread.Width, 70))
                               where spread.HitTest(x, y).Type == HitTestType.RowHeader
                               select (X: x, Y: y)).First();
            typeof(InputLoadComponent).GetMethod("OnSpreadMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component,
                [spread, new MouseEventArgs(MouseButtons.Left, 1, headerPoint.X, headerPoint.Y, 0)]);
            var delete = new KeyEventArgs(Keys.Delete);
            typeof(myFpSpread).GetMethod("myFpSpread_KeyDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(spread, [spread, delete]);

            Assert.True(delete.SuppressKeyPress);
            AssertRow(service, "1", 1, node: "12");
            AssertRow(service, "2", 1, node: "22");
            Assert.DoesNotContain(service.IntensityRows, row => row.n is "11" or "13" or "21");
        }));
    }

    [Fact]
    public void BackslashInsertsAtActiveCaseRowAndCellDeleteClearsOnlyTheValue()
    {
        RunSta(() => WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"load_node":[{"row":1,"n":"11","tx":1}]},
                         "2":{"load_node":[{"row":1,"n":"21","tx":2}]}}}
                """);
            using var component = new InputLoadComponent();
            var spread = Assert.IsType<myFpSpread>(component.Controls.OfType<FpSpread>().Single());
            component.CreateControl();
            spread.ActiveSheetIndex = 1;
            var sheet = spread.ActiveSheet;
            sheet.SetActiveCell(service.FindIntensityRowIndex("2", 1), 10);
            var insert = new KeyEventArgs(Keys.Oem5);
            typeof(InputLoadComponent).GetMethod("OnSpreadKeyDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, [spread, insert]);
            Assert.True(insert.SuppressKeyPress);
            AssertRow(service, "2", 1);
            AssertRow(service, "2", 2, node: "21");
            AssertRow(service, "1", 1, node: "11");

            int count = service.IntensityRows.Count;
            sheet.SetActiveCell(service.FindIntensityRowIndex("2", 2), 10);
            var delete = new KeyEventArgs(Keys.Delete);
            typeof(myFpSpread).GetMethod("myFpSpread_KeyDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(spread, [spread, delete]);
            Assert.True(delete.SuppressKeyPress);
            Assert.Equal(count, service.IntensityRows.Count);
            Assert.Equal("21", AssertRow(service, "2", 2, node: "21").n);
            Assert.Null(AssertRow(service, "2", 2, node: "21").tx);
            Assert.Equal(1, AssertRow(service, "1", 1, node: "11").tx);
        }));
    }

    [Fact]
    public void DeletingTheFinalCaseRowUpdatesSelectedCaseAndIntensitySheet()
    {
        RunSta(() => WithLoads(service =>
        {
            Load(service, """
                {"load":{"1":{"load_node":[{"row":1,"n":"11","tx":1}]},
                         "2":{"load_node":[{"row":1,"n":"21","tx":2}]}}}
                """);
            service.SelectCase("2");
            using var component = new InputLoadComponent();
            var spread = component.Controls.OfType<FpSpread>().Single();
            Assert.True(component.SelectGridRow(1, caseId: "2"));
            var sheet = spread.ActiveSheet;
            Assert.Equal("2", sheet.Cells[sheet.ActiveRowIndex, 0].Text);

            Assert.True(service.DeleteIntensityRows([("2", 1)]));
            Assert.Equal("1", service.SelectedCaseId);
            Assert.DoesNotContain("2", service.CaseIds);
            Assert.True(component.SelectGridRow(1, caseId: "1"));
            Assert.Equal("1", sheet.Cells[sheet.ActiveRowIndex, 0].Text);
            AssertRow(service, "1", 1, node: "11");
            using (var document = JsonDocument.Parse(Save(service)))
                Assert.False(document.RootElement.GetProperty("load").TryGetProperty("2", out _));

            Assert.True(service.DeleteIntensityRows([("1", 1)]));
            Assert.Empty(service.CaseIds);
            Assert.Equal("1", service.SelectedCaseId);
            Assert.True(component.SelectGridRow(1, caseId: "1"));
            Assert.Equal("1", sheet.Cells[sheet.ActiveRowIndex, 0].Text);
            AssertRow(service, "1", 1);
            using (var document = JsonDocument.Parse(Save(service)))
                Assert.Empty(document.RootElement.GetProperty("load").EnumerateObject());
        }));
    }

    private static clsLoadIntensityRow AssertRow(InputLoadService service, string caseId, int row,
        string? node = null, string? member = null)
    {
        int index = service.FindIntensityRowIndex(caseId, row);
        Assert.True(index >= 0, $"Missing load row {caseId}:{row}");
        var actual = Assert.IsType<clsLoadIntensityRow>(service.GetIntensityRowAt(index));
        Assert.Equal(caseId, actual.CaseId);
        Assert.Equal(caseId, actual.LoadId);
        Assert.Equal(row, actual.Row);
        Assert.Equal(node, actual.n);
        Assert.Equal(member, actual.m1);
        return actual;
    }

    private static int[] RowNumbers(JsonElement loadCase) =>
        loadCase.GetProperty("input_rows").EnumerateArray().Select(value => value.GetInt32()).ToArray();

    private static int[] SavedLoadRows(JsonElement loadCase) =>
        loadCase.GetProperty("load_node").EnumerateArray()
            .Concat(loadCase.GetProperty("load_member").EnumerateArray())
            .Select(value => value.GetProperty("row").GetInt32()).Distinct().Order().ToArray();

    private static void Load(InputLoadService service, string json)
    {
        using var document = JsonDocument.Parse(json);
        service.setLoadJson(document.RootElement);
    }

    private static string Save(InputLoadService service) =>
        JsonSerializer.Serialize(new { load = service.getLoadJson() });

    private static void WithLoads(System.Action<InputLoadService> test)
    {
        var service = InputLoadService.Instance;
        service.clear();
        service.SelectCase("1");
        try { test(service); }
        finally
        {
            service.clear();
            service.SelectCase("1");
        }
    }

    private static void RunSta(System.Action test)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { test(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Load sheet STA test timed out.");
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}

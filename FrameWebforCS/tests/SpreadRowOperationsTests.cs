using FarPoint.Win.Spread;
using FrameWebforCS.components;
using System.ComponentModel;
using System.Windows.Forms;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class SpreadRowOperationsTests
{
    [Fact]
    public void OnlyAnEnabledSheetHandlesPlainBackslash()
    {
        LoadSheetTest.RunSta(() =>
        {
            using var form = ShowSpread(out var spread, out var disabled, out var enabled);
            int insertCount = 0;
            spread.EnableRowOperations(enabled, (row, column) =>
            {
                Assert.Equal((0, 0), (row, column));
                insertCount++;
                return true;
            }, (_, _) => true);

            spread.ActiveSheetIndex = 0;
            disabled.SetActiveCell(0, 0);
            Assert.False(LoadSheetTest.Key(spread, Keys.Oem5).SuppressKeyPress);
            Assert.Equal(0, insertCount);

            spread.ActiveSheetIndex = 1;
            enabled.SetActiveCell(0, 0);
            Assert.True(LoadSheetTest.Key(spread, Keys.Oem5).SuppressKeyPress);
            Assert.True(LoadSheetTest.Key(spread, Keys.Oem102).SuppressKeyPress);
            Assert.Equal(2, insertCount);
            Assert.False(LoadSheetTest.Key(spread, Keys.Control | Keys.Oem5).SuppressKeyPress);
            Assert.Equal(2, insertCount);

            spread.DisableRowOperations(enabled);
            Assert.False(LoadSheetTest.Key(spread, Keys.Oem5).SuppressKeyPress);
            Assert.Equal(2, insertCount);
        });
    }

    [Fact]
    public void HeaderDeletePassesSelectedRowsAndRejectedDeleteDoesNotClearCell()
    {
        LoadSheetTest.RunSta(() =>
        {
            using var form = ShowSpread(out var spread, out _, out var enabled);
            IReadOnlyList<int>? deletedRows = null;
            spread.EnableRowOperations(enabled, (_, _) => true, (rows, column) =>
            {
                Assert.Equal(0, column);
                deletedRows = rows.ToArray();
                return false;
            });
            spread.ActiveSheetIndex = 1;
            enabled.SetActiveCell(0, 0);
            LoadSheetTest.MarkRowHeader(spread);
            enabled.ClearSelection();
            enabled.AddSelection(1, -1, 1, -1);
            enabled.AddSelection(3, -1, 1, -1);

            Assert.True(LoadSheetTest.Key(spread, Keys.Delete).SuppressKeyPress);
            Assert.Equal(new[] { 1, 3 }, deletedRows);
            Assert.Equal("seed", enabled.Cells[0, 0].Text);

            enabled.ClearSelection();
            enabled.AddSelection(0, 0, 1, 1);
            Assert.True(LoadSheetTest.Key(spread, Keys.Delete).SuppressKeyPress);
            Assert.Equal("", enabled.Cells[0, 0].Text);
        });
    }

    private static Form ShowSpread(out myFpSpread spread, out SheetView disabled,
        out SheetView enabled)
    {
        var form = new Form { Width = 500, Height = 300, ShowInTaskbar = false };
        spread = new myFpSpread { Dock = DockStyle.Fill };
        form.Controls.Add(spread);
        disabled = AddSheet(spread, "disabled");
        enabled = AddSheet(spread, "enabled");
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static SheetView AddSheet(myFpSpread spread, string name)
    {
        var sheet = spread.AddNewSheetView();
        sheet.SheetName = name;
        sheet.AutoGenerateColumns = false;
        sheet.ColumnCount = 1;
        sheet.Columns[0].DataField = nameof(BoundRow.Value);
        sheet.DataSource = new BindingList<BoundRow>
        {
            new() { Value = "seed" },
            new() { Value = "second" },
            new() { Value = "third" },
            new() { Value = "fourth" }
        };
        return sheet;
    }

    private sealed class BoundRow
    {
        public string? Value { get; set; }
    }
}

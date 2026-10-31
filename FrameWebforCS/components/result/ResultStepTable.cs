using FarPoint.Win.Spread;
using FrameWebforCS.calculation;
using System.Globalization;
using System.Windows.Forms;

namespace FrameWebforCS.components.result;

// GLOSSARY: 段階. The step table beside a result screen: step, λ and the control node's displacement
// of the nonlinear combination on the active tab. A row picks the step the tab shows; each tab keeps
// its own step and opens on the final one. Hidden for tabs without steps.
internal sealed class ResultStepTable : UserControl
{
    private readonly FpSpread _spread = new() { Dock = DockStyle.Fill };
    private CalculationResultPresentation? _presentation;
    private int[] _pages = [];
    private int _tab = -1;
    private bool _showing;

    private ResultStepTable()
    {
        Name = "resultStepTable";
        Dock = DockStyle.Left;
        Visible = false;
        Sheet = _spread.AddNewSheetView();
        Sheet.OperationMode = OperationMode.SingleSelect;
        Sheet.RowHeaderVisible = false;
        Sheet.DefaultStyle.Locked = true;
        Sheet.Protect = true;
        Sheet.ColumnCount = 3;
        Sheet.RowCount = 0;
        var header = Sheet.ColumnHeader;
        header.RowCount = 2;
        string[] titles = ["段階", "荷重の倍率", "制御節点"];
        string[] units = ["No", "λ", "変位(mm)"];
        int[] widths = [50, 80, 120];
        for (int column = 0; column < titles.Length; column++)
        {
            header.Cells[0, column].Text = titles[column];
            header.Cells[1, column].Text = units[column];
            Sheet.Columns[column].Width = widths[column];
            // Text as formatted, like the other result tables.
            Sheet.Columns[column].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
            Sheet.Columns[column].HorizontalAlignment = CellHorizontalAlignment.Right;
        }
        Width = widths.Sum() + 30;
        Controls.Add(_spread);
        _spread.EnterCell += (_, e) => { if (!_showing) Pick(e.Row); };
        _spread.SelectionChanged += (_, _) => { if (!_showing) Pick(Sheet.ActiveRowIndex); };
    }

    internal static ResultStepTable Attach(Control owner)
    {
        var table = new ResultStepTable();
        owner.Controls.Add(table);
        return table;
    }

    internal SheetView Sheet { get; }

    /// <summary>Raised when a row picks another step for the active tab.</summary>
    internal event System.Action? PageSelected;

    /// <summary>The page the tab shows, or -1 when there is no such tab.</summary>
    internal int Page(int tab) => tab >= 0 && tab < _pages.Length ? _pages[tab] : -1;

    internal void Reset(CalculationResultPresentation? presentation)
    {
        _presentation = presentation;
        _pages = presentation?.Tabs.Select(tab => tab.DefaultPageIndex).ToArray() ?? [];
        if (presentation is not null) Sheet.ColumnHeader.Cells[1, 2].Text = $"変位({presentation.DisplayLengthUnit})";
        ShowTab(-1);
    }

    internal void ShowTab(int tab)
    {
        _tab = tab;
        IReadOnlyList<CalculationResultStep> steps =
            _presentation is { } presentation && tab >= 0 && tab < presentation.Tabs.Count
                ? presentation.Tabs[tab].Steps : [];
        _showing = true;
        try
        {
            Sheet.RowCount = steps.Count;
            for (int row = 0; row < steps.Count; row++)
            {
                CalculationResultStep step = steps[row];
                Sheet.Cells[row, 0].Text = step.StepIndex.ToString(CultureInfo.CurrentCulture);
                Sheet.Cells[row, 1].Text = step.LoadFactor.ToString("F4", CultureInfo.CurrentCulture);
                Sheet.Cells[row, 2].Text = step.ControlDisplacement is double displacement
                    ? ResultPrintNumberFormat.Displacement(_presentation!.DisplayLength(displacement)) : "";
                if (step.PageIndex == _pages[tab]) Select(row);
            }
            Visible = steps.Count > 0;
        }
        finally { _showing = false; }
    }

    internal void SelectStep(int row)
    {
        _showing = true;
        try { Select(row); }
        finally { _showing = false; }
        Pick(row);
    }

    private void Select(int row)
    {
        Sheet.SetActiveCell(row, 0);
        Sheet.ClearSelection();
        Sheet.AddSelection(row, 0, 1, Sheet.ColumnCount);
        _spread.ShowActiveCell(VerticalPosition.Center, HorizontalPosition.Left);
    }

    private void Pick(int row)
    {
        if (_presentation is null || _tab < 0 || _tab >= _presentation.Tabs.Count) return;
        IReadOnlyList<CalculationResultStep> steps = _presentation.Tabs[_tab].Steps;
        if (row < 0 || row >= steps.Count || steps[row].PageIndex == _pages[_tab]) return;
        _pages[_tab] = steps[row].PageIndex;
        PageSelected?.Invoke();
    }
}

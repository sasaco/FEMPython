using FarPoint.Win.Spread;
using System;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputPanelComponent : UserControl
    {
        private readonly SheetView fpSpread1_Sheet1;
        private bool _suppressSelection;
        internal event Action<int?>? PanelSelected;

        public InputPanelComponent()
        {
            InitializeComponent();

            fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();
            fpSpread1_Sheet1.SheetName = "面要素";
            fpSpread1_Sheet1.AutoGenerateColumns = false;
            fpSpread1_Sheet1.DataAutoCellTypes = false;
            fpSpread1_Sheet1.DataAutoHeadings = false;
            fpSpread1_Sheet1.RowHeaderAutoText = HeaderAutoText.Numbers;
            fpSpread1_Sheet1.StartingRowNumber = 1;
            fpSpread1_Sheet1.ColumnCount = 5;
            fpSpread1_Sheet1.DataSource = InputPanelService.Instance.Panels;

            var header = fpSpread1_Sheet1.ColumnHeader;
            var columns = fpSpread1_Sheet1.Columns;
            string[] fields = { nameof(clsPanel.E), nameof(clsPanel.Point1), nameof(clsPanel.Point2),
                nameof(clsPanel.Point3), nameof(clsPanel.Point4) };
            string[] titles = { "材料 No", "節点 1", "節点 2", "節点 3", "節点 4" };
            for (int i = 0; i < fields.Length; i++)
            {
                header.Cells[0, i].Text = titles[i];
                columns[i].DataField = fields[i];
                columns[i].Width = 90;
            }

            Width = fields.Length * 90 + 100;
            fpSpread1.EnterCell += OnEnterCell;
            Disposed += (_, _) => fpSpread1.EnterCell -= OnEnterCell;
        }

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_suppressSelection || IsDisposed || e.Row < 0 || e.Column < 0) return;
            PanelSelected?.Invoke(e.Row + 1);
        }

        internal void SelectPanel(int? id)
        {
            if (IsDisposed || id is < 1 or > 100_000) return;
            _suppressSelection = true;
            try
            {
                if (id.HasValue) fpSpread1_Sheet1.SetActiveCell(id.Value - 1, 0);
                else fpSpread1_Sheet1.ClearSelection();
            }
            finally { _suppressSelection = false; }
        }
    }
}

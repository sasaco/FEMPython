using System;
using System.Drawing;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputPanelComponent : UserControl
    {
        private readonly DataGridView _grid;
        private bool _suppressSelection;
        internal event Action<int?>? PanelSelected;

        public InputPanelComponent()
        {
            InitializeComponent();
            // JS input-panel.component binds row, e and point-1..4. The desktop grid
            // uses the same row IDs and edits InputPanelService's sparse backing rows.
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersWidth = 64,
                BackgroundColor = Color.White,
                DataSource = InputPanelService.Instance.Panels
            };
            AddColumn(nameof(clsPanel.E), "材料 No", 90);
            AddColumn(nameof(clsPanel.Point1), "節点 1", 90);
            AddColumn(nameof(clsPanel.Point2), "節点 2", 90);
            AddColumn(nameof(clsPanel.Point3), "節点 3", 90);
            AddColumn(nameof(clsPanel.Point4), "節点 4", 90);
            _grid.RowPostPaint += (_, e) =>
                _grid.Rows[e.RowIndex].HeaderCell.Value = (e.RowIndex + 1).ToString();
            _grid.CellEnter += (_, e) =>
            {
                if (!_suppressSelection) PanelSelected?.Invoke(e.RowIndex >= 0 ? e.RowIndex + 1 : null);
            };
            Controls.Add(_grid);
        }

        internal void SelectPanel(int? id)
        {
            if (id is < 1 or > 100_000) return;
            _suppressSelection = true;
            try
            {
                if (id.HasValue) _grid.CurrentCell = _grid.Rows[id.Value - 1].Cells[0];
                else
                {
                    _grid.ClearSelection();
                    _grid.CurrentCell = null;
                }
            }
            finally { _suppressSelection = false; }
        }

        private void AddColumn(string property, string title, int width) => _grid.Columns.Add(
            new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = title, Width = width });
    }
}

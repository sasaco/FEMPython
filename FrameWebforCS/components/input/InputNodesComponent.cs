using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
using FrameWebforCS.providers;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputNodesComponent : UserControl
    {
        private readonly InputDataService _input = InputDataService.Instance;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet1;

        public InputNodesComponent()
        {
            InitializeComponent();

            fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();
            fpSpread1_Sheet1.SheetName = "Node";
            fpSpread1_Sheet1.AutoGenerateColumns = false;
            fpSpread1_Sheet1.DataAutoCellTypes = false;
            fpSpread1_Sheet1.DataAutoHeadings = false;
            fpSpread1_Sheet1.RowHeaderAutoText = HeaderAutoText.Numbers;
            fpSpread1_Sheet1.StartingRowNumber = 1;
            fpSpread1_Sheet1.DataSource = InputNodesService.Instance.Nodes;

            RefreshDimension();
        }

        internal void RefreshDimension()
        {
            if (fpSpread1.EditMode) fpSpread1.StopCellEditing();
            fpSpread1_Sheet1.ColumnCount = _input.dimension == 3 ? 3 : 2;

            var header = fpSpread1_Sheet1.ColumnHeader;
            var columns = fpSpread1_Sheet1.Columns;
            string[] fields = _input.dimension == 3
                ? new[] { "X", "Y", "Z" }
                : new[] { "X", "Y" };
            var coordinateType = new GeneralCellType { FormatString = "F3" };

            for (int i = 0; i < fields.Length; i++)
            {
                header.Cells[0, i].Text = fields[i];
                columns[i].DataField = fields[i];
                columns[i].CellType = coordinateType;
                columns[i].Width = 80;
            }

            Width = fields.Length * 80 + 100;
        }


    }
}

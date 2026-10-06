using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using FarPoint.Win.Spread.CellType;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputElementsComponent : UserControl
    {
        internal event Action<int>? GridSelectionChanged;
        private InputDataService _input = InputDataService.Instance;
        private List<FarPoint.Win.Spread.SheetView> fpSpread1_Sheets;
        private MemberDetailPanel? _detailPanel;
        internal int? DetailMemberId => _detailPanel?.MemberId;
        internal bool DetailVisible => _detailPanel?.Visible == true;


        public InputElementsComponent()
        {
            InitializeComponent();

            fpSpread1_Sheets = new List<SheetView>();

            for (int i = 0; i < InputElementsService.TypeCount; i++) { 

                var fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();

                fpSpread1_Sheet1.SheetName = "TYPE-" + (i + 1).ToString();

                fpSpread1_Sheet1.AutoGenerateColumns = false;
                fpSpread1_Sheet1.DataAutoCellTypes = false;
                fpSpread1_Sheet1.DataAutoHeadings = false;
                fpSpread1_Sheet1.RowHeaderAutoText = HeaderAutoText.Numbers;
                fpSpread1_Sheet1.StartingRowNumber = 1;
                fpSpread1_Sheet1.DataSource = InputElementsService.Instance.GetRows(i + 1);

                setColumn(fpSpread1_Sheet1);

                fpSpread1_Sheets.Add(fpSpread1_Sheet1);
            }

            float w = 0;
            var col = fpSpread1_Sheets.First().Columns;
            for (int i= 0; i < col.Count; i++) 
            {
                w += col[i].Width;
            }
            w += 100;

            this.Width = (int)w;

            fpSpread1.EnterCell += OnEnterCell;
            Disposed += (_, _) => fpSpread1.EnterCell -= OnEnterCell;

        }

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (IsDisposed || e.Row < 0 || e.Column < 0) return;
            // JS selectEnd passes the one-based row number as the element ID.
            GridSelectionChanged?.Invoke(e.Row + 1);
        }

        internal void ShowMemberDetail(int id)
        {
            if (IsDisposed) return;
            _detailPanel ??= new MemberDetailPanel();
            if (_detailPanel.Parent == null) Controls.Add(_detailPanel);
            _detailPanel.ShowMember(id);
        }

        private void setColumn(FarPoint.Win.Spread.SheetView fpSpread1_Sheet1)
        {
            var header = fpSpread1_Sheet1.ColumnHeader;
            if (fpSpread1_Sheet1.ColumnCount > 5)
                header.Cells[0, 5].ColumnSpan = 1;
            header.RowCount = 2;

            if (_input.dimension == 3)
            {
                fpSpread1_Sheet1.ColumnCount = 8;

                header.Cells[0, 0].Text = "弾性係数";
                header.Cells[1, 0].Text = "E(kN/m2)";
                header.Cells[0, 1].Text = "せん断弾性係数";
                header.Cells[1, 1].Text = "G(kN/m2)";
                header.Cells[0, 2].Text = "膨張係数";
                header.Cells[1, 2].Text = " ";
                header.Cells[0, 3].Text = "断面積";
                header.Cells[1, 3].Text = "A(m2)";
                header.Cells[0, 4].Text = "ねじり定数";
                header.Cells[1, 4].Text = "J(m4)";
                header.Cells[0, 5].Text = "断面二次モーメント";
                header.Cells[1, 5].Text = "Iy(m4)";
                header.Cells[0, 6].Text = header.Cells[0, 5].Text;
                header.Cells[1, 6].Text = "Iz(m4)";
                header.Cells[0, 7].Text = "名前";
                header.Cells[1, 7].Text = " ";

                header.Cells[0, 5].ColumnSpan = 2;

                var column = fpSpread1_Sheet1.Columns;
                column[0].DataField = nameof(clsElement.ElasticModulus);
                column[1].DataField = nameof(clsElement.ShearModulus);
                column[2].DataField = nameof(clsElement.Expansion);
                column[3].DataField = nameof(clsElement.Area);
                column[4].DataField = nameof(clsElement.Torsion);
                column[5].DataField = nameof(clsElement.InertiaY);
                column[6].DataField = nameof(clsElement.InertiaZ);
                column[7].DataField = nameof(clsElement.Name);

                column[0].CellType = new PrintNumberCellType("E2");
                column[1].CellType = new PrintNumberCellType("E2");
                column[2].CellType = new PrintNumberCellType("E2");
                column[3].CellType = new PrintNumberCellType("F4", useDefaultAt999: true);
                column[4].CellType = new PrintNumberCellType("F6", useDefaultAt999: true);
                column[5].CellType = new PrintNumberCellType("F6", useDefaultAt999: true);
                column[6].CellType = new PrintNumberCellType("F6", useDefaultAt999: true);

                column[0].Width = 80;
                column[1].Width = 150;
                column[2].Width = 80;
                column[3].Width = 80;
                column[4].Width = 100;
                column[5].Width = 80;
                column[6].Width = 80;
                column[7].Width = 150;
            }
            else
            {
                fpSpread1_Sheet1.ColumnCount = 5;

                header.Cells[0, 0].Text = "弾性係数";
                header.Cells[1, 0].Text = "E(kN/m2)";
                header.Cells[0, 1].Text = "膨張係数";
                header.Cells[1, 1].Text = " ";
                header.Cells[0, 2].Text = "断面積";
                header.Cells[1, 2].Text = "A(m2)";
                header.Cells[0, 3].Text = "断面二次モーメント";
                header.Cells[1, 3].Text = "I(m4)";
                header.Cells[0, 4].Text = "名前";
                header.Cells[1, 4].Text = " ";

                var column = fpSpread1_Sheet1.Columns;
                column[0].DataField = nameof(clsElement.ElasticModulus);
                column[1].DataField = nameof(clsElement.Expansion);
                column[2].DataField = nameof(clsElement.Area);
                column[3].DataField = nameof(clsElement.InertiaZ);
                column[4].DataField = nameof(clsElement.Name);

                column[0].CellType = new PrintNumberCellType("E2");
                column[1].CellType = new PrintNumberCellType("E2");
                column[2].CellType = new PrintNumberCellType("F4");
                column[3].CellType = new PrintNumberCellType("F6");
                column[4].CellType = new GeneralCellType();

                column[0].Width = 80;
                column[1].Width = 80;
                column[2].Width = 80;
                column[3].Width = 80;
                column[4].Width = 150;

            }
        }

        internal void RefreshDimension()
        {
            if (fpSpread1.EditMode) fpSpread1.StopCellEditing();
            foreach (var sheet in fpSpread1_Sheets) setColumn(sheet);
            float width = 100;
            foreach (Column column in fpSpread1_Sheets[0].Columns) width += column.Width;
            Width = (int)width;
        }

    }
}

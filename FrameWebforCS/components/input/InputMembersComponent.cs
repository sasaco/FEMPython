using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
using FrameWebforCS.providers;
using GrapeCity.Win.Spread.InputMan.CellType;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputMembersComponent : UserControl
    {
        internal event Action<string, int, string>? GridSelectionChanged;
        internal event Action<string>? ActiveMemberDisplayModeChanged;
        internal string ActiveMemberDisplayMode =>
            ReferenceEquals(fpSpread1.ActiveSheet, fpSpread1_Sheet2) ? "rigid" : "member";
        private bool _syncingSelection;
        private InputDataService _input = InputDataService.Instance;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet1;
        private FarPoint.Win.Spread.SheetView fpSpread1_Sheet2;
        private MemberDetailPanel? _detailPanel;
        internal int? DetailMemberId => _detailPanel?.MemberId;
        internal bool DetailVisible => _detailPanel?.Visible == true;

        public InputMembersComponent()
        {
            InitializeComponent();

            fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();

            ConfigureSheet(fpSpread1_Sheet1);
            fpSpread1_Sheet1.DataSource = InputMembersService.Instance.Members;

            SetSheet1();

            float w = 0;
            var col = fpSpread1_Sheet1.Columns;
            for (int i = 0; i < col.Count; i++)
            {
                w += col[i].Width;
            }
            w += 100;

            this.Width = (int)w;


            fpSpread1_Sheet2 = fpSpread1.AddNewSheetView();

            ConfigureSheet(fpSpread1_Sheet2);
            fpSpread1_Sheet2.DataSource = InputRigidZoneService.Instance.Rows;

            SetSheet2();

            fpSpread1.EnterCell += OnEnterCell;
            fpSpread1.ActiveSheetChanged += OnActiveSheetChanged;
            HandleCreated += OnDisplayActivated;
            VisibleChanged += OnDisplayActivated;
            Disposed += (_, _) =>
            {
                fpSpread1.EnterCell -= OnEnterCell;
                fpSpread1.ActiveSheetChanged -= OnActiveSheetChanged;
                HandleCreated -= OnDisplayActivated;
                VisibleChanged -= OnDisplayActivated;
            };

        }

        private static void ConfigureSheet(SheetView sheet)
        {
            sheet.AutoGenerateColumns = false;
            sheet.DataAutoCellTypes = false;
            sheet.DataAutoHeadings = false;
            sheet.RowHeaderAutoText = HeaderAutoText.Numbers;
            sheet.StartingRowNumber = 1;
        }

        private void SetSheet1()
        {
            fpSpread1_Sheet1.SheetName = "部材";

            var header = fpSpread1_Sheet1.ColumnHeader;
            if (fpSpread1_Sheet1.ColumnCount > 0)
                header.Cells[0, 0].ColumnSpan = 1;
            fpSpread1_Sheet1.ColumnCount = _input.dimension == 3 ? 6 : 5;

            var column = fpSpread1_Sheet1.Columns;

            column[0].DataField = nameof(clsMember.Ni);
            column[1].DataField = nameof(clsMember.Nj);
            column[3].DataField = nameof(clsMember.E);
            if (_input.dimension == 3)
            {
                column[4].DataField = nameof(clsMember.Cg);
                column[5].DataField = string.Empty;
            }
            else
                column[4].DataField = string.Empty;

            header.RowCount = 2;

            if (_input.dimension == 3)
            {
                fpSpread1_Sheet1.ColumnCount = 6;

                header.Cells[0, 0].Text = "節点";
                header.Cells[1, 0].Text = "i端";
                header.Cells[0, 1].Text = "";
                header.Cells[1, 1].Text = "j端";
                header.Cells[0, 2].Text = "部材長";
                header.Cells[1, 2].Text = "(m)";
                header.Cells[0, 3].Text = "材料";
                header.Cells[1, 3].Text = "No";
                header.Cells[0, 4].Text = "コードアングル";
                header.Cells[1, 4].Text = "(°)";
                header.Cells[0, 5].Text = "材料名称";
                header.Cells[1, 5].Text = " ";

                header.Cells[0, 0].ColumnSpan = 2;

                column[0].Width = 50;
                column[1].Width = 50;
                column[2].Width = 80;
                column[3].Width = 50;
                column[4].Width = 150;
                column[5].Width = 150;

                column[2].BackColor = SystemColors.Control;
                column[5].BackColor = SystemColors.Control;
            }
            else
            {
                fpSpread1_Sheet1.ColumnCount = 5;

                header.Cells[0, 0].Text = "節点";
                header.Cells[1, 0].Text = "i端";
                header.Cells[0, 1].Text = "";
                header.Cells[1, 1].Text = "j端";
                header.Cells[0, 2].Text = "部材長";
                header.Cells[1, 2].Text = "(m)";
                header.Cells[0, 3].Text = "材料";
                header.Cells[1, 3].Text = "No";
                header.Cells[0, 4].Text = "材料名称";
                header.Cells[1, 4].Text = " ";

                header.Cells[0, 0].ColumnSpan = 2;

                column[0].Width = 50;
                column[1].Width = 50;
                column[2].Width = 80;
                column[3].Width = 50;
                column[4].Width = 150;

                column[2].BackColor = SystemColors.Control;
                column[4].BackColor = SystemColors.Control;
            }

            for (int i = 0; i < column.Count; i++)
            {
                column[i].Locked = false;
                column[i].BackColor = SystemColors.Window;
            }
            column[2].Locked = true;
            column[_input.dimension == 3 ? 5 : 4].Locked = true;
            column[2].BackColor = SystemColors.Control;
            column[_input.dimension == 3 ? 5 : 4].BackColor = SystemColors.Control;
            if (_input.dimension == 3)
                column[4].CellType = new PrintNumberCellType("F3");
            else
                column[4].CellType = new GeneralCellType();
            fpSpread1_Sheet1.Protect = true;

        }

        private void SetSheet2()
        {
            fpSpread1_Sheet2.SheetName = "剛域";
            var column = fpSpread1_Sheet2.Columns;

            column[1].DataField = nameof(clsRigit.E);
            column[3].DataField = nameof(clsRigit.Ilength);
            column[4].DataField = nameof(clsRigit.Jlength);
            column[5].DataField = nameof(clsRigit.E1);

            var header = fpSpread1_Sheet2.ColumnHeader;
            header.RowCount = 2;

            fpSpread1_Sheet2.ColumnCount = 7;

            header.Cells[0, 0].Text = "部材長";
            header.Cells[1, 0].Text = "(m)";
            header.Cells[0, 1].Text = "材料";
            header.Cells[1, 1].Text = "No";
            header.Cells[0, 2].Text = "材料名称";
            header.Cells[1, 2].Text = " ";
            header.Cells[0, 3].Text = "剛域";
            header.Cells[1, 3].Text = "i端の距離";
            header.Cells[0, 4].Text = "";
            header.Cells[1, 4].Text = "j端の距離";
            header.Cells[0, 5].Text = "材料";
            header.Cells[1, 5].Text = "No";
            header.Cells[0, 6].Text = "材料名称";
            header.Cells[1, 6].Text = " ";

            header.Cells[0, 3].ColumnSpan = 2;

            column[0].Width = 80;
            column[1].Width = 50;
            column[2].Width = 150;
            column[3].Width = 80;
            column[4].Width = 80;
            column[5].Width = 50;
            column[6].Width = 150;

            for (int i = 0; i < column.Count; i++)
                column[i].Locked = false;

            for (int i=0; i<3; i++)
            {
                column[i].Locked = true;
                column[i].BackColor = SystemColors.Control;
            }
            column[6].Locked = true;
            column[6].BackColor = SystemColors.Control;
            column[3].CellType = new PrintNumberCellType("F2");
            column[4].CellType = new PrintNumberCellType("F2");
            fpSpread1_Sheet2.Protect = true;

        }

        public void setActiveSheet(int index)
        {
            this.fpSpread1.ActiveSheetIndex = index;　
        }

        internal void RefreshDimension()
        {
            if (fpSpread1.EditMode) fpSpread1.StopCellEditing();
            SetSheet1();
            float width = 100;
            foreach (Column column in fpSpread1_Sheet1.Columns) width += column.Width;
            Width = (int)width;
        }

        private void OnActiveSheetChanged(object? sender, EventArgs e) =>
            ActiveMemberDisplayModeChanged?.Invoke(ActiveMemberDisplayMode);

        private void OnDisplayActivated(object? sender, EventArgs e)
        {
            if (Visible && !IsDisposed)
                ActiveMemberDisplayModeChanged?.Invoke(ActiveMemberDisplayMode);
        }

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0) return;
            var sheet = fpSpread1.ActiveSheet;
            string kind = ReferenceEquals(sheet, fpSpread1_Sheet2) ? "rigid_zone" : "members";
            string axis = kind == "rigid_zone" ? e.Column switch
            {
                3 => "i", 4 => "j", _ => ""
            } : "";
            // JS InputMembersComponent.selectEnd uses 1-based row identity; the
            // rigid zone editor is a second C# sheet under the same component.
            GridSelectionChanged?.Invoke(kind, e.Row + 1, axis);
        }

        internal bool SelectGridRow(string kind, int row, string? axis = null)
        {
            if (IsDisposed || row < 1) return false;
            var sheet = kind == "rigid_zone" ? fpSpread1_Sheet2 :
                kind == "members" ? fpSpread1_Sheet1 : null;
            if (sheet == null || row > sheet.RowCount) return false;
            int column = kind == "rigid_zone" ? axis == "j" ? 4 : 3 : 0;
            _syncingSelection = true;
            try
            {
                fpSpread1.ActiveSheetIndex = ReferenceEquals(sheet, fpSpread1_Sheet2) ? 1 : 0;
                sheet.SetActiveCell(row - 1, column);
            }
            finally { _syncingSelection = false; }
            return true;
        }

        internal void ShowMemberDetail(int id)
        {
            if (IsDisposed) return;
            EnsureDetailPanel();
            _detailPanel!.ShowMember(id);
        }

        internal bool ApplyMemberDetail(int ni, int nj, int element, float cg) =>
            _detailPanel?.ApplyMember(ni, nj, element, cg) == true;

        private void EnsureDetailPanel()
        {
            if (_detailPanel != null) return;
            _detailPanel = new MemberDetailPanel();
            Controls.Add(_detailPanel);
        }
    }
}

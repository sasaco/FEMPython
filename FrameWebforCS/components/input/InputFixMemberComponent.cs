using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static THREE.ArcballControls;

namespace FrameWebforCS.components.input
{
    public partial class InputFixMemberComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        private bool _syncingSelection;
        private InputDataService _input = InputDataService.Instance;
        private readonly Dictionary<SheetView, SpringGroupDataModel> _groups = new();
        private readonly InputFixMemberService _service = InputFixMemberService.Instance;

        public InputFixMemberComponent()
        {
            InitializeComponent();

            for (int i = 0; i < InputFixMemberService.TypeCount; i++)
            {
                var fpSpread1_Sheet1 = fpSpread1.AddNewSheetView();

                fpSpread1_Sheet1.SheetName = (i + 1).ToString();
                fpSpread1_Sheet1.AutoGenerateColumns = false;
                fpSpread1_Sheet1.DataAutoCellTypes = false;
                fpSpread1_Sheet1.DataAutoHeadings = false;
                fpSpread1_Sheet1.RowHeaderAutoText = HeaderAutoText.Numbers;
                fpSpread1_Sheet1.StartingRowNumber = 1;

                setColumn(fpSpread1_Sheet1);
                fpSpread1_Sheet1.SelectionPolicy = FarPoint.Win.Spread.Model.SelectionPolicy.MultiRange;
                fpSpread1_Sheet1.DataSource = _service.GetEditorRows(fpSpread1_Sheet1.SheetName);
                var grouped = new SpringGroupDataModel(fpSpread1_Sheet1.Models.Data,
                    _service.GetEditorRows(fpSpread1_Sheet1.SheetName), MemberLength);
                grouped.Group([new SortInfo(0, true)]);
                fpSpread1_Sheet1.Models.Data = grouped;
                // Binding the grouped model rebuilds columns and clears DataField mappings.
                setColumn(fpSpread1_Sheet1);
                _groups.Add(fpSpread1_Sheet1, grouped);
                SheetView sheet = fpSpread1_Sheet1;
                fpSpread1.EnableRowOperations(sheet,
                    (row, column) => InsertRow(sheet, row, column),
                    (rows, column) => DeleteRows(sheet, rows, column));
            }

            float w = 0;
            FarPoint.Win.Spread.SheetView fs = (SheetView)fpSpread1.Sheets.First();
            var col = fs.Columns;
            for (int i = 0; i < col.Count; i++)
            {
                w += col[i].Width;
            }
            w += 100;

            this.Width = (int)w;

            fpSpread1.EnterCell += OnEnterCell;
            fpSpread1.ActiveSheetChanged += OnActiveSheetChanged;
            InputNodesService.Instance.NodeEdited += OnGeometryChanged;
            InputMembersService.Instance.MemberEdited += OnGeometryChanged;
            Disposed += (_, _) =>
            {
                fpSpread1.EnterCell -= OnEnterCell;
                fpSpread1.ActiveSheetChanged -= OnActiveSheetChanged;
                InputNodesService.Instance.NodeEdited -= OnGeometryChanged;
                InputMembersService.Instance.MemberEdited -= OnGeometryChanged;
                foreach (SheetView sheet in fpSpread1.Sheets)
                    fpSpread1.DisableRowOperations(sheet);
            };
            InputFixMemberService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        }

        private void OnActiveSheetChanged(object? sender, EventArgs e) =>
            InputFixMemberService.Instance.SelectCase(fpSpread1.ActiveSheet.SheetName);

        private void OnGeometryChanged(int _) => fpSpread1.Invalidate();

        private void OnEnterCell(object? sender, EnterCellEventArgs e)
        {
            if (_syncingSelection || e.Row < 0 || e.Column < 0) return;
            var sheet = fpSpread1.ActiveSheet;
            var grouped = _groups[sheet];
            int sourceIndex = grouped.GetIndex(e.Row);
            var rows = _service.GetEditorRows(sheet.SheetName);
            if (sourceIndex < 0 || sourceIndex >= rows.Count) return;
            string axis = FieldName(e.Column);
            // JS InputFixMemberComponent.selectEnd passes the 1-based row and field key.
            GridSelectionChanged?.Invoke(rows[sourceIndex].row, axis);
        }

        internal bool SelectGridRow(int row, string? axis = null, string? caseId = null)
        {
            if (IsDisposed || row < 1) return false;
            int sheetIndex = caseId == null ? fpSpread1.ActiveSheetIndex :
                fpSpread1.Sheets.Cast<SheetView>().ToList().FindIndex(s => s.SheetName == caseId);
            if (sheetIndex < 0) return false;
            var sheet = fpSpread1.Sheets[sheetIndex];
            var rows = _service.GetEditorRows(sheet.SheetName);
            int sourceIndex = rows.ToList().FindIndex(item => item.row == row);
            if (sourceIndex < 0) return false;
            var grouped = _groups[sheet];
            int displayRow = grouped.GetModelIndexFromTargetIndex(sourceIndex);
            if (displayRow < 0) return false;
            int column = 0;
            if (axis != null)
                for (int i = 0; i < sheet.ColumnCount; i++)
                    if (string.Equals(FieldName(i), axis,
                            StringComparison.OrdinalIgnoreCase)) { column = i; break; }
            _syncingSelection = true;
            try
            {
                fpSpread1.ActiveSheetIndex = sheetIndex;
                for (int index = displayRow - 1; index >= 0; index--)
                    if (grouped.IsGroup(index))
                    {
                        grouped.GetGroup(index).Expanded = true;
                        break;
                    }
                sheet.SetActiveCell(displayRow, column);
            }
            finally { _syncingSelection = false; }
            return true;
        }

        private float? MemberLength(string id)
        {
            if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ||
                InputMembersService.Instance.GetDisplayMember(number) is not { } member)
                return null;
            var start = InputNodesService.Instance.GetDisplayNode(member.Ni);
            var end = InputNodesService.Instance.GetDisplayNode(member.Nj);
            return start != null && end != null ? start.DistanceTo(end) : null;
        }

        private string FieldName(int column)
        {
            string[] fields = _input.dimension == 3
                ? ["m", "length", "tx", "ty", "tz", "tr"]
                : ["m", "length", "tx", "ty"];
            return column >= 0 && column < fields.Length ? fields[column] : "";
        }

        private int SourceRow(SheetView sheet, int displayRow)
        {
            var grouped = _groups[sheet];
            int source = grouped.GetIndex(displayRow);
            var rows = _service.GetEditorRows(sheet.SheetName);
            return source >= 0 && source < rows.Count ? rows[source].row : -1;
        }

        private bool InsertRow(SheetView sheet, int displayRow, int column)
        {
            var grouped = _groups[sheet];
            string? memberId = null;
            int row = SourceRow(sheet, displayRow);
            if (grouped.IsGroup(displayRow))
            {
                int last = displayRow;
                while (last + 1 < grouped.RowCount && !grouped.IsGroup(last + 1)) last++;
                int lastRow = SourceRow(sheet, last);
                if (lastRow < 1) return false;
                row = lastRow + 1;
                int firstSource = grouped.GetIndex(displayRow + 1);
                if (firstSource >= 0)
                    memberId = _service.GetEditorRows(sheet.SheetName)[firstSource].m;
            }
            else
            {
                int source = grouped.GetIndex(displayRow);
                if (source >= 0)
                    memberId = _service.GetEditorRows(sheet.SheetName)[source].m;
            }
            if (!_service.InsertEditorRow(sheet.SheetName, row, memberId)) return false;
            SelectGridRow(row, caseId: sheet.SheetName);
            return true;
        }

        private bool DeleteRows(SheetView sheet, IReadOnlyList<int> selectedRows, int column)
        {
            var grouped = _groups[sheet];
            var indices = new HashSet<int>();
            foreach (int selected in selectedRows)
            {
                if (grouped.IsGroup(selected))
                {
                    for (int index = selected + 1; index < grouped.RowCount && !grouped.IsGroup(index); index++)
                    {
                        int row = SourceRow(sheet, index);
                        if (row > 0) indices.Add(row);
                    }
                }
                else
                {
                    int row = SourceRow(sheet, selected);
                    if (row > 0) indices.Add(row);
                }
            }
            if (indices.Count == 0 || !_service.DeleteEditorRows(sheet.SheetName, indices)) return false;
            int next = Math.Min(indices.Min(), _service.GetEditorRows(sheet.SheetName).Last().row);
            SelectGridRow(next, caseId: sheet.SheetName);
            return true;
        }

        private void setColumn(FarPoint.Win.Spread.SheetView fpSpread1_Sheet1)
        {
            var header = fpSpread1_Sheet1.ColumnHeader;
            if (fpSpread1_Sheet1.ColumnCount > 1) header.Cells[0, 1].ColumnSpan = 1;
            header.RowCount = 2;

            if (_input.dimension == 3)
            {
                fpSpread1_Sheet1.ColumnCount = 6;

                header.Cells[0, 0].Text = "部材";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "部材長";
                header.Cells[1, 1].Text = "(m)";
                header.Cells[0, 2].Text = "変位拘束";
                header.Cells[1, 2].Text = "部材軸方向";
                header.Cells[0, 3].Text = "";
                header.Cells[1, 3].Text = "部材Y軸";
                header.Cells[0, 4].Text = "";
                header.Cells[1, 4].Text = "部材Z軸";
                header.Cells[0, 5].Text = "回転拘束";
                header.Cells[1, 5].Text = "(kNm/rad/m)";

                header.Cells[0, 2].ColumnSpan = 3;

                var column = fpSpread1_Sheet1.Columns;
                string[] fields = ["M", "Length", "Tx", "Ty", "Tz", "Tr"];
                column[0].DataField = fields[0];
                for (int i = 1; i < fields.Length; i++)
                    column[i].DataField = fields[i];

                column[0].Width = 50;
                column[1].Width = 80;
                for (var i = 2; i < column.Count; i++)
                {
                    column[i].Width = 100;
                }
            }
            else
            {
                fpSpread1_Sheet1.ColumnCount = 4;

                header.Cells[0, 0].Text = "部材";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "部材長";
                header.Cells[1, 1].Text = "(m)";
                header.Cells[0, 2].Text = "部材軸方向";
                header.Cells[1, 2].Text = "(kN/m/m)";
                header.Cells[0, 3].Text = "部材直角方向";
                header.Cells[1, 3].Text = "(kN/m/m)";

                var column = fpSpread1_Sheet1.Columns;
                column[0].DataField = "M";
                column[1].DataField = "Length";
                column[2].DataField = "Tx";
                column[3].DataField = "Ty";

                column[0].Width = 50;
                column[1].Width = 80;
                for (var i = 2; i < column.Count; i++)
                {
                    column[i].Width = 100;
                }
            }
        }

        internal void RefreshDimension()
        {
            if (fpSpread1.EditMode) fpSpread1.StopCellEditing();
            foreach (SheetView sheet in fpSpread1.Sheets)
            {
                // GroupDataModel owns the visible count; update its bound target first.
                _groups[sheet].TargetModel.ColumnCount = _input.dimension == 3 ? 6 : 4;
                setColumn(sheet);
            }
            float width = 100;
            foreach (Column column in fpSpread1.Sheets[0].Columns) width += column.Width;
            Width = (int)width;
        }
    }
}

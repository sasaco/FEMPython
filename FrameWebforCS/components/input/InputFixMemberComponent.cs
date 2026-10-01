using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace FrameWebforCS.components.input
{
    public partial class InputFixMemberComponent : UserControl
    {
        internal event Action<int, string>? GridSelectionChanged;
        private bool _syncingSelection;
        private InputDataService _input = InputDataService.Instance;
        private readonly Dictionary<SheetView, BindingList<SpringOutlineRow>> _views = new();
        private readonly Dictionary<SheetView, ListChangedEventHandler> _editorHandlers = new();
        private bool _rebuildingOutlines;
        private readonly InputFixMemberService _service = InputFixMemberService.Instance;

        public InputFixMemberComponent()
        {
            InitializeComponent();
            fpSpread1.InterfaceRenderer = null;

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
                _views.Add(fpSpread1_Sheet1, new BindingList<SpringOutlineRow>());
                SheetView observedSheet = fpSpread1_Sheet1;
                ListChangedEventHandler handler = (_, e) =>
                {
                    if (e.ListChangedType == ListChangedType.Reset ||
                        e.ListChangedType == ListChangedType.ItemAdded ||
                        e.ListChangedType == ListChangedType.ItemDeleted ||
                        e.PropertyDescriptor?.Name == nameof(clsFixMember.M))
                        RebuildOutline(observedSheet);
                };
                _service.GetEditorRows(fpSpread1_Sheet1.SheetName).ListChanged += handler;
                _editorHandlers.Add(fpSpread1_Sheet1, handler);
                RebuildOutline(fpSpread1_Sheet1);
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
            fpSpread1.ShowOutline = RowCol.Rows;
            Disposed += (_, _) =>
            {
                fpSpread1.EnterCell -= OnEnterCell;
                fpSpread1.ActiveSheetChanged -= OnActiveSheetChanged;
                InputNodesService.Instance.NodeEdited -= OnGeometryChanged;
                InputMembersService.Instance.MemberEdited -= OnGeometryChanged;
                foreach (SheetView sheet in fpSpread1.Sheets)
                {
                    _service.GetEditorRows(sheet.SheetName).ListChanged -= _editorHandlers[sheet];
                    fpSpread1.DisableRowOperations(sheet);
                }
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
            if (e.Row >= _views[sheet].Count || _views[sheet][e.Row].Source is not { } source) return;
            string axis = FieldName(e.Column);
            // JS InputFixMemberComponent.selectEnd passes the 1-based row and field key.
            GridSelectionChanged?.Invoke(source.row, axis);
        }

        internal bool SelectGridRow(int row, string? axis = null, string? caseId = null)
        {
            if (IsDisposed || row < 1) return false;
            int sheetIndex = caseId == null ? fpSpread1.ActiveSheetIndex :
                fpSpread1.Sheets.Cast<SheetView>().ToList().FindIndex(s => s.SheetName == caseId);
            if (sheetIndex < 0) return false;
            var sheet = fpSpread1.Sheets[sheetIndex];
            int displayRow = _views[sheet].ToList().FindIndex(item => item.Source?.row == row);
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
                ExpandContainingOutline(sheet, displayRow);
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

        private void RebuildOutline(SheetView sheet)
        {
            if (_rebuildingOutlines || IsDisposed) return;
            _rebuildingOutlines = true;
            try
            {
                var collapsed = (sheet.GetRangeGroupInfo(1, true) ?? Array.Empty<RangeGroupInfo>())
                    .Where(group => group.State == GroupState.Collapsed && group.Start > 0 &&
                        group.Start - 1 < _views[sheet].Count)
                    .Select(group => _views[sheet][group.Start - 1].M)
                    .Where(id => id != null).ToHashSet(StringComparer.Ordinal);
                var view = new List<SpringOutlineRow>();
                var spans = new List<(int Start, int Count, string Member)>();
                var editor = _service.GetEditorRows(sheet.SheetName);
                foreach (var group in editor.Where(item => !string.IsNullOrWhiteSpace(item.m))
                    .GroupBy(item => item.m!, StringComparer.Ordinal)
                    .OrderBy(group => group.Key, StringComparer.Ordinal))
                {
                    int summary = view.Count;
                    view.Add(new SpringOutlineRow(group.Key, MemberLength));
                    foreach (var item in group.OrderBy(item => item.row))
                        view.Add(new SpringOutlineRow(item));
                    spans.Add((summary + 1, view.Count - summary - 1, group.Key));
                }
                foreach (var item in editor.Where(item => string.IsNullOrWhiteSpace(item.m))
                    .OrderBy(item => item.row))
                    view.Add(new SpringOutlineRow(item));

                sheet.ClearRangeGroup(true);
                _views[sheet] = new BindingList<SpringOutlineRow>(view);
                sheet.DataSource = _views[sheet];
                setColumn(sheet);
                sheet.RangeGroupSummaryRowBelow = false;
                sheet.RangeGroupButtonStyle = RangeGroupButtonStyle.Enhanced;
                sheet.Protect = true;
                foreach (var (start, count, member) in spans)
                {
                    sheet.Rows[start - 1].Locked = true;
                    for (int index = start; index < start + count; index++)
                        sheet.Rows[index].Locked = false;
                    sheet.AddRangeGroup(start, count, true);
                    if (collapsed.Contains(member))
                    {
                        var outline = (sheet.GetRangeGroupInfo(1, true) ?? Array.Empty<RangeGroupInfo>())
                            .First(group => group.Start == start);
                        sheet.ExpandRangeGroup(outline, true, false);
                    }
                }
                for (int index = spans.Sum(span => span.Count + 1); index < view.Count; index++)
                    sheet.Rows[index].Locked = false;
            }
            finally { _rebuildingOutlines = false; }
        }

        private static void ExpandContainingOutline(SheetView sheet, int row)
        {
            foreach (var group in sheet.GetRangeGroupInfo(1, true) ?? Array.Empty<RangeGroupInfo>())
                if (row >= group.Start && row <= group.End && group.State == GroupState.Collapsed)
                {
                    sheet.ExpandRangeGroup(group, true, true);
                    break;
                }
        }

        private sealed class SpringOutlineRow
        {
            private readonly string? _member;
            private readonly Func<string, float?>? _memberLength;
            internal clsFixMember? Source { get; }

            internal SpringOutlineRow(clsFixMember source) => Source = source;
            internal SpringOutlineRow(string member, Func<string, float?> memberLength)
            {
                _member = member;
                _memberLength = memberLength;
            }

            public string? M
            {
                get => Source?.M ?? _member;
                set { if (Source != null) Source.M = value; }
            }
            public object? Length
            {
                get => Source != null ? Source.Length :
                    _memberLength?.Invoke(_member!)?.ToString("0.00", CultureInfo.InvariantCulture);
                set { if (Source != null) Source.Length = Number(value); }
            }
            public object? Tx { get => Source != null ? Source.Tx : "***";
                set { if (Source != null) Source.Tx = Number(value); } }
            public object? Ty { get => Source != null ? Source.Ty : "***";
                set { if (Source != null) Source.Ty = Number(value); } }
            public object? Tz { get => Source != null ? Source.Tz : "***";
                set { if (Source != null) Source.Tz = Number(value); } }
            public object? Tr { get => Source != null ? Source.Tr : "***";
                set { if (Source != null) Source.Tr = Number(value); } }

            private static float? Number(object? value) => value == null ||
                value is string text && string.IsNullOrWhiteSpace(text)
                    ? null : Convert.ToSingle(value, CultureInfo.CurrentCulture);
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
            return displayRow >= 0 && displayRow < _views[sheet].Count
                ? _views[sheet][displayRow].Source?.row ?? -1 : -1;
        }

        private bool InsertRow(SheetView sheet, int displayRow, int column)
        {
            string? memberId = null;
            int row = SourceRow(sheet, displayRow);
            if (displayRow >= 0 && displayRow < _views[sheet].Count &&
                _views[sheet][displayRow].Source == null)
            {
                memberId = _views[sheet][displayRow].M;
                int last = displayRow + 1;
                while (last + 1 < _views[sheet].Count &&
                    _views[sheet][last + 1].Source?.m == memberId)
                    last++;
                int lastRow = SourceRow(sheet, last);
                if (lastRow < 1) return false;
                row = lastRow + 1;
            }
            else
            {
                if (displayRow >= 0 && displayRow < _views[sheet].Count)
                    memberId = _views[sheet][displayRow].Source?.m;
            }
            if (!_service.InsertEditorRow(sheet.SheetName, row, memberId)) return false;
            SelectGridRow(row, caseId: sheet.SheetName);
            return true;
        }

        private bool DeleteRows(SheetView sheet, IReadOnlyList<int> selectedRows, int column)
        {
            var indices = new HashSet<int>();
            foreach (int selected in selectedRows)
            {
                if (selected < 0 || selected >= _views[sheet].Count) continue;
                if (_views[sheet][selected].Source == null)
                {
                    string? memberId = _views[sheet][selected].M;
                    for (int index = selected + 1; index < _views[sheet].Count &&
                        _views[sheet][index].Source?.m == memberId; index++)
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
                setColumn(sheet);
            }
            float width = 100;
            foreach (Column column in fpSpread1.Sheets[0].Columns) width += column.Width;
            Width = (int)width;
        }
    }
}

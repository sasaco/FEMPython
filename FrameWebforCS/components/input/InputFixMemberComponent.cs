using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
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
        private readonly Dictionary<SheetView, SpringOutlineRow[]> _sourceRows = new();
        private readonly Dictionary<SheetView, ListChangedEventHandler> _rowHandlers = new();
        private readonly Dictionary<SheetView, List<OutlineSpan>> _outlines = new();
        private readonly Dictionary<SheetView, int[]> _displayIndices = new();
        private bool _rebuildingOutlines;
        private readonly InputFixMemberService _service = InputFixMemberService.Instance;

        private sealed record OutlineSpan(int First, int Count, string Member);

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
                var source = _service.GetRows(fpSpread1_Sheet1.SheetName);
                _sourceRows.Add(fpSpread1_Sheet1, Enumerable.Range(0, source.Count)
                    .Select(index => new SpringOutlineRow(index + 1, source[index], MemberLength)).ToArray());
                _views.Add(fpSpread1_Sheet1, new BindingList<SpringOutlineRow>());
                _outlines.Add(fpSpread1_Sheet1, new List<OutlineSpan>());
                _displayIndices.Add(fpSpread1_Sheet1, new int[source.Count + 1]);
                SheetView observedSheet = fpSpread1_Sheet1;
                ListChangedEventHandler handler = (_, e) =>
                {
                    if (e.ListChangedType == ListChangedType.Reset ||
                        e.ListChangedType == ListChangedType.ItemAdded ||
                        e.ListChangedType == ListChangedType.ItemDeleted ||
                        e.PropertyDescriptor?.Name == nameof(clsFixMember.M))
                        RebuildOutline(observedSheet);
                    else if (e.ListChangedType == ListChangedType.ItemChanged &&
                        e.NewIndex >= 0 && e.NewIndex + 1 < _displayIndices[observedSheet].Length &&
                        _displayIndices[observedSheet][e.NewIndex + 1] > 0)
                        _views[observedSheet].ResetItem(_displayIndices[observedSheet][e.NewIndex + 1] - 1);
                };
                source.ListChanged += handler;
                _rowHandlers.Add(fpSpread1_Sheet1, handler);
                RebuildOutline(fpSpread1_Sheet1);
                fpSpread1_Sheet1.Models.RangeGroupModel.Changed += (_, _) =>
                    SyncOutlineStates(observedSheet);
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
                    _service.GetRows(sheet.SheetName).ListChanged -= _rowHandlers[sheet];
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
            if (e.Row >= _views[sheet].Count || _views[sheet][e.Row].IsSummary) return;
            string axis = FieldName(e.Column);
            // JS InputFixMemberComponent.selectEnd passes the 1-based row and field key.
            GridSelectionChanged?.Invoke(_views[sheet][e.Row].RowId, axis);
        }

        internal bool SelectGridRow(int row, string? axis = null, string? caseId = null)
        {
            if (IsDisposed || row < 1) return false;
            int sheetIndex = caseId == null ? fpSpread1.ActiveSheetIndex :
                fpSpread1.Sheets.Cast<SheetView>().ToList().FindIndex(s => s.SheetName == caseId);
            if (sheetIndex < 0) return false;
            var sheet = fpSpread1.Sheets[sheetIndex];
            if (row >= _displayIndices[sheet].Length || _displayIndices[sheet][row] == 0)
                return false;
            int displayRow = _displayIndices[sheet][row] - 1;
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
                var oldGroups = sheet.GetRangeGroupInfo(1, true) ?? Array.Empty<RangeGroupInfo>();
                var collapsed = _outlines[sheet]
                    .Where(span => oldGroups.Any(group => group.Start == span.First + 1 &&
                        group.State == GroupState.Collapsed))
                    .Select(span => span.Member).ToHashSet(StringComparer.Ordinal);
                foreach (var span in _outlines[sheet])
                    sheet.Rows[span.First].Locked = false;

                var assigned = new List<SpringOutlineRow>();
                var partial = new List<SpringOutlineRow>();
                var empty = new List<SpringOutlineRow>();
                var source = _service.GetRows(sheet.SheetName);
                foreach (var row in _sourceRows[sheet])
                {
                    row.Source = source[row.RowId - 1];
                    row.IsSummary = false;
                    if (!string.IsNullOrWhiteSpace(row.Source.m)) assigned.Add(row);
                    else if (!row.Source.IsEmpty) partial.Add(row);
                    else empty.Add(row);
                }
                assigned.Sort((left, right) =>
                {
                    int member = StringComparer.Ordinal.Compare(left.Source.m, right.Source.m);
                    return member != 0 ? member : left.RowId.CompareTo(right.RowId);
                });
                var view = new List<SpringOutlineRow>(source.Count);
                view.AddRange(assigned);
                view.AddRange(partial);
                view.AddRange(empty);
                var spans = new List<OutlineSpan>();
                for (int first = 0; first < assigned.Count;)
                {
                    int end = first + 1;
                    while (end < assigned.Count && assigned[end].Source.m == assigned[first].Source.m)
                        end++;
                    if (end - first > 1)
                        spans.Add(new OutlineSpan(first, end - first, assigned[first].Source.m!));
                    first = end;
                }
                sheet.ClearRangeGroup(true);
                _views[sheet] = new BindingList<SpringOutlineRow>(view);
                sheet.DataSource = _views[sheet];
                setColumn(sheet);
                sheet.RangeGroupSummaryRowBelow = false;
                sheet.RangeGroupButtonStyle = RangeGroupButtonStyle.Enhanced;
                sheet.DefaultStyle.Locked = false;
                sheet.Protect = true;
                _outlines[sheet] = spans;
                var indices = _displayIndices[sheet];
                for (int index = 0; index < view.Count; index++)
                    indices[view[index].RowId] = index + 1;
                foreach (var span in spans)
                {
                    sheet.Rows[span.First].Locked = false;
                    sheet.AddRangeGroup(span.First + 1, span.Count - 1, true);
                    if (collapsed.Contains(span.Member))
                    {
                        var outline = (sheet.GetRangeGroupInfo(1, true) ?? Array.Empty<RangeGroupInfo>())
                            .First(group => group.Start == span.First + 1);
                        sheet.ExpandRangeGroup(outline, true, false);
                    }
                    UpdateSummary(sheet, span, collapsed.Contains(span.Member));
                }
            }
            finally { _rebuildingOutlines = false; }
        }

        private void SyncOutlineStates(SheetView sheet)
        {
            if (_rebuildingOutlines || IsDisposed) return;
            var groups = sheet.GetRangeGroupInfo(1, true) ?? Array.Empty<RangeGroupInfo>();
            foreach (var span in _outlines[sheet])
            {
                var group = groups.FirstOrDefault(item => item.Start == span.First + 1);
                if (group != null)
                    UpdateSummary(sheet, span, group.State == GroupState.Collapsed);
            }
        }

        private void UpdateSummary(SheetView sheet, OutlineSpan span, bool collapsed)
        {
            if (_views[sheet][span.First].IsSummary == collapsed) return;
            _views[sheet][span.First].IsSummary = collapsed;
            sheet.Rows[span.First].Locked = collapsed;
            _views[sheet].ResetItem(span.First);
            fpSpread1.Invalidate();
        }

        private void ExpandContainingOutline(SheetView sheet, int row)
        {
            foreach (var group in sheet.GetRangeGroupInfo(1, true) ?? Array.Empty<RangeGroupInfo>())
                if (row >= group.Start - 1 && row <= group.End && group.State == GroupState.Collapsed)
                {
                    sheet.ExpandRangeGroup(group, true, true);
                    var span = _outlines[sheet].First(item => item.First == group.Start - 1);
                    UpdateSummary(sheet, span, false);
                    break;
                }
        }

        private sealed class SpringOutlineRow
        {
            private readonly Func<string, float?> _memberLength;
            internal int RowId { get; }
            internal clsFixMember Source { get; set; }
            internal bool IsSummary { get; set; }

            internal SpringOutlineRow(int rowId, clsFixMember source, Func<string, float?> memberLength)
            {
                RowId = rowId;
                Source = source;
                _memberLength = memberLength;
            }

            public string? M
            {
                get => Source.M;
                set { if (!IsSummary) Source.M = value; }
            }
            public object? Length
            {
                get => IsSummary ? _memberLength(Source.m!)?.ToString("0.00", CultureInfo.InvariantCulture)
                    : Source.Length;
                set { if (!IsSummary) Source.Length = Number(value); }
            }
            public object? Tx { get => IsSummary ? "***" : Source.Tx;
                set { if (!IsSummary) Source.Tx = Number(value); } }
            public object? Ty { get => IsSummary ? "***" : Source.Ty;
                set { if (!IsSummary) Source.Ty = Number(value); } }
            public object? Tz { get => IsSummary ? "***" : Source.Tz;
                set { if (!IsSummary) Source.Tz = Number(value); } }
            public object? Tr { get => IsSummary ? "***" : Source.Tr;
                set { if (!IsSummary) Source.Tr = Number(value); } }

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
                ? _views[sheet][displayRow].RowId : -1;
        }

        private bool InsertRow(SheetView sheet, int displayRow, int column)
        {
            string? memberId = null;
            int row = SourceRow(sheet, displayRow);
            if (row < 1) return false;
            var current = _views[sheet][displayRow];
            if (current.IsSummary)
            {
                var span = _outlines[sheet].First(item => item.First == displayRow);
                memberId = span.Member;
                row = _views[sheet][span.First + span.Count - 1].RowId + 1;
            }
            else
                memberId = current.Source.m;
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
                if (_views[sheet][selected].IsSummary)
                {
                    var span = _outlines[sheet].First(item => item.First == selected);
                    for (int index = span.First; index < span.First + span.Count; index++)
                        indices.Add(_views[sheet][index].RowId);
                }
                else
                    indices.Add(_views[sheet][selected].RowId);
            }
            if (indices.Count == 0 || !_service.DeleteEditorRows(sheet.SheetName, indices)) return false;
            int next = Math.Min(indices.Min(), _service.GetRows(sheet.SheetName).Count);
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
                column[1].CellType = new NumberCellType() { DecimalPlaces = 3};
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

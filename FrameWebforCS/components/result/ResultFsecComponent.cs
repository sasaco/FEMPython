using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace FrameWebforCS.components.result
{
    public partial class ResultFsecComponent : UserControl
    {
        private readonly ResultFsecService _input = ResultFsecService.Instance;
        private bool _rebuilding;
        private int _materializedSheet = -1;

        public ResultFsecComponent()
        {
            InitializeComponent();
            fpSpread1.EditModeOn += fpSpread1.faSpread_EditModeOn;

            fpSpread1.ActiveSheetChanged += (_, _) => { MaterializeSelectedSheet(); PublishCurrentPage(); };
            VisibleChanged += (_, _) => { if (Visible) PublishCurrentPage(); };
            _input.Changed += OnResultsChanged;
            Disposed += (_, _) => _input.Changed -= OnResultsChanged;
            HandleCreated += (_, _) => RefreshResults();
            RefreshResults();

        }

        public void setActiveSheet(int index)
        {
            if (fpSpread1.Sheets.Count > 0 && index >= 0 && index < fpSpread1.Sheets.Count)
                fpSpread1.ActiveSheetIndex = index;
            MaterializeSelectedSheet();
            PublishCurrentPage();
        }

        private void PublishCurrentPage()
        {
            int index = fpSpread1.ActiveSheetIndex;
            var cases = _input.getFsec();
            if (Visible && index >= 0 && index < cases.Count)
                FrameWebforCS.three.ThreeResultsService.PublishPage("fsec", cases.Keys.ElementAt(index),
                    FrameWebforCS.three.ThreeResultsService.DefaultSectionForceComponent(
                        InputDataService.Instance.dimension));
        }

        private void OnResultsChanged(object? sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                if (IsHandleCreated) BeginInvoke((System.Action)RefreshResults);
                return;
            }
            RefreshResults();
        }

        private void RefreshResults()
        {
            _rebuilding = true;
            try
            {
                fpSpread1.Sheets.Clear();
                _materializedSheet = -1;
                foreach (var result in _input.getFsec())
                {
                    SheetView sheet = fpSpread1.AddNewSheetView();
                    sheet.SheetName = result.Key.Replace("Case", "", StringComparison.Ordinal);
                    SetSheet1(sheet);
                    sheet.RowCount = 0;
                    for (int column = 0; column < sheet.ColumnCount; column++)
                        sheet.Columns[column].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
                    sheet.Protect = true;
                }
                if (fpSpread1.Sheets.Count > 0)
                {
                    fpSpread1.ActiveSheetIndex = 0;
                    float width = 100;
                    var columns = fpSpread1.Sheets[0].Columns;
                    for (int column = 0; column < columns.Count; column++) width += columns[column].Width;
                    Width = (int)width;
                }
            }
            finally { _rebuilding = false; }
            MaterializeSelectedSheet();
        }

        private void MaterializeSelectedSheet()
        {
            if (_rebuilding) return;
            int selected = fpSpread1.ActiveSheetIndex;
            var cases = _input.getFsec();
            if (selected < 0 || selected >= cases.Count || selected >= fpSpread1.Sheets.Count) return;
            if (_materializedSheet == selected) return;
            if (_materializedSheet >= 0 && _materializedSheet < fpSpread1.Sheets.Count)
                fpSpread1.Sheets[_materializedSheet].RowCount = 0;

            SheetView sheet = fpSpread1.Sheets[selected];
            var result = cases.ElementAt(selected).Value;
            var members = InputMembersService.Instance.Members;
            var rows = new List<(string Member, string Node, double Station, double Fx,
                double Fy, double Fz, double Mx, double My, double Mz)>();
            foreach (var member in result)
            {
                string memberId = member.Key.Replace("member", "", StringComparison.Ordinal);
                clsMember? info = null;
                if (int.TryParse(memberId, NumberStyles.None, CultureInfo.InvariantCulture,
                    out int memberNumber) && memberNumber >= 1 && memberNumber <= members.Count)
                    info = members[memberNumber - 1];
                double station = 0;
                int count = member.Value.Count;
                for (int point = 1; point <= count; point++)
                {
                    if (!member.Value.TryGetValue($"P{point}", out clsFsec? segment)) break;
                    if (point == 1)
                        rows.Add((memberId, info?.ni ?? "", station,
                            segment.fxi ?? 0, segment.fyi ?? 0, segment.fzi ?? 0,
                            segment.mxi ?? 0, segment.myi ?? 0, segment.mzi ?? 0));
                    station += JsRound(segment.L ?? 0, 1000);
                    rows.Add(("", point == count ? info?.nj ?? "" : "", station,
                        segment.fxj ?? 0, segment.fyj ?? 0, segment.fzj ?? 0,
                        segment.mxj ?? 0, segment.myj ?? 0, segment.mzj ?? 0));
                }
            }

            sheet.RowCount = rows.Count;
            bool is3D = sheet.ColumnCount == 9;
            for (int row = 0; row < rows.Count; row++)
            {
                var value = rows[row];
                sheet.Cells[row, 0].Text = value.Member;
                sheet.Cells[row, 1].Text = value.Node;
                sheet.Cells[row, 2].Text = value.Station.ToString("F3", CultureInfo.InvariantCulture);
                sheet.Cells[row, 3].Text = Format(value.Fx);
                sheet.Cells[row, 4].Text = Format(value.Fy);
                if (is3D)
                {
                    sheet.Cells[row, 5].Text = Format(value.Fz);
                    sheet.Cells[row, 6].Text = Format(value.Mx);
                    sheet.Cells[row, 7].Text = Format(value.My);
                    sheet.Cells[row, 8].Text = Format(value.Mz);
                }
                else sheet.Cells[row, 5].Text = Format(value.Mz);
            }
            _materializedSheet = selected;
        }

        private static double JsRound(double value, double scale) =>
            Math.Floor(value * scale + 0.5) / scale;

        private static string Format(double value) =>
            JsRound(value, 100).ToString("F2", CultureInfo.InvariantCulture);

        internal static void SetSheet1(SheetView _Sheet)
        {
            var header = _Sheet.ColumnHeader;
            header.RowCount = 2;

            if (InputDataService.Instance.dimension == 3)
            {
                _Sheet.ColumnCount = 9;

                header.Cells[0, 0].Text = "部材";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "節点";
                header.Cells[1, 1].Text = "No";
                header.Cells[0, 2].Text = "着目位置";
                header.Cells[1, 2].Text = "(m)";
                header.Cells[0, 3].Text = "軸方向力";
                header.Cells[1, 3].Text = "(kN)";
                header.Cells[0, 4].Text = "せん断力(kN)";
                header.Cells[1, 4].Text = "Y軸方向";
                header.Cells[0, 5].Text = "";
                header.Cells[1, 5].Text = "Z軸方向";
                header.Cells[0, 6].Text = "ねじりモーメント";
                header.Cells[1, 6].Text = "(kN・m)";
                header.Cells[0, 7].Text = "曲げモーメント(kN・m)";
                header.Cells[1, 7].Text = "Y軸回り";
                header.Cells[0, 8].Text = "";
                header.Cells[1, 8].Text = "Z軸回り";

                header.Cells[0, 4].ColumnSpan = 2;
                header.Cells[0, 7].ColumnSpan = 2;

                var column = _Sheet.Columns;
                column[0].Width = 50;
                column[1].Width = 50;
            }
            else
            {
                _Sheet.ColumnCount = 6;

                header.Cells[0, 0].Text = "部材";
                header.Cells[1, 0].Text = "No";
                header.Cells[0, 1].Text = "節点";
                header.Cells[1, 1].Text = "No";
                header.Cells[0, 2].Text = "着目位置";
                header.Cells[1, 2].Text = "(m)";
                header.Cells[0, 3].Text = "軸方向力";
                header.Cells[1, 3].Text = "(kN)";
                header.Cells[0, 4].Text = "せん断力";
                header.Cells[1, 4].Text = "(kN)";
                header.Cells[0, 5].Text = "曲げモーメント";
                header.Cells[1, 5].Text = "(kN・m)";

                var column = _Sheet.Columns;
                column[0].Width = 50;
                column[1].Width = 50;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PDF_Manager.Printing.Comon;
using PdfSharpCore.Drawing;

namespace PDF_Manager.Printing
{
    internal static class ReferenceResultLayout
    {
        internal static string Provenance(string source, string expression) =>
            string.IsNullOrEmpty(expression) ? source ?? "" : "(" + source + " : " + expression + ")";

        internal static List<string> Wrap(PdfDocument document, XFont font, string text, double width)
        {
            var lines = new List<string>();
            string line = "";
            // Keep a decimal or scientific coefficient readable as one token.
            // Oversized tokens still wrap by character so long identifiers and
            // captions cannot prevent pagination from making progress.
            foreach (Match match in Regex.Matches(text ?? "", @"[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?|[\s\S]"))
            {
                string token = match.Value;
                if (document.gfx.MeasureString(token, font).Width <= width)
                {
                    if (line.Length > 0 && document.gfx.MeasureString(line + token, font).Width > width)
                    { lines.Add(line); line = ""; }
                    line += token;
                    continue;
                }
                foreach (char character in token)
                {
                    if (line.Length > 0 && document.gfx.MeasureString(line + character, font).Width > width)
                    { lines.Add(line); line = ""; }
                    line += character;
                }
            }
            lines.Add(line);
            return lines;
        }

        private static IReadOnlyList<string[]> WrapPanels(PdfDocument document, XFont font,
            string[] cells, double[] widths, int[] starts)
        {
            var lines = new Dictionary<int, List<string>>();
            foreach (int column in starts)
            {
                int end = starts.FirstOrDefault(next => next > column);
                if (end == 0) end = widths.Length;
                lines[column] = Wrap(document, font, cells[column], widths.Skip(column).Take(end - column).Sum() - 12);
            }
            var result = new List<string[]>();
            for (int row = 0; row < lines.Values.Select(value => value.Count).DefaultIfEmpty(1).Max(); row++)
            {
                var output = new string[widths.Length];
                foreach (int column in starts)
                    if (row < lines[column].Count) output[column] = lines[column][row];
                result.Add(output);
            }
            return result;
        }

        internal sealed class Row
        {
            internal readonly string[] Cells;
            internal readonly string[] Caption;
            internal readonly bool IsCaption;
            internal readonly double Height;
            internal Row(string[] cells, string[] caption = null, bool isCaption = false, double height = 0)
            { Cells = cells; Caption = caption; IsCaption = isCaption; Height = height; }
        }

        // FrameG's coordinates are A4 portrait points; other papers keep the same font and distances.
        internal static XFont ReportFont(PdfDocument document, PrintData data) =>
            new XFont(data.language == "cn" ? "SimSun" : "MS Mincho", PdfDocument.ReferencePortraitFontSize);

        // FrameG's case heading: the ID right-aligned in two columns, then the separator and the name.
        internal static string FrameGCaseTitle(string key, string name, string separator)
        {
            int suffix = key.IndexOf("/Static/", StringComparison.Ordinal);
            string id = suffix < 0 ? key : key.Substring(0, suffix);
            return $"CASE {id,2}" + (name == id || string.IsNullOrEmpty(name) ? "" : separator + name);
        }

        // These are report panels, not Table.Nup's successive rows of the same case: FrameG's basic member
        // forces (squareViewport) and the paired COMBINE displacements ("Displacement").
        internal static void Print(PdfDocument document, PrintData data, ref int page,
            string title, string[][] headers, double[] widths, IReadOnlyList<Row> rows,
            bool newGroup = false, int[] panelStarts = null, bool keepMemberTogether = false,
            bool headersOnlyAtPageStart = false, bool wrapFirstHeader = true, bool squareViewport = false)
        {
            if (rows.Count == 0) return;
            if (newGroup && !document.currentPos.Equals(document.initialPos)) document.NewPage(ref page);
            const double lineHeight = 9;
            var font = ReportFont(document, data);
            // FrameG fills the page with one case's member forces (原宿ボックス 0404).
            double pageHeight = document.currentPage.Height;
            bool referenceBasic = squareViewport;
            bool referenceDisplacement = !squareViewport;
            double bottomMargin = referenceDisplacement ? 15 : document.Margine.Bottom;
            if (referenceBasic)
            {
                widths = new double[] { 137, 69, 58, 58, 58 };
                headers = new[] { headers[1], headers[0] };
                wrapFirstHeader = false;
            }
            else
            {
                widths = new double[] { 59, 58, 50, 67, 16, 59, 58, 50, 67 };
                headers = headers.Select(row => row.ToArray()).ToArray();
                headers[0][0] = headers[0][5] = "";
            }
            // A case caption can include a long load name and a weighted expression.
            // Wrap within each panel so it cannot overwrite its neighbour's caption.
            var starts = panelStarts ?? Enumerable.Range(0, widths.Length).Where(column => !string.IsNullOrEmpty(headers[0][column])).ToArray();
            if (wrapFirstHeader)
                headers = WrapPanels(document, font, headers[0], widths, starts).Concat(headers.Skip(1)).ToArray();
            var captions = new Dictionary<string[], IReadOnlyList<string[]>>();
            foreach (Row row in rows.Where(row => row.Caption != null))
                if (!captions.ContainsKey(row.Caption))
                    captions[row.Caption] = WrapPanels(document, font, row.Caption, widths, starts);
            var captionHeights = rows.Where(row => row.IsCaption).GroupBy(row => row.Caption)
                .ToDictionary(group => group.Key, group => group.First().Height);
            rows = rows.SelectMany(row => row.IsCaption
                ? captions[row.Caption].Select((cells, index) =>
                    new Row(cells, row.Caption, true, index == 0 ? row.Height : lineHeight))
                : new[] { row }).ToArray();
            double Height(Row row) => row.Height > 0 ? row.Height : lineHeight;
            int CaptionRows(int index) => rows.Skip(index).TakeWhile(row => row.IsCaption &&
                ReferenceEquals(row.Caption, rows[index].Caption)).Count();
            int next = 0;
            while (next < rows.Count)
            {
                document.CheckBudget();
                bool titleRequired = document.currentPos.Equals(document.initialPos);
                string[][] pageHeaders = headersOnlyAtPageStart && !titleRequired ? new string[0][] : headers;
                double available = pageHeight - bottomMargin - document.currentPos.Y
                    - (titleRequired ? printManager.LineSpacing2 : 0) - pageHeaders.Length * lineHeight;
                bool continued = !rows[next].IsCaption && rows[next].Caption != null;
                double captionHeight = continued ? captionHeights[rows[next].Caption] : 0;
                if (captionHeight <= 0) captionHeight = lineHeight;
                var continuationRows = continued ? captions[rows[next].Caption]
                    .Select((cells, index) => new Row(cells, rows[next].Caption, true,
                        index == 0 ? captionHeight : lineHeight)).ToArray() : Array.Empty<Row>();
                double minimumHeight = continued ? continuationRows.Sum(Height) + Height(rows[next]) :
                    rows[next].IsCaption ? rows.Skip(next).Take(CaptionRows(next) + 1).Sum(Height) : Height(rows[next]);
                if (available < minimumHeight)
                {
                    if (document.currentPos.Equals(document.initialPos))
                        throw new InvalidOperationException("A report caption leaves no room for a data row on the selected paper.");
                    document.NewPage(ref page);
                    continue;
                }
                var pageRows = new List<Row>();
                if (continued)
                {
                    pageRows.AddRange(continuationRows);
                    available -= continuationRows.Sum(Height);
                }
                while (next < rows.Count)
                {
                    // Keep a member caption with at least one of its data rows.
                    if (rows[next].IsCaption)
                    {
                        if (available < rows.Skip(next).Take(CaptionRows(next) + 1).Sum(Height)) break;
                        if (keepMemberTogether)
                        {
                            int captionRows = CaptionRows(next);
                            var block = rows.Skip(next).Take(captionRows).Concat(rows.Skip(next + captionRows).TakeWhile(row => !row.IsCaption));
                            double blockHeight = block.Sum(Height);
                            double emptyAvailable = pageHeight - bottomMargin - document.initialPos.Y
                                - printManager.LineSpacing2 - headers.Length * lineHeight;
                            if (blockHeight <= emptyAvailable && blockHeight > available) break;
                        }
                    }
                    if (Height(rows[next]) > available) break;
                    available -= Height(rows[next]);
                    pageRows.Add(rows[next++]);
                }
                if (pageRows.Count == 0) { document.NewPage(ref page); continue; }
                var table = new Table(pageHeaders.Length + pageRows.Count, widths.Length) { LineSpacing3 = lineHeight };
                for (int column = 0; column < widths.Length; column++) table.ColWidth[column] = widths[column];
                if (referenceBasic && pageHeaders.Length == 2)
                {
                    table.RowHeight[0] = 11.6;
                    table.RowHeight[1] = 11.6;
                }
                for (int row = 0; row < pageRows.Count; row++) table.RowHeight[pageHeaders.Length + row] = Height(pageRows[row]);
                var cells = pageHeaders.Concat(pageRows.Select(row => row.Cells)).ToArray();
                for (int row = 0; row < cells.Length; row++)
                for (int column = 0; column < cells[row].Length; column++)
                {
                    bool dataRow = row >= pageHeaders.Length && !pageRows[row - pageHeaders.Length].IsCaption;
                    // FrameG right-aligns the basic values on its own column edges; drawn below.
                    bool basicLabelOrValue = referenceBasic && dataRow;
                    bool basicCaption = referenceBasic && column == 0 &&
                        (row == 1 || row >= pageHeaders.Length && pageRows[row - pageHeaders.Length].IsCaption);
                    table[row, column] = basicLabelOrValue || basicCaption ? "" : cells[row][column];
                    table.AlignX[row, column] = referenceBasic ? (dataRow && column == 0 ? "R" : "L")
                        : dataRow && (column == 0 || column == 5) ? "C"
                        : row < pageHeaders.Length || starts.Contains(column) ? "L" : "R";
                }
                if (titleRequired)
                {
                    document.currentPos.X = 115.7;
                    string heading = referenceDisplacement ? "変 位 量" : title;
                    Text.PrtText(document, heading, font);
                    if (referenceBasic)
                    {
                        // FrameG underlines the report title with a hairline.
                        double width = document.gfx.MeasureString(heading, font).Width;
                        document.gfx.DrawLine(new XPen(XColors.Black, 0.12), document.currentPos.X - 3.7,
                            document.currentPos.Y + 2.1, document.currentPos.X + width + 6.7, document.currentPos.Y + 2.1);
                    }
                    document.addCurrentY(referenceBasic ? -0.8 : 13.4);
                }
                double tableStartX = referenceBasic ? 115.7 : 75.1;
                document.currentPos.X = tableStartX;
                double tableStartY = document.currentPos.Y;
                table.PrintTable(document, font);
                if (referenceBasic || pageHeaders.Length == 2)
                {
                    var tableEnd = document.currentPos;
                    if (referenceBasic)
                    {
                        double baselineY = tableStartY;
                        for (int row = 0; row < cells.Length; row++)
                        {
                            baselineY += table.RowHeight[row] > 0 ? table.RowHeight[row] : lineHeight;
                            if (row == 1)
                            {
                                document.currentPos = new XPoint(132.6, baselineY);
                                Text.PrtText(document, cells[row][0], font);
                            }
                            if (row < pageHeaders.Length) continue;
                            if (pageRows[row - pageHeaders.Length].IsCaption)
                            {
                                document.currentPos = new XPoint(146.1, baselineY);
                                Text.PrtText(document, cells[row][0], font);
                                continue;
                            }
                            if (!string.IsNullOrEmpty(cells[row][0]))
                            {
                                document.currentPos = new XPoint(tableStartX + widths[0] - 20, baselineY);
                                Text.PrtText(document, cells[row][0], font, XStringFormats.BottomRight);
                            }
                            for (int column = 1; column < cells[row].Length; column++)
                            {
                                if (string.IsNullOrEmpty(cells[row][column])) continue;
                                document.currentPos = new XPoint(tableStartX + BasicValueRight[column - 1], baselineY);
                                Text.PrtText(document, cells[row][column], font, XStringFormats.BottomRight);
                            }
                        }
                    }
                    else
                        foreach (double x in new[] { 89.8, 339.8 })
                        {
                            document.currentPos = new XPoint(x, tableStartY + table.RowHeight[0]);
                            Text.PrtText(document, "JOINT", font);
                            document.currentPos = new XPoint(x, tableStartY + table.RowHeight[0] + table.RowHeight[1]);
                            Text.PrtText(document, "NO.", font);
                        }
                    document.currentPos = tableEnd;
                }
                document.addCurrentY(printManager.LineSpacing1);
                document.addCurrentY((referenceDisplacement ? 0 : 5) - printManager.LineSpacing1);
                if (next < rows.Count) document.NewPage(ref page);
            }
        }


        // FrameG's displacement report (原宿ボックス 0404, 5-18): one column of cases, a case kept whole.
        // Positions are FrameG's in points; values are right-aligned on their column edges.
        internal static void FrameGDisplacements(PdfDocument document, PrintData data, ref int page,
            IReadOnlyList<(string Caption, IReadOnlyList<Disg> Rows)> cases)
        {
            const double captionGap = 17.9, rowPitch = 8.97, caseGap = 18.1;
            var font = ReportFont(document, data);
            double bottom = document.currentPage.Height - document.Margine.Bottom;
            bool pageStarted = false;
            int pageNo = page; // A local function cannot capture the ref parameter.
            void Header()
            {
                if (!document.currentPos.Equals(document.initialPos)) document.NewPage(ref pageNo);
                document.currentPos = new XPoint(115.7, document.currentPos.Y);
                const string title = "変  位  量";
                Text.PrtText(document, title, font);
                double width = document.gfx.MeasureString(title, font).Width;
                document.gfx.DrawLine(new XPen(XColors.Black, 0.12), 105.1, document.currentPos.Y + 2.1,
                    115.7 + width + 3.7, document.currentPos.Y + 2.1);
                double y = document.currentPos.Y + 13.4;
                void Put(string text, double x, double baseline, bool right = false)
                {
                    document.currentPos = new XPoint(x, baseline);
                    if (right) Text.PrtText(document, text, font, XStringFormats.BottomRight);
                    else Text.PrtText(document, text, font);
                }
                Put("JOINT", 252.6, y);
                Put("X-DISP.", 328.5, y, true);
                Put("Y-DISP.", 386.1, y, true);
                Put("ROTATION", 441.4, y, true);
                Put("NO.", 275.0, y + 9, true);
                Put("(mm)", 319.5, y + 9, true);
                Put("(mm)", 377.1, y + 9, true);
                Put("(mRad)", 436.9, y + 9, true);
                document.currentPos = new XPoint(document.currentPos.X, y + 9);
                pageStarted = true;
            }
            const double captionX = 136.1;
            double captionWidth = document.currentPage.Width - document.Margine.Right - captionX;
            foreach (var (caption, rows) in cases)
            {
                if (rows.Count == 0) continue;
                document.CheckBudget();
                // A long case name wraps within the page; one that cannot leave room for a row fails first.
                var lines = Wrap(document, font, caption, captionWidth);
                double captionHeight = captionGap + (lines.Count - 1) * rowPitch;
                double emptyPage = bottom - (document.initialPos.Y + 13.4 + 9); // Below a fresh page's header.
                if (captionHeight + rowPitch > emptyPage)
                    throw new InvalidOperationException("A displacement case caption leaves no room for a row on the selected paper.");
                double need = captionHeight + rows.Count * rowPitch;
                // FrameG keeps a case whole: one that does not fit in the rest of the page starts a new one
                // (and a case longer than a page continues from there with its caption repeated).
                if (!pageStarted || need > bottom - document.currentPos.Y) Header();
                double y = document.currentPos.Y;
                void Caption()
                {
                    foreach (string line in lines)
                    {
                        y += line == lines[0] ? captionGap : rowPitch;
                        document.currentPos = new XPoint(captionX, y);
                        Text.PrtText(document, line, font);
                    }
                }
                Caption();
                foreach (Disg row in rows)
                {
                    if (y + rowPitch > bottom)
                    {
                        // A case longer than a page continues with its caption repeated.
                        Header();
                        y = document.currentPos.Y;
                        Caption();
                    }
                    y += rowPitch;
                    foreach (var (text, right) in new[] { (row.id, 271.4), (Number(row.dx, 4), 328.5),
                        (Number(row.dy, 4), 386.1), (Number(row.rz, 4), 436.9) })
                    {
                        document.currentPos = new XPoint(right, y);
                        Text.PrtText(document, text, font, XStringFormats.BottomRight);
                    }
                }
                document.currentPos = new XPoint(document.currentPos.X, y + caseGap - captionGap);
            }
            page = pageNo;
        }

        // FrameG never prints a negative zero ("-0.00" is "0.00").
        internal static string Number(double value, int digits)
        {
            string text = printManager.toString(value, digits);
            return text.StartsWith("-", StringComparison.Ordinal) && text.Skip(1).All(c => c == '0' || c == '.')
                ? text.Substring(1) : text;
        }

        // Right edges of DIST, M, S and N from the table's left edge in FrameG's member forces.
        private static readonly double[] BasicValueRight = { 158.4, 222.8, 280.4, 337.9 };

        // The paired COMBINE displacements ("Combine変位量", not part of the FrameG comparison).
        internal static void Displacements(PdfDocument document, PrintData data, ref int page,
            string leftTitle, IReadOnlyList<Disg> left, string rightTitle, IReadOnlyList<Disg> right)
        {
            var font = ReportFont(document, data);
            double idWidth = Math.Max(document.gfx.MeasureString("JOINT NO.", font).Width,
                left.Concat(right).Select(row => document.gfx.MeasureString(row.id ?? "", font).Width).DefaultIfEmpty(0).Max()) + 8;
            if (idWidth > document.currentPageSize.Width / 2 - 3 * 55)
                throw new InvalidOperationException("A displacement node ID is too wide for the paired report.");
            var rows = new List<Row>();
            string[] caption = { leftTitle, "", "", "", "", rightTitle, "", "", "" };
            rows.Add(new Row(caption, caption, true, 17.9));
            for (int i = 0; i < Math.Max(left.Count, right.Count); i++)
                rows.Add(new Row(DisplacementCells(i < left.Count ? left[i] : null)
                    .Concat(new[] { "" }).Concat(DisplacementCells(i < right.Count ? right[i] : null)).ToArray(),
                    caption, height: i == 0 ? 13.5 : 0));
            Print(document, data, ref page, "Displacement",
                new[] {
                    new[] { "JOINT NO.", "X-DISP.", "Y-DISP.", "ROTATION", "", "JOINT NO.", "X-DISP.", "Y-DISP.", "ROTATION" },
                    new[] { "", "(mm)", "(mm)", "(mRad)", "", "", "(mm)", "(mm)", "(mRad)" } },
                new[] { idWidth, 75, 75, 75, 20, idWidth, 75, 75, 75 }, rows,
                panelStarts: new[] { 0, 5 }, headersOnlyAtPageStart: true, wrapFirstHeader: false);
        }

        private static string[] DisplacementCells(Disg row) => row == null ? new[] { "", "", "", "" }
            : new[] { row.id, printManager.toString(row.dx, 4), printManager.toString(row.dy, 4), printManager.toString(row.rz, 4) };

        // FrameG's basic member forces (原宿ボックス 0404, 5-7..5-17): one case filling the page.
        internal static void SingleForces(PdfDocument document, PrintData data, ref int page,
            string caption, IReadOnlyList<Fsec> forces, bool newGroup)
        {
            double[] widths = { 50, 50, 75, 75, 75 };
            var rows = new List<Row>();
            string member = null;
            string[] memberCaption = null;
            foreach (var force in forces)
            {
                bool first = member != force.m;
                if (first)
                {
                    member = force.m;
                    memberCaption = new string[widths.Length];
                    memberCaption[0] = string.IsNullOrEmpty(force.memberCaption) ? "MEMBER " + member : force.memberCaption;
                    rows.Add(new Row(memberCaption, memberCaption, true, 13.5));
                }
                var cells = new[] { string.IsNullOrEmpty(force.stationLabel) ? force.n : force.stationLabel,
                    printManager.toString(force.l, 3), Number(force.mz, 2), Number(force.fy, 2), Number(force.fx, 2) };
                rows.Add(new Row(cells, memberCaption, height: first ? 13.5 : 9));
            }
            var heading = new string[widths.Length];
            heading[0] = caption;
            Print(document, data, ref page, "MEMBER FORCES", new[] { heading,
                    new[] { "", "DIST.", "M", "S", "N" }, new[] { "", "(m)", "(kN m)", "(kN)", "(kN)" } },
                widths, rows, newGroup, new[] { 0 }, keepMemberTogether: true, squareViewport: true);
        }
    }
}

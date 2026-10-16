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

        private static string CompactProvenance(string source, string expression) =>
            string.IsNullOrEmpty(expression) ? source ?? "" : "(" + source + ":" +
                new string(expression.Where(character => !char.IsWhiteSpace(character)).ToArray()) + ")";

        private static List<string> Wrap(PdfDocument document, XFont font, string text, double width)
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

        internal static bool Portrait(PdfDocument document) => document.currentPage.Width < document.currentPage.Height;

        private static XFont ReportFont(PdfDocument document, PrintData data) =>
            new XFont(data.language == "cn" ? "SimSun" : "MS Mincho",
                Portrait(document) ? PdfDocument.ReferencePortraitFontSize : 10);

        internal static bool Enabled(Dictionary<string, object> root) =>
            root.TryGetValue("referenceResultLayout", out var enabled) && Convert.ToBoolean(enabled);

        internal static string CaseTitle(string key, string name)
        {
            // Canonical lookup keys include result kind and step; report captions do not.
            int suffix = key.IndexOf("/Static/", StringComparison.Ordinal);
            string id = suffix < 0 ? key : key.Substring(0, suffix);
            return "CASE " + id + (name == id ? "" : " " + name);
        }

        // These are report panels, not Table.Nup's successive rows of the same case.
        internal static void Print(PdfDocument document, PrintData data, ref int page,
            string title, string[][] headers, double[] widths, IReadOnlyList<Row> rows,
            bool newGroup = false, int[] panelStarts = null, bool keepMemberTogether = false,
            bool headersOnlyAtPageStart = false, bool wrapFirstHeader = true, bool squareViewport = false)
        {
            if (rows.Count == 0) return;
            if (newGroup && !document.currentPos.Equals(document.initialPos)) document.NewPage(ref page);
            double lineHeight = Portrait(document) ? 9 : 10.5;
            var font = ReportFont(document, data);
            // The portrait basic-force reference keeps the compact table viewport;
            // its tall paper does not expand that report's vertical composition.
            double pageHeight = squareViewport ? Math.Min(document.currentPage.Height, document.currentPage.Width) : (double)document.currentPage.Height;
            bool referenceBasic = data.referenceResultLayout && Portrait(document) && squareViewport;
            bool referencePickup = data.referenceResultLayout && Portrait(document) &&
                (title.StartsWith("PICK UP ", StringComparison.Ordinal) || title.StartsWith("COMBINE ", StringComparison.Ordinal));
            bool referenceDisplacement = data.referenceResultLayout && Portrait(document) && title == "Displacement";
            double bottomMargin = referenceDisplacement ? 15 : document.Margine.Bottom;
            if (referenceBasic)
            {
                widths = new double[] { 137, 69, 58, 58, 58 };
                headers = new[] { headers[1], headers[0] };
                wrapFirstHeader = false;
            }
            else if (referencePickup)
            {
                widths = new double[] { 79.3, 68.7, 54.1, 54.2, 37.8, 144.3 };
                headers = new[] { headers[0], headers[1] };
                wrapFirstHeader = false;
            }
            else if (referenceDisplacement)
            {
                widths = new double[] { 59, 58, 50, 67, 16, 59, 58, 50, 67 };
                headers = headers.Select(row => row.ToArray()).ToArray();
                headers[0][0] = headers[0][5] = "";
            }
            else
            {
                double scale = document.currentPageSize.Width / widths.Sum();
                widths = widths.Select(width => width * scale).ToArray();
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
                if (referencePickup && pageHeaders.Length == 2)
                {
                    table.RowHeight[0] = 13.5;
                    table.RowHeight[1] = 13.4;
                }
                for (int row = 0; row < pageRows.Count; row++) table.RowHeight[pageHeaders.Length + row] = Height(pageRows[row]);
                var cells = pageHeaders.Concat(pageRows.Select(row => row.Cells)).ToArray();
                for (int row = 0; row < cells.Length; row++)
                for (int column = 0; column < cells[row].Length; column++)
                {
                    bool stationLabel = (referenceBasic || referencePickup) && column == 0 &&
                        row >= pageHeaders.Length && !pageRows[row - pageHeaders.Length].IsCaption;
                    bool basicCaption = referenceBasic && column == 0 &&
                        (row == 1 || row >= pageHeaders.Length && pageRows[row - pageHeaders.Length].IsCaption);
                    table[row, column] = stationLabel || basicCaption ? "" : cells[row][column];
                    table.AlignX[row, column] = (referenceBasic || referencePickup) && column == 0 &&
                        row >= pageHeaders.Length && !pageRows[row - pageHeaders.Length].IsCaption ? "R"
                        : (referenceBasic || referencePickup) ? "L"
                        : referenceDisplacement && row >= pageHeaders.Length &&
                        !pageRows[row - pageHeaders.Length].IsCaption && (column == 0 || column == 5) ? "C"
                        : row < pageHeaders.Length || starts.Contains(column) ? "L" : "R";
                }
                if (referenceBasic || referencePickup || referenceDisplacement)
                {
                    if (titleRequired)
                    {
                        document.currentPos.X = referencePickup ? 91.9 : 115.7;
                        Text.PrtText(document, referenceDisplacement ? "変 位 量" : title, font);
                        document.addCurrentY(referenceBasic ? -0.8 : referencePickup ? -0.1 : 13.4);
                    }
                    double tableStartX = referenceBasic ? 115.7 : referencePickup ? 91.9 : 75.1;
                    document.currentPos.X = tableStartX;
                    double tableStartY = document.currentPos.Y;
                    table.PrintTable(document, font);
                    if (referenceBasic || referencePickup || referenceDisplacement && pageHeaders.Length == 2)
                    {
                        var tableEnd = document.currentPos;
                        if (referenceBasic || referencePickup)
                        {
                            double baselineY = tableStartY;
                            for (int row = 0; row < cells.Length; row++)
                            {
                                baselineY += table.RowHeight[row] > 0 ? table.RowHeight[row] : lineHeight;
                                if (referenceBasic && row == 1)
                                {
                                    document.currentPos = new XPoint(132.6, baselineY);
                                    Text.PrtText(document, cells[row][0], font);
                                }
                                if (row < pageHeaders.Length) continue;
                                if (referenceBasic && pageRows[row - pageHeaders.Length].IsCaption)
                                {
                                    document.currentPos = new XPoint(146.1, baselineY);
                                    Text.PrtText(document, cells[row][0], font);
                                }
                                else if (!pageRows[row - pageHeaders.Length].IsCaption &&
                                    !string.IsNullOrEmpty(cells[row][0]))
                                {
                                    document.currentPos = new XPoint(tableStartX + widths[0] - 20, baselineY);
                                    Text.PrtText(document, cells[row][0], font, XStringFormats.BottomRight);
                                }
                            }
                        }
                        if (referenceDisplacement && pageHeaders.Length == 2)
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
                }
                else
                    printManager.printTableContents(document, table, new[] { title }, titleRequired, font);
                document.addCurrentY((referenceDisplacement ? 0 : 5) - printManager.LineSpacing1);
                if (next < rows.Count) document.NewPage(ref page);
            }
        }

        internal static void Displacements(PdfDocument document, PrintData data, ref int page,
            string leftTitle, IReadOnlyList<Disg> left, string rightTitle, IReadOnlyList<Disg> right)
        {
            var font = ReportFont(document, data);
            double idWidth = Math.Max(document.gfx.MeasureString("JOINT NO.", font).Width,
                left.Concat(right).Select(row => document.gfx.MeasureString(row.id ?? "", font).Width).DefaultIfEmpty(0).Max()) + 8;
            if (idWidth > document.currentPageSize.Width / 2 - 3 * 55)
                throw new InvalidOperationException("A displacement node ID is too wide for the paired report.");
            var rows = new List<Row>();
            string[] caption = null;
            if (Portrait(document))
            {
                caption = new[] { leftTitle, "", "", "", "", rightTitle, "", "", "" };
                rows.Add(new Row(caption, caption, true, data.referenceResultLayout ? 17.9 : 13.5));
            }
            for (int i = 0; i < Math.Max(left.Count, right.Count); i++)
                rows.Add(new Row(DisplacementCells(i < left.Count ? left[i] : null)
                    .Concat(new[] { "" }).Concat(DisplacementCells(i < right.Count ? right[i] : null)).ToArray(),
                    caption, height: Portrait(document) && i == 0 ? 13.5 : 0));
            if (Portrait(document))
            {
                Print(document, data, ref page, "Displacement",
                    new[] {
                        new[] { "JOINT NO.", "X-DISP.", "Y-DISP.", "ROTATION", "", "JOINT NO.", "X-DISP.", "Y-DISP.", "ROTATION" },
                        new[] { "", "(mm)", "(mm)", "(mRad)", "", "", "(mm)", "(mm)", "(mRad)" } },
                    new[] { idWidth, 75, 75, 75, 20, idWidth, 75, 75, 75 }, rows,
                    panelStarts: new[] { 0, 5 }, headersOnlyAtPageStart: true, wrapFirstHeader: false);
                return;
            }
            Print(document, data, ref page, "Displacement",
                new[] {
                    new[] { leftTitle, "", "", "", "", rightTitle, "", "", "" },
                    new[] { "JOINT NO.", "X-DISP.", "Y-DISP.", "ROTATION", "", "JOINT NO.", "X-DISP.", "Y-DISP.", "ROTATION" },
                    new[] { "", "(mm)", "(mm)", "(mRad)", "", "", "(mm)", "(mm)", "(mRad)" } },
                new[] { idWidth, 75, 75, 75, 20, idWidth, 75, 75, 75 }, rows, panelStarts: new[] { 0, 5 });
        }

        private static string[] DisplacementCells(Disg row) => row == null ? new[] { "", "", "", "" }
            : new[] { row.id, printManager.toString(row.dx, 4), printManager.toString(row.dy, 4), printManager.toString(row.rz, 4) };

        internal static void Forces(PdfDocument document, PrintData data, ref int page,
            string leftTitle, IReadOnlyList<Fsec> left, string rightTitle, IReadOnlyList<Fsec> right,
            bool envelope, bool newGroup)
        {
            double[] widths = envelope ? new double[] { 50, 45, 50, 50, 50, 130, 12, 50, 50, 50, 130 }
                : new double[] { 50, 50, 75, 75, 75, 20, 50, 50, 75, 75, 75 };
            var font = new XFont(data.language == "cn" ? "SimSun" : "MS Mincho", 10);
            double sourceWidth = widths[5] * document.currentPageSize.Width / widths.Sum() - 4;
            // Include occurrence in the key: equal positions and repeated station IDs may
            // represent different physical sides and must never be silently collapsed.
            Dictionary<string, Fsec> Index(IReadOnlyList<Fsec> values)
            {
                var occurrences = new Dictionary<string, int>();
                var index = new Dictionary<string, Fsec>();
                foreach (var row in values)
                {
                    string identity = row.m + "\0" + row.n;
                    occurrences.TryGetValue(identity, out int occurrence);
                    occurrences[identity] = occurrence + 1;
                    index.Add(identity + "\0" + occurrence, row);
                }
                return index;
            }
            var a = Index(left);
            var b = Index(right);
            var keys = a.Keys.Concat(b.Keys.Where(key => !a.ContainsKey(key)));
            var rows = new List<Row>();
            string member = null;
            string[] caption = null;
            foreach (string key in keys)
            {
                a.TryGetValue(key, out var x);
                b.TryGetValue(key, out var y);
                Fsec location = x ?? y;
                if (member != location.m)
                {
                    member = location.m;
                    caption = new string[11];
                    caption[0] = location.memberCaption ?? "MEMBER " + location.m;
                    if (!envelope) caption[6] = (y ?? x).memberCaption ?? "MEMBER " + location.m;
                    rows.Add(new Row(caption, caption, true));
                }
                string[] cells;
                if (envelope)
                    cells = new[] { location.stationLabel ?? location.n, printManager.toString(location.l, 3) }
                        .Concat(ForceCells(x, true)).Concat(new[] { "" }).Concat(ForceCells(y, true)).ToArray();
                else
                    cells = StationForceCells(x).Concat(new[] { "" }).Concat(StationForceCells(y)).ToArray();
                if (!envelope) { rows.Add(new Row(cells, caption)); continue; }
                var sourceA = Wrap(document, font, cells[5], sourceWidth);
                var sourceB = Wrap(document, font, cells[10], sourceWidth);
                int lines = Math.Max(sourceA.Count, sourceB.Count);
                for (int line = 0; line < lines; line++)
                {
                    var output = line == 0 ? cells : new string[11];
                    output[5] = line < sourceA.Count ? sourceA[line] : "";
                    output[10] = line < sourceB.Count ? sourceB[line] : "";
                    rows.Add(new Row(output, caption));
                }
            }
            var headers = envelope ? new[] {
                new[] { leftTitle, "", "", "", "", "", "", rightTitle, "", "", "" },
                new[] { "", "DIST.", "M", "S", "N", "COM: CASE", "", "M", "S", "N", "COM: CASE" },
                new[] { "", "(m)", "(kN m)", "(kN)", "(kN)", "", "", "(kN m)", "(kN)", "(kN)", "" } }
                : new[] {
                    new[] { leftTitle, "", "", "", "", "", rightTitle, "", "", "", "" },
                    new[] { "", "DIST.", "M", "S", "N", "", "", "DIST.", "M", "S", "N" },
                    new[] { "", "(m)", "(kN m)", "(kN)", "(kN)", "", "", "(m)", "(kN m)", "(kN)", "(kN)" } };
            Print(document, data, ref page, "MEMBER FORCES", headers, widths, rows, newGroup, new[] { 0, envelope ? 7 : 6 });
        }

        private static string[] StationForceCells(Fsec row) => row == null ? new[] { "", "", "", "", "" }
            : new[] { row.stationLabel ?? row.n, printManager.toString(row.l, 3) }.Concat(ForceCells(row, false)).ToArray();

        private static string[] ForceCells(Fsec row, bool source)
        {
            var values = row == null ? new[] { "", "", "" } : new[] {
                printManager.toString(row.mz, 2), printManager.toString(row.fy, 2), printManager.toString(row.fx, 2) };
            return source ? values.Concat(new[] { row == null ? "" : CompactProvenance(row.caseStr, row.comb) }).ToArray() : values;
        }

        internal static void SingleForces(PdfDocument document, PrintData data, ref int page,
            string caption, IReadOnlyList<Fsec> forces, bool envelope, bool newGroup)
        {
            double[] widths = envelope ? new double[] { 50, 45, 50, 50, 50, 130 } : new double[] { 50, 50, 75, 75, 75 };
            var font = ReportFont(document, data);
            double sourceWidth = envelope && data.referenceResultLayout && Portrait(document) ? 143
                : widths.Last() * document.currentPageSize.Width / widths.Sum() - 4;
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
                    printManager.toString(force.l, 3) }.Concat(ForceCells(force, envelope)).ToArray();
                var source = envelope ? Wrap(document, font, cells.Last(), sourceWidth) : new List<string> { "" };
                for (int line = 0; line < source.Count; line++)
                {
                    var output = line == 0 ? cells : new string[widths.Length];
                    if (envelope) output[widths.Length - 1] = source[line];
                    rows.Add(new Row(output, memberCaption, height: first && line == 0 ? 13.5 : 9));
                }
            }
            var heading = new string[widths.Length];
            string reportTitle = "MEMBER FORCES";
            if (envelope && data.referenceResultLayout && Portrait(document))
            {
                var match = Regex.Match(caption, @"^(.*)\s+((?:MOMENT|SHARE|AXIAL) (?:MAX|MIN)\.)$");
                if (match.Success)
                {
                    reportTitle = match.Groups[1].Value;
                    heading[2] = "     " + match.Groups[2].Value;
                }
                else heading[0] = caption;
            }
            else heading[0] = caption;
            var columns = envelope ? new[] { "", "DIST.", "M", "S", "N", "COM: CASE" } : new[] { "", "DIST.", "M", "S", "N" };
            var units = envelope ? new[] { "", "(m)", "(kN m)", "(kN)", "(kN)", "" } : new[] { "", "(m)", "(kN m)", "(kN)", "(kN)" };
            Print(document, data, ref page, reportTitle, new[] { heading, columns, units }, widths, rows,
                newGroup, new[] { 0 }, keepMemberTogether: !envelope, squareViewport: !envelope);
        }
    }
}

using Newtonsoft.Json.Linq;
using PDF_Manager.Printing.Comon;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    /// <summary>Bridge reports use the existing pagination, fonts, margins and page budget.</summary>
    internal sealed class BridgeLoadTables : IPrintable
    {
        public const string KEY = "bridge_reports";
        private const int MaxCellCharacters = 4096;
        private const int MaxHeaderCharacters = 256;
        private const int MaxDocumentCharacters = 2_000_000;
        private const int MaxWrappedLines = 200_000;
        private readonly List<BridgeTable> tables = new List<BridgeTable>();
        internal bool HasData => tables.Count > 0;

        public BridgeLoadTables(Dictionary<string, object> data)
        {
            if (!data.TryGetValue(KEY, out var source)) return;
            if (!data.TryGetValue("dimension", out var dimension) || Convert.ToInt32(dimension) != 3)
                throw new InvalidOperationException("Bridge load reports require a 3D document.");
            var reports = JArray.FromObject(source);
            if (reports.Count > 10000) throw new InvalidOperationException("Too many bridge report sections.");
            var budget = new LayoutBudget();
            foreach (JObject report in reports)
                tables.Add(new BridgeTable(report, budget));
        }

        private sealed class LayoutBudget
        {
            private int characters;
            private int lines;
            internal void AddText(string text, int limit)
            {
                int count = text?.Length ?? 0;
                if (count > limit || count > MaxDocumentCharacters - characters)
                    throw new InvalidOperationException("Bridge report text exceeds the layout budget.");
                characters += count;
            }
            internal void AddLine()
            {
                if (++lines > MaxWrappedLines)
                    throw new InvalidOperationException("Bridge table exceeds the layout budget.");
            }
        }

        public void printPDF(PdfDocument mc, PrintData data, ref int indexPage)
        {
            foreach (var table in tables)
            {
                mc.CheckBudget();
                table.printPDF(mc, data, ref indexPage);
            }
        }

        private sealed class BridgeTable : PrintableBaseA
        {
            private readonly string title;
            private readonly string[] headers;
            private readonly double[] widths;
            private readonly string[][] rows;
            private readonly LayoutBudget budget;
            private Table table;

            internal BridgeTable(JObject report, LayoutBudget budget)
            {
                this.budget = budget;
                title = report.Value<string>("title") ?? "";
                headers = report["headers"]?.ToObject<string[]>() ?? Array.Empty<string>();
                widths = report["widths"]?.ToObject<double[]>() ?? Array.Empty<double>();
                rows = report["rows"]?.ToObject<string[][]>() ?? Array.Empty<string[]>();
                if (headers.Length == 0 || headers.Length > 12 || widths.Length != headers.Length ||
                    widths.Any(width => double.IsNaN(width) || double.IsInfinity(width) || width < 25) ||
                    widths.Sum() > 510 || rows.Length > 100000 ||
                    rows.Any(row => row == null || row.Length != headers.Length) ||
                    title.Length > 1000)
                    throw new InvalidOperationException("Invalid bridge report table.");
                budget.AddText(title, 1000);
                foreach (string header in headers) budget.AddText(header, MaxHeaderCharacters);
                foreach (string[] row in rows)
                    foreach (string cell in row) budget.AddText(cell, MaxCellCharacters);
            }

            protected override bool HasAnyData() => rows.Length > 0;

            protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles,
                out int headerRows, out Table.NupInfo[] nupInfo)
            {
                var font = data.language == "cn" ? mc.font_simsun : mc.font_mic;
                mc.xpen ??= new XPen(XColors.Black, 0.3);
                var expanded = new List<string[]>();
                foreach (var row in rows)
                {
                    mc.CheckBudget();
                    var cells = row.Select((cell, column) => Wrap(cell ?? "", widths[column] - 8,
                        text => mc.gfx.MeasureString(text, font).Width, mc.CheckBudget)).ToArray();
                    for (int line = 0; line < cells.Max(cell => cell.Count); line++)
                    {
                        if (expanded.Count >= 200000) throw new InvalidOperationException("Bridge table exceeds the layout budget.");
                        expanded.Add(cells.Select(cell => line < cell.Count ? cell[line] : "").ToArray());
                    }
                }
                var headingLines = headers.Select((header, column) => Wrap(header, widths[column] - 8,
                    text => mc.gfx.MeasureString(text, font).Width, mc.CheckBudget)).ToArray();
                headerRows = headingLines.Max(lines => lines.Count);
                table = new Table(expanded.Count + headerRows, headers.Length);
                for (int column = 0; column < headers.Length; column++)
                {
                    table.ColWidth[column] = widths[column];
                    for (int line = 0; line < headerRows; line++)
                    {
                        table[line, column] = line < headingLines[column].Count ? headingLines[column][line] : "";
                        table.AlignX[line, column] = "L";
                    }
                    table.HolLW[headerRows, column] = 0.3;
                }
                for (int row = 0; row < expanded.Count; row++)
                {
                    mc.CheckBudget();
                    table.RowHeight[row + headerRows] = printManager.LineSpacing2;
                    for (int column = 0; column < headers.Length; column++)
                    {
                        table[row + headerRows, column] = expanded[row][column];
                        table.AlignX[row + headerRows, column] = "L";
                    }
                }
                titles = Wrap(title, widths.Sum(), text => mc.gfx.MeasureString(text, font).Width, mc.CheckBudget).ToArray();
                nupInfo = Table.OneUpInfo;
            }

            private List<string> Wrap(string text, double width, Func<string, double> measure, Action checkBudget)
            {
                var lines = new List<string>();
                string line = "";
                foreach (char character in text ?? "")
                {
                    checkBudget();
                    if (character == '\r') continue;
                    if (character == '\n') { budget.AddLine(); lines.Add(line); line = ""; continue; }
                    if (line.Length > 0 && measure(line + character) > width)
                    { budget.AddLine(); lines.Add(line); line = ""; }
                    line += character;
                }
                budget.AddLine();
                lines.Add(line);
                return lines;
            }

            protected override Table GetTable() => table;
        }
    }
}

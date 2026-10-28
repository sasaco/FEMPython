using System;
using System.Collections.Generic;
using System.Linq;
using PDF_Manager.Printing.Comon;
using PdfSharpCore.Drawing;

namespace PDF_Manager.Printing
{
    // FrameG's COMBINE and PICK UP member forces (原宿ボックス 0404, 5-23..5-128): one page for each
    // extremum of each force, the selected cases without their coefficients in the last column.
    // Positions are FrameG's in points; values are right-aligned on their column edges.
    internal static class FrameGEnvelopeLayout
    {
        // FrameG prints the PICK UP table 10.2 pt left of the COMBINE table; its heading and member
        // captions move differently, so each report keeps its own positions.
        private sealed class Columns
        {
            internal string Prefix, CaseHeading;
            internal double Heading, Focus, Caption, Label;
            internal double DistHeading, MHeading, SHeading, NHeading, Case;
            internal double Dist, M, S, N;
        }

        private static readonly Columns Combine = new Columns
        {
            Prefix = "COMBINE ", CaseHeading = "CASE",
            Heading = 88.6, Focus = 276.6, Caption = 105.6, Label = 159.8,
            DistHeading = 181.4, MHeading = 250.2, SHeading = 304.3, NHeading = 358.6, Case = 393.6,
            Dist = 202.9, M = 267.5, S = 321.6, N = 376.0,
        };

        private static readonly Columns Pickup = new Columns
        {
            Prefix = "PICK UP ", CaseHeading = "COM:    CASE",
            Heading = 91.9, Focus = 266.4, Caption = 91.9, Label = 149.6,
            DistHeading = 171.2, MHeading = 240.0, SHeading = 294.1, NHeading = 348.5, Case = 383.5,
            Dist = 192.7, M = 257.4, S = 311.4, N = 365.8,
        };

        // FrameG's line pitch; the heading lines, member captions and a member's first row are 1.5 lines apart.
        private const double RowPitch = 8.975, Gap = 1.5 * RowPitch;
        // The heading's hairline starts before the text and ends past "COMBINE n" / "PICK UP n".
        private const double UnderlineLead = 7.1, UnderlineTail = 5.7, UnderlineDrop = 1.1;

        // "( 1+2+3+4+5+8-10 )": the cases a COMBINE row took, signed, without the unused (0) choices.
        // "( 2 :1+2+3+4+8-10 )": a PICK UP row's COMBINE and the cases of that COMBINE's expression.
        internal static string CaseColumn(Fsec row, bool pickup)
        {
            if (!pickup)
                return string.IsNullOrEmpty(row.caseStr) ? "" : "( " + Signed(row.caseStr.Split(',')) + " )";
            // Rows without the combination expression (e.g. JSON from the Web version) keep the source as given.
            if (string.IsNullOrEmpty(row.comb)) return row.caseStr ?? "";
            return "( " + row.caseStr + " :" + Signed(Terms(row.comb)) + " )";
        }

        // The combination expression is "1.1*1 + 1.2*2 - 10 + 1.1*0": signed terms, each an optional coefficient and a case.
        private static IEnumerable<string> Terms(string expression) =>
            expression.Replace(" - ", " + -").Split(new[] { " + " }, StringSplitOptions.None).Select(term =>
            {
                term = term.Trim();
                string sign = term.StartsWith("-", StringComparison.Ordinal) ? "-" : "";
                int product = term.LastIndexOf('*');
                return sign + (product < 0 ? term.Substring(sign.Length) : term.Substring(product + 1));
            });

        private static string Signed(IEnumerable<string> cases)
        {
            var selected = cases.Select(item => item.Trim()).Where(item => item.Length > 0 && item != "0" && item != "-0").ToList();
            return string.Concat(selected.Select((item, index) =>
                index == 0 || item.StartsWith("-", StringComparison.Ordinal) ? item : "+" + item));
        }

        internal static void Print(PdfDocument document, PrintData data, ref int page, bool pickup,
            string id, string name, string focus, IReadOnlyList<Fsec> rows)
        {
            if (rows.Count == 0) return;
            var columns = pickup ? Pickup : Combine;
            var font = ReferenceResultLayout.ReportFont(document, data);
            string label = columns.Prefix + id;
            string heading = label + (string.IsNullOrEmpty(name) ? "" : "  " + name);
            double bottom = document.currentPage.Height - document.Margine.Bottom;
            double caseWidth = document.currentPage.Width - document.Margine.Right - columns.Case;
            int pageNo = page; // A local function cannot capture the ref parameter.
            double y = 0;
            void Put(string text, double x, double baseline, bool right = false)
            {
                document.currentPos = new XPoint(x, baseline);
                Text.PrtText(document, text, font, right ? XStringFormats.BottomRight : null);
            }
            void Header()
            {
                if (!document.currentPos.Equals(document.initialPos)) document.NewPage(ref pageNo);
                double top = document.initialPos.Y;
                Put(heading, columns.Heading, top);
                document.gfx.DrawLine(new XPen(XColors.Black, 0.12), columns.Heading - UnderlineLead, top + UnderlineDrop,
                    columns.Heading + document.gfx.MeasureString(label, font).Width + UnderlineTail, top + UnderlineDrop);
                Put(focus, columns.Focus, top + Gap);
                y = top + 2 * Gap;
                Put("DIST.", columns.DistHeading, y);
                Put("M", columns.MHeading, y);
                Put("S", columns.SHeading, y);
                Put("N", columns.NHeading, y);
                Put(columns.CaseHeading, columns.Case, y);
            }
            Header();
            double emptyPage = bottom - y; // Below a fresh page's column headings.
            if (2 * Gap > emptyPage)
                throw new InvalidOperationException("A COMBINE or PICK UP heading leaves no room for a row on the selected paper.");
            // A member's rows are consecutive; a member that appears again later is captioned again.
            var members = new List<List<Fsec>>();
            foreach (Fsec row in rows)
            {
                if (members.Count == 0 || members[members.Count - 1][0].m != row.m) members.Add(new List<Fsec>());
                members[members.Count - 1].Add(row);
            }
            foreach (var member in members)
            {
                document.CheckBudget();
                var lines = member.Select(row => (Row: row,
                    Cases: ReferenceResultLayout.Wrap(document, font, CaseColumn(row, pickup), caseWidth))).ToList();
                // Without the member caption or station label (JSON from the Web version), the report's defaults.
                string caption = member[0].memberCaption;
                if (string.IsNullOrEmpty(caption)) caption = "MEMBER " + member[0].m;
                void Caption()
                {
                    y += Gap;
                    Put(caption, columns.Caption, y);
                }
                // FrameG keeps a member whole: one that does not fit in the rest of the page starts a new one
                // (and a member longer than a page continues from there with its caption repeated).
                double need = Gap + Gap + (lines.Sum(line => line.Cases.Count) - 1) * RowPitch;
                if (need > bottom - y && y > document.initialPos.Y + 2 * Gap) Header();
                Caption();
                bool first = true;
                foreach (var (row, cases) in lines)
                {
                    double next = y + (first ? Gap : RowPitch);
                    if (next > bottom)
                    {
                        Header();
                        Caption();
                        next = y + Gap;
                    }
                    y = next;
                    first = false;
                    Put(string.IsNullOrEmpty(row.stationLabel) ? row.n : row.stationLabel, columns.Label, y, true);
                    Put(printManager.toString(row.l, 3), columns.Dist, y, true);
                    Put(ReferenceResultLayout.Number(row.mz, 2), columns.M, y, true);
                    Put(ReferenceResultLayout.Number(row.fy, 2), columns.S, y, true);
                    Put(ReferenceResultLayout.Number(row.fx, 2), columns.N, y, true);
                    for (int line = 0; line < cases.Count; line++)
                    {
                        if (line > 0)
                        {
                            if (y + RowPitch > bottom) { Header(); Caption(); y += Gap - RowPitch; }
                            y += RowPitch;
                        }
                        Put(cases[line], columns.Case, y);
                    }
                }
            }
            document.currentPos = new XPoint(document.initialPos.X, y);
            page = pageNo;
        }
    }
}

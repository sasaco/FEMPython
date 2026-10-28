using Newtonsoft.Json.Linq;
using PDF_Manager;
using PDF_Manager.Printing;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using PdfSharpCore.Pdf.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace FrameWebforCS.Tests;

// FrameG's input copy (原宿ボックス 0404, sample pages 5-3 to 5-6) for 2D portrait reports.
public sealed class FrameGInputLayoutTests
{
    private const double PageHeight = 842;
    private const double Line = 8.975;
    private const double Half = PdfDocument.ReferencePortraitFontSize / 2;
    private const double Tolerance = 72 / 25.4; // 1 mm

    [Fact]
    public void SectionsFollowFrameGsOrderWithoutTheSeparateMaterialTable()
    {
        var pdf = Pdf(Model());

        string[] headings = { "格 点 DATA", "部 材 DATA", "部 材 DATA（剛域データ）", "部材バネ DATA", "着目点 DATA",
            "基本荷重 DATA", "実荷重 DATA", "DEFINE DATA", "COMBINE DATA", "PickUp DATA" };
        var order = headings.Select(heading => pdf.Runs.FindIndex(run => run.Text == heading)).ToArray();
        Assert.All(order, index => Assert.True(index >= 0));
        Assert.Equal(order.OrderBy(index => index), order);
        Assert.DoesNotContain(pdf.Runs, run => run.Text.Contains("材料データ"));
        Assert.All(pdf.Runs.Where(run => headings.Contains(run.Text)), run => Assert.Equal(78.4, run.X, 1));
        Assert.All(pdf.Sizes, size => Assert.Equal(PdfDocument.ReferencePortraitFontSize, size, 3));
    }

    [Fact]
    public void NodesAreOneColumnWithYUpAsFrameGShowsIt()
    {
        var pdf = Pdf(Model());

        var heading = pdf.Single("格 点 DATA");
        Assert.Equal(87.5, Baseline(heading), 1);
        // FrameWeb's 2D y points down; the node at y = -3.225 is 3.225 above the bottom slab.
        var y = pdf.Runs.Where(run => run.Text == "3.225").OrderBy(Baseline).First();
        AssertRight(y, 221.0);
        Assert.Equal(87.5 + 44.9, Baseline(y), 1);
        Assert.Equal(87.5 + 44.9 + 3 * Line, Baseline(pdf.Single("0.000", 221.0)), 1);
    }

    [Fact]
    public void MembersCarryTheirMaterialAndLeaveHalfALineBetweenMemberGroups()
    {
        var pdf = Pdf(Model());

        var rows = pdf.Runs.Where(run => run.Text == "2.65E+7").OrderBy(Baseline).ToList();
        Assert.Equal(3, rows.Count);
        AssertRight(rows[0], 390.4);
        Assert.Equal(Line * 1.5, Baseline(rows[1]) - Baseline(rows[0]), 2); // 剛域 -> 上床版
        Assert.Equal(Line, Baseline(rows[2]) - Baseline(rows[1]), 2);       // 上床版 -> 上床版
        Assert.Contains(pdf.Runs, run => run.Text == "1.00E-5");
        // A too-wide area or inertia (a rigid zone's 1000) is an integer one character in from the field.
        Assert.Equal(481.9 - 5 * Half, pdf.Runs.First(run => run.Text == "1000").X, 1);
        AssertRight(pdf.Runs.First(run => run.Text == "0.3500"), 481.9);
        AssertRight(pdf.Runs.First(run => run.Text == "0.003570"), 536.4);
        // The mark (ﾏｰｸ) is not in the saved input: only its column heading is printed.
        Assert.Contains(pdf.Runs, run => run.Text == "ﾏｰｸ" && Math.Abs(run.X - 241.2) < 0.5);
        Assert.DoesNotContain(pdf.Runs, run => Math.Abs(run.X - 243.5) < 1);
    }

    [Fact]
    public void NoticePointsIncludeTheRigidZoneEnd()
    {
        var pdf = Pdf(Model());

        var rigidEnd = pdf.Single("3.125"); // member 3 (L = 3.225) has a 0.1 rigid zone at its J end
        var name = pdf.Runs.Single(run => run.Text == "剛域" && Math.Abs(Baseline(run) - Baseline(rigidEnd) - Line) < 0.1);
        Assert.InRange(name.X + name.Width / 2, rigidEnd.X + rigidEnd.Width / 2 - Tolerance, rigidEnd.X + rigidEnd.Width / 2 + Tolerance);
    }

    [Fact]
    public void MemberLoadsShowFrameGsMarkColumnsAndSerialNumbers()
    {
        var pdf = Pdf(Model());

        var label = pdf.Runs.First(run => run.Text == "Ｃase 1  固定死荷重");
        Assert.Equal(91.9, label.X, 1);
        var rows = pdf.Runs.Where(run => run.Text.StartsWith("(...", StringComparison.Ordinal)).OrderBy(Baseline).ToList();
        Assert.Equal(new[] { "(...1)", "(...2)", "(...3)", "(...4)", "(...1)" }, rows.Select(run => run.Text));
        Assert.Equal(Baseline(label) + 13.5, Baseline(rows[0]), 1);
        string Cell(int row, double right) => pdf.Runs.Single(run => Math.Abs(Baseline(run) - Baseline(rows[row])) < 0.1 &&
            Math.Abs(run.X + run.Width - right) < 0.5).Text;
        Assert.Equal("6", Cell(1, 227.5));      // axial (x) load: FrameG's mark 5/6
        Assert.Equal("M", pdf.Runs.Single(run => Math.Abs(Baseline(run) - Baseline(rows[0])) < 0.1 && Math.Abs(run.X - 443.3) < 0.5).Text);
        Assert.Equal("-2.00", Cell(2, 369.95));  // H: the converter's sign turned back
        Assert.Contains(pdf.Runs, run => run.Text == "V" && Math.Abs(Baseline(run) - Baseline(rows[3])) < 0.1);
        Assert.DoesNotContain(pdf.Runs, run => Math.Abs(Baseline(run) - Baseline(rows[3])) < 0.1 &&
            Math.Abs(run.X + run.Width - 420.8) < 0.5); // P2 = 0 is blank
        Assert.Equal("1.10", Cell(4, 329.2));    // nodal F.Y
        Assert.Equal(2, pdf.Lines.Count);         // a rule under each load header
    }

    [Fact]
    public void CombineFactorsAreOnTheLineBelowTheirDefineNumbers()
    {
        var pdf = Pdf(Model());

        var factor = pdf.Single("1.200");
        var define = pdf.Runs.Single(run => run.Text == "2" && Math.Abs(run.X + run.Width - 355.3) < 0.5);
        Assert.Equal(Line, Baseline(factor) - Baseline(define), 2);
        AssertRight(factor, 322.6 + 32.87);
        AssertRight(pdf.Single("1.0000", 132.9), 132.9);
        Assert.Contains(pdf.Runs, run => run.Text == "C 8");
        Assert.Contains(pdf.Runs, run => run.Text == "C10");
    }

    [Fact]
    public void ALongBasicLoadListContinuesWithItsHeadingOnTheNextPage()
    {
        var root = Model();
        var load = (JObject)root["load"]!;
        for (int id = 3; id <= 120; id++)
            load[id.ToString(CultureInfo.InvariantCulture)] = JObject.Parse(
                """{"fix_node":1,"fix_member":1,"element":1,"joint":1,"symbol":"X","rate":1.0,"name":"追加","load_member":[],"load_node":[]}""");

        var pdf = Pdf(root);

        var headings = pdf.Runs.Where(run => run.Text == "基本荷重 DATA").ToList();
        Assert.True(headings.Count >= 2);
        Assert.All(headings.Skip(1), heading => Assert.Equal(87.5, Baseline(heading), 1));
        Assert.All(pdf.Runs, run => Assert.True(Baseline(run) <= PageHeight - 60 + 0.01, run.Text));
    }

    [Fact]
    public void EnglishKeepsThePositionsWithTranslatedLabels()
    {
        var root = Model();
        root["language"] = "en";

        var pdf = Pdf(root);

        Assert.Equal(87.5, Baseline(pdf.Single("NODE DATA")), 1);
        Assert.Equal(78.4, pdf.Single("NODE DATA").X, 1);
        Assert.Contains(pdf.Runs, run => run.Text == "Case 1  固定死荷重");
        Assert.DoesNotContain(pdf.Runs, run => run.Text == "格 点 DATA");
    }

    [Fact]
    public void ChineseKeepsThePositionsWithinThePageAndLeavesNoJapaneseLabel()
    {
        var root = Model();
        root["language"] = "cn";
        var japanese = Pdf(Model()).Runs;

        var pdf = Pdf(root);

        Assert.Equal(87.5, Baseline(pdf.Single("节 点 DATA")), 1);
        Assert.Equal(78.4, pdf.Single("节 点 DATA").X, 1);
        Assert.Contains(pdf.Runs, run => run.Text == "Case 1  固定死荷重");
        // Every label of the Japanese report is translated; only the model's own names stay as entered
        // (and 支点, which is the same word in Chinese).
        string[] data = { "剛域", "上床版", "固定死荷重", "付加死荷重", "安全性", "破壊", "支点" };
        var labels = japanese.Select(run => run.Text)
            .Where(text => Regex.IsMatch(text, @"[぀-ヿ一-鿿｡-ﾟＡ-Ｚ]"))
            .Where(text => !data.Any(text.Contains)).Distinct().ToList();
        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.DoesNotContain(pdf.Runs, run => run.Text == label));
        Assert.DoesNotContain(pdf.Runs, run => Regex.IsMatch(run.Text, @"[぀-ヿ｡-ﾟ]"));
        // FrameG's coordinates hold: nothing passes the sample's right extent (PickUp C10 ends at 565.2 pt).
        Assert.All(pdf.Runs, run => Assert.True(run.X + run.Width <= 566, run.Text));
        Assert.All(pdf.Sizes, size => Assert.Equal(PdfDocument.ReferencePortraitFontSize, size, 3));
    }

    [Fact]
    public void ThreeDimensionalInputKeepsTheGenericTables()
    {
        var root = Model();
        root["dimension"] = 3;

        var pdf = Pdf(root);

        Assert.DoesNotContain(pdf.Runs, run => run.Text == "格 点 DATA");
        Assert.Contains(pdf.Runs, run => run.Text == "格点データ");
    }

    private static void AssertRight((string Text, double X, double Y, double Width, int Page) run, double right) =>
        Assert.InRange(run.X + run.Width, right - Tolerance, right + Tolerance);

    private static double Baseline((string Text, double X, double Y, double Width, int Page) run) => PageHeight - run.Y;

    private static JObject Model() => JObject.Parse("""
        {
          "dimension": 2, "language": "ja", "pageSize": "A4", "pageOrientation": "Vertical",
          "hasPrintInputData": true, "hasPrintCalculation": false, "title": "FrameG",
          "node": { "1": { "x": 0, "y": -3.225 }, "2": { "x": 0.35, "y": -3.225 }, "3": { "x": 4.4, "y": -3.225 },
                    "4": { "x": 4.4, "y": 0 } },
          "member": { "1": { "ni": "1", "nj": "2", "e": "1" }, "2": { "ni": "2", "nj": "3", "e": "2" },
                      "3": { "ni": "3", "nj": "4", "e": "3" } },
          "element": { "1": {
            "1": { "E": 26500000, "Xp": 1e-05, "A": 1000, "Iz": 1000, "n": "剛域" },
            "2": { "E": 26500000, "Xp": 1e-05, "A": 0.35, "Iz": 0.00357, "n": "上床版" },
            "3": { "E": 26500000, "Xp": 1e-05, "A": 0.35, "Iz": 0.00357, "n": "上床版" } } },
          "rigid": [ { "m": "3", "Ilength": 0.0, "Jlength": 0.1, "e": 1 } ],
          "notice_points": [ { "row": 1, "m": "3", "Points": [ 0.5 ] } ],
          "fix_member": { "1": [ { "row": 1, "m": "3", "tx": 1646, "ty": 4937 } ] },
          "fix_node": {}, "joint": {},
          "load": {
            "1": { "fix_node": 1, "fix_member": 1, "element": 1, "joint": 1, "symbol": "D1", "rate": 1.0, "name": "固定死荷重",
                   "load_member": [
                     { "row": 1, "m1": "1", "m2": "-3", "direction": "y", "mark": "2", "L1": "0.000", "L2": 0, "P1": 8.6, "P2": 8.6, "ms": "M" },
                     { "row": 2, "m1": "1", "m2": "", "direction": "x", "mark": "2", "L1": "0.000", "L2": 0, "P1": 11, "P2": 11 },
                     { "row": 3, "m1": "2", "m2": "", "direction": "gx", "mark": "2", "L1": "0.000", "L2": 0, "P1": 2, "P2": 2 },
                     { "row": 4, "m1": "3", "m2": "", "direction": "gy", "mark": "1", "L1": "0.5", "L2": 0, "P1": 3, "P2": 0 } ],
                   "load_node": [ { "row": 1, "n": "1", "tx": 0, "ty": 1.1, "rz": 0 } ] },
            "2": { "fix_node": 1, "fix_member": 1, "element": 1, "joint": 1, "symbol": "D2", "rate": 1.0, "name": "付加死荷重",
                   "load_member": [], "load_node": [] } },
          "define": { "1": { "row": 1, "C1": 1 }, "2": { "row": 2, "C1": 0, "C2": 2 } },
          "combine": { "1": { "row": 1, "name": "安全性", "C1": 1.1, "C2": 1.2 } },
          "pickup": { "1": { "row": 1, "name": "破壊", "C1": 1 } }
        }
        """);

    private sealed record Document(List<(string Text, double X, double Y, double Width, int Page)> Runs,
        List<double> Sizes, List<double> Lines)
    {
        internal (string Text, double X, double Y, double Width, int Page) Single(string text) => Runs.Single(run => run.Text == text);

        internal (string Text, double X, double Y, double Width, int Page) Single(string text, double right) =>
            Runs.Single(run => run.Text == text && Math.Abs(run.X + run.Width - right) < 0.5);
    }

    // Text runs (with their widths at FrameG's half-width advance) and the horizontal rules of the generated PDF.
    private static Document Pdf(JObject root)
    {
        byte[] bytes = DirectPdfGenerator.Generate(root.ToString());
        using var document = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        var runs = new List<(string Text, double X, double Y, double Width, int Page)>();
        var sizes = new List<double>();
        var lines = new List<double>();
        int pageNumber = 0;
        foreach (var page in document.Pages)
        {
            pageNumber++;
            var fonts = page.Resources.Elements.GetDictionary("/Font");
            var maps = new Dictionary<string, Dictionary<int, string>>();
            foreach (string font in fonts?.Elements.Keys ?? Enumerable.Empty<string>())
            {
                var cmap = fonts!.Elements.GetDictionary(font).Elements.GetDictionary("/ToUnicode");
                if (cmap?.Stream is null) continue;
                var map = new Dictionary<int, string>();
                string source = Encoding.ASCII.GetString(cmap.Stream.UnfilteredValue);
                foreach (Match block in Regex.Matches(source, @"beginbfchar(?<map>.*?)endbfchar", RegexOptions.Singleline))
                    foreach (Match entry in Regex.Matches(block.Groups["map"].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
                        map[Convert.ToInt32(entry.Groups[1].Value, 16)] = Encoding.BigEndianUnicode.GetString(Convert.FromHexString(entry.Groups[2].Value));
                foreach (Match block in Regex.Matches(source, @"beginbfrange(?<map>.*?)endbfrange", RegexOptions.Singleline))
                    foreach (Match entry in Regex.Matches(block.Groups["map"].Value, @"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>"))
                    {
                        int start = Convert.ToInt32(entry.Groups[1].Value, 16), end = Convert.ToInt32(entry.Groups[2].Value, 16);
                        int unicode = Convert.ToInt32(entry.Groups[3].Value, 16);
                        for (int cid = start; cid <= end; cid++) map[cid] = char.ConvertFromUtf32(unicode + cid - start);
                    }
                maps[font] = map;
            }
            string selectedFont = "";
            double x = 0, y = 0;
            var operands = new List<double>();
            foreach (var operation in ContentReader.ReadContent(page).OfType<COperator>())
            {
                double Number(int i) => double.Parse(operation.Operands[i].ToString()!, CultureInfo.InvariantCulture);
                switch (operation.OpCode.Name)
                {
                    case "BT": x = 0; y = 0; break;
                    case "Td": case "TD": x += Number(0); y += Number(1); break;
                    case "Tm": x = Number(4); y = Number(5); break;
                    case "Tf": selectedFont = ((CName)operation.Operands[0]).Name; sizes.Add(Number(1)); break;
                    case "m": operands = new List<double> { Number(1) }; break;
                    case "l": if (operands.Count == 1 && Math.Abs(Number(1) - operands[0]) < 0.01) lines.Add(PageHeight - operands[0]); break;
                }
                if (operation.OpCode.Name != "Tj" || operation.Operands[0] is not CString value) continue;
                if (!maps.TryGetValue(selectedFont, out var map)) continue;
                var decoded = new StringBuilder();
                for (int i = 0; i + 1 < value.Value.Length; i += 2)
                    if (map.TryGetValue((value.Value[i] << 8) | value.Value[i + 1], out string? character)) decoded.Append(character);
                string text = decoded.ToString();
                double width = text.Sum(c => c < 0x80 || c >= 0xFF61 && c <= 0xFF9F ? Half : 2 * Half);
                runs.Add((text, x, y, width, pageNumber));
            }
        }
        return new Document(runs, sizes, lines);
    }
}

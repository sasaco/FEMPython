using Newtonsoft.Json.Linq;
using PDF_Manager.Printing;
using Xunit;

namespace FrameWebforCS.Tests;

// FrameG's COMBINE and PICK UP member forces (原宿ボックス 0404, 見本_電算出力.pdf 21-128).
public sealed class FrameGEnvelopeLayoutTests
{
    private const double Tolerance = 72 / 25.4; // 1 mm
    private const double PageHeight = 842;

    [Theory]
    [InlineData("1,2,3,4,5,8,-10,0", "1.1*1 + 1.2*2 + 1.1*3 + 1.2*4 + 1.2*5 + 1.2*8 - 10 + 1.1*0", false, "( 1+2+3+4+5+8-10 )")]
    [InlineData("6,0,11", "6 + 0 + 11", false, "( 6+11 )")]
    [InlineData("-3,4", "-3 + 4", false, "( -3+4 )")]
    [InlineData("2", "1 + 2 + 3 + 1.2*4 + 1.2*8 - 10", true, "( 2 :1+2+3+4+8-10 )")]
    [InlineData("8", "1 + 2 + 3 + 4 - 0.8*10", true, "( 8 :1+2+3+4-10 )")]
    [InlineData("10", "6 + 8 + 0", true, "( 10 :6+8 )")]
    [InlineData("1", "1.1*1 + 1.2*2 + 1.2*0 + 10 + 1.1*11", true, "( 1 :1+2+10+11 )")]
    public void CaseColumnPrintsTheSelectedCasesWithoutCoefficients(string source, string expression, bool pickup, string expected) =>
        Assert.Equal(expected, FrameGEnvelopeLayout.CaseColumn(new Fsec { caseStr = source, comb = expression }, pickup));

    [Fact]
    public void CaseColumnKeepsTheDefaultsWhenTheMetadataIsMissing()
    {
        Assert.Equal("", FrameGEnvelopeLayout.CaseColumn(new Fsec(), false));
        Assert.Equal("", FrameGEnvelopeLayout.CaseColumn(new Fsec(), true));
        Assert.Equal("3", FrameGEnvelopeLayout.CaseColumn(new Fsec { caseStr = "3" }, true));
    }

    [Fact]
    public void CombinePrintsOnePageForEachExtremumAtFrameGPositions()
    {
        var root = Portrait();
        root["fsecCombine"] = new JObject { ["1"] = Modes("1,2,3,4,5,8,-10,0", "1.1*1 + 1.2*2 + 1.1*3 + 1.2*4 + 1.2*5 + 1.2*8 - 10 + 1.1*0") };
        root["fsecCombineName"] = JArray.Parse("""[["1","Envelope"]]""");
        var pdf = TransverseReportCompositionTests.Pdf(root, "framegenvelope-combine");

        Assert.Equal(6, pdf.Pages);
        Assert.DoesNotContain("SHARE", pdf.Text);
        Assert.DoesNotContain("1.1*1", pdf.Text);
        foreach (var (page, focus) in new[] { (1, "MOMENT MAX."), (2, "MOMENT MIN."), (3, "SHEAR  MAX."),
            (4, "SHEAR  MIN."), (5, "AXIAL  MAX."), (6, "AXIAL  MIN.") })
        {
            var runs = pdf.Runs.Where(run => run.Page == page).ToArray();
            AssertAt(runs, "COMBINE 1  Envelope", 88.6, 87.5);
            AssertAt(runs, focus, 276.6, 100.9);
            AssertAt(runs, "DIST.", 181.4, 114.5);
            AssertAt(runs, "CASE", 393.6, 114.5);
            AssertAt(runs, "部 材  1 ( 1 - 2 ) 剛域", 105.6, 127.8);
            AssertAt(runs, "I-TAN", 159.8 - Width("I-TAN"), 141.4);
            AssertAt(runs, "0.000", 202.9 - Width("0.000"), 141.4);
            AssertAt(runs, "-187.32", 267.5 - Width("-187.32"), 141.4);
            AssertAt(runs, "( 1+2+3+4+5+8-10 )", 393.6, 141.4);
            AssertAt(runs, "1", 159.8 - Width("1"), 150.4);
            AssertAt(runs, "部 材  2 ( 2 - 3 ) 上床版", 105.6, 172.7);
        }
    }

    [Fact]
    public void PickupUsesItsOwnColumnsAndTheCombinationOfEachRow()
    {
        var root = Portrait();
        root["fsecPickup"] = new JObject { ["2"] = Modes("2", "1 + 2 + 3 + 1.2*4 + 1.2*8 - 10") };
        root["fsecPickupName"] = JArray.Parse("""[["2","Pickup"]]""");
        var pdf = TransverseReportCompositionTests.Pdf(root, "framegenvelope-pickup");

        Assert.Equal(6, pdf.Pages);
        var runs = pdf.Runs.Where(run => run.Page == 4).ToArray();
        AssertAt(runs, "PICK UP 2  Pickup", 91.9, 87.5);
        AssertAt(runs, "SHEAR  MIN.", 266.4, 100.9);
        AssertAt(runs, "DIST.", 171.2, 114.5);
        AssertAt(runs, "COM:    CASE", 383.5, 114.5);
        AssertAt(runs, "部 材  1 ( 1 - 2 ) 剛域", 91.9, 127.8);
        AssertAt(runs, "I-TAN", 149.6 - Width("I-TAN"), 141.4);
        AssertAt(runs, "-100.22", 365.8 - Width("-100.22"), 141.4);
        AssertAt(runs, "( 2 :1+2+3+4+8-10 )", 383.5, 141.4);
    }

    [Fact]
    public void RowsWithoutMetadataUseTheDefaultsAndALongMemberContinuesWithItsCaption()
    {
        var root = Portrait();
        // JSON without FrameWebforCS's metadata (no captions, station labels or provenance), one member longer than a page.
        var rows = new JArray(Enumerable.Range(0, 120).Select(i => new JObject
        {
            ["m"] = i < 100 ? "1" : "2", ["n"] = "S" + i, ["l"] = i, ["fx"] = 1, ["fy"] = 2, ["mz"] = 3,
        }));
        root["fsecCombine"] = new JObject { ["1"] = new JObject { ["mz_max"] = rows } };
        // The report lists a COMBINE section only with its names; an empty name prints "COMBINE 1" alone.
        root["fsecCombineName"] = JArray.Parse("""[["1",""]]""");
        var pdf = TransverseReportCompositionTests.Pdf(root, "framegenvelope-no-metadata");

        Assert.Equal(2, pdf.Pages);
        Assert.Equal(new[] { 1, 2 }, pdf.Runs.Where(run => run.Text == "MEMBER 1").Select(run => run.Page));
        Assert.Equal(new[] { 2 }, pdf.Runs.Where(run => run.Text == "MEMBER 2").Select(run => run.Page));
        Assert.Equal(120, pdf.Runs.Count(run => run.Text.StartsWith("S", StringComparison.Ordinal) && run.Text.Length > 1 && char.IsDigit(run.Text[1])));
        Assert.All(pdf.Runs.GroupBy(run => run.Page), page =>
            Assert.Contains(page, run => run.Text == "COMBINE 1"));
        // Every row stays inside FrameG's printable area.
        Assert.All(pdf.Runs, run => Assert.True(PageHeight - run.Y <= PageHeight - PdfDocument.FrameGBottom));
    }

    private static Dictionary<string, object> Portrait()
    {
        var root = TransverseReportCompositionTests.Root();
        root["pageOrientation"] = "Vertical";
        return root;
    }

    // The first two members of the sample's COMBINE 1 MOMENT MAX. page, with the same rows in every mode.
    private static JObject Modes(string source, string expression)
    {
        var stations = new[] { ("1", "I-TAN", 0.0), ("1", "1", 0.225), ("1", "J-TAN", 0.35), ("2", "I-TAN", 0.0), ("2", "1", 0.1) };
        var modes = new JObject();
        foreach (string mode in new[] { "mz_max", "mz_min", "fy_max", "fy_min", "fx_max", "fx_min" })
            modes[mode] = new JArray(stations.Select((station, index) => new JObject
            {
                ["m"] = station.Item1, ["n"] = "S" + index, ["stationLabel"] = station.Item2, ["l"] = station.Item3,
                ["memberCaption"] = station.Item1 == "1" ? "部 材  1 ( 1 - 2 ) 剛域" : "部 材  2 ( 2 - 3 ) 上床版",
                ["fx"] = -100.22, ["fy"] = 291.79, ["mz"] = -187.32, ["case"] = source, ["comb"] = expression,
            }));
        return modes;
    }

    private static double Width(string text)
    {
        using var measurement = new PdfSharpCore.Pdf.PdfDocument();
        using var graphics = PdfSharpCore.Drawing.XGraphics.FromPdfPage(measurement.AddPage());
        return graphics.MeasureString(text, new PdfSharpCore.Drawing.XFont("MS Mincho", PdfDocument.ReferencePortraitFontSize)).Width;
    }

    // x is the text's left edge and baseline its distance from the top, both from the sample.
    private static void AssertAt((string Text, double X, double Y, int Page)[] runs, string text, double x, double baseline)
    {
        var run = runs.First(item => item.Text == text);
        Assert.InRange(run.X, x - Tolerance, x + Tolerance);
        Assert.InRange(PageHeight - run.Y, baseline - Tolerance, baseline + Tolerance);
    }
}

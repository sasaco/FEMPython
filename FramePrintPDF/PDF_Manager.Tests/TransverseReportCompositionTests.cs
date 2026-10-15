using Newtonsoft.Json.Linq;
using PDF_Manager;
using PDF_Manager.Printing;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class TransverseReportCompositionTests
{
    [Theory]
    [InlineData("1.367")]
    [InlineData("-1.182E-06")]
    public void PortraitWrappedProvenanceKeepsNumericCoefficientOnOneLine(string coefficient)
    {
        var root = Root();
        root["pageOrientation"] = "Vertical";
        root["referenceResultLayout"] = true;
        string name = new string('X', 44) + " " + coefficient + "*D1";
        root["disg"] = JObject.Parse("""{"1":[{"id":"1","dx":1,"dy":2,"rz":3}]}""");
        root["disgName"] = new JArray { new JArray("1", name) };
        var pdf = Pdf(root, "numeric-coefficient-" + coefficient);
        Assert.Contains(name, pdf.Text.Replace("\n", ""));
        var coefficientLine = Assert.Single(pdf.Runs.Where(run => run.Text.Contains(coefficient)));
        using var measurement = new PdfSharpCore.Pdf.PdfDocument();
        using var graphics = PdfSharpCore.Drawing.XGraphics.FromPdfPage(measurement.AddPage());
        var font = new PdfSharpCore.Drawing.XFont("MS Mincho", 9.572);
        double rightEdge = coefficientLine.X + graphics.MeasureString(coefficientLine.Text, font).Width;
        Assert.InRange(rightEdge, 40, 310);
    }

    [Fact]
    public void PortraitBasicForcesKeepWholeMemberBlocksAndOneCasePerFlow()
    {
        var root = Root();
        root["pageOrientation"] = "Vertical";
        root["referenceResultLayout"] = true;
        var cases = new JObject();
        var names = new JArray();
        for (int i = 1; i <= 10; i++)
        {
            cases[i.ToString()] = PortraitForceRows();
            names.Add(new JArray(i.ToString(), "Load" + i));
        }
        root["fsec"] = cases;
        root["fsecName"] = names;
        var pdf = Pdf(root, "portrait-basic-forces");
        Assert.Equal(30, pdf.Pages);
        Assert.Equal(950, pdf.Runs.Count(run => run.Text == "123.45"));
        Assert.DoesNotContain(pdf.Runs.Where(run => run.Page == 1), run => run.Text.StartsWith("MEMBER 10 "));
        Assert.Contains(pdf.Runs.Where(run => run.Page == 2), run => run.Text.StartsWith("MEMBER 10 "));
        Assert.DoesNotContain(pdf.Runs.Where(run => run.Page == 2), run => run.Text.StartsWith("MEMBER 11 "));
        Assert.Contains(pdf.Runs.Where(run => run.Page == 3), run => run.Text.StartsWith("MEMBER 11 "));
        foreach (var page in pdf.Runs.GroupBy(run => run.Page))
            Assert.Single(page.Where(run => run.Text.StartsWith("CASE ")));
    }

    private static JArray PortraitForceRows()
    {
        int[] counts = [4, 4, 2, 2, 3, 2, 2, 3, 3, 35, 35];
        return new JArray(counts.SelectMany((count, index) => Enumerable.Range(0, count).Select(station => new JObject
        {
            ["m"] = (index + 1).ToString(), ["n"] = "S" + station,
            ["stationLabel"] = station == 0 ? "I-TAN" : station == count - 1 ? "J-TAN" : station.ToString(),
            ["memberCaption"] = "MEMBER " + (index + 1) + " (I - J) Beam", ["l"] = station,
            ["fx"] = 10, ["fy"] = 20, ["mz"] = 123.45,
        })));
    }

    [Fact]
    public void PortraitPickupUsesSeparateOrderedExtremumFlowsAndPreservesWeightedSources()
    {
        var root = Root();
        root["pageOrientation"] = "Vertical";
        root["referenceResultLayout"] = true;
        var cases = new JObject();
        var names = new JArray();
        foreach (int id in Enumerable.Range(1, 5))
        {
            var modes = new JObject();
            foreach (string mode in new[] { "mz_max", "mz_min", "fy_max", "fy_min", "fx_max", "fx_min" })
            {
                var rows = PortraitForceRows();
                foreach (JObject row in rows)
                {
                    row["case"] = mode.EndsWith("max") ? "6" : "1";
                    row["comb"] = "1.1*1 + 1.367*4 + 5";
                    row["mz"] = mode.EndsWith("max") ? 123.45 : -67.89;
                }
                modes[mode] = rows;
            }
            cases[id.ToString()] = modes;
            names.Add(new JArray(id.ToString(), "Envelope" + id));
        }
        root["fsecPickup"] = cases;
        root["fsecPickupName"] = names;
        var pdf = Pdf(root, "portrait-pickup-forces");
        Assert.Equal(60, pdf.Pages);
        Assert.Equal(1425, pdf.Runs.Count(run => run.Text == "123.45"));
        Assert.Equal(1425, pdf.Runs.Count(run => run.Text == "-67.89"));
        Assert.Contains("(6:1.1*1+1.367*4+5)", pdf.Text);
        Assert.Contains("(1:1.1*1+1.367*4+5)", pdf.Text);
        foreach (var (page, focus) in new[] { (1, "MOMENT MAX."), (3, "MOMENT MIN."),
            (5, "SHARE MAX."), (7, "SHARE MIN."), (9, "AXIAL MAX."), (11, "AXIAL MIN.") })
        {
            Assert.Contains(pdf.Runs.Where(run => run.Page == page), run => run.Text.Contains(focus));
            string opposite = focus.Contains("MAX") ? focus.Replace("MAX", "MIN") : focus.Replace("MIN", "MAX");
            Assert.DoesNotContain(pdf.Runs.Where(run => run.Page == page), run => run.Text.Contains(opposite));
        }
    }

    [Fact]
    public void PortraitDisplacementPrintsFiveCasePairsAndFiveCombinationPairsOnSeparatePages()
    {
        var root = Root();
        root["pageOrientation"] = "Vertical";
        root["referenceResultLayout"] = true;
        var cases = new JObject();
        var combinations = new JObject();
        var names = new JArray();
        var ordinary = new JArray();
        foreach (int id in Enumerable.Range(1, 10))
        {
            cases[id.ToString()] = new JArray(Enumerable.Range(1, 12).Select(node => new JObject
            { ["id"] = node.ToString(), ["dx"] = id, ["dy"] = -id, ["rz"] = node / 10d }));
            var rows = new JObject();
            foreach (int node in Enumerable.Range(1, 12)) rows[node.ToString()] = new JObject
            { ["dx"] = 100 + id, ["dy"] = -100 - id, ["rz"] = node / 10d, ["comb"] = "1.1*1+4" };
            combinations[id.ToString()] = new JObject { ["rz_min"] = rows };
            names.Add(new JArray(id.ToString(), "Load" + id));
            ordinary.Add(id.ToString());
        }
        root["disg"] = cases;
        root["disgName"] = names;
        root["disgCombine"] = combinations;
        root["disgCombineName"] = names.DeepClone();
        root["ordinaryDisplacementCombinations"] = ordinary;
        var pdf = Pdf(root, "portrait-displacement-and-combine");
        Assert.Equal(2, pdf.Pages);
        Assert.Equal(240, pdf.Runs.Count(run => Regex.IsMatch(run.Text, @"^-\d+[.]0000$")));
        foreach (int id in Enumerable.Range(1, 10))
        {
            Assert.Contains(pdf.Runs.Where(run => run.Page == 1), run => run.Text == "CASE " + id + " Load" + id);
            Assert.Contains(pdf.Runs.Where(run => run.Page == 2), run => run.Text.StartsWith("MIX. " + id + " Load" + id + " "));
        }
        Assert.Equal(4, pdf.Runs.Count(run => run.Text == "JOINT NO."));
        Assert.All(pdf.Runs.Where(run => Regex.IsMatch(run.Text, @"^-\d+[.]0000$")), run => Assert.InRange(run.Y, 25, 780));
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(100, 3)]
    public void PortraitBasicKeepsSmallBlocksAndSplitsAnOversizedMemberWithoutLosingRows(int middleRows, int pages)
    {
        var root = Root();
        root["pageOrientation"] = "Vertical";
        root["referenceResultLayout"] = true;
        var rows = new JArray(new[] { 2, middleRows, 2 }.SelectMany((count, member) =>
            Enumerable.Range(0, count).Select(station => new JObject
            {
                ["m"] = (member + 1).ToString(), ["n"] = "S" + station, ["stationLabel"] = "P" + station,
                ["memberCaption"] = "MEMBER " + (member + 1) + " (I - J) Beam", ["l"] = station,
                ["fx"] = 10, ["fy"] = 20, ["mz"] = 123.45,
            })));
        root["fsec"] = new JObject { ["1"] = rows };
        root["fsecName"] = JArray.Parse("""[["1","Boundary"]]""");
        var pdf = Pdf(root, "portrait-member-boundary-" + middleRows);
        Assert.Equal(pages, pdf.Pages);
        var values = pdf.Runs.Where(run => run.Text == "123.45").ToArray();
        Assert.Equal(middleRows + 4, values.Length);
        // A4 portrait basic tables use the short-side viewport; text remains
        // above its bottom margin, even when a member must span several pages.
        Assert.All(values, run => Assert.InRange(run.Y, 270, 820));
        if (middleRows > 50)
            Assert.True(pdf.Runs.Count(run => run.Text == "MEMBER 2 (I - J) Beam") >= 2);
    }

    [Fact]
    public void PortraitLongMemberCaptionRepeatsCompleteTextWithoutLosingRows()
    {
        var root = Root();
        root["pageOrientation"] = "Vertical";
        root["referenceResultLayout"] = true;
        string caption = "MEMBER " + new string('荷', 120);
        var rows = new JArray(Enumerable.Range(0, 65).Select(i => new JObject
        {
            ["m"] = "5", ["n"] = "S" + i, ["stationLabel"] = "P" + i,
            ["memberCaption"] = caption, ["l"] = i, ["fx"] = 1, ["fy"] = 2, ["mz"] = 3,
        }));
        root["fsec"] = new JObject { ["1"] = rows };
        root["fsecName"] = JArray.Parse("""[["1","Long member"]]""");
        var pdf = Pdf(root, "portrait-long-member-caption");
        Assert.True(pdf.Pages >= 2);
        Assert.Equal(65, pdf.Runs.Count(run => run.Text == "3.00"));
        foreach (var page in pdf.Runs.GroupBy(run => run.Page))
        {
            var fragments = page.Where(run => run.Text.StartsWith("MEMBER 荷") ||
                run.Text.Length > 0 && run.Text.All(character => character == '荷')).ToArray();
            Assert.Equal(caption, string.Concat(fragments.Select(run => run.Text)));
            Assert.True(fragments.Length > 1);
            var firstValue = page.First(run => run.Text == "3.00");
            Assert.True(fragments.Last().Y - firstValue.Y >= 8);
        }
    }

    [Fact]
    public void PortraitMinOnlyPickupKeepsCorrelatedVectorAndWrappedSourceAcrossPages()
    {
        var root = Root();
        root["pageOrientation"] = "Vertical";
        root["referenceResultLayout"] = true;
        string expression = string.Join(" + ", Enumerable.Range(1, 70).Select(i => "1.125*CASE_A" + i.ToString("D3")));
        string expected = "(77:" + expression.Replace(" ", "") + ")";
        var rows = new JArray(Enumerable.Range(0, 95).Select(i => new JObject
        {
            ["m"] = "5", ["n"] = "S" + i, ["stationLabel"] = "P" + i,
            ["memberCaption"] = "MEMBER 5 (I - J) Beam", ["l"] = i,
            ["fx"] = i == 70 ? 700.25 : 10, ["fy"] = i == 70 ? -1400.5 : 20,
            ["mz"] = -1000 - i / 100d, ["case"] = i == 70 ? "77" : "6",
            ["comb"] = i == 70 ? expression : "1+4",
        }));
        root["fsecPickup"] = new JObject { ["1"] = new JObject { ["mz_min"] = rows } };
        root["fsecPickupName"] = JArray.Parse("""[["1","Filtered"]]""");
        var pdf = Pdf(root, "portrait-min-only-wrapped-source");
        Assert.DoesNotContain("MAX.", pdf.Text);
        Assert.Contains("MOMENT MIN.", pdf.Text);
        Assert.Equal(95, pdf.Runs.Count(run => Regex.IsMatch(run.Text, @"^-1000[.]\d\d$")));
        var moment = Assert.Single(pdf.Runs.Where(run => run.Text == "-1000.70"));
        var vector = pdf.Runs.Where(run => run.Page == moment.Page && Math.Abs(run.Y - moment.Y) < 0.01).ToArray();
        Assert.Contains(vector, run => run.Text == "700.25");
        Assert.Contains(vector, run => run.Text == "-1400.50");
        Assert.Contains(vector, run => run.Text.StartsWith("(77:"));
        var fragments = new List<(string Text, int Page)>();
        bool collecting = false;
        foreach (var run in pdf.Runs)
        {
            if (run.Text.StartsWith("(77:")) collecting = true;
            if (!collecting || run.X < 385 || run.Text.StartsWith("Page ") || run.Text == "COM: CASE") continue;
            fragments.Add((run.Text, run.Page));
            if (run.Text.EndsWith(")")) break;
        }
        Assert.Equal(expected, string.Concat(fragments.Select(fragment => fragment.Text)));
        Assert.True(fragments.Select(fragment => fragment.Page).Distinct().Count() > 1);
    }

    [Fact]
    public void ReferenceDisplacementPairsAdjacentCasesWithoutRepeatingValues()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        root["disg"] = JObject.Parse("""{"1":[{"id":"1","dx":12.3456,"dy":2,"rz":3}],"2":[{"id":"1","dx":98.7654,"dy":4,"rz":5}],"3":[{"id":"generated:11:S9","dx":23.4567,"dy":6,"rz":7}]}""");
        root["disgName"] = JArray.Parse("""[["1","Left"],["2","Right"],["3","Odd"]]""");
        var pdf = Pdf(root, "paired-displacement");
        Assert.Contains("Left", pdf.Text);
        Assert.Contains("12.3456", pdf.Text);
        Assert.Contains("CASE 1 Left", pdf.Text);
        Assert.Contains("CASE 2 Right", pdf.Text);
        Assert.Contains("generated:11:S9", pdf.Text);
        Assert.Equal(1, Regex.Matches(pdf.Text, "12[.]3456").Count);
        Assert.Equal(1, pdf.Pages);
        var left = Assert.Single(pdf.Runs.Where(run => run.Text == "CASE 1 Left"));
        var right = Assert.Single(pdf.Runs.Where(run => run.Text == "CASE 2 Right"));
        Assert.True(right.X > left.X + 250);
        Assert.Equal(left.Y, right.Y, 3);
    }

    [Fact]
    public void ReferenceForcesPairCasesAndRepeatMemberCaptionAfterPageBreak()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        var rows = new JArray(Enumerable.Range(0, 65).Select(i => new JObject
        {
            ["m"] = "5", ["n"] = "S" + i, ["stationLabel"] = i == 0 ? "I-TAN" : "POINT" + i,
            ["memberCaption"] = "MEMBER 5 (5 - 6) Beam", ["l"] = i / 10d,
            ["fx"] = 10, ["fy"] = 20, ["mz"] = 123.45,
        }));
        root["fsec"] = new JObject { ["1"] = rows, ["2"] = rows.DeepClone() };
        root["fsecName"] = JArray.Parse("""[["1","Left"],["2","Right"]]""");
        var pdf = Pdf(root, "paired-forces");
        Assert.Contains("MEMBER 5 (5 - 6) Beam", pdf.Text);
        Assert.Contains("I-TAN", pdf.Text);
        Assert.DoesNotContain("S64", pdf.Text);
        Assert.Equal(130, Regex.Matches(pdf.Text, "123[.]45").Count);
        Assert.True(pdf.Pages >= 2);
        Assert.True(Regex.Matches(pdf.Text, Regex.Escape("MEMBER 5 (5 - 6) Beam")).Count >= 2 * pdf.Pages);
        var left = pdf.Runs.First(run => run.Text == "CASE 1 Left");
        var right = pdf.Runs.First(run => run.Text == "CASE 2 Right");
        Assert.Equal(left.Y, right.Y, 3);
        Assert.True(right.X > left.X + 250);
        var rightMember = pdf.Runs.First(run => run.Text == "MEMBER 5 (5 - 6) Beam" && run.X > 300);
        // PDF Td operands are serialized at four decimal places; accumulated
        // text positions can differ by a fraction of one hundredth of a point.
        Assert.InRange(Math.Abs(right.X - rightMember.X), 0, 0.01);
    }

    [Fact]
    public void ReferencePickupPairsSignedModesWithCorrelatedSourcesAndExpressions()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        root["fsecPickup"] = JObject.Parse("""{"1":{"mz_max":[{"m":"5","n":"S0","stationLabel":"I-TAN","memberCaption":"MEMBER 5 (5 - 6) Beam","l":0,"fx":13,"fy":21,"mz":123.45,"case":"6","comb":"1+4+5+6"}],"mz_min":[{"m":"5","n":"S0","stationLabel":"I-TAN","memberCaption":"MEMBER 5 (5 - 6) Beam","l":0,"fx":17,"fy":28,"mz":-67.89,"case":"1","comb":"1+2+3+4+5"}]}}""");
        root["fsecPickupName"] = JArray.Parse("""[["1","Envelope"]]""");
        var pdf = Pdf(root, "paired-pickup");
        Assert.Contains("(6:1+4+5+6)", pdf.Text);
        Assert.Contains("(1:1+2+3+4+5)", pdf.Text);
        Assert.Contains("I-TAN", pdf.Text);
        Assert.Contains("PICK UP 1", pdf.Text);
        var max = Assert.Single(pdf.Runs.Where(run => run.Text == "123.45"));
        var min = Assert.Single(pdf.Runs.Where(run => run.Text == "-67.89"));
        Assert.Equal(max.Y, min.Y, 3);
        Assert.True(min.X > max.X + 250);
    }

    [Fact]
    public void ReferenceOrdinaryCombinationsPairFilteredVectorsAndKeepWeightedHeader()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        root["ordinaryDisplacementCombinations"] = new JArray("1", "2");
        root["disgCombine"] = JObject.Parse("""{"1":{"rz_min":{"1":{"dx":12.3456,"dy":2,"rz":3,"case":"1,2","comb":"1*D1 + 1.2*D2"}}},"2":{"dy_max":{"1":{"dx":98.7654,"dy":4,"rz":5,"case":"1","comb":"-0.8*D1"}}}}""");
        root["disgCombineName"] = JArray.Parse("""[["1","Left"],["2","Right"]]""");
        var pdf = Pdf(root, "paired-ordinary");
        Assert.Contains("1*D1 + 1.2*D2", pdf.Text);
        Assert.Equal(1, Regex.Matches(pdf.Text, "12[.]3456").Count);
        var left = Assert.Single(pdf.Runs.Where(run => run.Text == "12.3456"));
        var right = Assert.Single(pdf.Runs.Where(run => run.Text == "98.7654"));
        Assert.Equal(left.Y, right.Y, 3);
        Assert.True(right.X > left.X + 250);
        Assert.Equal(1, pdf.Pages);
    }

    [Fact]
    public void ReferenceTwelveNodeDisplacementUsesThreePairsPerPage()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        var cases = new JObject();
        var names = new JArray();
        for (int i = 1; i <= 10; i++)
        {
            cases[i + "/Static/0"] = new JArray(Enumerable.Range(1, 12).Select(node =>
                new JObject { ["id"] = node.ToString(), ["dx"] = i + node / 100d, ["dy"] = -i, ["rz"] = node / 10d }));
            names.Add(new JArray(i + "/Static/0", "Load" + i));
        }
        root["disg"] = cases;
        root["disgName"] = names;
        var pdf = Pdf(root, "ten-case-displacement");
        Assert.Equal(2, pdf.Pages);
        Assert.Contains("CASE 1 Load1", pdf.Text);
        Assert.DoesNotContain("/Static/0", pdf.Text);
        Assert.Equal(120, pdf.Runs.Count(run => Regex.IsMatch(run.Text, @"^-\d+[.]0000$")));
        Assert.Equal(1, Assert.Single(pdf.Runs.Where(run => run.Text == "CASE 5 Load5")).Page);
        Assert.Equal(2, Assert.Single(pdf.Runs.Where(run => run.Text == "CASE 7 Load7")).Page);
    }

    [Theory]
    [InlineData("Horizontal")]
    [InlineData("Vertical")]
    public void ReferenceLongCaseCaptionWrapsWithinItsPanelAndRetainsText(string orientation)
    {
        var root = Root();
        root["pageOrientation"] = orientation;
        root["referenceResultLayout"] = true;
        string name = "A deliberately long case caption with a weighted expression 1.2*D1 + 0.85*D2 + 1.35*D3 and a retained source";
        root["disg"] = JObject.Parse("""{"1":[{"id":"1","dx":1,"dy":2,"rz":3}]}""");
        root["disgName"] = new JArray { new JArray("1", name) };
        var pdf = Pdf(root, "long-caption-" + orientation);
        Assert.Contains(name, pdf.Text.Replace("\n", ""));
        Assert.DoesNotContain(pdf.Runs, run => run.Text == "CASE 1 " + name);
        Assert.Contains(pdf.Runs, run => run.Text.StartsWith("CASE 1 ") && run.Text.Length < name.Length);
    }

    [Fact]
    public void ReferenceMixedOrdinaryAndEnvelopeCombinationPreservesBothRoutes()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        root["ordinaryDisplacementCombinations"] = new JArray("1");
        root["disgCombine"] = JObject.Parse("""{"1":{"rz_min":{"1":{"dx":12.3456,"dy":2,"rz":3,"case":"1","comb":"1*D1"}}},"2":{"dy_max":{"1":{"dx":98.7654,"dy":4,"rz":5,"case":"2"}},"dy_min":{"1":{"dx":-67.8912,"dy":-4,"rz":-5,"case":"3"}}}}""");
        root["disgCombineName"] = JArray.Parse("""[["1","Ordinary"],["2","Envelope"]]""");
        Exception? failure = Record.Exception(() =>
        {
            var pdf = Pdf(root, "mixed-ordinary-envelope");
            Assert.Contains("12.3456", pdf.Text);
            Assert.Contains("98.7654", pdf.Text);
            Assert.Contains("-67.8912", pdf.Text);
        });
        Assert.Null(failure);
    }

    [Theory]
    [InlineData("Horizontal")]
    [InlineData("Vertical")]
    public void OversizedCaptionFailsBeforeAppendingEmptyPagesAndClosesDocument(string orientation)
    {
        var root = Root();
        root["pageOrientation"] = orientation;
        root["referenceResultLayout"] = true;
        root["disg"] = JObject.Parse("""{"1":[{"id":"1","dx":1,"dy":2,"rz":3}]}""");
        root["disgName"] = new JArray { new JArray("1", new string('荷', 2000)) };
        int active = PdfDocument.ActiveDocumentCount;
        var failure = Assert.Throws<InvalidOperationException>(() => new PrintInput(JObject.FromObject(root).ToString())
            .GetPdfBytes(maxPages: 3, maxBytes: DirectPdfGenerator.MaxPdfBytes));
        Assert.Contains("caption", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(active, PdfDocument.ActiveDocumentCount);
    }

    [Fact]
    public void WeightedPickupExpressionUsesMeasuredSourceWidthWithoutUnnecessaryWrap()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        root["fsecPickup"] = JObject.Parse("""{"1":{"mz_max":[{"m":"5","n":"S0","stationLabel":"I-TAN","memberCaption":"MEMBER 5","l":0,"fx":13,"fy":21,"mz":123.45,"case":"6","comb":"1.1*1 + 1.367*4 + 5 + 1.367*6"}]}}""");
        root["fsecPickupName"] = JArray.Parse("""[["1","Envelope"]]""");
        var pdf = Pdf(root, "weighted-pickup");
        Assert.Single(pdf.Runs.Where(run => run.Text == "(6:1.1*1+1.367*4+5+1.367*6)"));
        Assert.Contains("123.45", pdf.Text);
    }

    [Fact]
    public void LongMemberCaptionWrapsAndRepeatsEveryLineOnContinuationPages()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        string caption = "MEMBER " + new string('荷', 120);
        var rows = new JArray(Enumerable.Range(0, 65).Select(i => new JObject
        {
            ["m"] = "5", ["n"] = "S" + i, ["stationLabel"] = "P" + i,
            ["memberCaption"] = caption, ["l"] = i, ["fx"] = 1, ["fy"] = 2, ["mz"] = 3,
        }));
        root["fsec"] = new JObject { ["1"] = rows, ["2"] = rows.DeepClone() };
        root["fsecName"] = JArray.Parse("""[["1","Left"],["2","Right"]]""");
        var pdf = Pdf(root, "long-member-caption");
        Assert.True(pdf.Pages >= 2);
        Assert.DoesNotContain(pdf.Runs, run => run.Text == caption);
        foreach (var page in pdf.Runs.GroupBy(run => run.Page))
        foreach (bool right in new[] { false, true })
        {
            var fragments = page.Where(run => (run.X > 300) == right &&
                (run.Text.StartsWith("MEMBER 荷") || run.Text.Length > 0 && run.Text.All(character => character == '荷'))).ToArray();
            Assert.Equal(caption, string.Concat(fragments.Select(run => run.Text)));
            Assert.True(fragments.Length > 1);
            Assert.All(fragments, fragment => Assert.InRange(fragment.X, right ? 430 : 35, right ? 450 : 45));
        }
        Assert.Equal(130, pdf.Runs.Count(run => run.Text == "3.00"));
    }

    [Fact]
    public void ShortNodeIdsLeaveRoomForJointHeadingInBothBaseAndCombinationPanels()
    {
        var root = Root();
        root["referenceResultLayout"] = true;
        root["ordinaryDisplacementCombinations"] = new JArray("1", "2");
        root["disg"] = JObject.Parse("""{"1":[{"id":"1","dx":1,"dy":2,"rz":3}],"2":[{"id":"12","dx":4,"dy":5,"rz":6}]}""");
        root["disgName"] = JArray.Parse("""[["1","Left"],["2","Right"]]""");
        root["disgCombine"] = JObject.Parse("""{"1":{"rz_min":{"1":{"dx":1,"dy":2,"rz":3}}},"2":{"dy_max":{"12":{"dx":4,"dy":5,"rz":6}}}}""");
        root["disgCombineName"] = JArray.Parse("""[["1","Left"],["2","Right"]]""");
        var pdf = Pdf(root, "short-id-header-spacing");
        using var measurement = new PdfSharpCore.Pdf.PdfDocument();
        using var graphics = PdfSharpCore.Drawing.XGraphics.FromPdfPage(measurement.AddPage());
        var font = new PdfSharpCore.Drawing.XFont("MS Mincho", 10);
        double headingWidth = graphics.MeasureString("JOINT NO.", font).Width;
        var headings = pdf.Runs.Where(run => run.Text == "JOINT NO.").ToArray();
        Assert.Equal(4, headings.Length);
        foreach (var heading in headings)
        {
            var translation = pdf.Runs.First(run => run.Text == "X-DISP." && run.Page == heading.Page &&
                Math.Abs(run.Y - heading.Y) < 0.01 && run.X > heading.X);
            Assert.InRange(heading.X + headingWidth, double.MinValue, translation.X + 0.01);
        }
    }
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void MarkedOrdinaryCombinationPrintsOneSelectedVector(int dimension)
    {
        var root = Root(dimension);
        root["ordinaryDisplacementCombinations"] = new JArray("1");
        root["disgCombine"] = JObject.Parse("""{"1":{"dy_max":{"1":{"dx":1,"dy":2,"rz":3}},"rz_min":{"1":{"dx":1,"dy":2,"rz":3}}}}""");
        root["disgCombineName"] = JArray.Parse("""[["1","Ordinary"]]""");
        var tables = new InspectDisg(root).Tables(new PrintData(root));
        Assert.Single(tables);
        Assert.DoesNotContain("Max", tables[0][1, 0] ?? "");
        AssertParseable(root, "ordinary-" + dimension);
    }

    [Fact]
    public void UnmarkedCombinationAndPickupKeepDistinctEnvelopeModes()
    {
        foreach (string key in new[] { "disgCombine", "disgPickup" })
        {
            var root = Root();
            root["ordinaryDisplacementCombinations"] = new JArray("1");
            root[key] = JObject.Parse("""{"1":{"dy_max":{"1":{"dy":20}},"rz_min":{"1":{"dy":-7}}}}""");
            if (key == "disgCombine") root.Remove("ordinaryDisplacementCombinations");
            root[key + "Name"] = JArray.Parse("""[["1","Envelope"]]""");
            Assert.Equal(2, new InspectDisg(root, key).Tables(new PrintData(root)).Length);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ProvenancePreservesSourceAndWeightedExpressionAsText(int dimension)
    {
        var root = Root(dimension);
        root["disgCombine"] = JObject.Parse("""{"1":{"rz_min":{"1":{"dx":1,"dy":2,"rz":3,"case":"1,2,3,4,5","comb":"1*D1 + 1.2*D2"}}}}""");
        root["disgCombineName"] = JArray.Parse("""[["1","Combination"]]""");
        var table = Assert.Single(new InspectDisg(root).Tables(new PrintData(root)));
        string provenance = string.Join("", Enumerable.Range(5, table.Rows - 5).Select(row => table[row, table.Columns - 1]));
        Assert.Equal("(1,2,3,4,5 : 1*D1 + 1.2*D2)", provenance);
        AssertParseable(root, "provenance-" + dimension);
    }

    internal static Dictionary<string, object> Root(int dimension = 2) => new()
    {
        ["dimension"] = dimension, ["language"] = "en", ["pageSize"] = "A4",
        ["pageOrientation"] = "Horizontal", ["hasPrintInputData"] = false,
        ["hasPrintCalculation"] = true,
    };

    private static void AssertParseable(Dictionary<string, object> root, string name)
    {
        byte[] bytes = DirectPdfGenerator.Generate(JObject.FromObject(root).ToString());
        using var document = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.NotEmpty(document.Pages);
        string? artifacts = Environment.GetEnvironmentVariable("TRANSVERSE_COMPOSER_ARTIFACTS");
        if (artifacts is not null)
        {
            Directory.CreateDirectory(artifacts);
            File.WriteAllBytes(Path.Combine(artifacts, name + ".pdf"), bytes);
        }
    }

    private static (string Text, int Pages, List<(string Text, double X, double Y, int Page)> Runs) Pdf(Dictionary<string, object> root, string name)
    {
        byte[] bytes = DirectPdfGenerator.Generate(JObject.FromObject(root).ToString());
        using var document = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        var text = new StringBuilder();
        var runs = new List<(string Text, double X, double Y, int Page)>();
        int pageNumber = 0;
        foreach (var page in document.Pages)
        {
            pageNumber++;
            var fonts = page.Resources.Elements.GetDictionary("/Font");
            var maps = new Dictionary<string, Dictionary<int, string>>();
            foreach (string font in fonts.Elements.Keys)
            {
                var cmap = fonts.Elements.GetDictionary(font).Elements.GetDictionary("/ToUnicode");
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
            foreach (var operation in ContentReader.ReadContent(page).OfType<COperator>())
            {
                double Number(int i) => double.Parse(operation.Operands[i].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                if (operation.OpCode.Name == "BT") { x = 0; y = 0; }
                if (operation.OpCode.Name == "Td" || operation.OpCode.Name == "TD") { x += Number(0); y += Number(1); }
                if (operation.OpCode.Name == "Tm") { x = Number(4); y = Number(5); }
                if (operation.OpCode.Name == "Tf") selectedFont = ((CName)operation.Operands[0]).Name;
                if (operation.OpCode.Name != "Tj" || operation.Operands[0] is not CString value) continue;
                if (!maps.TryGetValue(selectedFont, out var map)) { text.Append(value.Value); continue; }
                var decoded = new StringBuilder();
                for (int i = 0; i + 1 < value.Value.Length; i += 2)
                    if (map.TryGetValue((value.Value[i] << 8) | value.Value[i + 1], out string? character)) decoded.Append(character);
                text.Append(decoded);
                runs.Add((decoded.ToString(), x, y, pageNumber));
                text.Append('\n');
            }
        }
        string? artifacts = Environment.GetEnvironmentVariable("TRANSVERSE_COMPOSER_ARTIFACTS");
        if (artifacts is not null)
        {
            Directory.CreateDirectory(artifacts);
            File.WriteAllBytes(Path.Combine(artifacts, name + ".pdf"), bytes);
            File.WriteAllText(Path.Combine(artifacts, name + ".txt"), text.ToString());
        }
        return (text.ToString(), document.PageCount, runs);
    }

    private sealed class InspectDisg(Dictionary<string, object> root, string key = "disgCombine") : ResultDisgCombine(root, key)
    {
        internal Table[] Tables(PrintData data)
        {
            int page = 1;
            using var document = new PdfDocument(data, ref page);
            PrintInit(document, data, out _, out _, out _);
            return GetContexts().SelectMany(context => context.GetTables()).ToArray();
        }
    }
}

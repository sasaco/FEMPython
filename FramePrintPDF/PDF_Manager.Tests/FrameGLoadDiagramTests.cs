using Newtonsoft.Json.Linq;
using PDF_Manager;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using PdfSharpCore.Pdf.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace FrameWebforCS.Tests;

/// <summary>
/// FrameG の体裁の荷重図（原宿ボックス 0404 の見本_荷重図.pdf）。位置は見本の pt（上から）を ±1 mm で見る。
/// </summary>
public sealed class FrameGLoadDiagramTests
{
    private const double Millimetre = 72 / 25.4;

    [Fact]
    public void TwoTierPagesKeepTheSingleCasesAloneLikeTheSample()
    {
        var pdf = Render(Box());

        Assert.Equal(7, pdf.Pages);
        int[][] expected = [[1], [2], [3, 4], [5, 6], [7, 8], [9, 10], [11]];
        for (int page = 1; page <= 7; page++)
            Assert.Equal(expected[page - 1], pdf.Runs.Where(r => r.Page == page && r.Text.StartsWith(" Ｃase "))
                .Select(r => int.Parse(r.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1])).ToArray());
        Assert.DoesNotContain(pdf.Runs, r => r.Text.StartsWith("TITLE"));
        Assert.DoesNotContain(pdf.Runs, r => r.Text.Contains("DataVer"));
    }

    [Fact]
    public void HeadingsSitWhereFrameGPrintsThem()
    {
        var pdf = Render(Box());

        Near(pdf.Single(1, " 荷 重 図"), 69.6, 79.6);
        Near(pdf.Single(1, " Ｃase 1  固定死荷重　D1"), 78.0, 92.2);
        Near(pdf.Single(3, " Ｃase 3  永久作用としての鉛直土圧　Evp"), 78.0, 83.8);
        Near(pdf.Single(3, " Ｃase 4  永久作用としての側圧　EHP"), 78.0, 443.3);
        Assert.Single(pdf.Runs, r => r.Text == " 荷 重 図");
        Assert.All(pdf.Runs.Where(r => r.Text.StartsWith(" Ｃase")), r => Assert.Equal(9.66, r.Size, 2));
    }

    [Fact]
    public void LoadValuesAndDimensionsAreWrittenOutsideTheFrame()
    {
        var pdf = Render(Box());

        // ケース 3：範囲指定の上床版の荷重は 4.75 の寸法 1 本、値は帯の上
        Near(pdf.Single(3, "  19.80"), 275.8, 212.8);
        Near(pdf.Single(3, "4.75"), 290.5, 193.0);
        // ケース 1：上床版の 2 つの荷重は外へ積み、1 本の部材の全長の下床版の荷重には寸法を付けない
        Assert.Equal(2, pdf.Runs.Count(r => r.Page == 1 && r.Text == "4.75"));
        Near(pdf.Single(1, "  8.60"), 291.5, 280.6);
        Near(pdf.Single(1, "  2.30"), 291.5, 238.1);
        Near(pdf.Single(1, "  12.30"), 289.4, 544.6);
        var axial = pdf.Runs.Where(r => r.Page == 1 && r.Text == "q =11.00").OrderBy(r => r.X).ToArray();
        Assert.Equal(2, axial.Length);
        Near(axial[0], 158.5, 465.7);
        Near(axial[1], 472.2, 465.7);
        Assert.All(axial, r => Assert.Equal(90, r.Angle, 1));
        var nodal = pdf.Runs.Where(r => r.Page == 1 && r.Text == "  1.10").OrderBy(r => r.X).ToArray();
        Near(nodal[0], 139.0, 334.9);
        Near(nodal[1], 492.0, 334.9);
        // ケース 2：L1・L2 のある荷重は 0.225・4.30・0.225
        Assert.Equal(["0.225", "4.30", "0.225", "0.225", "4.30", "0.225"],
            pdf.Runs.Where(r => r.Page == 2 && Regex.IsMatch(r.Text, @"^\d\.\d+$")).Select(r => r.Text).ToArray());
        // ケース 4：鉛直の部材の台形は両端に横書き、側壁の寸法 2.474・0.751 は回転
        foreach (var (text, x, y) in new[] { ("14.00", 136.6, 603.2), ("40.00", 120.1, 706.6), ("34.30", 124.4, 714.4),
            ("41.00", 129.0, 741.7), ("14.00", 426.6, 603.2), ("40.00", 443.0, 706.6), ("34.30", 438.6, 714.4) })
        {
            Run run = pdf.Runs.Where(r => r.Page == 3 && r.Text == text).OrderBy(r => Math.Abs(r.X - x)).First();
            Near(run, x, y);
            Assert.Equal(0, run.Angle, 1);
        }
        var side = pdf.Runs.Where(r => r.Page == 3 && r.Text == "2.474").OrderBy(r => r.X).ToArray();
        Near(side[0], 110.8, 666.8);
        Near(side[1], 495.8, 666.8);
        // ケース 5：M/S の対は 1 つの帯に Ws（外）と Wm
        Near(pdf.Single(4, "  Ws=73.10"), 269.6, 205.4);
        Near(pdf.Single(4, "  Wm=61.00"), 269.6, 214.2);
        // ケース 7：一様な側圧は値も回転して帯の外
        var lateral = pdf.Runs.Where(r => r.Page == 5 && r.Text == "  17.30" && 842 - r.Y < 421).OrderBy(r => r.X).ToArray();
        Near(lateral[0], 163.8, 306.1);
        Near(lateral[1], 439.7, 306.1);
        // ケース 10：温度
        Near(pdf.Single(6, "12.5℃"), 286.3, 593.9);
    }

    [Fact]
    public void FrameAndRigidZonesUseFrameGLineWeights()
    {
        var pdf = Render(Box());
        var strokes = pdf.Strokes.Where(s => s.Page == 3).ToList();
        // 上床版（0.84）と剛域の部材（1.44）、下の隅の剛域（1.68）
        Assert.Contains(strokes, s => s.Width == 0.84 && Close(s.Points[0], 203.6, 231.8) && Close(s.Points[^1], 394.1, 231.8));
        Assert.Contains(strokes, s => s.Width == 1.44 && Close(s.Points[0], 187.2, 231.8) && Close(s.Points[^1], 203.6, 231.8));
        Assert.Contains(strokes, s => s.Width == 1.68 && Close(s.Points[0], 187.2, 365.9) && Close(s.Points[^1], 187.2, 360.2));
    }

    [Fact]
    public void SingleLayoutPrintsEveryCaseOnItsOwnPage()
    {
        JObject root = Box();
        root["PrintLoad"]!["diagramInput"]!["layout"] = "single";
        ((JObject)root["PrintLoad"]!["diagramInput"]!).Remove("single_layout_cases");
        var pdf = Render(root);
        Assert.Equal(11, pdf.Pages);
        Assert.All(Enumerable.Range(1, 11), page => Near(pdf.Runs.Single(r => r.Page == page && r.Text.StartsWith(" Ｃase ")),
            78.0, page == 1 ? 92.2 : 83.8));
    }

    [Theory]
    [InlineData("en", " Load Diagram", " Case 3  永久作用としての鉛直土圧  Evp")]
    [InlineData("cn", " 载 重 图", " Case 3  永久作用としての鉛直土圧　Evp")]
    public void TranslatedHeadingsKeepThePlacement(string language, string title, string heading)
    {
        JObject root = Box();
        root["language"] = language;
        var pdf = Render(root);
        Near(pdf.Single(1, title), 69.6, 79.6);
        Near(pdf.Single(3, heading), 78.0, 83.8);
    }

    [Fact]
    public void LandscapePaperUsesTheSameProportions()
    {
        JObject root = Box();
        root["pageOrientation"] = "Horizontal";
        root["PrintLoad"]!["pageOrientation"] = "Horizontal";
        var pdf = Render(root);
        Assert.Equal(7, pdf.Pages);
        Near(pdf.Single(3, " Ｃase 3  永久作用としての鉛直土圧　Evp"), 78.0 / 595 * 842, 83.8 / 842 * 595, height: 595);
        Near(pdf.Single(3, " Ｃase 4  永久作用としての側圧　EHP"), 78.0 / 595 * 842, 443.3 / 842 * 595, height: 595);
    }

    // ---- 見本に無い荷重（WebPier・関内EVピットの帳票も通る）。長さ 10 m の水平の梁 1 本で確かめる ----

    [Fact]
    public void NegativeL1ContinuesFromThePreviousRowAndNegativeL2IsTheLoadedWidth()
    {
        // 行 1：1 m から幅 2 m（1〜3）。行 2：前の終わり 3 m から 1 m 空けて幅 3 m（4〜7）。
        var pdf = Render(Beam("""
            [{"row":1,"m1":"1","m2":"","direction":"y","mark":"2","L1":"1","L2":-2,"P1":5,"P2":5},
             {"row":2,"m1":"1","m2":"","direction":"y","mark":"2","L1":"-1","L2":-3,"P1":6,"P2":6}]
            """));
        // 外の段（行 2）が上
        Assert.Equal(["4.00", "3.00", "3.00", "1.00", "2.00", "7.00"], Dimensions(pdf));
    }

    [Fact]
    public void ANegativeL1WithoutAPreviousRowStartsBeforeTheMemberAndIsCut()
    {
        // 続きでない負の L1：-1 m から 3 m（正の L2 は終わりの位置。FrameWeb の展開と同じ）を 0 m で切り、0 m の値を補う。
        var pdf = Render(Beam("""
            [{"row":5,"m1":"1","m2":"","direction":"y","mark":"2","L1":"-1","L2":3,"P1":0,"P2":8}]
            """));
        // 寸法 3.00・7.00 と、台形の両端の値 2.00（0 m の値 0 + 8·1/4）・8.00
        Assert.Equal(["2.00", "3.00", "7.00", "8.00"], Dimensions(pdf).OrderBy(text => text, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void NegativeEndMemberRangeKeepsRelativeDistancesAlongTheRange()
    {
        // 部材 1〜2（範囲指定、合わせて 10 m）に 6 m から幅 3 m。
        var pdf = Render(Beam("""
            [{"row":1,"m1":"1","m2":"-2","direction":"y","mark":"2","L1":"6","L2":-3,"P1":5,"P2":5}]
            """, split: true));
        Assert.Equal(["6.00", "3.00", "1.00"], Dimensions(pdf));
    }

    [Fact]
    public void PartialSpringIntervalsFollowEachOtherAndZeroStiffnessIsNotDrawn()
    {
        JObject root = Beam("[]");
        root["fix_member"] = JObject.Parse("""
            {"1":[{"row":1,"m":"1","length":4,"tx":0,"ty":100},{"row":2,"m":"1","length":3,"tx":0,"ty":0},
                  {"row":3,"m":"1","tx":0,"ty":200}]}
            """);
        var pdf = Render(root);
        var (x0, scale, _) = BeamLine(pdf);
        var springs = pdf.Strokes.Where(s => s.Width == 0.36 && s.Points.Count == 6).Select(s => (s.Points.Min(p => p.X) - x0) / scale).ToList();
        Assert.Contains(springs, x => x < 4);
        Assert.Contains(springs, x => x > 7);
        Assert.DoesNotContain(springs, x => x > 4.05 && x < 6.95);
    }

    [Fact]
    public void ALoadThatChangesSignIsDrawnOnBothSidesWithSignedValues()
    {
        var pdf = Render(Beam("""
            [{"row":1,"m1":"1","m2":"","direction":"y","mark":"2","L1":"0","L2":0,"P1":-10,"P2":20}]
            """));
        var (_, _, y) = BeamLine(pdf);
        Assert.Contains(pdf.Runs, r => r.Text.Trim() == "-10.00");
        Assert.Contains(pdf.Runs, r => r.Text.Trim() == "20.00");
        var outlines = pdf.Strokes.Where(s => s.Width == 0.48).SelectMany(s => s.Points).ToList();
        Assert.Contains(outlines, p => p.Y > y + 3);   // PDF の y は上向き：部材の上
        Assert.Contains(outlines, p => p.Y < y - 3);   // 部材の下
    }

    [Fact]
    public void AnMsPairWithDifferentEndsShowsBothEnds()
    {
        var pdf = Render(Beam("""
            [{"row":1,"m1":"1","m2":"","direction":"y","mark":"2","L1":"0","L2":0,"P1":10,"P2":20,"ms":"M"},
             {"row":2,"m1":"1","m2":"","direction":"y","mark":"2","L1":"0","L2":0,"P1":12,"P2":24,"ms":"S"}]
            """));
        Assert.Contains(pdf.Runs, r => r.Text.Trim() == "Ws=12.00～24.00");
        Assert.Contains(pdf.Runs, r => r.Text.Trim() == "Wm=10.00～20.00");
    }

    [Fact]
    public void AnAxialLoadShowsItsRangeAndBothValues()
    {
        var pdf = Render(Beam("""
            [{"row":1,"m1":"1","m2":"","direction":"x","mark":"2","L1":"2","L2":3,"P1":4,"P2":6}]
            """));
        var (x0, scale, _) = BeamLine(pdf);
        Assert.Contains(pdf.Runs, r => r.Text == "q =4.00～6.00");
        // 範囲の両端の印（2 m と 7 m）
        var ticks = pdf.Strokes.Where(s => s.Width == 0.36 && s.Points.Count == 2 && Math.Abs(s.Points[0].X - s.Points[1].X) < 0.01)
            .Select(s => Math.Round((s.Points[0].X - x0) / scale, 2)).ToList();
        Assert.Contains(2.0, ticks);
        Assert.Contains(7.0, ticks);
    }

    [Theory]
    [InlineData(1, 1, 1, false)]
    [InlineData(1, 5000, 0, true)]
    [InlineData(2000, 1, 0, true)]
    public void SpringSupportsUseTheSpringSymbol(double tx, double ty, double rz, bool spring)
    {
        JObject root = Beam("[]");
        root["fix_node"] = JObject.Parse($$"""{"1":[{"row":1,"n":"1","tx":{{tx}},"ty":{{ty}},"rz":{{rz}}}]}""");
        var pdf = Render(root);
        bool zigzag = pdf.Strokes.Any(s => s.Points.Count >= 5 && s.Width == 0.36);
        Assert.Equal(spring, zigzag);
    }

    [Theory]
    [InlineData("z")]
    [InlineData("gz")]
    [InlineData("r")]
    public void OutOfPlaneMemberLoadsAreNotDrawnAsLocalY(string direction)
    {
        var pdf = Render(Beam($$"""
            [{"row":1,"m1":"1","m2":"","direction":"{{direction}}","mark":"2","L1":"0","L2":0,"P1":5,"P2":5}]
            """));
        Assert.DoesNotContain(pdf.Runs, r => r.Text.Contains("5.00"));
        Assert.DoesNotContain(pdf.Strokes, s => s.Width == 0.48);
    }

    /// <summary>寸法の文字（頁・上から下・左から右の順）。</summary>
    private static string[] Dimensions(PdfResult pdf) => pdf.Runs
        .Where(r => Regex.IsMatch(r.Text, @"^\d+\.\d+$")).OrderByDescending(r => Math.Round(r.Y, 1)).ThenBy(r => r.X)
        .Select(r => r.Text).ToArray();

    /// <summary>梁の線（0.84）の左端の x・1 m の pt・y（PDF の座標）。</summary>
    private static (double X0, double Scale, double Y) BeamLine(PdfResult pdf)
    {
        var lines = pdf.Strokes.Where(s => s.Width == 0.84).ToList();
        double left = lines.Min(s => s.Points.Min(p => p.X)), right = lines.Max(s => s.Points.Max(p => p.X));
        return (left, (right - left) / 10, lines[0].Points[0].Y);
    }

    /// <summary>長さ 10 m の水平の梁（<paramref name="split"/> なら 4 m と 6 m の 2 部材）と 1 ケースの部材荷重。</summary>
    private static JObject Beam(string memberLoads, bool split = false)
    {
        var root = JObject.Parse(split ? """
            {"node":{"1":{"x":0,"y":0},"2":{"x":4,"y":0},"3":{"x":10,"y":0}},
             "member":{"1":{"ni":"1","nj":"2","e":"1"},"2":{"ni":"2","nj":"3","e":"1"}}}
            """ : """
            {"node":{"1":{"x":0,"y":0},"2":{"x":10,"y":0}},"member":{"1":{"ni":"1","nj":"2","e":"1"}}}
            """);
        root.Merge(JObject.Parse("""
            {"dimension":2,"language":"ja","pageSize":"A4","pageOrientation":"Vertical","ver":"2.5.12","title":"Beam",
             "element":{"1":{"1":{"E":1,"A":1,"Iz":1,"n":"梁"}}},"fix_node":{"1":[]},"fix_member":{"1":[]},
             "PrintLoad":{"diagramInput":{"layout":"single","output":["load"]},"pageOrientation":"Vertical"}}
            """));
        root["load"] = new JObject { ["1"] = new JObject { ["fix_node"] = 1, ["fix_member"] = 1, ["symbol"] = "L",
            ["name"] = "荷重", ["load_node"] = new JArray(), ["load_member"] = JArray.Parse(memberLoads) } };
        return root;
    }


    private static bool Close((double X, double Y) point, double x, double top) =>
        Math.Abs(point.X - x) < 0.5 && Math.Abs(842 - point.Y - top) < 0.5;

    private static void Near(Run run, double x, double top, double height = 842)
    {
        Assert.True(Math.Abs(height - run.Y - top) <= Millimetre, $"{run.Text}: baseline {height - run.Y:0.00} (sample {top})");
        Assert.True(Math.Abs(run.X - x) <= Millimetre, $"{run.Text}: x {run.X:0.00} (sample {x})");
    }

    /// <summary>原宿ボックス 0404 の骨組と 11 ケースの荷重（変換した保存形式の JSON から印刷用の項目だけ）。</summary>
    private static JObject Box()
    {
        var root = JObject.Parse("""
            {"dimension":2,"language":"ja","pageSize":"A4","pageOrientation":"Vertical","ver":"2.5.12",
             "title":"0404_完成時FrameG",
             "node":{"1":{"x":0.0,"y":-3.225},"2":{"x":0.35,"y":-3.225},"3":{"x":4.4,"y":-3.225},"4":{"x":4.75,"y":-3.225},
                     "5":{"x":0.0,"y":-2.975},"6":{"x":0.0,"y":-0.751},"7":{"x":0.0,"y":0.0},"8":{"x":4.75,"y":-2.975},
                     "9":{"x":4.75,"y":-0.751},"10":{"x":4.75,"y":0.0}},
             "member":{"1":{"ni":"1","nj":"2","e":"1"},"2":{"ni":"2","nj":"3","e":"2"},"3":{"ni":"3","nj":"4","e":"1"},
                       "4":{"ni":"1","nj":"5","e":"1"},"5":{"ni":"5","nj":"6","e":"3"},"6":{"ni":"6","nj":"7","e":"3"},
                       "7":{"ni":"4","nj":"8","e":"1"},"8":{"ni":"8","nj":"9","e":"4"},"9":{"ni":"9","nj":"10","e":"4"},
                       "10":{"ni":"7","nj":"10","e":"5"}},
             "element":{"1":{"1":{"E":26500000.0,"Xp":1e-05,"A":1000.0,"Iz":1000.0,"n":"剛域"},
                             "2":{"E":26500000.0,"Xp":1e-05,"A":0.35,"Iz":0.00357,"n":"上床版"},
                             "3":{"E":26500000.0,"Xp":1e-05,"A":0.45,"Iz":0.00759,"n":"左側壁"},
                             "4":{"E":26500000.0,"Xp":1e-05,"A":0.45,"Iz":0.00759,"n":"右側壁"},
                             "5":{"E":26500000.0,"Xp":1e-05,"A":0.5,"Iz":0.01042,"n":"下床版"}}},
             "rigid":[{"m":"6","Ilength":0.0,"Jlength":0.138,"e":1},{"m":"9","Ilength":0.0,"Jlength":0.138,"e":1},
                      {"m":"10","Ilength":0.1,"Jlength":0.1,"e":1}],
             "fix_node":{"1":[]},
             "fix_member":{"1":[{"m":"10","tx":1646.0,"ty":4937.0}],
                           "2":[{"m":"4","tx":2146.0,"ty":6438.0},{"m":"5","tx":2146.0,"ty":6438.0},{"m":"6","tx":4292.0,"ty":12875.0},
                                {"m":"7","tx":2146.0,"ty":6438.0},{"m":"8","tx":2146.0,"ty":6438.0},{"m":"9","tx":4292.0,"ty":12875.0},
                                {"m":"10","tx":3291.0,"ty":9874.0}],
                           "3":[{"m":"10","tx":3291.0,"ty":9874.0}],
                           "4":[{"m":"7","tx":2146.0,"ty":6438.0},{"m":"8","tx":2146.0,"ty":6438.0},{"m":"9","tx":4292.0,"ty":12875.0},
                                {"m":"10","tx":3291.0,"ty":9874.0}],
                           "5":[{"m":"4","tx":2146.0,"ty":6438.0},{"m":"5","tx":2146.0,"ty":6438.0},{"m":"6","tx":4292.0,"ty":12875.0},
                                {"m":"10","tx":3291.0,"ty":9874.0}]},
             "load":{
              "1":{"fix_node":1,"fix_member":1,"symbol":"D1","name":"固定死荷重",
                   "load_node":[{"n":"1","tx":0.0,"ty":1.1,"rz":0.0},{"n":"4","tx":0.0,"ty":1.1,"rz":0.0}],
                   "load_member":[{"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":8.6,"P2":8.6},
                                  {"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":2.3,"P2":2.3},
                                  {"m1":"10","m2":"","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":12.3,"P2":12.3},
                                  {"m1":"4","m2":"-6","direction":"x","mark":"2","L1":"0.000","L2":0.0,"P1":11.0,"P2":11.0},
                                  {"m1":"7","m2":"-9","direction":"x","mark":"2","L1":"0.000","L2":0.0,"P1":11.0,"P2":11.0}]},
              "2":{"fix_node":1,"fix_member":1,"symbol":"D2","name":"付加死荷重","load_node":[],
                   "load_member":[{"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.225","L2":0.225,"P1":2.3,"P2":2.3},
                                  {"m1":"10","m2":"","direction":"y","mark":"2","L1":"0.225","L2":0.225,"P1":2.3,"P2":2.3}]},
              "3":{"fix_node":1,"fix_member":1,"symbol":"Evp","name":"永久作用としての鉛直土圧","load_node":[],
                   "load_member":[{"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":19.8,"P2":19.8}]},
              "4":{"fix_node":1,"fix_member":1,"symbol":"EHP","name":"永久作用としての側圧","load_node":[],
                   "load_member":[{"m1":"4","m2":"-5","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":-14.0,"P2":-40.0},
                                  {"m1":"6","m2":"","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":-34.3,"P2":-41.0},
                                  {"m1":"7","m2":"-8","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":14.0,"P2":40.0},
                                  {"m1":"9","m2":"","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":34.3,"P2":41.0}]},
              "5":{"fix_node":1,"fix_member":2,"symbol":"GLV1","name":"地表面上の変動作用による鉛直土圧","load_node":[],
                   "load_member":[{"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":61.0,"P2":61.0,"ms":"M"},
                                  {"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":73.1,"P2":73.1,"ms":"S"}]},
              "6":{"fix_node":1,"fix_member":2,"symbol":"GLV2","name":"地表面上の変動作用による鉛直土圧","load_node":[],
                   "load_member":[{"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":56.2,"P2":56.2,"ms":"M"},
                                  {"m1":"1","m2":"-3","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":67.5,"P2":67.5,"ms":"S"}]},
              "7":{"fix_node":1,"fix_member":3,"symbol":"GLH1","name":"地表面上の変動作用による水平土圧（→←）","load_node":[],
                   "load_member":[{"m1":"4","m2":"-5","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":-17.3,"P2":-17.3},
                                  {"m1":"6","m2":"","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":-14.8,"P2":-14.8},
                                  {"m1":"7","m2":"-8","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":17.3,"P2":17.3},
                                  {"m1":"9","m2":"","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":14.8,"P2":14.8}]},
              "8":{"fix_node":1,"fix_member":4,"symbol":"GLH2","name":"地表面上の変動作用による水平土圧（→）","load_node":[],
                   "load_member":[{"m1":"4","m2":"-5","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":-17.3,"P2":-17.3},
                                  {"m1":"6","m2":"","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":-14.8,"P2":-14.8}]},
              "9":{"fix_node":1,"fix_member":5,"symbol":"GLH3","name":"地表面上の変動作用による水平土圧（←）","load_node":[],
                   "load_member":[{"m1":"7","m2":"-8","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":17.3,"P2":17.3},
                                  {"m1":"9","m2":"","direction":"y","mark":"2","L1":"0.000","L2":0.0,"P1":14.8,"P2":14.8}]},
              "10":{"fix_node":1,"fix_member":1,"symbol":"T","name":"温度変化","load_node":[],
                    "load_member":[{"m1":"1","m2":"-3","direction":"","mark":"9","L1":"0.000","L2":0.0,"P1":12.5,"P2":0.0}]},
              "11":{"fix_node":1,"fix_member":3,"symbol":"LP","name":"群集荷重","load_node":[],
                    "load_member":[{"m1":"10","m2":"","direction":"y","mark":"2","L1":"0.225","L2":0.225,"P1":5.0,"P2":5.0}]}},
             "PrintLoad":{"diagramInput":{"layout":"splitHorizontal","output":["load"],"single_layout_cases":[0,1]},
                          "pageOrientation":"Vertical"}}
            """);
        return root;
    }

    private sealed record Run(string Text, double X, double Y, int Page, double Angle, double Size);
    private sealed record Stroke(List<(double X, double Y)> Points, int Page, double Width);
    private sealed record PdfResult(int Pages, List<Run> Runs, List<Stroke> Strokes)
    {
        public Run Single(int page, string text) => Assert.Single(Runs, r => r.Page == page && r.Text == text);
    }
    private sealed record PdfMatrix(double A, double B, double C, double D, double E, double F)
    {
        internal static PdfMatrix Identity => new(1, 0, 0, 1, 0, 0);
        internal (double X, double Y) Point(double x, double y) => (A * x + C * y + E, B * x + D * y + F);
        internal PdfMatrix Multiply(PdfMatrix right) => new(A * right.A + C * right.B, B * right.A + D * right.B,
            A * right.C + C * right.D, B * right.C + D * right.D,
            A * right.E + C * right.F + E, B * right.E + D * right.F + F);
    }

    /// <summary>PDF の文字（基線の始まり、PDF の座標）と線（太さ）を読む。</summary>
    private static PdfResult Render(JObject root)
    {
        byte[] bytes = DirectPdfGenerator.Generate(root.ToString());
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        var runs = new List<Run>(); var strokes = new List<Stroke>();
        for (int pageIndex = 0; pageIndex < pdf.PageCount; pageIndex++)
        {
            var page = pdf.Pages[pageIndex];
            var maps = new Dictionary<string, Dictionary<int, string>>();
            var fonts = page.Resources.Elements.GetDictionary("/Font");
            foreach (string font in fonts?.Elements.Keys ?? Enumerable.Empty<string>())
            {
                var cmap = fonts!.Elements.GetDictionary(font).Elements.GetDictionary("/ToUnicode");
                if (cmap?.Stream is null) continue;
                var map = new Dictionary<int, string>(); string source = Encoding.ASCII.GetString(cmap.Stream.UnfilteredValue);
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
            string selectedFont = ""; double x = 0, y = 0, size = 0, width = 1;
            PdfMatrix transform = PdfMatrix.Identity, textMatrix = PdfMatrix.Identity;
            var state = new Stack<(PdfMatrix Matrix, double Width)>();
            var points = new List<(double X, double Y)>();
            foreach (COperator op in ContentReader.ReadContent(page).OfType<COperator>())
            {
                double Number(int i) => double.Parse(op.Operands[i].ToString(), CultureInfo.InvariantCulture);
                string name = op.OpCode.Name;
                if (name == "q") state.Push((transform, width));
                if (name == "Q") (transform, width) = state.Pop();
                if (name == "cm") transform = transform.Multiply(new(Number(0), Number(1), Number(2), Number(3), Number(4), Number(5)));
                if (name == "w") width = Math.Round(Number(0), 2);
                if (name == "m") points = [transform.Point(Number(0), Number(1))];
                if (name == "l") points.Add(transform.Point(Number(0), Number(1)));
                if (name == "S") { if (points.Count > 0) strokes.Add(new(points.ToList(), pageIndex + 1, width)); points.Clear(); }
                if (name == "BT") { x = 0; y = 0; textMatrix = PdfMatrix.Identity; }
                if (name is "Td" or "TD")
                { x += textMatrix.A * Number(0) + textMatrix.C * Number(1); y += textMatrix.B * Number(0) + textMatrix.D * Number(1); }
                if (name == "Tm")
                { textMatrix = new(Number(0), Number(1), Number(2), Number(3), Number(4), Number(5)); x = Number(4); y = Number(5); }
                if (name == "Tf") { selectedFont = ((CName)op.Operands[0]).Name; size = Number(1); }
                if (name != "Tj" || op.Operands[0] is not CString value) continue;
                var text = new StringBuilder();
                if (maps.TryGetValue(selectedFont, out var glyphs))
                {
                    for (int i = 0; i + 1 < value.Value.Length; i += 2)
                        if (glyphs.TryGetValue((value.Value[i] << 8) | value.Value[i + 1], out string? character)) text.Append(character);
                }
                else text.Append(value.Value);
                var origin = transform.Point(x, y);
                PdfMatrix direction = transform.Multiply(textMatrix);
                // PDF の文字の大きさ：行列の縦横の幾何平均（PyMuPDF と同じ）
                double scale = Math.Sqrt(Math.Abs(direction.A * direction.D - direction.B * direction.C));
                runs.Add(new(text.ToString(), origin.X, origin.Y, pageIndex + 1,
                    Math.Atan2(direction.B, direction.A) * 180 / Math.PI, Math.Round(size * scale, 2)));
            }
        }
        return new(pdf.PageCount, runs, strokes);
    }
}

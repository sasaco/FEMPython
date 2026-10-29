using Newtonsoft.Json.Linq;
using Xunit;
using static FrameWebforCS.Tests.CanonicalSectionDiagramTests;

namespace FrameWebforCS.Tests;

/// <summary>FrameG's 断面力図 layout (原宿ボックス 0404, 見本_断面力図.pdf): header, scale, labels, sides.</summary>
public sealed class FrameGSectionDiagramTests
{
    private const double A4Height = 842;

    private static JObject FrameGJob(string language = "ja")
    {
        JObject root = Job();
        root["language"] = language;
        var settings = (JObject)root["PrintDiagram"]!["diagramResult"]!;
        settings["yUp"] = false;
        return root;
    }

    [Fact]
    public void PagesCarryFrameGHeaderInsteadOfTheTextPageTitle()
    {
        var pdf = Render(FrameGJob());
        Assert.Equal(2, pdf.Pages);
        Assert.DoesNotContain(pdf.Runs, run => run.Text.StartsWith("TITLE :"));
        foreach (int page in new[] { 1, 2 })
        {
            Run title = Assert.Single(pdf.Runs, run => run.Page == page && run.Text == "Diagram Test");
            Assert.True(title.Blue);
            Assert.Equal(80.4, title.X, 1);
            Assert.Equal(A4Height - 89.2, title.Y, 1);
            Run caption = Assert.Single(pdf.Runs, run => run.Page == page && run.Text == " Case A: Service load");
            Assert.Equal(A4Height - 117.4, caption.Y, 1);
        }
        // Two panels per page, the second 339.2 pt below the first; every text is blue.
        Run moment = Assert.Single(pdf.Runs, run => run.Text == "　曲げモーメント図");
        Run shear = Assert.Single(pdf.Runs, run => run.Text == "　せん断力図");
        Assert.Equal(A4Height - 140.2, moment.Y, 1);
        Assert.Equal(339.2, moment.Y - shear.Y, 1);
        Assert.Contains(pdf.Runs, run => run.Page == 2 && run.Text == "　軸　力　図");
        Assert.Contains(pdf.Runs, run => run.Page == 2 && run.Text == "　変　位　図  (㎜)");
        Assert.All(pdf.Runs, run => Assert.True(run.Blue, run.Text));
    }

    [Fact]
    public void TableAndFrameGDiagramKeepSeparatePagesWithoutBlankOnes()
    {
        JObject root = FrameGJob(); root["hasPrintCalculation"] = true;
        var pdf = Render(root);
        Assert.Equal(4, pdf.Pages);
        Assert.Contains(pdf.Runs, run => run.Page == 1 && run.Text == "MEMBER FORCES");
        Assert.Contains(pdf.Runs, run => run.Page <= 2 && run.Text == "TITLE : Diagram Test");
        foreach (int page in new[] { 3, 4 })
        {
            Assert.DoesNotContain(pdf.Runs, run => run.Page == page && run.Text.StartsWith("TITLE :"));
            Assert.Single(pdf.Runs, run => run.Page == page && run.Text == "Diagram Test");
            Assert.Contains(pdf.Strokes, stroke => stroke.Page == page && stroke.Points.Count > 2);
        }
    }

    [Fact]
    public void DisplacementFrameIsClosedWithItsRigidPartsThicker()
    {
        // FrameG draws the whole undeflected frame under the displacement curve (原宿ボックス 0404, 4-33):
        // flexible parts 0.24 pt, rigid members and rigid zones 0.48 pt, so the frame does not break at them.
        JObject root = FrameGJob();
        var settings = (JObject)root["PrintDiagram"]!["diagramResult"]!;
        settings["rigidMembers"] = new JArray("pile/B");
        settings["rigidZones"] = new JObject { ["beam/A"] = new JArray(0.5, 0) };
        var pdf = Render(root);
        // The lower panel of page 2 (its top 135.3 + 339.2 pt from the top of the page) holds the displacement.
        var frame = pdf.Strokes.Where(stroke => stroke.Page == 2 && stroke.Points.Count == 2 && stroke.Width < .5 &&
            stroke.Points.All(point => point.Y < A4Height - 135.3 - 339.2)).ToList();
        double Length(Stroke stroke) => Math.Sqrt(Math.Pow(stroke.Points[1].X - stroke.Points[0].X, 2) +
            Math.Pow(stroke.Points[1].Y - stroke.Points[0].Y, 2));
        var thin = frame.Where(stroke => Math.Abs(stroke.Width - .24) < .001).ToList();
        var rigid = frame.Where(stroke => Math.Abs(stroke.Width - .48) < .001).ToList();
        // The beam's flexible part (1.5 m), its rigid zone (0.5 m) and the rigid pile (20 m), end to end.
        Assert.Single(thin);
        Assert.Equal(2, rigid.Count);
        double zone = rigid.Select(Length).Min(), pile = rigid.Select(Length).Max();
        Assert.Equal(Length(thin[0]) / 3, zone, 1);
        Assert.Equal(40 * zone, pile, 1);
        Assert.Contains(rigid, stroke => stroke.Points.Any(point =>
            Math.Abs(point.X - thin[0].Points[0].X) < .01 && Math.Abs(point.Y - thin[0].Points[0].Y) < .01));
    }

    [Fact]
    public void FrameUsesARoundScaleWithinItsShareOfThePanel()
    {
        // The 20 m pile limits the scale: 0.44 x 339.2 pt / 20 m is 1:380, rounded up to 1:400.
        var pdf = Render(FrameGJob());
        double expected = 20 * 1000 / 400.0 * 72 / 25.4;
        Assert.Contains(pdf.Strokes, stroke => stroke.Page == 1 && stroke.Points.Count == 2 &&
            Math.Abs(stroke.Points[0].X - stroke.Points[1].X) < 1e-6 &&
            Math.Abs(Math.Abs(stroke.Points[0].Y - stroke.Points[1].Y) - expected) < .01);
    }

    [Fact]
    public void ValuesAreLabelledOnlyAtMemberEndsAndTheInteriorExtreme()
    {
        JObject root = FrameGJob();
        root["PrintDiagram"]!["diagramResult"]!["output"] = new JArray("mz");
        // The beam has a station at 0.5 m with 7.00 (not an end, not an extreme): FrameG prints no value there.
        var beam = (JArray)root["fsec"]!["Case/A/Static/0"]!;
        beam.Insert(1, new JObject { ["m"] = "beam/A", ["n"] = "S0.5", ["l"] = .5, ["mz"] = 7, ["fx"] = 14,
            ["fy"] = 0, ["mx"] = 0, ["my"] = 0, ["fz"] = 0, ["dummy"] = false });
        // Zero end slopes keep the drawn moment between its stations, so 10.00 is the extreme.
        foreach (JObject row in beam.OfType<JObject>()) row["fy"] = 0;
        root["notice_points"] = JArray.Parse("""[{"m":"beam/A","Points":[0.5]}]""");
        var pdf = Render(root);
        Assert.DoesNotContain(pdf.Runs, run => run.Text == "7.00");
        Assert.Equal(2, pdf.Runs.Count(run => run.Text == "5.00" && Math.Abs(run.Angle) > 89));
        Assert.Contains(pdf.Runs, run => run.Text == "10.00");
        // A beam's values read upward across it.
        Assert.All(pdf.Runs.Where(run => run.Text is "5.00" or "10.00"), run => Assert.Equal(90, run.Angle, 1));
    }

    [Fact]
    public void AxialForceIsDrawnAsPlusNOnTheMemberNormal()
    {
        // FrameG draws -N on its y-up local +y side, which is +N on FrameWeb's y-down normal: a beam's
        // tension below it, wherever the closed frame's inside is.
        JObject root = FrameGJob();
        root["node"] = JObject.Parse("""{"node/A":{"x":0,"y":0},"node/B":{"x":2,"y":0},"node/C":{"x":2,"y":-20}}""");
        root["PrintDiagram"]!["diagramResult"]!["output"] = new JArray("fx");
        var pdf = Render(root);
        var beam = pdf.Strokes.Where(stroke => stroke.Page == 1 && stroke.Points.Count == 2 &&
            Math.Abs(stroke.Points[0].Y - stroke.Points[1].Y) < 1e-6 &&
            Math.Abs(stroke.Points[0].X - stroke.Points[1].X) > 10).Select(stroke => stroke.Points[0].Y).ToArray();
        Assert.True(beam.Length >= 1);
        double member = beam.Max();
        // Every beam row has fx = 2 x (5, 10, 5) > 0: the curve lies below the member (smaller PDF y).
        Assert.Contains(pdf.Strokes, stroke => stroke.Points.Count > 2 && stroke.Points.All(point => point.Y < member - 1));
    }

    [Fact]
    public void ChineseKeepsTheLayoutAndTranslatesTheTitles()
    {
        var pdf = Render(FrameGJob("cn"));
        Assert.Contains(pdf.Runs, run => run.Text == "　弯 矩 图");
        Assert.Contains(pdf.Runs, run => run.Text == "　轴 向 力 图");
        Run moment = Assert.Single(pdf.Runs, run => run.Text == "　弯 矩 图");
        Assert.Equal(A4Height - 140.2, moment.Y, 1);
    }

    [Fact]
    public void MissingCaptionNamesAreLeftOut()
    {
        JObject root = FrameGJob();
        ((JObject)root["PrintDiagram"]!["diagramResult"]!).Remove("caseLabels");
        root.Remove("fsecName");
        var pdf = Render(root);
        Assert.Contains(pdf.Runs, run => run.Text == " Case Case/A/Static/0");
    }

    [Fact]
    public void LandscapeAndA3UseTheSameRuleOnTheirOwnPaper()
    {
        foreach (var (size, orientation, height) in new[] { ("A4", "Horizontal", 595.0), ("A3", "Vertical", 1191.0) })
        {
            JObject root = FrameGJob(); root["pageSize"] = size; root["pageOrientation"] = orientation;
            root["PrintDiagram"]!["pageOrientation"] = orientation;
            var pdf = Render(root);
            Run moment = Assert.Single(pdf.Runs, run => run.Text == "　曲げモーメント図");
            Run shear = Assert.Single(pdf.Runs, run => run.Text == "　せん断力図");
            Assert.Equal(height - 140.2, moment.Y, 0);
            Assert.Equal((height - 135.3 - 28.3) / 2, moment.Y - shear.Y, 0);
        }
    }

    [Fact]
    public void LongTitleAndCaptionWrapWithoutMovingThePanels()
    {
        JObject root = FrameGJob();
        string title = string.Concat(Enumerable.Repeat("長い題名の帳票", 12));
        string caption = "Case A " + string.Concat(Enumerable.Repeat("非常に長い荷重の名称", 8)) + ":D1";
        root["title"] = title;
        root["PrintDiagram"]!["diagramResult"]!["caseLabels"]!["Case/A/Static/0"] = caption;
        var pdf = Render(root);
        Run[] titles = pdf.Runs.Where(run => run.Page == 1 && run.Y > A4Height - 105).OrderByDescending(run => run.Y).ToArray();
        Run[] captions = pdf.Runs.Where(run => run.Page == 1 && run.Y < A4Height - 110 && run.Y > A4Height - 135)
            .OrderByDescending(run => run.Y).ToArray();
        Assert.Equal(2, titles.Length);
        Assert.Equal(2, captions.Length);
        Assert.Equal(title, string.Concat(titles.Select(run => run.Text)));
        Assert.Equal(" " + caption, string.Concat(captions.Select(run => run.Text)));
        Assert.Equal(A4Height - 89.2, titles[0].Y, 1);
        Assert.Equal(A4Height - 117.4, captions[0].Y, 1);
        Assert.All(titles.Concat(captions), run => Assert.InRange(run.X, 80.3, 80.5));
        // The panels keep the one-line positions.
        Run moment = Assert.Single(pdf.Runs, run => run.Text == "　曲げモーメント図");
        Assert.Equal(A4Height - 140.2, moment.Y, 1);
    }

    [Fact]
    public void CaptionReachingThePanelsFailsClearly()
    {
        JObject root = FrameGJob();
        root["PrintDiagram"]!["diagramResult"]!["caseLabels"]!["Case/A/Static/0"] =
            "Case A " + string.Concat(Enumerable.Repeat("非常に長い荷重の名称", 20));
        var error = Assert.Throws<InvalidOperationException>(() => PDF_Manager.DirectPdfGenerator.Generate(root.ToString()));
        Assert.Contains("caption is too long", error.Message);
    }

    [Theory]
    [InlineData("fx", true)]
    [InlineData("mz", false)]
    [InlineData("fy", false)]
    public void EnvelopeEndsKeepTheGoverningSideAndAxialForceAlsoItsOppositeSign(string component, bool both)
    {
        // The sample's COMBINE 10 N prints 17.82 and -14.27 at the bottom slab's ends (見本 p.43); its M and S
        // print one side only (COMBINE 3, p.28: 17.31 at node 6, not the other side's 13.47).
        JObject root = FrameGJob();
        root.Remove("PrintDiagram"); root.Remove("fsec"); root.Remove("fsecName");
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        JArray Side(double value) => new(new[] { 0.0, 2.0 }.Select(position => new JObject
        {
            ["m"] = "beam/A", ["n"] = "S" + position, ["l"] = position, ["mz"] = 0, ["fy"] = 0, ["fx"] = 0,
            [component] = value,
        }));
        root["fsecCombine"] = new JObject { ["1"] = new JObject
            { [component + "_max"] = Side(5), [component + "_min"] = Side(-3) } };
        root["fsecCombineName"] = new JArray { new JArray("1", "Envelope") };
        var settings = new JObject { ["canonicalStations"] = true, ["yUp"] = false, ["layout"] = "single",
            ["output"] = new JArray(component), ["displacementLabelScale"] = 1000 };
        root["CombPrintDiagram"] = new JObject { ["diagramResult"] = settings };
        var pdf = Render(root);
        Assert.Equal(2, pdf.Runs.Count(run => run.Text == "5.00"));
        Assert.Equal(both ? 2 : 0, pdf.Runs.Count(run => run.Text is "-3.00" or "3.00"));
    }

    [Fact]
    public void OnlyABasicCaseSetsALongInteriorExtremeBetweenMemberAndCurve()
    {
        // 見本: case 1's 44.19 sits between the bottom slab and its curve; every COMBINE's slab extreme
        // (pp.24-44, e.g. COMBINE 2's 75.18) sits beyond the curve.
        JArray Beam() => new(new[] { (0.0, 0.0), (1.0, -10.0), (2.0, 0.0) }.Select(item => new JObject
        {
            ["m"] = "beam/A", ["n"] = "S" + item.Item1, ["l"] = item.Item1, ["mz"] = item.Item2, ["fy"] = 0, ["fx"] = 0,
        }));
        JObject Root()
        {
            JObject root = FrameGJob();
            ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
            root["PrintDiagram"]!["diagramResult"]!["output"] = new JArray("mz");
            return root;
        }
        JObject basic = Root();
        basic["fsec"]!["Case/A/Static/0"] = Beam();
        JObject envelope = Root();
        envelope.Remove("fsec"); envelope.Remove("fsecName");
        envelope["fsecCombine"] = new JObject { ["1"] = new JObject { ["mz_max"] = Beam(), ["mz_min"] = Beam() } };
        envelope["fsecCombineName"] = new JArray { new JArray("1", "Envelope") };
        envelope["CombPrintDiagram"] = new JObject { ["diagramResult"] = envelope["PrintDiagram"]!["diagramResult"]!.DeepClone() };
        envelope.Remove("PrintDiagram");
        foreach (var (root, between) in new[] { (basic, true), (envelope, false) })
        {
            var pdf = Render(root);
            // The negative moment is drawn above the beam (y down); the curve's peak is 10 mm above it.
            double peak = pdf.Strokes.Where(stroke => stroke.Points.Count > 2).SelectMany(stroke => stroke.Points).Max(point => point.Y);
            Run label = Assert.Single(pdf.Runs, run => run.Text == "10.00");
            if (between) Assert.True(label.Y < peak, "A basic case's long extreme sits below its curve's peak.");
            else Assert.True(label.Y > peak, "An envelope's extreme sits beyond its curve's peak.");
        }
    }
}

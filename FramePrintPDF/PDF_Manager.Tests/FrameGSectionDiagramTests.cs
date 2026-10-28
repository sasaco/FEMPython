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
}

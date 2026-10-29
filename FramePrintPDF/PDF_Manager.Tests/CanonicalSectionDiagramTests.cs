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

public sealed class CanonicalSectionDiagramTests
{
    [Fact]
    public void CanonicalBasicDiagramRendersCompositeKeysStationsAndEveryPageCaption()
    {
        JObject root = Job();
        string before = root.ToString();
        var pdf = Render(root);
        Assert.Equal(before, root.ToString());
        Assert.Equal(2, pdf.Pages);
        Assert.Contains("10.00", pdf.Text);
        Assert.Contains("1.00", pdf.Text); // Raw 0.001 m displacement is labelled in mm.
        Assert.Contains(pdf.Runs, run => run.Page == 1 && run.Text == "Bending moment");
        Assert.Contains(pdf.Runs, run => run.Page == 2 && run.Text == "Displacement  (mm)");
        Assert.DoesNotContain(pdf.Runs, run => run.Text.StartsWith("TITLE :"));
        foreach (int page in Enumerable.Range(1, pdf.Pages))
        {
            // FrameG's page header: the title alone, then the case.
            Run title = Assert.Single(pdf.Runs, r => r.Page == page && r.Text == "Diagram Test");
            Run caption = Assert.Single(pdf.Runs, r => r.Page == page && r.Text == " Case A: Service load");
            Assert.True(title.Y - caption.Y >= 12, "The case caption must be below the title without overlap.");
            Assert.Contains(pdf.Strokes, s => s.Page == page && s.Points.Count >= 3);
        }
        Assert.All(pdf.Runs, run => { Assert.InRange(run.X, 0, 596); Assert.InRange(run.Y, 0, 842); });
        Assert.Contains(pdf.Runs, run => run.Text == "10.00" && run.Blue);
        Assert.All(pdf.Strokes, stroke => Assert.False(stroke.Blue));
        Assert.DoesNotContain("NaN", pdf.Content);
        Assert.DoesNotContain("Infinity", pdf.Content);
    }

    [Theory]
    [InlineData("fsecCombine", "CombPrintDiagram")]
    [InlineData("fsecPickup", "PickPrintDiagram")]
    public void CanonicalEnvelopeRendersBothIndependentSidesWithoutChangingParsedRows(string family, string container)
    {
        JObject root = Job();
        root.Remove("PrintDiagram"); root.Remove("fsec"); root.Remove("fsecName");
        JArray minimum = Rows("beam/A", -3, -7, -3);
        JArray maximum = Rows("beam/A", 2, 4, 2);
        maximum.Add(new JObject { ["m"] = "beam/A", ["n"] = "S1", ["l"] = 1,
            ["mz"] = 999, ["fy"] = 999, ["fx"] = 999, ["occurrence"] = 2 });
        root[family] = new JObject { ["1"] = new JObject { ["mz_min"] = minimum, ["mz_max"] = maximum } };
        root[family + "Name"] = new JArray { new JArray("1", "Envelope") };
        JObject config = Config("mz");
        config["caseLabels"] = new JObject { ["1"] = "Envelope A" };
        root[container] = new JObject { ["diagramResult"] = config };
        var pdf = Render(root);
        Assert.Equal(1, pdf.Pages);
        Assert.Contains("7.00", pdf.Text); // Both curves remain; the governing ordinate is labelled.
        Assert.DoesNotContain("999", pdf.Text);
        Assert.Equal(2, pdf.Strokes.Count(s => s.Points.Count == 3));

        // Reuse the actual parsed renderer data, so mutation of minimum lists is observable.
        var data = new PrintData(root.ToObject<Dictionary<string, object>>()!);
        var child = new PrintData(JObject.FromObject(data.printScreens[container]).ToObject<Dictionary<string, object>>()!);
        var forces = (ResultFsecCombine)child.printDatas[family];
        int minCount = forces.Fsecs["1"].mz_min.Count, maxCount = forces.Fsecs["1"].mz_max.Count;
        int index = 1;
        using var document = new PDF_Manager.Printing.PdfDocument(data, ref index);
        var renderer = (DiagramResult)child.printDatas[DiagramResult.KEY];
        Assert.Null(Record.Exception(() => renderer.printPDF(document, child, ref index)));
        Assert.Equal(minCount, forces.Fsecs["1"].mz_min.Count);
        Assert.Equal(maxCount, forces.Fsecs["1"].mz_max.Count);
    }

    [Fact]
    public void CanonicalDiagramUsesOneScaleForBothAxesAndKeepsNegativeYBelowBeam()
    {
        JObject root = Job();
        ((JObject)root["PrintDiagram"]!)["diagramResult"] = Config("mz");
        var pdf = Render(root);
        Stroke pile = pdf.Strokes.Where(s => s.Points.Count == 2 && Math.Abs(s.Points[0].X - s.Points[1].X) < 0.001)
            .OrderByDescending(s => Math.Abs(s.Points[1].Y - s.Points[0].Y)).First();
        Stroke beam = pdf.Strokes.First(s => s.Points.Count == 2 && Math.Abs(s.Points[0].Y - s.Points[1].Y) < 0.001 &&
            Math.Abs(s.Points[1].X - pile.Points[0].X) < 0.001 && Math.Abs(s.Points[1].X - s.Points[0].X) > 10);
        Assert.True(pile.Points[0].Y > pile.Points[1].Y, "PDF coordinates must put the negative-Y pile end below its start.");
        Assert.Equal(beam.Points[1].Y, pile.Points[0].Y, 3);
        // FrameG draws the frame at one scale: the 20 m pile is ten times the 2 m beam.
        Assert.Equal(10, Math.Abs(pile.Points[1].Y - pile.Points[0].Y) / Math.Abs(beam.Points[1].X - beam.Points[0].X), 3);

        root["PrintDiagram"]!["diagramResult"]!["scaleX"] = 0.05; // 1:20, model metres.
        root["PrintDiagram"]!["diagramResult"]!["scaleY"] = 0.005; // 1:200.
        var scaled = Render(root);
        Assert.Contains(scaled.Strokes, s => s.Points.Count == 2 &&
            Math.Abs(Math.Abs(s.Points[1].X - s.Points[0].X) - 283.4646) < 0.05);
        Assert.Contains(scaled.Strokes, s => s.Points.Count == 2 &&
            Math.Abs(Math.Abs(s.Points[1].Y - s.Points[0].Y) - 283.4646) < 0.05);
    }

    [Fact]
    public void CanonicalZeroDiagramHasFiniteGeometryAndNoZeroLabelFlood()
    {
        JObject root = Job();
        root["PrintDiagram"]!["diagramResult"] = Config("mz", "fy", "fx");
        foreach (JObject row in (JArray)root["fsec"]!["Case/A/Static/0"]!)
            foreach (string component in new[] { "mz", "fy", "fx" }) row[component] = 0;
        var pdf = Render(root);
        Assert.Equal(2, pdf.Pages);
        Assert.DoesNotContain("0.00", pdf.Text);
        Assert.DoesNotContain("NaN", pdf.Content); Assert.DoesNotContain("Infinity", pdf.Content);
        Assert.All(Enumerable.Range(1, pdf.Pages), page => Assert.Contains(pdf.Strokes, s => s.Page == page));
    }

    [Fact]
    public void CanonicalMomentUsesEndpointShearToDrawUniformLoadCurvatureAndLabelInteriorExtreme()
    {
        JObject root = Job();
        ((JObject)root["member"]!).Remove("pile/B");
        ((JObject)root["node"]!).Remove("node/C");
        root["PrintDiagram"]!["diagramResult"] = Config("mz");
        root["fsec"]!["Case/A/Static/0"] = new JArray(
            new JObject { ["m"] = "beam/A", ["n"] = "S0", ["l"] = 0, ["mz"] = 0, ["fy"] = -10, ["fx"] = 0 },
            new JObject { ["m"] = "beam/A", ["n"] = "S1", ["l"] = 2, ["mz"] = 0, ["fy"] = 10, ["fx"] = 0 });
        var pdf = Render(root);
        Assert.Contains("5.00", pdf.Text); // M(x)=-10*x+5*x*x: interior x=1 gives -5, both ends give zero.
        Stroke curve = Assert.Single(pdf.Strokes.Where(s => s.Points.Count > 10));
        Assert.Equal(curve.Points[0].Y, curve.Points[^1].Y, 3);
        Assert.True(curve.Points.Min(p => p.Y) < curve.Points[0].Y - 10);
        Assert.All(curve.Points, point => Assert.InRange(point.X, curve.Points[0].X, curve.Points[^1].X));
    }

    [Fact]
    public void CanonicalForceSidesAndMagnitudeLabelsFollowReferenceWithoutChangingNumbers()
    {
        JObject root = Job();
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        root["PrintDiagram"]!["diagramResult"] = Config("mz", "fy", "fx");
        var rows = Rows("beam/A", 5, 5, 5);
        foreach (JObject row in rows) { row["fy"] = 7; row["fx"] = -9; }
        root["fsec"]!["Case/A/Static/0"] = rows;
        var pdf = Render(root);
        Assert.Contains("5.00", pdf.Text); Assert.DoesNotContain("-5.00", pdf.Text);
        Assert.Contains("7.00", pdf.Text); Assert.DoesNotContain("-7.00", pdf.Text);
        Assert.Contains("-9.00", pdf.Text);
        Stroke[] frames = pdf.Strokes.Where(s => s.Points.Count == 2 &&
            Math.Abs(s.Points[0].Y - s.Points[1].Y) < .001 && Math.Abs(s.Points[1].X - s.Points[0].X) > 150).ToArray();
        Stroke[] curves = pdf.Strokes.Where(s => s.Points.Count >= 3).ToArray();
        Assert.Equal(3, frames.Length); Assert.Equal(3, curves.Length);
        Assert.True(curves[0].Points[0].Y > frames[0].Points[0].Y); // Positive moment above beam.
        Assert.True(curves[1].Points[0].Y < frames[1].Points[0].Y); // Positive shear below beam.
        // FrameG draws -N on its y-up local +y side, which is +N on FrameWeb's member normal: the
        // compression of this left-to-right beam lies on the side opposite its positive moment.
        Assert.True(curves[2].Points[0].Y < frames[2].Points[0].Y);
        Assert.All(rows.Cast<JObject>(), row => Assert.Equal(5, (double)row["mz"]!));
    }

    [Fact]
    public void CanonicalMomentLabelsTheExactExtremeBetweenDrawingSamples()
    {
        JObject root = Job();
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        root["PrintDiagram"]!["diagramResult"] = Config("mz");
        root["fsec"]!["Case/A/Static/0"] = new JArray(
            new JObject { ["m"] = "beam/A", ["n"] = "S0", ["l"] = 0, ["mz"] = 0, ["fy"] = 10, ["fx"] = 0 },
            new JObject { ["m"] = "beam/A", ["n"] = "S1", ["l"] = 2, ["mz"] = 4, ["fy"] = -6, ["fx"] = 0 });
        var pdf = Render(root);
        Assert.Contains("6.25", pdf.Text); // M(x)=10*x-4*x*x has its exact maximum at x=1.25.
        Assert.DoesNotContain("6.24", pdf.Text);
    }

    [Fact]
    public void CanonicalMovingDisplacementDrawsRawCorrelatedEnvelopeModes()
    {
        JObject root = Job();
        root["PrintDiagram"]!["diagramResult"] = Config("disg");
        root["fsec"]!["Case/A/Static/0"] = new JObject
        { ["mz_min"] = Rows("beam/A", -1, -2, -1), ["mz_max"] = Rows("beam/A", 1, 2, 1) };
        JObject Mode(double value, string source) => new(new[] { "node/A", "node/B", "node/C" }.Select(id =>
            new JProperty(id, new JObject { ["id"] = id, ["case"] = source, ["dx"] = value, ["dy"] = 0,
                ["dz"] = 0, ["rx"] = 0, ["ry"] = 0, ["rz"] = 0 })));
        root["PrintDiagram"]!["disg"]!["Case/A/Static/0"] = new JObject
        { ["dx_max"] = Mode(.003, "vehicle/A"), ["dx_min"] = Mode(-.002, "vehicle/B") };
        foreach (JObject row in root["disg"]!["Case/A/Static/0"]!) row["dx"] = 9000;
        string before = root.ToString();
        var pdf = Render(root);
        Assert.Equal(1, pdf.Pages);
        Assert.Contains("3.00", pdf.Text); Assert.Contains("2.00", pdf.Text);
        Assert.DoesNotContain("9000", pdf.Text);
        Assert.Equal(4, pdf.Strokes.Count(stroke => stroke.Points.Count >= 3));
        Assert.Equal(before, root.ToString());
    }

    [Fact]
    public void CanonicalMixedTableAndDiagramStartsOnTheNextPageWithoutBlankPages()
    {
        JObject root = Job(); root["hasPrintCalculation"] = true;
        root.Remove("disg"); root.Remove("disgName"); // The diagram retains its own raw displacement geometry.
        var pdf = Render(root);
        Assert.Equal(3, pdf.Pages);
        Assert.Contains(pdf.Runs, run => run.Page == 1 && run.Text == "MEMBER FORCES");
        Assert.DoesNotContain(pdf.Runs, run => run.Page == 1 && run.Text == " Case A: Service load");
        foreach (int page in new[] { 2, 3 })
        {
            Assert.DoesNotContain(pdf.Runs, run => run.Page == page && run.Text.StartsWith("TITLE :"));
            Assert.Single(pdf.Runs, run => run.Page == page && run.Text == "Diagram Test");
            Assert.Single(pdf.Runs, run => run.Page == page && run.Text == " Case A: Service load");
            Assert.Contains(pdf.Strokes, stroke => stroke.Page == page && stroke.Points.Count > 2);
        }
    }

    [Fact]
    public void CanonicalLandscapeExplicitScaleUsesTheDeclaredModelLengthUnit()
    {
        JObject metres = Job(); metres["pageOrientation"] = "Horizontal";
        metres["PrintDiagram"]!["diagramResult"] = Config("fx");
        metres["PrintDiagram"]!["diagramResult"]!["scaleX"] = .05;
        metres["PrintDiagram"]!["diagramResult"]!["scaleY"] = .005;
        JObject millimetres = (JObject)metres.DeepClone();
        millimetres["PrintDiagram"]!["diagramResult"]!["displacementLabelScale"] = 1;
        foreach (JObject node in ((JObject)millimetres["node"]!).Properties().Select(p => p.Value))
        { node["x"] = (double)node["x"]! * 1000; node["y"] = (double)node["y"]! * 1000; }
        foreach (JObject row in millimetres["fsec"]!["Case/A/Static/0"]!) row["l"] = (double)row["l"]! * 1000;
        var first = Render(metres); var second = Render(millimetres);
        Assert.Equal(1, first.Pages); Assert.Equal(1, second.Pages);
        Assert.Equal(first.Strokes.Take(2).SelectMany(s => s.Points), second.Strokes.Take(2).SelectMany(s => s.Points));
        Assert.All(second.Runs, run => { Assert.InRange(run.X, 0, 842); Assert.InRange(run.Y, 0, 596); });
    }

    [Fact]
    public void UnflaggedLegacyDiagramKeepsItsExistingNodeAndContinuationContract()
    {
        JObject root = Job();
        root["PrintDiagram"]!["diagramResult"] = new JObject { ["layout"] = "single", ["output"] = new JArray("mz") };
        root["fsec"] = new JObject { ["1"] = new JArray(
            new JObject { ["m"] = "beam/A", ["n"] = "node/A", ["l"] = 0, ["mz"] = 5 },
            new JObject { ["m"] = "", ["n"] = "", ["l"] = 1, ["mz"] = 10 },
            new JObject { ["m"] = "", ["n"] = "node/B", ["l"] = 2, ["mz"] = 5 }) };
        root["fsecName"] = new JArray { new JArray("1", "Legacy") };
        var pdf = Render(root);
        Assert.Contains("5.00", pdf.Text); Assert.Contains("10.00", pdf.Text);
        Assert.Contains(pdf.Strokes, stroke => stroke.Blue);
    }

    [Fact]
    public void CanonicalCompressionOnDownwardPileFollowsTheMemberNormalAndKeepsNegativeLabels()
    {
        JObject root = Job();
        ((JObject)root["member"]!).Remove("beam/A"); ((JObject)root["node"]!).Remove("node/A");
        root["PrintDiagram"]!["diagramResult"] = Config("fx");
        JArray rows = Rows("pile/B", -9, -9, -9);
        foreach (JObject row in rows) { row["l"] = (double)row["l"]! * 10; row["fx"] = -9; }
        root["fsec"]!["Case/A/Static/0"] = rows;
        var pdf = Render(root);
        Stroke frame = pdf.Strokes.First(stroke => stroke.Points.Count == 2);
        Stroke curve = Assert.Single(pdf.Strokes.Where(stroke => stroke.Points.Count >= 3));
        // +N on FrameWeb's normal: a pile drawn downward (y up) has its normal to the right, so its
        // compression lies to the left.
        Assert.All(curve.Points, point => Assert.True(point.X < frame.Points[0].X));
        Assert.Contains("-9.00", pdf.Text);
    }

    [Fact]
    public void CanonicalEnvelopeLabelsGoverningStationsAndUsesMemberNormalOrientation()
    {
        JObject root = Job(); root.Remove("PrintDiagram"); root.Remove("fsec"); root.Remove("fsecName");
        JArray minimum = Rows("beam/A", -3, -7, -3), maximum = Rows("beam/A", 2, 4, 2);
        foreach (JObject row in Rows("pile/B", -.2, -.4, -.2))
        { row["l"] = (double)row["l"]! * 10; minimum.Add(row); }
        foreach (JObject row in Rows("pile/B", .1, .3, .1))
        { row["l"] = (double)row["l"]! * 10; maximum.Add(row); }
        root["fsecPickup"] = new JObject { ["1"] = new JObject { ["mz_min"] = minimum, ["mz_max"] = maximum } };
        root["fsecPickupName"] = new JArray { new JArray("1", "Envelope") };
        root["PickPrintDiagram"] = new JObject { ["diagramResult"] = Config("mz") };
        string before = root.ToString();
        var pdf = Render(root);
        Assert.Equal(before, root.ToString());
        Assert.Equal(4, pdf.Strokes.Count(stroke => stroke.Points.Count == 3));
        Assert.Contains("7.00", pdf.Text); Assert.Contains("0.40", pdf.Text);
        Assert.DoesNotContain("4.00", pdf.Text); Assert.DoesNotContain("0.30", pdf.Text);
        Assert.All(pdf.Runs.Where(run => run.Text is "3.00" or "7.00"), run => Assert.InRange(Math.Abs(run.Angle), 89.9, 90.1));
        Assert.All(pdf.Runs.Where(run => run.Text is "0.20" or "0.40"), run => Assert.InRange(Math.Abs(run.Angle), 0, .1));
        Assert.All(pdf.Runs, run => { Assert.InRange(run.X, 0, 596); Assert.InRange(run.Y, 0, 842); });
    }

    [Fact]
    public void CanonicalLabelsTrueSignedExtremaWithoutRoundoffOscillationFlood()
    {
        JObject root = Job();
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        root["node"]!["node/B"]!["x"] = 5;
        root["PrintDiagram"]!["diagramResult"] = Config("mz");
        JArray rows = Rows("beam/A", 0, 10, 9.999999999, 10, -.04, 0);
        foreach (JObject row in rows) row["fy"] = 0;
        root["fsec"]!["Case/A/Static/0"] = rows;
        var pdf = Render(root);
        Assert.Single(pdf.Runs, run => run.Text == "10.00");
        // FrameG labels one interior extreme per member, the larger one; the small opposite one is not printed.
        Assert.DoesNotContain(pdf.Runs, run => run.Text is "0.04" or "-0.04" or "9.99");
    }

    [Fact]
    public void CanonicalTwoResultTablesAndDiagramHaveFourSeparateNonblankPages()
    {
        JObject root = Job(); root["hasPrintCalculation"] = true;
        var pdf = Render(root);
        Assert.Equal(4, pdf.Pages);
        // FrameG's order (原宿ボックス 0404): member forces before displacements.
        Assert.Contains(pdf.Runs, run => run.Page == 1 && run.Text == "MEMBER FORCES");
        Assert.Contains(pdf.Runs, run => run.Page == 2 && run.Text == "X-DISP.");
        Assert.DoesNotContain(pdf.Runs, run => run.Page <= 2 && run.Text == " Case A: Service load");
        foreach (int page in new[] { 3, 4 })
        {
            Assert.DoesNotContain(pdf.Runs, run => run.Page == page && run.Text.StartsWith("TITLE :"));
            Assert.Single(pdf.Runs, run => run.Page == page && run.Text == "Diagram Test");
            Assert.Single(pdf.Runs, run => run.Page == page && run.Text == " Case A: Service load");
            Assert.Contains(pdf.Strokes, stroke => stroke.Page == page && stroke.Points.Count > 2);
        }
    }

    [Fact]
    public void CanonicalLocalOneSidedForceRowsKeepPointLoadMomentCornerAndRootRowsUntouched()
    {
        JObject root = Job();
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        root["PrintDiagram"]!["diagramResult"] = Config("mz");
        JObject Row(double position, double moment, double shear) => new()
        { ["m"] = "beam/A", ["n"] = "S" + position, ["l"] = position, ["mz"] = moment, ["fy"] = shear };
        root["PrintDiagram"]!["fsec"] = new JObject { ["Case/A/Static/0"] = new JArray(
            Row(0, 0, -10), Row(1, -10, -10), Row(1, -10, 10), Row(2, 0, 10)) };
        string before = root.ToString();
        var pdf = Render(root);
        Assert.Equal(before, root.ToString()); Assert.Contains("10.00", pdf.Text);
        Stroke curve = Assert.Single(pdf.Strokes.Where(stroke => stroke.Points.Count > 10));
        double left = curve.Points[0].X, right = curve.Points[^1].X, baseline = curve.Points[0].Y;
        double amplitude = baseline - curve.Points.Min(point => point.Y);
        foreach (var point in curve.Points)
        {
            double fraction = (point.X - left) / (right - left);
            double triangle = 1 - Math.Abs(2 * fraction - 1);
            Assert.InRange(Math.Abs(point.Y - (baseline - amplitude * triangle)), 0, .02);
        }
    }

    [Fact]
    public void CanonicalDisplacementUsesSolvedInteriorStationsAndNodeSelectionOnlyRestrictsLabels()
    {
        JObject root = Job();
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        root["PrintDiagram"]!["diagramResult"] = Config("disg");
        root["PrintDiagram"]!["diagramResult"]!["displacementStations"] = new JObject
        { ["beam/A"] = new JArray(
            new JObject { ["nodeId"] = "node/A", ["position"] = 0 },
            // Between FrameG's 0.4 m bends (0.8 and 1.2 m): the curve still passes through the solved station.
            new JObject { ["nodeId"] = "opaque interior ID", ["position"] = 1.0 },
            new JObject { ["nodeId"] = "node/B", ["position"] = 2 }) };
        JArray displacements = new(new[] { ("node/A", 0.001), ("opaque interior ID", -.004), ("node/B", .002) }
            .Select(pair => new JObject { ["id"] = pair.Item1, ["dx"] = 0, ["dy"] = pair.Item2,
                ["dz"] = 0, ["rx"] = 0, ["ry"] = 0, ["rz"] = 0 }));
        root["PrintDiagram"]!["disg"]!["Case/A/Static/0"] = displacements;
        var complete = Render(root);
        Assert.Contains("4.00", complete.Text); // The solved interior node governs over the original endpoints.
        Stroke frame = complete.Strokes.First(stroke => stroke.Points.Count == 2);
        // It is the largest displacement, drawn 10 mm (28.35 pt) from the member.
        Assert.Contains(complete.Strokes.SelectMany(stroke => stroke.Points), point =>
            Math.Abs(point.X - (frame.Points[0].X + .5 * (frame.Points[1].X - frame.Points[0].X))) < .02 &&
            Math.Abs(point.Y - (frame.Points[0].Y - 72 / 2.54)) < .02);
        root["PrintDiagram"]!["diagramResult"]!["displacementLabelNodeIds"] = new JArray("node/A");
        var selected = Render(root);
        Assert.Equal(complete.Strokes.SelectMany(stroke => stroke.Points), selected.Strokes.SelectMany(stroke => stroke.Points));
        Assert.Contains("1.00", selected.Text); Assert.DoesNotContain("4.00", selected.Text); Assert.DoesNotContain("2.00", selected.Text);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CanonicalDerivedMomentCurvesUseOnlyMatchingCorrelatedSourceSlopes(bool sourceChanges, bool combinationChanges)
    {
        JObject root = Job(); root.Remove("PrintDiagram"); root.Remove("fsec"); root.Remove("fsecName");
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        JObject Row(double position, double shear, string source, string combination) => new()
        { ["m"] = "beam/A", ["n"] = "S" + position, ["l"] = position, ["mz"] = 0, ["fy"] = shear,
            ["case"] = source, ["comb"] = combination };
        root["fsecPickup"] = new JObject { ["1"] = new JObject { ["mz_max"] = new JArray(
            Row(0, 10, "source/A", "1.2*dead + live"),
            Row(2, -10, sourceChanges ? "source/B" : "source/A", combinationChanges ? "dead + live" : "1.2*dead + live")) } };
        root["fsecPickupName"] = new JArray { new JArray("1", "Envelope") };
        root["PickPrintDiagram"] = new JObject { ["diagramResult"] = Config("mz") };
        var pdf = Render(root);
        if (sourceChanges || combinationChanges)
        { Assert.DoesNotContain("5.00", pdf.Text); Assert.DoesNotContain(pdf.Strokes, stroke => stroke.Points.Count > 2); }
        else
        {
            Assert.Contains("5.00", pdf.Text);
            Stroke curve = Assert.Single(pdf.Strokes.Where(stroke => stroke.Points.Count > 10));
            Assert.All(curve.Points, point => Assert.InRange(point.X, curve.Points[0].X, curve.Points[^1].X));
        }
    }

    [Fact]
    public void CanonicalDerivedMomentRejectsInconsistentOneSidedSourceSlopes()
    {
        JObject root = Job(); root.Remove("PrintDiagram"); root.Remove("fsec"); root.Remove("fsecName");
        ((JObject)root["member"]!).Remove("pile/B"); ((JObject)root["node"]!).Remove("node/C");
        root["node"]!["node/B"]!["x"] = 1;
        JObject Row(double position, double moment, double shear) => new()
        { ["m"] = "beam/A", ["n"] = "S" + position, ["l"] = position, ["mz"] = moment, ["fy"] = shear,
            ["case"] = "source/A", ["comb"] = "1.2*dead + live" };
        root["fsecPickup"] = new JObject { ["1"] = new JObject { ["mz_max"] = new JArray(
            Row(0, 10, 10), Row(1, 0, -10)) } };
        root["fsecPickupName"] = new JArray { new JArray("1", "Envelope") };
        root["PickPrintDiagram"] = new JObject { ["diagramResult"] = Config("mz") };
        string before = root.ToString();
        var pdf = Render(root);
        Assert.Equal(before, root.ToString());
        Assert.Contains("10.00", pdf.Text);
        Assert.DoesNotContain(pdf.Strokes, stroke => stroke.Points.Count > 2);
        Assert.Contains(pdf.Strokes, stroke => stroke.Points.Count == 2 &&
            Math.Abs(stroke.Points[0].X - stroke.Points[1].X) > 100 &&
            Math.Abs(stroke.Points[0].Y - stroke.Points[1].Y) > 10);
    }

    [Fact]
    public void CanonicalDisplacementStationMapSelectsMembersAndRejectsMalformedSelectedGeometry()
    {
        JObject root = Job(); root["PrintDiagram"]!["diagramResult"] = Config("disg");
        var stations = new JObject { ["beam/A"] = new JArray(
            new JObject { ["nodeId"] = "node/A", ["position"] = 0 },
            new JObject { ["nodeId"] = "node/B", ["position"] = 2 }) };
        root["PrintDiagram"]!["diagramResult"]!["displacementStations"] = stations;
        var pdf = Render(root);
        Assert.Single(pdf.Strokes.Where(stroke => stroke.Points.Count > 2));
        Assert.Contains(pdf.Strokes, stroke => stroke.Points.Count == 2 &&
            Math.Abs(stroke.Points[0].Y - stroke.Points[1].Y) > 200); // The unselected pile remains context geometry.
        ((JArray)stations["beam/A"]!).RemoveAt(1);
        Assert.Throws<InvalidOperationException>(() => DirectPdfGenerator.Generate(root.ToString()));
        stations.RemoveAll();
        Assert.Throws<InvalidOperationException>(() => DirectPdfGenerator.Generate(root.ToString()));
        stations["unknown member"] = new JArray(
            new JObject { ["nodeId"] = "node/A", ["position"] = 0 },
            new JObject { ["nodeId"] = "node/B", ["position"] = 2 });
        Assert.Throws<InvalidOperationException>(() => DirectPdfGenerator.Generate(root.ToString()));
    }

    [Fact]
    public void CanonicalBasicCaseLabelsEveryMemberEndWhileAnEnvelopePrintsAnEqualValueOnceAtOnePlace()
    {
        JObject root = Job(); root["PrintDiagram"]!["diagramResult"] = Config("fx");
        // Fixed display scales make the short adjacent member ends 5 points apart.
        root["PrintDiagram"]!["diagramResult"]!["scaleX"] = 100 * 25.4 / (1000 * 72);
        root["PrintDiagram"]!["diagramResult"]!["scaleY"] = 50 * 25.4 / (1000 * 72);
        root["node"] = JObject.Parse("""
            {"A":{"x":0,"y":0},"B":{"x":0,"y":-0.1},"C":{"x":0,"y":-10},
             "D":{"x":4,"y":0},"E":{"x":4,"y":-10},
             "F":{"x":0.1,"y":0},"G":{"x":0.1,"y":-0.1},
             "H":{"x":0.42,"y":-0.84},"I":{"x":0.92,"y":-0.84}}
            """);
        root["member"] = JObject.Parse("""
            {"short":{"ni":"A","nj":"B","e":"1"},"main":{"ni":"B","nj":"C","e":"1"},
             "far":{"ni":"D","nj":"E","e":"1"},"unequal":{"ni":"F","nj":"G","e":"1"},
             "rotated":{"ni":"H","nj":"I","e":"1"}}
            """);
        var rows = new JArray();
        foreach (var item in new[] { ("short", .1, -95.78), ("main", 9.9, -95.78),
            ("far", 10d, -95.78), ("unequal", .1, -95.79), ("rotated", .5, -95.78) })
        foreach (double position in new[] { 0, item.Item2 })
            rows.Add(new JObject { ["m"] = item.Item1, ["n"] = "S" + position,
                ["l"] = position, ["fx"] = item.Item3, ["fy"] = 0, ["mz"] = 0 });
        root["fsec"]!["Case/A/Static/0"] = rows;
        string before = root.ToString();
        var pdf = Render(root);
        Assert.Equal(before, root.ToString());
        Assert.Equal(20, pdf.Strokes.Count); // All five frames, curves, and both end ordinates survive.
        // A basic case labels both ends of every member, so the shared node B carries the short and the
        // main member's equal values (FrameG's sample prints 17.50 twice at a wall node).
        Assert.Equal(6, pdf.Runs.Count(run => run.Text == "-95.78" && Math.Abs(run.Angle) < .1));
        Assert.Equal(2, pdf.Runs.Count(run => run.Text == "-95.78" && Math.Abs(run.Angle) > 89.9));
        Assert.Equal(2, pdf.Runs.Count(run => run.Text == "-95.79"));

        // An envelope prints an equal value once where labels would overlap (the 5-point short members'
        // ends and the shared node B collapse to one), keeping distant, unequal and rotated ones.
        root.Remove("fsec"); root.Remove("fsecName");
        root["fsecCombine"] = new JObject { ["1"] = new JObject { ["fx_max"] = rows, ["fx_min"] = rows.DeepClone() } };
        root["fsecCombineName"] = new JArray { new JArray("1", "Envelope") };
        root["CombPrintDiagram"] = new JObject { ["diagramResult"] = root["PrintDiagram"]!["diagramResult"]!.DeepClone() };
        root.Remove("PrintDiagram");
        var envelope = Render(root);
        Assert.Equal(4, envelope.Runs.Count(run => run.Text == "-95.78" && Math.Abs(run.Angle) < .1));
        Assert.Equal(2, envelope.Runs.Count(run => run.Text == "-95.78" && Math.Abs(run.Angle) > 89.9));
        Assert.Single(envelope.Runs, run => run.Text == "-95.79");
    }

    private static JObject Config(params string[] outputs) => new()
    {
        ["canonicalStations"] = true, ["yUp"] = true,
        ["layout"] = "splitHorizontal", ["output"] = new JArray(outputs),
        ["displacementLabelScale"] = 1000,
        ["caseLabels"] = new JObject { ["Case/A/Static/0"] = "Case A: Service load" },
    };

    internal static JObject Job()
    {
        JArray forces = Rows("beam/A", 5, 10, 5);
        foreach (JObject row in Rows("pile/B", 2, 4, 2)) { row["l"] = (double)row["l"]! * 10; forces.Add(row); }
        var displacements = new JObject { ["Case/A/Static/0"] = new JArray(
            new[] { "node/A", "node/B", "node/C" }.Select(id => new JObject
            { ["id"] = id, ["dx"] = 0.001, ["dy"] = 0, ["dz"] = 0, ["rx"] = 0, ["ry"] = 0, ["rz"] = 0 })) };
        return new JObject
        {
            ["dimension"] = 2, ["language"] = "en", ["pageSize"] = "A4", ["pageOrientation"] = "Vertical",
            ["ver"] = "2.5.12", ["title"] = "Diagram Test",
            ["hasPrintInputData"] = false, ["hasPrintCalculation"] = false,
            ["node"] = JObject.Parse("""{"node/A":{"x":0,"y":0},"node/B":{"x":2,"y":0},"node/C":{"x":2,"y":-20}}"""),
            ["member"] = JObject.Parse("""{"beam/A":{"ni":"node/A","nj":"node/B","e":"1"},"pile/B":{"ni":"node/B","nj":"node/C","e":"1"}}"""),
            ["load"] = new JObject(), ["combine"] = new JObject(), ["pickup"] = new JObject(),
            ["fsec"] = new JObject { ["Case/A/Static/0"] = forces },
            ["fsecName"] = new JArray { new JArray("Case/A/Static/0", "Service load") },
            ["disg"] = displacements.DeepClone(),
            ["disgName"] = new JArray { new JArray("Case/A/Static/0", "Service load") },
            ["PrintDiagram"] = new JObject { ["diagramResult"] = Config("mz", "fy", "fx", "disg"),
                ["disg"] = displacements, ["disgName"] = new JArray { new JArray("Case/A/Static/0", "Service load") } },
        };
    }

    private static JArray Rows(string member, params double[] values) => new(values.Select((value, index) => new JObject
    {
        ["m"] = member, ["n"] = "S" + index, ["l"] = index, ["mz"] = value,
        ["fx"] = value * 2, ["fy"] = value * 3, ["mx"] = 0, ["my"] = 0, ["fz"] = 0, ["dummy"] = false,
    }));

    internal sealed record Run(string Text, double X, double Y, int Page, bool Blue, double Angle);
    internal sealed record Stroke(List<(double X, double Y)> Points, int Page, bool Blue, double Width = 0);
    internal sealed record PdfResult(int Pages, List<Run> Runs, List<Stroke> Strokes, string Content)
    { public string Text => string.Join("\n", Runs.Select(r => r.Text)); }
    internal sealed record PdfMatrix(double A, double B, double C, double D, double E, double F)
    {
        internal static PdfMatrix Identity => new(1, 0, 0, 1, 0, 0);
        internal (double X, double Y) Point(double x, double y) => (A * x + C * y + E, B * x + D * y + F);
        internal PdfMatrix Multiply(PdfMatrix right) => new(A * right.A + C * right.B, B * right.A + D * right.B,
            A * right.C + C * right.D, B * right.C + D * right.D,
            A * right.E + C * right.F + E, B * right.E + D * right.F + F);
    }

    internal static PdfResult Render(JObject root)
    {
        byte[] bytes = [];
        Assert.Null(Record.Exception(() => bytes = DirectPdfGenerator.Generate(root.ToString())));
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        var runs = new List<Run>(); var strokes = new List<Stroke>(); var content = new StringBuilder();
        for (int pageIndex = 0; pageIndex < pdf.PageCount; pageIndex++)
        {
            var page = pdf.Pages[pageIndex];
            var fonts = page.Resources.Elements.GetDictionary("/Font");
            var maps = new Dictionary<string, Dictionary<int, string>>();
            foreach (string font in fonts.Elements.Keys)
            {
                var cmap = fonts.Elements.GetDictionary(font).Elements.GetDictionary("/ToUnicode");
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
            string selectedFont = ""; double x = 0, y = 0, width = 1; bool fillBlue = false, strokeBlue = false;
            PdfMatrix transform = PdfMatrix.Identity, textMatrix = PdfMatrix.Identity;
            var state = new Stack<(PdfMatrix Matrix, bool FillBlue, bool StrokeBlue, double Width)>();
            var points = new List<(double X, double Y)>();
            foreach (COperator op in ContentReader.ReadContent(page).OfType<COperator>())
            {
                content.AppendLine(op.ToString());
                double Number(int i) => double.Parse(op.Operands[i].ToString(), CultureInfo.InvariantCulture);
                string name = op.OpCode.Name;
                if (name == "q") state.Push((transform, fillBlue, strokeBlue, width));
                if (name == "Q") (transform, fillBlue, strokeBlue, width) = state.Pop();
                if (name == "w") width = Number(0);
                if (name == "cm") transform = transform.Multiply(new(Number(0), Number(1), Number(2), Number(3), Number(4), Number(5)));
                if (name == "rg") fillBlue = Number(2) > 0.5 && Number(0) < 0.5;
                if (name == "RG") strokeBlue = Number(2) > 0.5 && Number(0) < 0.5;
                if (name == "m") points = [transform.Point(Number(0), Number(1))];
                if (name == "l") points.Add(transform.Point(Number(0), Number(1)));
                if (name == "S") { if (points.Count > 0) strokes.Add(new(points.ToList(), pageIndex + 1, strokeBlue, width)); points.Clear(); }
                if (name == "BT") { x = 0; y = 0; textMatrix = PdfMatrix.Identity; }
                if (name is "Td" or "TD")
                { x += textMatrix.A * Number(0) + textMatrix.C * Number(1); y += textMatrix.B * Number(0) + textMatrix.D * Number(1); }
                if (name == "Tm")
                { textMatrix = new(Number(0), Number(1), Number(2), Number(3), Number(4), Number(5)); x = Number(4); y = Number(5); }
                if (name == "Tf") selectedFont = ((CName)op.Operands[0]).Name;
                if (name != "Tj" || op.Operands[0] is not CString value) continue;
                var text = new StringBuilder();
                if (maps.TryGetValue(selectedFont, out var map))
                {
                    for (int i = 0; i + 1 < value.Value.Length; i += 2)
                        if (map.TryGetValue((value.Value[i] << 8) | value.Value[i + 1], out string? character)) text.Append(character);
                }
                else text.Append(value.Value);
                var origin = transform.Point(x, y);
                PdfMatrix direction = transform.Multiply(textMatrix);
                runs.Add(new(text.ToString(), origin.X, origin.Y, pageIndex + 1, fillBlue,
                    Math.Atan2(direction.B, direction.A) * 180 / Math.PI));
            }
        }
        return new(pdf.PageCount, runs, strokes, content.ToString());
    }
}

using FrameWebforCS.calculation;
using FrameWebforCS.providers.printing;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class TransverseDisplayProjectionTests
{
    [Theory]
    [InlineData("m", 2, 1000, -1)]
    [InlineData("mm", 2, 1, -1)]
    [InlineData("cm", 2, 10, -1)]
    [InlineData("unspecified", 2, 1000, -1)]
    [InlineData("m", 3, 1000, 1)]
    public void PhysicalBaseUsesDeclaredLengthAndReferenceFacingTwoDimensionalSigns(
        string length, int dimension, double scale, double sign)
    {
        JsonObject fixture = Fixture("single-static");
        fixture["units"]!["length"] = length;
        JsonNode result = fixture["results"]![0]!;
        // Analytic cantilever: P=3, L=2, EI=4000. Positive Y load,
        // right-handed positive canonical rotation, fixed-end Vy=-3/Mz=-6.
        result["node_displacements"]![1]!["components"]!["dy"] = 0.002;
        result["node_displacements"]![1]!["components"]!["rz"] = 0.0015;
        result["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["fy"] = -3;
        result["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["mz"] = -6;
        var presentation = Presentation(fixture, dimension);
        string before = fixture.ToJsonString();
        JsonObject root = Project(presentation, new([PrintOption.Displacement, PrintOption.SectionForce]));
        JsonNode displacement = root["disg"]!.AsObject().Single().Value![1]!;
        JsonNode force = root["fsec"]!.AsObject().Single().Value![0]!;
        Assert.Equal(sign * 0.002 * scale, displacement["dy"]!.GetValue<double>(), 12);
        Assert.Equal(1.5, displacement["rz"]!.GetValue<double>(), 12);
        Assert.Equal(sign * -3, force["fy"]!.GetValue<double>());
        Assert.Equal(-6, force["mz"]!.GetValue<double>());
        Assert.Equal(0.002, ((ForceAnalysisResult)presentation.Pages[0].Result)
            .NodeDisplacements[1].Components.Dy);
        Assert.Equal(0.0015, ((ForceAnalysisResult)presentation.Pages[0].Result)
            .NodeDisplacements[1].Components.Rz);
        Assert.Equal(before, fixture.ToJsonString());
    }

    [Fact]
    public void ModalShapeIsNotScaledOrSignReversed()
    {
        JsonObject fixture = Fixture("modal");
        fixture["results"]![0]!["node_mode_shapes"]![0]!["components"]!["rz"] = 0.5;
        JsonObject root = Project(Presentation(fixture, 2), new([PrintOption.Displacement]));
        JsonNode shape = root["disg"]!.AsObject().First().Value![0]!;
        Assert.Equal(1, shape["dx"]!.GetValue<double>());
        Assert.Equal(0.5, shape["rz"]!.GetValue<double>());
    }

    [Fact]
    public void UnknownPhysicalLengthCannotBeMislabeledAsMillimetres()
    {
        JsonObject fixture = Fixture("single-static");
        fixture["units"]!["length"] = "furlong";
        Assert.Throws<PrintProjectionException>(() => Project(Presentation(fixture, 2),
            new([PrintOption.Displacement])));
    }

    [Theory]
    [InlineData(2, "min", -1)]
    [InlineData(3, "max", 1)]
    public void DerivedDisplayFilterRemapsSignedModeAndKeepsFullRawVector(
        int dimension, string sourceMode, double sign)
    {
        var resultSet = AnalysisResultSetJson.Deserialize(Fixture("single-static").ToJsonString());
        string[] dnames = ["dx", "dy", "dz", "rx", "ry", "rz"];
        string[] fnames = ["fx", "fy", "fz", "mx", "my", "mz"];
        var displacement = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>();
        var forces = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>();
        foreach (string suffix in new[] { "max", "min" })
        {
            double factor = suffix == "max" ? 1 : -2;
            displacement["dy_" + suffix] = [new("2", null,
                dnames.Select((n, i) => (n, v: factor * (i + 1) * 0.001))
                    .ToDictionary(x => x.n, x => x.v), suffix, "full provenance " + suffix)];
            forces["fy_" + suffix] = [new("M1", "S0",
                fnames.Select((n, i) => (n, v: factor * (i + 1)))
                    .ToDictionary(x => x.n, x => x.v), suffix, "full provenance " + suffix)];
        }
        foreach (string suffix in new[] { "max", "min" })
        {
            CalculationDerivedRow node = displacement["dy_" + suffix][0];
            displacement["dy_" + suffix] = [node, node with { EntityId = "1" }];
            foreach (string component in dnames)
                displacement[component + "_" + suffix] = displacement["dy_" + suffix];
            foreach (string component in fnames)
                forces[component + "_" + suffix] = forces["fy_" + suffix];
        }
        var item = new CalculationDerivedCase("1", "Envelope", displacement,
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(), forces);
        var derived = new CalculationDerivedPresentation([], [item,
            new("max", null, displacement, item.Reactions, forces),
            new("min", null, displacement, item.Reactions, forces)], [item]);
        var presentation = new CalculationResultPresentation(resultSet, derived, dimension);
        string rawForceExport = PickupExportFormatter.Format(presentation);
        string rawNodeExport = PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Displacement);
        CalculationDerivedRow raw = displacement["dy_" + sourceMode][0];
        JsonObject root = Project(presentation, new(
            [PrintOption.CombinedDisplacement, PrintOption.PickupDisplacement,
             PrintOption.CombinedSectionForce, PrintOption.PickupSectionForce],
            DerivedIds: ["1"],
            DisplacementComponents: ["dy_max"], SectionForceComponents: ["fy_max"]));
        foreach (string key in new[] { "disgCombine", "disgPickup" })
        {
            Assert.Single(root[key]!["1"]!.AsObject());
            JsonNode row = root[key]!["1"]!["dy_max"]!["2"]!;
            Assert.Equal(sourceMode, row["case"]!.GetValue<string>());
            Assert.Equal(raw.Components["dx"] * 1000, row["dx"]!.GetValue<double>());
            Assert.Equal(sign * raw.Components["dy"] * 1000, row["dy"]!.GetValue<double>());
            Assert.Equal(raw.Provenance, row["comb"]!.GetValue<string>());
            foreach (string component in dnames)
                Assert.Equal(raw.Components[component] * 1000 *
                    (component == "dy" ? sign : 1), row[component]!.GetValue<double>(), 12);
        }
        foreach (string key in new[] { "fsecCombine", "fsecPickup" })
        {
            Assert.Single(root[key]!["1"]!.AsObject());
            JsonNode row = root[key]!["1"]!["fy_max"]![0]!;
            Assert.Equal(sourceMode, row["case"]!.GetValue<string>());
            Assert.Equal(forces["fy_" + sourceMode][0].Components["mz"], row["mz"]!.GetValue<double>());
            Assert.Equal(sign * forces["fy_" + sourceMode][0].Components["fy"], row["fy"]!.GetValue<double>());
            foreach (string component in fnames)
                Assert.Equal(forces["fy_" + sourceMode][0].Components[component] *
                    (component == "fy" ? sign : 1), row[component]!.GetValue<double>(), 12);
        }
        Assert.Same(raw, displacement["dy_" + sourceMode][0]);
        Assert.Equal(sourceMode == "max" ? 0.002 : -0.004, raw.Components["dy"], 12);
        Assert.Equal(rawForceExport, PickupExportFormatter.Format(presentation));
        Assert.Equal(rawNodeExport, PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Displacement));
    }

    private static JsonObject Project(CalculationResultPresentation presentation, PrintSelection selection)
    {
        string input = "{\"dimension\":" + presentation.Dimension +
            ",\"node\":{},\"member\":{},\"shell\":{},\"element\":{},\"load\":{}}";
        var snapshot = new PrintSnapshot(selection, input, 1, 1, presentation, presentation.Derived, []);
        return JsonNode.Parse(PrintProjection.Build(snapshot))!.AsObject();
    }

    [Theory]
    [InlineData(2, "L.2", 300, -30)]
    [InlineData(3, "L.1", 200, 20)]
    public void MovingEnvelopeSelectsDisplayedExtremeBeforeFormattingAndRetainsFirstTie(
        int dimension, string expectedCase, double expectedDx, double expectedMoment)
    {
        JsonObject fixture = Fixture("single-static");
        JsonNode originalCase = fixture["cases"]![0]!.DeepClone();
        JsonNode originalResult = fixture["results"]![0]!.DeepClone();
        var cases = new JsonArray();
        var results = new JsonArray();
        foreach (var (id, deflection, dx, moment) in new[]
        {
            ("L", 0.001, 0.1, 10.0), ("L.1", 0.004, 0.2, 20.0),
            ("L.2", -0.003, 0.3, -30.0), ("L.3", -0.003, 0.4, -40.0),
        })
        {
            JsonNode itemCase = originalCase.DeepClone();
            JsonNode itemResult = originalResult.DeepClone();
            itemCase["case_id"] = id;
            itemCase["symbol"] = "LL";
            itemResult["case_id"] = id;
            itemResult["node_displacements"]![1]!["components"]!["dx"] = dx;
            itemResult["node_displacements"]![1]!["components"]!["dy"] = deflection;
            itemResult["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["fy"] = deflection * 1000;
            itemResult["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["mz"] = moment;
            cases.Add(itemCase);
            results.Add(itemResult);
        }
        fixture["cases"] = cases;
        fixture["results"] = results;
        var presentation = Presentation(fixture, dimension);
        JsonObject root = Project(presentation, new([PrintOption.Displacement, PrintOption.SectionForce],
            DisplacementComponents: ["dy_max"], SectionForceComponents: ["fy_max"]));
        JsonObject dispEnvelope = root["disg"]!.AsObject().Single().Value!.AsObject();
        JsonObject forceEnvelope = root["fsec"]!.AsObject().Single().Value!.AsObject();
        Assert.Single(dispEnvelope);
        Assert.Single(forceEnvelope);
        JsonNode displacement = dispEnvelope["dy_max"]!["2"]!;
        JsonNode force = forceEnvelope["fy_max"]![0]!;
        Assert.Equal(expectedCase, displacement["case"]!.GetValue<string>());
        Assert.Equal(expectedDx, displacement["dx"]!.GetValue<double>());
        Assert.Equal(dimension == 2 ? 3 : 4, displacement["dy"]!.GetValue<double>());
        Assert.Equal(expectedCase, force["case"]!.GetValue<string>());
        Assert.Equal(expectedMoment, force["mz"]!.GetValue<double>());
        Assert.Equal(dimension == 2 ? 3 : 4, force["fy"]!.GetValue<double>());
        Assert.Equal(-0.003, presentation.Pages[0].MovingChildren[1]
            .NodeDisplacements[1].Components.Dy);
    }

    private static CalculationResultPresentation Presentation(JsonObject fixture, int dimension) =>
        new(AnalysisResultSetJson.Deserialize(fixture.ToJsonString()), dimension: dimension);

    private static JsonObject Fixture(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        return JsonNode.Parse(File.ReadAllText(Path.Combine(directory!.FullName, "FrameWeb", "tests",
            "data", "contracts", "positive", name + ".json")))!.AsObject();
    }
}

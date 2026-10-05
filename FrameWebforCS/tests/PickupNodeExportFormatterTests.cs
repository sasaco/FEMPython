using FrameWebforCS.calculation;
using System.Globalization;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class PickupNodeExportFormatterTests
{
    private static readonly string[] DisplacementNames = ["dx", "dy", "dz", "rx", "ry", "rz"];
    private static readonly string[] ReactionNames = ["fx", "fy", "fz", "mx", "my", "mz"];

    [Theory]
    [InlineData(2, 0, 6)]
    [InlineData(3, 0, 12)]
    [InlineData(2, 1, 3)]
    [InlineData(3, 1, 6)]
    public void ExportsFocusModesInTopologyOrderWithFullSelectedVectors(
        int dimension, int quantityValue, int expectedRows)
    {
        var quantity = (PickupNodeQuantity)quantityValue;
        CalculationResultPresentation presentation = Presentation(dimension);

        string[] lines = PickupNodeExportFormatter.Format(presentation, quantity).Split('\n');

        Assert.Equal(expectedRows + 2, lines.Length); // Header, data, final newline.
        string[] header = lines[0].Split(',');
        Assert.Equal(17, header.Length);
        Assert.Equal(["pickup_id", "focus_component", "node_id", "max_combine_id", "min_combine_id"],
            header[..5]);
        if (quantity == PickupNodeQuantity.Displacement)
        {
            Assert.Equal("max_dx (m)", header[5]);
            Assert.Equal("max_rz (rad)", header[10]);
            Assert.Equal("min_dz (m)", header[13]);
        }
        else
        {
            Assert.Equal("max_fx (kN)", header[5]);
            Assert.Equal("max_mz (kN*m)", header[10]);
            Assert.Equal("min_my (kN*m)", header[15]);
        }

        string[] first = lines[1].Split(',');
        Assert.Equal(17, first.Length);
        Assert.Equal("1", first[0]);
        Assert.Equal(quantity == PickupNodeQuantity.Displacement ? "dx" : "fx", first[1]);
        Assert.Equal("1", first[2]); // Fixture mode rows are intentionally reversed.
        Assert.Equal(["40", "41"], first[3..5]);
        Assert.Equal(0.001234567890123456,
            double.Parse(first[5], CultureInfo.InvariantCulture));
        Assert.Equal("6", first[10]);
        Assert.Equal("-1", first[11]);
        Assert.Equal("-6", first[16]);
        if (quantity == PickupNodeQuantity.Displacement)
            Assert.Equal("2", lines[2].Split(',')[2]);
        if (dimension == 2)
            Assert.Equal(quantity == PickupNodeQuantity.Displacement ? "rz" : "mz",
                lines[expectedRows - (quantity == PickupNodeQuantity.Displacement ? 1 : 0)].Split(',')[1]);
    }

    [Fact]
    public void RejectsMissingPairAndNonfiniteCorrelatedComponent()
    {
        CalculationResultPresentation presentation = Presentation(2);
        CalculationDerivedCase pickup = Assert.Single(presentation.Derived!.Pickups);
        var missing = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(pickup.Reactions)
        {
            ["fx_min"] = []
        };
        presentation.UpdateDerived(Derived(pickup.Displacements, missing));
        Assert.Throws<InvalidOperationException>(() =>
            PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Reaction));

        var invalid = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(pickup.Displacements);
        CalculationDerivedRow row = invalid["dx_max"][0];
        var components = new Dictionary<string, double>(row.Components) { ["rz"] = double.NaN };
        invalid["dx_max"] = [row with { Components = components }, invalid["dx_max"][1]];
        presentation.UpdateDerived(Derived(invalid, pickup.Reactions));
        Assert.Throws<InvalidOperationException>(() =>
            PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Displacement));
    }

    [Fact]
    public void RejectsReactionModesWithDifferentNodeSets()
    {
        CalculationResultPresentation presentation = Presentation(2, secondSupport: true);
        CalculationDerivedCase pickup = Assert.Single(presentation.Derived!.Pickups);
        var reaction = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(pickup.Reactions);
        reaction["fy_max"] = [reaction["fy_max"][1]];
        reaction["fy_min"] = [reaction["fy_min"][1]];
        presentation.UpdateDerived(Derived(pickup.Displacements, reaction));

        Assert.Throws<InvalidOperationException>(() =>
            PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Reaction));
    }

    [Theory]
    [InlineData("unknown-node")]
    [InlineData("duplicate-node")]
    [InlineData("station")]
    [InlineData("source")]
    [InlineData("component")]
    [InlineData("mode")]
    [InlineData("missing-node")]
    public void RejectsMalformedDisplacementData(string corruption)
    {
        CalculationResultPresentation presentation = Presentation(2);
        CalculationDerivedCase pickup = Assert.Single(presentation.Derived!.Pickups);
        var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(pickup.Displacements);
        CalculationDerivedRow row = modes["dx_max"][0];
        switch (corruption)
        {
            case "unknown-node":
                modes["dx_max"] = [row with { EntityId = "missing" }, modes["dx_max"][1]];
                break;
            case "duplicate-node":
                modes["dx_max"] = [row, row];
                break;
            case "station":
                modes["dx_max"] = [row with { StationId = "S0" }, modes["dx_max"][1]];
                break;
            case "source":
                modes["dx_max"] = [row with { SourceCaseId = "missing" }, modes["dx_max"][1]];
                break;
            case "component":
                var components = new Dictionary<string, double>(row.Components);
                components.Remove("rz");
                modes["dx_max"] = [row with { Components = components }, modes["dx_max"][1]];
                break;
            case "mode":
                modes.Remove("dx_min");
                break;
            case "missing-node":
                modes["dx_max"] = [modes["dx_max"][0]];
                modes["dx_min"] = [modes["dx_min"][0]];
                break;
        }
        presentation.UpdateDerived(Derived(modes, pickup.Reactions));

        Assert.Throws<InvalidOperationException>(() =>
            PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Displacement));
    }

    [Fact]
    public void RejectsReactionOnNodeWithoutCanonicalSupportResult()
    {
        CalculationResultPresentation presentation = Presentation(2);
        CalculationDerivedCase pickup = Assert.Single(presentation.Derived!.Pickups);
        var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(pickup.Reactions);
        modes["fx_max"] = [modes["fx_max"][0] with { EntityId = "2" }];
        modes["fx_min"] = [modes["fx_min"][0] with { EntityId = "2" }];
        presentation.UpdateDerived(Derived(pickup.Displacements, modes));

        Assert.Throws<InvalidOperationException>(() =>
            PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Reaction));
    }

    [Fact]
    public void PreservesPickupOrderAndAlternateRawUnits()
    {
        CalculationResultPresentation presentation = Presentation(2, length: "mm", force: "N");
        CalculationDerivedCase pickup = Assert.Single(presentation.Derived!.Pickups);
        var second = new CalculationDerivedCase("0", null, pickup.Displacements, pickup.Reactions,
            pickup.SectionForces);
        presentation.UpdateDerived(new CalculationDerivedPresentation([], presentation.Derived.Combines,
            [pickup, second]));

        string[] displacement = PickupNodeExportFormatter.Format(presentation,
            PickupNodeQuantity.Displacement).Split('\n');
        Assert.Contains("max_dx (mm)", displacement[0]);
        Assert.Contains("max_rx (rad)", displacement[0]);
        Assert.StartsWith("1,dx,1,", displacement[1]);
        Assert.StartsWith("0,dx,1,", displacement[7]);
        string reaction = PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Reaction);
        Assert.Contains("max_fx (N)", reaction);
        Assert.Contains("max_mx (N*mm)", reaction);
    }

    [Fact]
    public void RejectsAbsentOrEmptyDerivedResults()
    {
        CalculationResultPresentation presentation = Presentation(2);
        presentation.UpdateDerived(null);
        Assert.Throws<InvalidOperationException>(() =>
            PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Displacement));
        presentation.UpdateDerived(new CalculationDerivedPresentation([], [], []));
        Assert.Throws<InvalidOperationException>(() =>
            PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Reaction));
    }

    [Fact]
    public void QuotesAndNeutralizesUntrustedCsvIdentifiers()
    {
        CalculationResultPresentation presentation = Presentation(3, " =P,\"1");

        string line = PickupNodeExportFormatter.Format(presentation, PickupNodeQuantity.Reaction)
            .Split('\n')[1];

        Assert.StartsWith("\"' =P,\"\"1\",fx,1,40,41,", line);
    }

    [Fact]
    public void SaveReplacesExistingFileWithoutBomOrTemporaryFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "FrameWebPickup-" +
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "pickup.csv");
            File.WriteAllText(path, "old");
            MenuComponent.WritePickupCsv(path, "node,変位\n1,2\n");

            byte[] saved = File.ReadAllBytes(path);
            Assert.Equal("node,変位\n1,2\n", System.Text.Encoding.UTF8.GetString(saved));
            Assert.False(saved.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FailedReplacementPreservesDestinationAndCleansTemporaryFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), "FrameWebPickup-" +
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "pickup.csv");
            File.WriteAllText(path, "old");
            using (FileStream locked = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.Throws<IOException>(() => MenuComponent.WritePickupCsv(path, "new"));
            Assert.Equal("old", File.ReadAllText(path));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static CalculationResultPresentation Presentation(int dimension, string pickupId = "1",
        bool secondSupport = false, string length = "m", string force = "kN")
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException();
        string path = Path.Combine(root, "FrameWeb", "tests", "data", "contracts", "positive",
            "single-static.json");
        JsonObject wire = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        wire["units"]!["length"] = length;
        wire["units"]!["force"] = force;
        if (secondSupport)
        {
            wire["cases"]!.AsArray()[0]!["support_node_ids"]!.AsArray().Add("2");
            JsonArray reactions = wire["results"]!.AsArray()[0]!["support_reactions"]!.AsArray();
            JsonNode second = reactions[0]!.DeepClone();
            second["node_id"] = "2";
            reactions.Add(second);
        }
        AnalysisResultSet resultSet = AnalysisResultSetJson.Deserialize(wire.ToJsonString());

        var displacement = Modes(DisplacementNames, dimension, ["2", "1"]);
        var reaction = Modes(ReactionNames, dimension, secondSupport ? ["2", "1"] : ["1"]);
        return new CalculationResultPresentation(resultSet, Derived(displacement, reaction, pickupId), dimension);
    }

    private static CalculationDerivedPresentation Derived(
        IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> displacement,
        IReadOnlyDictionary<string, IReadOnlyList<CalculationDerivedRow>> reaction,
        string pickupId = "1") =>
        new([], [Combine("40"), Combine("41")],
            [new CalculationDerivedCase(pickupId, null, displacement, reaction,
                new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>())]);

    private static CalculationDerivedCase Combine(string id) => new(id, null,
        new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(),
        new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(),
        new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>());

    private static Dictionary<string, IReadOnlyList<CalculationDerivedRow>> Modes(
        string[] names, int dimension, string[] nodeIds)
    {
        var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>();
        foreach (string focus in dimension == 2
            ? new[] { names[0], names[1], names[5] } : names)
        {
            modes[focus + "_max"] = nodeIds.Select(id => Row(id, "40", names,
                [0.001234567890123456, 2, 3, 4, 5, 6])).ToArray();
            modes[focus + "_min"] = nodeIds.Select(id => Row(id, "41", names,
                [-1, -2, -3, -4, -5, -6])).ToArray();
        }
        return modes;
    }

    private static CalculationDerivedRow Row(string nodeId, string source, string[] names,
        double[] values) => new(nodeId, null,
            names.Select((name, index) => (name, value: values[index]))
                .ToDictionary(item => item.name, item => item.value), source, source);
}

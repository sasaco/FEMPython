using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Collections.Immutable;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCtDisgParityTests
{
    [Fact]
    public void CtPresetMatchesLegacyDisplacementShapeAndRepresentativeValues()
    {
        string preset = FindCtPreset();
        try
        {
            using var file = File.OpenRead(preset);
            using var document = JsonDocument.Parse(file);
            InputDataService.Instance.JsonDataOpen(document.RootElement);

            ResultCombineDisgSnapshot snapshot = Assert.IsType<ResultCombineDisgSnapshot>(
                ResultCombineDisgCoordinator.Instance.Snapshot);
            Assert.Equal(11, snapshot.Displacements.Length);
            Assert.All(snapshot.Displacements, item => Assert.Equal(63, item.Nodes.Length));
            Assert.Equal(0.5004880259043605,
                snapshot.Displacements[0].Nodes[0].Value.Dz, 12);

            ResultCombineDisgOutput combined = ResultCombineDisgAggregator.Calculate(snapshot);
            Assert.Equal(94, combined.Cases.Count);
            Assert.All(combined.Cases, item => Assert.All(item.Rows.Values,
                rows => Assert.Equal(63, rows.Count)));
            CombineDisgNodeResult first = combined.Cases[0].Rows["dx_max"][0];
            Assert.Equal("1", first.Id);
            Assert.Equal(1.584227560540803, first.Dz, 10);
            Assert.Equal("+1+2+3+7+11", first.Case);

            ImmutableArray<PickupSelection> picks = InputCombineService.Instance.PickupRows.Values
                .Select(row => new PickupSelection(row.Id, row.name,
                    row.Coefficients.Values.ToImmutableArray())).ToImmutableArray();
            PickupTableOutput picked = ResultPickupDisgAggregator.SelectCombined(
                combined, snapshot.Dimension, picks);
            Assert.Equal(14, picked.Cases.Count);
            Assert.All(picked.Cases, item => Assert.All(item.Rows.Values,
                rows => Assert.Equal(63, rows.Count)));
            string[] row24 = picked.Cases[0].Rows["dz_max"][23];
            Assert.Equal("24", row24[0]);
            Assert.Equal("11.9084", row24[3]);
            Assert.Equal("+1+2+3+4+6-7+8+10+11", row24[^1]);
        }
        finally
        {
            ResultCombineDisgCoordinator.Instance.FailLoad();
            ResultCombineFsecCoordinator.Instance.FailLoad();
            ResultCombineReacCoordinator.Instance.FailLoad();
            InputCombineService.Instance.clear();
            ResultDisgService.Instance.clear();
            ResultFsecService.Instance.clear();
            ResultReacService.Instance.clear();
        }
    }

    [Fact]
    public void LegacyDisplayNodeFilterRunsBeforeCombiningCases()
    {
        try
        {
            using var document = JsonDocument.Parse("""
                {"dimension":3,"load":{"1":{},"2":{}},
                 "define":{"1":{"row":1,"C1":1},"2":{"row":2,"C1":2}},
                 "combine":{"1":{"row":1,"C1":1,"C2":1}},
                 "result":{"1":{"disg":{"node1":{"dz":0.001},"2n1":{"dz":9}}},
                           "2":{"disg":{"node1":{"dz":0.002},"3l1":{"dz":8}}}}}
                """);
            InputDataService.Instance.JsonDataOpen(document.RootElement);
            ResultCombineDisgSnapshot snapshot = Assert.IsType<ResultCombineDisgSnapshot>(
                ResultCombineDisgCoordinator.Instance.Snapshot);
            Assert.All(snapshot.Displacements, item => Assert.Single(item.Nodes));
            var row = Assert.Single(ResultCombineDisgAggregator.Calculate(snapshot)
                .Cases[0].Rows["dz_max"]);
            Assert.Equal("1", row.Id);
            Assert.Equal(3, row.Dz, 12);
        }
        finally
        {
            ResultCombineDisgCoordinator.Instance.FailLoad();
            ResultCombineFsecCoordinator.Instance.FailLoad();
            ResultCombineReacCoordinator.Instance.FailLoad();
            InputCombineService.Instance.clear();
            ResultDisgService.Instance.clear();
            ResultFsecService.Instance.clear();
            ResultReacService.Instance.clear();
        }
    }

    private static string FindCtPreset()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null;
             directory = directory.Parent)
        {
            string path = Path.Combine(directory.FullName, "FrameWebforJS", "src", "assets",
                "preset", "サンプル（Ct桁）.json");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Ct preset was not found from the test directory.");
    }
}

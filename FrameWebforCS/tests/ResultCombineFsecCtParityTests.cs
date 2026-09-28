using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCombineFsecCtParityTests
{
    [Theory]
    [InlineData(7.625, "7.63")]
    [InlineData(-7.625, "-7.63")]
    [InlineData(1.005, "1.00")]
    [InlineData(-1.005, "-1.00")]
    public void PickupFormattingMatchesJavascriptBinary64ToFixed(double value, string expected)
    {
        Assert.Equal(expected, ResultPickupFsecAggregator.Format(value));
    }

    [Fact]
    public void LegacyCaptureRoundsEndpointsAndKeepsPrecedingEndAtRepeatedStation()
    {
        using var document = JsonDocument.Parse("""
            {"dimension":3,"load":{"1":{"name":"first"}},
             "define":{"1":{"row":1,"C1":1}},
             "combine":{"1":{"row":1,"C1":1}},
             "pickup":{"1":{"row":1,"C1":1}},
             "result":{"1":{"disg":{"1":{"dx":0}},"reac":{"1":{"tx":0}},
               "fsec":{"1":{"P1":{"fzi":1.23496,"fzj":2.34501,"L":0.12349},
                              "P2":{"fzi":99,"fzj":3.456,"L":0.2}}}}}}
            """);
        var coordinator = ResultCombineFsecCoordinator.Instance;
        try
        {
            InputDataService.Instance.JsonDataOpen(document.RootElement);
            Assert.Equal(CombineFsecState.Valid, coordinator.State);
            ResultCombineFsecSnapshot snapshot = Assert.IsType<ResultCombineFsecSnapshot>(
                coordinator.Snapshot);
            Assert.Equal(2.35, snapshot.Forces[0].Rows[1].Value.Fz);
            Assert.Equal(99, snapshot.Forces[0].Rows[2].Value.Fz);
            Assert.Equal(0.123, snapshot.Forces[0].Rows[2].Location, 9);

            var combined = ResultCombineFsecAggregator.Calculate(snapshot);
            var rows = Assert.Single(combined.Cases).Rows["fz_max"];
            Assert.Equal(3, rows.Count);
            Assert.Equal(new[] { 1.23, 2.35, 3.46 }, rows.Select(row => row.Fz));
            Assert.Equal(new[] { 0.0, 0.123, 0.323 },
                rows.Select(row => Math.Round(row.Location, 3)));
            Assert.All(rows, row => Assert.Equal("+1", row.Case));
        }
        finally { coordinator.FailLoad(); }
    }

    [Fact]
    public void CtSizedOutputFitsBoundWhileLargerWorkStillFails()
    {
        const long ctOutputCells = 962L * 12 * 94 * 10;
        ResultCombineFsecAggregator.ValidateBudget(18_744, 0, ctOutputCells);
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineFsecAggregator.ValidateBudget(18_744, 0,
                ResultCombineFsecAggregator.MaxOutputCells + 1));
    }
}

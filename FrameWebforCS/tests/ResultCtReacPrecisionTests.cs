using System.Collections.Immutable;
using System.Text.Json;
using FrameWebforCS.components.result;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCtReacPrecisionTests
{
    [Fact]
    public void CtBoundaryValueKeepsItsDisplaySideThroughLoadAndPickup()
    {
        using var json = JsonDocument.Parse("""
            {"result":{"1":{"reac":{"7":{"tz":-1888.444989252514}}}}}
            """);
        ResultReacService.Instance.setReacJson(json.RootElement);
        double value = ResultReacService.Instance.getReac()["1"]["7"].tz!.Value;

        var combined = new ResultCombineReacOutput(
        [
            new CombineReacCaseResult("1", null,
                new Dictionary<string, IReadOnlyList<CombineReacNodeResult>>
                {
                    ["tz_min"] = [new("7", 0, 0, value, 0, 0, 0, "+1")]
                })
        ]);
        PickupTableOutput pickup = ResultPickupReacAggregator.SelectCombined(combined, 3,
            ImmutableArray.Create(new PickupSelection("1", null, [1])));

        Assert.Equal(-1888.444989252514, value, 12);
        Assert.Equal("-1888.44", pickup.Cases[0].Rows["tz_min"][0][3]);
    }
}

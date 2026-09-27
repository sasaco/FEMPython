using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class CalculationRequestRevisionTests
{
    [Fact]
    public void ElementListResetInvalidatesInFlightCalculationRevision()
    {
        var input = InputDataService.Instance;
        long before = input.CalculationInputRevision;
        InputElementsService.Instance.GetRows(1).ResetBindings();
        Assert.True(input.CalculationInputRevision > before);
    }

    [Fact]
    public void SelectingConstraintSheetDoesNotInvalidateCalculationRevision()
    {
        var input = InputDataService.Instance;
        var supports = InputFixNodeService.Instance;
        string previous = supports.SelectedCaseId;
        string next = previous == "1" ? "2" : "1";
        try
        {
            long before = input.CalculationInputRevision;
            supports.SelectCase(next);
            Assert.Equal(before, input.CalculationInputRevision);
        }
        finally { supports.SelectCase(previous); }
    }
}

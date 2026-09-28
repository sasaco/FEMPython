using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCombineDisgIntegrationTests
{
    [Fact]
    public void FileLoadPublishesOnlyCompleteResultAndRejectedReplacementRetainsPreviousResult()
    {
        var coordinator = ResultCombineDisgCoordinator.Instance;
        try
        {
            Open("""
                {"dimension":2,"load":{"1":{"name":"first"}},
                 "combine":{"1":{"row":1,"C1":2}},
                 "result":{"1":{"disg":{"1":{"dx":3,"dy":4,"dz":null,"rx":null,"ry":null,"rz":5}}}}}
                """);
            Assert.Equal(CombineDisgState.Valid, coordinator.State);
            Assert.NotNull(coordinator.Snapshot);

            // Input-only files are supported, but cannot reuse the previous result.
            Open("""
                {"dimension":3,"load":{"1":{"name":"second"}},
                 "combine":{"1":{"row":1,"C1":4}}}
                """);
            Assert.Equal(CombineDisgState.Invalid, coordinator.State);
            Assert.Null(coordinator.Snapshot);
            InputCombineService.Instance.SetCombineCoefficient(1, 1, 5);
            Assert.Equal(CombineDisgState.Invalid, coordinator.State);
            Assert.Null(coordinator.Snapshot);

            Open("""
                {"dimension":2,"load":{"1":{"name":"third"}},
                 "combine":{"1":{"row":1,"C1":6}},
                 "result":{"1":{"disg":{"1":{"dx":7,"dy":8,"dz":null,"rx":null,"ry":null,"rz":9}}}}}
                """);
            Assert.Equal(CombineDisgState.Valid, coordinator.State);
            Assert.NotNull(coordinator.Snapshot);
            var previousSnapshot = coordinator.Snapshot;

            Assert.Throws<JsonException>(() => Open("""
                {"dimension":2,"load":{"1":{"name":"bad"}},
                 "combine":{"1":{"row":1,"C1":8}},
                 "result":{"1":{"disg":{"1":{"dx":"invalid"}}}}}
                """));
            // The viewport replacement path now parses the entire file before BeginLoad.
            // Rejecting B leaves the committed result, combine row, and derived snapshot A.
            Assert.Equal(CombineDisgState.Valid, coordinator.State);
            Assert.Same(previousSnapshot, coordinator.Snapshot);
            Assert.Equal(6, InputCombineService.Instance.CombineRows[1].Coefficients[1]);
            InputCombineService.Instance.SetCombineCoefficient(1, 1, 9);
            Assert.Equal(CombineDisgState.Valid, coordinator.State);
            Assert.NotNull(coordinator.Snapshot);
        }
        finally
        {
            coordinator.FailLoad();
            InputCombineService.Instance.clear();
            ResultDisgService.Instance.clear();
        }
    }

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }

    [Fact]
    public void CombineEditNotifiesOnlyAfterSuccessfulChangeAndKeepsRowsReplacedContract()
    {
        var service = InputCombineService.Instance;
        service.clear();
        int changed = 0;
        int replaced = 0;
        EventHandler onChanged = (_, _) => changed++;
        EventHandler onReplaced = (_, _) => replaced++;
        service.RowsChanged += onChanged;
        service.RowsReplaced += onReplaced;
        try
        {
            service.SetCombineCoefficient(1, 2, 1.5f);
            service.SetCombineCoefficient(1, 2, 1.5f);
            service.SetCombineName(1, "C1");
            service.SetCombineName(1, "C1");
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.SetCombineCoefficient(1, 2, float.NaN));

            Assert.Equal(2, changed);
            Assert.Equal(0, replaced);

            service.clear();
            Assert.Equal(3, changed);
            Assert.Equal(1, replaced);
        }
        finally
        {
            service.RowsChanged -= onChanged;
            service.RowsReplaced -= onReplaced;
            service.clear();
        }
    }
}

using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class CalculationResultPersistenceTests
{
    private const string LegacyDocument = """
        {"dimension":3,"result":{"Old":{"disg":{"node1":{"dx":0.01}}}}}
        """;

    [Fact]
    public void ValidCalculationStartClearsLegacySaveFieldsAndCanonicalResultStaysRuntimeOnly()
    {
        InputDataService input = InputDataService.Instance;
        Open(input, LegacyDocument);
        try
        {
            AssertSavedResult(input, expected: true);
            input.ClearLegacyResultsForCalculation();
            CalculationResultStore.Instance.Clear();
            AssertSavedResult(input, expected: false);

            AnalysisResultSet resultSet = AnalysisResultSetJson.Deserialize(Fixture());
            CalculationResultStore.Instance.Commit(new CalculationResultPresentation(resultSet));
            Assert.NotNull(CalculationResultStore.Instance.Current);
            AssertSavedResult(input, expected: false);

            Open(input, LegacyDocument);
            Assert.Null(CalculationResultStore.Instance.Current);
            AssertSavedResult(input, expected: true);
        }
        finally
        {
            Open(input, "{}");
            CalculationResultStore.Instance.Clear();
        }
    }

    [Fact]
    public async Task DerivedInputEditRebuildsPresentationWithoutReplacingBaseResult()
    {
        InputDataService input = InputDataService.Instance;
        Open(input, "{}");
        var presentation = new CalculationResultPresentation(
            AnalysisResultSetJson.Deserialize(Fixture()),
            new CalculationDerivedPresentation([], [], []));
        CalculationResultStore.Instance.Commit(presentation);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnChanged(object? sender, EventArgs args)
        {
            if (ReferenceEquals(CalculationResultStore.Instance.Current, presentation) &&
                presentation.Derived is not null)
                completed.TrySetResult();
        }
        CalculationResultStore.Instance.Changed += OnChanged;
        try
        {
            InputCombineService.Instance.ApplyCombine(new(), new(), new());
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Same(presentation, CalculationResultStore.Instance.Current);
            Assert.NotNull(presentation.Derived);
        }
        finally
        {
            CalculationResultStore.Instance.Changed -= OnChanged;
            Open(input, "{}");
        }
    }

    private static void AssertSavedResult(InputDataService input, bool expected)
    {
        using JsonDocument saved = JsonDocument.Parse(JsonSerializer.Serialize(input.GetSaveJson()));
        Assert.Equal(expected, saved.RootElement.TryGetProperty("result", out _));
        Assert.Equal(expected, saved.RootElement.TryGetProperty("resultDimension", out _));
    }

    private static void Open(InputDataService input, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        input.JsonDataOpen(document.RootElement);
    }

    private static string Fixture()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException();
        return File.ReadAllText(Path.Combine(directory.FullName, "FrameWeb", "tests", "data",
            "contracts", "positive", "single-static.json"));
    }
}

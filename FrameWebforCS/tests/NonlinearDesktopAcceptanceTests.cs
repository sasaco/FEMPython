using FrameWebforCS.calculation;
using FrameWebforCS.providers;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("Python runtime")]
public sealed class NonlinearDesktopAcceptanceTests(Xunit.Abstractions.ITestOutputHelper output)
{
    // Opening the model in the desktop stores E/A as float and drops nu. The README's acceptance
    // band against JRSNAP is ±1 %; a model opened in the desktop must stay in it.
    [Fact]
    public async Task P7OpenedInTheDesktopStaysWithinTheAcceptanceBand()
    {
        if (await IsolatedPythonTest.RanInChildAsync(typeof(NonlinearDesktopAcceptanceTests), TimeSpan.FromMinutes(5))) return;
        CalculationRequest request;
        try
        {
            using (var document = JsonDocument.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path())))
                InputDataService.Instance.JsonDataOpen(document.RootElement);
            request = InputDataService.Instance.CreateCalculationRequest();
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            InputDataService.Instance.JsonDataOpen(empty.RootElement);
        }
        await using var runtime = new PythonCalculationRuntime(NonlinearCalculationEngineTests.RepositoryRoot());
        string json = await runtime.CalculateAsync(request.Json).WaitAsync(TimeSpan.FromMinutes(5));
        double peak = NonlinearCalculationEngineTests.PeakBaseShear(json);
        output.WriteLine($"desktop path peak base shear {peak:R} kN (raw file 10701.4)");
        Assert.InRange(peak, 10701.4*0.99, 10701.4*1.01);
    }
}

using Convert_Manager.FrameG;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Convert_Manager.Tests;

// .frd -> JSON (ConvertManager) -> .frd (FrdExporter) -> JSON must describe the same model.
public class FrdSampleRoundTripTests
{
    private readonly ITestOutputHelper output;

    public FrdSampleRoundTripTests(ITestOutputHelper output) => this.output = output;

    [Theory]
    [InlineData("4_2-FrameG.frd", false)]
    [InlineData("バネ連衡なし.frd", true)]
    public void SampleModelsSurviveARoundTrip(string fileName, bool hasTrainLoads)
    {
        JObject first = FrdExporterTests.Forward(FrdExporterTests.SampleBytes(fileName));
        first["dimension"] = 2;
        if (hasTrainLoads)
        {
            // Train loads (FrameG mark 14) are outside the export; without them the rest must survive.
            Assert.Contains(FrdExporterTests.Export(first).Errors, error => error.Code == FrdDiagnosticCodes.LoadType);
            foreach (var loadCase in ((JObject)first["load"]!).Properties())
                ((JArray)loadCase.Value["load_member"]!).Where(row => (string)row["mark"]! == "14").ToList()
                    .ForEach(row => row.Remove());
        }

        FrdExportResult result = FrdExporterTests.Export(first);
        foreach (var diagnostic in result.Diagnostics)
            output.WriteLine(diagnostic.ToString());
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        JObject second = FrdExporterTests.Forward(result.Frd);
        second["dimension"] = 2;

        var expected = Normalize(first);
        var actual = Normalize(second);
        foreach (var property in expected.Properties())
            Assert.True(JToken.DeepEquals(property.Value, actual[property.Name]),
                $"{property.Name}\nexpected: {property.Value}\nactual: {actual[property.Name]}");
        Assert.Equal(expected.Properties().Select(p => p.Name), actual.Properties().Select(p => p.Name));
    }

    [Theory]
    [InlineData("バネ連衡あり.frd")]
    [InlineData("杭2こ.lzh")]
    public void PileSpringSamplesAreRefused(string fileName)
    {
        JObject json = FrdExporterTests.Forward(FrdExporterTests.SampleBytes(fileName));
        json["dimension"] = 2;

        FrdExportResult result = FrdExporterTests.Export(json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == FrdDiagnosticCodes.PileSpring);
    }

    // Removes what the archive does not carry or carries only cosmetically.
    private static JObject Normalize(JObject json)
    {
        var copy = (JObject)json.DeepClone();
        // Member names are cut to 30 bytes; FrameG's own names carry a stray digit at byte 30.
        foreach (var sheet in ((JObject)copy["element"]!).Properties())
            foreach (var material in ((JObject)sheet.Value).Properties())
                ((JObject)material.Value).Remove("n");
        foreach (var sheet in ((JObject)copy["fix_node"]!).Properties())
            foreach (var row in (JArray)sheet.Value)
                row["n"] = ((string)row["n"]!).Trim();
        return copy;
    }
}

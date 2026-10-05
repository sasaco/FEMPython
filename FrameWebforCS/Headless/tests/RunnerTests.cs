using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Headless.Tests;

public sealed class RunnerTests
{
    private const string SavedModel = """
        {"dimension":2,
         "node":{"1":{"x":0,"y":0},"2":{"x":1,"y":0}},
         "member":{"1":{"ni":"1","nj":"2","e":"1"}},
         "element":{"1":{"1":{"E":10000,"A":1,"Iz":1}}},
         "fix_node":{"1":[{"n":"1","tx":1,"ty":1,"rz":1}]},
         "load":{"1":{"symbol":"DL","fix_node":1,"element":1,
           "load_node":[{"row":1,"n":"2","tx":4}]}},
         "define":{"1":{"row":1,"C1":1}},
         "combine":{"1":{"row":1,"C1":1}},
         "pickup":{"1":{"row":1,"C1":1}},
         "result":{"stale":true},"analysis_result_set":{"stale":true}}
        """;

    [Theory]
    [InlineData("--generate-pdf", "yes")]
    [InlineData("--pdf-sections", "input,input")]
    [InlineData("--pdf-sections", "input,unknown")]
    [InlineData("--pdf-sections", "")]
    [InlineData("--unknown", "true")]
    public void InvalidOptionsAreRejected(string key, string value)
    {
        Assert.Throws<RunnerException>(() => RunOptions.Parse(
            ["run", "--input", "C:\\input.json", "--output-dir", "C:\\output", key, value]));
    }

    [Fact]
    public void RepeatedOptionsAndRelativePathsAreRejected()
    {
        Assert.Throws<RunnerException>(() => RunOptions.Parse(
            ["run", "--input", "C:\\input.json", "--output-dir", "C:\\output", "--input", "C:\\other.json"]));
        Assert.Throws<RunnerException>(() => RunOptions.Parse(
            ["run", "--input", "input.json", "--output-dir", "C:\\output"]));
    }

    [Fact]
    public void DefaultsIncludeAllFiveSections()
    {
        var options = RunOptions.Parse(["run", "--input", "C:\\input.json", "--output-dir", "C:\\output"]);
        Assert.True(options.GeneratePdf);
        Assert.True(options.GeneratePik);
        Assert.Equal(RunOptions.DefaultSections, options.PdfSections);
    }

    [Theory]
    [InlineData("{\"dimension\":2,\"dimension\":3}")]
    [InlineData("{\"dimension\":4}")]
    [InlineData("{\"dimension\":\"2\"}")]
    [InlineData("{\"node\":{\"1\":{},\"1\":{}}}")]
    [InlineData("[]")]
    public void InvalidDocumentsAreRejected(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Throws<RunnerException>(() => HeadlessCalculation.ValidateDocument(document.RootElement));
    }

    [Fact]
    public async Task RealProcessRecalculatesAndEmitsRequestedArtifacts()
    {
        using var job = new Job(SavedModel);
        var response = await Run(job);
        Assert.Equal(0, response.ExitCode);
        using var envelope = JsonDocument.Parse(response.Stdout);
        var root = envelope.RootElement;
        Assert.True(root.GetProperty("ok").GetBoolean(), response.Stderr);
        Assert.Equal("success", root.GetProperty("executionStatus").GetString());
        Assert.Equal("not_checked", root.GetProperty("engineeringStatus").GetString());
        Assert.Equal(5, root.GetProperty("summary").GetProperty("pdfSections").GetArrayLength());
        Assert.Equal(3, root.GetProperty("artifacts").GetArrayLength());
        foreach (var artifact in root.GetProperty("artifacts").EnumerateArray())
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(job.Output, artifact.GetProperty("relativePath").GetString()!));
            Assert.Equal(bytes.LongLength, artifact.GetProperty("bytes").GetInt64());
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), artifact.GetProperty("sha256").GetString());
        }
        using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(job.Output, "result.json")));
        var analysis = result.RootElement.GetProperty("analysisResultSet");
        Assert.Equal("analysis_result_set", analysis.GetProperty("kind").GetString());
        Assert.Single(analysis.GetProperty("cases").EnumerateArray());
        var tip = analysis.GetProperty("results")[0].GetProperty("node_displacements")
            .EnumerateArray().Single(row => row.GetProperty("node_id").GetString() == "2");
        Assert.Equal(0.0004, tip.GetProperty("components").GetProperty("dx").GetDouble(), 10);
        Assert.Single(result.RootElement.GetProperty("derived").GetProperty("pickups").EnumerateArray());
        string pik = File.ReadAllText(Path.Combine(job.Output, "pickup.pik"), new UTF8Encoding(false, true));
        Assert.True(pik.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length > 1);
        string firstPikRow = pik.Split('\n')[1];
        var pickup = result.RootElement.GetProperty("derived").GetProperty("pickups")[0];
        Assert.Equal(pickup.GetProperty("id").GetString(), firstPikRow[..5].Trim());
        Assert.Equal("M", firstPikRow[5..10].Trim());
        foreach (var (mode, sourceOffset, valueOffset) in new[] { ("mz_max", 15, 40), ("mz_min", 20, 70) })
        {
            var vector = pickup.GetProperty("sectionForces").GetProperty(mode)[0];
            Assert.Equal(vector.GetProperty("entityId").GetString(), firstPikRow[10..15].Trim());
            Assert.Equal(vector.GetProperty("sourceCaseId").GetString(), firstPikRow.Substring(sourceOffset, 5).Trim());
            foreach (var (component, index) in new[] { "mz", "fy", "fx" }.Select((value, index) => (value, index)))
                Assert.Equal(vector.GetProperty("components").GetProperty(component).GetDouble(),
                    double.Parse(firstPikRow.Substring(valueOffset + index * 10, 10), CultureInfo.InvariantCulture), 2);
        }
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(job.Output, "report.pdf")), 0, 5));
        Assert.DoesNotContain(Directory.EnumerateFileSystemEntries(job.Output), name => name.Contains(".staging-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DisabledExportsProduceOnlyFreshJson()
    {
        using var job = new Job(SavedModel);
        var response = await Run(job, "--generate-pdf", "false", "--generate-pik", "false");
        Assert.Equal(0, response.ExitCode);
        Assert.Equal(new[] { "result.json" }, Directory.GetFiles(job.Output).Select(Path.GetFileName));
    }

    [Theory]
    [InlineData("{\"dimension\":2}", "PICKUP_REQUIRED")]
    [InlineData("{\"dimension\":3}", "PIK_REQUIRES_2D")]
    [InlineData("{\"dimension\":2,\"dimension\":2}", "INVALID_INPUT")]
    [InlineData("{broken", "INVALID_INPUT")]
    public async Task InvalidInputProducesFailureEnvelopeAndNoArtifacts(string input, string code)
    {
        using var job = new Job(input);
        var response = await Run(job);
        Assert.NotEqual(0, response.ExitCode);
        using var document = JsonDocument.Parse(response.Stdout);
        Assert.False(document.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(code, document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Empty(document.RootElement.GetProperty("artifacts").EnumerateArray());
        Assert.False(Directory.Exists(job.Output));
    }

    [Fact]
    public async Task NonemptyDirectoryIsNotOverwritten()
    {
        using var job = new Job(SavedModel);
        Directory.CreateDirectory(job.Output);
        string existing = Path.Combine(job.Output, "result.json");
        File.WriteAllText(existing, "preserve");
        var response = await Run(job);
        Assert.NotEqual(0, response.ExitCode);
        Assert.Equal("preserve", File.ReadAllText(existing));
        using var document = JsonDocument.Parse(response.Stdout);
        Assert.Equal("OUTPUT_NOT_EMPTY", document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task MissingCombinationTermFailsInsteadOfPublishingPartialPickup()
    {
        using var job = new Job(SavedModel.Replace("\"combine\":{\"1\":{\"row\":1,\"C1\":1}}",
            "\"combine\":{\"1\":{\"row\":1,\"C1\":1,\"C99\":1}}", StringComparison.Ordinal));
        var response = await Run(job);
        Assert.NotEqual(0, response.ExitCode);
        using var document = JsonDocument.Parse(response.Stdout);
        Assert.Equal("INVALID_DERIVED_REFERENCE", document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.False(Directory.Exists(job.Output));
    }

    [Fact]
    public async Task EmptySavedLoadCaseRetainsDesktopZeroContributionSemantics()
    {
        string model = SavedModel
            .Replace("\"load\":{\"1\":", "\"load\":{\"2\":{\"symbol\":\"EMPTY\",\"fix_node\":1,\"element\":1},\"1\":", StringComparison.Ordinal)
            .Replace("\"define\":{\"1\":{\"row\":1,\"C1\":1}}",
                "\"define\":{\"1\":{\"row\":1,\"C1\":1},\"2\":{\"row\":2,\"C1\":2}}", StringComparison.Ordinal)
            .Replace("\"combine\":{\"1\":{\"row\":1,\"C1\":1}}",
                "\"combine\":{\"1\":{\"row\":1,\"C1\":1,\"C2\":1}}", StringComparison.Ordinal);
        using var job = new Job(model);
        var response = await Run(job);
        Assert.Equal(0, response.ExitCode);
        using var document = JsonDocument.Parse(response.Stdout);
        Assert.True(document.RootElement.GetProperty("ok").GetBoolean(), response.Stderr);
        Assert.Equal(1, document.RootElement.GetProperty("summary").GetProperty("cases").GetInt32());
        Assert.Equal("EMPTY_LOAD_CASE_SKIPPED", document.RootElement.GetProperty("messages")[0].GetProperty("code").GetString());
        Assert.Equal("load.2", document.RootElement.GetProperty("messages")[0].GetProperty("path").GetString());
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> Run(Job job, params string[] additional)
    {
        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string runner = Path.Combine(FindRoot(), "FrameWebforCS", "Headless", "bin", configuration, "net10.0-windows", "FrameWebforCS.Headless.exe");
        var start = new ProcessStartInfo(runner)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
            RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (string argument in new[] { "run", "--input", job.Input, "--output-dir", job.Output }.Concat(additional))
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(entireProcessTree: true); throw; }
        return (process.ExitCode, await stdout, await stderr);
    }

    private static string FindRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "FrameWeb"))) return directory.FullName;
        throw new InvalidOperationException("Repository not found.");
    }

    private sealed class Job : IDisposable
    {
        private readonly string directory = Path.Combine(FindRoot(), ".tmp", "headless-tests", Guid.NewGuid().ToString("N"));
        internal Job(string input)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Input, input, new UTF8Encoding(false));
        }
        internal string Input => Path.Combine(directory, "input.json");
        internal string Output => Path.Combine(directory, "output");
        public void Dispose() => Directory.Delete(directory, recursive: true);
    }
}

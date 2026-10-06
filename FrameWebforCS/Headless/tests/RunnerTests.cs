using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    [InlineData("--generate-pickup-displacement-csv", "yes")]
    [InlineData("--generate-pickup-reaction-csv", "1")]
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
        Assert.False(options.GeneratePickupDisplacementCsv);
        Assert.False(options.GeneratePickupReactionCsv);
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

    [Theory]
    [InlineData(2, true, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, true)]
    [InlineData(3, true, false)]
    [InlineData(3, false, true)]
    [InlineData(3, true, true)]
    public async Task NodeCsvExportsPreserveRawCorrelatedVectors(int dimension, bool displacement, bool reaction)
    {
        using var job = new Job(NodeCsvModel(dimension));
        var response = await Run(job, "--generate-pdf", "false", "--generate-pik", "false",
            "--generate-pickup-displacement-csv", displacement ? "true" : "false",
            "--generate-pickup-reaction-csv", reaction ? "true" : "false");
        Assert.True(response.ExitCode == 0, response.Stdout + response.Stderr);
        using var envelope = JsonDocument.Parse(response.Stdout);
        using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(job.Output, "result.json")));
        Assert.Equal(1 + (displacement ? 1 : 0) + (reaction ? 1 : 0),
            envelope.RootElement.GetProperty("artifacts").GetArrayLength());
        Assert.Equal(displacement, File.Exists(Path.Combine(job.Output, "pickup-displacement.csv")));
        Assert.Equal(reaction, File.Exists(Path.Combine(job.Output, "pickup-reaction.csv")));
        Assert.False(File.Exists(Path.Combine(job.Output, "pickup.pik")));
        Assert.False(File.Exists(Path.Combine(job.Output, "report.pdf")));
        if (displacement) AssertNodeCsv(job, envelope.RootElement, result.RootElement, dimension, "displacement");
        if (reaction) AssertNodeCsv(job, envelope.RootElement, result.RootElement, dimension, "reaction");
    }

    [Fact]
    public async Task BothCsvExportsCanAccompanyExistingPikAndPdf()
    {
        using var job = new Job(SavedModel);
        var response = await Run(job, "--generate-pickup-displacement-csv", "true",
            "--generate-pickup-reaction-csv", "true");
        Assert.True(response.ExitCode == 0, response.Stdout + response.Stderr);
        using var document = JsonDocument.Parse(response.Stdout);
        Assert.Equal(new[] { "result.json", "pickup.pik", "report.pdf", "pickup-displacement.csv", "pickup-reaction.csv" },
            document.RootElement.GetProperty("artifacts").EnumerateArray().Select(item => item.GetProperty("relativePath").GetString()));
    }

    [Theory]
    [InlineData("--generate-pickup-displacement-csv", "pickup-displacement.csv")]
    [InlineData("--generate-pickup-reaction-csv", "pickup-reaction.csv")]
    public async Task ShellNodeCsvDoesNotRequireMemberSectionForces(string flag, string filename)
    {
        var model = JsonNode.Parse(NodeCsvModel(3))!.AsObject();
        model["node"]!["3"] = JsonNode.Parse("""{"x":0,"y":1,"z":0}""");
        model["member"] = new JsonObject();
        model["shell"] = JsonNode.Parse("""{"1":{"nodes":[1,2,3],"e":1}}""");
        model["element"]!["1"]!["1"] = JsonNode.Parse("""{"E":10000,"G":4000,"nu":0.25,"A":0.2}""");
        model["fix_node"]!["1"] = JsonNode.Parse("""
            [{"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1},
             {"n":"2","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1},
             {"n":"3","rz":1}]
            """);
        model["load"]!["1"]!["load_node"]![0]!["n"] = "3";
        model["load"]!["2"]!["load_node"]![0]!["n"] = "3";
        using var job = new Job(model.ToJsonString());
        var response = await Run(job, "--generate-pdf", "false", "--generate-pik", "false", flag, "true");
        Assert.True(response.ExitCode == 0, response.Stdout + response.Stderr);
        Assert.True(File.Exists(Path.Combine(job.Output, filename)));
        using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(job.Output, "result.json")));
        Assert.Empty(result.RootElement.GetProperty("analysisResultSet").GetProperty("topology").GetProperty("members").EnumerateArray());
        var sectionModes = result.RootElement.GetProperty("derived").GetProperty("pickups")[0].GetProperty("sectionForces");
        Assert.All(sectionModes.EnumerateObject(), mode => Assert.Empty(mode.Value.EnumerateArray()));
    }

    [Theory]
    [InlineData("--generate-pickup-displacement-csv")]
    [InlineData("--generate-pickup-reaction-csv")]
    public async Task RequestedNodeCsvRequiresPickupDefinitions(string flag)
    {
        var model = JsonNode.Parse(SavedModel)!.AsObject();
        model.Remove("pickup");
        using var job = new Job(model.ToJsonString());
        var response = await Run(job, "--generate-pdf", "false", "--generate-pik", "false", flag, "true");
        Assert.NotEqual(0, response.ExitCode);
        using var document = JsonDocument.Parse(response.Stdout);
        Assert.Equal("PICKUP_REQUIRED", document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Empty(document.RootElement.GetProperty("artifacts").EnumerateArray());
        Assert.False(Directory.Exists(job.Output));
    }

    [Theory]
    [InlineData("--generate-pickup-displacement-csv", "dx")]
    [InlineData("--generate-pickup-reaction-csv", "fx")]
    public async Task MissingRequestedNodeQuantityRejectsAllPreparedArtifacts(string flag, string component)
    {
        var model = JsonNode.Parse(SavedModel)!.AsObject();
        model["load"]!["2"] = JsonNode.Parse("""{"symbol":"EMPTY","fix_node":1,"element":1}""");
        model["define"]!["1"]!["C1"] = 2;
        using var job = new Job(model.ToJsonString());
        var response = await Run(job, "--generate-pik", "false", "--pdf-sections", "input", flag, "true");
        Assert.NotEqual(0, response.ExitCode);
        using var document = JsonDocument.Parse(response.Stdout);
        Assert.Equal("EXECUTION_FAILED", document.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Contains(component + " nodes", document.RootElement.GetProperty("errors")[0].GetProperty("message").GetString());
        Assert.Empty(document.RootElement.GetProperty("artifacts").EnumerateArray());
        Assert.False(Directory.Exists(job.Output));
    }

    private static string NodeCsvModel(int dimension)
    {
        var model = JsonNode.Parse(SavedModel)!.AsObject();
        model["dimension"] = dimension;
        model["element"]!["1"]!["1"] = JsonNode.Parse("""{"E":10000,"G":4000,"A":1,"Iz":1,"Iy":1,"J":1}""");
        model["fix_node"]!["1"] = JsonNode.Parse("""[{"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]""");
        model["load"] = JsonNode.Parse("""
            {"1":{"symbol":"DL","fix_node":1,"element":1,
                  "load_node":[{"row":1,"n":"2","tx":4.12345678901234,"ty":5,"tz":6,"rx":7,"ry":8,"rz":9}]},
             "2":{"symbol":"DL","fix_node":1,"element":1,
                  "load_node":[{"row":1,"n":"2","tx":-3,"ty":-5,"tz":2,"rx":-7,"ry":-9,"rz":1}]}}
            """);
        model["define"]!["2"] = JsonNode.Parse("""{"row":2,"C1":2}""");
        model["combine"]!["2"] = JsonNode.Parse("""{"row":2,"C2":1}""");
        model["pickup"] = JsonNode.Parse("""{"検証":{"row":1,"C1":1,"C2":2}}""");
        return model.ToJsonString();
    }

    private static void AssertNodeCsv(Job job, JsonElement envelope, JsonElement result, int dimension, string quantity)
    {
        string filename = "pickup-" + quantity + ".csv";
        byte[] bytes = File.ReadAllBytes(Path.Combine(job.Output, filename));
        Assert.False(bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }));
        string csv = new UTF8Encoding(false, true).GetString(bytes);
        Assert.Contains("検証", csv);
        var artifact = envelope.GetProperty("artifacts").EnumerateArray()
            .Single(item => item.GetProperty("relativePath").GetString() == filename);
        Assert.Equal("pickup-" + quantity, artifact.GetProperty("kind").GetString());
        Assert.Equal("text/csv; charset=utf-8", artifact.GetProperty("mediaType").GetString());
        Assert.Equal(bytes.Length, artifact.GetProperty("bytes").GetInt32());
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), artifact.GetProperty("sha256").GetString());
        string[] components = quantity == "displacement" ? ["dx", "dy", "dz", "rx", "ry", "rz"] : ["fx", "fy", "fz", "mx", "my", "mz"];
        string[] focus = dimension == 2 ? [components[0], components[1], components[5]] : components;
        string[][] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split(',')).ToArray();
        string length = result.GetProperty("analysisResultSet").GetProperty("units").GetProperty("length").GetString()!;
        string force = result.GetProperty("analysisResultSet").GetProperty("units").GetProperty("force").GetString()!;
        string[] units = quantity == "displacement" ? [length, length, length, "rad", "rad", "rad"] :
            [force, force, force, force + "*" + length, force + "*" + length, force + "*" + length];
        string[] expectedHeader = ["pickup_id", "focus_component", "node_id", "max_combine_id", "min_combine_id",
            .. new[] { "max", "min" }.SelectMany(prefix => components.Select((name, index) => $"{prefix}_{name} ({units[index]})"))];
        Assert.Equal(expectedHeader, lines[0]);
        var pickup = result.GetProperty("derived").GetProperty("pickups")[0];
        var modes = pickup.GetProperty(quantity == "displacement" ? "displacements" : "reactions");
        string[] topology = result.GetProperty("analysisResultSet").GetProperty("topology").GetProperty("nodes")
            .EnumerateArray().Select(node => node.GetProperty("node_id").GetString()!).ToArray();
        var expectedOrder = new List<string>();
        foreach (string component in focus)
        {
            var nodes = modes.GetProperty(component + "_max").EnumerateArray().Select(row => row.GetProperty("entityId").GetString()).ToHashSet();
            expectedOrder.AddRange(topology.Where(nodes.Contains).Select(node => component + "/" + node));
        }
        Assert.Equal(expectedOrder, lines.Skip(1).Select(line => line[1] + "/" + line[2]));
        foreach (string[] cells in lines.Skip(1))
        {
            Assert.Equal(17, cells.Length);
            Assert.Equal(pickup.GetProperty("id").GetString(), cells[0]);
            foreach (var (extreme, sourceColumn, valueColumn) in new[] { ("max", 3, 5), ("min", 4, 11) })
            {
                var vector = modes.GetProperty(cells[1] + "_" + extreme).EnumerateArray()
                    .Single(row => row.GetProperty("entityId").GetString() == cells[2]);
                Assert.Equal(vector.GetProperty("sourceCaseId").GetString(), cells[sourceColumn]);
                for (int index = 0; index < components.Length; index++)
                    Assert.Equal(vector.GetProperty("components").GetProperty(components[index]).GetDouble(),
                        double.Parse(cells[valueColumn + index], CultureInfo.InvariantCulture));
            }
        }
        Assert.Contains(lines.Skip(1), cells => cells[3] != cells[4]);
        if (quantity == "reaction")
        {
            // The shared 2D projection adds out-of-plane restraints at other nodes.
            // Match the actual support-reaction subset, rather than the original input supports.
            string[] reactionNodes = result.GetProperty("analysisResultSet").GetProperty("results")
                .EnumerateArray().SelectMany(item => item.GetProperty("support_reactions").EnumerateArray())
                .Select(row => row.GetProperty("node_id").GetString()!).Distinct().Order().ToArray();
            Assert.Equal(reactionNodes, lines.Skip(1).Select(cells => cells[2]).Distinct().Order());
        }
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

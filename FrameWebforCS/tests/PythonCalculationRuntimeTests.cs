using System.IO.Pipes;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FrameWebforCS.calculation;
using Xunit;

namespace FrameWebforCS.Tests;

[CollectionDefinition("Python runtime", DisableParallelization = true)]
public sealed class PythonRuntimeCollection;

[Collection("Python runtime")]
public sealed class PythonCalculationRuntimeTests
{
    private static readonly string RepositoryRoot = FindRoot();

    private const string Request = """
        {
          "node":{"30":{"x":2,"y":0,"z":0},"10":{"x":0,"y":0,"z":0}},
          "member":{"7":{"ni":10,"nj":30,"e":1,"cg":0}},
          "element":{"1":{"1":{"E":10000,"G":4000,"A":1,"Iy":1,"Iz":1,"J":1}}},
          "fix_node":{"1":[
            {"n":10,"tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1},
            {"n":30,"tx":0,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}
          ]},
          "load":{
            "positive":{"symbol":"P","element":1,"fix_node":1,"load_node":[{"n":30,"tx":4}]},
            "negative":{"symbol":"N","element":1,"fix_node":1,"load_node":[{"n":30,"tx":-6}]}
          }
        }
        """;

    [Fact]
    public async Task RealPythonCanSolveTwiceAndShutDown()
    {
        await using (var runtime = new PythonCalculationRuntime(RepositoryRoot))
        {
            for (int iteration = 0; iteration < 2; iteration++)
            {
                string json = await runtime.CalculateAsync(Request).WaitAsync(TimeSpan.FromSeconds(30));
                using JsonDocument document = JsonDocument.Parse(json);
                Assert.Equal("analysis_result_set", document.RootElement.GetProperty("kind").GetString());
                Assert.Equal("1.0", document.RootElement.GetProperty("schema_version").GetString());
                Assert.Equal(new[] { "positive", "negative" }, document.RootElement.GetProperty("cases")
                    .EnumerateArray().Select(item => item.GetProperty("case_id").GetString()));
            }
            const string saved2D = """
                {"dimension":2,
                 "node":{"1":{"x":0,"y":0},"2":{"x":1,"y":0}},
                 "member":{"1":{"ni":"1","nj":"2","e":"1"}},
                 "element":{"1":{"1":{"E":10000,"A":1,"Iz":1}}},
                 "fix_node":{"1":[{"n":"1","tx":1,"ty":1,"rz":1}]},
                 "load":{"1":{"symbol":"DL","fix_node":1,"element":1,
                   "load_node":[{"row":1,"n":"2","tx":4}]}}}
                """;
            CalculationRequest projected = CalculationRequestBuilder.FromSavedJson(saved2D);
            string bridgeJson = await runtime.CalculateAsync(projected.Json)
                .WaitAsync(TimeSpan.FromSeconds(30));
            AnalysisResultSet strict = AnalysisResultSetJson.Deserialize(Encoding.UTF8.GetBytes(bridgeJson));
            Assert.Equal("1", Assert.Single(strict.Cases).CaseId);
            Assert.IsType<StaticAnalysisResult>(Assert.Single(strict.Results));
            await VerifyLlSweepMatchesCantileverEquilibrium(runtime);
            await VerifyLlSweepWithEmptyInitialPositionKeepsMovingGroup(runtime);
            PythonCalculationException conversion = await Assert.ThrowsAsync<PythonCalculationException>(
                () => runtime.CalculateAsync("{bad"));
            Assert.Equal(PythonCalculationStage.Conversion, conversion.Stage);
            PythonCalculationException solver = await Assert.ThrowsAsync<PythonCalculationException>(
                () => runtime.CalculateAsync("{}"));
            Assert.Equal(PythonCalculationStage.Solver, solver.Stage);
            using var cancellation = new CancellationTokenSource();
            Task<string> abandoned = runtime.CalculateAsync(Request, cancellation.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned);
            await runtime.WaitForIdleAsync().WaitAsync(TimeSpan.FromSeconds(30));
        }
        await using var second = new PythonCalculationRuntime(RepositoryRoot);
        PythonCalculationException error = await Assert.ThrowsAsync<PythonCalculationException>(
            () => second.CalculateAsync(Request));
        Assert.Equal(PythonCalculationStage.Initialization, error.Stage);
        Assert.Contains("only once", error.Message);
    }

    private static async Task VerifyLlSweepMatchesCantileverEquilibrium(PythonCalculationRuntime runtime)
    {
        const string saved = """
            {"dimension":2,
             "node":{"1":{"x":0,"y":0},"2":{"x":2,"y":0}},
             "member":{"1":{"ni":"1","nj":"2","e":"1"}},
             "element":{"1":{"1":{"E":10000,"A":1,"Iz":1}}},
             "fix_node":{"1":[{"n":"1","tx":1,"ty":1,"rz":1}]},
             "load":{"1":{"symbol":"LL","fix_node":1,"element":1,"LL_pitch":0.5,
               "load_member":[{"row":1,"m1":"1","m2":"1","direction":"y",
                 "mark":"1","L1":"0.5","L2":"0","P1":10,"P2":0}]}}}
            """;
        CalculationRequest projected = CalculationRequestBuilder.FromSavedJson(saved);
        using JsonDocument request = JsonDocument.Parse(projected.Json);
        string[] caseIds = request.RootElement.GetProperty("load").EnumerateObject()
            .Select(item => item.Name).ToArray();
        Assert.True(caseIds.Length >= 3, string.Join(",", caseIds));
        Assert.Equal("1", caseIds[0]);

        string json = await runtime.CalculateAsync(projected.Json).WaitAsync(TimeSpan.FromSeconds(30));
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(Encoding.UTF8.GetBytes(json));
        Assert.Equal(caseIds, result.Cases.Select(item => item.CaseId));
        double[] tipDisplacements = result.Results.Cast<StaticAnalysisResult>()
            .Select(item => item.NodeDisplacements.Single(row => row.NodeId == "2").Components.Dy)
            .ToArray();
        Assert.All(tipDisplacements, value => Assert.True(double.IsFinite(value)));
        // Cantilever tip displacement under a point load at station a:
        // P a^2 (3L - a) / (6 E I), with L = 2 m, E I = 10,000.
        foreach ((int index, double station) in new[] { 0.5, 1.0, 1.5 }.Index())
        {
            double expected = 10 * station * station * (6 - station) / 60_000;
            Assert.Equal(expected, Math.Abs(tipDisplacements[index]), 8);
            var support = ((StaticAnalysisResult)result.Results[index]).SupportReactions
                .Single(row => row.NodeId == "1");
            Assert.Equal(10, Math.Abs(support.Components.Fy), 8);
            Assert.Equal(10 * station, Math.Abs(support.Components.Mz), 8);
        }
    }

    private static async Task VerifyLlSweepWithEmptyInitialPositionKeepsMovingGroup(
        PythonCalculationRuntime runtime)
    {
        const string saved = """
            {"dimension":2,
             "node":{"1":{"x":0,"y":0},"2":{"x":2,"y":0}},
             "member":{"1":{"ni":"1","nj":"2","e":"1"}},
             "element":{"1":{"1":{"E":10000,"A":1,"Iz":1}}},
             "fix_node":{"1":[{"n":"1","tx":1,"ty":1,"rz":1}]},
             "load":{
               "1":{"symbol":"DL","fix_node":1,"element":1,
                 "load_node":[{"row":1,"n":"2","tx":1}]},
               "2":{"symbol":"LL","fix_node":1,"element":1,"LL_pitch":0.5,
                 "load_member":[{"row":1,"m1":"1","m2":"1","direction":"y",
                   "mark":"1","L1":"0","L2":"0","P1":10,"P2":0}]}}}
            """;
        CalculationRequest projected = CalculationRequestBuilder.FromSavedJson(saved);
        using JsonDocument request = JsonDocument.Parse(projected.Json);
        string[] caseIds = request.RootElement.GetProperty("load").EnumerateObject()
            .Select(item => item.Name).ToArray();
        Assert.Contains("1", caseIds);
        Assert.Contains("2", caseIds);
        Assert.Contains(caseIds, id => id.StartsWith("2.", StringComparison.Ordinal));

        string json = await runtime.CalculateAsync(projected.Json).WaitAsync(TimeSpan.FromSeconds(30));
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(Encoding.UTF8.GetBytes(json));
        CalculationResultPresentation presentation = new(result, dimension: 2);
        CalculationResultPage page = Assert.Single(presentation.Pages,
            item => item.Case.CaseId == "2");
        Assert.NotEmpty(page.MovingChildren);
        var support = ((StaticAnalysisResult)page.Result).SupportReactions
            .Single(row => row.NodeId == "1");
        Assert.Equal(10, Math.Abs(support.Components.Fy), 8);
        Assert.Equal(5, Math.Abs(support.Components.Mz), 8);
    }

    [Fact]
    public async Task MissingEnvironmentFailsBeforeImport()
    {
        string empty = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        await using var runtime = new PythonCalculationRuntime(empty);
        PythonCalculationException error = await Assert.ThrowsAsync<PythonCalculationException>(
            () => runtime.CalculateAsync(Request));
        Assert.Equal(PythonCalculationStage.Environment, error.Stage);
    }

    [Fact]
    public async Task ImportFailureIsClassifiedInIsolatedProcess()
    {
        if (Environment.GetEnvironmentVariable("FRAMEWEB_IMPORT_FAILURE_WORKER") == "1")
        {
            await using var runtime = new PythonCalculationRuntime(RepositoryRoot, null,
                "fem.missing_calculation_module");
            PythonCalculationException error = await Assert.ThrowsAsync<PythonCalculationException>(
                () => runtime.CalculateAsync(Request));
            Assert.Equal(PythonCalculationStage.Import, error.Stage);
            return;
        }

        string project = Path.Combine(RepositoryRoot, "FrameWebforCS.Tests", "FrameWebforCS.Tests.csproj");
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[] { "test", project, "--no-build", "--no-restore",
            "--filter", "FullyQualifiedName~ImportFailureIsClassifiedInIsolatedProcess", "-v:q" })
            start.ArgumentList.Add(argument);
        start.Environment["FRAMEWEB_IMPORT_FAILURE_WORKER"] = "1";
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Test worker did not start.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The isolated Python import test timed out.");
        }
        Assert.True(process.ExitCode == 0,
            $"Isolated Python import test failed. {await output} {await errors}");
    }

    [Fact]
    public async Task PipeGateAcknowledgesAttachThenBreakpoint()
    {
        int pid = Environment.ProcessId;
        await using var gate = new CalculationDebugPipeGate(TimeSpan.FromSeconds(5));
        await Acknowledge(gate.WaitForAttachAsync(pid, CancellationToken.None), "ATTACH", pid);
        await Acknowledge(gate.WaitForBreakpointAsync(pid, CancellationToken.None), "BREAKPOINT", pid);
    }

    [Fact]
    public async Task PipeGateRequiresMatchingPidAndStage()
    {
        int pid = Environment.ProcessId;
        await using var gate = new CalculationDebugPipeGate(TimeSpan.FromSeconds(5));
        Task waiting = gate.WaitForAttachAsync(pid, CancellationToken.None);
        using var client = new NamedPipeClientStream(".", CalculationDebugPipeGate.PipeNameForProcess(pid),
            PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);
        using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);
        using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        Assert.Equal($"READY ATTACH {pid}", await reader.ReadLineAsync());
        await writer.WriteLineAsync($"RELEASE BREAKPOINT {pid}");
        await Assert.ThrowsAsync<InvalidOperationException>(() => waiting);
    }

    [Fact]
    public async Task CanceledDebugWaitDoesNotAcknowledgeAttach()
    {
        using var cancellation = new CancellationTokenSource();
        await using var gate = new CalculationDebugPipeGate();
        int pid = Environment.ProcessId;
        Task waiting = gate.WaitForAttachAsync(pid, cancellation.Token);
        using var client = new NamedPipeClientStream(".", CalculationDebugPipeGate.PipeNameForProcess(pid),
            PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);
        using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);
        Assert.Equal($"READY ATTACH {pid}", await reader.ReadLineAsync());
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
    }

    private static string FindRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (Directory.Exists(Path.Combine(dir.FullName, "FrameWeb"))) return dir.FullName;
        throw new InvalidOperationException("Repository root was not found.");
    }

    private static async Task Acknowledge(Task waiting, string stage, int pid)
    {
        using var client = new NamedPipeClientStream(".", CalculationDebugPipeGate.PipeNameForProcess(pid),
            PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);
        using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);
        using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        Assert.Equal($"READY {stage} {pid}", await reader.ReadLineAsync());
        await writer.WriteLineAsync($"RELEASE {stage} {pid}");
        Assert.Equal($"ACK {stage} {pid}", await reader.ReadLineAsync());
        await waiting;
    }
}

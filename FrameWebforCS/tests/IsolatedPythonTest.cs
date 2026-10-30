using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace FrameWebforCS.Tests;

// CPython has one lifetime per process, so a test that solves through PythonCalculationRuntime
// reruns itself alone in a child test host (as NoticeStationSurvivesTheActualPythonSolver does).
internal static class IsolatedPythonTest
{
    private const string WorkerVariable = "FRAMEWEB_ISOLATED_PYTHON_TEST";

    /// <summary>True in the parent after the child ran the test and passed; false inside the child.</summary>
    internal static async Task<bool> RanInChildAsync(Type testClass, TimeSpan timeout,
        [CallerMemberName] string method = "")
    {
        string name = testClass.FullName + "." + method;
        if (Environment.GetEnvironmentVariable(WorkerVariable) == name) return false;
        string evidence = Path.Combine(Path.GetTempPath(), "frameweb-isolated-python", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(evidence);
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (string argument in new[] { "vstest", testClass.Assembly.Location,
            "/ResultsDirectory:" + evidence, "/Logger:trx;LogFileName=child.trx",
            "/TestCaseFilter:FullyQualifiedName=" + name })
            start.ArgumentList.Add(argument);
        start.Environment[WorkerVariable] = name;
        using var process = Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout);
        try { await process.WaitForExitAsync(cancellation.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new TimeoutException($"{name} timed out in its child test host.");
        }
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        string trx = Path.Combine(evidence, "child.trx");
        var report = File.Exists(trx) ? XDocument.Load(trx) : null;
        var counters = report?.Descendants(ns + "Counters").SingleOrDefault();
        if ((string?)counters?.Attribute("executed") != "1")
            throw new InvalidOperationException($"{name} did not run in its child test host.\n{await output}\n{await errors}");
        var result = report!.Descendants(ns + "UnitTestResult").Single();
        string message = string.Join("\n", result.Descendants(ns + "Message").Select(item => item.Value)
            .Concat(result.Descendants(ns + "StdOut").Select(item => item.Value)));
        Assert.True((string?)result.Attribute("outcome") == "Passed" && process.ExitCode == 0,
            $"{name} failed in its child test host:\n{message}");
        if (message.Length > 0) Console.WriteLine(message);
        return true;
    }
}

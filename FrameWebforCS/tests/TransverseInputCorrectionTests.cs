using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Xml.Linq;
using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.three;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class TransverseInputCorrectionTests
{
    [Theory]
    [InlineData("true")]
    [InlineData("\"unexpected\"")]
    public void SavedRowsCannotSupplyThePrivateRelativeZeroLengthMarker(string marker)
    {
        var saved = Snapshot();
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse("""
            [{"row":1,"m1":"1","direction":"y","mark":"2","L1":"1","L2":"0","P1":1,"P2":1}]
            """);
        string expected = CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json;
        saved["load"]!["1"]!["load_member"]![0]!["_relativeZeroLength"] = JsonNode.Parse(marker);
        string? actual = null;
        var error = Record.Exception(() => actual = CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json);
        Assert.Null(error);
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("_relativeZeroLength", actual!);
    }

    [Theory]
    [InlineData("-0", 0, 10, 0)]
    [InlineData("-0.0004", 9, 1, 8)]
    [InlineData("-0.0005", 8.999, 1.001, 7.999)]
    [InlineData("-0.0006", 8.999, 1.001, 7.999)]
    public void NegativeDistributedLengthRetainsItsMeaningWhenRoundedToZero(
        string length, double firstMargin, double nextStart, double nextMargin)
    {
        var saved = Snapshot();
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse($$"""
            [{"row":1,"m1":"1","direction":"y","mark":"2","L1":"1","L2":"{{length}}","P1":1,"P2":1},
             {"row":2,"m1":"1","direction":"y","mark":"2","L1":"-0","L2":"-1","P1":1,"P2":1}]
            """);
        string original = saved.ToJsonString();
        var rows = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(original).Json)!["load"]!["1"]!["load_member"]!.AsArray();
        Assert.Equal(2, rows.Count);
        Assert.Equal(1, (double)rows[0]!["L1"]!);
        Assert.Equal(firstMargin, (double)rows[0]!["L2"]!);
        Assert.Equal(nextStart, (double)rows[1]!["L1"]!);
        Assert.Equal(nextMargin, (double)rows[1]!["L2"]!);
        Assert.Equal(original, saved.ToJsonString());
    }

    // Captured from actual Angular InputLoadService.getMemberLoadJson(0):
    // physical spans are rounded individually to whole mm before carrying.
    [Theory]
    [InlineData(3, 1, 1, 2.067, 6.900)]
    [InlineData(4, 1, 1, 0, 8.967)]
    [InlineData(3, 0, 2, 0, 8.967)]
    [InlineData(3, 2, 0, 0, 8.967)]
    public void DistributedFractionalMillimetreSpansRoundBeforeRelativeCarry(
        int thirdRow, int first, int last, double start, double margin)
    {
        var saved = FractionalSpanSnapshot();
        var third = saved["load"]!["1"]!["load_member"]![2]!;
        third["row"] = thirdRow; third["m1"] = first.ToString(); third["m2"] = last.ToString();
        string original = saved.ToJsonString();
        var request = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(original).Json)!;
        var rows = request["load"]!["1"]!["load_member"]!.AsArray();
        Assert.Equal(8.967, (double)rows[0]!["L2"]!);
        Assert.Equal(7.933, (double)rows[1]!["L2"]!);
        Assert.Equal(start, (double)rows[2]!["L1"]!);
        Assert.Equal(margin, (double)rows[2]!["L2"]!);
        Assert.Equal(original, saved.ToJsonString());
        var service = InputLoadService.Instance; service.clear();
        try
        {
            using var document = JsonDocument.Parse(original); service.setLoadJson(document.RootElement);
            var displayed = LoadCaseExpansion.ExpandMemberLoads("1", service.GetDisplaySnapshot()["1"].MemberLoads,
                new Dictionary<int, float> { [1] = 10, [2] = 10 })["1"].Single(row => row.Row == thirdRow);
            Assert.Equal((float)start, displayed.L1);
            Assert.Equal((float)margin, displayed.L2);
        }
        finally { service.clear(); }
    }

    [Theory]
    [InlineData(1, "-0.0625", "-0.1", 1.062, 1.162)]
    [InlineData(11, "-0.0625", "-0.1", 1.062, 1.162)]
    [InlineData(2, "-0.0625", "-0.1", 1.063, 8.837)]
    [InlineData(1, "0.0625", "-0.1", 0.063, 0.163)]
    [InlineData(2, "0.0625", "-0.1", 0.063, 9.837)]
    [InlineData(1, "-0", "-0.0625", 0, 0.062)]
    [InlineData(11, "-0", "-0.0625", 0, 0.062)]
    [InlineData(2, "-0", "-0.0625", 1, 8.937)]
    public void SignedHalfMillimetresPreserveAngularPointAndPhysicalCarryRules(
        int mark, string l1, string l2, double start, double endOrMargin)
    {
        var saved = Snapshot();
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse($$"""
            [{"row":1,"m1":"1","direction":"y","mark":"{{mark}}","L1":"0.1","L2":"{{(mark == 2 ? "-0.9" : "1")}}","P1":1,"P2":1},
             {"row":2,"m1":"1","direction":"y","mark":"{{mark}}","L1":"{{l1}}","L2":"{{l2}}","P1":1,"P2":1}]
            """);
        var rows = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!["load"]!["1"]!["load_member"]!.AsArray();
        Assert.Equal(start, (double)rows[1]!["L1"]!);
        Assert.Equal(endOrMargin, (double)rows[1]!["L2"]!);
    }

    [Fact]
    public void DistributedCarryUsesWholeMillimetreLengthWithoutRoundingGeometry()
    {
        var saved = Snapshot(); saved["node"]!["2"]!["x"] = 10.0004;
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse("""
            [{"row":1,"m1":"1","direction":"y","mark":"2","L1":"0","L2":"5","P1":1,"P2":1},
             {"row":2,"m1":"1","direction":"y","mark":"2","L1":"-0.0004","L2":"-1","P1":1,"P2":1}]
            """);
        var request = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!;
        Assert.Equal(10.0004, (double)request["node"]!["2"]!["x"]!);
        var second = request["load"]!["1"]!["load_member"]![1]!;
        Assert.Equal(5, (double)second["L1"]!);
        Assert.Equal(4, (double)second["L2"]!);
    }

    private static JsonObject FractionalSpanSnapshot()
    {
        var saved = Snapshot(); saved["load"]!["1"]!.AsObject().Remove("load_node");
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse("""
            [{"row":1,"m1":"1","direction":"y","mark":"2","L1":"0","L2":"-1.0333","P1":1,"P2":1},
             {"row":2,"m1":"1","direction":"y","mark":"2","L1":"-0","L2":"-1.034","P1":1,"P2":1},
             {"row":3,"m1":"1","direction":"y","mark":"2","L1":"-0","L2":"-1.0333","P1":1,"P2":1}]
            """);
        return saved;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void PointRowsCarryThePreviousRawL2IndependentlyOfPhysicalDistributedEnd(int mark)
    {
        var saved = Snapshot();
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse($$"""
            [{"row":1,"m1":"1","direction":"y","mark":"2","L1":"0.1","L2":"2","P1":1,"P2":1},
             {"row":2,"m1":"1","direction":"y","mark":"{{mark}}","L1":"-0.1","L2":"0","P1":1,"P2":0}]
            """);
        var rows = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!["load"]!["1"]!["load_member"]!.AsArray();
        Assert.Equal(2.1, (double)rows[1]!["L1"]!); // Angular raw L2=2, not physical end=8.
    }

    [Fact]
    public void NegativeHalfMillimetreDistributedClippingUsesTheSignedNumericStage()
    {
        var saved = Snapshot();
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse("""
            [{"row":1,"m1":"1","direction":"y","mark":"2","L1":"-0.0625","L2":"-0.1","P1":0,"P2":100}]
            """);
        var row = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!["load"]!["1"]!["load_member"]![0]!;
        Assert.Equal(0, (double)row["L1"]!);
        Assert.Equal(9.962, (double)row["L2"]!);
        Assert.Equal(62, (double)row["P1"]!); // Math.round(-62.5)=-62 before interpolation.
    }

    [Fact]
    public void TemperatureRowsRetainFullMemberExtentAndLexicalSaveData()
    {
        var saved = Snapshot();
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse("""
            [{"row":1,"m1":"1","m2":"1","direction":"x","mark":"9","L1":"0","L2":"0","P1":12,"P2":12},
             {"row":2,"m1":"1","m2":"1","direction":"x","mark":"9","L1":"-0","L2":"0","P1":15,"P2":15}]
            """);
        var request = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!;
        var rows = (JsonArray)request["load"]!["1"]!["load_member"]!;
        Assert.Equal(2, rows.Count);
        Assert.All(rows.OfType<JsonObject>(), row =>
        {
            Assert.Equal(0, (double)row["L1"]!);
            Assert.Equal(0, (double)row["L2"]!);
            Assert.Equal((double)row["P1"]!, (double)row["P2"]!);
        });
        var service = InputLoadService.Instance;
        service.clear();
        try
        {
            using var source = JsonDocument.Parse(saved.ToJsonString());
            service.setLoadJson(source.RootElement);
            var displayed = LoadCaseExpansion.ExpandMemberLoads("1", service.GetDisplaySnapshot()["1"].MemberLoads,
                new Dictionary<int, float> { [1] = 10 })["1"];
            Assert.Equal(2, displayed.Count);
            Assert.All(displayed, row => { Assert.Equal(0, row.L1); Assert.Equal(0, row.L2); });
            Assert.Equal("-0", saved["load"]!["1"]!["load_member"]![1]!["L1"]!.GetValue<string>());
        }
        finally { service.clear(); }
    }

    [Fact]
    public async Task NoticeStationSurvivesTheActualPythonSolverWithPhysicalResults()
    {
        // CPython has one lifetime per process. Keep this regression independent
        // of PythonCalculationRuntimeTests and the application's singleton state.
        if (Environment.GetEnvironmentVariable("FRAMEWEB_NOTICE_STATION_WORKER") != "1")
        {
            string evidence = Path.Combine(PythonEnvironment.FindRepositoryRoot(AppContext.BaseDirectory),
                ".tmp", "transverse-analysis-print-corrections", "native", "notice-station-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(evidence);
            var start = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            foreach (string argument in new[] { "vstest", typeof(TransverseInputCorrectionTests).Assembly.Location,
                "/ResultsDirectory:" + evidence, "/Logger:trx;LogFileName=station.trx",
                "/TestCaseFilter:FullyQualifiedName=FrameWebforCS.Tests.TransverseInputCorrectionTests.NoticeStationSurvivesTheActualPythonSolverWithPhysicalResults" })
                start.ArgumentList.Add(argument);
            start.Environment["FRAMEWEB_NOTICE_STATION_WORKER"] = "1";
            using var process = Process.Start(start)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> errors = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                throw new TimeoutException("The isolated notice station solver timed out.");
            }
            finally
            {
                Console.WriteLine(await output);
                Console.Error.WriteLine(await errors);
                File.WriteAllText(Path.Combine(evidence, "stdout.log"), await output);
                File.WriteAllText(Path.Combine(evidence, "stderr.log"), await errors);
            }
            string trx = Path.Combine(evidence, "station.trx");
            Console.WriteLine("Isolated station assertion evidence: " + trx);
            AcceptStationChild(XDocument.Load(trx), process.ExitCode);
            return;
        }
        var saved = Snapshot();
        string root = PythonEnvironment.FindRepositoryRoot(AppContext.BaseDirectory);
        await using var runtime = new PythonCalculationRuntime(root);
        string baseline = await runtime.CalculateAsync(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)
            .WaitAsync(TimeSpan.FromSeconds(30));
        using var before = JsonDocument.Parse(baseline);
        Assert.Equal(2, before.RootElement.GetProperty("topology").GetProperty("members")[0]
            .GetProperty("stations").GetArrayLength());
        saved["notice_points"] = JsonNode.Parse("[{\"m\":1,\"Points\":[0.566,2]}]");
        string solved = await runtime.CalculateAsync(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)
            .WaitAsync(TimeSpan.FromSeconds(30));
        using var after = JsonDocument.Parse(solved);
        var topology = after.RootElement.GetProperty("topology");
        Assert.Equal(new[] { 0, 0.566, 2, 10 }, topology.GetProperty("members")[0].GetProperty("stations")
            .EnumerateArray().Select(row => row.GetProperty("position").GetDouble()));
        string generated = topology.GetProperty("nodes").EnumerateArray()
            .Single(node => Math.Abs(node.GetProperty("coordinates").GetProperty("x").GetDouble() - 0.566) < 1e-10)
            .GetProperty("node_id").GetString()!;
        var result = after.RootElement.GetProperty("results")[0];
        var displacement = result.GetProperty("node_displacements").EnumerateArray()
            .Single(row => row.GetProperty("node_id").GetString() == generated);
        Assert.Equal(4 * 0.566 / 10000, displacement.GetProperty("components").GetProperty("dx").GetDouble(), 10);
        var forces = result.GetProperty("member_section_forces").EnumerateArray()
            .Single(row => row.GetProperty("member_id").GetString() == "1");
        Assert.Equal(new[] { 0.566, 1.434, 8 }, forces.GetProperty("segments").EnumerateArray()
            .Select(segment => segment.GetProperty("length").GetDouble()).ToArray(), new DoubleToleranceComparer());
        Assert.All(forces.GetProperty("segments").EnumerateArray(), segment =>
            Assert.Equal(4, segment.GetProperty("i_end").GetProperty("fx").GetDouble(), 8));
        var fractional = FractionalSpanSnapshot();
        fractional["fix_member"] = JsonNode.Parse("""
            {"1":[{"row":1,"m":"1","length":3.1,"ty":5},{"row":2,"m":"1","ty":0}]}
            """);
        string fractionalSolved = await runtime.CalculateAsync(CalculationRequestBuilder.FromSavedJson(fractional.ToJsonString()).Json)
            .WaitAsync(TimeSpan.FromSeconds(30));
        using var fractionalResult = JsonDocument.Parse(fractionalSolved);
        var stations = fractionalResult.RootElement.GetProperty("topology").GetProperty("members")[0]
            .GetProperty("stations").EnumerateArray().Select(station => station.GetProperty("position").GetDouble()).ToArray();
        Assert.Equal(5, stations.Length); // One shared load/spring end; no extra 3.101 station.
        Assert.Equal(new[] { 0, 1.033, 2.067, 3.1, 10 }, stations, new DoubleToleranceComparer());
    }

    private sealed class DoubleToleranceComparer : IEqualityComparer<double>
    {
        public bool Equals(double left, double right) => Math.Abs(left - right) < 1e-10;
        public int GetHashCode(double value) => 0;
    }

    private const string StationTestName = "FrameWebforCS.Tests.TransverseInputCorrectionTests.NoticeStationSurvivesTheActualPythonSolverWithPhysicalResults";

    private static void AcceptStationChild(XDocument? report, int exitCode)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var results = report?.Descendants(ns + "UnitTestResult").ToArray() ?? [];
        var counts = report?.Descendants(ns + "Counters").SingleOrDefault();
        if (results.Length != 1 || counts == null || (string?)counts.Attribute("total") != "1" ||
            (string?)counts.Attribute("executed") != "1")
            throw new InvalidOperationException("The station child did not execute exactly one selected test.");
        var result = results[0];
        string? outcome = (string?)result.Attribute("outcome");
        string? testId = (string?)result.Attribute("testId");
        var definition = report!.Descendants(ns + "UnitTest").SingleOrDefault(test => (string?)test.Attribute("id") == testId);
        string? adapter = (string?)definition?.Element(ns + "TestMethod")?.Attribute("adapterTypeName");
        if ((string?)result.Attribute("testName") != StationTestName ||
            adapter?.StartsWith("executor://xunit/", StringComparison.Ordinal) != true)
            throw new InvalidOperationException("The station child did not execute the expected xUnit test.");
        if (outcome == "Passed" && exitCode == 0 && (string?)counts.Attribute("passed") == "1" &&
            (string?)counts.Attribute("failed") == "0") return;
        var error = result.Element(ns + "Output")?.Element(ns + "ErrorInfo");
        string detail = error?.Value ?? "";
        string message = error?.Element(ns + "Message")?.Value ?? "";
        if (outcome != "Failed" || exitCode != 1 || (string?)counts.Attribute("failed") != "1" ||
            (string?)counts.Attribute("passed") != "0" ||
            !(detail.Contains("Xunit.Sdk.", StringComparison.Ordinal) ||
              System.Text.RegularExpressions.Regex.IsMatch(message, @"^Assert\.[A-Za-z]+\(.*\) Failure")))
            throw new InvalidOperationException("The station child failed without a valid xUnit assertion: " + detail);
        Assert.Fail("Assert.Fail() Failure: isolated station assertion. " + detail);
    }

    private static XDocument StationChildFixture(string outcome = "Passed", string message = "")
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        return new XDocument(new XElement(ns + "TestRun",
            new XElement(ns + "Results", new XElement(ns + "UnitTestResult",
                new XAttribute("testId", "station"), new XAttribute("testName", StationTestName), new XAttribute("outcome", outcome),
                new XElement(ns + "Output", new XElement(ns + "ErrorInfo", new XElement(ns + "Message", message))))),
            new XElement(ns + "TestDefinitions", new XElement(ns + "UnitTest", new XAttribute("id", "station"),
                new XElement(ns + "TestMethod", new XAttribute("adapterTypeName", "executor://xunit/VsTestRunner2/netcore/")))),
            new XElement(ns + "ResultSummary", new XElement(ns + "Counters", new XAttribute("total", 1),
                new XAttribute("executed", 1), new XAttribute("passed", outcome == "Passed" ? 1 : 0),
                new XAttribute("failed", outcome == "Failed" ? 1 : 0)))));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("skipped")]
    [InlineData("unknown")]
    [InlineData("runtime")]
    [InlineData("build")]
    [InlineData("adapter")]
    public void StationChildInfrastructureCannotBecomeAProductAssertion(string scenario)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        XDocument? report = StationChildFixture();
        int exitCode = 0;
        switch (scenario)
        {
            case "missing": report = null; break;
            case "empty": report.Descendants(ns + "UnitTestResult").Remove(); break;
            case "skipped": report.Descendants(ns + "Counters").Single().SetAttributeValue("executed", 0); break;
            case "unknown": report.Descendants(ns + "UnitTestResult").Single().SetAttributeValue("testName", "Other"); break;
            case "runtime": report = StationChildFixture("Failed", "System.InvalidOperationException: runtime failed."); exitCode = 1; break;
            case "build": exitCode = 1; break;
            case "adapter": report.Descendants(ns + "TestMethod").Single().SetAttributeValue("adapterTypeName", "unknown"); break;
        }
        Assert.Throws<InvalidOperationException>(() => AcceptStationChild(report, exitCode));
    }

    [Fact]
    public void StationChildOnlyPropagatesAnExecutedNamedXunitAssertion()
    {
        const string message = "Assert.Equal() Failure: expected physical station value.";
        var error = Assert.Throws<Xunit.Sdk.FailException>(() => AcceptStationChild(StationChildFixture("Failed", message), 1));
        Assert.Contains(message, error.Message);
        AcceptStationChild(StationChildFixture(), 0);
    }

    [Theory]
    [InlineData(2, "-0", 2, 1, 4, 6)]
    [InlineData(2, "0", 2, 1, 0, 2)]
    [InlineData(2, "-1", 2, 1, 5, 7)]
    [InlineData(2, "-0", 4, 1, 0, 2)]
    [InlineData(2, "-0", 2, 2, 0, 2)]
    [InlineData(2, "-1", 2, 2, 0, 1)]
    [InlineData(1, "-0", 2, 1, 0, 2)]
    [InlineData(11, "-0", 2, 1, 0, 2)]
    [InlineData(1, "-1", 2, 1, 5, 7)]
    [InlineData(11, "-1", 2, 1, 5, 7)]
    public void SavedLexicalDistancesProjectToTheSamePhysicalIntervalsAsDisplay(
        int mark, string l1, int rowNumber, int member, double start, double end)
        => CheckPhysicalIntervals(mark, l1, rowNumber, member, start, end);

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    public void ZeroMemberEndpointAliasesResetContinuationLikeTheirEffectiveMember(int first, int last)
        => CheckPhysicalIntervals(2, "-0", 2, 2, 0, 2, first, last);

    private static void CheckPhysicalIntervals(int mark, string l1, int rowNumber, int member,
        double start, double end, int? firstAlias = null, int? lastAlias = null)
    {
        var saved = Snapshot();
        saved["load"]!["1"]!["load_member"] = JsonNode.Parse($$"""
            [{"row":1,"m1":"1","m2":"1","direction":"y","mark":"{{mark}}","L1":"1","L2":"{{(mark == 2 ? "-3" : "4")}}","P1":10,"P2":10},
             {"row":{{rowNumber}},"m1":"{{firstAlias ?? member}}","m2":"{{lastAlias ?? member}}","direction":"y","mark":"{{mark}}","L1":"{{l1}}","L2":"-2","P1":20,"P2":20}]
            """);
        var request = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!;
        var projected = ((JsonArray)request["load"]!["1"]!["load_member"]!).OfType<JsonObject>()
            .Single(row => (int?)row["row"] == rowNumber);
        Assert.Equal(start, (double)projected["L1"]!, 3);
        Assert.Equal(end, mark == 2 ? 10 - (double)projected["L2"]! : (double)projected["L2"]!, 3);
        var service = InputLoadService.Instance;
        service.clear();
        try
        {
            using var document = JsonDocument.Parse(saved.ToJsonString());
            service.setLoadJson(document.RootElement);
            var before = JsonSerializer.Serialize(service.getLoadJson());
            var source = service.GetDisplaySnapshot()["1"].MemberLoads;
            var displayed = LoadCaseExpansion.ExpandMemberLoads("1", source,
                new Dictionary<int, float> { [1] = 10, [2] = 10 })["1"]
                .Single(row => row.Row == rowNumber);
            Assert.Equal(start, displayed.L1, 3);
            Assert.Equal(end, mark == 2 ? 10 - displayed.L2 : displayed.L2, 3);
            var after = JsonSerializer.Serialize(service.getLoadJson());
            Assert.Equal(before, after);
            using var reloaded = JsonDocument.Parse("{\"load\":" + after + "}");
            service.setLoadJson(reloaded.RootElement);
            using var roundTrip = JsonDocument.Parse(JsonSerializer.Serialize(service.getLoadJson()));
            Assert.Equal(l1, roundTrip.RootElement.GetProperty("1").GetProperty("load_member")[1]
                .GetProperty("L1").GetString());
        }
        finally { service.clear(); }
    }

    [Fact]
    public void NoticeArraysAreDetachedAndRetainedIncludingEmptyArray()
    {
        foreach (string notices in new[] { "[]", "[{\"m\":1,\"Points\":[0.566,2]}]" })
        {
            var saved = Snapshot();
            saved["notice_points"] = JsonNode.Parse(notices);
            var request = JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()).Json)!;
            Assert.IsType<JsonArray>(request["notice_points"]);
            Assert.True(JsonNode.DeepEquals(saved["notice_points"], request["notice_points"]));
            ((JsonArray)request["notice_points"]!).Add(new JsonObject());
            Assert.Equal(notices, saved["notice_points"]!.ToJsonString());
        }
        Assert.Null(JsonNode.Parse(CalculationRequestBuilder.FromSavedJson(Snapshot().ToJsonString()).Json)!["notice_points"]);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("17")]
    [InlineData("[null]")]
    [InlineData("[{\"m\":1,\"Points\":{}}]")]
    [InlineData("[{\"m\":1,\"Points\":[\"bad\"]}]")]
    public void MalformedNoticeDataRaisesExplicitRequestError(string notices)
    {
        var saved = Snapshot();
        saved["notice_points"] = JsonNode.Parse(notices);
        var error = Assert.Throws<CalculationRequestException>(() =>
            CalculationRequestBuilder.FromSavedJson(saved.ToJsonString()));
        Assert.Contains("notice_points", error.Message);
    }

    private static JsonObject Snapshot() => (JsonObject)JsonNode.Parse("""
        {"dimension":3,
         "node":{"1":{"x":0,"y":0,"z":0},"2":{"x":10,"y":0,"z":0},"3":{"x":20,"y":0,"z":0}},
         "member":{"1":{"ni":"1","nj":"2","e":"1"},"2":{"ni":"2","nj":"3","e":"1"}},
         "element":{"1":{"1":{"E":10000,"G":4000,"A":1,"J":1,"Iy":1,"Iz":1}}},
         "fix_node":{"1":[{"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]},
         "load":{"1":{"symbol":"DL","fix_node":1,"element":1,"load_node":[{"row":1,"n":"3","tx":4}]}}}
        """)!;
}

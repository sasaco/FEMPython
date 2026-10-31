using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    // Step 4 acceptance: the P7 model entered on the input screens' services, starting from its
    // linear part, saves every nonlinear field of the file and solves to the recorded peak.
    [Fact]
    public async Task P7EnteredOnTheInputScreensSavesTheFileAndSolvesToTheRecordedPeak()
    {
        if (await IsolatedPythonTest.RanInChildAsync(typeof(NonlinearDesktopAcceptanceTests), TimeSpan.FromMinutes(5))) return;
        JsonObject source = JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();
        CalculationRequest request;
        try
        {
            Open(LinearPart(source).ToJsonString());
            InputDataService.Instance.SetAnalysisMode(2, true);
            EnterLoadKinds(source);
            EnterCombination(source);
            EnterSkeletons(source);
            EnterSprings(source);

            JsonObject saved = JsonNode.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()))!.AsObject();
            Assert.True(saved["nonlinear"]!.GetValue<bool>());
            Assert.True(JsonNode.DeepEquals(source["nonlinear_combine"], saved["nonlinear_combine"]));
            foreach (var (id, load) in source["load"]!.AsObject())
                foreach (string key in new[] { "kind", "limit" })
                    Assert.True(JsonNode.DeepEquals(load![key], saved["load"]![id]![key]), $"load {id} {key}");
            foreach (var (sheet, materials) in source["element"]!.AsObject())
                foreach (var (id, material) in materials!.AsObject())
                    Assert.True(JsonNode.DeepEquals(material!["nonlinear"], saved["element"]![sheet]![id]!["nonlinear"]),
                        $"material {sheet}-{id}");
            foreach (var (sheet, rows) in source["fix_node"]!.AsObject())
                for (int i = 0; i < rows!.AsArray().Count; i++)
                    foreach (var (component, value) in rows[i]!.AsObject())
                        if (value is JsonObject)
                            Assert.True(JsonNode.DeepEquals(value, saved["fix_node"]![sheet]![i]![component]),
                                $"support {sheet}-{i + 1} {component}");
            request = InputDataService.Instance.CreateCalculationRequest();
        }
        finally { Open("{}"); }
        await using var runtime = new PythonCalculationRuntime(NonlinearCalculationEngineTests.RepositoryRoot());
        string json = await runtime.CalculateAsync(request.Json).WaitAsync(TimeSpan.FromMinutes(5));
        double peak = NonlinearCalculationEngineTests.PeakBaseShear(json);
        output.WriteLine($"input-screen path peak base shear {peak:R} kN (raw file 10701.4)");
        Assert.Equal(10701.4, peak, 1);
    }

    // The file without anything that belongs only to the 2D非線形 mode. A spring's component is
    // fixed in the linear model.
    private static JsonObject LinearPart(JsonObject source)
    {
        JsonObject linear = source.DeepClone().AsObject();
        linear.Remove("nonlinear");
        linear.Remove("nonlinear_combine");
        foreach (var (_, load) in linear["load"]!.AsObject())
        {
            load!.AsObject().Remove("kind");
            load.AsObject().Remove("limit");
        }
        foreach (var (_, materials) in linear["element"]!.AsObject())
            foreach (var (_, material) in materials!.AsObject())
                material!.AsObject().Remove("nonlinear");
        foreach (var (_, rows) in linear["fix_node"]!.AsObject())
            foreach (var row in rows!.AsArray())
                foreach (var (component, value) in row!.AsObject().ToArray())
                    if (value is JsonObject) row[component] = 1.0;
        return linear;
    }

    private static void EnterLoadKinds(JsonObject source)
    {
        foreach (var (id, load) in source["load"]!.AsObject())
        {
            clsLoadNameRow row = InputLoadService.Instance.LoadNames[int.Parse(id) - 1];
            row.kind = load!["kind"]!.GetValue<string>();
            if (load["limit"] is JsonNode limit) row.limit = limit.GetValue<double>();
        }
    }

    private static void EnterCombination(JsonObject source)
    {
        var service = InputCombineService.Instance;
        foreach (var (id, value) in source["nonlinear_combine"]!.AsObject())
        {
            JsonObject combination = value!.AsObject();
            int row = int.Parse(id);
            service.SetNonlinearName(row, combination["name"]!.GetValue<string>());
            foreach (var (key, coefficient) in combination.Where(item => item.Key.StartsWith('C')))
                service.SetNonlinearCoefficient(row, int.Parse(key[1..]), coefficient!.GetValue<double>());
            JsonObject control = combination["displacement_control"]!.AsObject();
            JsonArray targets = control["targets"]!.AsArray();
            service.SetNonlinearMethod(row, NonlinearMethod.Displacement);
            service.SetNonlinearControlNode(row, control["node"]!.GetValue<int>());
            service.SetNonlinearTarget(row, targets[^1]!.GetValue<double>());
            service.SetNonlinearDivisions(row, targets.Count);
            service.SetNonlinearMaxIterations(row, combination["max_iterations"]!.GetValue<int>());
            service.SetNonlinearTolerance(row, combination["tolerance"]!.GetValue<double>());
        }
    }

    private static void EnterSkeletons(JsonObject source)
    {
        foreach (var (sheet, materials) in source["element"]!.AsObject())
            foreach (var (id, material) in materials!.AsObject())
            {
                if (material!["nonlinear"] is not JsonObject skeleton) continue;
                clsElement row = InputElementsService.Instance.GetRows(int.Parse(sheet))[int.Parse(id) - 1];
                row.Skeleton = skeleton.ContainsKey("theta_1") ? clsElement.RotationSkeleton : clsElement.CurvatureSkeleton;
                string deformation = skeleton.ContainsKey("theta_1") ? "theta_" : "delta_";
                double? Point(string key) => skeleton[key]?.GetValue<double>();
                row.M1 = Point("P_1"); row.D1 = Point(deformation + "1");
                row.M2 = Point("P_2"); row.D2 = Point(deformation + "2");
                row.M3 = Point("P_3"); row.D3 = Point(deformation + "3");
                row.M4 = Point("P_4"); row.D4 = Point(deformation + "4");
            }
    }

    private static void EnterSprings(JsonObject source)
    {
        foreach (var (sheet, rows) in source["fix_node"]!.AsObject())
            for (int i = 0; i < rows!.AsArray().Count; i++)
                foreach (var (component, value) in rows[i]!.AsObject())
                {
                    if (value is not JsonObject spring) continue;
                    clsFixNode row = InputFixNodeService.Instance.GetRows(sheet)[i];
                    double? Value(string key) => spring[key]?.GetValue<double>();
                    row.SpringComponent = component;
                    row.SpringK1 = Value("K1_pos") ?? Value("K1");
                    row.SpringF1Pos = Value("F1_pos");
                    row.SpringF1Neg = Value("F1_neg");
                    row.SpringK2 = Value("K2_pos");
                    row.SpringF2Pos = Value("F2_pos");
                    row.SpringF2Neg = Value("F2_neg");
                    row.SpringK3 = Value("K3_pos");
                    row.SpringK1Neg = Value("K1_neg");
                    row.SpringK2Neg = Value("K2_neg");
                    row.SpringK3Neg = Value("K3_neg");
                }
    }

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
}

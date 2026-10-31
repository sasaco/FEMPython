using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// GLOSSARY: 非線形解析の組合せ. A row is entered as 変位増分 (control node, signed target,
// divisions) or 荷重増分 (max factor JALF, divisions MDV) and saved as uniform lists.
[Collection("DisplacementSingletons")]
public sealed class NonlinearCombineSheetTests
{
    [Fact]
    public void TheP7RowShowsAsADisplacementIncrementOf300Divisions()
    {
        try
        {
            Open(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()));
            clsNonlinearCombine row = InputCombineService.Instance.NonlinearRows[1];

            Assert.Equal(NonlinearMethod.Displacement, row.Method);
            Assert.Equal(2, row.ControlNode);
            Assert.Equal(-0.3, row.Target);
            Assert.Equal(300, row.Divisions);
            Assert.Null(row.MaxFactor);
            Assert.False(row.IsIndividual);
            Assert.Equal(200, row.MaxIterations);
            Assert.Equal(1e-6, row.Tolerance);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void EnteringTheP7RowSavesTheP7Combination()
    {
        try
        {
            JsonObject source = JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();
            JsonNode expected = source["nonlinear_combine"]!.DeepClone();
            source["nonlinear_combine"] = new JsonObject();
            Open(source.ToJsonString());
            var service = InputCombineService.Instance;

            service.SetNonlinearName(1, "線路方向_Case1");
            for (int basicCase = 1; basicCase <= 7; basicCase++)
                service.SetNonlinearCoefficient(1, basicCase, 1);
            service.SetNonlinearMethod(1, NonlinearMethod.Displacement);
            service.SetNonlinearControlNode(1, 2);
            // Divisions before the target: the list is written once both are known.
            service.SetNonlinearDivisions(1, 300);
            service.SetNonlinearTarget(1, -0.3);
            service.SetNonlinearMaxIterations(1, 200);
            service.SetNonlinearTolerance(1, 1e-6);

            Assert.True(JsonNode.DeepEquals(expected, Save()["nonlinear_combine"]));
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void ALoadIncrementRowWithoutAMaxFactorGoesUpToOne()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true}""");
            var service = InputCombineService.Instance;

            service.SetNonlinearMethod(1, NonlinearMethod.Load);
            service.SetNonlinearDivisions(1, 4);
            Assert.Equal(4, Row()["n_load_steps"]!.GetValue<int>());

            service.SetNonlinearMaxFactor(1, 2);
            Assert.Equal([0.5, 1.0, 1.5, 2.0], Row()["load_factors"]!.AsArray().Select(value => value!.GetValue<double>()));
            Assert.False(Row().ContainsKey("n_load_steps"));

            service.SetNonlinearMaxFactor(1, null);
            Assert.Equal(4, Row()["n_load_steps"]!.GetValue<int>());
            Assert.False(Row().ContainsKey("load_factors"));
        }
        finally { Open("{}"); }

        static JsonObject Row() => Save()["nonlinear_combine"]!["1"]!.AsObject();
    }

    [Fact]
    public void ANonUniformListShowsAsIndividualAndIsKeptUntilItsRowIsRedivided()
    {
        try
        {
            Open("""
                {"dimension":2,"nonlinear":true,"nonlinear_combine":{"1":{"row":1,"C1":1,
                 "displacement_control":{"node":2,"dof":"dx","targets":[-0.01,-0.05,-0.2]}}}}
                """);
            var service = InputCombineService.Instance;
            clsNonlinearCombine row = service.NonlinearRows[1];
            Assert.True(row.IsIndividual);
            Assert.Null(row.Target);
            Assert.Equal(3, row.Divisions);

            service.SetNonlinearName(1, "renamed");
            Assert.Equal([-0.01, -0.05, -0.2], Targets());

            service.SetNonlinearDivisions(1, 4);
            Assert.Equal([-0.05, -0.1, -0.15000000000000002, -0.2], Targets());
            Assert.False(service.NonlinearRows[1].IsIndividual);
        }
        finally { Open("{}"); }

        static double[] Targets() => Save()["nonlinear_combine"]!["1"]!["displacement_control"]!["targets"]!
            .AsArray().Select(value => value!.GetValue<double>()).ToArray();
    }

    [Fact]
    public void EntriesTheEngineWouldRejectAreRefusedWithoutChangingTheRow()
    {
        try
        {
            Open(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()));
            var service = InputCombineService.Instance;
            JsonNode before = Save()["nonlinear_combine"]!;

            Assert.Throws<ArgumentException>(() => service.SetNonlinearMaxFactor(1, 2));
            Assert.Throws<ArgumentException>(() => service.SetNonlinearTarget(1, 0));
            Assert.Throws<ArgumentException>(() => service.SetNonlinearDivisions(1, 0));
            Assert.Throws<ArgumentException>(() => service.SetNonlinearTolerance(1, 0));
            Assert.Throws<ArgumentException>(() => service.SetNonlinearMaxIterations(1, -1));
            Assert.Throws<ArgumentException>(() => service.SetNonlinearControlNode(2, 2));
            Assert.True(JsonNode.DeepEquals(before, Save()["nonlinear_combine"]));

            // Blank iterations and tolerance are the engine defaults (50, 1e-6).
            service.SetNonlinearMaxIterations(1, null);
            service.SetNonlinearTolerance(1, null);
            JsonObject row = Save()["nonlinear_combine"]!["1"]!.AsObject();
            Assert.False(row.ContainsKey("max_iterations"));
            Assert.False(row.ContainsKey("tolerance"));

            // Switching the method drops the other method's settings.
            service.SetNonlinearMethod(1, NonlinearMethod.Load);
            row = Save()["nonlinear_combine"]!["1"]!.AsObject();
            Assert.False(row.ContainsKey("displacement_control"));
            Assert.Equal(300, row["n_load_steps"]!.GetValue<int>());
        }
        finally { Open("{}"); }
    }

    private static JsonObject Save() =>
        JsonNode.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()))!.AsObject();

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
}

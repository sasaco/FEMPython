using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// GLOSSARY: 骨格曲線. A material row is 線形, M-φ or M-θ with four points (M1..M4 with φ1..φ4
// or θ1..θ4), saved as the engine's jr_stiffness_reduction object.
[Collection("DisplacementSingletons")]
public sealed class NonlinearElementSkeletonTests
{
    [Fact]
    public void TheP7MaterialsShowAsLinearAndMPhi()
    {
        try
        {
            JsonObject source = JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();
            JsonObject skeleton = source["element"]!["1"]!["2"]!["nonlinear"]!.AsObject();
            Open(source.ToJsonString());

            Assert.Equal(clsElement.LinearSkeleton, Row(1).Skeleton);
            clsElement row = Row(2);
            Assert.Equal(clsElement.CurvatureSkeleton, row.Skeleton);
            Assert.Equal(skeleton["P_1"]!.GetValue<double>(), row.M1);
            Assert.Equal(skeleton["P_4"]!.GetValue<double>(), row.M4);
            Assert.Equal(skeleton["delta_1"]!.GetValue<double>(), row.D1);
            Assert.Equal(skeleton["delta_4"]!.GetValue<double>(), row.D4);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void EnteringTheP7SkeletonSavesTheP7Material()
    {
        try
        {
            JsonObject source = JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();
            JsonObject expected = source["element"]!["1"]!["2"]!["nonlinear"]!.AsObject();
            JsonObject material = source["element"]!["1"]!["2"]!.AsObject();
            material.Remove("nonlinear");
            Open(source.ToJsonString());
            clsElement row = Row(2);

            row.Skeleton = clsElement.CurvatureSkeleton;
            row.M1 = expected["P_1"]!.GetValue<double>();
            row.D1 = expected["delta_1"]!.GetValue<double>();
            row.M2 = expected["P_2"]!.GetValue<double>();
            row.D2 = expected["delta_2"]!.GetValue<double>();
            row.M3 = expected["P_3"]!.GetValue<double>();
            row.D3 = expected["delta_3"]!.GetValue<double>();
            row.M4 = expected["P_4"]!.GetValue<double>();
            row.D4 = expected["delta_4"]!.GetValue<double>();

            Assert.True(JsonNode.DeepEquals(expected, Save()["element"]!["1"]!["2"]!["nonlinear"]));
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void AnMThetaRowSavesRotationsAndSwitchingKindKeepsOnlyTheMoments()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true,"element":{"1":{"1":{"E":1,"A":1,"Iz":1}}}}""");
            clsElement row = Row(1);

            row.Skeleton = clsElement.RotationSkeleton;
            row.M1 = 5;
            row.D1 = 0.0005;
            JsonObject saved = Saved();
            Assert.Equal("m_theta", saved["skeleton"]!.GetValue<string>());
            Assert.Equal(0.0005, saved["theta_1"]!.GetValue<double>());
            Assert.Equal(5, saved["P_1"]!.GetValue<double>());

            row.Skeleton = clsElement.CurvatureSkeleton;
            saved = Saved();
            Assert.False(saved.ContainsKey("skeleton"));
            Assert.False(saved.ContainsKey("theta_1"));
            Assert.False(saved.ContainsKey("delta_1"));
            Assert.Equal(5, saved["P_1"]!.GetValue<double>());

            row.Skeleton = clsElement.LinearSkeleton;
            Assert.False(Save()["element"]!["1"]!["1"]!.AsObject().ContainsKey("nonlinear"));
            Assert.Null(row.M1);
        }
        finally { Open("{}"); }

        static JsonObject Saved() => Save()["element"]!["1"]!["1"]!["nonlinear"]!.AsObject();
    }

    [Fact]
    public void PointsAreRefusedOnALinearRowAndNonFiniteValuesAreRefused()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true,"element":{"1":{"1":{"E":1,"A":1,"Iz":1}}}}""");
            clsElement row = Row(1);

            row.M1 = 5;
            Assert.Null(row.M1);
            Assert.Null(row.Nonlinear);

            row.Skeleton = clsElement.CurvatureSkeleton;
            row.M1 = 5;
            row.M1 = double.NaN;
            row.D1 = double.PositiveInfinity;
            Assert.Equal(5, row.M1);
            Assert.Null(row.D1);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void AMaterialTheScreenCannotShowIsKeptAsWritten()
    {
        try
        {
            const string laws = """{"laws":{"moment_z":{"type":"bilinear","k1":1,"k2":0.1,"yield":2}}}""";
            Open("""{"dimension":2,"nonlinear":true,"element":{"1":{"1":{"E":1,"A":1,"Iz":1,"nonlinear":""" +
                laws + "}}}}");
            clsElement row = Row(1);

            Assert.Equal(clsElement.IndividualSkeleton, row.Skeleton);
            row.M1 = 5;
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(laws), Save()["element"]!["1"]!["1"]!["nonlinear"]));
        }
        finally { Open("{}"); }
    }

    private static clsElement Row(int id) => InputElementsService.Instance.GetRows(1)[id - 1];

    private static JsonObject Save() =>
        JsonNode.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()))!.AsObject();

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
}

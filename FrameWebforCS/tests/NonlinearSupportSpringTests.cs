using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// A support row's 非線形ばね is entered in JRSNAP order (K1, +F1, −F1, K2, +F2, −F2, K3, K1−,
// K2−, K3−) on one component; the filled values decide jrsnap_bilinear or jrsnap_trilinear.
[Collection("DisplacementSingletons")]
public sealed class NonlinearSupportSpringTests
{
    [Fact]
    public void TheP7SupportShowsItsRotationalSpringInJrsnapOrder()
    {
        try
        {
            JsonObject source = JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();
            JsonObject spring = source["fix_node"]!["1"]![0]!["rz"]!.AsObject();
            Open(source.ToJsonString());
            clsFixNode row = Row();

            Assert.Equal("rz", row.SpringComponent);
            Assert.Equal(
                new[] { "K1_pos", "F1_pos", "F1_neg", "K2_pos", "F2_pos", "F2_neg", "K3_pos", "K1_neg", "K2_neg", "K3_neg" }
                    .Select(key => (double?)spring[key]!.GetValue<double>()),
                new[] { row.SpringK1, row.SpringF1Pos, row.SpringF1Neg, row.SpringK2, row.SpringF2Pos,
                    row.SpringF2Neg, row.SpringK3, row.SpringK1Neg, row.SpringK2Neg, row.SpringK3Neg });
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void EnteringTheP7SpringSavesTheP7Support()
    {
        try
        {
            JsonObject source = JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();
            JsonObject support = source["fix_node"]!["1"]![0]!.AsObject();
            JsonObject expected = support["rz"]!.AsObject();
            support["rz"] = 1.0;
            Open(source.ToJsonString());
            clsFixNode row = Row();

            row.SpringComponent = "rz";
            row.SpringK1 = Value("K1_pos");
            row.SpringF1Pos = Value("F1_pos");
            row.SpringF1Neg = Value("F1_neg");
            row.SpringK2 = Value("K2_pos");
            row.SpringF2Pos = Value("F2_pos");
            row.SpringF2Neg = Value("F2_neg");
            row.SpringK3 = Value("K3_pos");
            row.SpringK1Neg = Value("K1_neg");
            row.SpringK2Neg = Value("K2_neg");
            row.SpringK3Neg = Value("K3_neg");

            // The spring takes the place of the component's number.
            Assert.Null(row.Rz);
            Assert.True(JsonNode.DeepEquals(expected, Save()["fix_node"]!["1"]![0]!["rz"]));

            double Value(string key) => expected[key]!.GetValue<double>();
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void K1AndTheFirstYieldForcesAloneMakeABilinearSpring()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true,"fix_node":{"1":[{"row":1,"n":1,"tx":1,"ty":1}]}}""");
            clsFixNode row = Row();

            row.SpringComponent = "tx";
            row.SpringK1 = 100;
            row.SpringF1Pos = 5;
            row.SpringF1Neg = 4;
            Assert.True(JsonNode.DeepEquals(
                JsonNode.Parse("""{"type":"jrsnap_bilinear","K1":100,"F1_pos":5,"F1_neg":4}"""), Spring()));

            row.SpringK2 = 10;
            Assert.True(JsonNode.DeepEquals(
                JsonNode.Parse("""{"type":"jrsnap_trilinear","K1_pos":100,"F1_pos":5,"F1_neg":4,"K2_pos":10}"""),
                Spring()));

            row.SpringK2 = null;
            Assert.Equal("jrsnap_bilinear", Spring()["type"]!.GetValue<string>());

            row.SpringComponent = null;
            Assert.Null(Save()["fix_node"]!["1"]![0]!["tx"]);
            Assert.Null(row.SpringK1);
        }
        finally { Open("{}"); }

        static JsonNode Spring() => Save()["fix_node"]!["1"]![0]!["tx"]!;
    }

    [Fact]
    public void ValuesNeedASpringComponentAndAFiniteNumber()
    {
        try
        {
            Open("""{"dimension":2,"nonlinear":true,"fix_node":{"1":[{"row":1,"n":1,"tx":1}]}}""");
            clsFixNode row = Row();

            row.SpringK1 = 100;
            Assert.Null(row.SpringK1);

            row.SpringComponent = "tx";
            row.SpringK1 = 100;
            row.SpringK1 = double.NaN;
            Assert.Equal(100, row.SpringK1);

            row.SpringComponent = "bad";
            Assert.Equal("tx", row.SpringComponent);
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void ASpringTheSheetCannotShowIsKeptAsWritten()
    {
        try
        {
            const string slip = """{"type":"jrsnap_slip","K1":1,"F1":2}""";
            Open("""{"dimension":2,"nonlinear":true,"fix_node":{"1":[{"row":1,"n":1,"tx":""" + slip +
                 ""","rz":{"type":"jrsnap_bilinear","K1":1,"F1_pos":1,"F1_neg":1}}]}}""");
            clsFixNode row = Row();

            Assert.Equal(clsFixNode.IndividualSpring, row.SpringComponent);
            row.SpringK1 = 5;
            row.SpringComponent = "ty";
            JsonObject saved = Save()["fix_node"]!["1"]![0]!.AsObject();
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(slip), saved["tx"]));
            Assert.Equal(1, saved["rz"]!["K1"]!.GetValue<double>());
        }
        finally { Open("{}"); }
    }

    private static clsFixNode Row() => InputFixNodeService.Instance.GetRows("1")[0];

    private static JsonObject Save() =>
        JsonNode.Parse(JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson()))!.AsObject();

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
}

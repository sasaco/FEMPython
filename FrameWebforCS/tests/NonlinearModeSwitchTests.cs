using FrameWebforCS.providers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// GLOSSARY: 2D非線形モード. A model is either linear or nonlinear: switching lists the data
// that belongs only to the mode being left (for the confirmation dialog), then removes it.
[Collection("DisplacementSingletons")]
public sealed class NonlinearModeSwitchTests
{
    [Fact]
    public void SwitchingALinearModelToNonlinearRemovesDefineCombineAndPickup()
    {
        var input = InputDataService.Instance;
        try
        {
            Open("""
                {"dimension":3,"node":{"1":{"x":0,"y":0},"2":{"x":1,"y":0}},
                 "load":{"1":{"name":"a","symbol":"A"}},
                 "define":{"1":{"row":1,"C1":1}},
                 "combine":{"1":{"row":1,"name":"c","C1":1.5}},
                 "pickup":{"1":{"row":1,"name":"p","C1":1}}}
                """);

            Assert.Equal(["DEFINE", "COMBINE", "PICKUP"], input.DataRemovedBySwitching(nonlinear: true));
            Assert.Empty(input.DataRemovedBySwitching(nonlinear: false));

            input.SetAnalysisMode(dimension: 2, nonlinear: true);

            Assert.True(input.IsNonlinear);
            Assert.Equal(2, input.dimension);
            JsonObject saved = Save();
            Assert.True(saved["nonlinear"]!.GetValue<bool>());
            Assert.Empty(saved["nonlinear_combine"]!.AsObject());
            Assert.False(saved.ContainsKey("define"));
            Assert.False(saved.ContainsKey("combine"));
            Assert.False(saved.ContainsKey("pickup"));
            Assert.Equal("a", saved["load"]!["1"]!["name"]!.GetValue<string>());
            Assert.Empty(input.DataRemovedBySwitching(nonlinear: true));
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void SwitchingTheP7ModelToLinearRemovesOnlyTheNonlinearData()
    {
        var input = InputDataService.Instance;
        try
        {
            JsonObject source = JsonNode.Parse(File.ReadAllText(NonlinearInputPersistenceTests.P7Path()))!.AsObject();
            Open(source.ToJsonString());
            JsonObject before = Save();

            Assert.Equal(["荷重の種類・限定倍率", "非線形解析の組合せ", "材料の骨格曲線", "支点の非線形ばね"],
                input.DataRemovedBySwitching(nonlinear: false));

            input.SetAnalysisMode(dimension: 3, nonlinear: false);

            Assert.False(input.IsNonlinear);
            Assert.Equal(3, input.dimension);
            JsonObject saved = Save();
            Assert.False(saved.ContainsKey("nonlinear"));
            Assert.False(saved.ContainsKey("nonlinear_combine"));
            Assert.Empty(saved["combine"]!.AsObject());
            foreach (var (id, loadCase) in saved["load"]!.AsObject())
            {
                Assert.False(loadCase!.AsObject().ContainsKey("kind"));
                Assert.False(loadCase.AsObject().ContainsKey("limit"));
                Assert.True(JsonNode.DeepEquals(before["load"]![id]!["load_member"], loadCase["load_member"]));
            }
            foreach (var (_, sheet) in saved["element"]!.AsObject())
                foreach (var (_, row) in sheet!.AsObject())
                    Assert.False(row!.AsObject().ContainsKey("nonlinear"));
            Assert.Equal(before["element"]!["1"]!["2"]!["E"]!.GetValue<float>(),
                saved["element"]!["1"]!["2"]!["E"]!.GetValue<float>());
            // A spring replaced its component's number; without it the component is free.
            JsonObject support = saved["fix_node"]!["1"]![0]!.AsObject();
            Assert.Null(support["rz"]);
            Assert.Equal(before["fix_node"]!["1"]![0]!["tx"]!.GetValue<float>(), support["tx"]!.GetValue<float>());
            Assert.Empty(input.DataRemovedBySwitching(nonlinear: false));
        }
        finally { Open("{}"); }
    }

    [Fact]
    public void ANonlinearModelMustBeTwoDimensional()
    {
        var input = InputDataService.Instance;
        try
        {
            Open("""{"dimension":3,"combine":{"1":{"row":1,"C1":1}}}""");
            Assert.Throws<ArgumentOutOfRangeException>(() => input.SetAnalysisMode(dimension: 3, nonlinear: true));
            Assert.False(input.IsNonlinear);
            Assert.Equal(3, input.dimension);
            Assert.Single(Save()["combine"]!.AsObject());
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

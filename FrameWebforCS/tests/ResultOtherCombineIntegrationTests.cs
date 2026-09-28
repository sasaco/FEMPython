using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultOtherCombineIntegrationTests
{
    [Fact]
    public void MalformedForceAndReactionCandidatesPreservePreviousResults()
    {
        var fsec = ResultFsecService.Instance;
        var reac = ResultReacService.Instance;
        try
        {
            using var valid = JsonDocument.Parse("""
                {"result":{"1":{"fsec":{"2":{"P1":{"fxi":3,"L":1,"dummyi":true}}},
                                  "reac":{"4":{"tx":5}}}}}
                """);
            fsec.setFsecJson(valid.RootElement);
            reac.setReacJson(valid.RootElement);

            using var badForce = JsonDocument.Parse("""
                {"result":{"1":{"fsec":{"2":{"P1":{"fxi":7},"P2":{"fxi":"bad"}}}}}}
                """);
            using var badReaction = JsonDocument.Parse("""
                {"result":{"1":{"reac":{"4":{"tx":7},"5":{"tx":"bad"}}}}}
                """);
            Assert.Throws<JsonException>(() => fsec.setFsecJson(badForce.RootElement));
            Assert.Throws<JsonException>(() => reac.setReacJson(badReaction.RootElement));
            Assert.Equal(3, fsec.getFsec()["1"]["2"]["P1"].fxi);
            Assert.Equal(5, reac.getReac()["1"]["4"].tx);
        }
        finally
        {
            fsec.clear();
            reac.clear();
        }
    }

    [Fact]
    public void FileLoadPublishesAndInvalidatesSectionForceAndReactionTogether()
    {
        var fsec = ResultCombineFsecCoordinator.Instance;
        var reac = ResultCombineReacCoordinator.Instance;
        try
        {
            Open("""
                {"dimension":2,"load":{"1":{"name":"first"}},
                 "combine":{"7":{"row":1,"C1":2}},
                 "result":{"1":{"disg":{"1":{"dx":1}},
                                  "fsec":{"1":{"0":{"fxi":2,"fyi":3,"mzi":4}}},
                                  "reac":{"1":{"tx":5,"ty":6,"mz":7}}}}}
                """);
            Assert.Equal(CombineFsecState.Valid, fsec.State);
            Assert.Equal(CombineReacState.Valid, reac.State);
            Assert.NotNull(fsec.Snapshot);
            Assert.NotNull(reac.Snapshot);

            Open("""{"dimension":2,"load":{"1":{"name":"input only"}},"combine":{"7":{"row":1,"C1":2}}}""");
            Assert.Equal(CombineFsecState.Invalid, fsec.State);
            Assert.Equal(CombineReacState.Invalid, reac.State);
            Assert.Null(fsec.Snapshot);
            Assert.Null(reac.Snapshot);

            InputCombineService.Instance.SetCombineCoefficient(1, 1, 3);
            Assert.Equal(CombineFsecState.Invalid, fsec.State);
            Assert.Equal(CombineReacState.Invalid, reac.State);
        }
        finally
        {
            fsec.FailLoad();
            reac.FailLoad();
            ResultCombineDisgCoordinator.Instance.FailLoad();
            InputCombineService.Instance.clear();
            ResultFsecService.Instance.clear();
            ResultReacService.Instance.clear();
            ResultDisgService.Instance.clear();
        }
    }

    private static void Open(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }
}

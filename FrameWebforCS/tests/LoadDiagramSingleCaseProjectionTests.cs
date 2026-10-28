using FrameWebforCS.providers.printing;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

/// <summary>
/// 荷重図の 2 段書きで 1 頁に 1 ケースにするケース（FrameG の荷重図の書式「n.k」の k ケース）を、印刷用 JSON の
/// diagramInput.single_layout_cases（荷重の並びの添字）に写す。
/// </summary>
public sealed class LoadDiagramSingleCaseProjectionTests
{
    [Fact]
    public void SingleCaseIdsBecomeLoadOrderIndices()
    {
        JsonObject root = Project(new([PrintOption.LoadDiagram], Layout: PrintLayout.SplitHorizontal,
            LoadDiagramSingleCaseIds: ["2", "1", "9"]));
        JsonObject input = root["PrintLoad"]!["diagramInput"]!.AsObject();
        Assert.Equal("splitHorizontal", input["layout"]!.GetValue<string>());
        // 荷重に無いケース（9）は除く。添字は荷重の並び（10・1・2）による
        Assert.Equal([2, 1], input["single_layout_cases"]!.AsArray().Select(node => node!.GetValue<int>()).ToArray());
    }

    [Fact]
    public void WithoutSingleCasesTheLoadDiagramSettingsAreUnchanged()
    {
        JsonObject root = Project(new([PrintOption.LoadDiagram], Layout: PrintLayout.SplitHorizontal));
        Assert.Null(root["PrintLoad"]!["diagramInput"]!["single_layout_cases"]);
    }

    [Fact]
    public void BlankSingleCaseIdsAreRejected()
    {
        Assert.Throws<PrintProjectionException>(() => Project(
            new([PrintOption.LoadDiagram], LoadDiagramSingleCaseIds: [" "])));
    }

    private static JsonObject Project(PrintSelection selection) => JsonNode.Parse(PrintProjection.Build(new PrintSnapshot(
        selection, Saved().ToJsonString(), 1, 1, null, null, [])))!.AsObject();

    private static JsonObject Saved() => JsonNode.Parse("""
        {"dimension":2,"node":{"1":{"x":0,"y":0,"z":0},"2":{"x":1,"y":0,"z":0}},
         "member":{"1":{"ni":"1","nj":"2","e":"1"}},"shell":{},"element":{},
         "load":{"10":{"name":"Wind"},"1":{"name":"Dead load"},"2":{"name":"Live load"}},
         "notice_points":[],"rigid":[]}
        """)!.AsObject();
}

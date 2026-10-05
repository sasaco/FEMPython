using FrameWebforCS.providers;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputFileLoaderTests
{
    [Fact]
    public void FrdIsConvertedToLoadableJson()
    {
        string fileName = Path.Combine(RepositoryRoot(), "FrameGConverter", "Convert_Test",
            "TestData", "4_2-FrameG.frd");

        using JsonDocument document = InputFileLoader.Open(fileName);

        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.True(document.RootElement.TryGetProperty("node", out JsonElement nodes));
        Assert.Equal(JsonValueKind.Object, nodes.ValueKind);
        Assert.NotEmpty(nodes.EnumerateObject());
        Assert.False(document.RootElement.GetProperty("fix_node").TryGetProperty("7", out _));
        Assert.Equal(2, document.RootElement.GetProperty("dimension").GetInt32());

        try
        {
            InputDataService.Instance.JsonDataOpen(document.RootElement);
            Assert.Equal(2, InputDataService.Instance.dimension);
            Assert.NotEmpty(InputDataService.Instance.GetSaveJson()["node"] as
                System.Collections.IDictionary ?? throw new InvalidOperationException("Nodes were not saved."));
        }
        finally
        {
            using JsonDocument empty = JsonDocument.Parse("{}");
            InputDataService.Instance.JsonDataOpen(empty.RootElement);
        }
    }

    [Fact]
    public void PileSpringFrdReportsUnsupportedMemberLoadMark()
    {
        string fileName = Path.Combine(RepositoryRoot(), "FrameGConverter", "Convert_Test",
            "TestData", "バネ連衡あり.frd");

        using JsonDocument document = InputFileLoader.Open(fileName);
        try
        {
            InputDataService.Instance.JsonDataOpen(document.RootElement);
            var error = Assert.Throws<FrameWebforCS.calculation.CalculationRequestException>(
                () => InputDataService.Instance.CreateCalculationRequest());
            Assert.Contains("load.2.load_member row 1", error.Message);
            Assert.Contains("mark 14", error.Message);
        }
        finally
        {
            using JsonDocument empty = JsonDocument.Parse("{}");
            InputDataService.Instance.JsonDataOpen(empty.RootElement);
        }
    }

    [Theory]
    [InlineData(".json")]
    [InlineData(".ndt")]
    public void OtherSupportedExtensionsReadJsonDirectly(string extension)
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);
        try
        {
            File.WriteAllText(fileName, """{"dimension":3,"node":{"1":{"x":1}}}""");
            using JsonDocument document = InputFileLoader.Open(fileName);
            Assert.Equal(3, document.RootElement.GetProperty("dimension").GetInt32());
            Assert.Equal(1, document.RootElement.GetProperty("node").GetProperty("1")
                .GetProperty("x").GetInt32());
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameGConverter")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}

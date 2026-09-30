using System.Text;
using PDF_Manager;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace FrameWebforCS.Tests;

public sealed class PrintLibraryTests
{
    private const string SmallJob = """
        {"dimension":3,"language":"ja","pageSize":"A4","pageOrientation":"Vertical", "title":"印刷試験", "hasPrintInputData":false,"hasPrintCalculation":false}
        """;

    [Fact]
    public void DirectGeneratorProducesParseablePdfFromLegacyRoot()
    {
        var bytes = DirectPdfGenerator.Generate(SmallJob);

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        Assert.Equal(1, pdf.PageCount);
    }

    [Fact]
    public void DirectGeneratorSupportsRepeatedIndependentJobs()
    {
        var first = DirectPdfGenerator.Generate(SmallJob);
        var second = DirectPdfGenerator.Generate(SmallJob);

        using var firstPdf = PdfReader.Open(new MemoryStream(first), PdfDocumentOpenMode.Import);
        using var secondPdf = PdfReader.Open(new MemoryStream(second), PdfDocumentOpenMode.Import);
        Assert.Equal(firstPdf.PageCount, secondPdf.PageCount);
        Assert.Equal(firstPdf.Pages[0].Width, secondPdf.Pages[0].Width);
        Assert.Equal(firstPdf.Pages[0].Height, secondPdf.Pages[0].Height);
    }

    [Fact]
    public void DirectGeneratorRejectsOversizedAndMalformedRequests()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DirectPdfGenerator.Generate(new string('x', DirectPdfGenerator.MaxRequestBytes + 1)));
        Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => DirectPdfGenerator.Generate("{"));
    }

    [Fact]
    public void DirectGeneratorHonorsPreCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() => DirectPdfGenerator.Generate(SmallJob, source.Token));
    }

    [Fact]
    public void UpgradedImageSharpCanEmbedDiagramPng()
    {
        var png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "PrintLibraryLogo.png"));
        using var document = new PdfSharpCore.Pdf.PdfDocument();
        var page = document.AddPage();
        using var graphics = XGraphics.FromPdfPage(page);
        using var image = XImage.FromStream(() => new MemoryStream(png));
        graphics.DrawImage(image, 10, 10, 24, 24);
        using var output = new MemoryStream();
        document.Save(output, false);

        using var parsed = PdfReader.Open(new MemoryStream(output.ToArray()), PdfDocumentOpenMode.Import);
        Assert.Single(parsed.Pages);
    }

    [Fact]
    public void DirectGeneratorEmbedsLegacyThreeDimensionalDiagram()
    {
        var png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "PrintLibraryLogo.png"));
        var job = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            dimension = 3,
            pageSize = "A4",
            pageOrientation = "Vertical",
            PrintScreenData = new[]
            {
                new
                {
                    mode = "print_load",
                    title1 = "荷重図",
                    result = new[]
                    {
                        new { src = "data:image/png;base64," + Convert.ToBase64String(png), title = "case 1", type = "load" }
                    }
                }
            }
        });

        var bytes = DirectPdfGenerator.Generate(job);

        using var parsed = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.Single(parsed.Pages);
        Assert.Contains("/XObject", Encoding.ASCII.GetString(bytes));
    }

    [Fact]
    public void PageLimitRejectsBeforeThirdPageAndClosesPartialDocument()
    {
        int before = PDF_Manager.Printing.PdfDocument.ActiveDocumentCount;
        string job = ScreenJob(3);

        var failure = Assert.Throws<InvalidOperationException>(() =>
            new PrintInput(job).GetPdfBytes(maxPages: 2, maxBytes: DirectPdfGenerator.MaxPdfBytes));

        Assert.Contains("page limit", failure.Message);
        Assert.Equal(before, PDF_Manager.Printing.PdfDocument.ActiveDocumentCount);
        Assert.NotEmpty(DirectPdfGenerator.Generate(SmallJob));
    }

    [Fact]
    public void OutputLimitClosesPartialDocument()
    {
        int before = PDF_Manager.Printing.PdfDocument.ActiveDocumentCount;

        var failure = Assert.Throws<InvalidOperationException>(() =>
            new PrintInput(SmallJob).GetPdfBytes(maxPages: 2, maxBytes: 128));

        Assert.Contains("PDF output exceeds", failure.Message);
        Assert.Equal(before, PDF_Manager.Printing.PdfDocument.ActiveDocumentCount);
    }

    [Fact]
    public async Task CancellationDuringCompositionClosesPartialDocument()
    {
        int before = PDF_Manager.Printing.PdfDocument.ActiveDocumentCount;
        string job = ScreenJob(700);
        using var source = new CancellationTokenSource();
        Task<byte[]> pending = Task.Run(() => DirectPdfGenerator.Generate(job, source.Token));
        Assert.True(SpinWait.SpinUntil(
            () => PDF_Manager.Printing.PdfDocument.ActiveDocumentCount > before,
            TimeSpan.FromSeconds(10)), "PDF composition did not begin.");
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await pending.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(before, PDF_Manager.Printing.PdfDocument.ActiveDocumentCount);
    }

    private static string ScreenJob(int count) => Newtonsoft.Json.JsonConvert.SerializeObject(new
    {
        dimension = 3,
        PrintScreenData = Enumerable.Range(0, count).Select(index =>
            new { mode = "print_load", title1 = $"diagram {index}", result = Array.Empty<object>() })
    });
}

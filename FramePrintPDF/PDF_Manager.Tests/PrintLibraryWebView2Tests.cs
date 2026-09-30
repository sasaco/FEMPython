using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PDF_Manager;
using PdfSharpCore.Pdf.IO;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class PrintLibraryWebView2Tests
{
    [Fact]
    public async Task GeneratedPdfNavigatesInRealWinFormsWebView2()
    {
        var bytes = DirectPdfGenerator.Generate("""
            {"dimension":3,"title":"Preview probe","hasPrintInputData":false,"hasPrintCalculation":false}
            """);
        var folder = Path.Combine(Path.GetTempPath(), "FrameWebPrintProbe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var pdfPath = Path.Combine(folder, "preview.pdf");
        var printedPath = Path.Combine(folder, "rendered-print.pdf");
        File.WriteAllBytes(pdfPath, bytes);
        var completion = new TaskCompletionSource<(bool Success, string Source, bool Printed,
            CoreWebView2PrintStatus PrintStatus)>(TaskCreationOptions.RunContinuationsAsynchronously);
        Form? window = null;
        var uiThread = new Thread(() =>
        {
            using var form = new Form { Width = 640, Height = 480, ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual, Location = new Point(-2000, -2000) };
            window = form;
            using var view = new WebView2 { Dock = DockStyle.Fill };
            form.Controls.Add(view);
            form.Shown += async (_, _) =>
            {
                try
                {
                    var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(folder, "webview-data"));
                    await view.EnsureCoreWebView2Async(environment);
                    view.NavigationCompleted += async (_, args) =>
                    {
                        try
                        {
                            if (!args.IsSuccess)
                            {
                                completion.TrySetResult((false, view.CoreWebView2.Source, false,
                                    CoreWebView2PrintStatus.OtherError));
                                return;
                            }
                            await Task.Delay(500);
                            bool printed = await view.CoreWebView2.PrintToPdfAsync(printedPath);
                            var settings = view.CoreWebView2.Environment.CreatePrintSettings();
                            settings.PrinterName = "FrameWeb Missing Probe Printer " + Guid.NewGuid().ToString("N");
                            CoreWebView2PrintStatus status = await view.CoreWebView2.PrintAsync(settings);
                            completion.TrySetResult((true, view.CoreWebView2.Source, printed, status));
                        }
                        catch (Exception error) { completion.TrySetException(error); }
                        finally { form.Close(); }
                    };
                    view.CoreWebView2.Navigate(new Uri(pdfPath).AbsoluteUri);
                }
                catch (Exception error)
                {
                    completion.TrySetException(error);
                    form.Close();
                }
            };
            Application.Run(form);
        });
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        try
        {
            var navigation = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.True(navigation.Success);
            Assert.Equal(new Uri(pdfPath).AbsoluteUri, navigation.Source);
            Assert.True(navigation.Printed);
            Assert.Equal(CoreWebView2PrintStatus.PrinterUnavailable, navigation.PrintStatus);
            using var printed = PdfReader.Open(printedPath, PdfDocumentOpenMode.Import);
            Assert.Equal(1, printed.PageCount);
        }
        finally
        {
            if (window is { IsDisposed: false, IsHandleCreated: true })
                window.BeginInvoke((Action)(() => window.Close()));
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(10)), "WebView2 UI thread did not close.");
            File.Delete(pdfPath);
            File.Delete(printedPath);
            for (var attempt = 0; attempt < 50; attempt++)
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                    break;
                }
                catch (IOException) when (attempt < 49)
                {
                    await Task.Delay(200);
                }
            }
        }
    }
}

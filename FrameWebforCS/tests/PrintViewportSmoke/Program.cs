using System.Drawing;
using System.Reflection;
using System.Text.Json;
using FrameWebforCS;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using FrameWebforCS.three;
using OpenTK.WinForms;

namespace PrintViewportSmoke;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Exception? failure = null;
        using var owner = new Form
        {
            Text = "Print viewport modal smoke", StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(100, 100, 960, 700)
        };
        using var glControl = new GLControl { Dock = DockStyle.Fill };
        owner.Controls.Add(glControl);
        using var viewport = new ThreeComponent(glControl);
        owner.Shown += (_, _) => owner.BeginInvoke(() =>
        {
            try { RunSmoke(owner, glControl, viewport, args.FirstOrDefault()); }
            catch (Exception error) { failure = error; }
            finally { owner.Close(); }
        });
        Application.Run(owner);
        if (failure is null)
        {
            Console.WriteLine("PASS: real GL capture under owner-bound covering modal");
            return 0;
        }
        Console.Error.WriteLine(failure);
        return 1;
    }

    private static void RunSmoke(Form owner, GLControl glControl,
        ThreeComponent viewport, string? outputPath)
    {
        AppRoutingModule.Instance.NotifyInputMode("node");
        using var fixture = JsonDocument.Parse("""
            {"node":{"1":{"x":0},"2":{"x":10}},
             "member":{"1":{"ni":"1","nj":"2"}},
             "result":{"Case1":{"disg":{"1":{"dx":0.1},"2":{"dx":0.2}}}}}
            """);
        InputDataService.Instance.JsonDataOpen(fixture.RootElement);
        var field = typeof(ThreeComponent).GetField("_threeService",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var service = (ThreeService)(field.GetValue(viewport) ??
            throw new InvalidOperationException("ThreeService did not initialize."));
        service.FlushPending();
        service.SelectNode(1);
        var before = service.CapturePrintState();

        using var modal = new Form
        {
            Text = "Covering print modal", StartPosition = FormStartPosition.Manual,
            Bounds = owner.Bounds, BackColor = Color.Magenta
        };
        Exception? captureFailure = null;
        modal.Shown += (_, _) => modal.BeginInvoke(() =>
        {
            try
            {
                if (!modal.Bounds.Contains(glControl.RectangleToScreen(glControl.ClientRectangle)))
                    throw new InvalidOperationException("The modal did not fully cover the viewport.");
                var request = new PrintDiagramRequest(0, PrintOption.DisplacementDiagram,
                    "disg", "Case1", "disg", "Displacement");
                var images = viewport.CapturePrintDiagrams([request]);
                if (images.Count != 1 || images[0].Request != request)
                    throw new InvalidOperationException("Capture order changed.");
                using var png = new MemoryStream(images[0].PngBytes);
                using var image = new Bitmap(png);
                if (image.Width != glControl.ClientSize.Width || image.Height != glControl.ClientSize.Height)
                    throw new InvalidOperationException("Captured dimensions are wrong.");
                int nonwhite = 0;
                int magenta = 0;
                int overlayInk = 0;
                for (int y = 0; y < image.Height; y += 2)
                    for (int x = 0; x < image.Width; x += 2)
                    {
                        Color pixel = image.GetPixel(x, y);
                        if (pixel.R < 240 || pixel.G < 240 || pixel.B < 240) nonwhite++;
                        if (pixel.R > 240 && pixel.G < 20 && pixel.B > 240) magenta++;
                        if (x > image.Width - 338 && y < 90 &&
                            pixel.R < 100 && pixel.G < 100 && pixel.B < 100) overlayInk++;
                    }
                if (nonwhite < 50 || magenta > 0 || overlayInk < 5)
                    throw new InvalidOperationException(
                        $"Capture lacks model/annotation or contains modal: nonwhite={nonwhite}, magenta={magenta}, ink={overlayInk}.");
                var after = service.CapturePrintState();
                if (after.VisibleMode != before.VisibleMode || after.NodeId != before.NodeId ||
                    after.ResultMode != before.ResultMode || after.ResultCase != before.ResultCase)
                    throw new InvalidOperationException("Viewport state was not restored.");
                for (int repeat = 0; repeat < 2; repeat++)
                    if (viewport.CapturePrintDiagrams([request]).Count != 1)
                        throw new InvalidOperationException("Repeated capture failed.");
                owner.ClientSize = new Size(800, 560);
                var resized = viewport.CapturePrintDiagrams([request]);
                using (var resizedStream = new MemoryStream(resized[0].PngBytes))
                using (var resizedImage = new Bitmap(resizedStream))
                    if (resizedImage.Size != glControl.ClientSize)
                        throw new InvalidOperationException("Capture did not follow viewport resize.");
                var missing = new PrintDiagramRequest(1, PrintOption.SectionDiagram,
                    "fsec", "missing", "fx", "Missing result");
                try
                {
                    viewport.CapturePrintDiagrams([request, missing]);
                    throw new InvalidOperationException("Missing result capture unexpectedly succeeded.");
                }
                catch (InvalidOperationException error) when (error.Message.Contains("no 'fsec' diagram"))
                {
                    var afterFailure = service.CapturePrintState();
                    if (afterFailure.VisibleMode != before.VisibleMode ||
                        afterFailure.NodeId != before.NodeId ||
                        afterFailure.ResultMode != before.ResultMode)
                        throw new InvalidOperationException("Viewport state was not restored after failure.");
                }
                if (outputPath is not null) File.WriteAllBytes(outputPath, images[0].PngBytes);
            }
            catch (Exception error) { captureFailure = error; }
            finally { modal.Close(); }
        });
        modal.ShowDialog(owner);
        if (captureFailure is not null) throw captureFailure;
        glControl.Dispose();
        if (viewport.CapturePrintDiagrams([]).Count != 0)
            throw new InvalidOperationException("Input-only capture unexpectedly required OpenGL.");
        try
        {
            viewport.CapturePrintDiagrams([new PrintDiagramRequest(0,
                PrintOption.DisplacementDiagram, "disg", "Case1", "disg", "Disposed")]);
            throw new InvalidOperationException("Disposed context capture unexpectedly succeeded.");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("unavailable")) { }
    }
}

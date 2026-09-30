using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FrameWebforCS.providers.printing;
using SingleFormsDemo;
using THREE;

namespace FrameWebforCS.three;

internal sealed record PrintDiagramImage(PrintDiagramRequest Request, byte[] PngBytes);

/// <summary>Reads the existing renderer through an off-screen framebuffer, independent of window occlusion.</summary>
internal static class PrintViewportCapture
{
    internal const int MaximumDimension = 4096;
    internal const long MaximumPixels = 8_000_000;

    internal static Bitmap Capture(SceneService scene, Size size)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ValidateSize(size);
        if (scene.renderer is null || scene.CurrentCamera is null)
            throw new InvalidOperationException("The viewport renderer is unavailable.");

        var target = new GLRenderTarget(size.Width, size.Height);
        var renderer = scene.renderer;
        var priorTarget = renderer.GetRenderTarget();
        var rgba = new byte[checked(size.Width * size.Height * 4)];
        try
        {
            renderer.SetRenderTarget(target);
            renderer.Render(scene.scene, scene.CurrentCamera);
            renderer.ReadRenderTargetPixels(target, 0, 0, size.Width, size.Height, rgba, null);
            return FromRgbaBottomUp(rgba, size);
        }
        finally
        {
            try { renderer.SetRenderTarget(priorTarget); }
            finally { target.Dispose(); }
        }
    }

    internal static Bitmap FromRgbaBottomUp(byte[] rgba, Size size)
    {
        ValidateSize(size);
        ArgumentNullException.ThrowIfNull(rgba);
        if (rgba.Length != checked(size.Width * size.Height * 4))
            throw new ArgumentException("The framebuffer byte count does not match its dimensions.", nameof(rgba));

        var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
        BitmapData? bits = null;
        try
        {
            try
            {
                bits = bitmap.LockBits(new System.Drawing.Rectangle(Point.Empty, size), ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);
                var row = new byte[checked(size.Width * 4)];
                for (int y = 0; y < size.Height; y++)
                {
                    int source = checked((size.Height - y - 1) * row.Length);
                    for (int x = 0; x < row.Length; x += 4)
                    {
                        row[x] = rgba[source + x + 2];
                        row[x + 1] = rgba[source + x + 1];
                        row[x + 2] = rgba[source + x];
                        row[x + 3] = rgba[source + x + 3];
                    }
                    Marshal.Copy(row, 0, bits.Scan0 + y * bits.Stride, row.Length);
                }
                return bitmap;
            }
            finally
            {
                if (bits is not null) bitmap.UnlockBits(bits);
            }
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    internal static void ValidateSize(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0 ||
            size.Width > MaximumDimension || size.Height > MaximumDimension ||
            (long)size.Width * size.Height > MaximumPixels)
            throw new ArgumentOutOfRangeException(nameof(size),
                "The print viewport must have a positive, bounded size.");
    }
}

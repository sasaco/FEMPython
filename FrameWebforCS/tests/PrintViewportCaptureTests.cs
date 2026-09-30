using System.Drawing;
using FrameWebforCS.three;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class PrintViewportCaptureTests
{
    [Fact]
    public void RgbaReadback_FlipsOpenGlRowsAndSwapsRedBlue()
    {
        // OpenGL supplies the bottom row first and RGBA channel order.
        byte[] rgba =
        [
            255, 0, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 255, 255, 255, 255
        ];

        using var bitmap = PrintViewportCapture.FromRgbaBottomUp(rgba, new Size(2, 2));

        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(1, 0).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(0, 1).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(4097, 1)]
    [InlineData(3000, 3000)]
    public void InvalidImageDimensions_AreRejectedBeforeAllocation(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PrintViewportCapture.ValidateSize(new Size(width, height)));
    }

    [Fact]
    public void InvalidReadbackLength_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            PrintViewportCapture.FromRgbaBottomUp(new byte[15], new Size(2, 2)));
    }
}

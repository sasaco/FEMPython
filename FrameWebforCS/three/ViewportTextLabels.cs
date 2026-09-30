using System.Drawing;
using System.Windows.Forms;
using THREE;

namespace FrameWebforCS.three;

internal readonly record struct ViewportTextLabel(string Text, Vector3 Position,
    System.Drawing.Color? ForeColor = null);

/// <summary>Draws the legacy CSS2D-style labels over the swapped GL frame.</summary>
internal static class ViewportTextLabels
{
    internal const int MaximumVisibleLabels = 500;
    internal const int MaximumCandidateLabels = 2_000;

    internal static void Draw(Control viewport, Camera camera,
        IEnumerable<ViewportTextLabel> labels)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(labels);
        if (!viewport.IsHandleCreated || viewport.IsDisposed ||
            viewport.ClientSize.Width <= 0 || viewport.ClientSize.Height <= 0) return;

        camera.UpdateMatrixWorld(true);
        using var graphics = Graphics.FromHwnd(viewport.Handle);
        Draw(graphics, camera, labels, viewport.ClientSize);
    }

    internal static void Draw(Graphics graphics, Camera camera,
        IEnumerable<ViewportTextLabel> labels, Size viewportSize)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(labels);
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0) return;

        camera.UpdateMatrixWorld(true);
        graphics.SetClip(new System.Drawing.Rectangle(Point.Empty, viewportSize));
        int candidates = 0;
        int drawn = 0;
        foreach (var label in labels)
        {
            if (candidates++ >= MaximumCandidateLabels || drawn >= MaximumVisibleLabels) break;
            if (string.IsNullOrEmpty(label.Text) ||
                !TryProject(label.Position, camera, viewportSize, out var center)) continue;
            var size = TextRenderer.MeasureText(graphics, label.Text, SystemFonts.DefaultFont,
                Size.Empty, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(graphics, label.Text, SystemFonts.DefaultFont,
                new Point(center.X - size.Width / 2, center.Y - size.Height),
                label.ForeColor ?? System.Drawing.Color.Black,
                TextFormatFlags.NoPadding);
            drawn++;
        }
    }

    internal static bool TryProject(Vector3 position, Camera camera, Size viewport, out Point point)
    {
        point = default;
        if (viewport.Width <= 0 || viewport.Height <= 0) return false;
        var projected = new Vector3(position.X, position.Y, position.Z).Project(camera);
        if (!float.IsFinite(projected.X) || !float.IsFinite(projected.Y) ||
            !float.IsFinite(projected.Z) || projected.X is < -1 or > 1 ||
            projected.Y is < -1 or > 1 || projected.Z is < -1 or > 1) return false;
        point = new Point((int)((projected.X + 1) * viewport.Width / 2),
            (int)((1 - projected.Y) * viewport.Height / 2));
        return true;
    }
}

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using OpenTK.Graphics.ES30;
using THREE;

namespace FrameWebforCS.three;

internal readonly record struct ViewportTextLabel(string Text, Vector3 Position,
    System.Drawing.Color? ForeColor = null, bool AvoidOverlap = false);

/// <summary>Projects scene labels for the print bitmap and the live GL overlay.</summary>
internal static class ViewportTextLabels
{
    internal const int MaximumVisibleLabels = 500;
    internal const int MaximumCandidateLabels = 2_000;

    // The bitmap is cached by projected content; idle 10 ms frames only draw one quad.
    internal sealed class GlOverlay : IDisposable
    {
        private const long MaximumPixels = 16_000_000;
        private readonly List<ProjectedLabel> _previous = [];
        private Size _previousSize;
        private int _texture;
        private int _program;
        private int _vertexArray;
        private bool _hasImage;

        internal void Draw(Camera camera, IEnumerable<ViewportTextLabel> labels, Size size)
        {
            if (size.Width <= 0 || size.Height <= 0 ||
                (long)size.Width * size.Height > MaximumPixels) return;

            var projected = ProjectLabels(camera, labels, size);

            if (projected.Count == 0)
            {
                _hasImage = false;
                _previous.Clear();
                return;
            }

            // Preserve THREE's actual GL state and its cached view of that state.
            GL.GetInteger(GetPName.CurrentProgram, out int previousProgram);
            GL.GetInteger(GetPName.VertexArrayBinding, out int previousVertexArray);
            GL.GetInteger(GetPName.ActiveTexture, out int previousActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.GetInteger(GetPName.TextureBinding2D, out int previousTexture);
            bool blend = GL.IsEnabled(EnableCap.Blend);
            bool depth = GL.IsEnabled(EnableCap.DepthTest);
            bool scissor = GL.IsEnabled(EnableCap.ScissorTest);
            bool cull = GL.IsEnabled(EnableCap.CullFace);
            GL.GetInteger(GetPName.BlendSrcRgb, out int blendSrcRgb);
            GL.GetInteger(GetPName.BlendDstRgb, out int blendDstRgb);
            GL.GetInteger(GetPName.BlendSrcAlpha, out int blendSrcAlpha);
            GL.GetInteger(GetPName.BlendDstAlpha, out int blendDstAlpha);
            GL.GetInteger(GetPName.BlendEquationRgb, out int blendEquationRgb);
            GL.GetInteger(GetPName.BlendEquationAlpha, out int blendEquationAlpha);
            GL.GetInteger(GetPName.UnpackAlignment, out int unpackAlignment);
            try
            {
                EnsureGlObjects();
                if (!_hasImage || size != _previousSize || !_previous.SequenceEqual(projected))
                    UploadLabels(projected, size);
                DrawTexture();
            }
            finally
            {
                GL.BindVertexArray(previousVertexArray);
                GL.UseProgram(previousProgram);
                GL.BlendEquationSeparate((BlendEquationMode)blendEquationRgb,
                    (BlendEquationMode)blendEquationAlpha);
                GL.BlendFuncSeparate((BlendingFactorSrc)blendSrcRgb,
                    (BlendingFactorDest)blendDstRgb,
                    (BlendingFactorSrc)blendSrcAlpha,
                    (BlendingFactorDest)blendDstAlpha);
                if (!blend) GL.Disable(EnableCap.Blend);
                if (depth) GL.Enable(EnableCap.DepthTest);
                if (scissor) GL.Enable(EnableCap.ScissorTest);
                if (cull) GL.Enable(EnableCap.CullFace);
                GL.BindTexture(TextureTarget.Texture2D, previousTexture);
                GL.ActiveTexture((TextureUnit)previousActiveTexture);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, unpackAlignment);
            }
        }

        private void UploadLabels(IReadOnlyList<ProjectedLabel> projected, Size size)
        {
            using var bitmap = new Bitmap(size.Width, size.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Transparent);
                DrawProjected(graphics, projected, size);
            }
            var data = bitmap.LockBits(new System.Drawing.Rectangle(Point.Empty, size),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
                GL.TexImage2D(TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba,
                    size.Width, size.Height, 0, (OpenTK.Graphics.ES30.PixelFormat)All.BgraImg,
                    PixelType.UnsignedByte, data.Scan0);
            }
            finally { bitmap.UnlockBits(data); }
            _previous.Clear();
            _previous.AddRange(projected);
            _previousSize = size;
            _hasImage = true;
        }

        private void DrawTexture()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.ScissorTest);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.UseProgram(_program);
            GL.BindVertexArray(_vertexArray);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        private void EnsureGlObjects()
        {
            if (_program != 0) return;
            const string vertex = """
                #version 330 core
                out vec2 uv;
                void main() {
                    vec2 positions[6] = vec2[6](vec2(-1, 1), vec2(-1, -1), vec2(1, 1),
                                                   vec2(1, 1), vec2(-1, -1), vec2(1, -1));
                    vec2 coords[6] = vec2[6](vec2(0, 0), vec2(0, 1), vec2(1, 0),
                                                vec2(1, 0), vec2(0, 1), vec2(1, 1));
                    gl_Position = vec4(positions[gl_VertexID], 0, 1);
                    uv = coords[gl_VertexID];
                }
                """;
            const string fragment = """
                #version 330 core
                in vec2 uv;
                uniform sampler2D labelTexture;
                out vec4 color;
                void main() { color = texture(labelTexture, uv); }
                """;
            int vs = Compile(ShaderType.VertexShader, vertex);
            int fs = 0;
            try
            {
                fs = Compile(ShaderType.FragmentShader, fragment);
                _program = GL.CreateProgram();
                GL.AttachShader(_program, vs);
                GL.AttachShader(_program, fs);
                GL.LinkProgram(_program);
                GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out int linked);
                if (linked == 0)
                    throw new InvalidOperationException(GL.GetProgramInfoLog(_program));
                _vertexArray = GL.GenVertexArray();
                _texture = GL.GenTexture();
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                    (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                    (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                    (int)TextureWrapMode.ClampToEdge);
                GL.UseProgram(_program);
                GL.Uniform1(GL.GetUniformLocation(_program, "labelTexture"), 0);
            }
            catch
            {
                Dispose();
                throw;
            }
            finally
            {
                GL.DeleteShader(vs);
                if (fs != 0) GL.DeleteShader(fs);
            }
        }

        private static int Compile(ShaderType type, string code)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, code);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int compiled);
            if (compiled != 0) return shader;
            string message = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException(message);
        }

        public void Dispose()
        {
            if (_texture != 0) GL.DeleteTexture(_texture);
            if (_vertexArray != 0) GL.DeleteVertexArray(_vertexArray);
            if (_program != 0) GL.DeleteProgram(_program);
            _texture = _vertexArray = _program = 0;
            _previous.Clear();
            _hasImage = false;
        }
    }

    private readonly record struct ProjectedLabel(string Text, Point Center,
        System.Drawing.Color? ForeColor, bool AvoidOverlap);

    internal static void Draw(Graphics graphics, Camera camera,
        IEnumerable<ViewportTextLabel> labels, Size viewportSize)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(labels);
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0) return;

        DrawProjected(graphics, ProjectLabels(camera, labels, viewportSize), viewportSize);
    }

    private static List<ProjectedLabel> ProjectLabels(Camera camera,
        IEnumerable<ViewportTextLabel> labels, Size viewportSize)
    {
        camera.UpdateMatrixWorld(true);
        var projected = new List<ProjectedLabel>();
        int candidates = 0;
        foreach (var label in labels)
        {
            if (candidates++ >= MaximumCandidateLabels ||
                projected.Count >= MaximumVisibleLabels) break;
            if (!string.IsNullOrEmpty(label.Text) &&
                TryProject(label.Position, camera, viewportSize, out var center))
                projected.Add(new ProjectedLabel(label.Text, center,
                    label.ForeColor, label.AvoidOverlap));
        }
        return projected;
    }

    private static void DrawProjected(Graphics graphics,
        IReadOnlyList<ProjectedLabel> projected, Size viewportSize)
    {
        graphics.SetClip(new System.Drawing.Rectangle(Point.Empty, viewportSize));
        var occupied = new List<System.Drawing.Rectangle>();
        foreach (var label in projected)
        {
            var size = TextRenderer.MeasureText(graphics, label.Text, SystemFonts.DefaultFont,
                Size.Empty, TextFormatFlags.NoPadding);
            var bounds = new System.Drawing.Rectangle(label.Center.X - size.Width / 2,
                label.Center.Y - size.Height,
                size.Width, size.Height);
            if (label.AvoidOverlap)
            {
                bounds.X = Math.Clamp(bounds.X, 2, Math.Max(2, viewportSize.Width - bounds.Width - 2));
                int originalY = bounds.Y;
                for (int attempt = 0; attempt < 12 && occupied.Any(rect => rect.IntersectsWith(bounds)); attempt++)
                    bounds.Y = originalY + (attempt % 2 == 0 ? -1 : 1) * (attempt / 2 + 1) * (size.Height + 3);
                bounds.Y = Math.Clamp(bounds.Y, 2, Math.Max(2, viewportSize.Height - bounds.Height - 2));
                if (bounds.Y != originalY)
                    graphics.DrawLine(Pens.LightGray, label.Center,
                        new Point(Math.Clamp(label.Center.X, bounds.Left, bounds.Right), bounds.Bottom));
                graphics.FillRectangle(Brushes.White, bounds);
            }
            TextRenderer.DrawText(graphics, label.Text, SystemFonts.DefaultFont,
                bounds.Location,
                label.ForeColor ?? System.Drawing.Color.Black,
                TextFormatFlags.NoPadding);
            occupied.Add(bounds);
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

using OpenTK.Graphics.ES30;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.WinForms;
using SingleFormsDemo;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace FrameWebforCS.three
{
    public class ThreeComponent : GLControl
    {
        public SceneService threeInstance = null;
        private ThreeService? _threeService;
        private bool _disposed;
        private Point? _mouseDownPosition;
        private Panel? _viewportControls;
        private Label? _scaleLabel;
        private NumericUpDown? _scaleValue;
        private Label? _extremaLabel;
        private ListBox? _gradientLegend;
        private PanelGradientLegendEntry[] _legendEntries = [];
        private string? _scaleKind;
        private bool _settingScaleControl;
        private long _lastHoverStamp;
        private int _glThreadId;
        internal const int MaximumPrintDiagrams = 120;
        internal const long MaximumPrintPixels = 64_000_000;

        private System.Windows.Forms.Timer _timer;
        private int timeInterval = 10;

        private GLControl glControl;

        public ThreeComponent(GLControl control)
        {
            this.glControl = control;

            this.glControl.Load += glControl_Load;
            this.glControl.Paint += glControl_Paint;
            this.glControl.KeyDown += glControl_KeyDown;
            this.glControl.KeyPress += glControl_KeyPress;
            this.glControl.KeyUp += glControl_KeyUp;
            this.glControl.MouseDown += glControl_MouseDown;
            this.glControl.MouseMove += glControl_MouseMove;
            this.glControl.MouseLeave += glControl_MouseLeave;
            this.glControl.MouseUp += glControl_MouseUp;
            this.glControl.Resize += glControl_Resize;
            this.glControl.MouseWheel += glControl_MouseWheel;
        }


        private void Run()
        {
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = timeInterval;
            _timer.Tick += (sender, e) =>
            {
                Render();
            };
            _timer.Start();
        }

        private void Render()
        {
            if (_disposed || threeInstance == null)
                return;
            this.glControl.MakeCurrent();
            // Existing WinForms timer owns the GL frame; apply the coordinator's queued
            // changes here instead of JS SceneService's immediate render calls.
            _threeService?.FlushPending();
            threeInstance.render();
            this.glControl.SwapBuffers();
            if (_threeService != null)
                ViewportTextLabels.Draw(glControl, threeInstance.CurrentCamera,
                    _threeService.GetVisibleLabels());
            RefreshViewportControls();
        }

        private void glControl_MouseWheel(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            (threeInstance.glControl as GLControl).Focus();
            threeInstance.OnMouseWheel(e.X, e.Y, e.Delta);
        }

        private void glControl_Load(object? sender, EventArgs e)
        {
            _glThreadId = Environment.CurrentManagedThreadId;
            this.glControl.Profile = OpenTK.Windowing.Common.ContextProfile.Compatability;
            threeInstance = new SceneService();
            threeInstance.OnInit(glControl);
            threeInstance.OnResize(new ResizeEventArgs(glControl.ClientSize.Width, glControl.ClientSize.Height));

            _threeService = new ThreeService(threeInstance);
            CreateViewportControls();

            Run();
        }

        internal IReadOnlyList<PrintDiagramImage> CapturePrintDiagrams(
            IReadOnlyList<PrintDiagramRequest> requests)
        {
            ArgumentNullException.ThrowIfNull(requests);
            if (requests.Count == 0) return [];
            if (_disposed || glControl.IsDisposed || !glControl.IsHandleCreated ||
                threeInstance is null || _threeService is null || _glThreadId == 0)
                throw new InvalidOperationException("The active OpenGL viewport is unavailable for printing.");
            if (Environment.CurrentManagedThreadId != _glThreadId)
                throw new InvalidOperationException("Print diagrams must be captured on the viewport UI thread.");
            if (requests.Count > MaximumPrintDiagrams)
                throw new ArgumentOutOfRangeException(nameof(requests), "Too many print diagrams.");
            Size size = glControl.ClientSize;
            PrintViewportCapture.ValidateSize(size);
            if ((long)requests.Count * size.Width * size.Height > MaximumPrintPixels)
                throw new ArgumentOutOfRangeException(nameof(requests),
                    "The selected diagrams exceed the print image budget.");

            glControl.MakeCurrent();
            var state = _threeService.CapturePrintState();
            var images = new List<PrintDiagramImage>(requests.Count);
            try
            {
                foreach (var request in requests)
                {
                    _threeService.ApplyPrintView(request);
                    using var bitmap = PrintViewportCapture.Capture(threeInstance, size);
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        ViewportTextLabels.Draw(graphics, threeInstance.CurrentCamera,
                            _threeService.GetVisibleLabels(), size);
                        DrawPrintOverlays(graphics, size);
                    }
                    using var stream = new MemoryStream();
                    bitmap.Save(stream, ImageFormat.Png);
                    if (stream.Length > 16_777_216)
                        throw new InvalidOperationException("A print diagram exceeds the PNG size limit.");
                    images.Add(new PrintDiagramImage(request, stream.ToArray()));
                }
                return images;
            }
            finally
            {
                _threeService.RestorePrintState(state);
                RefreshViewportControls();
            }
        }

        private void DrawPrintOverlays(Graphics graphics, Size size)
        {
            if (_threeService is null) return;
            var scale = _threeService.GetScaleControl();
            if (scale is { } control)
            {
                var bounds = new Rectangle(Math.Max(0, size.Width - 338), 8, 330, 83);
                using var background = new SolidBrush(System.Drawing.Color.WhiteSmoke);
                graphics.FillRectangle(background, bounds);
                TextRenderer.DrawText(graphics, control.Label, Font,
                    new Rectangle(bounds.X + 7, bounds.Y + 8, 105, 22),
                    System.Drawing.Color.Black);
                TextRenderer.DrawText(graphics, control.Value.ToString("0.###", CultureInfo.InvariantCulture),
                    Font, new Rectangle(bounds.X + 115, bounds.Y + 5, 205, 23),
                    System.Drawing.Color.Black);
                if (_threeService.CurrentResultExtrema is { } extrema)
                {
                    var first = extrema.Primary;
                    string text = string.Create(CultureInfo.InvariantCulture,
                        $"{extrema.CaseId}  Max {first.Max:0.###} ({first.MaxEntityId})  Min {first.Min:0.###} ({first.MinEntityId})");
                    if (extrema.Secondary is { } second)
                        text += string.Create(CultureInfo.InvariantCulture,
                            $"\nMax {second.Max:0.###} ({second.MaxEntityId})  Min {second.Min:0.###} ({second.MinEntityId})");
                    TextRenderer.DrawText(graphics, text, Font,
                        new Rectangle(bounds.X + 7, bounds.Y + 34, 315, 45),
                        System.Drawing.Color.Black);
                }
            }

            var legend = _threeService.GetPanelGradientLegend();
            if (legend.Count == 0) return;
            var listBounds = new Rectangle(Math.Max(0, size.Width - 153), 95, 145, 230);
            using var white = new SolidBrush(System.Drawing.Color.White);
            graphics.FillRectangle(white, listBounds);
            using var border = new Pen(System.Drawing.Color.Gray);
            graphics.DrawRectangle(border, listBounds);
            int visible = Math.Min(legend.Count, listBounds.Height / 20);
            for (int index = 0; index < visible; index++)
            {
                var entry = legend[index];
                int y = listBounds.Y + index * 20;
                using var swatch = new SolidBrush(entry.Color);
                graphics.FillRectangle(swatch, listBounds.X + 3, y + 3, 14, 14);
                TextRenderer.DrawText(graphics, entry.Text, Font,
                    new Rectangle(listBounds.X + 22, y + 2, listBounds.Width - 25, 18),
                    System.Drawing.Color.Black);
            }
        }

        private void glControl_Resize(object? sender, EventArgs e)
        {
            var control = sender as GLControl;

            if (control.ClientSize.Height == 0)
                control.ClientSize = new Size(control.ClientSize.Width, 1);

            GL.Viewport(0, 0, control.ClientSize.Width, control.ClientSize.Height);
            threeInstance?.OnResize(new ResizeEventArgs(control.ClientSize.Width, control.ClientSize.Height));
        }

        private void glControl_Paint(object? sender, PaintEventArgs e)
        {
            Render();
        }
        private MouseButton GetMouseButton(System.Windows.Forms.MouseEventArgs e)
        {
            MouseButton button = MouseButton.Left;
            switch (e.Button)
            {
                case MouseButtons.Middle:
                    button = MouseButton.Middle;
                    break;
                case MouseButtons.Right:
                    button = MouseButton.Right;
                    break;
                case MouseButtons.Left:
                case MouseButtons.None:
                default:
                    break;
            }
            return button;
        }

        private void glControl_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            _mouseDownPosition = e.Button == MouseButtons.Left ? e.Location : null;
            threeInstance?.OnMouseDown(GetMouseButton(e), e.X, e.Y);
        }

        private void glControl_MouseMove(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            threeInstance?.OnMouseMove(GetMouseButton(e), e.X, e.Y);
            if (e.Button != MouseButtons.None) return;
            long now = Stopwatch.GetTimestamp();
            if (now - _lastHoverStamp < Stopwatch.Frequency / 20) return;
            _lastHoverStamp = now;
            _threeService?.HoverAt(e.X, e.Y, glControl.ClientSize.Width,
                glControl.ClientSize.Height);
        }

        private void glControl_MouseLeave(object? sender, EventArgs e) =>
            _threeService?.ClearHover();

        private void glControl_MouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            threeInstance?.OnMouseUp(GetMouseButton(e), e.X, e.Y);
            // JS picks on pointerdown. Delay selection until a short MouseUp click so
            // a TrackballControls drag does not also highlight a node.
            if (e.Button == MouseButtons.Left && _mouseDownPosition is { } start &&
                Math.Abs(e.X - start.X) <= 4 && Math.Abs(e.Y - start.Y) <= 4)
                _threeService?.SelectAt(e.X, e.Y, glControl.ClientSize.Width, glControl.ClientSize.Height);
            _mouseDownPosition = null;
        }

        private void CreateViewportControls()
        {
            if (glControl.Parent is not Control parent) return;
            _viewportControls = new Panel
            {
                Name = "viewportControls",
                Size = new Size(330, 83),
                Location = new Point(Math.Max(0, parent.ClientSize.Width - 338), 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = System.Drawing.Color.WhiteSmoke,
                Visible = false
            };
            _scaleLabel = new Label { Location = new Point(7, 8), Size = new Size(105, 22) };
            _scaleValue = new NumericUpDown
            {
                Location = new Point(115, 5), Size = new Size(205, 23),
                ThousandsSeparator = true
            };
            _scaleValue.ValueChanged += ScaleValueChanged;
            _extremaLabel = new Label
            {
                Location = new Point(7, 34), Size = new Size(315, 45),
                AutoEllipsis = true
            };
            _viewportControls.Controls.Add(_scaleLabel);
            _viewportControls.Controls.Add(_scaleValue);
            _viewportControls.Controls.Add(_extremaLabel);
            parent.Controls.Add(_viewportControls);
            _viewportControls.BringToFront();
            _gradientLegend = new ListBox
            {
                Name = "panelGradientLegend", DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 20, IntegralHeight = false, Size = new Size(145, 230),
                Location = new Point(Math.Max(0, parent.ClientSize.Width - 153), 95),
                Anchor = AnchorStyles.Top | AnchorStyles.Right, Visible = false
            };
            _gradientLegend.DrawItem += DrawGradientLegendItem;
            parent.Controls.Add(_gradientLegend);
            _gradientLegend.BringToFront();
            RefreshViewportControls();
        }

        private void DrawGradientLegendItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _legendEntries.Length) return;
            e.DrawBackground();
            var entry = _legendEntries[e.Index];
            using (var brush = new SolidBrush(entry.Color))
                e.Graphics.FillRectangle(brush, e.Bounds.Left + 3,
                    e.Bounds.Top + 3, 14, 14);
            TextRenderer.DrawText(e.Graphics, entry.Text, e.Font ?? Font,
                new Point(e.Bounds.Left + 22, e.Bounds.Top + 2), e.ForeColor);
            e.DrawFocusRectangle();
        }

        private void ScaleValueChanged(object? sender, EventArgs e)
        {
            if (!_settingScaleControl && _scaleKind != null && _scaleValue != null)
                _threeService?.QueueScale(_scaleKind, (float)_scaleValue.Value);
        }

        private void RefreshViewportControls()
        {
            if (_viewportControls == null || _scaleLabel == null || _scaleValue == null ||
                _extremaLabel == null || _threeService == null) return;
            RefreshGradientLegend();
            var option = _threeService.GetScaleControl();
            _viewportControls.Visible = option.HasValue;
            if (option is not { } scale) return;
            _settingScaleControl = true;
            try
            {
                _scaleKind = scale.Kind;
                _scaleLabel.Text = scale.Label;
                _scaleValue.DecimalPlaces = scale.Step < 1 ? 1 : 0;
                _scaleValue.Increment = (decimal)scale.Step;
                _scaleValue.Minimum = (decimal)scale.Minimum;
                _scaleValue.Maximum = (decimal)scale.Maximum;
                decimal value = Math.Clamp((decimal)scale.Value,
                    _scaleValue.Minimum, _scaleValue.Maximum);
                if (_scaleValue.Value != value) _scaleValue.Value = value;
            }
            finally { _settingScaleControl = false; }
            var extrema = _threeService.CurrentResultExtrema;
            if (extrema is { } current)
            {
                var primary = current.Primary;
                string text = string.Create(CultureInfo.InvariantCulture,
                    $"{current.CaseId}  Max {primary.Max:0.###} ({primary.MaxEntityId})  Min {primary.Min:0.###} ({primary.MinEntityId})");
                if (current.Secondary is { } secondary)
                    text += string.Create(CultureInfo.InvariantCulture,
                        $"\nMax {secondary.Max:0.###} ({secondary.MaxEntityId})  Min {secondary.Min:0.###} ({secondary.MinEntityId})");
                if (_extremaLabel.Text != text) _extremaLabel.Text = text;
            }
            else if (_extremaLabel.Text.Length > 0) _extremaLabel.Text = "";
        }

        private void RefreshGradientLegend()
        {
            if (_gradientLegend == null || _threeService == null) return;
            var entries = _threeService.GetPanelGradientLegend();
            _gradientLegend.Visible = entries.Count > 0;
            bool unchanged = _legendEntries.Length == entries.Count;
            for (int i = 0; unchanged && i < entries.Count; i++)
                unchanged = _legendEntries[i].Equals(entries[i]);
            if (unchanged) return;
            _legendEntries = entries.ToArray();
            _gradientLegend.BeginUpdate();
            try
            {
                _gradientLegend.Items.Clear();
                foreach (var entry in _legendEntries)
                    _gradientLegend.Items.Add(entry.Text);
            }
            finally { _gradientLegend.EndUpdate(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _timer?.Stop();
                _timer?.Dispose();
                if (_scaleValue != null) _scaleValue.ValueChanged -= ScaleValueChanged;
                if (_gradientLegend != null) _gradientLegend.DrawItem -= DrawGradientLegendItem;
                _gradientLegend?.Dispose();
                _viewportControls?.Dispose();
                _viewportControls = null;
                glControl.Load -= glControl_Load;
                glControl.Paint -= glControl_Paint;
                glControl.KeyDown -= glControl_KeyDown;
                glControl.KeyPress -= glControl_KeyPress;
                glControl.KeyUp -= glControl_KeyUp;
                glControl.MouseDown -= glControl_MouseDown;
                glControl.MouseMove -= glControl_MouseMove;
                glControl.MouseLeave -= glControl_MouseLeave;
                glControl.MouseUp -= glControl_MouseUp;
                glControl.Resize -= glControl_Resize;
                glControl.MouseWheel -= glControl_MouseWheel;
                if (threeInstance != null && !glControl.IsDisposed)
                {
                    glControl.MakeCurrent();
                }
                _threeService?.Dispose();
                if (threeInstance != null)
                {
                    if (!glControl.IsDisposed)
                        threeInstance.renderer?.Dispose();
                    InputDataService.Instance.UnregisterSceneService(threeInstance);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        private void glControl_KeyUp(object? sender, KeyEventArgs e)
        {
            Keys key = (Keys)e.KeyCode;
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Right:
                    key = Keys.Right;
                    break;
                case System.Windows.Forms.Keys.Left:
                    key = Keys.Left;
                    break;
                case System.Windows.Forms.Keys.Down:
                    key = Keys.Down;
                    break;
                case System.Windows.Forms.Keys.Up:
                    key = Keys.Up;
                    break;

            }
            threeInstance?.OnKeyUp(key, e.KeyValue, (KeyModifiers)e.Modifiers);
        }

        private void glControl_KeyDown(object? sender, KeyEventArgs e)
        {
            Keys key = (Keys)e.KeyCode;
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Right:
                    key = Keys.Right;
                    break;
                case System.Windows.Forms.Keys.Left:
                    key = Keys.Left;
                    break;
                case System.Windows.Forms.Keys.Down:
                    key = Keys.Down;
                    break;
                case System.Windows.Forms.Keys.Up:
                    key = Keys.Up;
                    break;

            }
            threeInstance?.OnKeyDown(key, e.KeyValue, (KeyModifiers)e.Modifiers);
        }

        private void glControl_KeyPress(object? sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            threeInstance?.OnKeyPress(e.KeyChar.ToString());
        }


        /// <summary>
        /// 
        /// デザイナー上で GLControl が実際に初期化されている
        /// 読み込み経路は次のとおりです。
        /// 1. FrameWebforCS/AppComponent.Designer.cs:34 が ThreeComponent を生成
        /// 2. FrameWebforCS/three/ThreeComponent.cs:21 が InitializeComponent() を実行
        /// 3. FrameWebforCS/three/ThreeComponent.Designer.cs:33 が GLControl を生成
        /// デザイン時だけ GLControl を非表示にして、ハンドル生成とレンダラー初期化を防ぐことです。
        /// DesignMode 単独ではコンストラクター内で正しく判定できない場合があるため、LicenseManager と IsAncestorSiteInDesignMode を併用します。実行時の描画処理には影響せず、デザイナーでは ThreeComponent 部分だけ空白になります。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            if (DesignMode ||
                IsAncestorSiteInDesignMode ||
                LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                glControl.Visible = false;
            }

            base.OnHandleCreated(e);
        }
    }
}

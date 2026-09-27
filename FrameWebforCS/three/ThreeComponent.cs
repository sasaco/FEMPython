using OpenTK.Graphics.ES30;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.WinForms;
using SingleFormsDemo;
using FrameWebforCS.providers;
using System.ComponentModel;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace FrameWebforCS.three
{
    public class ThreeComponent : GLControl
    {
        public SceneService threeInstance = null;
        private ThreeService? _threeService;
        private bool _disposed;
        private Point? _mouseDownPosition;

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
        }

        private void glControl_MouseWheel(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            (threeInstance.glControl as GLControl).Focus();
            threeInstance.OnMouseWheel(e.X, e.Y, e.Delta);
        }

        private void glControl_Load(object? sender, EventArgs e)
        {
            this.glControl.Profile = OpenTK.Windowing.Common.ContextProfile.Compatability;
            threeInstance = new SceneService();
            threeInstance.OnInit(glControl);
            threeInstance.OnResize(new ResizeEventArgs(glControl.ClientSize.Width, glControl.ClientSize.Height));

            _threeService = new ThreeService(threeInstance);

            Run();
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
        }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _timer?.Stop();
                _timer?.Dispose();
                glControl.Load -= glControl_Load;
                glControl.Paint -= glControl_Paint;
                glControl.KeyDown -= glControl_KeyDown;
                glControl.KeyPress -= glControl_KeyPress;
                glControl.KeyUp -= glControl_KeyUp;
                glControl.MouseDown -= glControl_MouseDown;
                glControl.MouseMove -= glControl_MouseMove;
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

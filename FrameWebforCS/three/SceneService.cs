using FrameWebforCS.providers;
using OpenTK.Windowing.Common;
using OpenTK.WinForms;
using THREE;
using Color = THREE.Color;

namespace SingleFormsDemo
{
    public class SceneService : THREE.ControlsContainer
    {
        private const float PerspectivePanSpeed = 3;
        private const float OrthographicPanSpeed = 1;

        // シーン
        public Scene scene;

        // レンダラー
        public GLRenderer renderer;

        // ギズモ

        // カメラ
        private Camera camera;

        // JS SceneService creates a Raycaster for ThreeService. C# keeps its existing
        // camera/renderer ownership and exposes only the active camera for node picking.
        internal Camera CurrentCamera => camera;

        public Camera PerspectiveCamera;
        public Camera OrthographicCamera;

        // input_data
        private AxesHelper axisHelper;
        private GridHelper GridHelper;

        // gui
        public TrackballControls controls;
        public GLControl glControl;
        private THREE.Vector3? _perspectiveTarget;

        private InputDataService input_data = InputDataService.Instance;
        // 初期化
        public SceneService()
        {
            // シーンを作成
            this.scene = new Scene();
            this.scene.Background = Color.Hex(0xffffff);

        }

        public void OnInit(GLControl control)
        {
            glControl = control;

            // カメラ
            // 3次元用カメラ
            this.PerspectiveCamera = new PerspectiveCamera(
                70.0f,
                this.glControl.AspectRatio,
                0.1f,
                1000.0f
            );
            // 2次元用カメラ
            this.OrthographicCamera = new OrthographicCamera(
                -this.glControl.Width / 10,
                this.glControl.Width / 10,
                this.glControl.Height / 10,
                -this.glControl.Height / 10,
                -1000.0f,
                1000.0f
            );
            initCamera();

            // 環境光源

            // レンダラー
            createRender();

            // 床面を生成する
            createHelper();

            // gui を生成する

            // コントロール
            this.addControls();

            // カメラを2Dモードで再登録
            changeCamera();
            input_data.RegisterSceneService(this);
        }

        // カメラをシーンに登録する
        private void initCamera()
        {
            // 一旦カメラを消す
            var target = this.scene.GetObjectByName("camera");
            if (target != null)
            {
                this.scene.Remove(this.camera);
            }

            // カメラを登録しなおす
            if (this.input_data.dimension == 3)
            {
                // 3次元の場合
                this.camera = this.PerspectiveCamera;
            }
            else
            {
                // 2次元の場合
                this.camera = this.OrthographicCamera;
            }

            // Keep each camera's own view when switching dimensions.
            this.PerspectiveCamera.Position.Set(50.0f, 50.0f, -50.0f);
            this.PerspectiveCamera.LookAt(0, 0, 0);
            if (this.input_data.dimension == 2)
            {
                this.OrthographicCamera.Up.Set(0, -1, 0);
                this.OrthographicCamera.Position.Set(0, 0, -10);
                this.OrthographicCamera.LookAt(0, 0, 0);
            }

            this.camera.Name = "camera";
            this.scene.Add(this.camera);
        }

        // カメラを切り替える
        public void changeCamera()
        {
            if (input_data.dimension == 2)
            {
                // The 2D camera keeps its own pan, independent of the 3D pose.
                if (ReferenceEquals(this.camera, this.PerspectiveCamera) && this.controls != null)
                    _perspectiveTarget = this.controls.Target.Clone();
                var pos = this.OrthographicCamera.Position;
                this.camera = this.OrthographicCamera;
                this.camera.Up.Set(0, -1, 0);
                this.camera.Position.X = pos.X;
                this.camera.Position.Y = pos.Y;
                this.camera.Position.Z = -10;
                if (this.controls != null)
                    this.controls.Target.Set(pos.X, pos.Y, 0);
                this.camera.LookAt(pos.X, pos.Y, 0);

                // 2次元なら回転できないように設定する
                if (this.controls != null)
                {
                    this.controls.NoRotate = true;
                    this.controls.PanSpeed = OrthographicPanSpeed;
                }
            }
            else
            {
                // 3次元の場合は、ポジションと回転角は引き継ぐ
                // The perspective camera is reused across round trips. Its position and
                // rotation are the last 3D view, not the flattened 2D camera pose.
                this.camera = this.PerspectiveCamera;
                this.camera.Up.Set(0, 0, -1);
                if (this.controls != null && _perspectiveTarget != null)
                    this.controls.Target.Copy(_perspectiveTarget);

                // 3次元なら回転できるように設定する
                if (this.controls != null)
                {
                    this.controls.NoRotate = false;
                    this.controls.PanSpeed = PerspectivePanSpeed;
                }
            }

            if (this.controls != null)
                this.controls.camera = this.camera;

        }

        // The input document is not changed if camera application fails. Keep both
        // reusable camera objects intact, since changeCamera can switch between them.
        internal virtual void ApplyDocumentCamera((float X, float Y, float Z)? position)
        {
            var previousCamera = camera;
            var previousControlCamera = controls?.camera;
            bool previousNoRotate = controls?.NoRotate ?? false;
            float previousPanSpeed = controls?.PanSpeed ?? PerspectivePanSpeed;
            var previousTarget = controls?.Target.Clone();
            var previousPerspectiveTarget = _perspectiveTarget?.Clone();
            var perspective = Capture(PerspectiveCamera);
            var orthographic = Capture(OrthographicCamera);
            try
            {
                changeCamera();
                if (position is { } value)
                    SetCameraPosition(value.X, value.Y, value.Z);
            }
            catch
            {
                Restore(PerspectiveCamera, perspective);
                Restore(OrthographicCamera, orthographic);
                camera = previousCamera;
                if (controls != null)
                {
                    controls.camera = previousControlCamera;
                    controls.NoRotate = previousNoRotate;
                    controls.PanSpeed = previousPanSpeed;
                    if (previousTarget != null) controls.Target.Copy(previousTarget);
                }
                _perspectiveTarget = previousPerspectiveTarget;
                throw;
            }
        }

        private static (float X, float Y, float Z, float RX, float RY, float RZ,
            float UX, float UY, float UZ) Capture(Camera source) =>
            (source.Position.X, source.Position.Y, source.Position.Z,
             source.Rotation.X, source.Rotation.Y, source.Rotation.Z,
             source.Up.X, source.Up.Y, source.Up.Z);

        private static void Restore(Camera target, (float X, float Y, float Z,
            float RX, float RY, float RZ, float UX, float UY, float UZ) value)
        {
            target.Position.Set(value.X, value.Y, value.Z);
            target.Rotation.X = value.RX;
            target.Rotation.Y = value.RY;
            target.Rotation.Z = value.RZ;
            target.Up.Set(value.UX, value.UY, value.UZ);
        }

        internal (float X, float Y, float Z) GetCameraPosition()
        {
            return (camera.Position.X, camera.Position.Y, camera.Position.Z);
        }

        internal void SetCameraPosition(float x, float y, float z)
        {
            camera.Position.Set(x, y, z);
            if (input_data.dimension == 2)
            {
                controls?.Target.Set(x, y, 0);
                camera.LookAt(x, y, 0);
            }
            else
            {
                controls?.Target.Set(0, 0, 0);
                _perspectiveTarget = controls?.Target.Clone();
                camera.LookAt(0, 0, 0);
            }
        }

        public void createRender()
        {
            this.renderer = new THREE.GLRenderer();

            this.renderer.Context = this.glControl.Context;
            this.renderer.Width = this.glControl.Width;
            this.renderer.Height = this.glControl.Height;

            this.renderer.Init();

        }

        public void addControls()
        {
            this.controls = new TrackballControls(this, this.camera);
            this.controls.StaticMoving = false;
            this.controls.RotateSpeed = 4.0f;
            this.controls.ZoomSpeed = 3;
            this.controls.PanSpeed = PerspectivePanSpeed;
            this.controls.NoZoom = false;
            this.controls.NoPan = false;
            this.controls.NoRotate = false;
            this.controls.StaticMoving = true;
            this.controls.DynamicDampingFactor = 0.3f;
            this.controls.Update();
        }

        // 床面を生成する
        private void createHelper()
        {
            this.axisHelper = new AxesHelper(200);

            scene.Add(this.axisHelper);

            this.GridHelper = new GridHelper(200, 20);
            this.GridHelper.Rotation.X = (float)(-0.5 * Math.PI);
            this.GridHelper.Position.Set(15, 0, 0);

            scene.Add(this.GridHelper);
        }


        public override THREE.Rectangle GetClientRectangle()
        {
            return new THREE.Rectangle(0, 0, renderer.Width, renderer.Height);
        }


        public void render()
        {
            controls?.Update();
            renderer?.Render(scene, this.camera);
        }

        public override void OnResize(ResizeEventArgs clientSize)
        {
            if (renderer != null)
            {
                renderer.Resize(clientSize.Width, clientSize.Height);
                this.camera.Aspect = this.glControl.AspectRatio;



                this.camera.UpdateProjectionMatrix();
            }
            base.OnResize(clientSize);
        }
    }
}

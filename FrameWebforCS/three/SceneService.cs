using FrameWebforCS.providers;
using OpenTK.Windowing.Common;
using OpenTK.WinForms;
using THREE;
using Color = THREE.Color;

namespace SingleFormsDemo
{
    public class SceneService : THREE.ControlsContainer
    {
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

            // 初期位置
            this.camera.Position.X = 50.0f;
            this.camera.Position.Y = 50.0f;
            this.camera.Position.Z = -50.0f;
            this.camera.LookAt(0, 0, 0);

            this.camera.Name = "camera";
            this.scene.Add(this.camera);
        }

        // カメラを切り替える
        public void changeCamera()
        {
            if (input_data.dimension == 2)
            {
                // 2次元に切り替わった場合は、ポジションと回転角をリセットする
                var pos = this.camera.Position;
                this.camera = this.OrthographicCamera;
                this.camera.Up = new THREE.Vector3(0, -1, 0);
                this.camera.Position.X = pos.X;
                this.camera.Position.Y = pos.Y;
                this.camera.Position.Z = -10;
                this.camera.LookAt(pos.X, pos.Y, 0);

                // 2次元なら回転できないように設定する
                if (this.controls != null)
                    this.controls.NoRotate = true;
            }
            else
            {
                // 3次元の場合は、ポジションと回転角は引き継ぐ
                var pos = this.camera.Position;
                var rot = this.camera.Rotation;
                this.camera = this.PerspectiveCamera;
                this.camera.Up = new THREE.Vector3(0, 0, -1);
                this.camera.Position.X = pos.X;
                this.camera.Position.Y = pos.Y;
                this.camera.Position.Z = pos.Z;
                this.camera.Rotation.X = rot.X;
                this.camera.Rotation.Y = rot.Y;
                this.camera.Rotation.Z = rot.Z;

                // 3次元なら回転できるように設定する
                if (this.controls != null)
                    this.controls.NoRotate = false;
            }

            if (this.controls != null)
                this.controls.camera = this.camera;

        }

        internal (float X, float Y, float Z) GetCameraPosition()
        {
            return (camera.Position.X, camera.Position.Y, camera.Position.Z);
        }

        internal void SetCameraPosition(float x, float y, float z)
        {
            camera.Position.Set(x, y, z);
            if (input_data.dimension == 2)
                camera.LookAt(x, y, 0);
            else
                camera.LookAt(0, 0, 0);
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
            this.controls.PanSpeed = 3;
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

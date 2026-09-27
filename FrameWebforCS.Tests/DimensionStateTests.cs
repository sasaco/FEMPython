using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using FrameWebforCS.three;
using SingleFormsDemo;
using System.Reflection;
using System.Text.Json;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class DimensionStateTests
{
    [Fact]
    public void SetDimensionValidatesAndPublishesOnlySuccessfulChanges()
    {
        var input = InputDataService.Instance;
        var changes = new List<int>();
        input.DimensionChanged += OnChanged;
        try
        {
            Load("{}");
            Assert.Equal(3, input.dimension);
            input.SetDimension(3);
            Assert.Empty(changes);
            Assert.Throws<ArgumentOutOfRangeException>(() => input.SetDimension(4));
            Assert.Equal(3, input.dimension);
            Assert.Empty(changes);

            input.SetDimension(2);
            input.SetDimension(2);
            input.SetDimension(3);
            Assert.Equal(new[] { 2, 3 }, changes);
        }
        finally
        {
            input.DimensionChanged -= OnChanged;
            Load("{}");
        }

        void OnChanged(int dimension) => changes.Add(dimension);
    }

    [Fact]
    public void CameraFailureRollsBackDimensionAndNotification()
    {
        var input = InputDataService.Instance;
        Load("{}");
        var camera = new FailingCameraScene();
        input.RegisterSceneService(camera);
        int changes = 0;
        input.DimensionChanged += OnChanged;
        try
        {
            Assert.Throws<InvalidOperationException>(() => input.SetDimension(2));
            Assert.Equal(3, input.dimension);
            Assert.Equal(0, changes);
        }
        finally
        {
            input.DimensionChanged -= OnChanged;
            input.UnregisterSceneService(camera);
            Load("{}");
        }

        void OnChanged(int _) => changes++;
    }

    [Fact]
    public void CameraRoundTripRestoresPerspectivePoseAndRotationControl()
    {
        var input = InputDataService.Instance;
        Load("{}");
        var scene = new SceneService
        {
            PerspectiveCamera = new PerspectiveCamera(70, 4f / 3f, 0.1f, 1000),
            OrthographicCamera = new OrthographicCamera(-40, 40, 30, -30, -1000, 1000),
            renderer = new GLRenderer { Width = 800, Height = 600 }
        };
        // OnInit also creates a native GL context. Initialize only its camera state
        // here so this test runs deterministically without a display device.
        typeof(SceneService).GetField("camera", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(scene, scene.PerspectiveCamera);
        scene.controls = new TrackballControls(scene, scene.PerspectiveCamera);
        scene.PerspectiveCamera.Position.Set(21, 34, -56);
        scene.PerspectiveCamera.Rotation.X = 0.2f;
        scene.PerspectiveCamera.Rotation.Y = -0.4f;
        scene.PerspectiveCamera.Rotation.Z = 0.6f;
        input.RegisterSceneService(scene);
        try
        {
            input.SetDimension(2);
            Assert.Same(scene.OrthographicCamera, scene.CurrentCamera);
            Assert.Same(scene.OrthographicCamera, scene.controls.camera);
            Assert.True(scene.controls.NoRotate);
            Assert.Equal(21, scene.OrthographicCamera.Position.X);
            Assert.Equal(34, scene.OrthographicCamera.Position.Y);
            Assert.Equal(-10, scene.OrthographicCamera.Position.Z);

            input.SetDimension(3);
            Assert.Same(scene.PerspectiveCamera, scene.CurrentCamera);
            Assert.Same(scene.PerspectiveCamera, scene.controls.camera);
            Assert.False(scene.controls.NoRotate);
            Assert.Equal(21, scene.PerspectiveCamera.Position.X);
            Assert.Equal(34, scene.PerspectiveCamera.Position.Y);
            Assert.Equal(-56, scene.PerspectiveCamera.Position.Z);
            Assert.Equal(0.2f, scene.PerspectiveCamera.Rotation.X);
            Assert.Equal(-0.4f, scene.PerspectiveCamera.Rotation.Y);
            Assert.Equal(0.6f, scene.PerspectiveCamera.Rotation.Z);
        }
        finally
        {
            input.UnregisterSceneService(scene);
            Load("{}");
        }
    }

    [Fact]
    public void ResultDimensionAndValuesSurviveSwitchSaveAndReload()
    {
        var input = InputDataService.Instance;
        try
        {
            Load("""
                {"dimension":2,"node":{"1":{"x":1,"y":2,"z":3}},
                 "result":{"C1":{"disg":{"1":{"dx":0.25,"dz":0.5}},
                                 "reac":{"1":{"tx":4}},
                                 "fsec":{"7":{"0":{"fxi":8}}}}}}
                """);
            Assert.Equal(2, input.ResultDimension);
            input.SetDimension(3);
            Assert.Equal(2, input.ResultDimension);
            string saved = JsonSerializer.Serialize(input.GetSaveJson());
            using var document = JsonDocument.Parse(saved);
            var root = document.RootElement;
            Assert.Equal(3, root.GetProperty("dimension").GetInt32());
            Assert.Equal(2, root.GetProperty("resultDimension").GetInt32());
            Assert.Equal(0.5, root.GetProperty("result").GetProperty("C1")
                .GetProperty("disg").GetProperty("1").GetProperty("dz").GetDouble());
            Assert.Equal(4, root.GetProperty("result").GetProperty("C1")
                .GetProperty("reac").GetProperty("1").GetProperty("tx").GetDouble());
            Assert.Equal(8, root.GetProperty("result").GetProperty("C1")
                .GetProperty("fsec").GetProperty("7").GetProperty("0")
                .GetProperty("fxi").GetDouble());
            input.JsonDataOpen(root);
            Assert.Equal(3, input.dimension);
            Assert.Equal(2, input.ResultDimension);
            Assert.Equal(0.5, ResultDisgService.Instance.getDisg()["C1"]["1"].dz);
            Assert.Equal(4, ResultReacService.Instance.getReac()["C1"]["1"].tx);
            Assert.Equal(8, ResultFsecService.Instance.getFsec()["C1"]["7"]["0"].fxi);
        }
        finally { Load("{}"); }
    }

    [Fact]
    public void LegacyResultDimensionDefaultsToInputAndMalformedMetadataIsAtomic()
    {
        var input = InputDataService.Instance;
        try
        {
            Load("""
                {"dimension":2,"result":{"C1":{"disg":{"1":{"dx":1}}}}}
                """);
            Assert.Equal(2, input.ResultDimension);
            long revision = input.DocumentRevision;
            using var bad = JsonDocument.Parse("""
                {"dimension":3,"resultDimension":4,
                 "result":{"C2":{"disg":{"1":{"dx":2}}}}}
                """);
            Assert.Throws<JsonException>(() => input.JsonDataOpen(bad.RootElement));
            Assert.Equal(revision, input.DocumentRevision);
            Assert.Equal(2, input.dimension);
            Assert.Equal(2, input.ResultDimension);
            Assert.Contains("C1", ResultDisgService.Instance.getDisg().Keys);
        }
        finally { Load("{}"); }
    }

    [Fact]
    public void SaveMergesResultSectionsByCaseWithoutDroppingReactionOnlyCase()
    {
        var input = InputDataService.Instance;
        try
        {
            Load("""
                {"dimension":3,"result":{"C1":{"disg":{"1":{"dx":1}}}}}
                """);
            ResultReacService.Instance.ApplyReac(new()
            {
                ["C2"] = new() { ["1"] = new clsReac { tx = 7 } }
            });
            using var saved = JsonDocument.Parse(JsonSerializer.Serialize(input.GetSaveJson()));
            var cases = saved.RootElement.GetProperty("result");
            Assert.Equal(1, cases.GetProperty("C1").GetProperty("disg")
                .GetProperty("1").GetProperty("dx").GetDouble());
            Assert.Equal(7, cases.GetProperty("C2").GetProperty("reac")
                .GetProperty("1").GetProperty("tx").GetDouble());
            Assert.Equal(JsonValueKind.Object, cases.GetProperty("C2")
                .GetProperty("disg").ValueKind);
            input.JsonDataOpen(saved.RootElement);
            Assert.Contains("C2", ResultReacService.Instance.getReac().Keys);
        }
        finally { Load("{}"); }
    }

    [Fact]
    public void DirectResultReplacementUsesCurrentDimensionAfterEarlierDocument()
    {
        var input = InputDataService.Instance;
        try
        {
            Load("""
                {"dimension":3,"resultDimension":2,
                 "result":{"Old":{"disg":{"1":{"dx":1}}}}}
                """);
            Assert.Equal(2, input.ResultDimension);

            using (var fresh3D = JsonDocument.Parse("""
                {"result":{"New3D":{"disg":{"1":{"dz":2}},
                                      "reac":{"1":{"tz":3}},
                                      "fsec":{"7":{"0":{"fzi":4}}}}}}
                """))
            {
                ResultDisgService.Instance.setDisgJson(fresh3D.RootElement);
                ResultReacService.Instance.setReacJson(fresh3D.RootElement);
                ResultFsecService.Instance.setFsecJson(fresh3D.RootElement);
            }
            Assert.Equal(3, input.ResultDimension);

            input.SetDimension(2);
            using (var fresh2D = JsonDocument.Parse("""
                {"result":{"New2D":{"disg":{"1":{"dx":5}},
                                      "reac":{"1":{"tx":6}},
                                      "fsec":{"7":{"0":{"fxi":7}}}}}}
                """))
            {
                ResultDisgService.Instance.setDisgJson(fresh2D.RootElement);
                ResultReacService.Instance.setReacJson(fresh2D.RootElement);
                ResultFsecService.Instance.setFsecJson(fresh2D.RootElement);
            }
            Assert.Equal(2, input.ResultDimension);
        }
        finally { Load("{}"); }
    }

    [Fact]
    public void ViewportHidesIncompatibleResultsAndRestoresThemOnReturn()
    {
        var input = InputDataService.Instance;
        var routing = AppRoutingModule.Instance;
        try
        {
            routing.NotifyInputMode("basedisg");
            using var viewport = new ThreeService(new SceneService());
            Load("""
                {"dimension":3,"node":{"1":{"x":0},"2":{"x":2}},
                 "member":{"1":{"ni":"1","nj":"2"}},
                 "result":{"C1":{"disg":{"1":{"dx":0.1},"2":{"dx":0.2}}}}}
                """);
            viewport.FlushPending();
            Assert.True(viewport.DisplacementCount > 0);

            input.SetDimension(2);
            viewport.FlushPending();
            Assert.Equal(0, viewport.DisplacementCount);
            Assert.True(ResultDisgService.Instance.getDisg().Count > 0);

            input.SetDimension(3);
            viewport.FlushPending();
            Assert.True(viewport.DisplacementCount > 0);
        }
        finally
        {
            Load("{}");
            routing.NotifyInputMode("node");
        }
    }

    private static void Load(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }

    private sealed class FailingCameraScene : SceneService
    {
        internal override void ApplyDocumentCamera((float X, float Y, float Z)? position) =>
            throw new InvalidOperationException("camera failed");
    }
}

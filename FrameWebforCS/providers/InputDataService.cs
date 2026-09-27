using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using SingleFormsDemo;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FrameWebforCS.providers
{
    public class InputDataService
    {
        // Lazy<T> を使ってスレッドセーフかつ遅延評価のシングルトンを実装
        private static readonly Lazy<InputDataService> _instance =
            new Lazy<InputDataService>(() => new InputDataService());

        // 外部からはこのプロパティを通じてのみインスタンスにアクセスできる
        public static InputDataService Instance => _instance.Value;


        // コンストラクタを private にして、外部からの new を禁止する
        private InputDataService()
        {
            // 初期化処理があればここに書く
            CurrentComponent = null;


            dimension = 3;

        }

        // --------------------------------------------------
        // 保持したいデータやプロパティを以下に定義する
        // --------------------------------------------------

        //現在編集中のコンポーネント
        public UserControl? CurrentComponent { get; set; }

        // ３次元解析=3, ２次元解析=2
        public int dimension { get; set; }

        private SceneService? _sceneService;
        private (float X, float Y, float Z)? _cameraPosition;

        internal long DocumentRevision { get; private set; }
        internal event Action<long>? FileReplaced;

        internal void RegisterSceneService(SceneService sceneService)
        {
            _sceneService = sceneService;
            if (_cameraPosition is { } position)
            {
                sceneService.SetCameraPosition(position.X, position.Y, position.Z);
            }
        }

        internal void UnregisterSceneService(SceneService sceneService)
        {
            if (ReferenceEquals(_sceneService, sceneService))
                _sceneService = null;
        }



        /// <summary>
        /// Dfineケースのケース番号を
        /// </summary>
        /// <returns></returns>
        internal List<string> GetDifineCase()
        {
            var result = new List<string>();

            for(int i = 0; i < 10; i++)
            {
                result.Add("D" + (i + 1).ToString());
            }

            return result;
        }

        /// <summary>
        /// Componentに表示するデータを返す
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, object> getDisg()
        {
            var result = new Dictionary<string, object>();

            for(int i = 0; i < 20; i++)
            {
                result.Add("case " + i.ToString(), null);
            }
            return result;
        }
        internal Dictionary<string, object> getCombineDisg()
        {
            return getDisg();
        }
        internal Dictionary<string, object> getPickupDisg()
        {
            return getDisg();
        }
        private Dictionary<string, object> getFsec()
        {
            return getDisg();
        }
        internal Dictionary<string, object> getCombineFsec()
        {
            return getFsec();
        }
        internal Dictionary<string, object> getPickupFsec()
        {
            return getFsec();
        }
        private Dictionary<string, object> getReac()
        {
            return getDisg();
        }
        internal Dictionary<string, object> getCombineReac()
        {
            return getReac();
        }
        internal Dictionary<string, object> getPickupReac()
        {
            return getReac();
        }

        internal void JsonDataOpen(JsonElement rootElement)
        {
            // JS loadInputData mutates one service at a time before three.fileload(). Stage every
            // section first so a late invalid result cannot leave mixed old/new input behind.
            // UI/GL publication still happens once through FileReplaced after the commit.
            var preparedNodes = InputNodesService.ParseNodeJson(rootElement);
            var preparedMembers = InputMembersService.ParseMemberJson(rootElement);
            var preparedPanels = InputPanelService.ParsePanelJson(rootElement);
            var preparedRigid = InputRigidZoneService.ParseRigidJson(rootElement);
            var preparedElements = InputElementsService.ParseElementJson(rootElement);
            var preparedFixNodes = InputFixNodeService.ParseFixNodeJson(rootElement);
            var preparedFixMembers = InputFixMemberService.ParseFixMemberJson(rootElement);
            var preparedJoints = InputJointService.ParseJointJson(rootElement);
            var preparedLoads = InputLoadService.ParseLoadJson(rootElement);
            var preparedNoticePoints = InputNoticePointsService.ParseNoticePointsJson(rootElement);
            var preparedCombine = InputCombineService.ParseCombineJson(rootElement);
            bool hasResult = rootElement.TryGetProperty("result", out _);
            var preparedDisg = hasResult ? ResultDisgService.ParseDisgJson(rootElement) : new();
            var preparedFsec = hasResult ? ResultFsecService.ParseFsecJson(rootElement) : new();
            var preparedReac = hasResult ? ResultReacService.ParseReacJson(rootElement) : new();

            int loadedDimension = 3;
            if (rootElement.TryGetProperty("dimension", out var dimensionElement))
            {
                if (dimensionElement.ValueKind != JsonValueKind.Number ||
                    !dimensionElement.TryGetInt32(out loadedDimension) ||
                    loadedDimension is not (2 or 3))
                    throw new JsonException("dimension must be 2 or 3.");
            }

            (float X, float Y, float Z)? loadedCameraPosition = null;
            if (rootElement.TryGetProperty("three", out var threeElement))
            {
                if (threeElement.ValueKind != JsonValueKind.Object ||
                    !threeElement.TryGetProperty("camera", out var cameraElement) ||
                    cameraElement.ValueKind != JsonValueKind.Object)
                    throw new JsonException("three.camera must be an object.");

                loadedCameraPosition = (
                    ReadCameraCoordinate(cameraElement, "x"),
                    ReadCameraCoordinate(cameraElement, "y"),
                    ReadCameraCoordinate(cameraElement, "z"));
            }

            using var notifications = DocumentReplacementNotifications.Begin();
            int previousDimension = dimension;
            dimension = loadedDimension;
            try
            {
                _sceneService?.ApplyDocumentCamera(loadedCameraPosition);
            }
            catch
            {
                dimension = previousDimension;
                throw;
            }
            _cameraPosition = loadedCameraPosition;

            var combineCoordinator = ResultCombineDisgCoordinator.Instance;
            var combineFsecCoordinator = ResultCombineFsecCoordinator.Instance;
            var combineReacCoordinator = ResultCombineReacCoordinator.Instance;
            combineCoordinator.BeginLoad();
            combineFsecCoordinator.BeginLoad();
            combineReacCoordinator.BeginLoad();
            // Apply-path notifications, including BindingList resets, are deferred until
            // all sections and derived snapshots have been installed.
            {
                InputMembersService.Instance.ApplyMembers(preparedMembers);
                InputPanelService.Instance.ApplyPanels(preparedPanels);
                InputRigidZoneService.Instance.ApplyRigid(preparedRigid);
                InputElementsService.Instance.ApplyElements(preparedElements);
                InputFixNodeService.Instance.ApplyFixNode(preparedFixNodes);
                InputFixMemberService.Instance.ApplyFixMember(preparedFixMembers);
                InputJointService.Instance.ApplyJoint(preparedJoints);
                InputLoadService.Instance.ApplyLoads(preparedLoads ?? new Dictionary<string, clsLoad>());
                InputNoticePointsService.Instance.ApplyNoticePoints(preparedNoticePoints);
                InputCombineService.Instance.ApplyCombine(preparedCombine.Combine,
                    preparedCombine.Define, preparedCombine.Pickup);
                ResultDisgService.Instance.ApplyDisg(preparedDisg);
                ResultFsecService.Instance.ApplyFsec(preparedFsec);
                ResultReacService.Instance.ApplyReac(preparedReac);
                InputNodesService.Instance.ApplyNodes(preparedNodes);
                if (hasResult)
                {
                    combineCoordinator.CompleteLoad(dimension);
                    combineFsecCoordinator.CompleteLoad(dimension);
                    combineReacCoordinator.CompleteLoad(dimension);
                }
                else
                {
                    combineCoordinator.FailLoad();
                    combineFsecCoordinator.FailLoad();
                    combineReacCoordinator.FailLoad();
                }
            }

            // Publish the committed revision to the viewport first. A failing observer
            // is reported after every queued observer has had a chance to update.
            DocumentRevision++;
            notifications.PublishFirst(FileReplaced, DocumentRevision);
            notifications.Complete();
        }

        private static float ReadCameraCoordinate(JsonElement camera, string name)
        {
            if (!camera.TryGetProperty(name, out var value) ||
                value.ValueKind != JsonValueKind.Number ||
                !value.TryGetSingle(out float coordinate) ||
                !float.IsFinite(coordinate))
                throw new JsonException($"three.camera.{name} must be a finite number.");

            return coordinate;
        }

        internal Dictionary<string, object>? GetSaveJson()
        {
            var cameraPosition = _sceneService?.GetCameraPosition() ??
                _cameraPosition ?? (50.0f, 50.0f, -50.0f);

            return new Dictionary<string, object> {
                ["dimension"] = dimension,
                ["three"] = new Dictionary<string, object> {
                    ["camera"] = new Dictionary<string, float> {
                        ["x"] = cameraPosition.X,
                        ["y"] = cameraPosition.Y,
                        ["z"] = cameraPosition.Z,
                    },
                },
                ["node"] = InputNodesService.Instance.getNodeJson(),
                ["member"] = InputMembersService.Instance.getMemberJson(),
                ["shell"] = InputPanelService.Instance.getPanelJson(),
                ["rigid"] = InputRigidZoneService.Instance.getRigidJson(),
                ["element"] = InputElementsService.Instance.getElementJson(),
                ["fix_node"] = InputFixNodeService.Instance.getFixNodeJson(),
                ["fix_member"] = InputFixMemberService.Instance.getFixMemberJson(),
                ["joint"] = InputJointService.Instance.getJointJson(),
                ["load"] = InputLoadService.Instance.getLoadJson(),
                ["notice_points"] = InputNoticePointsService.Instance.getNoticePointsJson(),
                ["define"] = InputCombineService.Instance.getDefineJson(),
                ["combine"] = InputCombineService.Instance.getCombineJson(),
                ["pickup"] = InputCombineService.Instance.getPickupJson(),
            };
        }
    }
}

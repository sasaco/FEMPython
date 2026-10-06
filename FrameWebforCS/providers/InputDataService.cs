using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using SingleFormsDemo;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using FrameWebforCS.calculation;

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


            _dimension = 3;
            ResultDisgService.Instance.Changed += OnDirectResultChanged;
            ResultReacService.Instance.Changed += OnDirectResultChanged;
            ResultFsecService.Instance.Changed += OnDirectResultChanged;
            InputNodesService.Instance.NodeEdited += _ => CalculationInputRevision++;
            InputMembersService.Instance.MemberEdited += _ => CalculationInputRevision++;
            InputPanelService.Instance.PanelEdited += _ => CalculationInputRevision++;
            InputRigidZoneService.Instance.Changed += (_, _) => CalculationInputRevision++;
            for (int sheet = 1; sheet <= 6; sheet++)
            {
                string id = sheet.ToString(System.Globalization.CultureInfo.InvariantCulture);
                InputFixNodeService.Instance.GetRows(id).ListChanged += (_, _) => CalculationInputRevision++;
                InputFixMemberService.Instance.GetRows(id).ListChanged += (_, _) => CalculationInputRevision++;
                InputJointService.Instance.GetRows(id).ListChanged += (_, _) => CalculationInputRevision++;
            }
            InputNoticePointsService.Instance.Changed += (_, _) => CalculationInputRevision++;
            InputLoadService.Instance.LoadsEdited += () => CalculationInputRevision++;
            InputBridgeLoadService.Instance.Changed += () =>
            {
                CalculationInputRevision++;
                InputLoadService.Instance.NotifyBridgeCasesChanged();
            };
            InputCombineService.Instance.RowsChanged += (_, _) => CalculationInputRevision++;
            for (int type = 1; type <= InputElementsService.TypeCount; type++)
                InputElementsService.Instance.GetRows(type).ListChanged += (_, _) =>
                    CalculationInputRevision++;

        }

        // --------------------------------------------------
        // 保持したいデータやプロパティを以下に定義する
        // --------------------------------------------------

        //現在編集中のコンポーネント
        public UserControl? CurrentComponent { get; set; }

        // ３次元解析=3, ２次元解析=2
        private int _dimension;
        public int dimension
        {
            get => _dimension;
            set => SetDimension(value);
        }

        internal event Action<int>? DimensionChanged;
        internal int? ResultDimension { get; private set; }
        private bool _publishingDocumentReplacement;

        private void OnDirectResultChanged(object? sender, EventArgs args)
        {
            if (_publishingDocumentReplacement) return;
            bool changedSectionHasResults = sender switch
            {
                ResultDisgService => ResultDisgService.Instance.getDisg().Count != 0,
                ResultReacService => ResultReacService.Instance.getReac().Count != 0,
                ResultFsecService => ResultFsecService.Instance.getFsec().Count != 0,
                _ => false
            };
            if (changedSectionHasResults)
                ResultDimension = dimension;
            else if (ResultDisgService.Instance.getDisg().Count == 0 &&
                     ResultReacService.Instance.getReac().Count == 0 &&
                     ResultFsecService.Instance.getFsec().Count == 0)
                ResultDimension = null;
        }

        public void SetDimension(int value)
        {
            if (value is not (2 or 3))
                throw new ArgumentOutOfRangeException(nameof(value), "Dimension must be 2 or 3.");
            if (_dimension == value) return;

            int previous = _dimension;
            _dimension = value;
            try
            {
                _sceneService?.ApplyDocumentCamera(null);
            }
            catch
            {
                _dimension = previous;
                throw;
            }
            CalculationInputRevision++;
            DimensionChanged?.Invoke(value);
        }

        private SceneService? _sceneService;
        private (float X, float Y, float Z)? _cameraPosition;

        internal long DocumentRevision { get; private set; }
        internal long CalculationInputRevision { get; private set; }
        internal event Action<long>? FileReplaced;

        // Call on the UI thread. Serialization detaches every mutable input row before
        // projection, so the worker never reads WinForms-bound service state.
        internal CalculationRequest CreateCalculationRequest() =>
            CalculationRequestBuilder.FromSavedJson(CaptureCalculationSnapshotJson());

        // Capture on the UI thread. Projection and moving-load expansion can then run
        // on a worker without reading mutable controls or service collections.
        internal string CaptureCalculationSnapshotJson()
        {
            var snapshot = new Dictionary<string, object?>
            {
                ["dimension"] = dimension,
                ["node"] = InputNodesService.Instance.getNodeJson(),
                ["member"] = InputMembersService.Instance.getMemberJson(),
                ["shell"] = InputPanelService.Instance.getPanelJson(),
                ["element"] = InputElementsService.Instance.getElementJson(),
                ["rigid"] = InputRigidZoneService.Instance.getRigidJson(),
                ["joint"] = InputJointService.Instance.getJointJson(),
                ["fix_node"] = InputFixNodeService.Instance.getFixNodeJson(),
                ["fix_member"] = InputFixMemberService.Instance.getFixMemberJson(),
                ["notice_points"] = InputNoticePointsService.Instance.getNoticePointsJson(),
                ["load"] = InputLoadService.Instance.getLoadJson(),
                ["bridge_loads"] = InputBridgeLoadService.Instance.GetSaveJson(),
            };
            return JsonSerializer.Serialize(snapshot);
        }

        internal void ClearLegacyResultsForCalculation()
        {
            ResultDisgService.Instance.ApplyDisg(new());
            ResultReacService.Instance.ApplyReac(new());
            ResultFsecService.Instance.ApplyFsec(new());
            ResultDimension = null;
        }

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
            InputFixMemberService.ValidateAgainstGeometry(preparedFixMembers, preparedMembers, preparedNodes);
            var preparedJoints = InputJointService.ParseJointJson(rootElement);
            var preparedLoads = InputLoadService.ParseLoadData(rootElement);
            var preparedBridge = InputBridgeLoadService.Parse(rootElement);
            var preparedNoticePoints = InputNoticePointsService.ParseNoticePointsJson(rootElement);
            var preparedCombine = InputCombineService.ParseCombineJson(rootElement);
            bool hasResult = rootElement.TryGetProperty("result", out var resultElement);
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

            bool hasResultCases = hasResult && resultElement.ValueKind == JsonValueKind.Object &&
                resultElement.EnumerateObject().Any();
            int? loadedResultDimension = hasResultCases ? loadedDimension : null;
            if (rootElement.TryGetProperty("resultDimension", out var resultDimensionElement))
            {
                if (!hasResultCases || resultDimensionElement.ValueKind != JsonValueKind.Number ||
                    !resultDimensionElement.TryGetInt32(out int parsedResultDimension) ||
                    parsedResultDimension is not (2 or 3))
                    throw new JsonException("resultDimension requires results and must be 2 or 3.");
                loadedResultDimension = parsedResultDimension;
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
            int previousDimension = _dimension;
            _dimension = loadedDimension;
            try
            {
                _sceneService?.ApplyDocumentCamera(loadedCameraPosition);
            }
            catch
            {
                _dimension = previousDimension;
                throw;
            }
            _cameraPosition = loadedCameraPosition;
            ResultDimension = loadedResultDimension;

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
                InputLoadService.Instance.ApplyLoads(preparedLoads);
                InputBridgeLoadService.Instance.Apply(preparedBridge);
                InputNoticePointsService.Instance.ApplyNoticePoints(preparedNoticePoints);
                InputCombineService.Instance.ApplyCombine(preparedCombine.Combine,
                    preparedCombine.Define, preparedCombine.Pickup);
                ResultDisgService.Instance.ApplyDisg(preparedDisg);
                ResultFsecService.Instance.ApplyFsec(preparedFsec);
                ResultReacService.Instance.ApplyReac(preparedReac);
                InputNodesService.Instance.ApplyNodes(preparedNodes);
                if (hasResult)
                {
                    combineCoordinator.CompleteLoad(loadedResultDimension ?? loadedDimension);
                    combineFsecCoordinator.CompleteLoad(loadedResultDimension ?? loadedDimension);
                    combineReacCoordinator.CompleteLoad(loadedResultDimension ?? loadedDimension);
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
            CalculationInputRevision++;
            if (previousDimension != loadedDimension)
                notifications.PublishFirst(DimensionChanged, loadedDimension);
            notifications.PublishFirst(FileReplaced, DocumentRevision);
            _publishingDocumentReplacement = true;
            try { notifications.Complete(); }
            finally { _publishingDocumentReplacement = false; }
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

            var saved = new Dictionary<string, object> {
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
                ["bridge_loads"] = InputBridgeLoadService.Instance.GetSaveJson(),
                ["notice_points"] = InputNoticePointsService.Instance.getNoticePointsJson(),
                ["define"] = InputCombineService.Instance.getDefineJson(),
                ["combine"] = InputCombineService.Instance.getCombineJson(),
                ["pickup"] = InputCombineService.Instance.getPickupJson(),
            };

            if (InputLoadService.Instance.AssignedIntensityRowIndices.Any())
                saved["load_intensity_layout"] = InputLoadService.Instance.GetIntensityLayoutJson();

            var results = MergeResults(
                ResultDisgService.Instance.getDisgJson(),
                ResultReacService.Instance.getReacJson(),
                ResultFsecService.Instance.getFsecJson());
            if (results.Count != 0)
            {
                // Legacy result readers require every case to have a displacement
                // section, even when this document only contains reactions/forces.
                foreach (var value in results.Values)
                    ((Dictionary<string, object>)value).TryAdd("disg", new Dictionary<string, object>());
                saved["result"] = results;
                saved["resultDimension"] = ResultDimension ?? dimension;
            }
            return saved;
        }

        private static Dictionary<string, object> MergeResults(params Dictionary<string, object>[] sections)
        {
            var merged = new Dictionary<string, object>();
            foreach (var section in sections)
                foreach (var (caseId, value) in section)
                {
                    if (value is not Dictionary<string, object> fields)
                        throw new InvalidOperationException($"Invalid result case '{caseId}'.");
                    if (!merged.TryGetValue(caseId, out var existing))
                        merged.Add(caseId, new Dictionary<string, object>(fields));
                    else
                        foreach (var (name, data) in fields)
                            ((Dictionary<string, object>)existing).Add(name, data);
                }
            return merged;
        }
    }
}

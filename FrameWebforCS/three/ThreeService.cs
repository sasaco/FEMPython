using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using SingleFormsDemo;
using System.Globalization;
using THREE;

namespace FrameWebforCS.three;

/// <summary>
/// Node-only counterpart of JS three.service.ts. JS calls fileload/changeData/ChangeMode
/// directly from components; C# receives file, grid, and sidebar events and applies them
/// before the next GL frame. Member, panel, load, and result owners are not ported here yet.
/// Crosswalk: fileload -> OnFileReplaced/ReplaceAll; changeData("nodes") -> OnNodeEdited;
/// ChangeMode("nodes") -> OnInputModeChanged; detectObject -> SelectAt.
/// </summary>
internal sealed class ThreeService : IDisposable
{
    private readonly SceneService _scene;
    private readonly InputNodesService _inputNodes;
    private readonly InputDataService _inputData;
    private readonly AppRoutingModule _routing;
    private readonly ThreeNodesService _nodes;
    private readonly object _pendingLock = new();
    private readonly HashSet<int> _pendingIds = new();
    private long _pendingRevision;
    private bool _replacePending = true;
    private bool _disposed;

    internal ThreeService(SceneService scene)
    {
        _scene = scene;
        _inputNodes = InputNodesService.Instance;
        _inputData = InputDataService.Instance;
        _routing = AppRoutingModule.Instance;
        _pendingRevision = _inputData.DocumentRevision;
        _nodes = new ThreeNodesService(scene.scene);
        _nodes.SetNodeMode(_routing.ActiveModeKey == "node");
        _inputNodes.NodeEdited += OnNodeEdited;
        _inputData.FileReplaced += OnFileReplaced;
        _routing.InputModeChanged += OnInputModeChanged;
    }

    internal int? SelectedNodeId => _nodes.SelectedNodeId;
    internal int NodeCount => _nodes.NodeCount;

    internal void SelectNode(int? id)
    {
        FlushPending();
        _nodes.Select(id);
    }

    private void OnNodeEdited(int id)
    {
        // JS input-nodes.component calls three.changeData("nodes") after an edit.
        // The WinForms BindingList can raise several edits before one frame, so keep IDs once.
        lock (_pendingLock)
            _pendingIds.Add(id);
    }

    private void OnFileReplaced(long revision)
    {
        // JS menu calls three.fileload() after loadInputData(). The revision drops edits
        // queued for the previous file and also covers files opened before GL initialization.
        lock (_pendingLock)
        {
            _pendingRevision = revision;
            _pendingIds.Clear();
            _replacePending = true;
        }
    }

    private void OnInputModeChanged(string modeKey)
    {
        // Scene changes are made only from FlushPending on the GL-owning thread.
        lock (_pendingLock)
            _pendingMode = modeKey;
    }

    private string? _pendingMode;

    internal void FlushPending()
    {
        bool replace;
        int[] ids;
        string? mode;
        long revision;
        lock (_pendingLock)
        {
            if (_disposed)
                return;
            replace = _replacePending;
            _replacePending = false;
            ids = _pendingIds.ToArray();
            _pendingIds.Clear();
            mode = _pendingMode;
            _pendingMode = null;
            revision = _pendingRevision;
        }

        if (revision != _inputData.DocumentRevision)
            replace = true;

        if (replace)
        {
            var displayNodes = new Dictionary<int, Vector3>();
            foreach (var (id, node) in _inputNodes.getNodeJson(0))
                displayNodes.Add(int.Parse(id, CultureInfo.InvariantCulture),
                    new Vector3(node.X!.Value, node.Y!.Value, node.Z!.Value));
            _nodes.ReplaceAll(displayNodes);
        }
        else
        {
            foreach (int id in ids)
                _nodes.UpdateNode(id, _inputNodes.GetDisplayNode(id), deferScale: true);
            if (ids.Length > 0)
                _nodes.FinishNodeUpdates();
        }

        if (mode != null)
            _nodes.SetNodeMode(mode == "node");
    }

    internal int? SelectAt(int x, int y, int width, int height)
    {
        // JS ThreeComponent handles pointerdown. WinForms waits for a short MouseUp click
        // so TrackballControls drags do not select a node; grid selection is still pending.
        if (width <= 0 || height <= 0 || _routing.ActiveModeKey != "node")
            return null;

        FlushPending();
        _scene.scene.UpdateMatrixWorld(true);
        var camera = _scene.CurrentCamera;
        camera.UpdateMatrixWorld(true);
        var raycaster = new Raycaster();
        raycaster.SetFromCamera(new Vector2(2f * x / width - 1f, 1f - 2f * y / height), camera);
        int? id = _nodes.Pick(raycaster);
        _nodes.Select(id);
        return id;
    }

    public void Dispose()
    {
        lock (_pendingLock)
        {
            if (_disposed)
                return;
            _disposed = true;
            _pendingIds.Clear();
        }
        _inputNodes.NodeEdited -= OnNodeEdited;
        _inputData.FileReplaced -= OnFileReplaced;
        _routing.InputModeChanged -= OnInputModeChanged;
        _nodes.Dispose();
    }
}

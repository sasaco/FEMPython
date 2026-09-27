using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

/// <summary>
/// C# counterpart of JS geometry/three-nodes.service.ts. Owns node markers on the GL thread.
/// JS keeps one Mesh per ID in nodeList; the fixed 100,000-row C# grid uses one InstancedMesh
/// and an explicit ID map so draw calls and lookup remain bounded without relying on "node" + ID names.
/// Crosswalk: nodeList -> _root/_markers/_indexById; selectionItem -> _selection/SelectedNodeId;
/// baseScale -> _baseScale; changeData -> ReplaceAll/UpdateNode; detectObject -> Pick/Select.
/// </summary>
internal sealed class ThreeNodesService : IDisposable
{
    private const int MaximumNodes = 100_000;
    private const int NormalColor = 0x00A5FF;
    private const int SelectedColor = 0xFF0000;

    private readonly Scene _scene;
    private readonly Group _root = new() { Name = "nodes" };
    private readonly SphereBufferGeometry _geometry = new(1, 8, 6);
    private readonly MeshBasicMaterial _normalMaterial = new();
    private readonly MeshBasicMaterial _selectedMaterial = new();
    private readonly InstancedMesh _markers;
    private readonly Mesh _selection;
    private readonly Dictionary<int, int> _indexById = new();
    private readonly List<int> _ids = new();
    private readonly List<Vector3> _positions = new();
    private bool _nodeMode;
    private bool _disposed;
    private float _baseScale = 1;
    private double _minDistance = double.PositiveInfinity;

    internal ThreeNodesService(Scene scene)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _normalMaterial.Color = Color.Hex(NormalColor);
        _selectedMaterial.Color = Color.Hex(SelectedColor);
        _markers = new InstancedMesh(_geometry, _normalMaterial, MaximumNodes)
        {
            Name = "node-markers",
            InstanceCount = 0
        };
        _markers.InstanceMatrix.SetUsage(Constants.DynamicDrawUsage);
        _selection = new Mesh(_geometry, _selectedMaterial)
        {
            Name = "selected-node",
            Visible = false
        };
        _root.Add(_markers);
        _root.Add(_selection);
        _scene.Add(_root);
    }

    internal int NodeCount => _ids.Count;
    internal int? SelectedNodeId { get; private set; }
    internal float BaseScale => _baseScale;
    internal double MinDistance => _minDistance;

    internal void ReplaceAll(IReadOnlyDictionary<int, Vector3> nodes)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(nodes);
        if (nodes.Count > MaximumNodes)
            throw new ArgumentOutOfRangeException(nameof(nodes), $"At most {MaximumNodes} nodes are supported.");

        // Validate before changing the visible scene.
        foreach (var (id, position) in nodes)
            Validate(id, position);

        Select(null);
        _indexById.Clear();
        _ids.Clear();
        _positions.Clear();
        var extrema = NodeDistanceExtrema.Find(nodes.Values.ToArray());
        _minDistance = extrema.MinDistance;
        _baseScale = GetBaseScale(extrema);

        foreach (var (id, position) in nodes)
        {
            var index = _ids.Count;
            _indexById.Add(id, index);
            _ids.Add(id);
            _positions.Add(new Vector3(position.X, position.Y, position.Z));
            WriteMatrix(index, position);
        }

        _markers.InstanceCount = _ids.Count;
        _markers.InstanceMatrix.NeedsUpdate = true;
    }

    internal void UpdateNode(int id, Vector3? position, bool deferScale = false)
    {
        ThrowIfDisposed();
        if (position is null)
        {
            if (!_indexById.Remove(id, out var index)) return;
            if (SelectedNodeId == id) Select(null);

            var last = _ids.Count - 1;
            if (index != last)
            {
                var movedId = _ids[last];
                var movedPosition = _positions[last];
                _ids[index] = movedId;
                _positions[index] = movedPosition;
                _indexById[movedId] = index;
                WriteMatrix(index, movedPosition);
                _markers.InstanceMatrix.NeedsUpdate = true;
            }
            _ids.RemoveAt(last);
            _positions.RemoveAt(last);
            _markers.InstanceCount = last;
            if (!deferScale) RefreshBaseScale();
            return;
        }

        Validate(id, position);
        if (!_indexById.TryGetValue(id, out var target))
        {
            if (_ids.Count == MaximumNodes)
                throw new ArgumentOutOfRangeException(nameof(id), $"At most {MaximumNodes} nodes are supported.");
            target = _ids.Count;
            _indexById.Add(id, target);
            _ids.Add(id);
            _positions.Add(new Vector3(position.X, position.Y, position.Z));
            _markers.InstanceCount = _ids.Count;
        }
        else
        {
            _positions[target].Copy(position);
        }

        WriteMatrix(target, position);
        _markers.InstanceMatrix.NeedsUpdate = true;
        if (SelectedNodeId == id) UpdateSelectionPosition();
        if (!deferScale) RefreshBaseScale();
    }

    // JS changeData calls setBaseScale/onResize after each data change. The coordinator
    // batches paste-like edits, then calls this once so every edited marker gets the same scale.
    internal void FinishNodeUpdates()
    {
        ThrowIfDisposed();
        RefreshBaseScale();
    }

    internal void SetNodeMode(bool enabled)
    {
        ThrowIfDisposed();
        // JS visibleChange(flag, text, gui) also controls node-number labels and the scale GUI.
        // Those features are outside this node-body slice; markers stay visible in other modes.
        _nodeMode = enabled;
        if (!enabled) Select(null);
    }

    internal void Select(int? id)
    {
        ThrowIfDisposed();
        // JS selectChange/detectObject recolor individual Mesh materials. Shared instancing
        // uses one red overlay instead; matching JS's black unselected markers remains open.
        SelectedNodeId = _nodeMode && id is int value && _indexById.ContainsKey(value) ? value : null;
        _selection.Visible = SelectedNodeId.HasValue;
        if (_selection.Visible) UpdateSelectionPosition();
    }

    internal int? Pick(Raycaster raycaster)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(raycaster);
        if (!_nodeMode || _ids.Count == 0) return null;

        // JS detectObject emits nodeSelected$ with a position for grid synchronization.
        // This slice returns the stable ID; grid synchronization and hover remain unfinished.
        _root.UpdateWorldMatrix(true, true);
        var hits = raycaster.IntersectObject(_markers);
        if (hits.Count == 0) return null;

        // This THREE port sorts by truncating the distance delta to int.
        Intersection? nearest = null;
        foreach (var hit in hits)
        {
            if (hit.instanceId < 0 || hit.instanceId >= _ids.Count) continue;
            if (nearest is null || hit.distance < nearest.distance) nearest = hit;
        }
        return nearest is null ? null : _ids[nearest.instanceId];
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _scene.Remove(_root);
        _root.Remove(_markers);
        _root.Remove(_selection);
        _markers.Dispose();
        _selection.Dispose();
        _root.Dispose();
        _normalMaterial.Dispose();
        _selectedMaterial.Dispose();
        _geometry.Dispose();
        _indexById.Clear();
        _ids.Clear();
        _positions.Clear();
    }

    private void UpdateSelectionPosition()
    {
        var position = _positions[_indexById[SelectedNodeId!.Value]];
        _selection.Position.Copy(position);
        _selection.Scale.Set(_baseScale * 1.3f, _baseScale * 1.3f, _baseScale * 1.3f);
    }

    private void WriteMatrix(int index, Vector3 position)
    {
        var values = _markers.InstanceMatrix.Array;
        var offset = index * 16;
        values[offset] = _baseScale;
        values[offset + 1] = 0;
        values[offset + 2] = 0;
        values[offset + 3] = 0;
        values[offset + 4] = 0;
        values[offset + 5] = _baseScale;
        values[offset + 6] = 0;
        values[offset + 7] = 0;
        values[offset + 8] = 0;
        values[offset + 9] = 0;
        values[offset + 10] = _baseScale;
        values[offset + 11] = 0;
        values[offset + 12] = position.X;
        values[offset + 13] = position.Y;
        values[offset + 14] = position.Z;
        values[offset + 15] = 1;
    }

    private void RefreshBaseScale()
    {
        var extrema = NodeDistanceExtrema.Find(_positions);
        _minDistance = extrema.MinDistance;
        float nextScale = GetBaseScale(extrema);
        if (nextScale == _baseScale)
        {
            if (SelectedNodeId.HasValue) UpdateSelectionPosition();
            return;
        }
        _baseScale = nextScale;
        for (int i = 0; i < _positions.Count; i++)
            WriteMatrix(i, _positions[i]);
        _markers.InstanceMatrix.NeedsUpdate = true;
        if (SelectedNodeId.HasValue) UpdateSelectionPosition();
    }

    private static float GetBaseScale(NodeDistanceExtrema extrema)
    {
        // Match JS setBaseScale(): ignore zero-distance pairs, then use
        // max(maxDistance / 500, minDistance / 50), or 1 without a distinct pair.
        // A single unordered pair has the same distances as JS's ordered double loop.
        // JS also updates center, sizeNode, and scene helpers; those consumers are not ported.
        return double.IsPositiveInfinity(extrema.MinDistance)
            ? 1
            : (float)Math.Max(extrema.MaxDistance / 500, extrema.MinDistance / 50);
    }

    private static void Validate(int id, Vector3 position)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(position);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
            throw new ArgumentOutOfRangeException(nameof(position), "Node coordinates must be finite.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

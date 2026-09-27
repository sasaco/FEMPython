using FrameWebforCS.components.input;
using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

/// <summary>Owns panel geometry corresponding to JS geometry/three-panel.service.ts.</summary>
internal sealed class ThreePanelsService : IDisposable
{
    private readonly Scene _scene;
    private readonly Group _root = new() { Name = "panels" };
    // JS panelList contains one named Mesh for each triangle, including two for a quad.
    // The dictionary retains the panel ID so either triangle selects the same grid row.
    private readonly Dictionary<int, List<Mesh>> _panelList = new();
    private bool _disposed;
    private bool _selectable;
    private float _opacity = 0.7f;

    internal ThreePanelsService(Scene scene)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _scene.Add(_root);
    }

    internal int PanelCount => _panelList.Count;
    internal int? SelectedPanelId { get; private set; }
    internal float Opacity => _opacity;

    internal void ReplaceAll(IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, DisplayPanel> panels)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(panels);
        foreach (var (id, panel) in panels) Validate(id, panel);
        ClearData();
        foreach (var (id, panel) in panels) AddPanel(id, panel, nodes);
    }

    internal void UpdatePanel(int id, DisplayPanel? panel, IReadOnlyDictionary<int, Vector3> nodes)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(nodes);
        if (panel is { } value) Validate(id, value);
        RemovePanel(id);
        if (panel is { } next) AddPanel(id, next, nodes);
    }

    internal void SetMode(bool visible, float opacity)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(opacity) || opacity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity));
        _root.Visible = visible;
        _opacity = opacity;
        // JS visibleChange(true, 0.7) enables the panel GUI and selection;
        // visibleChange(true, 0.3) is contextual background geometry.
        _selectable = visible && opacity >= 0.7f;
        if (!_selectable) Select(null);
        foreach (var meshes in _panelList.Values)
            foreach (var mesh in meshes) mesh.Material.Opacity = opacity;
        // JS meshScale GUI calls a gmsh HTTP endpoint; native meshing GUI is pending.
    }

    internal void Select(int? id, int selectedColor = 0x00AFAF)
    {
        ThrowIfDisposed();
        SelectedPanelId = _selectable && id.HasValue && _panelList.ContainsKey(id.Value) ? id : null;
        foreach (var (panelId, meshes) in _panelList)
            foreach (var mesh in meshes)
                mesh.Material.Color = Color.Hex(panelId == SelectedPanelId ? selectedColor : 0x7F8F9F);
    }

    internal int? Pick(Raycaster raycaster)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(raycaster);
        if (!_selectable || _panelList.Count == 0) return null;
        _root.UpdateWorldMatrix(true, true);
        Intersection? nearest = null;
        int? selectedId = null;
        foreach (var (id, meshes) in _panelList)
            foreach (var mesh in meshes)
                foreach (var hit in raycaster.IntersectObject(mesh))
                    if (nearest is null || hit.distance < nearest.distance)
                    {
                        nearest = hit;
                        selectedId = id;
                    }
        return selectedId;
    }

    public void Dispose()
    {
        if (_disposed) return;
        ClearData();
        _disposed = true;
        _scene.Remove(_root);
        _root.Dispose();
    }

    private void ClearData()
    {
        foreach (var id in _panelList.Keys.ToArray()) RemovePanel(id);
        SelectedPanelId = null;
    }

    private void RemovePanel(int id)
    {
        if (!_panelList.Remove(id, out var meshes)) return;
        if (SelectedPanelId == id) SelectedPanelId = null;
        var material = meshes[0].Material;
        foreach (var mesh in meshes)
        {
            _root.Remove(mesh);
            mesh.Geometry.Dispose();
            mesh.Dispose();
        }
        // The two triangles of a quadrilateral share one material, as in JS.
        material.Dispose();
    }

    private void AddPanel(int id, DisplayPanel panel, IReadOnlyDictionary<int, Vector3> nodes)
    {
        var vertices = new Vector3[panel.Nodes.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // JS three-panel.service.ts:123-130 checks index positions in Object.keys(nodeData),
            // which drops sparse node IDs. Direct stable-ID lookup fixes that; no behavior remains pending.
            if (!nodes.TryGetValue(panel.Nodes[i], out var vertex)) return;
            vertices[i] = vertex;
        }
        // JS createPanel makes one Mesh per triangle in this vertex order.
        var material = new MeshBasicMaterial
        {
            Color = Color.Hex(0x7F8F9F),
            Side = Constants.DoubleSide,
            Transparent = true,
            Opacity = _opacity
        };
        var meshes = new List<Mesh>(vertices.Length == 3 ? 1 : 2);
        meshes.Add(CreateTriangle(vertices[0], vertices[1], vertices[2], id, material));
        if (vertices.Length == 4)
            meshes.Add(CreateTriangle(vertices[3], vertices[0], vertices[2], id, material));
        _panelList.Add(id, meshes);
        foreach (var mesh in meshes) _root.Add(mesh);
    }

    private static Mesh CreateTriangle(Vector3 a, Vector3 b, Vector3 c, int id,
        MeshBasicMaterial material)
    {
        var geometry = new BufferGeometry();
        geometry.SetAttribute("position", new BufferAttribute<float>(
            new[] { a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z }, 3));
        geometry.ComputeVertexNormals();
        return new Mesh(geometry, material) { Name = "panel-" + id };
    }

    private static void Validate(int id, DisplayPanel panel)
    {
        if (id <= 0 || panel.Nodes is not { Length: 3 or 4 } ||
            panel.Nodes.Any(nodeId => nodeId <= 0) || panel.Nodes.Distinct().Count() != panel.Nodes.Length)
            throw new ArgumentOutOfRangeException(nameof(panel));
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

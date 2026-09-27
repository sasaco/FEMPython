using FrameWebforCS.providers;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace FrameWebforCS.components.input;

internal readonly record struct DisplayPanel(int Element, int[] Nodes);

internal sealed class clsPanel : INotifyPropertyChanged
{
    private string? _e;
    private string? _point1;
    private string? _point2;
    private string? _point3;
    private string? _point4;

    public event PropertyChangedEventHandler? PropertyChanged;
    public string? E { get => _e; set => Set(ref _e, value, nameof(E)); }
    public string? Point1 { get => _point1; set => Set(ref _point1, value, nameof(Point1)); }
    public string? Point2 { get => _point2; set => Set(ref _point2, value, nameof(Point2)); }
    public string? Point3 { get => _point3; set => Set(ref _point3, value, nameof(Point3)); }
    public string? Point4 { get => _point4; set => Set(ref _point4, value, nameof(Point4)); }
    public bool IsEmpty => string.IsNullOrWhiteSpace(E) && string.IsNullOrWhiteSpace(Point1) &&
        string.IsNullOrWhiteSpace(Point2) && string.IsNullOrWhiteSpace(Point3) && string.IsNullOrWhiteSpace(Point4);

    private void Set(ref string? field, string? value, string name)
    {
        if (field == value) return;
        field = value;
        DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(name));
    }
}

internal sealed class InputPanelService
{
    private const int MaximumPanels = 100_000;
    private static readonly Lazy<InputPanelService> _instance = new(() => new InputPanelService());
    internal static InputPanelService Instance => _instance.Value;

    private Dictionary<string, clsPanel> _panels = new();
    internal BindingList<clsPanel> Panels { get; } = new();
    internal event Action<int>? PanelEdited;

    private InputPanelService()
    {
        Panels.AllowNew = false;
        Panels.AllowRemove = false;
        Panels.RaiseListChangedEvents = false;
        for (int row = 0; row < MaximumPanels; row++) Panels.Add(new clsPanel());
        Panels.RaiseListChangedEvents = true;
        Panels.ListChanged += OnListChanged;
    }

    internal void clear() => ApplyPanels(new Dictionary<string, clsPanel>());
    internal void setPanelJson(JsonElement jsonData) => ApplyPanels(ParsePanelJson(jsonData));

    internal static Dictionary<string, clsPanel> ParsePanelJson(JsonElement jsonData)
    {
        if (jsonData.ValueKind != JsonValueKind.Object)
            throw new JsonException("Input data must be an object.");
        if (!jsonData.TryGetProperty("shell", out var shell))
            return new Dictionary<string, clsPanel>();
        if (shell.ValueKind != JsonValueKind.Object)
            throw new JsonException("shell must be an object.");

        var result = new Dictionary<string, clsPanel>();
        foreach (var entry in shell.EnumerateObject())
        {
            if (!int.TryParse(entry.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int id) ||
                id is < 1 or > MaximumPanels || entry.Value.ValueKind != JsonValueKind.Object)
                throw new JsonException($"Invalid shell panel: {entry.Name}");
            string key = id.ToString(CultureInfo.InvariantCulture);
            if (result.ContainsKey(key)) throw new JsonException($"Duplicate shell panel: {entry.Name}");

            var value = entry.Value;
            if (!value.TryGetProperty("nodes", out var nodes) || nodes.ValueKind != JsonValueKind.Array ||
                nodes.GetArrayLength() is < 1 or > 4)
                throw new JsonException($"shell.{key}.nodes must contain one to four node IDs.");
            var parsed = new int[nodes.GetArrayLength()];
            int offset = 0;
            foreach (var node in nodes.EnumerateArray())
            {
                if (!TryReadPositiveId(node, out parsed[offset]))
                    throw new JsonException($"Invalid shell.{key}.nodes[{offset}].");
                offset++;
            }
            if (parsed.Distinct().Count() != parsed.Length)
                throw new JsonException($"shell.{key}.nodes contains duplicate IDs.");
            if (!value.TryGetProperty("e", out var element) || !TryReadNonnegativeId(element, out int e))
                throw new JsonException($"Invalid shell.{key}.e.");

            var row = new clsPanel { E = e.ToString(CultureInfo.InvariantCulture) };
            row.Point1 = parsed[0].ToString(CultureInfo.InvariantCulture);
            if (parsed.Length >= 2) row.Point2 = parsed[1].ToString(CultureInfo.InvariantCulture);
            if (parsed.Length >= 3) row.Point3 = parsed[2].ToString(CultureInfo.InvariantCulture);
            if (parsed.Length == 4) row.Point4 = parsed[3].ToString(CultureInfo.InvariantCulture);
            result.Add(key, row);
        }
        return result;
    }

    internal void ApplyPanels(Dictionary<string, clsPanel> panels)
    {
        ArgumentNullException.ThrowIfNull(panels);
        Panels.RaiseListChangedEvents = false;
        try
        {
            foreach (var id in _panels.Keys)
                if (!panels.ContainsKey(id)) Panels[int.Parse(id, CultureInfo.InvariantCulture) - 1] = new clsPanel();
            foreach (var (id, panel) in panels)
                Panels[int.Parse(id, CultureInfo.InvariantCulture) - 1] = panel;
            _panels = panels;
        }
        finally
        {
            Panels.RaiseListChangedEvents = true;
            DocumentReplacementNotifications.Defer(() => Panels.ResetBindings());
        }
    }

    internal IReadOnlyDictionary<int, DisplayPanel> GetDisplayPanels()
    {
        var result = new Dictionary<int, DisplayPanel>();
        foreach (var (id, row) in _panels)
            if (ToDisplayPanel(row) is { } display)
                result.Add(int.Parse(id, CultureInfo.InvariantCulture), display);
        return result;
    }

    internal DisplayPanel? GetDisplayPanel(int id) =>
        _panels.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var row) ? ToDisplayPanel(row) : null;

    internal Dictionary<string, object> getPanelJson()
    {
        var result = new Dictionary<string, object>();
        foreach (var (id, row) in _panels)
        {
            if (!int.TryParse(row.E, NumberStyles.None, CultureInfo.InvariantCulture, out int element) || element < 0)
                continue;
            var nodes = new List<int>(4);
            foreach (var point in new[] { row.Point1, row.Point2, row.Point3, row.Point4 })
                if (int.TryParse(point, NumberStyles.None, CultureInfo.InvariantCulture, out int node) && node > 0)
                    nodes.Add(node);
            // JS InputPanelService.getPanelJson saves an in-progress row with one node.
            // A drawable panel still requires three or four distinct nodes in ToDisplayPanel.
            if (nodes.Count > 0) result.Add(id, new { e = element, nodes });
        }
        return result;
    }

    private static DisplayPanel? ToDisplayPanel(clsPanel row)
    {
        if (!int.TryParse(row.E, NumberStyles.None, CultureInfo.InvariantCulture, out int element) || element < 0)
            return null;
        var points = new[] { row.Point1, row.Point2, row.Point3, row.Point4 };
        var ids = new List<int>(4);
        foreach (var point in points)
        {
            if (string.IsNullOrWhiteSpace(point)) continue;
            if (!int.TryParse(point, NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id <= 0)
                return null;
            ids.Add(id);
        }
        if (ids.Count is < 3 or > 4 || ids.Distinct().Count() != ids.Count) return null;
        return new DisplayPanel(element, ids.ToArray());
    }

    private void OnListChanged(object? sender, ListChangedEventArgs e)
    {
        if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0) return;
        int id = e.NewIndex + 1;
        string key = id.ToString(CultureInfo.InvariantCulture);
        clsPanel row = Panels[e.NewIndex];
        if (row.IsEmpty) _panels.Remove(key);
        else _panels[key] = row;
        DocumentReplacementNotifications.Publish(PanelEdited, id);
    }

    private static bool TryReadPositiveId(JsonElement value, out int id) =>
        TryReadNonnegativeId(value, out id) && id > 0;

    private static bool TryReadNonnegativeId(JsonElement value, out int id)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out id)) return id >= 0;
        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out id)) return id >= 0;
        id = 0;
        return false;
    }
}

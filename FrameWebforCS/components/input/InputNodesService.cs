using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using THREE;

namespace FrameWebforCS.components.input
{
    internal class clsNode : INotifyPropertyChanged
    {
        private float? _x;
        private float? _y;
        private float? _z;

        public event PropertyChangedEventHandler? PropertyChanged;

        [JsonPropertyName("x")]
        public float? X { get => _x; set => SetCoordinate(ref _x, value, nameof(X)); }

        [JsonPropertyName("y")]
        public float? Y { get => _y; set => SetCoordinate(ref _y, value, nameof(Y)); }

        [JsonPropertyName("z")]
        public float? Z { get => _z; set => SetCoordinate(ref _z, value, nameof(Z)); }

        [JsonIgnore]
        public bool IsEmpty => X == null && Y == null && Z == null;

        private void SetCoordinate(ref float? field, float? value, string name)
        {
            if (value.HasValue && !float.IsFinite(value.Value))
                throw new ArgumentOutOfRangeException(nameof(value), "Node coordinates must be finite.");
            if (field == value)
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    internal class InputNodesService
    {
        private const int MaxNodeId = 100_000;

        private static readonly Lazy<InputNodesService> _instance =
            new Lazy<InputNodesService>(() => new InputNodesService());

        public static InputNodesService Instance => _instance.Value;

        private Dictionary<string, clsNode> _node = new Dictionary<string, clsNode>();

        internal event Action<int>? NodeEdited;

        // Row index + 1 is the node ID; missing IDs are represented by empty rows.
        public BindingList<clsNode> Nodes { get; } = new BindingList<clsNode>();

        private InputNodesService()
        {
            Nodes.AllowNew = false;
            Nodes.AllowRemove = false;
            Nodes.RaiseListChangedEvents = false;
            for (int row = 0; row < MaxNodeId; row++)
                Nodes.Add(new clsNode());
            Nodes.RaiseListChangedEvents = true;
            Nodes.ListChanged += Nodes_ListChanged;
        }

        public void clear()
        {
            ReplaceRows(new Dictionary<string, clsNode>());
        }

        public void setNodeJson(JsonElement jsonData)
        {
            ApplyNodes(ParseNodeJson(jsonData));
        }

        internal static Dictionary<string, clsNode> ParseNodeJson(JsonElement jsonData)
        {
            if (jsonData.ValueKind != JsonValueKind.Object)
                throw new JsonException("Input data must be an object.");
            if (!jsonData.TryGetProperty("node", out JsonElement nodeJson))
                return new Dictionary<string, clsNode>();
            if (nodeJson.ValueKind != JsonValueKind.Object)
                throw new JsonException("node must be an object.");

            var nodes = JsonSerializer.Deserialize<Dictionary<string, clsNode>>(nodeJson.GetRawText())
                ?? throw new JsonException("Invalid node data.");

            var numberedNodes = new Dictionary<string, clsNode>();
            var seenIds = new HashSet<string>();
            foreach (var (id, node) in nodes)
            {
                if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int row) ||
                    row < 1 || row > MaxNodeId || node == null)
                    throw new JsonException($"Invalid node: {id}");
                string rowId = row.ToString(CultureInfo.InvariantCulture);
                if (!seenIds.Add(rowId))
                    throw new JsonException($"Duplicate node: {id}");
                if (!node.IsEmpty)
                    numberedNodes.Add(rowId, node);
            }

            return numberedNodes;
        }

        internal void ApplyNodes(Dictionary<string, clsNode> nodes) => ReplaceRows(nodes);

        internal Vector3? GetDisplayNode(int id)
        {
            // JS getNodeJson(0) scans every populated row. Use one ID for C# grid edits;
            // full file replacement still uses getNodeJson(0) in ThreeService.
            if (!_node.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var node))
                return null;
            return new Vector3(node.X ?? 0, node.Y ?? 0, node.Z ?? 0);
        }

        public Dictionary<string, clsNode> getNodeJson(float? empty = null)
        {
            // JS getNodeJson(empty) has the same null-for-save/zero-for-display behavior.
            // C# keeps sparse typed rows beside the fixed 100,000-row BindingList.
            var nodes = new Dictionary<string, clsNode>(_node.Count);
            foreach (var (id, node) in _node)
                nodes.Add(id, empty.HasValue
                    ? new clsNode { X = node.X ?? empty, Y = node.Y ?? empty, Z = node.Z ?? empty }
                    : node);
            return nodes;
        }

        private void Nodes_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0)
                return;

            var node = Nodes[e.NewIndex];
            string id = (e.NewIndex + 1).ToString(CultureInfo.InvariantCulture);
            if (node.IsEmpty)
                _node.Remove(id);
            else
                _node[id] = node;

            NodeEdited?.Invoke(e.NewIndex + 1);

        }

        private void ReplaceRows(Dictionary<string, clsNode> nextNodes)
        {
            Nodes.RaiseListChangedEvents = false;
            try
            {
                foreach (string id in _node.Keys)
                    if (!nextNodes.ContainsKey(id))
                        Nodes[int.Parse(id, CultureInfo.InvariantCulture) - 1] = new clsNode();
                foreach (var (id, node) in nextNodes)
                    Nodes[int.Parse(id, CultureInfo.InvariantCulture) - 1] = node;
                _node = nextNodes;
            }
            finally
            {
                Nodes.RaiseListChangedEvents = true;
                Nodes.ResetBindings();
            }
        }
    }
}

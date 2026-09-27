using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FrameWebforCS.components.result
{
    internal class clsDisg
    {
        public double? dx;
        public double? dy;
        public double? dz;
        public double? rx;
        public double? ry;
        public double? rz;
    }

    internal class ResultDisgService
    {
        private static readonly Lazy<ResultDisgService> _instance = new(() => new ResultDisgService());
        public static ResultDisgService Instance => _instance.Value;
        private Dictionary<string, Dictionary<string, clsDisg>> _disg = new();
        public event EventHandler? Changed;

        private ResultDisgService() { }

        public void clear()
        {
            _disg = new();
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        public Dictionary<string, Dictionary<string, clsDisg>> getDisg() => _disg;

        public void setDisgJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("result", out JsonElement result)) return;
            ApplyDisg(ParseDisgJson(jsonData));
        }

        internal static Dictionary<string, Dictionary<string, clsDisg>> ParseDisgJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("result", out JsonElement result))
                return new();
            if (result.ValueKind != JsonValueKind.Object)
                throw new JsonException("result must be a JSON object.");

            var candidate = new Dictionary<string, Dictionary<string, clsDisg>>();
            foreach (JsonProperty caseProperty in result.EnumerateObject())
            {
                if (caseProperty.Value.ValueKind != JsonValueKind.Object ||
                    !caseProperty.Value.TryGetProperty("disg", out JsonElement nodes) ||
                    nodes.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"result '{caseProperty.Name}' has no valid disg object.");
                var nodeValues = new Dictionary<string, clsDisg>();
                foreach (JsonProperty node in nodes.EnumerateObject())
                {
                    if (node.Value.ValueKind != JsonValueKind.Object)
                        throw new JsonException($"result '{caseProperty.Name}' node '{node.Name}' is invalid.");
                    var value = new clsDisg
                    {
                        dx = ReadComponent(node.Value, "dx"),
                        dy = ReadComponent(node.Value, "dy"),
                        dz = ReadComponent(node.Value, "dz"),
                        rx = ReadComponent(node.Value, "rx"),
                        ry = ReadComponent(node.Value, "ry"),
                        rz = ReadComponent(node.Value, "rz")
                    };
                    if (!nodeValues.TryAdd(node.Name, value))
                        throw new JsonException($"Duplicate displacement node '{node.Name}'.");
                }
                if (!candidate.TryAdd(caseProperty.Name, nodeValues))
                    throw new JsonException($"Duplicate result case '{caseProperty.Name}'.");
            }
            return candidate;
        }

        internal void ApplyDisg(Dictionary<string, Dictionary<string, clsDisg>> candidate)
        {
            _disg = candidate;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private static double? ReadComponent(JsonElement node, string name)
        {
            if (!node.TryGetProperty(name, out JsonElement component) ||
                component.ValueKind == JsonValueKind.Null) return null;
            if (component.ValueKind != JsonValueKind.Number ||
                !component.TryGetDouble(out double value) || !double.IsFinite(value))
                throw new JsonException($"Displacement component '{name}' must be a finite number or null.");
            return value;
        }

        public Dictionary<string, object> getDisgJson()
        {
            var disgs = new Dictionary<string, object>();
            foreach (KeyValuePair<string, Dictionary<string, clsDisg>> result in _disg)
            {
                var nodes = new Dictionary<string, object>();
                foreach (KeyValuePair<string, clsDisg> node in result.Value)
                    nodes.Add(node.Key, new Dictionary<string, object?>
                    {
                        ["dx"] = node.Value.dx, ["dy"] = node.Value.dy,
                        ["dz"] = node.Value.dz, ["rx"] = node.Value.rx,
                        ["ry"] = node.Value.ry, ["rz"] = node.Value.rz
                    });
                disgs.Add(result.Key, new Dictionary<string, object> { ["disg"] = nodes });
            }
            return disgs;
        }
    }
}

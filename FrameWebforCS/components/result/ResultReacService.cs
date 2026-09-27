using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FrameWebforCS.components.result
{
    internal class clsReac
    {
        public double? tx = null;
        public double? ty = null;
        public double? tz = null;
        public double? mx = null;
        public double? my = null;
        public double? mz = null;
    }

    internal class ResultReacService
    {
        // Lazy<T> を使ってスレッドセーフかつ遅延評価のシングルトンを実装
        private static readonly Lazy<ResultReacService> _instance =
            new Lazy<ResultReacService>(() => new ResultReacService());

        // 外部からはこのプロパティを通じてのみインスタンスにアクセスできる
        public static ResultReacService Instance => _instance.Value;

        public event EventHandler? Changed;


        private Dictionary<string, Dictionary<string, clsReac>> _reac;


        // コンストラクタを private にして、外部からの new を禁止する
        private ResultReacService()
        {
            this.clear();
        }

        public void clear()
        {
            this._reac = new Dictionary<string, Dictionary<string, clsReac>>();
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }
        public Dictionary<string, Dictionary<string, clsReac>> getReac()
        {
            return this._reac;
        }

        /// <summary>
        /// ファイルを読み込むとき
        /// </summary>
        /// <param name="jsonData"></param>
        public void setReacJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("result", out JsonElement results)) return;
            ApplyReac(ParseReacJson(jsonData));
        }

        internal static Dictionary<string, Dictionary<string, clsReac>> ParseReacJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("result", out JsonElement results))
                return new();
            if (results.ValueKind != JsonValueKind.Object)
                throw new JsonException("result must be a JSON object.");

            var candidate = new Dictionary<string, Dictionary<string, clsReac>>();
            foreach (JsonProperty result in results.EnumerateObject())
            {
                if (result.Value.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"result '{result.Name}' must be a JSON object.");
                if (!result.Value.TryGetProperty("reac", out JsonElement nodes)) continue;
                if (nodes.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"result '{result.Name}' reac must be a JSON object.");
                var parsedNodes = new Dictionary<string, clsReac>();
                foreach (JsonProperty node in nodes.EnumerateObject())
                {
                    if (node.Value.ValueKind != JsonValueKind.Object)
                        throw new JsonException($"reaction node '{node.Name}' must be a JSON object.");
                    var parsed = new clsReac
                    {
                        tx = ReadComponent(node.Value, "tx"),
                        ty = ReadComponent(node.Value, "ty"),
                        tz = ReadComponent(node.Value, "tz"),
                        mx = ReadComponent(node.Value, "mx"),
                        my = ReadComponent(node.Value, "my"),
                        mz = ReadComponent(node.Value, "mz")
                    };
                    if (!parsedNodes.TryAdd(node.Name, parsed))
                        throw new JsonException($"Duplicate reaction node '{node.Name}'.");
                }
                if (!candidate.TryAdd(result.Name, parsedNodes))
                    throw new JsonException($"Duplicate result case '{result.Name}'.");
            }
            return candidate;
        }

        internal void ApplyReac(Dictionary<string, Dictionary<string, clsReac>> candidate)
        {
            _reac = candidate;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private static double? ReadComponent(JsonElement source, string name)
        {
            if (!source.TryGetProperty(name, out JsonElement value) ||
                value.ValueKind == JsonValueKind.Null) return null;
            if (value.ValueKind != JsonValueKind.Number ||
                !value.TryGetDouble(out double parsed) || !double.IsFinite(parsed))
                throw new JsonException($"Reaction component '{name}' must be finite or null.");
            return parsed;
        }

        /// <summary>
        /// ファイルに保存するとき
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public Dictionary<string, object> getReacJson()
        {

            var reacs = new Dictionary<string, object>();
            foreach (KeyValuePair<string, Dictionary<string, clsReac>> result in this._reac)
            {
                var nodes = new Dictionary<string, object>();
                foreach (KeyValuePair<string, clsReac> node in result.Value)
                {
                    nodes.Add(node.Key, DataHelperModule.ClassToDictionary(node.Value));
                }
                reacs.Add(result.Key, new Dictionary<string, object> { ["reac"] = nodes });
            }
            return reacs;
        }

    }
}

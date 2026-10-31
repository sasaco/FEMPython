using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrameWebforCS.components.input
{
    internal class clsCombine<T> where T : struct
    {
        public int row;
        public string Id { get; set; } = "";
        public string? name;
        public Dictionary<int, T> Coefficients { get; } = new();

        public virtual bool IsEmpty => string.IsNullOrWhiteSpace(name) && Coefficients.Count == 0;
    }

    // GLOSSARY: 非線形解析の組合せ. Coefficients refer to basic cases directly; the analysis
    // settings are kept as written (targets/load_factors lists, doubles) until a screen edits them.
    internal sealed class clsNonlinearCombine : clsCombine<double>
    {
        internal static readonly string[] AnalysisKeys =
            { "n_load_steps", "max_iterations", "tolerance", "load_factors", "displacement_control" };

        public string? symbol;
        public JsonObject Analysis { get; } = new();

        public override bool IsEmpty => base.IsEmpty && string.IsNullOrWhiteSpace(symbol) && Analysis.Count == 0;
    }


    internal class InputCombineService
    {
        internal const int MaxRows = 100_000;
        // Lazy<T> を使ってスレッドセーフかつ遅延評価のシングルトンを実装
        private static readonly Lazy<InputCombineService> _instance =
            new Lazy<InputCombineService>(() => new InputCombineService());

        // 外部からはこのプロパティを通じてのみインスタンスにアクセスできる
        public static InputCombineService Instance => _instance.Value;

        private Dictionary<int, clsCombine<double>> _combine = new();
        private Dictionary<int, clsCombine<int>> _define = new();
        private Dictionary<int, clsCombine<int>> _pickup = new();
        private Dictionary<int, clsNonlinearCombine> _nonlinear = new();

        public IReadOnlyDictionary<int, clsNonlinearCombine> NonlinearRows => _nonlinear;
        public IReadOnlyDictionary<int, clsCombine<int>> DefineRows => _define;
        public IReadOnlyDictionary<int, clsCombine<double>> CombineRows => _combine;
        public IReadOnlyDictionary<int, clsCombine<int>> PickupRows => _pickup;
        public event EventHandler? RowsReplaced;
        public event EventHandler? RowsChanged;

        // コンストラクタを private にして、外部からの new を禁止する
        private InputCombineService() { }

        public void clear()
        {
            _combine = new();
            _define = new();
            _pickup = new();
            _nonlinear = new();
            DocumentReplacementNotifications.Publish(RowsReplaced, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        /// <summary>
        /// ファイルを読み込むとき
        /// </summary>
        /// <param name="jsonData"></param>
        public void setCombineJson(JsonElement jsonData)
        {
            var parsed = ParseCombineJson(jsonData);
            ApplyCombine(parsed.Combine, parsed.Define, parsed.Pickup, parsed.Nonlinear);
        }

        internal static (Dictionary<int, clsCombine<double>> Combine,
            Dictionary<int, clsCombine<int>> Define,
            Dictionary<int, clsCombine<int>> Pickup,
            Dictionary<int, clsNonlinearCombine> Nonlinear) ParseCombineJson(JsonElement jsonData)
        {
            Dictionary<int, clsCombine<double>> combine =
                JsonToDict(jsonData, "combine", ReadDouble, () => new clsCombine<double>());
            Dictionary<int, clsCombine<int>> define =
                JsonToDict(jsonData, "define", ReadInt32, () => new clsCombine<int>());
            Dictionary<int, clsCombine<int>> pickup =
                JsonToDict(jsonData, "pickup", ReadInt32, () => new clsCombine<int>());
            Dictionary<int, clsNonlinearCombine> nonlinear =
                JsonToDict(jsonData, "nonlinear_combine", ReadDouble, () => new clsNonlinearCombine(),
                    ReadNonlinearSetting);

            return (combine, define, pickup, nonlinear);
        }

        internal void ApplyCombine(Dictionary<int, clsCombine<double>> combine,
            Dictionary<int, clsCombine<int>> define,
            Dictionary<int, clsCombine<int>> pickup,
            Dictionary<int, clsNonlinearCombine>? nonlinear = null)
        {
            // 4 種類をすべて検証してから置き換え、読込失敗時は現在値を保持する。
            _combine = combine;
            _define = define;
            _pickup = pickup;
            _nonlinear = nonlinear ?? new();
            DocumentReplacementNotifications.Publish(RowsReplaced, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        // GLOSSARY: 2D非線形モード. Each mode keeps only its own combination sheets.
        internal void RemoveLinearCombinations() => ApplyCombine(new(), new(), new(), _nonlinear);

        internal void RemoveNonlinearCombinations() => ApplyCombine(_combine, _define, _pickup, new());

        private static bool ReadNonlinearSetting(clsNonlinearCombine item, JsonProperty property)
        {
            if (property.Name == "symbol")
            {
                if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                    return false;
                item.symbol = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
                return true;
            }
            if (Array.IndexOf(clsNonlinearCombine.AnalysisKeys, property.Name) < 0) return false;
            item.Analysis[property.Name] = JsonNode.Parse(property.Value.GetRawText());
            return true;
        }

        private static Dictionary<int, TRow> JsonToDict<T, TRow>(
            JsonElement jsonData,
            string key,
            Func<JsonElement, T> readCoefficient,
            Func<TRow> create,
            Func<TRow, JsonProperty, bool>? readOther = null) where T : struct where TRow : clsCombine<T>
        {
            if (!jsonData.TryGetProperty(key, out JsonElement combineJson))
                return new Dictionary<int, TRow>();
            if (combineJson.ValueKind != JsonValueKind.Object)
                throw new JsonException($"{key} がJSONオブジェクトとして定義されていません。");

            var combine = new Dictionary<int, TRow>();
            var occupiedIds = new HashSet<string>();
            var occupiedRows = new HashSet<int>();
            foreach (JsonProperty caseJson in combineJson.EnumerateObject())
            {
                if (caseJson.Value.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"{key} '{caseJson.Name}' の値が不正です。");

                TRow item = create();
                item.Id = caseJson.Name;
                bool hasRow = false;
                foreach (JsonProperty property in caseJson.Value.EnumerateObject())
                {
                    if (property.Name == "row")
                    {
                        if (property.Value.ValueKind != JsonValueKind.Number ||
                            !property.Value.TryGetInt32(out item.row) ||
                            item.row <= 0 || item.row > MaxRows)
                            throw new JsonException($"{key} '{caseJson.Name}' の row が不正です。");
                        hasRow = true;
                    }
                    else if (property.Name == "name")
                    {
                        if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                            throw new JsonException($"{key} '{caseJson.Name}' の name が不正です。");
                        item.name = property.Value.ValueKind == JsonValueKind.Null
                            ? null : property.Value.GetString();
                    }
                    else if (property.Name.StartsWith('C') &&
                             int.TryParse(property.Name.AsSpan(1), NumberStyles.None,
                                 CultureInfo.InvariantCulture, out int coefficientId) && coefficientId > 0)
                    {
                        if (property.Value.ValueKind == JsonValueKind.Null)
                            continue;

                        T coefficient;
                        try
                        {
                            coefficient = readCoefficient(property.Value);
                        }
                        catch (JsonException exception)
                        {
                            throw new JsonException(
                                $"{key} '{caseJson.Name}' の {property.Name} が不正です。",
                                exception);
                        }

                        if (!item.Coefficients.TryAdd(coefficientId, coefficient))
                            throw new JsonException($"{key} '{caseJson.Name}' の {property.Name} が重複しています。");
                    }
                    else if (readOther == null || !readOther(item, property))
                    {
                        throw new JsonException($"{key} '{caseJson.Name}' に未知の項目 {property.Name} があります。");
                    }
                }

                if (!hasRow || !occupiedIds.Add(caseJson.Name) ||
                    !occupiedRows.Add(item.row))
                    throw new JsonException($"{key} '{caseJson.Name}' の行番号またはIDが不正です。");
                if (!item.IsEmpty) combine.Add(item.row, item);
            }

            return combine;
        }

        private static double ReadDouble(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.Number ||
                !value.TryGetDouble(out double result) ||
                !double.IsFinite(result))
            {
                throw new JsonException("有限な数値ではありません。");
            }

            return result;
        }

        private static int ReadInt32(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.Number ||
                !value.TryGetInt32(out int result))
            {
                throw new JsonException("整数ではありません。");
            }

            return result;
        }

        public void SetDefineCoefficient(int row, int column, int? value)
        {
            if (SetCoefficient(_define, row, column, value))
                DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        public void SetCombineCoefficient(int row, int column, double? value)
        {
            if (value.HasValue && !double.IsFinite(value.Value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (SetCoefficient(_combine, row, column, value))
                DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        public void SetPickupCoefficient(int row, int column, int? value)
        {
            if (SetCoefficient(_pickup, row, column, value))
                DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        public void SetCombineName(int row, string? name)
        {
            if (SetName(_combine, row, name))
                DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        public void SetPickupName(int row, string? name)
        {
            if (SetName(_pickup, row, name))
                DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        private static bool SetCoefficient<T>(Dictionary<int, clsCombine<T>> rows,
            int row, int column, T? value) where T : struct
        {
            if (row < 1 || row > MaxRows || column < 1)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (!rows.TryGetValue(row, out clsCombine<T>? item))
            {
                if (!value.HasValue) return false;
                item = AddRow(rows, row);
            }
            if (value.HasValue)
            {
                if (item.Coefficients.TryGetValue(column, out T previous) &&
                    EqualityComparer<T>.Default.Equals(previous, value.Value))
                    return false;
                item.Coefficients[column] = value.Value;
            }
            else
            {
                if (!item.Coefficients.Remove(column)) return false;
            }
            if (item.IsEmpty) rows.Remove(row);
            return true;
        }

        private static bool SetName<T>(Dictionary<int, clsCombine<T>> rows,
            int row, string? name) where T : struct
        {
            if (row < 1 || row > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(row));
            string? normalizedName = string.IsNullOrWhiteSpace(name) ? null : name;
            if (!rows.TryGetValue(row, out clsCombine<T>? item))
            {
                if (normalizedName == null) return false;
                item = AddRow(rows, row);
            }
            if (item.name == normalizedName) return false;
            item.name = normalizedName;
            if (item.IsEmpty) rows.Remove(row);
            return true;
        }

        private static clsCombine<T> AddRow<T>(Dictionary<int, clsCombine<T>> rows,
            int row) where T : struct
        {
            string id = row.ToString(CultureInfo.InvariantCulture);
            while (rows.Values.Any(item => item.Id == id)) id = "row_" + id;
            var item = new clsCombine<T> { row = row, Id = id };
            rows.Add(row, item);
            return item;
        }

        /// <summary>
        /// ファイルに保存するとき
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        public Dictionary<string, object> getCombineJson()
        {
            return ClassToDictionary(this._combine);
        }
        public Dictionary<string, object> getDefineJson()
        {
            return ClassToDictionary(this._define);
        }
        public Dictionary<string, object> getPickupJson()
        {
            return ClassToDictionary(this._pickup);
        }
        public Dictionary<string, object> getNonlinearCombineJson()
        {
            return ClassToDictionary<double, clsNonlinearCombine>(this._nonlinear, (item, row) =>
            {
                if (item.symbol != null)
                    row["symbol"] = item.symbol;
                foreach (var (name, value) in item.Analysis)
                    row[name] = value?.DeepClone();
            });
        }

        private static Dictionary<string, object> ClassToDictionary<T>(
            Dictionary<int, clsCombine<T>> source) where T : struct =>
            ClassToDictionary<T, clsCombine<T>>(source, null);

        private static Dictionary<string, object> ClassToDictionary<T, TRow>(
            Dictionary<int, TRow> source,
            Action<TRow, Dictionary<string, object?>>? writeOther) where T : struct where TRow : clsCombine<T>
        {
            var combine = new Dictionary<string, object>();
            foreach (TRow item in source.Values)
            {
                var row = new Dictionary<string, object?>
                {
                    ["row"] = item.row
                };

                if (item.name != null)
                    row["name"] = item.name;

                foreach (KeyValuePair<int, T> coefficient in item.Coefficients)
                    row.Add("C" + coefficient.Key.ToString(CultureInfo.InvariantCulture), coefficient.Value);
                writeOther?.Invoke(item, row);
                combine.Add(item.Id, row);
            }
            return combine;
        }
    }
}

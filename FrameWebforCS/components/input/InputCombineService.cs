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

        public override bool IsEmpty => base.IsEmpty && string.IsNullOrWhiteSpace(symbol) && Analysis.Count == 0 &&
            _pendingMethod == null && _pendingAmount == null && _pendingDivisions == null;

        // The sheet's view of the analysis settings. A list that is not uniform (step × k) shows
        // as 個別指定 and is kept as written until the row's target, factor or divisions change.
        // A target or factor entered without divisions (or the reverse) is held here, unsaved,
        // until the list can be written.
        private NonlinearMethod? _pendingMethod;
        private double? _pendingAmount;
        private int? _pendingDivisions;

        private JsonObject? Control => Analysis["displacement_control"] as JsonObject;

        private JsonNode? List => Control != null ? Control["targets"] : Analysis["load_factors"];

        public NonlinearMethod? Method =>
            Control != null ? NonlinearMethod.Displacement
            : Analysis.ContainsKey("load_factors") || Analysis.ContainsKey("n_load_steps") ? NonlinearMethod.Load
            : _pendingMethod;

        public int? ControlNode => Integer(Control?["node"]);

        private double? Amount => List is JsonArray ? UniformLast(List) : _pendingAmount;

        public double? Target => Method == NonlinearMethod.Displacement ? Amount : null;

        public double? MaxFactor => Method == NonlinearMethod.Load ? Amount : null;

        public int? Divisions =>
            List is JsonArray list ? list.Count : Integer(Analysis["n_load_steps"]) ?? _pendingDivisions;

        public bool IsIndividual => List is JsonArray && UniformLast(List) == null;

        public int? MaxIterations => Integer(Analysis["max_iterations"]);

        public double? Tolerance => Number(Analysis["tolerance"]);

        internal void SetMethod(NonlinearMethod? method)
        {
            if (method == Method) return;
            int? divisions = Divisions;
            Analysis.Remove("displacement_control");
            Analysis.Remove("load_factors");
            Analysis.Remove("n_load_steps");
            _pendingMethod = method;
            if (method == NonlinearMethod.Displacement)
                Analysis["displacement_control"] = new JsonObject { ["dof"] = "dx" };
            WriteList(null, method == null ? null : divisions);
        }

        internal void SetControlNode(int? node)
        {
            if (Control is not JsonObject control)
                throw new ArgumentException("制御節点は、方式が変位増分の行に入れてください。");
            if (node is <= 0)
                throw new ArgumentException("制御節点は節点番号（1 以上の整数）で入れてください。");
            if (node == null) control.Remove("node");
            else control["node"] = node.Value;
        }

        // The target (変位増分) or the max factor JALF (荷重増分).
        internal void SetAmount(NonlinearMethod method, double? amount)
        {
            if (Method != method)
                throw new ArgumentException(method == NonlinearMethod.Displacement
                    ? "目標変位は、方式が変位増分の行に入れてください。"
                    : "最大倍率は、方式が荷重増分の行に入れてください。");
            if (amount is double value && (!double.IsFinite(value) || value == 0 ||
                    method == NonlinearMethod.Load && value < 0))
                throw new ArgumentException(method == NonlinearMethod.Displacement
                    ? "目標変位は 0 以外の数で入れてください（向きは符号で表します）。"
                    : "最大倍率は 0 より大きい数で入れてください。");
            WriteList(amount, Divisions);
        }

        internal void SetDivisions(int? divisions)
        {
            if (Method == null)
                throw new ArgumentException("分割数は、方式を選んでから入れてください。");
            if (divisions is <= 0)
                throw new ArgumentException("分割数は 1 以上の整数で入れてください。");
            // Re-dividing a 個別指定 list keeps its final value.
            double? amount = Amount ?? (List is JsonArray list ? Number(list[^1]) : null);
            WriteList(amount, divisions);
        }

        internal void SetMaxIterations(int? iterations)
        {
            if (iterations is <= 0)
                throw new ArgumentException("最大反復回数は 1 以上の整数で入れてください（空欄は 50）。");
            if (iterations == null) Analysis.Remove("max_iterations");
            else Analysis["max_iterations"] = iterations.Value;
        }

        internal void SetTolerance(double? tolerance)
        {
            if (tolerance is double value && (!double.IsFinite(value) || value <= 0))
                throw new ArgumentException("許容誤差は 0 より大きい数で入れてください（空欄は 1e-6）。");
            if (tolerance == null) Analysis.Remove("tolerance");
            else Analysis["tolerance"] = tolerance.Value;
        }

        // Writes step × k for k = 1..divisions with step = amount / divisions, as the JRSNAP
        // comparison models do. Load control without a max factor goes up to λ = 1 (n_load_steps).
        private void WriteList(double? amount, int? divisions)
        {
            NonlinearMethod? method = Method;
            _pendingMethod = method;
            _pendingAmount = null;
            _pendingDivisions = null;
            Control?.Remove("targets");
            Analysis.Remove("load_factors");
            Analysis.Remove("n_load_steps");
            if (amount is double total && divisions is int count)
            {
                double step = total/count;
                var list = new JsonArray(Enumerable.Range(1, count).Select(k => (JsonNode)(step*k)).ToArray());
                if (Control is JsonObject control) control["targets"] = list;
                else Analysis["load_factors"] = list;
            }
            else if (method == NonlinearMethod.Load && amount == null && divisions is int steps)
                Analysis["n_load_steps"] = steps;
            else
            {
                _pendingAmount = amount;
                _pendingDivisions = divisions;
            }
        }

        private static double? UniformLast(JsonNode? node)
        {
            if (node is not JsonArray list || list.Count == 0) return null;
            double[] values = list.Select(Number).OfType<double>().ToArray();
            if (values.Length != list.Count) return null;
            double step = values[0];
            double scale = Math.Abs(values[^1]);
            for (int k = 0; k < values.Length; k++)
                if (Math.Abs(values[k] - step*(k + 1)) > 1e-12*scale) return null;
            return values[^1];
        }

        // Values written here (int or double) and values read from a file are both numbers.
        private static double? Number(JsonNode? node) =>
            node is JsonValue value && value.GetValueKind() == JsonValueKind.Number &&
            double.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                ? number : null;

        private static int? Integer(JsonNode? node) =>
            Number(node) is double number && number == Math.Truncate(number) && Math.Abs(number) <= int.MaxValue
                ? (int)number : null;
    }

    internal enum NonlinearMethod { Displacement, Load }


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

        public void SetNonlinearName(int row, string? name)
        {
            if (SetName<double, clsNonlinearCombine>(_nonlinear, row, name))
                DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        public void SetNonlinearCoefficient(int row, int column, double? value)
        {
            if (value.HasValue && !double.IsFinite(value.Value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (SetCoefficient<double, clsNonlinearCombine>(_nonlinear, row, column, value))
                DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        // The analysis settings of a 非線形解析の組合せ row. Invalid entries throw ArgumentException
        // with a message for the user, and leave the row unchanged.
        public void SetNonlinearMethod(int row, NonlinearMethod? method) =>
            EditNonlinear(row, item => item.SetMethod(method));

        public void SetNonlinearControlNode(int row, int? node) =>
            EditNonlinear(row, item => item.SetControlNode(node));

        public void SetNonlinearTarget(int row, double? target) =>
            EditNonlinear(row, item => item.SetAmount(NonlinearMethod.Displacement, target));

        public void SetNonlinearMaxFactor(int row, double? factor) =>
            EditNonlinear(row, item => item.SetAmount(NonlinearMethod.Load, factor));

        public void SetNonlinearDivisions(int row, int? divisions) =>
            EditNonlinear(row, item => item.SetDivisions(divisions));

        public void SetNonlinearMaxIterations(int row, int? iterations) =>
            EditNonlinear(row, item => item.SetMaxIterations(iterations));

        public void SetNonlinearTolerance(int row, double? tolerance) =>
            EditNonlinear(row, item => item.SetTolerance(tolerance));

        private void EditNonlinear(int row, Action<clsNonlinearCombine> edit)
        {
            if (row < 1 || row > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(row));
            bool added = !_nonlinear.TryGetValue(row, out clsNonlinearCombine? item);
            item ??= AddRow<double, clsNonlinearCombine>(_nonlinear, row);
            try { edit(item); }
            catch
            {
                if (added) _nonlinear.Remove(row);
                throw;
            }
            if (item.IsEmpty) _nonlinear.Remove(row);
            DocumentReplacementNotifications.Publish(RowsChanged, this, EventArgs.Empty);
        }

        private static bool SetCoefficient<T>(Dictionary<int, clsCombine<T>> rows,
            int row, int column, T? value) where T : struct =>
            SetCoefficient<T, clsCombine<T>>(rows, row, column, value);

        private static bool SetCoefficient<T, TRow>(Dictionary<int, TRow> rows,
            int row, int column, T? value) where T : struct where TRow : clsCombine<T>, new()
        {
            if (row < 1 || row > MaxRows || column < 1)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (!rows.TryGetValue(row, out TRow? item))
            {
                if (!value.HasValue) return false;
                item = AddRow<T, TRow>(rows, row);
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
            int row, string? name) where T : struct =>
            SetName<T, clsCombine<T>>(rows, row, name);

        private static bool SetName<T, TRow>(Dictionary<int, TRow> rows,
            int row, string? name) where T : struct where TRow : clsCombine<T>, new()
        {
            if (row < 1 || row > MaxRows)
                throw new ArgumentOutOfRangeException(nameof(row));
            string? normalizedName = string.IsNullOrWhiteSpace(name) ? null : name;
            if (!rows.TryGetValue(row, out TRow? item))
            {
                if (normalizedName == null) return false;
                item = AddRow<T, TRow>(rows, row);
            }
            if (item.name == normalizedName) return false;
            item.name = normalizedName;
            if (item.IsEmpty) rows.Remove(row);
            return true;
        }

        private static TRow AddRow<T, TRow>(Dictionary<int, TRow> rows,
            int row) where T : struct where TRow : clsCombine<T>, new()
        {
            string id = row.ToString(CultureInfo.InvariantCulture);
            while (rows.Values.Any(item => item.Id == id)) id = "row_" + id;
            var item = new TRow { row = row, Id = id };
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

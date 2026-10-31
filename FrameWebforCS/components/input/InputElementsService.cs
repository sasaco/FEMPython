using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrameWebforCS.components.input
{
    internal class clsElement : INotifyPropertyChanged
    {
        public float? E = null;
        public float? G = null;
        public float? Xp = null;
        public float? A = null;
        public float? J = null;
        public float? Iy = null;
        public float? Iz = null;
        public string? n = null;
        // The engine's nonlinear material object (GLOSSARY: 骨格曲線), kept as written so that
        // fields the screens do not edit (_neg, laws, axial tables) and doubles survive a save.
        public JsonObject? nonlinear = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public float? ElasticModulus { get => E; set { E = value; Changed(nameof(ElasticModulus)); } }
        public float? ShearModulus { get => G; set { G = value; Changed(nameof(ShearModulus)); } }
        public float? Expansion { get => Xp; set { Xp = value; Changed(nameof(Expansion)); } }
        public float? Area { get => A; set { A = value; Changed(nameof(Area)); } }
        public float? Torsion { get => J; set { J = value; Changed(nameof(Torsion)); } }
        public float? InertiaY { get => Iy; set { Iy = value; Changed(nameof(InertiaY)); } }
        public float? InertiaZ { get => Iz; set { Iz = value; Changed(nameof(InertiaZ)); } }
        public string? Name { get => n; set { n = value; Changed(nameof(Name)); } }
        public JsonObject? Nonlinear { get => nonlinear; set { nonlinear = value; Changed(nameof(Nonlinear)); } }

        // GLOSSARY: 骨格曲線. The sheet's view of nonlinear: the kind and four points, M_k = P_k with
        // φ_k = delta_k (M-φ) or θ_k = theta_k (M-θ). Edits rewrite only those keys, so beta and
        // other fields are kept; a material the sheet cannot show (laws, asymmetric) is 個別指定
        // and kept as written. (Internal constants: ClassToDictionary saves public fields.)
        internal const string LinearSkeleton = "線形";
        internal const string CurvatureSkeleton = "M-φ";
        internal const string RotationSkeleton = "M-θ";
        internal const string IndividualSkeleton = "個別指定";

        public string Skeleton
        {
            get => nonlinear == null ? LinearSkeleton
                : !IsEditable(nonlinear) ? IndividualSkeleton
                : IsRotation ? RotationSkeleton : CurvatureSkeleton;
            set
            {
                if (value == Skeleton || value is not (LinearSkeleton or CurvatureSkeleton or RotationSkeleton))
                {
                    Changed(nameof(Skeleton));
                    return;
                }
                if (value == LinearSkeleton) nonlinear = null;
                else
                {
                    // Switching between M-φ and M-θ keeps the moments; the deformations differ in kind.
                    JsonObject next = nonlinear != null && IsEditable(nonlinear)
                        ? nonlinear.DeepClone().AsObject()
                        : new JsonObject
                        {
                            ["type"] = "jr_stiffness_reduction",
                            ["symmetric"] = true,
                            ["hysteresis_dofs"] = new JsonArray("moment_z"),
                        };
                    next.Remove("skeleton");
                    for (int k = 1; k <= 4; k++)
                    {
                        next.Remove($"delta_{k}");
                        next.Remove($"theta_{k}");
                    }
                    if (value == RotationSkeleton) next["skeleton"] = "m_theta";
                    nonlinear = next;
                }
                Changed(nameof(Skeleton));
            }
        }

        public double? M1 { get => Point("P_1"); set => SetPoint("P_1", value, nameof(M1)); }
        public double? M2 { get => Point("P_2"); set => SetPoint("P_2", value, nameof(M2)); }
        public double? M3 { get => Point("P_3"); set => SetPoint("P_3", value, nameof(M3)); }
        public double? M4 { get => Point("P_4"); set => SetPoint("P_4", value, nameof(M4)); }
        // φ_k for M-φ, θ_k for M-θ.
        public double? D1 { get => Point(Deformation(1)); set => SetPoint(Deformation(1), value, nameof(D1)); }
        public double? D2 { get => Point(Deformation(2)); set => SetPoint(Deformation(2), value, nameof(D2)); }
        public double? D3 { get => Point(Deformation(3)); set => SetPoint(Deformation(3), value, nameof(D3)); }
        public double? D4 { get => Point(Deformation(4)); set => SetPoint(Deformation(4), value, nameof(D4)); }

        private bool IsRotation => nonlinear?["skeleton"]?.GetValueKind() == JsonValueKind.String &&
            nonlinear["skeleton"]!.GetValue<string>() == "m_theta";

        private string Deformation(int k) => (IsRotation ? "theta_" : "delta_") + k.ToString(CultureInfo.InvariantCulture);

        private static bool IsEditable(JsonObject material) =>
            !material.ContainsKey("laws") &&
            (material["type"] is null || material["type"]!.ToJsonString() == "\"jr_stiffness_reduction\"") &&
            (material["symmetric"] is null || material["symmetric"]!.GetValueKind() == JsonValueKind.True);

        private double? Point(string key) =>
            nonlinear != null && IsEditable(nonlinear) && nonlinear[key] is JsonValue value &&
            value.GetValueKind() == JsonValueKind.Number &&
            double.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                ? number : null;

        private void SetPoint(string key, double? value, string property)
        {
            // A refused entry (linear or 個別指定 row, non-finite value) restores the bound cell.
            if (nonlinear == null || !IsEditable(nonlinear) || value is double number && !double.IsFinite(number))
            {
                Changed(property);
                return;
            }
            if (value == null) nonlinear.Remove(key);
            else nonlinear[key] = value.Value;
            Changed(property);
        }

        public bool IsEmpty => E == null && G == null && Xp == null && A == null && J == null &&
            Iy == null && Iz == null && string.IsNullOrWhiteSpace(n) && nonlinear == null;

        private void Changed(string name) => DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(name));
    }

    internal class InputElementsService
    {
        private const int MaxNodeId = 100_000;
        internal const int TypeCount = 6;
        private static readonly Lazy<InputElementsService> _instance = new(() => new InputElementsService());
        public static InputElementsService Instance => _instance.Value;

        private Dictionary<string, Dictionary<string, clsElement>> _element = new();
        private readonly Dictionary<string, BindingList<clsElement>> _rows = new();

        private InputElementsService()
        {
            for (int type = 1; type <= TypeCount; type++)
            {
                string sheetId = type.ToString(CultureInfo.InvariantCulture);
                var rows = new BindingList<clsElement> { AllowNew = false, AllowRemove = false, RaiseListChangedEvents = false };
                for (int row = 0; row < MaxNodeId; row++)
                    rows.Add(new clsElement());
                rows.RaiseListChangedEvents = true;
                rows.ListChanged += (_, e) => Rows_ListChanged(sheetId, rows, e);
                _rows.Add(sheetId, rows);
            }
        }

        public BindingList<clsElement> GetRows(int sheetId) => _rows[sheetId.ToString(CultureInfo.InvariantCulture)];

        public void clear() => ReplaceRows(new Dictionary<string, Dictionary<string, clsElement>>());

        public void setElementJson(JsonElement jsonData)
        {
            var parsed = ParseElementJson(jsonData);
            if (parsed != null) ApplyElements(parsed);
        }

        internal static Dictionary<string, Dictionary<string, clsElement>>? ParseElementJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("element", out JsonElement elementJson) ||
                elementJson.ValueKind != JsonValueKind.Object)
                return null;

            var next = new Dictionary<string, Dictionary<string, clsElement>>();
            var seenSheets = new HashSet<string>();
            foreach (JsonProperty sheetEntry in elementJson.EnumerateObject())
            {
                if (!int.TryParse(sheetEntry.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int type) ||
                    type < 1 || type > TypeCount || sheetEntry.Value.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"Invalid element sheet: {sheetEntry.Name}");
                string sheetId = type.ToString(CultureInfo.InvariantCulture);
                if (!seenSheets.Add(sheetId))
                    throw new JsonException($"Duplicate element sheet: {sheetEntry.Name}");
                var sheet = new Dictionary<string, clsElement>();
                var seenRows = new HashSet<string>();
                foreach (JsonProperty entry in sheetEntry.Value.EnumerateObject())
                {
                    if (!int.TryParse(entry.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int row) ||
                        row < 1 || row > MaxNodeId)
                        throw new JsonException($"Invalid element row: {entry.Name}");
                    string id = row.ToString(CultureInfo.InvariantCulture);
                    if (!seenRows.Add(id))
                        throw new JsonException($"Duplicate element row: {entry.Name}");
                    if (entry.Value.ValueKind == JsonValueKind.Object &&
                        entry.Value.TryGetProperty(nameof(clsElement.nonlinear), out JsonElement nonlinear) &&
                        nonlinear.ValueKind != JsonValueKind.Object)
                        throw new JsonException($"Invalid element nonlinear: {sheetEntry.Name}/{entry.Name}");
                    clsElement? element = DataHelperModule.JsonToClass<clsElement>(entry.Value);
                    if (element != null && !element.IsEmpty)
                        sheet.Add(id, element);
                }
                if (sheet.Count > 0)
                    next.Add(sheetId, sheet);
            }
            return next;
        }

        internal void ApplyElements(Dictionary<string, Dictionary<string, clsElement>>? parsed) =>
            ReplaceRows(parsed ?? new Dictionary<string, Dictionary<string, clsElement>>());

        public Dictionary<string, object> getElementJson()
        {
            var elements = new Dictionary<string, object>();
            foreach (var (sheetId, sheet) in _element)
            {
                var rows = new Dictionary<string, object>();
                foreach (var (id, element) in sheet)
                    if (!element.IsEmpty)
                    {
                        var row = DataHelperModule.ClassToDictionary(element);
                        // Linear rows keep their shape: the engine reads any nonlinear key.
                        if (element.nonlinear == null) row.Remove(nameof(clsElement.nonlinear));
                        else row[nameof(clsElement.nonlinear)] = element.nonlinear.DeepClone();
                        rows.Add(id, row);
                    }
                if (rows.Count > 0)
                    elements.Add(sheetId, rows);
            }
            return elements;
        }

        internal bool HasNonlinear => _element.Values.Any(sheet => sheet.Values.Any(row => row.nonlinear != null));

        // Leaving 2D非線形モード: clears the skeletons through the bound rows, as an edit would.
        internal void RemoveNonlinear()
        {
            foreach (clsElement row in _element.Values.SelectMany(sheet => sheet.Values)
                         .Where(row => row.nonlinear != null).ToList())
                row.Nonlinear = null;
        }

        internal static bool HasNonlinearRows(Dictionary<string, Dictionary<string, clsElement>>? parsed) =>
            parsed?.Values.Any(sheet => sheet.Values.Any(row => row.nonlinear != null)) == true;

        private void Rows_ListChanged(string sheetId, BindingList<clsElement> rows, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0)
                return;
            string id = (e.NewIndex + 1).ToString(CultureInfo.InvariantCulture);
            clsElement element = rows[e.NewIndex];
            if (!_element.TryGetValue(sheetId, out var sheet))
            {
                if (element.IsEmpty)
                    return;
                sheet = new Dictionary<string, clsElement>();
                _element.Add(sheetId, sheet);
            }
            if (element.IsEmpty)
            {
                sheet.Remove(id);
                if (sheet.Count == 0)
                    _element.Remove(sheetId);
            }
            else
                sheet[id] = element;
        }

        private void ReplaceRows(Dictionary<string, Dictionary<string, clsElement>> next)
        {
            foreach (var (sheetId, rows) in _rows)
            {
                rows.RaiseListChangedEvents = false;
                try
                {
                    _element.TryGetValue(sheetId, out var oldSheet);
                    next.TryGetValue(sheetId, out var newSheet);
                    if (oldSheet != null)
                        foreach (string id in oldSheet.Keys)
                            if (newSheet == null || !newSheet.ContainsKey(id))
                                rows[int.Parse(id, CultureInfo.InvariantCulture) - 1] = new clsElement();
                    if (newSheet != null)
                        foreach (var (id, element) in newSheet)
                            rows[int.Parse(id, CultureInfo.InvariantCulture) - 1] = element;
                }
                finally
                {
                    rows.RaiseListChangedEvents = true;
                    DocumentReplacementNotifications.Defer(() => rows.ResetBindings());
                }
            }
            _element = next;
        }
    }
}

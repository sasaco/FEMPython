using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        public float? ElasticModulus { get => E; set { E = value; Changed(nameof(ElasticModulus)); } }
        public float? ShearModulus { get => G; set { G = value; Changed(nameof(ShearModulus)); } }
        public float? Expansion { get => Xp; set { Xp = value; Changed(nameof(Expansion)); } }
        public float? Area { get => A; set { A = value; Changed(nameof(Area)); } }
        public float? Torsion { get => J; set { J = value; Changed(nameof(Torsion)); } }
        public float? InertiaY { get => Iy; set { Iy = value; Changed(nameof(InertiaY)); } }
        public float? InertiaZ { get => Iz; set { Iz = value; Changed(nameof(InertiaZ)); } }
        public string? Name { get => n; set { n = value; Changed(nameof(Name)); } }

        public bool IsEmpty => E == null && G == null && Xp == null && A == null && J == null &&
            Iy == null && Iz == null && string.IsNullOrWhiteSpace(n);

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
                        rows.Add(id, DataHelperModule.ClassToDictionary(element));
                if (rows.Count > 0)
                    elements.Add(sheetId, rows);
            }
            return elements;
        }

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

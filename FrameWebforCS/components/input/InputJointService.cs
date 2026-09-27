using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace FrameWebforCS.components.input
{
    internal class clsJoint : INotifyPropertyChanged
    {
        public int? row;
        public string? m = null;
        public float? xi = null;
        public float? yi = null;
        public float? zi = null;
        public float? xj = null;
        public float? yj = null;
        public float? zj = null;

        public event PropertyChangedEventHandler? PropertyChanged;
        public string? M { get => m; set { m = value; Changed(nameof(M)); } }
        public float? Xi { get => xi; set { xi = value; Changed(nameof(Xi)); } }
        public float? Yi { get => yi; set { yi = value; Changed(nameof(Yi)); } }
        public float? Zi { get => zi; set { zi = value; Changed(nameof(Zi)); } }
        public float? Xj { get => xj; set { xj = value; Changed(nameof(Xj)); } }
        public float? Yj { get => yj; set { yj = value; Changed(nameof(Yj)); } }
        public float? Zj { get => zj; set { zj = value; Changed(nameof(Zj)); } }
        public bool IsEmpty => string.IsNullOrWhiteSpace(m) && xi == null && yi == null && zi == null && xj == null && yj == null && zj == null;
        private void Changed(string name) => DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(name));
    }

    internal class InputJointService
    {
        private const int MaxNodeId = 100_000;
        private const int SheetCount = 6;
        private static readonly Lazy<InputJointService> _instance = new(() => new InputJointService());
        public static InputJointService Instance => _instance.Value;

        private Dictionary<string, List<clsJoint>> _joint = new();
        private readonly Dictionary<string, BindingList<clsJoint>> _sheets = new();
        internal event EventHandler? Changed;
        internal string SelectedCaseId { get; private set; } = "1";

        internal void SelectCase(string caseId)
        {
            if (!_sheets.ContainsKey(caseId)) throw new ArgumentOutOfRangeException(nameof(caseId));
            if (SelectedCaseId == caseId) return;
            SelectedCaseId = caseId;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        internal IReadOnlyList<clsJoint> GetDisplaySnapshot(string caseId) =>
            _joint.TryGetValue(caseId, out var rows)
                ? rows.Select(value => new clsJoint { row = value.row, m = value.m,
                    xi = value.xi, yi = value.yi, zi = value.zi,
                    xj = value.xj, yj = value.yj, zj = value.zj }).ToArray()
                : Array.Empty<clsJoint>();

        private InputJointService()
        {
            for (int sheet = 1; sheet <= SheetCount; sheet++)
            {
                string id = sheet.ToString(CultureInfo.InvariantCulture);
                var rows = new BindingList<clsJoint> { AllowNew = false, AllowRemove = false, RaiseListChangedEvents = false };
                for (int index = 0; index < MaxNodeId; index++) rows.Add(new clsJoint());
                rows.RaiseListChangedEvents = true;
                rows.ListChanged += (_, e) => RowsChanged(id, rows, e);
                _sheets.Add(id, rows);
            }
        }

        public BindingList<clsJoint> GetRows(string sheetName) => _sheets[sheetName];

        public void clear() => ReplaceRows(new Dictionary<string, List<clsJoint>>());

        public void setJointJson(JsonElement jsonData)
        {
            var loaded = ParseJointJson(jsonData);
            if (loaded != null) ApplyJoint(loaded);
        }

        internal static Dictionary<string, List<clsJoint>>? ParseJointJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("joint", out var source)) return null;
            ValidateSource(source);
            var loaded = DataHelperModule.JsonToDict(jsonData, "joint",
                static json => DataHelperModule.JsonToList<clsJoint>(json));
            if (loaded == null) throw new JsonException("Invalid joint data.");
            foreach (var sheet in source.EnumerateObject())
                if (!loaded.TryGetValue(sheet.Name, out var rows) || rows.Count != sheet.Value.GetArrayLength())
                    throw new JsonException($"Invalid joint sheet: {sheet.Name}");
            ValidateRows(loaded);
            return loaded;
        }

        private static void ValidateSource(JsonElement source)
        {
            if (source.ValueKind != JsonValueKind.Object) throw new JsonException("joint must be an object.");
            foreach (var sheet in source.EnumerateObject())
            {
                if (sheet.Value.ValueKind != JsonValueKind.Array) throw new JsonException("joint sheet must be an array.");
                foreach (var row in sheet.Value.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Object) throw new JsonException("joint row must be an object.");
                    foreach (var field in row.EnumerateObject())
                        if (field.Name is "xi" or "yi" or "zi" or "xj" or "yj" or "zj" &&
                            field.Value.ValueKind != JsonValueKind.Null &&
                            (field.Value.ValueKind != JsonValueKind.Number ||
                             !field.Value.TryGetSingle(out float value) || !float.IsFinite(value)))
                            throw new JsonException($"Invalid joint {field.Name}.");
                }
            }
        }

        internal void ApplyJoint(Dictionary<string, List<clsJoint>>? prepared) =>
            ReplaceRows(prepared ?? new Dictionary<string, List<clsJoint>>());

        public Dictionary<string, object> getJointJson()
        {
            var result = new Dictionary<string, object>();
            foreach (var (sheet, rows) in _joint)
            {
                var data = new List<Dictionary<string, object?>>();
                foreach (var value in rows.OrderBy(value => value.row))
                    if (!value.IsEmpty) data.Add(DataHelperModule.ClassToDictionary(value));
                if (data.Count > 0) result.Add(sheet, data);
            }
            return result;
        }

        private static void ValidateRows(Dictionary<string, List<clsJoint>> data)
        {
            foreach (var (sheet, values) in data)
            {
                if (!int.TryParse(sheet, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ||
                    number < 1 || number > SheetCount || sheet != number.ToString(CultureInfo.InvariantCulture))
                    throw new JsonException($"Invalid joint sheet: {sheet}");
                var seen = new HashSet<int>();
                foreach (var value in values)
                    if (value.row is not int row || row < 1 || row > MaxNodeId || !seen.Add(row))
                        throw new JsonException($"Invalid joint row: {value.row}");
            }
        }

        private void RowsChanged(string sheet, BindingList<clsJoint> rows, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0) return;
            var value = rows[e.NewIndex];
            value.row = e.NewIndex + 1;
            if (!_joint.TryGetValue(sheet, out var active))
                _joint[sheet] = active = new List<clsJoint>();
            active.RemoveAll(item => item.row == value.row);
            if (!value.IsEmpty) active.Add(value);
            if (active.Count == 0) _joint.Remove(sheet);
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private void ReplaceRows(Dictionary<string, List<clsJoint>> next)
        {
            foreach (var (sheet, rows) in _sheets)
            {
                rows.RaiseListChangedEvents = false;
                try
                {
                    if (_joint.TryGetValue(sheet, out var old))
                        foreach (var item in old) rows[item.row!.Value - 1] = new clsJoint();
                    if (next.TryGetValue(sheet, out var current))
                        foreach (var item in current) rows[item.row!.Value - 1] = item;
                }
                finally { rows.RaiseListChangedEvents = true; DocumentReplacementNotifications.Defer(() => rows.ResetBindings()); }
            }
            _joint = next;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }
    }
}

using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace FrameWebforCS.components.input
{
    internal class clsFixMember : INotifyPropertyChanged
    {
        public int row;
        public string? m = null;
        public float? tx = null;
        public float? ty = null;
        public float? tz = null;
        public float? tr = null;

        public event PropertyChangedEventHandler? PropertyChanged;
        public string? M { get => m; set { m = value; Changed(nameof(M)); } }
        public float? Tx { get => tx; set { tx = value; Changed(nameof(Tx)); } }
        public float? Ty { get => ty; set { ty = value; Changed(nameof(Ty)); } }
        public float? Tz { get => tz; set { tz = value; Changed(nameof(Tz)); } }
        public float? Tr { get => tr; set { tr = value; Changed(nameof(Tr)); } }
        public bool IsEmpty => string.IsNullOrWhiteSpace(m) && tx == null && ty == null && tz == null && tr == null;
        private void Changed(string name) => DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(name));
    }

    internal class InputFixMemberService
    {
        private const int MaxNodeId = 100_000;
        internal const int TypeCount = 6;
        private static readonly Lazy<InputFixMemberService> _instance = new(() => new InputFixMemberService());
        public static InputFixMemberService Instance => _instance.Value;

        private Dictionary<string, List<clsFixMember>> _fixMember = new();
        private readonly Dictionary<string, BindingList<clsFixMember>> _sheets = new();
        internal event EventHandler? Changed;
        internal string SelectedCaseId { get; private set; } = "1";

        internal void SelectCase(string caseId)
        {
            if (!_sheets.ContainsKey(caseId)) throw new ArgumentOutOfRangeException(nameof(caseId));
            if (SelectedCaseId == caseId) return;
            SelectedCaseId = caseId;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        internal IReadOnlyList<clsFixMember> GetDisplaySnapshot(string caseId) =>
            _fixMember.TryGetValue(caseId, out var rows)
                ? rows.Select(value => new clsFixMember { row = value.row, m = value.m,
                    tx = value.tx, ty = value.ty, tz = value.tz, tr = value.tr }).ToArray()
                : Array.Empty<clsFixMember>();

        private InputFixMemberService()
        {
            for (int sheet = 1; sheet <= TypeCount; sheet++)
            {
                string id = sheet.ToString(CultureInfo.InvariantCulture);
                var rows = new BindingList<clsFixMember> { AllowNew = false, AllowRemove = false, RaiseListChangedEvents = false };
                for (int index = 0; index < MaxNodeId; index++) rows.Add(new clsFixMember());
                rows.RaiseListChangedEvents = true;
                rows.ListChanged += (_, e) => RowsChanged(id, rows, e);
                _sheets.Add(id, rows);
            }
        }

        public BindingList<clsFixMember> GetRows(string sheetName) => _sheets[sheetName];

        public void clear() => ReplaceRows(new Dictionary<string, List<clsFixMember>>());

        public void setFixMemberJson(JsonElement jsonData)
        {
            var loaded = ParseFixMemberJson(jsonData);
            if (loaded != null) ApplyFixMember(loaded);
        }

        internal static Dictionary<string, List<clsFixMember>>? ParseFixMemberJson(JsonElement jsonData)
        {
            if (!jsonData.TryGetProperty("fix_member", out var source)) return null;
            ValidateSource(source);
            var loaded = DataHelperModule.JsonToDict(jsonData, "fix_member",
                static json => DataHelperModule.JsonToList<clsFixMember>(json));
            if (loaded == null) throw new JsonException("Invalid fix_member data.");
            foreach (var sheet in source.EnumerateObject())
                if (!loaded.TryGetValue(sheet.Name, out var rows) || rows.Count != sheet.Value.GetArrayLength())
                    throw new JsonException($"Invalid fix_member sheet: {sheet.Name}");
            ValidateRows(loaded);
            return loaded;
        }

        private static void ValidateSource(JsonElement source)
        {
            if (source.ValueKind != JsonValueKind.Object) throw new JsonException("fix_member must be an object.");
            foreach (var sheet in source.EnumerateObject())
            {
                if (sheet.Value.ValueKind != JsonValueKind.Array) throw new JsonException("fix_member sheet must be an array.");
                foreach (var row in sheet.Value.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Object) throw new JsonException("fix_member row must be an object.");
                    foreach (var field in row.EnumerateObject())
                        if (field.Name is "tx" or "ty" or "tz" or "tr" &&
                            field.Value.ValueKind != JsonValueKind.Null &&
                            (field.Value.ValueKind != JsonValueKind.Number ||
                             !field.Value.TryGetSingle(out float value) || !float.IsFinite(value)))
                            throw new JsonException($"Invalid fix_member {field.Name}.");
                }
            }
        }

        internal void ApplyFixMember(Dictionary<string, List<clsFixMember>>? prepared) =>
            ReplaceRows(prepared ?? new Dictionary<string, List<clsFixMember>>());

        public Dictionary<string, object> getFixMemberJson()
        {
            var result = new Dictionary<string, object>();
            foreach (var (sheet, rows) in _fixMember)
            {
                var data = new List<Dictionary<string, object?>>();
                foreach (var value in rows.OrderBy(value => value.row))
                    if (!value.IsEmpty) data.Add(DataHelperModule.ClassToDictionary(value));
                if (data.Count > 0) result.Add(sheet, data);
            }
            return result;
        }

        private static void ValidateRows(Dictionary<string, List<clsFixMember>> data)
        {
            foreach (var (sheet, values) in data)
            {
                if (!int.TryParse(sheet, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ||
                    number < 1 || number > TypeCount || sheet != number.ToString(CultureInfo.InvariantCulture))
                    throw new JsonException($"Invalid fix_member sheet: {sheet}");
                var seen = new HashSet<int>();
                foreach (var value in values)
                    if (value.row < 1 || value.row > MaxNodeId || !seen.Add(value.row))
                        throw new JsonException($"Invalid fix_member row: {value.row}");
            }
        }

        private void RowsChanged(string sheet, BindingList<clsFixMember> rows, ListChangedEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0) return;
            var value = rows[e.NewIndex];
            value.row = e.NewIndex + 1;
            if (!_fixMember.TryGetValue(sheet, out var active))
                _fixMember[sheet] = active = new List<clsFixMember>();
            active.RemoveAll(item => item.row == value.row);
            if (!value.IsEmpty) active.Add(value);
            if (active.Count == 0) _fixMember.Remove(sheet);
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private void ReplaceRows(Dictionary<string, List<clsFixMember>> next)
        {
            foreach (var (sheet, rows) in _sheets)
            {
                rows.RaiseListChangedEvents = false;
                try
                {
                    if (_fixMember.TryGetValue(sheet, out var old))
                        foreach (var item in old) rows[item.row - 1] = new clsFixMember();
                    if (next.TryGetValue(sheet, out var current))
                        foreach (var item in current) rows[item.row - 1] = item;
                }
                finally { rows.RaiseListChangedEvents = true; DocumentReplacementNotifications.Defer(() => rows.ResetBindings()); }
            }
            _fixMember = next;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }
    }
}

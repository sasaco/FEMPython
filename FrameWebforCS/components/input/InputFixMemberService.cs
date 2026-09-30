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
        public float? length = null;
        public float? tx = null;
        public float? ty = null;
        public float? tz = null;
        public float? tr = null;

        public event PropertyChangedEventHandler? PropertyChanged;
        public string? M { get => m; set { m = value; Changed(nameof(M)); } }
        public float? Length
        {
            get => length;
            set
            {
                if (value is float number && (!float.IsFinite(number) || number <= 0))
                    throw new ArgumentOutOfRangeException(nameof(value), "Spring interval length must be positive and finite.");
                length = value;
                Changed(nameof(Length));
            }
        }
        public float? Tx { get => tx; set { tx = value; Changed(nameof(Tx)); } }
        public float? Ty { get => ty; set { ty = value; Changed(nameof(Ty)); } }
        public float? Tz { get => tz; set { tz = value; Changed(nameof(Tz)); } }
        public float? Tr { get => tr; set { tr = value; Changed(nameof(Tr)); } }
        public bool IsEmpty => string.IsNullOrWhiteSpace(m) && length == null &&
            tx == null && ty == null && tz == null && tr == null;
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
        private readonly Dictionary<string, BindingList<clsFixMember>> _editorSheets = new();
        private readonly Dictionary<string, HashSet<int>> _anonymousRows = new();
        private bool _updatingRows;
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
                ? rows.OrderBy(value => value.row).Select(value => new clsFixMember { row = value.row, m = value.m,
                    length = value.length, tx = value.tx, ty = value.ty, tz = value.tz, tr = value.tr }).ToArray()
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
                var editor = new BindingList<clsFixMember> { AllowNew = false };
                editor.Add(new clsFixMember { row = 1 });
                editor.ListChanged += (_, e) => EditorRowsChanged(id, editor, e);
                _editorSheets.Add(id, editor);
                _anonymousRows.Add(id, new HashSet<int>());
            }
        }

        public BindingList<clsFixMember> GetRows(string sheetName) => _sheets[sheetName];
        internal BindingList<clsFixMember> GetEditorRows(string sheetName) => _editorSheets[sheetName];

        internal bool InsertEditorRow(string sheetName, int row, string? memberId)
        {
            if (row < 1 || row > MaxNodeId ||
                _fixMember.GetValueOrDefault(sheetName)?.Any(item => item.row == MaxNodeId) == true ||
                _anonymousRows[sheetName].Contains(MaxNodeId)) return false;
            var next = (_fixMember.GetValueOrDefault(sheetName) ?? new List<clsFixMember>())
                .Select(Clone).ToList();
            foreach (var item in next)
                if (item.row >= row) item.row++;
            var anonymous = _anonymousRows[sheetName].Select(id => id >= row ? id + 1 : id).ToHashSet();
            if (string.IsNullOrWhiteSpace(memberId)) anonymous.Add(row);
            else next.Add(new clsFixMember { row = row, m = memberId });
            ReplaceSheet(sheetName, next, anonymous);
            return true;
        }

        internal bool DeleteEditorRows(string sheetName, IEnumerable<int> rowNumbers)
        {
            int[] deleted = rowNumbers.Distinct().OrderBy(id => id).ToArray();
            if (deleted.Length == 0 || deleted[0] < 1 || deleted[^1] > MaxNodeId) return false;
            var selected = deleted.ToHashSet();
            var next = (_fixMember.GetValueOrDefault(sheetName) ?? new List<clsFixMember>())
                .Where(item => !selected.Contains(item.row)).Select(Clone).ToList();
            foreach (var item in next)
                item.row -= CountBefore(deleted, item.row);
            var anonymous = _anonymousRows[sheetName].Where(id => !selected.Contains(id))
                .Select(id => id - CountBefore(deleted, id)).ToHashSet();
            ReplaceSheet(sheetName, next, anonymous);
            return true;
        }

        private static int CountBefore(int[] sorted, int row)
        {
            int index = Array.BinarySearch(sorted, row);
            return index < 0 ? ~index : index;
        }

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
                        if (field.Name is "length" or "tx" or "ty" or "tz" or "tr" &&
                            field.Value.ValueKind != JsonValueKind.Null &&
                            (field.Value.ValueKind != JsonValueKind.Number ||
                             !field.Value.TryGetSingle(out float value) || !float.IsFinite(value)))
                            throw new JsonException($"Invalid fix_member {field.Name}.");
                    if (row.TryGetProperty("length", out var length) &&
                        length.ValueKind == JsonValueKind.Number && length.GetSingle() <= 0)
                        throw new JsonException("Invalid fix_member length.");
                }
            }
        }

        internal void ApplyFixMember(Dictionary<string, List<clsFixMember>>? prepared) =>
            ReplaceRows(prepared ?? new Dictionary<string, List<clsFixMember>>());

        internal static void ValidateAgainstGeometry(
            Dictionary<string, List<clsFixMember>>? sheets,
            IReadOnlyDictionary<string, clsMember> members,
            IReadOnlyDictionary<string, clsNode> nodes)
        {
            if (sheets == null) return;
            foreach (var (caseId, rows) in sheets)
            {
                foreach (var group in rows.GroupBy(row => row.m))
                {
                    string? memberId = group.Key;
                    if (string.IsNullOrWhiteSpace(memberId) ||
                        !members.TryGetValue(memberId, out var member) ||
                        !nodes.TryGetValue(member.ni ?? "", out var start) ||
                        !nodes.TryGetValue(member.nj ?? "", out var end))
                        throw new JsonException($"fix_member.{caseId}: invalid member {memberId}.");
                    double dx = (end.X ?? 0) - (start.X ?? 0);
                    double dy = (end.Y ?? 0) - (start.Y ?? 0);
                    double dz = (end.Z ?? 0) - (start.Z ?? 0);
                    double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    try { MemberSpringIntervals.Resolve(group, length); }
                    catch (ArgumentException error)
                    {
                        throw new JsonException($"fix_member.{caseId}, member {memberId}: {error.Message}", error);
                    }
                }
            }
        }

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
            if (_updatingRows || e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0) return;
            var value = rows[e.NewIndex];
            value.row = e.NewIndex + 1;
            if (!_fixMember.TryGetValue(sheet, out var active))
                _fixMember[sheet] = active = new List<clsFixMember>();
            active.RemoveAll(item => item.row == value.row);
            if (!value.IsEmpty) active.Add(Clone(value));
            if (active.Count == 0) _fixMember.Remove(sheet);
            RebuildEditor(sheet);
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private void EditorRowsChanged(string sheet, BindingList<clsFixMember> editor, ListChangedEventArgs e)
        {
            if (_updatingRows || e.ListChangedType != ListChangedType.ItemChanged || e.NewIndex < 0) return;
            var value = editor[e.NewIndex];
            if (value.row < 1 || value.row > MaxNodeId) return;
            if (!_fixMember.TryGetValue(sheet, out var active))
                _fixMember[sheet] = active = new List<clsFixMember>();
            active.RemoveAll(item => item.row == value.row);
            if (value.IsEmpty) _anonymousRows[sheet].Add(value.row);
            else
            {
                _anonymousRows[sheet].Remove(value.row);
                active.Add(Clone(value));
            }
            if (active.Count == 0) _fixMember.Remove(sheet);
            var canonical = _sheets[sheet];
            _updatingRows = true;
            try
            {
                canonical.RaiseListChangedEvents = false;
                canonical[value.row - 1] = Clone(value);
                canonical.RaiseListChangedEvents = true;
                canonical.ResetItem(value.row - 1);
                if (e.NewIndex == editor.Count - 1 && !value.IsEmpty && value.row < MaxNodeId)
                    editor.Add(new clsFixMember { row = value.row + 1 });
            }
            finally
            {
                canonical.RaiseListChangedEvents = true;
                _updatingRows = false;
            }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private static clsFixMember Clone(clsFixMember value) => new()
        {
            row = value.row, m = value.m, length = value.length,
            tx = value.tx, ty = value.ty, tz = value.tz, tr = value.tr
        };

        private void ReplaceSheet(string sheet, List<clsFixMember> next, HashSet<int> anonymous)
        {
            var old = _fixMember.GetValueOrDefault(sheet) ?? new List<clsFixMember>();
            var canonical = _sheets[sheet];
            var nextByRow = next.Where(item => !item.IsEmpty).ToDictionary(item => item.row);
            _updatingRows = true;
            try
            {
                canonical.RaiseListChangedEvents = false;
                foreach (int row in old.Select(item => item.row).Concat(nextByRow.Keys).Distinct())
                    canonical[row - 1] = nextByRow.TryGetValue(row, out var value)
                        ? Clone(value) : new clsFixMember();
                canonical.RaiseListChangedEvents = true;
                if (nextByRow.Count == 0) _fixMember.Remove(sheet);
                else _fixMember[sheet] = nextByRow.Values.OrderBy(item => item.row).ToList();
                _anonymousRows[sheet] = anonymous;
                RebuildEditor(sheet);
                canonical.ResetBindings();
            }
            finally
            {
                canonical.RaiseListChangedEvents = true;
                _updatingRows = false;
            }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private void RebuildEditor(string sheet)
        {
            var editor = _editorSheets[sheet];
            bool previous = _updatingRows;
            _updatingRows = true;
            editor.RaiseListChangedEvents = false;
            try
            {
                editor.Clear();
                var active = _fixMember.GetValueOrDefault(sheet) ?? new List<clsFixMember>();
                foreach (var row in active.Select(Clone).Concat(_anonymousRows[sheet]
                    .Select(id => new clsFixMember { row = id })).OrderBy(row => row.row))
                    editor.Add(row);
                int last = editor.Count == 0 ? 0 : editor[^1].row;
                if (last < MaxNodeId) editor.Add(new clsFixMember { row = last + 1 });
            }
            finally
            {
                editor.RaiseListChangedEvents = true;
                _updatingRows = previous;
            }
            DocumentReplacementNotifications.Defer(() => editor.ResetBindings());
        }

        private void ReplaceRows(Dictionary<string, List<clsFixMember>> next)
        {
            _updatingRows = true;
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
            foreach (string sheet in _sheets.Keys)
            {
                _anonymousRows[sheet].Clear();
                RebuildEditor(sheet);
            }
            _updatingRows = false;
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }
    }
}

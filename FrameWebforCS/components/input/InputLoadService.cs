using FrameWebforCS.providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace FrameWebforCS.components.input
{
    internal class clsLoadNode
    {
        public int row;
        public string? n = null;
        public float? tx = null;
        public float? ty = null;
        public float? tz = null;
        public float? rx = null;
        public float? ry = null;
        public float? rz = null;
    }

    internal class clsLoadMember
    {
        public int row;
        public string? m1 = null;
        public string? m2 = null;
        public string? direction = null;
        public string? mark = null;
        public string? L1 = null;
        public string? L2 = null;
        public float? P1 = null;
        public float? P2 = null;
    }

    internal class clsLoad
    {
        public int? fix_node = null;
        public int? fix_member = null;
        public int? element = null;
        public int? joint = null;
        public string? symbol = null;
        public float? LL_pitch = null;
        public string? name = null;
        public List<clsLoadNode>? load_node = null;
        public List<clsLoadMember>? load_member = null;
        public List<int>? input_rows = null;
    }

    // Display values are detached from the editable rows and save JSON. In particular,
    // JS InputLoadService.getNodeLoadJson(0) projects a negative node number as an
    // imposed displacement and divides its six components by 1000.
    internal sealed record LoadNodeDisplay(int Row, int NodeId, bool IsDisplacement,
        float Tx, float Ty, float Tz, float Rx, float Ry, float Rz);
    internal sealed record LoadMemberDisplay(int Row, int MemberStart, int MemberEnd,
        string Direction, int Mark, float L1, float L2, float P1, float P2);
    internal sealed record LoadCaseDisplay(string Symbol,
        IReadOnlyList<LoadNodeDisplay> NodeLoads, IReadOnlyList<LoadMemberDisplay> MemberLoads,
        float? LLPitch = null);

    internal sealed class clsLoadNameRow : INotifyPropertyChanged
    {
        public clsLoad Value { get; set; } = new();
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool IsEmpty => Value.fix_node == null && Value.fix_member == null &&
            Value.element == null && Value.joint == null && string.IsNullOrWhiteSpace(Value.symbol) &&
            Value.LL_pitch == null && string.IsNullOrWhiteSpace(Value.name);
        private void Changed(string property) =>
            DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(property));
        public int? fix_node { get => Value.fix_node; set { Value.fix_node = value; Changed(nameof(fix_node)); } }
        public int? fix_member { get => Value.fix_member; set { Value.fix_member = value; Changed(nameof(fix_member)); } }
        public int? element { get => Value.element; set { Value.element = value; Changed(nameof(element)); } }
        public int? joint { get => Value.joint; set { Value.joint = value; Changed(nameof(joint)); } }
        public string? symbol { get => Value.symbol; set { Value.symbol = value; Changed(nameof(symbol)); } }
        public float? LL_pitch { get => Value.LL_pitch; set { Value.LL_pitch = value; Changed(nameof(LL_pitch)); } }
        public string? name { get => Value.name; set { Value.name = value; Changed(nameof(name)); } }
    }

    internal sealed class clsLoadIntensityRow : INotifyPropertyChanged
    {
        private int _row;
        public int Row
        {
            get => _row;
            set
            {
                _row = value;
                if (Member != null) Member.row = value;
                if (Node != null) Node.row = value;
            }
        }
        public string CaseId { get; set; } = "";
        public bool IsAssigned => CaseId.Length > 0;
        public clsLoadMember? Member { get; set; }
        public clsLoadNode? Node { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public string LoadId
        {
            get => CaseId;
            set
            {
                if (value == CaseId) return;
                if (!InputLoadService.Instance.MoveIntensityRow(this, value))
                    Changed(nameof(LoadId)); // Restore the bound cell from its unchanged row identity.
            }
        }
        public bool HasMember => Member != null &&
            (!string.IsNullOrWhiteSpace(Member.m1) || !string.IsNullOrWhiteSpace(Member.m2) ||
             !string.IsNullOrWhiteSpace(Member.direction) || !string.IsNullOrWhiteSpace(Member.mark) ||
             !string.IsNullOrWhiteSpace(Member.L1) || !string.IsNullOrWhiteSpace(Member.L2) ||
             Member.P1 != null || Member.P2 != null);
        public bool HasNode => Node != null &&
            (!string.IsNullOrWhiteSpace(Node.n) || Node.tx != null || Node.ty != null ||
             Node.tz != null || Node.rx != null || Node.ry != null || Node.rz != null);
        private void SetMember<T>(T value, Action<clsLoadMember, T> set, string property)
        {
            if (!IsAssigned && (value == null || value is string text && string.IsNullOrWhiteSpace(text))) return;
            if (!InputLoadService.Instance.EnsureIntensityAssignment(this)) return;
            set(Member ??= new clsLoadMember { row = Row }, value);
            Changed(property);
        }
        private void SetNode<T>(T value, Action<clsLoadNode, T> set, string property)
        {
            if (!IsAssigned && (value == null || value is string text && string.IsNullOrWhiteSpace(text))) return;
            if (!InputLoadService.Instance.EnsureIntensityAssignment(this)) return;
            set(Node ??= new clsLoadNode { row = Row }, value);
            Changed(property);
        }
        private void Changed(string property) =>
            DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(property));
        public string? m1 { get => Member?.m1; set { SetMember(value, (row, item) => row.m1 = item, nameof(m1)); } }
        public string? m2 { get => Member?.m2; set { SetMember(value, (row, item) => row.m2 = item, nameof(m2)); } }
        public string? direction { get => Member?.direction; set { SetMember(value, (row, item) => row.direction = item, nameof(direction)); } }
        public string? mark { get => Member?.mark; set { SetMember(value, (row, item) => row.mark = item, nameof(mark)); } }
        public string? L1 { get => Member?.L1; set { SetMember(value, (row, item) => row.L1 = item, nameof(L1)); } }
        public string? L2 { get => Member?.L2; set { SetMember(value, (row, item) => row.L2 = item, nameof(L2)); } }
        public float? P1 { get => Member?.P1; set { SetMember(value, (row, item) => row.P1 = item, nameof(P1)); } }
        public float? P2 { get => Member?.P2; set { SetMember(value, (row, item) => row.P2 = item, nameof(P2)); } }
        public string? n { get => Node?.n; set { SetNode(value, (row, item) => row.n = item, nameof(n)); } }
        public float? tx { get => Node?.tx; set { SetNode(value, (row, item) => row.tx = item, nameof(tx)); } }
        public float? ty { get => Node?.ty; set { SetNode(value, (row, item) => row.ty = item, nameof(ty)); } }
        public float? tz { get => Node?.tz; set { SetNode(value, (row, item) => row.tz = item, nameof(tz)); } }
        public float? rx { get => Node?.rx; set { SetNode(value, (row, item) => row.rx = item, nameof(rx)); } }
        public float? ry { get => Node?.ry; set { SetNode(value, (row, item) => row.ry = item, nameof(ry)); } }
        public float? rz { get => Node?.rz; set { SetNode(value, (row, item) => row.rz = item, nameof(rz)); } }
    }

    internal class InputLoadService
    {
        internal const int MaxNodeId = 100_000;
        // Lazy<T> を使ってスレッドセーフかつ遅延評価のシングルトンを実装
        private static readonly Lazy<InputLoadService> _instance =
            new Lazy<InputLoadService>(() => new InputLoadService());

        // 外部からはこのプロパティを通じてのみインスタンスにアクセスできる
        public static InputLoadService Instance => _instance.Value;

        private Dictionary<string, clsLoad> _load;
        private readonly SortedSet<int> _assignedIndices = new();
        private readonly Dictionary<(string CaseId, int Row), int> _rowIndices = new();
        private readonly Dictionary<clsLoadIntensityRow, int> _displayIndices = new();
        public IEnumerable<int> AssignedIntensityRowIndices => _assignedIndices;
        private string _selectedCaseId = "1";
        public BindingList<clsLoadNameRow> LoadNames { get; } = new();
        public BindingList<clsLoadIntensityRow> IntensityRows { get; } = new();
        public event EventHandler? CasesChanged;
        internal event Action? LoadsEdited;
        internal event Action<string>? SelectedCaseChanged;
        internal event Action<string, int>? IntensityRowMoved;
        public string SelectedCaseId => _selectedCaseId;
        public IEnumerable<string> CaseIds => _load.Keys;
        internal int MaximumEffectiveCaseId
        {
            get
            {
                int maximum = 0;
                foreach (var (id, load) in _load)
                {
                    if (IsEffectiveCase(load) &&
                        int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) &&
                        number > maximum)
                        maximum = number;
                }
                return maximum;
            }
        }

        // コンストラクタを private にして、外部からの new を禁止する
        private InputLoadService()
        {
            this._load = new Dictionary<string, clsLoad>();
            LoadNames.AllowNew = false;
            LoadNames.AllowRemove = false;
            LoadNames.RaiseListChangedEvents = false;
            IntensityRows.AllowNew = false;
            IntensityRows.AllowRemove = false;
            IntensityRows.RaiseListChangedEvents = false;
            for (int index = 0; index < MaxNodeId; index++)
            {
                LoadNames.Add(new clsLoadNameRow());
                var row = new clsLoadIntensityRow();
                IntensityRows.Add(row);
                _displayIndices.Add(row, index);
            }
            LoadNames.RaiseListChangedEvents = true;
            IntensityRows.RaiseListChangedEvents = true;
            LoadNames.ListChanged += LoadNames_ListChanged;
            IntensityRows.ListChanged += IntensityRows_ListChanged;
        }

        public void clear()
        {
            ReplaceLoad(new Dictionary<string, clsLoad>());
        }

        /// <summary>
        /// ファイルを読み込むとき
        /// </summary>
        /// <param name="jsonData"></param>
        public void setLoadJson(JsonElement jsonData)
        {
            ApplyLoads(ParseLoadData(jsonData));
        }

        // Parse before mutating live rows so InputDataService can stage the whole file.
        internal static Dictionary<string, clsLoad> ParseLoadJson(JsonElement jsonData)
        {
            if (jsonData.ValueKind != JsonValueKind.Object)
                throw new JsonException("Input root must be an object.");
            // JS loadInputData clears the previous load before setLoadJson. An omitted
            // `load` section therefore means an empty committed load set on file open.
            if (!jsonData.TryGetProperty("load", out JsonElement loadJson))
                return new Dictionary<string, clsLoad>();
            if (loadJson.ValueKind != JsonValueKind.Object)
                throw new JsonException("load must be an object.");
            var caseIds = new HashSet<string>();
            foreach (JsonProperty caseJson in loadJson.EnumerateObject())
            {
                if (!caseIds.Add(caseJson.Name))
                    throw new JsonException($"Duplicate load case ID: {caseJson.Name}");
                if (caseJson.Value.ValueKind != JsonValueKind.Object)
                    throw new JsonException($"Invalid load case: {caseJson.Name}");
                foreach (string rowName in new[] { "load_node", "load_member", "input_rows" })
                    if (caseJson.Value.TryGetProperty(rowName, out var rows) &&
                        rows.ValueKind is not (JsonValueKind.Array or JsonValueKind.Null))
                        throw new JsonException($"Invalid {rowName} in load case {caseJson.Name}");
                foreach (string rowName in new[] { "load_node", "load_member" })
                {
                    if (!caseJson.Value.TryGetProperty(rowName, out var rows) ||
                        rows.ValueKind == JsonValueKind.Null) continue;
                    foreach (JsonElement row in rows.EnumerateArray())
                        if (row.ValueKind != JsonValueKind.Object ||
                            !row.TryGetProperty("row", out JsonElement number) ||
                            number.ValueKind != JsonValueKind.Number ||
                            !number.TryGetInt32(out _))
                            throw new JsonException($"Invalid {rowName} row in case {caseJson.Name}");
                }
            }
            var load = DataHelperModule.JsonToDict(jsonData, "load", ReadLoad);
            if (load == null) throw new JsonException("Invalid load section.");
            var normalized = new Dictionary<string, clsLoad>();
            foreach (var (id, item) in load)
            {
                if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) ||
                    number < 1 || number > MaxNodeId)
                    throw new JsonException($"Invalid load case ID: {id}");
                ValidateRows(item.load_node, id);
                ValidateRows(item.load_member, id);
                if (item.input_rows == null)
                    item.input_rows = (item.load_node?.Select(row => row.row) ?? Enumerable.Empty<int>())
                        .Concat(item.load_member?.Select(row => row.row) ?? Enumerable.Empty<int>())
                        .Distinct().OrderBy(row => row).ToList();
                else
                {
                    if (item.input_rows.Any(row => row < 1 || row > MaxNodeId) ||
                        item.input_rows.Distinct().Count() != item.input_rows.Count ||
                        !item.input_rows.SequenceEqual(item.input_rows.OrderBy(row => row)))
                        throw new JsonException($"Invalid input_rows in load case {id}");
                    var rowSet = item.input_rows.ToHashSet();
                    if ((item.load_node?.Any(row => !rowSet.Contains(row.row)) ?? false) ||
                        (item.load_member?.Any(row => !rowSet.Contains(row.row)) ?? false))
                        throw new JsonException($"Load row missing from input_rows in case {id}");
                }
                if (!normalized.TryAdd(number.ToString(CultureInfo.InvariantCulture), item))
                    throw new JsonException($"Duplicate load case ID: {id}");
            }
            if (normalized.Values.Sum(item => (long)(item.input_rows?.Count ?? 0)) > MaxNodeId)
                throw new JsonException("Load intensity rows exceed display capacity.");
            return normalized;
        }

        internal sealed record PreparedLoadData(Dictionary<string, clsLoad> Loads,
            Dictionary<int, (string CaseId, int Row)> Layout);

        internal static PreparedLoadData ParseLoadData(JsonElement root)
        {
            var loads = ParseLoadJson(root);
            var layout = new Dictionary<int, (string CaseId, int Row)>();
            if (!root.TryGetProperty("load_intensity_layout", out var metadata))
            {
                int index = 0;
                foreach (var (id, load) in loads)
                    foreach (int row in load.input_rows ?? new List<int>())
                        layout.Add(index++, (id, row));
                return new PreparedLoadData(loads, layout);
            }
            if (metadata.ValueKind != JsonValueKind.Object ||
                !metadata.TryGetProperty("version", out var version) ||
                version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int value) || value != 1 ||
                !metadata.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
                throw new JsonException("Invalid load_intensity_layout format or version.");
            var expected = loads.SelectMany(item => (item.Value.input_rows ?? new List<int>())
                .Select(row => (CaseId: item.Key, Row: row))).ToHashSet();
            var seen = new HashSet<(string CaseId, int Row)>();
            foreach (var item in rows.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("slot", out var slotJson) || slotJson.ValueKind != JsonValueKind.Number ||
                    !slotJson.TryGetInt32(out int slot) || slot < 1 || slot > MaxNodeId ||
                    !item.TryGetProperty("case_id", out var caseJson) || caseJson.ValueKind != JsonValueKind.String ||
                    !item.TryGetProperty("row", out var rowJson) || rowJson.ValueKind != JsonValueKind.Number ||
                    !rowJson.TryGetInt32(out int row))
                    throw new JsonException("Invalid load intensity layout row.");
                string id = caseJson.GetString()!;
                if (!ValidCaseId(id) || !expected.Contains((id, row)) || !seen.Add((id, row)) ||
                    !layout.TryAdd(slot - 1, (id, row)))
                    throw new JsonException("Duplicate or unknown load intensity layout reference.");
            }
            if (!expected.SetEquals(seen))
                throw new JsonException("Load intensity layout omits assigned rows.");
            return new PreparedLoadData(loads, layout);
        }

        internal void ApplyLoads(PreparedLoadData prepared) => ReplaceLoad(prepared.Loads, prepared.Layout);
        internal void ApplyLoads(Dictionary<string, clsLoad> prepared) => ReplaceLoad(prepared);

        internal Dictionary<string, object> GetIntensityLayoutJson() => new()
        {
            ["version"] = 1,
            ["rows"] = _assignedIndices.Select(index => new Dictionary<string, object>
            {
                ["slot"] = index + 1, ["case_id"] = IntensityRows[index].CaseId,
                ["row"] = IntensityRows[index].Row
            }).ToArray()
        };

        /// <summary>
        /// ファイルに保存するとき
        /// </summary>
        public Dictionary<string, object> getLoadJson()
        {
            var load = new Dictionary<string, object>();
            foreach (KeyValuePair<string, clsLoad> item in this._load)
            {
                if (HasData(item.Value))
                    load.Add(item.Key, WriteLoad(item.Value));
            }
            return load;
        }

        internal IReadOnlyDictionary<string, LoadCaseDisplay> GetDisplaySnapshot()
        {
            var result = new Dictionary<string, LoadCaseDisplay>();
            foreach (var (id, item) in _load)
            {
                var nodeLoads = new List<LoadNodeDisplay>();
                foreach (var row in item.load_node ?? new List<clsLoadNode>())
                {
                    if (!int.TryParse(row.n, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out int signedId) || signedId is 0 or int.MinValue) continue;
                    if (row.tx == null && row.ty == null && row.tz == null &&
                        row.rx == null && row.ry == null && row.rz == null) continue;
                    bool displacement = signedId < 0;
                    float divisor = displacement ? 1000f : 1f;
                    nodeLoads.Add(new LoadNodeDisplay(row.row, Math.Abs(signedId), displacement,
                        (row.tx ?? 0) / divisor, (row.ty ?? 0) / divisor,
                        (row.tz ?? 0) / divisor, (row.rx ?? 0) / divisor,
                        (row.ry ?? 0) / divisor, (row.rz ?? 0) / divisor));
                }
                var memberLoads = new List<LoadMemberDisplay>();
                foreach (var row in item.load_member ?? new List<clsLoadMember>())
                {
                    if (!int.TryParse(row.m1, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out int start)) start = 0;
                    if (!int.TryParse(row.m2, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out int end)) end = 0;
                    if (start == 0) start = end;
                    if (end == 0) end = start;
                    if (start == 0 || !int.TryParse(row.mark, NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int mark)) continue;
                    if (!float.TryParse(row.L1, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out float l1)) l1 = 0;
                    if (!float.TryParse(row.L2, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out float l2)) l2 = 0;
                    memberLoads.Add(new LoadMemberDisplay(row.row, start, end,
                        row.direction?.Trim().ToLowerInvariant() ?? "", mark, l1, l2,
                        row.P1 ?? 0, row.P2 ?? 0));
                }
                result.Add(id, new LoadCaseDisplay(item.symbol ?? "", nodeLoads, memberLoads,
                    item.LL_pitch));
            }
            return result;
        }

        public void SelectCase(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Load case ID is required.", nameof(id));
            if (_selectedCaseId == id) return;
            _selectedCaseId = id;
            DocumentReplacementNotifications.Publish(SelectedCaseChanged, id);
        }

        private static bool ValidCaseId(string? id) =>
            int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) &&
            number is >= 1 and <= MaxNodeId &&
            id == number.ToString(CultureInfo.InvariantCulture);

        private clsLoad EnsureCase(string id)
        {
            if (_load.TryGetValue(id, out clsLoad? existing)) return existing;
            var created = new clsLoad();
            _load.Add(id, created);
            LoadNames[int.Parse(id, CultureInfo.InvariantCulture) - 1].Value = created;
            return created;
        }

        private void FinishRowOperation(bool normalizeSelection = false, string? selectCase = null)
        {
            string previousCaseId = _selectedCaseId;
            if (selectCase != null)
                _selectedCaseId = selectCase;
            else if (normalizeSelection && !_load.ContainsKey(_selectedCaseId))
                _selectedCaseId = _load.Keys.FirstOrDefault() ?? "1";
            DocumentReplacementNotifications.Publish(CasesChanged, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(LoadsEdited);
            if (previousCaseId != _selectedCaseId)
                DocumentReplacementNotifications.Publish(SelectedCaseChanged, _selectedCaseId);
        }

        public int FindIntensityRowIndex(string caseId, int rowNumber) =>
            _rowIndices.TryGetValue((caseId, rowNumber), out int index) ? index : -1;

        public clsLoadIntensityRow? GetIntensityRowAt(int displayIndex) =>
            displayIndex >= 0 && displayIndex < MaxNodeId ? IntensityRows[displayIndex] : null;

        internal bool EnsureIntensityAssignment(clsLoadIntensityRow row)
        {
            if (!_displayIndices.TryGetValue(row, out int index)) return false;
            if (row.IsAssigned) return true;
            return AssignIntensityRow(row, index, _selectedCaseId);
        }

        private bool AssignIntensityRow(clsLoadIntensityRow row, int index, string caseId)
        {
            if (!ValidCaseId(caseId)) return false;
            _load.TryGetValue(caseId, out var load);
            var rows = load?.input_rows;
            int rowNumber = rows?.Count > 0 ? rows[^1] + 1 : 1;
            if (rowNumber > MaxNodeId)
            {
                int available = 1;
                foreach (int used in rows!)
                {
                    if (used != available) break;
                    available++;
                }
                if (available > MaxNodeId) return false;
                rowNumber = available;
            }
            load ??= EnsureCase(caseId);
            (load.input_rows ??= new List<int>()).Add(rowNumber);
            load.input_rows.Sort();
            row.CaseId = caseId;
            row.Row = rowNumber;
            _assignedIndices.Add(index);
            _rowIndices.Add((caseId, rowNumber), index);
            return true;
        }

        public bool InsertIntensityRowAt(int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= MaxNodeId || IntensityRows[MaxNodeId - 1].IsAssigned)
                return false;
            // Only occupied slots need copying: anonymous slots are indistinguishable.
            var assignments = _assignedIndices.Select(index => (Index: index, Value: IntensityRows[index]))
                .Select(item => (Index: item.Index >= displayIndex ? item.Index + 1 : item.Index, item.Value)).ToArray();
            SetIntensityAssignments(assignments);
            FinishRowOperation();
            return true;
        }

        public bool InsertIntensityRow(string caseId, int rowNumber) =>
            InsertIntensityRowAt(FindIntensityRowIndex(caseId, rowNumber));

        public bool DeleteIntensityRows(IEnumerable<(string CaseId, int Row)> rows) =>
            rows != null && DeleteIntensityRowsAt(rows.Select(item => FindIntensityRowIndex(item.CaseId, item.Row)));

        public bool DeleteIntensityRowsAt(IEnumerable<int> displayIndices)
        {
            if (displayIndices == null) return false;
            int[] deletedIndices = displayIndices.Distinct().OrderBy(index => index).ToArray();
            if (deletedIndices.Length == 0 || deletedIndices[0] < 0 || deletedIndices[^1] >= MaxNodeId)
                return false;
            var deletedSet = deletedIndices.ToHashSet();
            var assignments = _assignedIndices.Select(index => (Index: index, Value: IntensityRows[index])).ToArray();
            foreach (var group in assignments.Where(item => deletedSet.Contains(item.Index))
                .GroupBy(item => item.Value.CaseId))
            {
                clsLoad load = _load[group.Key];
                int[] deleted = group.Select(item => item.Value.Row).OrderBy(row => row).ToArray();
                var rowSet = deleted.ToHashSet();
                load.input_rows = load.input_rows!.Where(row => !rowSet.Contains(row))
                    .Select(row => row - CountBefore(deleted, row)).ToList();
                load.load_node?.RemoveAll(node => rowSet.Contains(node.row));
                load.load_member?.RemoveAll(member => rowSet.Contains(member.row));
                if (load.load_node != null)
                    foreach (var node in load.load_node) node.row -= CountBefore(deleted, node.row);
                if (load.load_member != null)
                    foreach (var member in load.load_member) member.row -= CountBefore(deleted, member.row);
                foreach (var survivor in assignments.Where(item => item.Value.CaseId == group.Key &&
                    !deletedSet.Contains(item.Index)))
                    survivor.Value.Row -= CountBefore(deleted, survivor.Value.Row);
                RemoveEmptyCase(group.Key, load);
            }
            SetIntensityAssignments(assignments.Where(item => !deletedSet.Contains(item.Index))
                .Select(item => (Index: item.Index - CountBefore(deletedIndices, item.Index), item.Value)));
            FinishRowOperation(normalizeSelection: true);
            return true;
        }

        private static int CountBefore(int[] sorted, int row)
        {
            int index = Array.BinarySearch(sorted, row);
            return index < 0 ? ~index : index;
        }

        public bool MoveIntensityRow(clsLoadIntensityRow row, string destinationCaseId)
        {
            if (row == null || !ValidCaseId(destinationCaseId) ||
                !_displayIndices.TryGetValue(row, out int displayIndex)) return false;
            if (!row.IsAssigned)
            {
                if (!AssignIntensityRow(row, displayIndex, destinationCaseId)) return false;
                DocumentReplacementNotifications.Defer(() => IntensityRows.ResetItem(displayIndex));
                FinishRowOperation(selectCase: destinationCaseId);
                DocumentReplacementNotifications.Defer(() => IntensityRowMoved?.Invoke(destinationCaseId, row.Row));
                return true;
            }
            if (row.CaseId == destinationCaseId) return true;
            _load.TryGetValue(destinationCaseId, out clsLoad? destination);
            if (destination?.input_rows?.Count > 0 && destination.input_rows[^1] == MaxNodeId)
                return false;
            string sourceCaseId = row.CaseId;
            int rowNumber = row.Row;
            clsLoad source = _load[sourceCaseId];
            source.input_rows!.Remove(rowNumber);
            if (row.Node != null) source.load_node?.Remove(row.Node);
            if (row.Member != null) source.load_member?.Remove(row.Member);
            ShiftRowsAfterDeletion(source, rowNumber);
            RemoveEmptyCase(sourceCaseId, source);
            destination ??= EnsureCase(destinationCaseId);
            destination.input_rows ??= new List<int>();
            ShiftRows(destination, rowNumber, +1);
            destination.input_rows.Add(rowNumber);
            destination.input_rows.Sort();
            foreach (int index in _assignedIndices)
            {
                var affected = IntensityRows[index];
                if (ReferenceEquals(affected, row)) continue;
                if (affected.CaseId == sourceCaseId && affected.Row > rowNumber) affected.Row--;
                else if (affected.CaseId == destinationCaseId && affected.Row >= rowNumber) affected.Row++;
            }
            row.CaseId = destinationCaseId;
            if (row.Node != null)
            {
                row.Node.row = rowNumber;
                if (row.HasNode) (destination.load_node ??= new List<clsLoadNode>()).Add(row.Node);
            }
            if (row.Member != null)
            {
                row.Member.row = rowNumber;
                if (row.HasMember) (destination.load_member ??= new List<clsLoadMember>()).Add(row.Member);
            }
            ReindexAssignedRows();
            DocumentReplacementNotifications.Defer(() => IntensityRows.ResetBindings());
            FinishRowOperation(selectCase: destinationCaseId);
            DocumentReplacementNotifications.Defer(() => IntensityRowMoved?.Invoke(destinationCaseId, rowNumber));
            return true;
        }

        private void ReindexAssignedRows()
        {
            _rowIndices.Clear();
            foreach (int index in _assignedIndices)
                _rowIndices.Add((IntensityRows[index].CaseId, IntensityRows[index].Row), index);
        }

        private void SetIntensityAssignments(IEnumerable<(int Index, clsLoadIntensityRow Value)> assignments)
        {
            var next = assignments.ToDictionary(item => item.Index, item => item.Value);
            IntensityRows.RaiseListChangedEvents = false;
            try
            {
                int[] changed = _assignedIndices.Union(next.Keys)
                    .Where(index => !next.TryGetValue(index, out var row) ||
                        !ReferenceEquals(IntensityRows[index], row)).ToArray();
                foreach (int index in changed)
                {
                    _displayIndices.Remove(IntensityRows[index]);
                    IntensityRows[index] = next.TryGetValue(index, out var row) ? row : new clsLoadIntensityRow();
                }
                foreach (int index in changed)
                    _displayIndices[IntensityRows[index]] = index;
                _assignedIndices.Clear();
                _assignedIndices.UnionWith(next.Keys);
                ReindexAssignedRows();
            }
            finally
            {
                IntensityRows.RaiseListChangedEvents = true;
            }
            DocumentReplacementNotifications.Defer(() => IntensityRows.ResetBindings());
        }

        private static void ShiftRows(clsLoad load, int from, int delta)
        {
            if (load.input_rows != null)
                for (int i = 0; i < load.input_rows.Count; i++)
                    if (load.input_rows[i] >= from) load.input_rows[i] += delta;
            if (load.load_node != null)
                foreach (var node in load.load_node)
                    if (node.row >= from) node.row += delta;
            if (load.load_member != null)
                foreach (var member in load.load_member)
                    if (member.row >= from) member.row += delta;
        }

        private static void ShiftRowsAfterDeletion(clsLoad load, int deletedRow)
        {
            if (load.input_rows != null)
                for (int i = 0; i < load.input_rows.Count; i++)
                    if (load.input_rows[i] > deletedRow) load.input_rows[i]--;
            if (load.load_node != null)
                foreach (var node in load.load_node)
                    if (node.row > deletedRow) node.row--;
            if (load.load_member != null)
                foreach (var member in load.load_member)
                    if (member.row > deletedRow) member.row--;
        }

        private void RemoveEmptyCase(string id, clsLoad load)
        {
            if (HasData(load)) return;
            _load.Remove(id);
            LoadNames[int.Parse(id, CultureInfo.InvariantCulture) - 1].Value = new clsLoad();
        }

        private static void ValidateRows<T>(List<T>? rows, string id) where T : class
        {
            if (rows == null) return;
            var seen = new HashSet<int>();
            foreach (T item in rows)
            {
                int row = item is clsLoadNode node ? node.row : ((clsLoadMember)(object)item).row;
                if (row < 1 || row > MaxNodeId || !seen.Add(row))
                    throw new JsonException($"Invalid or duplicate load row in case {id}: {row}");
            }
        }

        private static bool HasData(clsLoad item)
        {
            if (item.input_rows?.Count > 0) return true;
            if (item.fix_node != null || item.fix_member != null || item.element != null ||
                item.joint != null || !string.IsNullOrWhiteSpace(item.symbol) ||
                item.LL_pitch != null || !string.IsNullOrWhiteSpace(item.name))
                return true;
            foreach (clsLoadNode node in item.load_node ?? new List<clsLoadNode>())
                if (!string.IsNullOrWhiteSpace(node.n) || node.tx != null || node.ty != null ||
                    node.tz != null || node.rx != null || node.ry != null || node.rz != null)
                    return true;
            foreach (clsLoadMember member in item.load_member ?? new List<clsLoadMember>())
                if (!string.IsNullOrWhiteSpace(member.m1) || !string.IsNullOrWhiteSpace(member.m2) ||
                    !string.IsNullOrWhiteSpace(member.direction) || !string.IsNullOrWhiteSpace(member.mark) ||
                    !string.IsNullOrWhiteSpace(member.L1) || !string.IsNullOrWhiteSpace(member.L2) ||
                    member.P1 != null || member.P2 != null)
                    return true;
            return false;
        }

        // COMBINE の列数は旧版の有効ケース判定に合わせ、保存対象の判定とは分ける。
        private static bool IsEffectiveCase(clsLoad item)
        {
            if (item.fix_node != null || item.fix_member != null || item.element != null ||
                item.joint != null || !string.IsNullOrWhiteSpace(item.symbol) ||
                !string.IsNullOrWhiteSpace(item.name))
                return true;
            if (item.load_node != null)
                foreach (clsLoadNode node in item.load_node)
                    if (IsNumeric(node.n) && (node.tx != null || node.ty != null ||
                        node.tz != null || node.rx != null || node.ry != null || node.rz != null))
                        return true;
            if (item.load_member != null)
                foreach (clsLoadMember member in item.load_member)
                    if (IsNumeric(member.m1) || IsNumeric(member.m2) ||
                        !string.IsNullOrWhiteSpace(member.direction) || IsNumeric(member.mark) ||
                        IsNumeric(member.L1) || IsNumeric(member.L2) ||
                        member.P1 != null || member.P2 != null)
                        return true;
            return false;
        }

        private static bool IsNumeric(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                out double number) && double.IsFinite(number);

        private void LoadNames_ListChanged(object? sender, ListChangedEventArgs change)
        {
            if (change.ListChangedType != ListChangedType.ItemChanged || change.NewIndex < 0)
                return;
            string id = (change.NewIndex + 1).ToString(CultureInfo.InvariantCulture);
            clsLoadNameRow row = LoadNames[change.NewIndex];
            if (row.IsEmpty && row.Value.load_node?.Count is not > 0 &&
                row.Value.load_member?.Count is not > 0 &&
                row.Value.input_rows?.Count is not > 0)
                _load.Remove(id);
            else
                _load[id] = row.Value;
            DocumentReplacementNotifications.Publish(CasesChanged, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(LoadsEdited);
        }

        private void IntensityRows_ListChanged(object? sender, ListChangedEventArgs change)
        {
            if (change.ListChangedType != ListChangedType.ItemChanged || change.NewIndex < 0)
                return;
            if (change.PropertyDescriptor == null ||
                change.PropertyDescriptor.Name == nameof(clsLoadIntensityRow.LoadId))
                return;
            clsLoadIntensityRow row = IntensityRows[change.NewIndex];
            if (!_load.TryGetValue(row.CaseId, out clsLoad? load))
                load = EnsureCase(row.CaseId);
            UpdateNestedRow(load.load_member ??= new List<clsLoadMember>(), row.Row,
                row.HasMember ? row.Member : null, member => member.row);
            UpdateNestedRow(load.load_node ??= new List<clsLoadNode>(), row.Row,
                row.HasNode ? row.Node : null, node => node.row);
            DocumentReplacementNotifications.Publish(CasesChanged, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(LoadsEdited);
        }

        private static void UpdateNestedRow<T>(List<T> list, int row, T? value,
            Func<T, int> getRow) where T : class
        {
            int index = list.FindIndex(item => getRow(item) == row);
            if (value == null)
            {
                if (index >= 0) list.RemoveAt(index);
            }
            else if (index < 0)
                list.Add(value);
            else
                list[index] = value;
        }

        private void ReplaceLoad(Dictionary<string, clsLoad> next, Dictionary<int, (string CaseId, int Row)>? layout = null)
        {
            string previousCaseId = _selectedCaseId;
            LoadNames.RaiseListChangedEvents = false;
            try
            {
                foreach (string id in _load.Keys)
                    if (int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) &&
                        number >= 1 && number <= MaxNodeId &&
                        id == number.ToString(CultureInfo.InvariantCulture))
                        LoadNames[number - 1] = new clsLoadNameRow();
                _load = next;
                // JS fileload clears load state before changeCase(1). Keep the C#
                // selected case inside the replacement (or editable case 1 when empty)
                // so the coordinator does not restore a stale, now missing case.
                if (!_load.ContainsKey(_selectedCaseId))
                    _selectedCaseId = _load.Keys.FirstOrDefault() ?? "1";
                foreach (var (id, item) in _load)
                    if (int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int number) &&
                        number >= 1 && number <= MaxNodeId &&
                        id == number.ToString(CultureInfo.InvariantCulture))
                        LoadNames[number - 1] = new clsLoadNameRow { Value = item };
            }
            finally
            {
                LoadNames.RaiseListChangedEvents = true;
            }
            ReplaceIntensityRows(layout);
            DocumentReplacementNotifications.Defer(() => LoadNames.ResetBindings());
            DocumentReplacementNotifications.Publish(CasesChanged, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(LoadsEdited);
            if (previousCaseId != _selectedCaseId)
                DocumentReplacementNotifications.Publish(SelectedCaseChanged, _selectedCaseId);
        }

        private void ReplaceIntensityRows(Dictionary<int, (string CaseId, int Row)>? layout = null)
        {
            var identities = new Dictionary<(string CaseId, int Row), clsLoadIntensityRow>();
            foreach (var (id, load) in _load)
            {
                var members = load.load_member?.ToDictionary(row => row.row) ?? new();
                var nodes = load.load_node?.ToDictionary(row => row.row) ?? new();
                foreach (int number in load.input_rows ?? new List<int>())
                {
                    members.TryGetValue(number, out var member);
                    nodes.TryGetValue(number, out var node);
                    identities.Add((id, number), new clsLoadIntensityRow
                    {
                        CaseId = id, Row = number, Member = member, Node = node
                    });
                }
            }
            if (layout == null)
                SetIntensityAssignments(identities.Values.Select((row, index) => (index, row)));
            else
                SetIntensityAssignments(layout.Select(item => (item.Key, identities[item.Value])));
        }

        private static clsLoad? ReadLoad(JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Object) return null;

            clsLoad load = DataHelperModule.JsonToClass<clsLoad>(json) ?? new clsLoad();
            if (json.TryGetProperty(nameof(clsLoad.load_node), out JsonElement loadNode))
                load.load_node = DataHelperModule.JsonToList<clsLoadNode>(loadNode);
            if (json.TryGetProperty(nameof(clsLoad.load_member), out JsonElement loadMember))
                load.load_member = DataHelperModule.JsonToList<clsLoadMember>(loadMember);
            if (json.TryGetProperty(nameof(clsLoad.input_rows), out JsonElement inputRows))
            {
                if (inputRows.ValueKind == JsonValueKind.Null)
                    return load; // ParseLoadJson derives row positions from the load arrays.
                if (inputRows.ValueKind != JsonValueKind.Array)
                    throw new JsonException("input_rows must be an array.");
                load.input_rows = new List<int>();
                foreach (JsonElement value in inputRows.EnumerateArray())
                {
                    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int row))
                        throw new JsonException("input_rows must contain integers.");
                    load.input_rows.Add(row);
                }
            }
            return load;
        }

        private static Dictionary<string, object?> WriteLoad(clsLoad load)
        {
            var result = DataHelperModule.ClassToDictionary(load);
            WriteRows(result, nameof(clsLoad.load_node), load.load_node);
            WriteRows(result, nameof(clsLoad.load_member), load.load_member);
            var valueRows = (load.load_node?.Where(HasNodeValues).Select(row => row.row) ??
                    Enumerable.Empty<int>())
                .Concat(load.load_member?.Where(HasMemberValues).Select(row => row.row) ??
                    Enumerable.Empty<int>())
                .ToHashSet();
            if (load.input_rows?.Any(row => !valueRows.Contains(row)) == true)
                result[nameof(clsLoad.input_rows)] = load.input_rows.ToArray();
            else
                result.Remove(nameof(clsLoad.input_rows));
            return result;
        }

        private static void WriteRows<T>(
            Dictionary<string, object?> target,
            string key,
            List<T>? rows)
        {
            if (rows == null)
            {
                target.Remove(key);
                return;
            }

            var values = new List<Dictionary<string, object?>>();
            foreach (T row in rows)
            {
                bool hasData = row switch
                {
                    clsLoadNode node => HasNodeValues(node),
                    clsLoadMember member => HasMemberValues(member),
                    _ => false
                };
                if (hasData)
                    values.Add(DataHelperModule.ClassToDictionary(row));
            }
            if (values.Count == 0) target.Remove(key);
            else target[key] = values;
        }

        private static bool HasNodeValues(clsLoadNode node) =>
            !string.IsNullOrWhiteSpace(node.n) || node.tx != null || node.ty != null ||
            node.tz != null || node.rx != null || node.ry != null || node.rz != null;

        private static bool HasMemberValues(clsLoadMember member) =>
            !string.IsNullOrWhiteSpace(member.m1) || !string.IsNullOrWhiteSpace(member.m2) ||
            !string.IsNullOrWhiteSpace(member.direction) || !string.IsNullOrWhiteSpace(member.mark) ||
            !string.IsNullOrWhiteSpace(member.L1) || !string.IsNullOrWhiteSpace(member.L2) ||
            member.P1 != null || member.P2 != null;
    }
}

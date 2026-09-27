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
        public int Row { get; init; }
        public string CaseId { get; set; } = "1";
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
        private clsLoadMember EnsureMember() => Member ??= new clsLoadMember { row = Row };
        private clsLoadNode EnsureNode() => Node ??= new clsLoadNode { row = Row };
        private void Changed(string property) =>
            DocumentReplacementNotifications.Publish(PropertyChanged, this, new PropertyChangedEventArgs(property));
        public string? m1 { get => Member?.m1; set { EnsureMember().m1 = value; Changed(nameof(m1)); } }
        public string? m2 { get => Member?.m2; set { EnsureMember().m2 = value; Changed(nameof(m2)); } }
        public string? direction { get => Member?.direction; set { EnsureMember().direction = value; Changed(nameof(direction)); } }
        public string? mark { get => Member?.mark; set { EnsureMember().mark = value; Changed(nameof(mark)); } }
        public string? L1 { get => Member?.L1; set { EnsureMember().L1 = value; Changed(nameof(L1)); } }
        public string? L2 { get => Member?.L2; set { EnsureMember().L2 = value; Changed(nameof(L2)); } }
        public float? P1 { get => Member?.P1; set { EnsureMember().P1 = value; Changed(nameof(P1)); } }
        public float? P2 { get => Member?.P2; set { EnsureMember().P2 = value; Changed(nameof(P2)); } }
        public string? n { get => Node?.n; set { EnsureNode().n = value; Changed(nameof(n)); } }
        public float? tx { get => Node?.tx; set { EnsureNode().tx = value; Changed(nameof(tx)); } }
        public float? ty { get => Node?.ty; set { EnsureNode().ty = value; Changed(nameof(ty)); } }
        public float? tz { get => Node?.tz; set { EnsureNode().tz = value; Changed(nameof(tz)); } }
        public float? rx { get => Node?.rx; set { EnsureNode().rx = value; Changed(nameof(rx)); } }
        public float? ry { get => Node?.ry; set { EnsureNode().ry = value; Changed(nameof(ry)); } }
        public float? rz { get => Node?.rz; set { EnsureNode().rz = value; Changed(nameof(rz)); } }
    }

    internal class InputLoadService
    {
        private const int MaxNodeId = 100_000;
        // Lazy<T> を使ってスレッドセーフかつ遅延評価のシングルトンを実装
        private static readonly Lazy<InputLoadService> _instance =
            new Lazy<InputLoadService>(() => new InputLoadService());

        // 外部からはこのプロパティを通じてのみインスタンスにアクセスできる
        public static InputLoadService Instance => _instance.Value;

        private Dictionary<string, clsLoad> _load;
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
                LoadNames.Add(new clsLoadNameRow());
            IntensityRows.Add(new clsLoadIntensityRow { Row = 1 });
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
            ApplyLoads(ParseLoadJson(jsonData));
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
            foreach (JsonProperty caseJson in loadJson.EnumerateObject())
            {
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
            return normalized;
        }

        internal void ApplyLoads(Dictionary<string, clsLoad> prepared) => ReplaceLoad(prepared);

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
            ReplaceIntensityRows();
            DocumentReplacementNotifications.Publish(CasesChanged, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(LoadsEdited);
            if (previousCaseId != _selectedCaseId)
                DocumentReplacementNotifications.Publish(SelectedCaseChanged, _selectedCaseId);
        }

        public int FindIntensityRowIndex(string caseId, int rowNumber)
        {
            for (int index = 0; index < IntensityRows.Count; index++)
                if (IntensityRows[index].CaseId == caseId && IntensityRows[index].Row == rowNumber)
                    return index;
            return -1;
        }

        public clsLoadIntensityRow? GetIntensityRowAt(int displayIndex) =>
            displayIndex >= 0 && displayIndex < IntensityRows.Count ?
                IntensityRows[displayIndex] : null;

        public bool InsertIntensityRow(string caseId, int rowNumber)
        {
            if (!ValidCaseId(caseId) || rowNumber < 1 || rowNumber > MaxNodeId ||
                FindIntensityRowIndex(caseId, rowNumber) < 0)
                return false;
            _load.TryGetValue(caseId, out clsLoad? load);
            if (load?.input_rows?.Count > 0 && load.input_rows[^1] == MaxNodeId)
                return false;
            load ??= EnsureCase(caseId);
            load.input_rows ??= new List<int>();
            ShiftRows(load, rowNumber, +1);
            load.input_rows.Add(rowNumber);
            load.input_rows.Sort();
            FinishRowOperation();
            return true;
        }

        public bool DeleteIntensityRows(IEnumerable<(string CaseId, int Row)> rows)
        {
            if (rows == null) return false;
            var selected = rows.Distinct().ToArray();
            var visible = IntensityRows.Select(row => (row.CaseId, row.Row)).ToHashSet();
            if (selected.Length == 0 || selected.Any(item =>
                !ValidCaseId(item.CaseId) || !visible.Contains(item)))
                return false;

            foreach (var group in selected.GroupBy(item => item.CaseId))
            {
                if (!_load.TryGetValue(group.Key, out clsLoad? load) ||
                    load.input_rows == null) continue; // Unsaved starter row.
                var existing = load.input_rows.ToHashSet();
                int[] deleted = group.Select(item => item.Row)
                    .Where(existing.Contains).OrderBy(row => row).ToArray();
                if (deleted.Length == 0) continue;
                var deletedSet = deleted.ToHashSet();
                load.input_rows = load.input_rows.Where(row => !deletedSet.Contains(row))
                    .Select(row => row - CountBefore(deleted, row)).ToList();
                if (load.load_node != null)
                {
                    load.load_node.RemoveAll(node => deletedSet.Contains(node.row));
                    foreach (var node in load.load_node)
                        node.row -= CountBefore(deleted, node.row);
                }
                if (load.load_member != null)
                {
                    load.load_member.RemoveAll(member => deletedSet.Contains(member.row));
                    foreach (var member in load.load_member)
                        member.row -= CountBefore(deleted, member.row);
                }
                RemoveEmptyCase(group.Key, load);
            }
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
                row.Row < 1 || row.Row > MaxNodeId ||
                FindIntensityRowIndex(row.CaseId, row.Row) < 0)
                return false;
            if (row.CaseId == destinationCaseId) return true;
            _load.TryGetValue(destinationCaseId, out clsLoad? destination);
            if (destination?.input_rows?.Count > 0 &&
                destination.input_rows[^1] == MaxNodeId &&
                destination.input_rows[^1] >= row.Row)
                return false;

            _load.TryGetValue(row.CaseId, out clsLoad? source);
            bool persisted = source?.input_rows?.Contains(row.Row) ?? false;
            clsLoadNode? node = source?.load_node?.FirstOrDefault(item => item.row == row.Row);
            clsLoadMember? member = source?.load_member?.FirstOrDefault(item => item.row == row.Row);
            if (persisted && source != null)
            {
                source.input_rows!.Remove(row.Row);
                source.load_node?.Remove(node!);
                source.load_member?.Remove(member!);
                ShiftRowsAfterDeletion(source, row.Row);
                RemoveEmptyCase(row.CaseId, source);
            }
            destination ??= EnsureCase(destinationCaseId);
            destination.input_rows ??= new List<int>();
            ShiftRows(destination, row.Row, +1);
            destination.input_rows.Add(row.Row);
            destination.input_rows.Sort();
            if (node != null)
            {
                node.row = row.Row;
                (destination.load_node ??= new List<clsLoadNode>()).Add(node);
            }
            if (member != null)
            {
                member.row = row.Row;
                (destination.load_member ??= new List<clsLoadMember>()).Add(member);
            }
            FinishRowOperation(selectCase: destinationCaseId);
            DocumentReplacementNotifications.Defer(() =>
                IntensityRowMoved?.Invoke(destinationCaseId, row.Row));
            return true;
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
            ReplaceIntensityRows();
            DocumentReplacementNotifications.Publish(CasesChanged, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(LoadsEdited);
        }

        private void IntensityRows_ListChanged(object? sender, ListChangedEventArgs change)
        {
            if (change.ListChangedType != ListChangedType.ItemChanged || change.NewIndex < 0)
                return;
            if (change.PropertyDescriptor?.Name == nameof(clsLoadIntensityRow.LoadId))
                return;
            clsLoadIntensityRow row = IntensityRows[change.NewIndex];
            if (!_load.TryGetValue(row.CaseId, out clsLoad? load))
                load = EnsureCase(row.CaseId);
            UpdateNestedRow(load.load_member ??= new List<clsLoadMember>(), row.Row,
                row.HasMember ? row.Member : null, member => member.row);
            UpdateNestedRow(load.load_node ??= new List<clsLoadNode>(), row.Row,
                row.HasNode ? row.Node : null, node => node.row);
            // Editing the starter row makes it persistent; clearing an existing row
            // leaves its blank position in input_rows for save and reload.
            if (row.HasMember || row.HasNode)
            {
                load.input_rows ??= new List<int>();
                if (!load.input_rows.Contains(row.Row))
                {
                    load.input_rows.Add(row.Row);
                    load.input_rows.Sort();
                    ReplaceIntensityRows();
                }
            }
            if (!HasData(load)) _load.Remove(row.CaseId);
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

        private void ReplaceLoad(Dictionary<string, clsLoad> next)
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
                DocumentReplacementNotifications.Defer(() => LoadNames.ResetBindings());
            }
            ReplaceIntensityRows();
            DocumentReplacementNotifications.Publish(CasesChanged, this, EventArgs.Empty);
            DocumentReplacementNotifications.Publish(LoadsEdited);
            if (previousCaseId != _selectedCaseId)
                DocumentReplacementNotifications.Publish(SelectedCaseChanged, _selectedCaseId);
        }

        private void ReplaceIntensityRows()
        {
            IntensityRows.RaiseListChangedEvents = false;
            try
            {
                IntensityRows.Clear();
                foreach (var (caseId, load) in _load)
                {
                    var members = load.load_member?.ToDictionary(member => member.row) ??
                        new Dictionary<int, clsLoadMember>();
                    var nodes = load.load_node?.ToDictionary(node => node.row) ??
                        new Dictionary<int, clsLoadNode>();
                    var rows = load.input_rows ?? members.Keys.Concat(nodes.Keys)
                        .Distinct().OrderBy(row => row).ToList();
                    foreach (int rowNumber in rows)
                    {
                        members.TryGetValue(rowNumber, out clsLoadMember? member);
                        nodes.TryGetValue(rowNumber, out clsLoadNode? node);
                        IntensityRows.Add(new clsLoadIntensityRow
                        {
                            CaseId = caseId, Row = rowNumber, Member = member, Node = node
                        });
                    }
                    int starter = rows.Count == 0 ? 1 : rows[^1] + 1;
                    if (starter <= MaxNodeId)
                        IntensityRows.Add(new clsLoadIntensityRow { CaseId = caseId, Row = starter });
                }
                if (_load.Count == 0)
                    IntensityRows.Add(new clsLoadIntensityRow { CaseId = "1", Row = 1 });
            }
            finally
            {
                IntensityRows.RaiseListChangedEvents = true;
                DocumentReplacementNotifications.Defer(() => IntensityRows.ResetBindings());
            }
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

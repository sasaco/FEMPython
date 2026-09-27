using FrameWebforCS.components.input;
using THREE;

namespace FrameWebforCS.three;

internal sealed record LoadMemberFrame(int Ni, int Nj, float Cg = 0);
internal sealed record MaxLoadDict(float pMax, float mMax, float wMax,
    float rMax, float qMax);

/// <summary>
/// Case-owned counterpart of JS three-load.service.ts. The coordinator calls ReplaceAll
/// on a committed load edit or node/member topology edit, on the GL-owning thread.
/// </summary>
internal sealed class ThreeLoadsService : IDisposable
{
    private readonly Scene _scene;
    private readonly Group _root = new() { Name = "loads" };
    private readonly Dictionary<string, Group> _cases = new();
    private readonly Dictionary<string, string[]> _movingCases = new();
    private readonly Dictionary<string, MaxLoadDict> _maxLoadDicts = new();
    private readonly List<LoadGlyph> _glyphs = new();
    private string? _currentCaseId;
    private string? _displayCaseId;
    private string[]? _animationCases;
    private int _animationIndex;
    private float _animationTime;
    private float _loadScale = 100;
    private bool _visible;
    private bool _disposed;

    private sealed record LoadGlyph(string CaseId, int Row, string Column, string Family,
        int SourceId, Group Root, Vector3 Anchor, Action<bool> Highlight,
        IDisposable Geometry);

    internal ThreeLoadsService(Scene scene)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _scene.Add(_root);
    }

    internal int GlyphCount => _glyphs.Count;
    internal int VisibleGlyphCount => _glyphs.Count(g => g.CaseId == _displayCaseId && _visible);
    internal string? CurrentCaseId => _currentCaseId;
    internal string? DisplayCaseId => _displayCaseId;
    internal (int Row, string Column)? Selection { get; private set; }
    internal IReadOnlyList<(string Family, int SourceId, int Row, string Column, Vector3 Anchor)> Glyphs =>
        _glyphs.Select(g => (g.Family, g.SourceId, g.Row, g.Column, g.Anchor.Clone())).ToArray();
    internal IReadOnlyDictionary<string, MaxLoadDict> MaxLoadDicts => _maxLoadDicts;

    internal void SetLoadScale(float value)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(value) || value is < 0 or > 400)
            throw new ArgumentOutOfRangeException(nameof(value));
        _loadScale = value;
    }

    internal void ReplaceAll(IReadOnlyDictionary<string, LoadCaseDisplay> cases,
        IReadOnlyDictionary<int, Vector3> nodes, IReadOnlyDictionary<int, LoadMemberFrame> members,
        float baseScale = 1, int dimension = 3)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(members);
        if (!float.IsFinite(baseScale) || baseScale <= 0 || dimension is not (2 or 3))
            throw new ArgumentOutOfRangeException(nameof(baseScale));
        ClearCases();
        var lengths = new Dictionary<int, float>();
        foreach (var (memberId, member) in members)
            if (nodes.TryGetValue(member.Ni, out var ni) &&
                nodes.TryGetValue(member.Nj, out var nj))
            {
                float length = new Vector3().SubVectors(nj, ni).Length();
                if (float.IsFinite(length) && length > 0) lengths[memberId] = length;
            }
        var projected = new Dictionary<string, (List<LoadNodeDisplay> Nodes,
            List<LoadMemberDisplay> Members)>();
        foreach (var (id, data) in cases)
        {
            projected[id] = (data.NodeLoads.ToList(), new List<LoadMemberDisplay>());
            var expanded = LoadDisplayConversion.ExpandMemberLoads(id, data.MemberLoads,
                lengths, data.Symbol, data.LLPitch);
            if (data.Symbol == "LL" && expanded.Count > 1)
                _movingCases[id] = expanded.Keys.ToArray();
            foreach (var (displayCase, rows) in expanded)
            {
                if (!projected.TryGetValue(displayCase, out var target))
                    projected[displayCase] = target = (new List<LoadNodeDisplay>(),
                        new List<LoadMemberDisplay>());
                target.Members.AddRange(rows);
            }
        }
        foreach (var (id, data) in projected)
        {
            var group = new Group { Name = $"load-case-{id}", Visible = false };
            _cases.Add(id, group);
            _root.Add(group);
            // JS ThreeLoadService.updateMaxLoad aggregates each LoadData family into
            // pMax/mMax/wMax/rMax/qMax, then onResize passes maxLoadDict to relocate.
            var maxLoadDict = GetMaxLoadDict(data.Nodes, data.Members, nodes, members,
                dimension);
            _maxLoadDicts[id] = maxLoadDict;
            foreach (var row in data.Nodes)
                AddNodeLoad(id, group, row, nodes, maxLoadDict.pMax,
                    maxLoadDict.mMax, baseScale);
            foreach (var row in data.Members)
                AddMemberLoad(id, group, row, nodes, members, baseScale, dimension,
                    maxLoadDict);
            // JS loadListSort uses rank, then the source grid row before
            // onResize/relocate places overlapping shapes.
            foreach (var glyph in _glyphs.Where(g => g.CaseId == id)
                         .OrderBy(LegacyRank).ThenBy(g => g.Row))
            {
                group.Remove(glyph.Root);
                group.Add(glyph.Root);
            }
        }
        if (_currentCaseId == null || !_cases.ContainsKey(_currentCaseId))
            _currentCaseId = _cases.Keys.FirstOrDefault();
        SelectDisplayCase(_currentCaseId);
        Selection = null;
        ApplyVisibility();
    }

    internal void SetCase(string? id)
    {
        ThrowIfDisposed();
        if (_currentCaseId == id) return;
        // JS changeCase clears the previous highlight and switches the case root.
        _currentCaseId = id;
        SelectDisplayCase(id);
        Select(null, null);
        ApplyVisibility();
    }

    internal void AdvanceAnimation(float seconds)
    {
        ThrowIfDisposed();
        if (!_visible || _animationCases is not { Length: > 1 } ||
            !float.IsFinite(seconds) || seconds <= 0) return;
        // JS ThreeLoadService.new_animation advances fractional LL cases every 0.5 s.
        _animationTime += seconds;
        if (_animationTime < 0.5f) return;
        int steps = (int)(_animationTime / 0.5f);
        _animationTime -= steps * 0.5f;
        _animationIndex = (_animationIndex + steps) % _animationCases.Length;
        _displayCaseId = _animationCases[_animationIndex];
        ApplyVisibility();
    }

    internal void SetVisible(bool visible)
    {
        ThrowIfDisposed();
        _visible = visible;
        if (!visible) Select(null, null);
        ApplyVisibility();
    }

    internal void Select(int? row, string? column)
    {
        ThrowIfDisposed();
        // JS selectChange(row,column) maps node columns individually, all other
        // columns to member `m`. Preserve the same grid highlight key.
        string key = column is "n" or "tx" or "ty" or "tz" or "rx" or "ry" or "rz"
            ? column : "m";
        Selection = _visible && row is > 0 ? (row.Value, key) : null;
        foreach (var glyph in _glyphs)
        {
            bool selected = glyph.CaseId == _displayCaseId &&
                Selection is { } target && glyph.Row == target.Row &&
                (target.Column == "n" ? glyph.Column != "m" : glyph.Column == target.Column);
            glyph.Highlight(selected);
        }
    }

    private void AddNodeLoad(string id, Group group, LoadNodeDisplay row,
        IReadOnlyDictionary<int, Vector3> nodes, float pMax, float mMax, float baseScale)
    {
        if (!nodes.TryGetValue(row.NodeId, out var position)) return;
        // JS ThreeLoadService.onResize supplies per-node OffsetDict lanes to avoid
        // overlapping arrows. C# currently leaves each arrow at its node anchor;
        // stacked offset placement is still pending.
        // JS getNodeLoadJson(0) maps negative n to imposed displacement; its
        // createPointLoad looks up that negative ID and silently drops it. Keep the
        // already projected C# displacement visible as a deliberate correction.
        var values = new[] { row.Tx, row.Ty, row.Tz, row.Rx, row.Ry, row.Rz };
        var keys = new[] { "tx", "ty", "tz", "rx", "ry", "rz" };
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == 0 || !float.IsFinite(values[i])) continue;
            LoadNodeGlyph? glyph = i < 3
                ? LoadNodeGeometry.CreatePoint(position, keys[i], values[i], pMax,
                    baseScale * 10, _loadScale)
                : LoadNodeGeometry.CreateMoment(position, keys[i], values[i], mMax,
                    baseScale * 10, _loadScale);
            if (glyph == null) continue;
            string family = row.IsDisplacement ? "displacement" :
                i < 3 ? "node-force" : "node-moment";
            glyph.Root.Name = $"load-{family}-{row.Row}-{keys[i]}-{row.NodeId}";
            group.Add(glyph.Root);
            _glyphs.Add(new LoadGlyph(id, row.Row, keys[i], family, row.NodeId,
                glyph.Root, glyph.Anchor, glyph.Highlight, glyph));
        }
    }

    private void AddMemberLoad(string id, Group group, LoadMemberDisplay row,
        IReadOnlyDictionary<int, Vector3> nodes, IReadOnlyDictionary<int, LoadMemberFrame> members,
        float baseScale, int dimension, MaxLoadDict maxLoadDict)
    {
        // JS InputLoadService.getMemberLoadJson(0) converts m1/m2 and chained L1/L2
        // into one-member display rows before ThreeLoadService.createMemberLoad.
        int memberId = row.MemberStart;
        if (memberId <= 0 || row.MemberEnd != memberId ||
            !members.TryGetValue(memberId, out var member) ||
            !nodes.TryGetValue(member.Ni, out var ni) ||
            !nodes.TryGetValue(member.Nj, out var nj) || !float.IsFinite(member.Cg))
            return;
        float maxValue = row.Mark switch
        {
            1 => maxLoadDict.pMax,
            11 => maxLoadDict.mMax,
            2 when row.Direction == "r" => maxLoadDict.rMax,
            2 when IsAxialDistribution(row.Direction, ni, nj) => maxLoadDict.qMax,
            2 => maxLoadDict.wMax,
            _ => 0
        };
        var glyph = MemberLoadGeometryFactory.Create(new MemberLoadGeometryInput(
            ni, nj, row.Mark, row.Direction, row.L1, row.L2, row.P1, row.P2,
            member.Cg, baseScale, _loadScale, maxValue, dimension == 3));
        if (glyph == null) return;
        glyph.Root.Name = $"load-{glyph.Family}-{row.Row}-m-{memberId}";
        group.Add(glyph.Root);
        _glyphs.Add(new LoadGlyph(id, row.Row, "m", glyph.Family, memberId,
            glyph.Root, glyph.Anchor, glyph.SetSelected, glyph));
    }

    private static MaxLoadDict GetMaxLoadDict(
        IReadOnlyList<LoadNodeDisplay> nodeLoads,
        IReadOnlyList<LoadMemberDisplay> memberLoads,
        IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, LoadMemberFrame> members, int dimension)
    {
        float pMax = 0, mMax = 0, wMax = 0, rMax = 0, qMax = 0;
        foreach (var row in nodeLoads)
        {
            if (!nodes.ContainsKey(row.NodeId)) continue;
            pMax = MathF.Max(pMax, MaxAbsolute(row.Tx, row.Ty, row.Tz));
            mMax = MathF.Max(mMax, MaxAbsolute(row.Rx, row.Ry, row.Rz));
        }
        foreach (var row in memberLoads)
        {
            if (row.MemberStart != row.MemberEnd ||
                !members.TryGetValue(row.MemberStart, out var member) ||
                !nodes.TryGetValue(member.Ni, out var ni) ||
                !nodes.TryGetValue(member.Nj, out var nj) || !float.IsFinite(member.Cg))
                continue;
            float length = new Vector3().SubVectors(nj, ni).Length();
            if (!float.IsFinite(length) || length <= 1e-6f ||
                row.P1 == 0 && row.P2 == 0 ||
                row.Mark is 1 or 11 && (row.L1 > length || row.L2 > length) ||
                row.Mark == 2 && row.L1 + row.L2 > length)
                continue;
            string direction = row.Direction;
            bool supported = row.Mark switch
            {
                1 => dimension == 3 ? direction is "x" or "y" or "z" or "gx" or "gy" or "gz"
                    : direction is "x" or "y" or "gx" or "gy",
                11 => dimension == 3 ? direction is "x" or "y" or "z" or "gx" or "gy" or "gz"
                    : direction is "z" or "gz",
                2 => dimension == 3 ? direction is "x" or "y" or "z" or "gx" or "gy" or "gz" or "r"
                    : direction is "x" or "y" or "gx" or "gy",
                _ => false
            };
            if (!supported) continue;
            float value = MaxAbsolute(row.P1, row.P2);
            switch (row.Mark)
            {
                case 1: pMax = MathF.Max(pMax, value); break;
                case 11: mMax = MathF.Max(mMax, value); break;
                case 2 when row.Direction == "r":
                    rMax = MathF.Max(rMax, value); break;
                case 2 when IsAxialDistribution(row.Direction, ni, nj):
                    qMax = MathF.Max(qMax, value); break;
                case 2: wMax = MathF.Max(wMax, value); break;
            }
        }
        return new MaxLoadDict(pMax, mMax, wMax, rMax, qMax);
    }

    private static float MaxAbsolute(params float[] values) =>
        values.Where(float.IsFinite).Select(MathF.Abs).DefaultIfEmpty(0).Max();

    private static int LegacyRank(LoadGlyph glyph) => glyph.Family switch
    {
        "node-moment" or "member-moment" or "member-torsion" => 0,
        "displacement" when glyph.Column.StartsWith("r", StringComparison.Ordinal) => 0,
        "node-force" or "displacement" => 20,
        _ => 10
    };

    private static bool IsAxialDistribution(string direction, Vector3 ni, Vector3 nj) =>
        direction == "x" ||
        direction == "gx" && ni.Y == nj.Y && ni.Z == nj.Z ||
        direction == "gy" && ni.X == nj.X && ni.Z == nj.Z ||
        direction == "gz" && ni.X == nj.X && ni.Y == nj.Y;

    private void ApplyVisibility()
    {
        foreach (var (id, group) in _cases)
            group.Visible = _visible && id == _displayCaseId;
    }

    private void SelectDisplayCase(string? id)
    {
        _animationCases = id != null && _movingCases.TryGetValue(id, out var keys)
            ? keys : null;
        _animationIndex = 0;
        _animationTime = 0;
        _displayCaseId = _animationCases?.FirstOrDefault() ?? id;
    }

    private void ClearCases()
    {
        Selection = null;
        foreach (var glyph in _glyphs)
        {
            if (_cases.TryGetValue(glyph.CaseId, out var owner)) owner.Remove(glyph.Root);
            glyph.Geometry.Dispose();
        }
        _glyphs.Clear();
        foreach (var group in _cases.Values)
        {
            _root.Remove(group);
            group.Dispose();
        }
        _cases.Clear();
        _movingCases.Clear();
        _maxLoadDicts.Clear();
        _animationCases = null;
        _displayCaseId = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearCases();
        _scene.Remove(_root);
        _root.Dispose();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

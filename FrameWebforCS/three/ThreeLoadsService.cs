using FrameWebforCS.components.input;
using System.Globalization;
using THREE;
using Color = THREE.Color;

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
    private float _animationElapsedTime;
    private float _animationDuration = 0.5f;
    private float _loadScale = 100;
    private bool _visible;
    private bool _disposed;

    private sealed record LoadPlacement(int[] NodeIds, int[] MemberIds, bool IsNode,
        LoadLocalAxis? LocalAxis, string Direction, ConflictSection Section,
        Vector3 OffsetVector, float Extent, string? OtherDirection = null,
        float Coef1 = 0, float Coef2 = 0);

    private sealed record LoadGlyph(string CaseId, int Row, string Column, string Family,
        int SourceId, Group Root, Vector3 Anchor, Action<bool> Highlight,
        IDisposable Geometry, LoadPlacement? Placement,
        IReadOnlyList<ViewportTextLabel> Labels, IReadOnlyList<Line> DimensionLines);

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
    internal IReadOnlyList<ViewportTextLabel> GetVisibleLabels()
    {
        if (!_visible || Selection is not { } target) return [];
        var labels = new List<ViewportTextLabel>();
        foreach (var glyph in _glyphs)
        {
            if (glyph.CaseId != _displayCaseId || !IsSelected(glyph, target))
                continue;
            var delta = glyph.Column == "m" ? glyph.Root.Position.Clone() :
                glyph.Root.Position.Clone().Sub(glyph.Anchor);
            foreach (var label in glyph.Labels)
                labels.Add(label with { Position = label.Position.Clone().Add(delta) });
        }
        return labels;
    }
    internal (bool IsActive, float ElapsedTime, int CurrentIndex, int TotalKeys)
        AnimationStatus => (_visible && _animationCases is { Length: > 1 },
            _animationElapsedTime, _animationIndex, _animationCases?.Length ?? 0);

    internal void SetAnimationDuration(float seconds)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(seconds) || seconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        _animationDuration = seconds;
    }

    internal void StopAnimation()
    {
        ThrowIfDisposed();
        _animationCases = null;
        _animationTime = 0;
        _animationElapsedTime = 0;
        _animationIndex = 0;
    }

    internal void RestartAnimation()
    {
        ThrowIfDisposed();
        SelectDisplayCase(_currentCaseId);
        ApplyVisibility();
    }

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
            var expanded = LoadCaseExpansion.ExpandMemberLoads(id, data.MemberLoads,
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
                AddNodeLoad(id, group, row, nodes, members, maxLoadDict.pMax,
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
            RelocateCaseGlyphs(id);
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
        // JS ThreeLoadService.new_animation uses a configurable duration per LL case.
        _animationElapsedTime += seconds;
        _animationTime += seconds;
        if (_animationTime < _animationDuration) return;
        double steps = Math.Floor((double)_animationTime / _animationDuration);
        _animationTime = (float)((double)_animationTime % _animationDuration);
        _animationIndex = (_animationIndex + (int)(steps % _animationCases.Length)) %
            _animationCases.Length;
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
                Selection is { } target && IsSelected(glyph, target);
            glyph.Highlight(selected);
            foreach (var line in glyph.DimensionLines) line.Visible = selected;
        }
    }

    private static bool IsSelected(LoadGlyph glyph, (int Row, string Column) target) =>
        glyph.Row == target.Row && (target.Column == "n" ?
            glyph.Column != "m" : glyph.Column == target.Column);

    private void AddNodeLoad(string id, Group group, LoadNodeDisplay row,
        IReadOnlyDictionary<int, Vector3> nodes,
        IReadOnlyDictionary<int, LoadMemberFrame> members,
        float pMax, float mMax, float baseScale)
    {
        if (!nodes.TryGetValue(row.NodeId, out var position)) return;
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
            var offsetVector = new Vector3(
                keys[i] == "tx" ? -MathF.Sign(values[i]) : 0,
                keys[i] == "ty" ? -MathF.Sign(values[i]) : 0,
                keys[i] == "tz" ? -MathF.Sign(values[i]) : 0);
            int[] adjacentMembers = members.Where(m => m.Value.Ni == row.NodeId ||
                m.Value.Nj == row.NodeId).Select(m => m.Key).ToArray();
            var placement = new LoadPlacement([row.NodeId], adjacentMembers, true,
                null, i < 3 ? $"g{keys[i][1]}{(values[i] < 0 ? '+' : '-')}" :
                    $"rg{keys[i][1]}", ConflictSection.EndToEnd,
                offsetVector, glyph.Size);
            _glyphs.Add(new LoadGlyph(id, row.Row, keys[i], family, row.NodeId,
                glyph.Root, glyph.Anchor, glyph.Highlight, glyph, placement,
                [new ViewportTextLabel(FormatValue(values[i],
                    i < 3 ? " kN" : " kN m"),
                    position.Clone().Add(new Vector3(0, 0, baseScale * 0.2f)))], []));
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
        var dimensionLines = CreateDimensionLines(glyph.Root, row, ni, nj,
            baseScale);
        var placement = MemberPlacement(row, member, ni, nj, maxValue,
            baseScale * 10 * _loadScale / 100, glyph.Family);
        _glyphs.Add(new LoadGlyph(id, row.Row, "m", glyph.Family, memberId,
            glyph.Root, glyph.Anchor, glyph.SetSelected, glyph, placement,
            MemberLabels(row, ni, nj, glyph.Family, baseScale), dimensionLines));
    }

    private static IReadOnlyList<Line> CreateDimensionLines(Group root,
        LoadMemberDisplay row, Vector3 ni, Vector3 nj, float baseScale)
    {
        var edge = new Vector3().SubVectors(nj, ni);
        float length = edge.Length();
        if (length <= 0) return [];
        var axis = edge.Normalize();
        var normal = new Vector3().CrossVectors(axis,
            MathF.Abs(axis.Z) < 0.9f ? new Vector3(0, 0, 1) :
                new Vector3(1, 0, 0)).Normalize();
        var offset = normal.MultiplyScalar(MathF.Max(baseScale, 0.01f) * 0.5f);
        float start = row.Mark == 9 ? 0 : Math.Clamp(row.L1, 0, length);
        float end = row.Mark == 9 ? length : Math.Clamp(
            row.Mark == 2 ? length - row.L2 : row.L2, 0, length);
        if (row.Mark is 1 or 11 && end < start) (start, end) = (end, start);
        var stations = new[] { 0f, start, end, length }.Distinct().Order().ToArray();
        var lines = new List<Line>();
        Vector3 At(float distance) => ni.Clone().AddScaledVector(axis, distance);
        void Add(Vector3 a, Vector3 b)
        {
            Vector3[] points = [a, b];
            var line = new Line(new BufferGeometry().SetFromPoints(points),
                new LineBasicMaterial { Color = Color.Hex(0x000000) })
            { Name = "load-dimension", Visible = false };
            root.Add(line);
            lines.Add(line);
        }
        for (int i = 0; i < stations.Length - 1; i++)
            Add(At(stations[i]).Add(offset), At(stations[i + 1]).Add(offset));
        foreach (float station in stations)
            Add(At(station), At(station).Add(offset));
        return lines;
    }

    private static IReadOnlyList<ViewportTextLabel> MemberLabels(
        LoadMemberDisplay row, Vector3 ni, Vector3 nj, string family, float scale)
    {
        var labels = new List<ViewportTextLabel>();
        var edge = new Vector3().SubVectors(nj, ni);
        float length = edge.Length();
        if (length <= 0) return labels;
        var axis = edge.Normalize();
        string unit = family switch
        {
            "member-temperature" => " °C",
            "member-torsion" or "member-moment" => " kNm/m",
            _ => " kN/m"
        };
        Vector3 At(float distance) => ni.Clone().AddScaledVector(axis, distance)
            .Add(new Vector3(0, 0, scale * 0.2f));
        var normal = new Vector3().CrossVectors(axis,
            MathF.Abs(axis.Z) < 0.9f ? new Vector3(0, 0, 1) :
                new Vector3(1, 0, 0)).Normalize();
        Vector3 DimAt(float distance) => ni.Clone().AddScaledVector(axis, distance)
            .AddScaledVector(normal, MathF.Max(scale, 0.01f) * 0.5f);
        labels.Add(new ViewportTextLabel(FormatValue(row.P1, unit),
            At(row.Mark == 9 ? length * 0.5f : row.L1)));
        if (row.P2 != 0 && row.Mark != 9)
            labels.Add(new ViewportTextLabel(FormatValue(row.P2, unit),
                At(row.Mark == 2 ? length - row.L2 : row.L2)));
        float start = row.Mark == 9 ? 0 : row.Mark == 2 ? row.L1 :
            MathF.Min(row.L1, row.L2);
        float end = row.Mark == 9 ? length : row.Mark == 2 ? length - row.L2 :
            MathF.Max(row.L1, row.L2);
        if (start > 0)
            labels.Add(new ViewportTextLabel(start.ToString("F3",
                CultureInfo.InvariantCulture), DimAt(start * 0.5f)));
        if (end > start)
            labels.Add(new ViewportTextLabel((end - start).ToString("F3",
                CultureInfo.InvariantCulture), DimAt((start + end) * 0.5f)));
        float tail = length - end;
        if (tail > 0)
            labels.Add(new ViewportTextLabel(tail.ToString("F3",
                CultureInfo.InvariantCulture), DimAt(end + tail * 0.5f)));
        return labels;
    }

    private static string FormatValue(float value, string unit) =>
        value.ToString("F2", CultureInfo.InvariantCulture) + unit;

    private void RelocateCaseGlyphs(string id)
    {
        var nodeOffsetDictMap = new Dictionary<int, LoadOffsetDict>();
        var memberOffsetDictMap = new Dictionary<int, LoadOffsetDict>();
        var localAxes = _glyphs.Where(g => g.CaseId == id &&
            g.Placement is { IsNode: false, LocalAxis: not null })
            .Select(g => g.Placement!)
            .GroupBy(p => p.MemberIds[0])
            .ToDictionary(g => g.Key, g => g.First().LocalAxis!);
        foreach (var glyph in _glyphs.Where(g => g.CaseId == id)
                     .OrderBy(LegacyRank).ThenBy(g => g.Row))
        {
            if (glyph.Placement is not { } placement) continue;
            var nodeDicts = new List<LoadOffsetDict>();
            foreach (int nodeId in placement.NodeIds)
            {
                if (!nodeOffsetDictMap.TryGetValue(nodeId, out var dict))
                    nodeOffsetDictMap[nodeId] = dict = new LoadOffsetDict();
                nodeDicts.Add(dict);
            }
            var memberDicts = new List<LoadOffsetDict>();
            foreach (int memberId in placement.MemberIds)
            {
                if (!memberOffsetDictMap.TryGetValue(memberId, out var dict))
                    memberOffsetDictMap[memberId] = dict = new LoadOffsetDict(
                        localAxes.GetValueOrDefault(memberId));
                memberDicts.Add(dict);
            }
            if (placement.OtherDirection != null)
            {
                RelocateDistributed(glyph, placement, nodeDicts, memberDicts);
                continue;
            }
            var getter = placement.IsNode
                ? Enumerable.Concat(nodeDicts, memberDicts) : memberDicts;
            var offsetData = getter.Select(dict => dict.Get(placement.Direction,
                    placement.IsNode ? ConflictSection.EndToEnd : placement.Section))
                .OrderByDescending(x => x.Offset).First();
            if (placement.Direction.StartsWith('r') || placement.Direction == "R")
            {
                // Legacy moment and torsion geometry stays centered but blocks
                // translation lanes around its radius.
                foreach (var dict in Enumerable.Concat(nodeDicts, memberDicts))
                {
                    var section = nodeDicts.Contains(dict) ?
                        ConflictSection.EndToEnd : placement.Section;
                    dict.Update(placement.Direction, placement.Extent, true, section);
                }
                continue;
            }
            glyph.Root.Position.AddScaledVector(placement.OffsetVector,
                offsetData.Offset);
            IEnumerable<LoadOffsetDict> updateDicts = placement.IsNode
                ? nodeDicts : Enumerable.Concat(memberDicts, nodeDicts);
            foreach (var dict in updateDicts)
            {
                var section = placement.IsNode || nodeDicts.Contains(dict)
                    ? ConflictSection.EndToEnd : placement.Section;
                bool conflicted = placement.IsNode ||
                    dict.Get(placement.Direction, section).Conflicted;
                dict.Update(placement.Direction, offsetData.Offset + placement.Extent,
                    conflicted, section);
            }
        }
    }

    private static void RelocateDistributed(LoadGlyph glyph,
        LoadPlacement placement, IReadOnlyList<LoadOffsetDict> nodeDicts,
        IReadOnlyList<LoadOffsetDict> memberDicts)
    {
        var section = placement.Section;
        string positive = placement.Direction;
        string negative = placement.OtherDirection!;
        float pOffset = memberDicts.Max(d => d.Get(positive, section).Offset);
        float nOffset = memberDicts.Max(d => d.Get(negative, section).Offset);
        var conflictFlags = memberDicts.Select(d => (
            Positive: d.Get(positive, section).Conflicted,
            Negative: d.Get(negative, section).Conflicted)).ToArray();
        float aMax = MathF.Max(placement.Coef1, placement.Coef2);
        float aMin = MathF.Min(placement.Coef1, placement.Coef2);
        float offset = pOffset == 0 && nOffset == 0 ? 0 :
            aMax < 0 ? -nOffset - MathF.Max(0, aMin) :
                pOffset - MathF.Min(0, aMin);
        glyph.Root.Position.AddScaledVector(placement.OffsetVector, offset);
        foreach (float end in new[] { offset + placement.Coef1,
                     offset + placement.Coef2 })
        {
            string direction = end < 0 ? negative : positive;
            float clearance = MathF.Abs(end);
            for (int i = 0; i < memberDicts.Count; i++)
                memberDicts[i].Update(direction, clearance,
                    end < 0 ? conflictFlags[i].Negative : conflictFlags[i].Positive,
                    section);
            foreach (var dict in nodeDicts)
                dict.Update(direction, clearance, true,
                    ConflictSection.EndToEnd);
        }
    }

    private static LoadPlacement? MemberPlacement(LoadMemberDisplay row,
        LoadMemberFrame member, Vector3 ni, Vector3 nj, float maxValue,
        float scale, string family)
    {
        if (family != "member-temperature" && maxValue <= 0 || scale <= 0)
            return null;
        var axis = new Vector3().SubVectors(nj, ni);
        float length = axis.Length();
        if (length <= 0) return null;
        axis.Normalize();
        var reference = ni.X == nj.X && ni.Y == nj.Y
            ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1);
        var localY = new Vector3().CrossVectors(reference, axis).Normalize();
        var localZ = new Vector3().CrossVectors(axis, localY).Normalize();
        float radians = member.Cg * MathF.PI / 180;
        var rotatedY = localY.Clone().MultiplyScalar(MathF.Cos(radians))
            .AddScaledVector(localZ, MathF.Sin(radians));
        var rotatedZ = localZ.Clone().MultiplyScalar(MathF.Cos(radians))
            .AddScaledVector(localY, -MathF.Sin(radians));
        var force = row.Direction switch
        {
            "x" => axis, "y" => rotatedY, "z" => rotatedZ,
            "gx" => new Vector3(1, 0, 0), "gy" => new Vector3(0, 1, 0),
            "gz" => new Vector3(0, 0, 1), _ => rotatedY
        };
        var offsetVector = family switch
        {
            "member-temperature" => rotatedY.Clone().MultiplyScalar(-1),
            "member-distributed" => force.Clone().MultiplyScalar(-1),
            "member-axial" or "member-point" when row.Direction == "x" =>
                rotatedY.Clone(),
            "member-axial" => rotatedY.Clone(),
            _ => force.Clone().MultiplyScalar(-MathF.Sign(
                MathF.Abs(row.P1) >= MathF.Abs(row.P2) ? row.P1 : row.P2))
        };
        float extent = family switch
        {
            "member-temperature" or "member-axial" => scale * 0.1f,
            "member-moment" => scale * MathF.Max(MathF.Abs(row.P1),
                MathF.Abs(row.P2)) / maxValue,
            "member-torsion" => scale * MathF.Max(MathF.Abs(row.P1),
                MathF.Abs(row.P2)) / maxValue,
            "member-point" => 2.5f * scale * MathF.Max(MathF.Abs(row.P1),
                MathF.Abs(row.P2)) / maxValue,
            _ => scale * MathF.Max(MathF.Abs(row.P1), MathF.Abs(row.P2)) / maxValue
        };
        var section = row.Mark == 2
            ? new ConflictSection(row.L1, length - row.L2)
            : row.Mark == 9 ? ConflictSection.EndToEnd
            : new ConflictSection(MathF.Min(row.L1, row.L2),
                MathF.Max(row.L1, row.L2) + 0.001f);
        char sign = (MathF.Abs(row.P1) >= MathF.Abs(row.P2)
            ? row.P1 : row.P2) < 0 ? '+' : '-';
        string direction = family switch
        {
            "member-temperature" => "ly-",
            "member-axial" => "ly+",
            "member-torsion" => "R",
            "member-moment" => row.Direction.StartsWith('g')
                ? $"r{row.Direction}" : $"rl{row.Direction}",
            "member-point" when row.Direction == "x" => "ly+",
            "member-distributed" => $"{(row.Direction.StartsWith('g') ?
                row.Direction : $"l{row.Direction}")}-",
            _ => $"{(row.Direction.StartsWith('g') ? row.Direction :
                $"l{row.Direction}")}{sign}"
        };
        return new LoadPlacement([member.Ni, member.Nj], [row.MemberStart], false,
            new LoadLocalAxis(axis, rotatedY, rotatedZ), direction, section,
            offsetVector, extent,
            family == "member-distributed" ? direction[..^1] + "+" : null,
            family == "member-distributed" ? row.P1 / maxValue * scale : 0,
            family == "member-distributed" ? row.P2 / maxValue * scale : 0);
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
        _animationElapsedTime = 0;
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

using THREE;

namespace FrameWebforCS.three;

/// <summary>Legacy ConflictSection: touching ends do not overlap.</summary>
internal readonly record struct ConflictSection(float Start, float End)
{
    internal static readonly ConflictSection EndToEnd = new(0, float.MaxValue);

    internal bool ConflictsTo(ConflictSection target) =>
        Start < target.End && End > target.Start;
}

internal sealed record LoadLocalAxis(Vector3 X, Vector3 Y, Vector3 Z);

/// <summary>
/// C# counterpart of JS OffsetDict. Named local/global lanes propagate their
/// clearances through the member-axis coefficients. Only overlapping member
/// station sections use currOffset; disjoint sections can reuse lastOffset.
/// </summary>
internal sealed class LoadOffsetDict
{
    private sealed class Lane
    {
        internal readonly List<ConflictSection> Sections = new();
        internal float CurrOffset;
        internal float LastOffset;
    }

    private readonly LoadLocalAxis? _localAxis;
    private readonly Dictionary<string, Lane> _dict = new();

    internal LoadOffsetDict(LoadLocalAxis? localAxis = null) => _localAxis = localAxis;

    internal (float Offset, bool Conflicted) Get(string direction,
        ConflictSection section)
    {
        if (!_dict.TryGetValue(direction, out var lane)) return (0, false);
        bool conflicts = lane.Sections.Any(s => s.ConflictsTo(section));
        return (conflicts ? lane.CurrOffset : lane.LastOffset, conflicts);
    }

    internal void Update(string direction, float endOffset, bool conflicted,
        ConflictSection section)
    {
        if (!float.IsFinite(endOffset) || endOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(endOffset));
        foreach (var (mappedDirection, factor) in Coefficients(direction))
        {
            if (!_dict.TryGetValue(mappedDirection, out var lane))
                _dict[mappedDirection] = lane = new Lane();
            if (conflicted)
            {
                lane.Sections.Clear();
                lane.Sections.Add(section);
                lane.LastOffset = lane.CurrOffset;
            }
            else
                lane.Sections.Add(section);
            lane.CurrOffset = MathF.Max(lane.CurrOffset, endOffset * factor);
        }
    }

    private IEnumerable<(string Direction, float Factor)> Coefficients(string direction)
    {
        yield return (direction, 1);
        var axes = _localAxis;
        if (direction is "rgx" or "rgy" or "rgz")
        {
            char excluded = direction[2];
            foreach (char axis in "xyz")
                if (axis != excluded)
                {
                    yield return ($"g{axis}+", 1);
                    yield return ($"g{axis}-", 1);
                }
            if (axes == null) yield break;
            var global = Global(excluded);
            foreach (var (name, local, other) in new[]
                     { ("ly", axes.Y, axes.Z), ("lz", axes.Z, axes.Y) })
            {
                var cross = new Vector3().CrossVectors(other, global);
                if (cross.Length() < 1e-5f) continue;
                float coefficient = MathF.Abs(cross.Normalize().Dot(local));
                if (coefficient > 1e-5f)
                {
                    yield return ($"{name}+", coefficient);
                    yield return ($"{name}-", coefficient);
                }
            }
            yield break;
        }
        if (axes == null) yield break;
        if (direction is "rlx" or "rly" or "rlz" or "R")
        {
            string[] localNames = direction switch
            {
                "rly" => ["lz"], "rlz" => ["ly"], _ => ["ly", "lz"]
            };
            foreach (string name in localNames)
            {
                yield return ($"{name}+", 1);
                yield return ($"{name}-", 1);
            }
            foreach (char axis in "xyz")
            {
                var global = Global(axis);
                var momentAxis = direction switch
                {
                    "rly" => axes.Y, "rlz" => axes.Z, _ => axes.X
                };
                if (direction is "rly" or "rlz" &&
                    MathF.Abs(momentAxis.Dot(global)) > 1e-5f) continue;
                float perpendicular = new Vector3().CrossVectors(axes.X, global).Length();
                if (perpendicular < 1e-5f) continue;
                float factor = 1 / perpendicular;
                yield return ($"g{axis}+", factor);
                yield return ($"g{axis}-", factor);
            }
            yield break;
        }
        if (direction.Length != 3 || direction[2] is not ('+' or '-'))
            yield break;
        char sign = direction[2];
        if (direction.StartsWith('l'))
        {
            var local = direction[1] == 'y' ? axes.Y : axes.Z;
            var other = direction[1] == 'y' ? axes.Z : axes.Y;
            foreach (char axis in "xyz")
            {
                var global = Global(axis);
                float cos = local.Dot(global);
                if (MathF.Abs(other.Dot(global)) > 1e-5f ||
                    MathF.Abs(cos) < 1e-5f) continue;
                char mappedSign = cos > 0 ? sign : Flip(sign);
                yield return ($"g{axis}{mappedSign}",
                    1 / MathF.Abs(cos));
            }
        }
        else if (direction.StartsWith('g'))
        {
            var global = Global(direction[1]);
            foreach (var (name, local, other) in new[]
                     { ("ly", axes.Y, axes.Z), ("lz", axes.Z, axes.Y) })
            {
                float cos = local.Dot(global);
                if (MathF.Abs(other.Dot(global)) > 1e-5f ||
                    MathF.Abs(cos) < 1e-5f) continue;
                char mappedSign = cos > 0 ? sign : Flip(sign);
                yield return ($"{name}{mappedSign}",
                    MathF.Abs(cos));
            }
        }
    }

    private static char Flip(char sign) => sign == '+' ? '-' : '+';
    private static Vector3 Global(char axis) => axis switch
    {
        'x' => new Vector3(1, 0, 0),
        'y' => new Vector3(0, 1, 0),
        _ => new Vector3(0, 0, 1)
    };
}

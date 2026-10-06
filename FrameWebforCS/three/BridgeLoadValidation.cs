using FrameWebforCS.components.input;
using static FrameWebforCS.three.BridgeLoadGeometry;

namespace FrameWebforCS.three;

/// <summary>Validates display geometry without producing equivalent structural forces.</summary>
internal static class BridgeLoadValidation
{
    internal static IReadOnlyList<Strip> Validate(BridgePanel panel,
        IReadOnlyList<BridgePoint[]> structuralTriangles, IReadOnlyList<BridgePoint[]> loadingTriangles,
        IReadOnlyList<BridgePoint[]> holes, IReadOnlyList<BridgePath> paths, BridgeLoad load)
    {
        if (structuralTriangles.Count == 0 || loadingTriangles.Count == 0)
            throw new ArgumentException("荷重伝達面の接続がありません。");
        var vertices = structuralTriangles.SelectMany(t => t).ToArray();
        double scale = Extent(vertices);
        double eps = Math.Max(panel.AbsoluteTolerance, panel.RelativeTolerance * scale);
        if (!double.IsFinite(eps) || eps <= 0 || scale <= eps)
            throw new ArgumentException("橋面の寸法または許容差が不正です。");
        var normal = panel.Plane is { } plane
            ? Unit(Cross(Unit(plane.AxisU), Unit(plane.AxisV))) : new BridgePoint(0, 0, 1);
        var origin = panel.Plane?.Origin ?? vertices[0];
        var geometry = new PlaneGeometry(normal, eps, scale);
        foreach (var point in vertices.Concat(loadingTriangles.SelectMany(t => t))
            .Concat(holes.SelectMany(h => h)).Concat(paths.SelectMany(p => p.Points)))
        {
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(point.Z) ||
                Math.Abs(Dot(Sub(point, origin), normal)) > eps)
                throw new ArgumentException(panel.Plane is null
                    ? "載荷点または面が平面外です。傾斜面には平面原点/U軸/V軸を指定してください。"
                    : "載荷点または面が指定した橋面の平面外です。");
        }
        geometry.ValidatePartition(structuralTriangles, null);
        geometry.ValidatePartition(loadingTriangles, holes);
        foreach (var cell in loadingTriangles)
        {
            double coverage = structuralTriangles.Sum(target => geometry.Area(geometry.Clip(cell, target)));
            if (Math.Abs(coverage - geometry.Area(cell)) > geometry.AreaTolerance)
                throw new ArgumentException("独立載荷メッシュが構造の荷重伝達面外にあります。");
        }
        if (paths.Count is not (1 or 2) || paths.Count != load.PathIds.Length)
            throw new ArgumentException("荷重には1本または2本の載荷ラインが必要です。");
        foreach (var path in paths) geometry.ValidatePath(path.Points, false);
        if (paths.Count == 1)
        {
            var points = paths[0].Points;
            for (int i = 0; i + 1 < points.Length; i++)
                geometry.RequireSegmentCoverage(points[i], points[i + 1], loadingTriangles);
            return [];
        }

        var strips = BuildStrips(paths[0], paths[1], load,
            eps * Extent(paths.SelectMany(path => path.Points).ToArray()));
        var holeTriangles = holes.SelectMany(geometry.Triangulate).ToArray();
        double totalCovered = 0;
        for (int i = 0; i < strips.Count; i++)
        {
            var corners = strips[i].Corners;
            geometry.RequireConvex(corners);
            double covered = loadingTriangles.Sum(cell => geometry.Area(geometry.Clip(corners, cell)));
            double excluded = holeTriangles.Sum(cell => geometry.Area(geometry.Clip(corners, cell)));
            if (Math.Abs(covered + excluded - geometry.Area(corners)) > geometry.AreaTolerance)
                throw new ArgumentException("面荷重が橋面の外周外にあります（凹部を含む）。");
            totalCovered += covered;
            for (int j = 0; j < i; j++)
                if (geometry.Area(geometry.Clip(corners, strips[j].Corners)) > geometry.AreaTolerance)
                    throw new ArgumentException("面荷重の帯が交差または重複しています。");
        }
        if (totalCovered <= 0)
            throw new ArgumentException("面荷重が開口内にあり、橋面との重なりがありません。");
        return strips;
    }

    private static double Extent(IReadOnlyList<BridgePoint> points) => Length(new(
        points.Max(p => p.X) - points.Min(p => p.X),
        points.Max(p => p.Y) - points.Min(p => p.Y),
        points.Max(p => p.Z) - points.Min(p => p.Z)));

    private sealed class PlaneGeometry(BridgePoint normal, double eps, double scale)
    {
        internal double AreaTolerance => eps * scale;
        private double Turn(BridgePoint a, BridgePoint b, BridgePoint c) =>
            Dot(Cross(Sub(b, a), Sub(c, a)), normal);
        private double SignedArea(IReadOnlyList<BridgePoint> polygon) => polygon.Count < 3 ? 0 :
            Enumerable.Range(1, polygon.Count - 2).Sum(i => Turn(polygon[0], polygon[i], polygon[i + 1])) / 2;
        internal double Area(IReadOnlyList<BridgePoint> polygon) => Math.Abs(SignedArea(polygon));
        private double SideTolerance(BridgePoint a, BridgePoint b) => eps * Length(Sub(b, a));
        private bool OnSegment(BridgePoint p, BridgePoint a, BridgePoint b)
        {
            var edge = Sub(b, a); double length = Length(edge);
            if (length <= eps) return Length(Sub(p, a)) <= eps;
            double position = Dot(Sub(p, a), edge);
            return Math.Abs(Turn(a, b, p)) <= eps * length &&
                position >= -eps * length && position <= length * length + eps * length;
        }

        private bool Intersects(BridgePoint a, BridgePoint b, BridgePoint c, BridgePoint d)
        {
            double abC = Turn(a, b, c), abD = Turn(a, b, d);
            double cdA = Turn(c, d, a), cdB = Turn(c, d, b);
            if (OnSegment(a, c, d) || OnSegment(b, c, d) || OnSegment(c, a, b) || OnSegment(d, a, b)) return true;
            return Math.Sign(abC) != Math.Sign(abD) && Math.Sign(cdA) != Math.Sign(cdB);
        }

        internal void ValidatePath(IReadOnlyList<BridgePoint> points, bool closed)
        {
            if (points.Count < (closed ? 3 : 2)) throw new ArgumentException("載荷形状の点が不足しています。");
            int segments = closed ? points.Count : points.Count - 1;
            for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                    if (Length(Sub(points[i], points[j])) <= eps)
                        throw new ArgumentException("載荷形状に重複点があります。");
            for (int i = 0; i < segments; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Count];
                if (closed || i + 2 < points.Count)
                {
                    var c = points[(i + 2) % points.Count];
                    if (Math.Abs(Turn(a, b, c)) <= eps * Math.Max(Length(Sub(b, a)), Length(Sub(c, b))) &&
                        Dot(Sub(b, a), Sub(c, b)) < 0)
                        throw new ArgumentException("載荷形状の隣接辺が重なっています。");
                }
                for (int j = i + 2; j < segments; j++)
                {
                    if (closed && i == 0 && j == segments - 1) continue;
                    if (Intersects(a, b, points[j], points[(j + 1) % points.Count]))
                        throw new ArgumentException("載荷形状が自己交差しています。");
                }
            }
        }

        internal void RequireConvex(IReadOnlyList<BridgePoint> polygon)
        {
            ValidatePath(polygon, true);
            int sign = Math.Sign(SignedArea(polygon));
            if (sign == 0 || Area(polygon) <= eps * Extent(polygon))
                throw new ArgumentException("載荷面の面積がゼロです。");
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count]; var c = polygon[(i + 2) % polygon.Count];
                if (sign * Turn(a, b, c) <= eps * Math.Max(Length(Sub(b, a)), Length(Sub(c, b))))
                    throw new ArgumentException("載荷面の各帯は凸四角形である必要があります。");
            }
        }

        internal BridgePoint[] Clip(IReadOnlyList<BridgePoint> subject, IReadOnlyList<BridgePoint> clip)
        {
            int sign = Math.Sign(SignedArea(clip));
            var polygon = subject.ToList();
            for (int edge = 0; edge < clip.Count && polygon.Count > 0; edge++)
            {
                var a = clip[edge]; var b = clip[(edge + 1) % clip.Count];
                double tolerance = SideTolerance(a, b);
                var input = polygon; polygon = [];
                var previous = input[^1]; double previousSide = sign * Turn(a, b, previous);
                foreach (var current in input)
                {
                    double side = sign * Turn(a, b, current);
                    if ((side >= -tolerance) != (previousSide >= -tolerance))
                    {
                        double denominator = previousSide - side;
                        if (denominator != 0) polygon.Add(Lerp(previous, current, Math.Clamp(previousSide / denominator, 0, 1)));
                    }
                    if (side >= -tolerance) polygon.Add(current);
                    previous = current; previousSide = side;
                }
            }
            return polygon.ToArray();
        }

        internal void RequireSegmentCoverage(BridgePoint a, BridgePoint b, IReadOnlyList<BridgePoint[]> cells)
        {
            var intervals = new List<(double Start, double End)>();
            foreach (var cell in cells)
            {
                double start = 0, end = 1; int sign = Math.Sign(SignedArea(cell));
                for (int edge = 0; edge < cell.Length; edge++)
                {
                    var p = cell[edge]; var q = cell[(edge + 1) % cell.Length];
                    double sa = sign * Turn(p, q, a), sb = sign * Turn(p, q, b), tolerance = SideTolerance(p, q);
                    if (sa < -tolerance && sb < -tolerance) { end = -1; break; }
                    if (sa < -tolerance) start = Math.Max(start, sa / (sa - sb));
                    if (sb < -tolerance) end = Math.Min(end, sa / (sa - sb));
                }
                if (end >= start) intervals.Add((start, end));
            }
            double covered = 0, parameterTolerance = eps / Length(Sub(b, a));
            foreach (var interval in intervals.OrderBy(value => value.Start))
            {
                if (interval.Start > covered + parameterTolerance) break;
                covered = Math.Max(covered, interval.End);
            }
            if (covered < 1 - parameterTolerance)
                throw new ArgumentException("載荷ラインが橋面外または開口を横切っています。");
        }

        internal IReadOnlyList<BridgePoint[]> Triangulate(BridgePoint[] polygon)
        {
            ValidatePath(polygon, true);
            int sign = Math.Sign(SignedArea(polygon));
            var remaining = polygon.ToList(); var triangles = new List<BridgePoint[]>();
            while (remaining.Count > 3)
            {
                bool removed = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    var a = remaining[(i + remaining.Count - 1) % remaining.Count];
                    var b = remaining[i]; var c = remaining[(i + 1) % remaining.Count];
                    if (OnSegment(b, a, c)) { remaining.RemoveAt(i); removed = true; break; }
                    if (sign * Turn(a, b, c) <= eps * Math.Max(Length(Sub(b, a)), Length(Sub(c, b)))) continue;
                    if (remaining.Any(p => p != a && p != b && p != c &&
                        sign * Turn(a, b, p) >= -SideTolerance(a, b) &&
                        sign * Turn(b, c, p) >= -SideTolerance(b, c) &&
                        sign * Turn(c, a, p) >= -SideTolerance(c, a))) continue;
                    triangles.Add([a, b, c]); remaining.RemoveAt(i); removed = true; break;
                }
                if (!removed) throw new ArgumentException("開口の三角形分割ができません。");
            }
            triangles.Add(remaining.ToArray());
            return triangles;
        }

        internal void ValidatePartition(IReadOnlyList<BridgePoint[]> cells, IReadOnlyList<BridgePoint[]>? declaredHoles)
        {
            var edges = new Dictionary<(BridgePoint, BridgePoint), int>();
            int sign = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Length != 3) throw new ArgumentException("伝達面は三角形に分割してください。");
                RequireConvex(cells[i]);
                int orientation = Math.Sign(SignedArea(cells[i]));
                if (sign != 0 && orientation != sign) throw new ArgumentException("伝達三角形の向きが一致していません。");
                sign = orientation;
                for (int edge = 0; edge < 3; edge++)
                {
                    var key = (cells[i][edge], cells[i][(edge + 1) % 3]);
                    if (!edges.TryAdd(key, i)) throw new ArgumentException("伝達面の辺が重複しています。");
                }
                for (int j = 0; j < i; j++)
                    if (Area(Clip(cells[i], cells[j])) > AreaTolerance)
                        throw new ArgumentException("伝達面の三角形が重なっています。");
            }
            var adjacency = Enumerable.Range(0, cells.Count).Select(_ => new List<int>()).ToArray();
            var boundary = new Dictionary<BridgePoint, BridgePoint>();
            foreach (var (edge, index) in edges)
                if (edges.TryGetValue((edge.Item2, edge.Item1), out int other)) adjacency[index].Add(other);
                else if (!boundary.TryAdd(edge.Item1, edge.Item2)) throw new ArgumentException("伝達面の境界が分岐しています。");
            var seen = new HashSet<int>(); var pending = new Stack<int>(); pending.Push(0);
            while (pending.TryPop(out int index))
                if (seen.Add(index)) foreach (int other in adjacency[index]) pending.Push(other);
            if (seen.Count != cells.Count) throw new ArgumentException("伝達面が連続していません。");
            var rings = new List<BridgePoint[]>();
            while (boundary.Count > 0)
            {
                var first = boundary.Keys.First(); var current = first; var ring = new List<BridgePoint>();
                do
                {
                    ring.Add(current);
                    if (!boundary.Remove(current, out var next)) throw new ArgumentException("伝達面の境界が閉じていません。");
                    current = next;
                } while (current != first);
                ValidatePath(ring, true); rings.Add(ring.ToArray());
            }
            if (rings.Count(ring => Math.Sign(SignedArea(ring)) == sign) != 1)
                throw new ArgumentException("伝達面の外周が一つではありません。");
            var inner = rings.Where(ring => Math.Sign(SignedArea(ring)) != sign).ToArray();
            if (declaredHoles != null && (inner.Length != declaredHoles.Count ||
                inner.Any(ring => !declaredHoles.Any(hole => SameRing(ring, hole)))))
                throw new ArgumentException("開口定義が伝達面の接続または節点順と一致しません。");
        }

        private static bool SameRing(BridgePoint[] first, BridgePoint[] second) =>
            first.Length == second.Length && Enumerable.Range(0, second.Length).Any(start =>
                Enumerable.Range(0, first.Length).All(i => first[i] == second[(start + i) % second.Length]));
    }
}

using THREE;

namespace FrameWebforCS.three;

/// <summary>Distance extrema used by the legacy node and displacement scales.</summary>
internal readonly record struct NodeDistanceExtrema(
    double MinDistance, double MaxDistance, long DistanceEvaluations)
{
    // Small models retain the exact JS all-pairs result. For larger models,
    // six coordinate extrema bound the diameter estimate and an X-sorted
    // eight-neighbor window estimates the nearest distinct pair. Both estimates
    // are deterministic; the distance work is bounded by 14 * node count.
    internal const int ExactNodeLimit = 2_048;
    private const int NearestWindow = 8;

    internal static NodeDistanceExtrema Find(IReadOnlyList<Vector3> points)
    {
        if (points.Count < 2) return new(double.PositiveInfinity, 0, 0);
        return points.Count <= ExactNodeLimit ? FindExact(points) : FindBounded(points);
    }

    private static NodeDistanceExtrema FindExact(IReadOnlyList<Vector3> points)
    {
        double minSquared = double.PositiveInfinity, maxSquared = 0;
        long evaluations = 0;
        for (int i = 0; i < points.Count; i++)
            for (int j = i + 1; j < points.Count; j++)
            {
                double squared = SquaredDistance(points[i], points[j]);
                evaluations++;
                if (squared == 0) continue;
                minSquared = Math.Min(minSquared, squared);
                maxSquared = Math.Max(maxSquared, squared);
            }
        return new(Math.Sqrt(minSquared), Math.Sqrt(maxSquared), evaluations);
    }

    private static NodeDistanceExtrema FindBounded(IReadOnlyList<Vector3> points)
    {
        int[] extremes = new int[6];
        for (int i = 1; i < points.Count; i++)
        {
            var point = points[i];
            if (point.X < points[extremes[0]].X) extremes[0] = i;
            if (point.X > points[extremes[1]].X) extremes[1] = i;
            if (point.Y < points[extremes[2]].Y) extremes[2] = i;
            if (point.Y > points[extremes[3]].Y) extremes[3] = i;
            if (point.Z < points[extremes[4]].Z) extremes[4] = i;
            if (point.Z > points[extremes[5]].Z) extremes[5] = i;
        }

        double maxSquared = 0;
        long evaluations = 0;
        foreach (int anchor in extremes.Distinct())
            foreach (var point in points)
            {
                maxSquared = Math.Max(maxSquared, SquaredDistance(points[anchor], point));
                evaluations++;
            }

        // Remove coincident positions before applying the local window. JS ignores
        // zero-distance pairs, and long duplicate runs must not hide neighbors.
        var ordered = points.Select(point => (point.X, point.Y, point.Z))
            .Distinct()
            .OrderBy(point => point.X).ThenBy(point => point.Y).ThenBy(point => point.Z)
            .ToArray();
        double minSquared = double.PositiveInfinity;
        for (int i = 0; i < ordered.Length; i++)
            for (int j = i + 1; j < Math.Min(i + NearestWindow + 1, ordered.Length); j++)
            {
                var a = ordered[i];
                var b = ordered[j];
                double dx = (double)a.X - b.X, dy = (double)a.Y - b.Y, dz = (double)a.Z - b.Z;
                minSquared = Math.Min(minSquared, dx * dx + dy * dy + dz * dz);
                evaluations++;
            }
        return new(Math.Sqrt(minSquared), Math.Sqrt(maxSquared), evaluations);
    }

    private static double SquaredDistance(Vector3 a, Vector3 b)
    {
        double dx = (double)a.X - b.X, dy = (double)a.Y - b.Y, dz = (double)a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}

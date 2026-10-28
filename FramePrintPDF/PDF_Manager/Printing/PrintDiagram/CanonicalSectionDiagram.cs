using Newtonsoft.Json.Linq;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PDF_Manager.Printing
{
    /// <summary>
    /// Private desktop diagram boundary. Stations are distances along original members;
    /// identifiers and force vectors remain the same objects used by the report tables.
    /// </summary>
    internal sealed partial class CanonicalSectionDiagram
    {
        private readonly JObject settings;
        private readonly string[] outputs;
        private readonly double labelScale;
        private readonly bool yUp;
        private readonly JObject displacementStations;
        private readonly HashSet<string> displacementLabelNodes;
        private const int CurveDivisions = 20;

        internal static bool Enabled(Dictionary<string, object> settings) =>
            settings.TryGetValue("canonicalStations", out var value) && Convert.ToBoolean(value);

        internal CanonicalSectionDiagram(Dictionary<string, object> value, Dictionary<string, object> root)
        {
            settings = JObject.FromObject(value);
            outputs = settings["output"]?.Values<string>().ToArray() ?? new[] { "mz", "fy", "fx" };
            labelScale = settings["displacementLabelScale"]?.Value<double>() ?? 1000;
            yUp = settings["yUp"]?.Value<bool>() ?? true;
            displacementStations = settings["displacementStations"] as JObject;
            displacementLabelNodes = settings["displacementLabelNodeIds"] is JArray selectedNodes
                ? new HashSet<string>(selectedNodes.Values<string>(), StringComparer.Ordinal) : null;
            if (!Finite(labelScale) || labelScale <= 0 || outputs.Length == 0 ||
                outputs.Any(output => !new[] { "mz", "fy", "fx", "disg" }.Contains(output)))
                throw new InvalidOperationException("Invalid canonical diagram settings.");
            ReadFrameGSettings(root);
        }

        // Canonical diagrams are 2D only (FrameWebforCS); they use FrameG's layout (原宿ボックス 0404).
        internal void Print(PdfDocument document, PrintData data, ref int pageIndex) =>
            PrintFrameG(document, data, ref pageIndex);

        private static IEnumerable<DiagramCase> Cases(PrintData data)
        {
            var displacements = (ResultDisg)data.printDatas[ResultDisg.KEY];
            foreach (var pair in ((ResultFsec)data.printDatas[ResultFsec.KEY]).fsecs)
            {
                displacements.disgs.TryGetValue(pair.Key, out var displacement);
                yield return new DiagramCase(pair.Key, pair.Value as List<Fsec>,
                    pair.Value is Dictionary<string, object> envelope ? ResultFsecCombine.getFsecCombine(envelope) : null,
                    DisplacementPaths(displacement), ResultFsec.KEY);
            }
            foreach (string family in new[] { ResultFsecCombine.KEY, ResultFsecPickup.KEY })
                foreach (var pair in ((ResultFsecCombine)data.printDatas[family]).Fsecs)
                    yield return new DiagramCase(pair.Key, null, pair.Value, null, family);
        }

        private static List<List<Disg>> DisplacementPaths(object value)
        {
            if (value is List<Disg> rows) return new List<List<Disg>> { rows };
            if (!(value is Dictionary<string, object> envelope)) return null;
            var paths = new List<List<Disg>>();
            foreach (var mode in envelope)
            {
                var path = new List<Disg>();
                foreach (var pair in JObject.FromObject(mode.Value).Properties())
                {
                    var row = (JObject)pair.Value;
                    path.Add(new Disg { id = row["id"]?.Value<string>() ?? pair.Name,
                        dx = row["dx"].Value<double>(), dy = row["dy"].Value<double>(),
                        dz = row["dz"]?.Value<double>() ?? 0,
                        rx = row["rx"]?.Value<double>() ?? 0, ry = row["ry"]?.Value<double>() ?? 0,
                        rz = row["rz"].Value<double>(), caseStr = row["case"]?.Value<string>() });
                }
                paths.Add(path);
            }
            return paths;
        }

        private static IEnumerable<List<Fsec>> Paths(DiagramCase item, string component)
        {
            if (item.Rows != null) { yield return DiagramResult.DrawingRows(item.Rows); yield break; }
            var envelope = item.Envelope;
            if (envelope == null) yield break;
            // Keep each envelope side separate, including a single-member envelope.
            yield return DiagramResult.DrawingRows(component == "mz" ? envelope.mz_min :
                component == "fy" ? envelope.fy_min : envelope.fx_min);
            yield return DiagramResult.DrawingRows(component == "mz" ? envelope.mz_max :
                component == "fy" ? envelope.fy_max : envelope.fx_max);
        }

        private static IEnumerable<List<Fsec>> MemberRuns(List<Fsec> rows)
        {
            var run = new List<Fsec>();
            foreach (Fsec row in rows)
            {
                if (run.Count > 0 && (row.m != run[0].m || row.l < run[run.Count - 1].l))
                { yield return run; run = new List<Fsec>(); }
                run.Add(row);
            }
            if (run.Count > 0) yield return run;
        }

        private List<Tuple<string, double>> DisplacementStations(MemberGeometry member)
        {
            if (displacementStations == null)
                return new List<Tuple<string, double>> { Tuple.Create(member.NodeI, 0d), Tuple.Create(member.NodeJ, member.Length) };
            var rows = displacementStations[member.Id] as JArray;
            if (rows == null) throw new InvalidOperationException("Missing canonical displacement geometry for member: " + member.Id);
            var stations = rows.Select(row => Tuple.Create(row["nodeId"]?.Value<string>(), row["position"].Value<double>())).ToList();
            double tolerance = 1e-10 * Math.Max(1, member.Length);
            if (stations.Count < 2 || stations.Any(station => string.IsNullOrEmpty(station.Item1) || !Finite(station.Item2)) ||
                stations[0].Item1 != member.NodeI || stations[stations.Count - 1].Item1 != member.NodeJ ||
                Math.Abs(stations[0].Item2) > tolerance || Math.Abs(stations[stations.Count - 1].Item2 - member.Length) > tolerance ||
                stations.Zip(stations.Skip(1), (first, last) => last.Item2 > first.Item2).Any(increasing => !increasing))
                throw new InvalidOperationException("Invalid canonical displacement geometry for member: " + member.Id);
            return stations;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static double Hermite(double start, double end, double slopeStart, double slopeEnd, double length, double t)
        {
            double t2 = t * t, t3 = t2 * t;
            return (2 * t3 - 3 * t2 + 1) * start + (t3 - 2 * t2 + t) * length * slopeStart +
                (-2 * t3 + 3 * t2) * end + (t3 - t2) * length * slopeEnd;
        }

        private static IEnumerable<double> HermiteExtrema(double start, double end,
            double slopeStart, double slopeEnd, double length)
        {
            double a = 3 * (2 * start - 2 * end + length * (slopeStart + slopeEnd));
            double b = 2 * (-3 * start + 3 * end - length * (2 * slopeStart + slopeEnd));
            double c = length * slopeStart;
            if (a == 0)
            {
                if (b != 0 && -c / b > 0 && -c / b < 1) yield return -c / b;
                yield break;
            }
            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0 || !Finite(discriminant)) yield break;
            // Stable quadratic roots avoid cancellation when a cubic is almost quadratic.
            double q = -.5 * (b + (b < 0 ? -1 : 1) * Math.Sqrt(discriminant));
            double first = q / a;
            double second = q == 0 ? -b / (2 * a) : c / q;
            if (first > 0 && first < 1) yield return first;
            if (second > 0 && second < 1 && second != first) yield return second;
        }

        private sealed class DiagramCase
        {
            internal readonly string Id;
            internal readonly List<Fsec> Rows;
            internal readonly FsecCombine Envelope;
            internal readonly List<List<Disg>> Displacements;
            internal readonly string Family;
            internal DiagramCase(string id, List<Fsec> rows, FsecCombine envelope, List<List<Disg>> displacements,
                string family)
            { Id = id; Rows = rows; Envelope = envelope; Displacements = displacements; Family = family; }
        }

        private sealed class MemberGeometry
        {
            internal readonly string Id, NodeI, NodeJ;
            internal readonly double X, Y, EndX, EndY, Length, Ux, Uy;
            internal MemberGeometry(string id, Member member, Dictionary<string, Vector3> nodes)
            {
                Id = id; NodeI = member.ni; NodeJ = member.nj;
                if (!nodes.TryGetValue(NodeI, out var start) || !nodes.TryGetValue(NodeJ, out var end))
                    throw new InvalidOperationException("Missing node on diagram member: " + id);
                X = start.x; Y = start.y; EndX = end.x; EndY = end.y;
                Length = Math.Sqrt(Math.Pow(EndX - X, 2) + Math.Pow(EndY - Y, 2));
                if (!Finite(Length) || Length <= 0) throw new InvalidOperationException("Invalid diagram member length: " + id);
                Ux = (EndX - X) / Length; Uy = (EndY - Y) / Length;
            }
        }

        private sealed class PlotTransform
        {
            private readonly double centerX, centerY, originX, originY, scaleX, scaleY, ySign;
            // FrameG layout: the frame's centre at a given page point with given scales (pt per model unit).
            internal PlotTransform(IEnumerable<Vector3> source, XPoint center, double scaleX, double scaleY, bool yUp)
            {
                Vector3[] nodes = source.ToArray();
                centerX = (nodes.Min(node => node.x) + nodes.Max(node => node.x)) / 2;
                centerY = (nodes.Min(node => node.y) + nodes.Max(node => node.y)) / 2;
                originX = center.X; originY = center.Y;
                this.scaleX = scaleX; this.scaleY = scaleY;
                if (!Finite(scaleX) || !Finite(scaleY) || scaleX <= 0 || scaleY <= 0)
                    throw new InvalidOperationException("Invalid canonical diagram scale.");
                ySign = yUp ? -1 : 1;
            }
            internal XPoint Point(double x, double y) => new XPoint(originX + (x - centerX) * scaleX,
                originY + (y - centerY) * scaleY * ySign);
            internal XPoint Normal(MemberGeometry member)
            {
                double x = -member.Uy * scaleY, y = member.Ux * scaleX * ySign;
                double length = Math.Sqrt(x * x + y * y);
                return new XPoint(x / length, y / length);
            }
        }

        private sealed class ForcePoint
        {
            internal readonly double Distance, Value;
            internal readonly bool End;
            internal ForcePoint(double distance, double value, bool end = false) { Distance = distance; Value = value; End = end; }
        }

        private sealed class ForceCurve
        {
            internal readonly MemberGeometry Member;
            internal readonly List<ForcePoint> Samples = new List<ForcePoint>();
            internal readonly List<ForcePoint> Labels = new List<ForcePoint>();
            internal ForceCurve(MemberGeometry member, List<Fsec> rows, string component, bool basic)
            {
                Member = member;
                for (int index = 0; index < rows.Count; index++)
                {
                    Fsec row = rows[index];
                    double value = row.getValue2D(component);
                    var point = new ForcePoint(row.l, value, index == 0 || index == rows.Count - 1);
                    if (index > 0)
                    {
                        Fsec previous = rows[index - 1];
                        double length = row.l - previous.l;
                        // Endpoint shear is dM/ds. An envelope can use these slopes only
                        // when the complete source case and combination agree at both ends.
                        bool sameSource = (!string.IsNullOrEmpty(previous.caseStr) || !string.IsNullOrEmpty(previous.comb)) &&
                            previous.caseStr == row.caseStr && previous.comb == row.comb;
                        // Derived table rows may have lost the other side of a point-load
                        // station. Only a linear-shear interval with consistent moment
                        // change proves these are usable endpoint slopes. The relative
                        // tolerance admits floating/serialization roundoff, with a one-unit
                        // floor, and never changes an input ordinate or derivative.
                        double shearIntegral = (previous.fy + row.fy) * length / 2;
                        double consistencyScale = Math.Max(1, Math.Max(Math.Abs(previous.mz),
                            Math.Max(Math.Abs(row.mz), Math.Abs(shearIntegral))));
                        bool consistentSlopes = Finite(shearIntegral) &&
                            Math.Abs(row.mz - previous.mz - shearIntegral) <= 1e-8 * consistencyScale;
                        if (component == "mz" && (basic || sameSource && consistentSlopes) && length > 0 &&
                            Finite(previous.fy) && Finite(row.fy))
                        {
                            var samples = Enumerable.Range(1, CurveDivisions - 1).Select(step => (double)step / CurveDivisions)
                                .Concat(HermiteExtrema(previous.mz, row.mz, previous.fy, row.fy, length)).Distinct().OrderBy(t => t);
                            foreach (double t in samples)
                            {
                                Samples.Add(new ForcePoint(previous.l + length * t,
                                    Hermite(previous.mz, row.mz, previous.fy, row.fy, length, t)));
                            }
                        }
                    }
                    Samples.Add(point);
                    if (!row.dummy && point.End) Labels.Add(point);
                }
                // The governing maximum and minimum carry the engineering information.
                // Labelling every tiny local oscillation floods long spring-supported members.
                // Keep small real extrema without an amplitude cutoff.
                ForcePoint minimum = Samples.OrderBy(point => point.Value).First();
                ForcePoint maximum = Samples.OrderByDescending(point => point.Value).First();
                if (!minimum.End) Labels.Add(minimum);
                if (!maximum.End) Labels.Add(maximum);
                // Retain the governing solved station values as well as between-station
                // interpolation extrema; they are independent evidence at a load corner.
                foreach (Fsec station in new[] { rows.OrderBy(row => row.getValue2D(component)).First(),
                    rows.OrderByDescending(row => row.getValue2D(component)).First() })
                    if (!station.dummy && station != rows[0] && station != rows[rows.Count - 1])
                        Labels.Add(new ForcePoint(station.l, station.getValue2D(component)));
            }
        }

        private sealed class DisplacementPoint
        {
            internal readonly double Distance, Dx, Dy;
            internal readonly string NodeId;
            internal double Magnitude => Math.Sqrt(Dx * Dx + Dy * Dy);
            internal DisplacementPoint(double distance, double dx, double dy, string nodeId = null)
            { Distance = distance; Dx = dx; Dy = dy; NodeId = nodeId; }
        }
    }
}

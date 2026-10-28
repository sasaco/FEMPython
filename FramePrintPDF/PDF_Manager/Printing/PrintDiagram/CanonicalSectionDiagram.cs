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
        private readonly Dictionary<string, List<double>> notices = new Dictionary<string, List<double>>();
        private const double LabelSize = 8;
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
            if (root.TryGetValue("notice_points", out var rawNotices))
                foreach (JObject row in JArray.FromObject(rawNotices))
                {
                    string member = row["m"]?.Value<string>();
                    if (member == null) continue;
                    if (!notices.TryGetValue(member, out var positions)) notices.Add(member, positions = new List<double>());
                    if (row["Points"] is JArray points) positions.AddRange(points.Values<double>());
                }
            frameGLayout = settings["referenceLayout"]?.Value<bool>() ?? false;
            if (frameGLayout) ReadFrameGSettings(root);
        }

        internal void Print(PdfDocument document, PrintData data, ref int pageIndex)
        {
            if (frameGLayout)
            {
                PrintFrameG(document, data, ref pageIndex);
                return;
            }
            var nodes = ((InputNode)data.printDatas[InputNode.KEY]).Nodes;
            var members = ((InputMember)data.printDatas[InputMember.KEY]).members;
            if (nodes.Count == 0 || members.Count == 0)
                throw new InvalidOperationException("A canonical diagram requires original nodes and members.");
            var geometry = members.ToDictionary(pair => pair.Key,
                pair => new MemberGeometry(pair.Key, pair.Value, nodes));
            var cases = Cases(data).ToArray();
            string layout = settings["layout"]?.Value<string>() ?? "single";
            bool vertical = string.Equals(layout, "splitVertical", StringComparison.OrdinalIgnoreCase);
            int panels = vertical || string.Equals(layout, "splitHorizontal", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            // A one-component selection can use the whole page, including an explicit model scale.
            panels = Math.Min(panels, outputs.Length);
            bool firstPage = true;
            foreach (var item in cases)
            {
                for (int first = 0; first < outputs.Length; first += panels)
                {
                    document.CheckBudget();
                    document.TitleArea = new XPoint();
                    if (!firstPage) document.NewPage(ref pageIndex);
                    firstPage = false;
                    string caption = settings["caseLabels"]?[item.Id]?.Value<string>() ?? item.Id;
                    DrawCaption(document, caption);
                    for (int slot = 0; slot < panels && first + slot < outputs.Length; slot++)
                    {
                        string component = outputs[first + slot];
                        var panel = Panel(document, panels, slot, vertical);
                        DrawHeading(document, data.language, component, panel);
                        var transform = new PlotTransform(nodes.Values, panel, settings, labelScale, yUp);
                        foreach (MemberGeometry member in geometry.Values)
                            document.gfx.DrawLine(new XPen(XColors.Black, .55), transform.Point(member.X, member.Y),
                                transform.Point(member.EndX, member.EndY));
                        var labels = new LabelPlacement(document, panel);
                        if (component == "disg") DrawDisplacements(document, item, geometry, transform, labels);
                        else DrawForces(document, item, component, geometry, transform, labels);
                    }
                }
            }
        }

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

        private static void DrawCaption(PdfDocument document, string caption)
        {
            var font = new XFont("MS Gothic", 10);
            double left = document.Margine.Left + 45;
            double width = document.currentPage.Width.Point - left - document.Margine.Right;
            if (document.gfx.MeasureString(caption, font).Width > width)
                throw new InvalidOperationException("The diagram case caption is too long for the page.");
            document.gfx.DrawString(caption, font, XBrushes.Blue, new XPoint(left, 113));
        }

        private static XRect Panel(PdfDocument document, int count, int index, bool vertical)
        {
            double left = document.Margine.Left + 25, top = 124;
            double width = document.currentPageSize.Width - 50;
            double height = document.currentPage.Height.Point - top - 28;
            return vertical ? new XRect(left + index * width / count, top, width / count, height)
                : new XRect(left, top + index * height / count, width, height / count);
        }

        private static void DrawHeading(PdfDocument document, string language, string component, XRect panel)
        {
            string title = language == "en" ? (component == "mz" ? "Bending moment" :
                component == "fy" ? "Shear force" : component == "fx" ? "Axial force" : "Displacement (mm)") :
                component == "mz" ? "曲げモーメント図" : component == "fy" ? "せん断力図" :
                component == "fx" ? "軸方向力図" : "変位図 (mm)";
            document.gfx.DrawString(title, new XFont("MS Gothic", 10), XBrushes.Blue,
                new XPoint(panel.Left + 30, panel.Top + 13));
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

        private void DrawForces(PdfDocument document, DiagramCase item, string component,
            Dictionary<string, MemberGeometry> geometry, PlotTransform transform, LabelPlacement labels)
        {
            var curves = new List<ForceCurve>();
            foreach (List<Fsec> path in Paths(item, component))
            foreach (List<Fsec> run in MemberRuns(path))
            {
                if (!geometry.TryGetValue(run[0].m, out var member))
                    throw new InvalidOperationException("Unknown diagram member: " + run[0].m);
                foreach (Fsec row in run)
                    if (!Finite(row.l) || row.l < -1e-9 * member.Length || row.l > member.Length * (1 + 1e-9) ||
                        !Finite(row.getValue2D(component)))
                        throw new InvalidOperationException("Invalid force station on diagram member: " + run[0].m);
                notices.TryGetValue(member.Id, out var noticePositions);
                curves.Add(new ForceCurve(member, run, component, item.Rows != null, noticePositions));
            }
            if (curves.SelectMany(curve => curve.Samples).Any(point => !Finite(point.Value)))
                throw new InvalidOperationException("Nonfinite canonical force interpolation.");
            double maximum = curves.SelectMany(curve => curve.Samples).Select(point => Math.Abs(point.Value)).DefaultIfEmpty(0).Max();
            double scale = maximum == 0 ? 0 : 42 / maximum;
            var pen = new XPen(XColors.Black, .6);
            var candidates = new List<Tuple<ForceCurve, ForcePoint>>();
            foreach (ForceCurve curve in curves)
            {
                document.CheckBudget();
                var normal = transform.Normal(curve.Member);
                double side = Side(component);
                XPoint Location(ForcePoint point)
                {
                    XPoint origin = transform.Point(curve.Member.X + curve.Member.Ux * point.Distance,
                        curve.Member.Y + curve.Member.Uy * point.Distance);
                    return new XPoint(origin.X + normal.X * point.Value * side * scale,
                        origin.Y + normal.Y * point.Value * side * scale);
                }
                XPoint[] points = curve.Samples.Select(Location).ToArray();
                if (points.Length > 1 && maximum > 0) document.gfx.DrawLines(pen, points);
                // End ordinates belong to the geometry, even when another envelope side
                // supplies the governing label at the same physical station.
                foreach (ForcePoint point in curve.Samples.Where(point => point.End))
                {
                    if (point.Value == 0) continue;
                    XPoint at = Location(point);
                    XPoint origin = transform.Point(curve.Member.X + curve.Member.Ux * point.Distance,
                        curve.Member.Y + curve.Member.Uy * point.Distance);
                    document.gfx.DrawLine(pen, origin, at);
                }
                foreach (ForcePoint point in curve.Labels)
                {
                    int existing = candidates.FindIndex(candidate => candidate.Item1.Member.Id == curve.Member.Id &&
                        Math.Abs(candidate.Item2.Distance - point.Distance) <= 1e-10 * Math.Max(1, curve.Member.Length));
                    if (existing < 0) candidates.Add(Tuple.Create(curve, point));
                    else if (Math.Abs(point.Value) > Math.Abs(candidates[existing].Item2.Value))
                        candidates[existing] = Tuple.Create(curve, point);
                }
            }
            foreach (var candidate in candidates)
            {
                MemberGeometry member = candidate.Item1.Member;
                ForcePoint point = candidate.Item2;
                XPoint normal = transform.Normal(member);
                XPoint origin = transform.Point(member.X + member.Ux * point.Distance, member.Y + member.Uy * point.Distance);
                double ordinate = point.Value * Side(component) * scale;
                labels.Add(new XPoint(origin.X + normal.X * ordinate, origin.Y + normal.Y * ordinate),
                    component == "fx" ? point.Value : Math.Abs(point.Value), normal);
            }
        }

        // Values arrive with FrameG's printed signs (M = raw mz, S = -raw fy, N = raw fx). M and S draw the
        // raw value on the member's local +y side, as FrameG does; N keeps its earlier -y side (FrameG draws
        // a closed frame's axial force inside it, which is not a local-axis rule).
        private static double Side(string component) => component == "mz" ? 1 : -1;

        private void DrawDisplacements(PdfDocument document, DiagramCase item,
            Dictionary<string, MemberGeometry> geometry, PlotTransform transform, LabelPlacement labels)
        {
            if (item.Displacements == null)
                throw new InvalidOperationException("Canonical displacement geometry is unavailable for " + item.Id + ".");
            IEnumerable<MemberGeometry> selectedMembers = geometry.Values;
            if (displacementStations != null)
            {
                if (!displacementStations.HasValues)
                    throw new InvalidOperationException("Canonical displacement geometry has no selected members.");
                selectedMembers = displacementStations.Properties().Select(property =>
                    geometry.TryGetValue(property.Name, out var member) ? member :
                    throw new InvalidOperationException("Unknown canonical displacement member: " + property.Name)).ToArray();
            }
            var curves = new List<Tuple<MemberGeometry, List<DisplacementPoint>>>();
            foreach (List<Disg> path in item.Displacements)
            {
                var values = path.ToDictionary(row => row.id);
                foreach (MemberGeometry member in selectedMembers)
                {
                    var stations = DisplacementStations(member);
                    var points = new List<DisplacementPoint>();
                    for (int segment = 1; segment < stations.Count; segment++)
                    {
                        var first = stations[segment - 1]; var last = stations[segment];
                        if (!values.TryGetValue(first.Item1, out var start) || !values.TryGetValue(last.Item1, out var end))
                            throw new InvalidOperationException("Missing canonical displacement station on member: " + member.Id);
                        double length = last.Item2 - first.Item2;
                        double ui = member.Ux * start.dx + member.Uy * start.dy;
                        double uj = member.Ux * end.dx + member.Uy * end.dy;
                        double vi = -member.Uy * start.dx + member.Ux * start.dy;
                        double vj = -member.Uy * end.dx + member.Ux * end.dy;
                        for (int i = segment == 1 ? 0 : 1; i <= CurveDivisions; i++)
                        {
                            double t = (double)i / CurveDivisions;
                            double u = ui * (1 - t) + uj * t;
                            // Different envelope source states cannot provide the slopes
                            // of one physical deflection curve across this segment.
                            double v = start.caseStr == end.caseStr ? Hermite(vi, vj, start.rz, end.rz, length, t)
                                : vi * (1 - t) + vj * t;
                            points.Add(new DisplacementPoint(first.Item2 + t * length,
                                member.Ux * u - member.Uy * v, member.Uy * u + member.Ux * v,
                                i == 0 ? first.Item1 : i == CurveDivisions ? last.Item1 : null));
                        }
                    }
                    curves.Add(Tuple.Create(member, points));
                }
            }
            if (curves.SelectMany(curve => curve.Item2).Any(point => !Finite(point.Dx) || !Finite(point.Dy)))
                throw new InvalidOperationException("Nonfinite canonical displacement geometry.");
            double maximum = curves.SelectMany(curve => curve.Item2).Select(point => point.Magnitude).DefaultIfEmpty(0).Max();
            if (!Finite(maximum)) throw new InvalidOperationException("Nonfinite canonical displacement geometry.");
            double scale = maximum == 0 ? 0 : 35 / maximum;
            foreach (var curve in curves)
            {
                MemberGeometry member = curve.Item1;
                XPoint Location(DisplacementPoint point)
                {
                    XPoint origin = transform.Point(member.X + member.Ux * point.Distance, member.Y + member.Uy * point.Distance);
                    return new XPoint(origin.X + point.Dx * scale, origin.Y + (yUp ? -1 : 1) * point.Dy * scale);
                }
                if (maximum > 0) document.gfx.DrawLines(new XPen(XColors.Black, .6), curve.Item2.Select(Location).ToArray());
                IEnumerable<DisplacementPoint> selected = displacementLabelNodes == null
                    ? curve.Item2.OrderByDescending(point => point.Magnitude).Take(1)
                    : curve.Item2.Where(point => point.NodeId != null && displacementLabelNodes.Contains(point.NodeId));
                foreach (DisplacementPoint point in selected)
                    labels.Add(Location(point), point.Magnitude * labelScale, transform.Normal(member));
            }
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
            internal PlotTransform(IEnumerable<Vector3> source, XRect panel, JObject settings, double unitScale, bool yUp)
            {
                Vector3[] nodes = source.ToArray();
                double xmin = nodes.Min(node => node.x), xmax = nodes.Max(node => node.x);
                double ymin = nodes.Min(node => node.y), ymax = nodes.Max(node => node.y);
                centerX = (xmin + xmax) / 2; centerY = (ymin + ymax) / 2;
                originX = panel.Left + panel.Width / 2 + 15;
                originY = panel.Top + 74 + (panel.Height - 106) / 2;
                double width = panel.Width * .66, height = panel.Height - 106;
                scaleX = xmax == xmin ? height / Math.Max(ymax - ymin, 1) : width / (xmax - xmin);
                scaleY = ymax == ymin ? scaleX : height / (ymax - ymin);
                if (settings["scaleX"] != null) scaleX = settings["scaleX"].Value<double>() * unitScale * 72 / 25.4;
                if (settings["scaleY"] != null) scaleY = settings["scaleY"].Value<double>() * unitScale * 72 / 25.4;
                if (!Finite(scaleX) || !Finite(scaleY) || scaleX <= 0 || scaleY <= 0)
                    throw new InvalidOperationException("Invalid canonical diagram scale.");
                ySign = yUp ? -1 : 1;
            }
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
            internal ForceCurve(MemberGeometry member, List<Fsec> rows, string component, bool basic, List<double> notices)
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
                    bool notice = notices != null && notices.Any(position => Math.Abs(position - row.l) <= 1e-10 * Math.Max(1, member.Length));
                    if (!row.dummy && (point.End || notice)) Labels.Add(point);
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

        private sealed class LabelPlacement
        {
            private readonly PdfDocument document;
            private readonly XRect panel;
            private readonly List<XRect> occupied = new List<XRect>();
            private readonly List<Tuple<XPoint, string, double>> labels = new List<Tuple<XPoint, string, double>>();
            internal LabelPlacement(PdfDocument document, XRect panel) { this.document = document; this.panel = panel; }
            internal void Add(XPoint anchor, double value, XPoint normal)
            {
                string text = value.ToString("0.00", CultureInfo.InvariantCulture);
                if (text == "0.00" || text == "-0.00") return;
                double angle = Math.Atan2(normal.Y, normal.X) * 180 / Math.PI;
                if (angle > 90) angle -= 180;
                if (angle < -90) angle += 180;
                // Adjacent short members can repeat one physical value within a glyph's
                // height. Suppress that repeat before collision placement pushes it away
                // from its source, while retaining different values and orientations.
                if (labels.Any(label => label.Item2 == text &&
                    Math.Pow(label.Item1.X - anchor.X, 2) + Math.Pow(label.Item1.Y - anchor.Y, 2) <= Math.Pow(LabelSize + 3, 2) &&
                    (Math.Abs(label.Item3 - angle) < 1e-6 || Math.Abs(Math.Abs(label.Item3 - angle) - 180) < 1e-6))) return;
                var font = new XFont("MS Gothic", LabelSize);
                double width = document.gfx.MeasureString(text, font).Width;
                double radians = angle * Math.PI / 180, cosine = Math.Cos(radians), sine = Math.Sin(radians);
                var corners = new[] { new XPoint(-1, -LabelSize - 1), new XPoint(width + 1, -LabelSize - 1),
                    new XPoint(-1, 2), new XPoint(width + 1, 2) }
                    .Select(point => new XPoint(point.X * cosine - point.Y * sine, point.X * sine + point.Y * cosine)).ToArray();
                double left = corners.Min(point => point.X), top = corners.Min(point => point.Y);
                double occupiedWidth = corners.Max(point => point.X) - left, occupiedHeight = corners.Max(point => point.Y) - top;
                for (int attempt = 0; attempt < 80; attempt++)
                {
                    int row = attempt / 5, column = attempt % 5;
                    double x = anchor.X + 3 + (column == 0 ? 0 : (column + 1) / 2 * (column % 2 == 1 ? -1 : 1) * (occupiedWidth + 3));
                    double y = anchor.Y - 3 + (row == 0 ? 0 : (row + 1) / 2 * (row % 2 == 1 ? -1 : 1) * (occupiedHeight + 3));
                    var rectangle = new XRect(x + left, y + top, occupiedWidth, occupiedHeight);
                    if (rectangle.Left < panel.Left || rectangle.Right > panel.Right || rectangle.Top < panel.Top + 24 ||
                        rectangle.Bottom > panel.Bottom || occupied.Any(area => area.IntersectsWith(rectangle))) continue;
                    var state = document.gfx.Save();
                    document.gfx.TranslateTransform(x, y);
                    document.gfx.RotateTransform(angle);
                    document.gfx.DrawString(text, font, XBrushes.Blue, new XPoint());
                    document.gfx.Restore(state);
                    occupied.Add(rectangle); labels.Add(Tuple.Create(anchor, text, angle));
                    return;
                }
                throw new InvalidOperationException("Diagram labels cannot fit without overlap; choose a larger page or fewer diagrams per page.");
            }
        }
    }
}

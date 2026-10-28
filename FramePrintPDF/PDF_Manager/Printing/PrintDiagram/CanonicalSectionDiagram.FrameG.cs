using Newtonsoft.Json.Linq;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PDF_Manager.Printing
{
    /// <summary>
    /// FrameG's 断面力図 page (原宿ボックス 0404, 見本_断面力図.pdf): a blue header with the title and the case,
    /// the frame at a round scale in each panel, the largest value drawn 10 mm from its member, and values only
    /// at member ends, rigid-zone faces and interior extremes, in small blue type across the member.
    /// Positions are the sample's, in pt from the page's top-left corner; another paper keeps them and
    /// fits the panels to its own size.
    /// </summary>
    internal sealed partial class CanonicalSectionDiagram
    {
        // Page header and panels (A4 sample: title at 80.4 pt, panels 339.2 pt high from 135.3 pt).
        private const double FrameGLeft = 80.4, FrameGRight = 39.9, FrameGBottom = 28.3;
        private const double TitleBaseline = 89.2, CaptionBaseline = 117.4, PanelTop = 135.3;
        private const double HeadingIndent = 2.9, HeadingBaseline = 4.9;
        private const double TitleSize = 8.86, CaptionSize = 9.66, HeadingSize = 8.16, ValueSize = 6.70;
        // At 1:70 the sample's 4.75 m x 3.225 m frame takes 0.41 x 0.38 of a panel; 1:60 would exceed 0.44.
        private const double FrameShare = .44;
        private const double MaxOrdinate = 72 / 2.54;   // 10 mm for the largest force or displacement
        private const double MemberPen = .72, RigidPen = 1.08, CurvePen = .48, DisplacementFramePen = .24;
        // Displacement curves bend at about 0.4 m intervals of a member's flexible part, at most 10 per part.
        private const double DisplacementDivision = .4;
        private const int MaxDisplacementDivisions = 10;

        private HashSet<string> rigidMembers = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Tuple<double, double>> rigidZones =
            new Dictionary<string, Tuple<double, double>>(StringComparer.Ordinal);
        private HashSet<string> springMembers = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> caseNames = new Dictionary<string, string>(StringComparer.Ordinal);

        private void ReadFrameGSettings(Dictionary<string, object> root)
        {
            // Both are optional: without them every member is drawn as an ordinary one.
            if (settings["rigidMembers"] is JArray members)
                rigidMembers = new HashSet<string>(members.Values<string>(), StringComparer.Ordinal);
            if (settings["springMembers"] is JArray springs)
                springMembers = new HashSet<string>(springs.Values<string>(), StringComparer.Ordinal);
            if (settings["rigidZones"] is JObject zones)
                foreach (var zone in zones.Properties())
                    if (zone.Value is JArray lengths && lengths.Count == 2)
                        rigidZones[zone.Name] = Tuple.Create(lengths[0].Value<double>(), lengths[1].Value<double>());
            // Names for a caption the projection did not supply (a missing name is left out).
            foreach (string key in new[] { ResultFsec.KEY, ResultFsecCombine.KEY, ResultFsecPickup.KEY })
                if (root.TryGetValue(key + "Name", out var rawNames) && rawNames != null)
                    foreach (var pair in JArray.FromObject(rawNames).OfType<JArray>())
                        if (pair.Count >= 2 && pair[0].Type != JTokenType.Null)
                            caseNames[key + "\n" + pair[0].Value<string>()] = pair[1]?.Value<string>() ?? "";
        }

        private void PrintFrameG(PdfDocument document, PrintData data, ref int pageIndex)
        {
            var nodes = ((InputNode)data.printDatas[InputNode.KEY]).Nodes;
            var members = ((InputMember)data.printDatas[InputMember.KEY]).members;
            if (nodes.Count == 0 || members.Count == 0)
                throw new InvalidOperationException("A canonical diagram requires original nodes and members.");
            var geometry = members.ToDictionary(pair => pair.Key,
                pair => new MemberGeometry(pair.Key, pair.Value, nodes));
            var cases = Cases(data).ToArray();
            if (cases.Length == 0) return;
            string layout = settings["layout"]?.Value<string>() ?? "single";
            bool vertical = string.Equals(layout, "splitVertical", StringComparison.OrdinalIgnoreCase);
            int panels = vertical || string.Equals(layout, "splitHorizontal", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            panels = Math.Min(panels, outputs.Length);

            TrimMargins margins = document.Margine;
            document.Margine = new TrimMargins
            { Left = FrameGLeft, Top = TitleBaseline - TitleSize, Right = FrameGRight, Bottom = FrameGBottom };
            // FrameG's diagram pages have their own header instead of the text pages' "TITLE :".
            document.BeginOwnHeaderPages(ref pageIndex);
            try
            {
                bool firstPage = true;
                foreach (var item in cases)
                    for (int first = 0; first < outputs.Length; first += panels)
                    {
                        document.CheckBudget();
                        if (!firstPage) document.NewPage(ref pageIndex);
                        firstPage = false;
                        DrawFrameGHeader(document, FrameGCaption(item), data.language);
                        for (int slot = 0; slot < panels && first + slot < outputs.Length; slot++)
                        {
                            string component = outputs[first + slot];
                            XRect panel = FrameGPanel(document, panels, slot, vertical);
                            DrawFrameGHeading(document, data.language, component, panel);
                            var transform = FrameGTransform(nodes.Values, panel);
                            DrawFrameGMembers(document, geometry.Values, transform, component == "disg");
                            if (component == "disg") DrawFrameGDisplacements(document, item, geometry, transform);
                            else DrawFrameGForces(document, item, component, geometry, transform, new FrameGLabels(
                                document, Bounds(nodes.Values.Select(node => transform.Point(node.x, node.y))),
                                item.Rows != null));
                        }
                    }
            }
            finally
            {
                document.EndOwnHeaderPages();
                document.Margine = margins;
            }
        }

        private string FrameGCaption(DiagramCase item)
        {
            string label = settings["caseLabels"]?[item.Id]?.Value<string>();
            if (label != null) return label;
            string prefix = item.Family == ResultFsecCombine.KEY ? "COMBINE" :
                item.Family == ResultFsecPickup.KEY ? "PICKUP" : "Case";
            caseNames.TryGetValue(item.Family + "\n" + item.Id, out string name);
            return prefix + " " + item.Id + (string.IsNullOrWhiteSpace(name) ? "" : " " + name);
        }

        private static XFont Mincho(double size, string language = null) =>
            new XFont(language == "cn" ? "simsun" : "MS Mincho", size, XFontStyle.Regular);

        private static void DrawFrameGHeader(PdfDocument document, string caption, string language)
        {
            if (!string.IsNullOrEmpty(document.Title))
                document.gfx.DrawString(document.Title, Mincho(TitleSize, language), XBrushes.Blue,
                    new XPoint(FrameGLeft, TitleBaseline));
            var font = Mincho(CaptionSize, language);
            string text = " " + caption;
            if (FrameGLeft + document.gfx.MeasureString(text, font).Width >
                document.currentPage.Width.Point - FrameGRight)
                throw new InvalidOperationException("The diagram case caption is too long for the page.");
            document.gfx.DrawString(text, font, XBrushes.Blue, new XPoint(FrameGLeft, CaptionBaseline));
        }

        private static XRect FrameGPanel(PdfDocument document, int count, int index, bool vertical)
        {
            double width = document.currentPage.Width.Point - FrameGLeft - FrameGRight;
            double height = document.currentPage.Height.Point - PanelTop - FrameGBottom;
            return vertical ? new XRect(FrameGLeft + index * width / count, PanelTop, width / count, height)
                : new XRect(FrameGLeft, PanelTop + index * height / count, width, height / count);
        }

        private static void DrawFrameGHeading(PdfDocument document, string language, string component, XRect panel)
        {
            // en and cn keep the layout and translate the words (cn: the earlier diagram's titles).
            string title = language == "en" ? (component == "mz" ? "Bending moment" :
                component == "fy" ? "Shear force" : component == "fx" ? "Axial force" : "Displacement  (mm)") :
                language == "cn" ? "　" + (component == "mz" ? "弯 矩 图" : component == "fy" ? "剪 力 图" :
                component == "fx" ? "轴 向 力 图" : "位 移 图  (mm)") :
                "　" + (component == "mz" ? "曲げモーメント図" : component == "fy" ? "せん断力図" :
                component == "fx" ? "軸　力　図" : "変　位　図  (㎜)");
            document.gfx.DrawString(title, Mincho(HeadingSize, language), XBrushes.Blue,
                new XPoint(panel.Left + HeadingIndent, panel.Top + HeadingBaseline));
        }

        // FrameG prints at a round scale (the sample's 1:70): the largest that keeps the frame within
        // FrameShare of the panel, rounded to one significant figure. An explicit scale still applies.
        private PlotTransform FrameGTransform(IEnumerable<Vector3> source, XRect panel)
        {
            Vector3[] nodes = source.ToArray();
            double width = nodes.Max(node => node.x) - nodes.Min(node => node.x);
            double height = nodes.Max(node => node.y) - nodes.Min(node => node.y);
            double fit = Math.Min(width > 0 ? panel.Width * FrameShare / width : double.PositiveInfinity,
                height > 0 ? panel.Height * FrameShare / height : double.PositiveInfinity);
            double pointsPerUnit = labelScale * 72 / 25.4;   // labelScale is millimetres per model unit
            double scale = double.IsPositiveInfinity(fit) ? pointsPerUnit / 100 : pointsPerUnit / RoundUp(pointsPerUnit / fit);
            double scaleX = settings["scaleX"] != null ? settings["scaleX"].Value<double>() * pointsPerUnit : scale;
            double scaleY = settings["scaleY"] != null ? settings["scaleY"].Value<double>() * pointsPerUnit : scale;
            return new PlotTransform(nodes, new XPoint(panel.Left + panel.Width / 2, panel.Top + panel.Height / 2),
                scaleX, scaleY, yUp);
        }

        private static double RoundUp(double denominator)
        {
            if (!Finite(denominator) || denominator <= 0)
                throw new InvalidOperationException("Invalid canonical diagram scale.");
            double unit = Math.Pow(10, Math.Floor(Math.Log10(denominator)));
            return Math.Ceiling(denominator / unit - 1e-9) * unit;
        }

        private static XRect Bounds(IEnumerable<XPoint> source)
        {
            XPoint[] points = source.ToArray();
            double left = points.Min(point => point.X), top = points.Min(point => point.Y);
            return new XRect(left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);
        }

        private Tuple<double, double> RigidZone(MemberGeometry member) =>
            rigidZones.TryGetValue(member.Id, out var zone) ? zone : Tuple.Create(0d, 0d);

        // Rigid members and rigid zones are drawn thick; the displacement panel shows only the flexible
        // parts, thin, under the deflected shape.
        private void DrawFrameGMembers(PdfDocument document, IEnumerable<MemberGeometry> members,
            PlotTransform transform, bool displacement)
        {
            foreach (MemberGeometry member in members)
            {
                void Line(double from, double to, double width)
                {
                    if (to - from <= 1e-9 * Math.Max(1, member.Length)) return;
                    document.gfx.DrawLine(new XPen(XColors.Black, width),
                        transform.Point(member.X + member.Ux * from, member.Y + member.Uy * from),
                        transform.Point(member.X + member.Ux * to, member.Y + member.Uy * to));
                }
                var zone = RigidZone(member);
                bool rigid = rigidMembers.Contains(member.Id);
                if (displacement)
                {
                    if (!rigid) Line(zone.Item1, member.Length - zone.Item2, DisplacementFramePen);
                    continue;
                }
                if (rigid) { Line(0, member.Length, RigidPen); continue; }
                Line(0, zone.Item1, RigidPen);
                Line(zone.Item1, member.Length - zone.Item2, MemberPen);
                Line(member.Length - zone.Item2, member.Length, RigidPen);
            }
        }

        private bool AtRigidFace(MemberGeometry member, double distance)
        {
            var zone = RigidZone(member);
            double tolerance = 1e-6 * Math.Max(1, member.Length);
            return zone.Item1 > tolerance && Math.Abs(distance - zone.Item1) <= tolerance ||
                zone.Item2 > tolerance && Math.Abs(distance - (member.Length - zone.Item2)) <= tolerance;
        }

        private void DrawFrameGForces(PdfDocument document, DiagramCase item, string component,
            Dictionary<string, MemberGeometry> geometry, PlotTransform transform, FrameGLabels labels)
        {
            var curves = new List<ForceCurve>();
            var runs = new List<List<Fsec>>();
            foreach (List<Fsec> path in Paths(item, component))
            foreach (List<Fsec> run in MemberRuns(path))
            {
                if (!geometry.TryGetValue(run[0].m, out var member))
                    throw new InvalidOperationException("Unknown diagram member: " + run[0].m);
                foreach (Fsec row in run)
                    if (!Finite(row.l) || row.l < -1e-9 * member.Length || row.l > member.Length * (1 + 1e-9) ||
                        !Finite(row.getValue2D(component)))
                        throw new InvalidOperationException("Invalid force station on diagram member: " + run[0].m);
                curves.Add(new ForceCurve(member, run, component, item.Rows != null));
                runs.Add(run);
            }
            if (curves.SelectMany(curve => curve.Samples).Any(point => !Finite(point.Value)))
                throw new InvalidOperationException("Nonfinite canonical force interpolation.");
            double maximum = curves.SelectMany(curve => curve.Samples).Select(point => Math.Abs(point.Value)).DefaultIfEmpty(0).Max();
            double scale = maximum == 0 ? 0 : MaxOrdinate / maximum;
            // FrameG draws M and S as the other layout does, and N as -N on its own (y-up) local +y side,
            // which is +N on FrameWeb's (y-down) member normal: a closed frame's compression is not always inside.
            double side = component == "fy" ? -1 : 1;
            var pen = new XPen(XColors.Black, CurvePen);
            XPoint Origin(MemberGeometry member, double distance) =>
                transform.Point(member.X + member.Ux * distance, member.Y + member.Uy * distance);
            XPoint End(MemberGeometry member, double distance, double value)
            {
                XPoint origin = Origin(member, distance), normal = transform.Normal(member);
                return new XPoint(origin.X + normal.X * value * side * scale, origin.Y + normal.Y * value * side * scale);
            }
            string Text(double value) => (component == "fx" ? value : Math.Abs(value)).ToString("0.00", CultureInfo.InvariantCulture);

            // A basic case's N sits before its station; an envelope's N is centred like M and S.
            var along = component == "fx" && item.Rows != null ? FrameGLabels.Along.Before : FrameGLabels.Along.Centre;
            var ends = new List<FrameGLabel>();
            var faces = new List<FrameGLabel>();
            var interior = new Dictionary<string, FrameGLabel>(StringComparer.Ordinal);
            for (int index = 0; index < curves.Count; index++)
            {
                document.CheckBudget();
                ForceCurve curve = curves[index];
                MemberGeometry member = curve.Member;
                XPoint[] points = curve.Samples.Select(point => End(member, point.Distance, point.Value)).ToArray();
                if (points.Length > 1 && maximum > 0) document.gfx.DrawLines(pen, points);
                List<Fsec> run = runs[index];
                for (int row = 0; row < run.Count; row++)
                {
                    bool end = row == 0 || row == run.Count - 1, face = AtRigidFace(member, run[row].l);
                    if (!end && !face) continue;
                    double value = run[row].getValue2D(component);
                    if (value != 0) document.gfx.DrawLine(pen, Origin(member, run[row].l), End(member, run[row].l, value));
                    if (run[row].dummy) continue;
                    // A face's value keeps over the flexible part, unless the member end beyond the rigid
                    // zone prints the same value (the sample's constant wall shear stays centred).
                    Fsec nearest = run[row].l - run[0].l <= run[run.Count - 1].l - run[row].l ? run[0] : run[run.Count - 1];
                    var label = new FrameGLabel(member, run[row].l, value,
                        face && !end && Text(nearest.getValue2D(component)) != Text(value));
                    // A basic case labels every end and both sides of a rigid-zone face; an envelope labels
                    // each member end once, with its governing side, and no faces.
                    if (item.Rows != null) (end ? ends : faces).Add(label);
                    else if (end) Govern(ends, label);
                }
                // The member's interior extreme of M: its largest or smallest value when that is not at an end.
                if (component == "mz")
                foreach (ForcePoint point in new[] { curve.Samples.OrderBy(sample => sample.Value).First(),
                    curve.Samples.OrderByDescending(sample => sample.Value).First() })
                {
                    if (point.End || AtRigidFace(member, point.Distance) ||
                        Math.Abs(point.Distance) <= 1e-9 * member.Length ||
                        Math.Abs(point.Distance - member.Length) <= 1e-9 * member.Length) continue;
                    if (!interior.TryGetValue(member.Id, out var current) || Math.Abs(point.Value) > Math.Abs(current.Value))
                        interior[member.Id] = new FrameGLabel(member, point.Distance, point.Value);
                }
            }
            foreach (FrameGLabel label in ends)
                labels.Add(Origin(label.Member, label.Distance), End(label.Member, label.Distance, label.Value),
                    transform.Normal(label.Member), Text(label.Value), false, along: along);
            foreach (FrameGLabel label in faces)
            {
                var zone = RigidZone(label.Member);
                labels.Add(Origin(label.Member, label.Distance), End(label.Member, label.Distance, label.Value),
                    transform.Normal(label.Member), Text(label.Value), false, !label.KeepOverFlexible ? null :
                    Tuple.Create(Origin(label.Member, zone.Item1), Origin(label.Member, label.Member.Length - zone.Item2)),
                    along);
            }
            foreach (FrameGLabel label in interior.Values)
                labels.Add(Origin(label.Member, label.Distance), End(label.Member, label.Distance, label.Value),
                    transform.Normal(label.Member), Text(label.Value), true);
        }

        private static void Govern(List<FrameGLabel> labels, FrameGLabel label)
        {
            int existing = labels.FindIndex(other => other.Member == label.Member &&
                Math.Abs(other.Distance - label.Distance) <= 1e-9 * Math.Max(1, label.Member.Length));
            if (existing < 0) labels.Add(label);
            else if (Math.Abs(label.Value) > Math.Abs(labels[existing].Value)) labels[existing] = label;
        }

        private void DrawFrameGDisplacements(PdfDocument document, DiagramCase item,
            Dictionary<string, MemberGeometry> geometry, PlotTransform transform)
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
                    foreach (double distance in DisplacementVertices(member))
                    {
                        int segment = 1;
                        while (segment < stations.Count - 1 && stations[segment].Item2 < distance) segment++;
                        var first = stations[segment - 1]; var last = stations[segment];
                        if (!values.TryGetValue(first.Item1, out var start) || !values.TryGetValue(last.Item1, out var end))
                            throw new InvalidOperationException("Missing canonical displacement station on member: " + member.Id);
                        double length = last.Item2 - first.Item2;
                        double t = Math.Min(1, Math.Max(0, (distance - first.Item2) / length));
                        double ui = member.Ux * start.dx + member.Uy * start.dy, uj = member.Ux * end.dx + member.Uy * end.dy;
                        double vi = -member.Uy * start.dx + member.Ux * start.dy, vj = -member.Uy * end.dx + member.Ux * end.dy;
                        double u = ui * (1 - t) + uj * t;
                        double v = start.caseStr == end.caseStr ? Hermite(vi, vj, start.rz, end.rz, length, t)
                            : vi * (1 - t) + vj * t;
                        string nodeId = Math.Abs(distance) <= 1e-12 ? member.NodeI :
                            Math.Abs(distance - member.Length) <= 1e-12 * Math.Max(1, member.Length) ? member.NodeJ : null;
                        points.Add(new DisplacementPoint(distance, member.Ux * u - member.Uy * v, member.Uy * u + member.Ux * v, nodeId));
                    }
                    curves.Add(Tuple.Create(member, points));
                }
            }
            if (curves.SelectMany(curve => curve.Item2).Any(point => !Finite(point.Dx) || !Finite(point.Dy)))
                throw new InvalidOperationException("Nonfinite canonical displacement geometry.");
            double maximum = curves.SelectMany(curve => curve.Item2).Select(point => point.Magnitude).DefaultIfEmpty(0).Max();
            double scale = maximum == 0 ? 0 : MaxOrdinate / maximum;
            XPoint Undeflected(MemberGeometry member, DisplacementPoint point) =>
                transform.Point(member.X + member.Ux * point.Distance, member.Y + member.Uy * point.Distance);
            XPoint Displaced(MemberGeometry member, DisplacementPoint point)
            {
                XPoint origin = Undeflected(member, point);
                return new XPoint(origin.X + point.Dx * scale, origin.Y + (yUp ? -1 : 1) * point.Dy * scale);
            }
            // Values keep within the deflected frame's extent, and one value is printed once at a place.
            var labels = new FrameGLabels(document, Bounds(curves.SelectMany(curve =>
                curve.Item2.Select(point => Displaced(curve.Item1, point)))), false);
            var starts = new List<Action>();
            var others = new List<Action>();
            foreach (var curve in curves)
            {
                MemberGeometry member = curve.Item1;
                XPoint Origin(DisplacementPoint point) => Undeflected(member, point);
                XPoint Location(DisplacementPoint point) => Displaced(member, point);
                if (maximum > 0) document.gfx.DrawLines(new XPen(XColors.Black, MemberPen), curve.Item2.Select(Location).ToArray());
                // The value across the member, as FrameG prints it (y for a beam, x for a wall), in mm.
                double Across(DisplacementPoint point) => Math.Abs(-member.Uy * point.Dx + member.Ux * point.Dy) * labelScale;
                // A member on ground springs shows every bend of its curve; another one its i end and its
                // largest value. An i end's value starts at the end and is placed before the others, so a
                // node shared with the previous member's j end keeps it.
                DisplacementPoint largest = curve.Item2.OrderByDescending(Across).First();
                bool springs = springMembers.Contains(member.Id);
                foreach (DisplacementPoint point in curve.Item2)
                {
                    bool first = point == curve.Item2[0];
                    if (!springs && !first && point != largest) continue;
                    if (displacementLabelNodes != null && (point.NodeId == null || !displacementLabelNodes.Contains(point.NodeId)) ||
                        AtRigidFace(member, point.Distance))
                        continue;
                    string text = Across(point).ToString("0.00", CultureInfo.InvariantCulture);
                    XPoint origin = Origin(point), location = Location(point), normal = transform.Normal(member);
                    (first ? starts : others).Add(() => labels.Add(origin, location, normal, text, false,
                        along: first ? FrameGLabels.Along.Start : FrameGLabels.Along.Centre));
                }
            }
            foreach (Action label in starts.Concat(others)) label();
        }

        private IEnumerable<double> DisplacementVertices(MemberGeometry member)
        {
            yield return 0;
            if (rigidMembers.Contains(member.Id)) { yield return member.Length; yield break; }
            var zone = RigidZone(member);
            double from = zone.Item1, to = member.Length - zone.Item2;
            if (from > 1e-9 * member.Length) yield return from;
            int divisions = Math.Max(1, Math.Min(MaxDisplacementDivisions,
                (int)Math.Ceiling((to - from) * labelScale / 1000 / DisplacementDivision - 1e-9)));
            for (int index = 1; index < divisions; index++) yield return from + (to - from) * index / divisions;
            if (to < member.Length * (1 - 1e-9)) yield return to;
            yield return member.Length;
        }

        private sealed class FrameGLabel
        {
            internal readonly MemberGeometry Member;
            internal readonly double Distance, Value;
            internal readonly bool KeepOverFlexible;
            internal FrameGLabel(MemberGeometry member, double distance, double value, bool keepOverFlexible = false)
            { Member = member; Distance = distance; Value = value; KeepOverFlexible = keepOverFlexible; }
        }

        /// <summary>
        /// FrameG's value labels: the text runs across the member (upward on a beam, rightward on a wall),
        /// beyond the drawn ordinate, centred on its station along the member and kept within the frame's
        /// extent; a rigid-zone face's value stays over the flexible part. An interior extreme whose ordinate
        /// is long enough sits between the member and the curve. FrameG does not move values that overlap.
        /// </summary>
        private sealed class FrameGLabels
        {
            // Measured on the sample: gaps from the curve, the baseline's offset from the station along the
            // member, the frame-end and face limits of the em box, and FrameG's estimate of a label's length.
            private const double GapAway = 3.8, GapToward = 3.4, BaselineAcross = 3.35, BaselineAlong = 2.45;
            private const double StartLimit = 5.9, FaceStartLimit = 6.6, EndLimit = 2.15;
            private const double StartAway = 4.75, BeforeStation = -4.3;
            private const double EstimatedCharacter = 4.08, BetweenSpare = 1.6, BetweenTowardMember = 1.0;
            private readonly PdfDocument document;
            private readonly XRect frame;
            private readonly bool duplicates;
            private readonly XFont font = Mincho(ValueSize);
            private readonly List<Tuple<XRect, string>> placed = new List<Tuple<XRect, string>>();

            internal FrameGLabels(PdfDocument document, XRect frame, bool duplicates)
            { this.document = document; this.frame = frame; this.duplicates = duplicates; }

            /// <param name="flexible">For a rigid-zone face: the ends of the member's flexible part.</param>
            // Centre: the station's value; Start: a member's i end (displacement); Before: N, which FrameG
            // prints just before its station and always on the upper or right side of the member.
            internal enum Along { Centre, Start, Before }

            internal void Add(XPoint origin, XPoint end, XPoint normal, string text, bool interior,
                Tuple<XPoint, XPoint> flexible = null, Along along = Along.Centre)
            {
                if (text == "0.00" || text == "-0.00") return;
                // Reading direction r is the member normal turned up or right; p is the glyphs' down.
                XPoint r = normal.Y > 1e-9 || Math.Abs(normal.Y) <= 1e-9 && normal.X < 0
                    ? new XPoint(-normal.X, -normal.Y) : normal;
                var p = new XPoint(-r.Y, r.X);
                double Dot(XPoint a, XPoint b) => a.X * b.X + a.Y * b.Y;
                double width = document.gfx.MeasureString(text, font).Width;
                double ordinate = Dot(new XPoint(end.X - origin.X, end.Y - origin.Y), r);
                double direction = ordinate < 0 ? -1 : 1;
                double start;
                if (interior && Math.Abs(ordinate) >= text.Length * EstimatedCharacter + BetweenSpare)
                    start = (Dot(origin, r) + Dot(end, r)) / 2 - direction * BetweenTowardMember - width / 2;
                else if (along == Along.Before) start = Math.Max(Dot(origin, r), Dot(end, r)) + GapAway;
                else start = direction > 0 ? Dot(end, r) + GapAway : Dot(end, r) - GapToward - width;
                if (along == Along.Start) start += direction * StartAway;
                bool across = Math.Abs(r.X) > Math.Abs(r.Y);
                double baseline = Dot(end, p) + (along == Along.Start ? StartLimit : along == Along.Before ? BeforeStation :
                    across ? BaselineAcross : BaselineAlong);
                double low = Math.Min(Dot(frame.TopLeft, p), Dot(frame.BottomRight, p));
                double high = Math.Max(Dot(frame.TopLeft, p), Dot(frame.BottomRight, p));
                baseline = Math.Max(low + StartLimit, Math.Min(high - EndLimit, baseline));
                if (flexible != null)
                {
                    double first = Dot(flexible.Item1, p), last = Dot(flexible.Item2, p);
                    baseline = Math.Max(Math.Min(first, last) + FaceStartLimit,
                        Math.Min(Math.Max(first, last) - EndLimit, baseline));
                }

                var corners = new[] { start, start + width }.SelectMany(a => new[] { baseline - ValueSize, baseline }
                    .Select(b => new XPoint(r.X * a + p.X * b, r.Y * a + p.Y * b))).ToArray();
                double left = corners.Min(c => c.X), top = corners.Min(c => c.Y);
                var box = new XRect(left, top, corners.Max(c => c.X) - left, corners.Max(c => c.Y) - top);
                if (!duplicates && placed.Any(item => item.Item2 == text && item.Item1.IntersectsWith(box))) return;
                placed.Add(Tuple.Create(box, text));
                var state = document.gfx.Save();
                document.gfx.TranslateTransform(r.X * start + p.X * baseline, r.Y * start + p.Y * baseline);
                document.gfx.RotateTransform(Math.Atan2(r.Y, r.X) * 180 / Math.PI);
                document.gfx.DrawString(text, font, XBrushes.Blue, new XPoint());
                document.gfx.Restore(state);
            }
        }
    }
}

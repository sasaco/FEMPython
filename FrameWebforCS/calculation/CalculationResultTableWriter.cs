using FarPoint.Win.Spread;
using FrameWebforCS.components.result;
using System.Globalization;

namespace FrameWebforCS.calculation;

internal static class CalculationResultTableWriter
{
    internal static string SheetName(CalculationResultPage page, int ordinal) =>
        $"{ordinal + 1} {page.Label}"[..Math.Min(31, $"{ordinal + 1} {page.Label}".Length)];

    internal static void FillDisplacements(SheetView sheet, CalculationResultPresentation presentation,
        CalculationResultPage page, int dimension)
    {
        if (page.MovingChildren.Count > 0 && page.Result is StaticAnalysisResult parent)
        {
            FillMovingEnvelope(sheet, [parent, .. page.MovingChildren],
                source => source.NodeDisplacements.Select(item =>
                    (item.NodeId, new[] { item.Components.Dx, item.Components.Dy,
                        item.Components.Dz, item.Components.Rx, item.Components.Ry, item.Components.Rz })),
                ["Dx", "Dy", "Dz", "Rx", "Ry", "Rz"],
                (component, value) => component < 3 ? presentation.DisplayLength(value) : value,
                component => component < 3 ? presentation.DisplayLengthUnit : "rad",
                dimension == 3 ? [0, 1, 2, 3, 4, 5] : [0, 1, 5]);
            return;
        }
        ResultDisgComponent.SetSheet1(sheet);
        sheet.ColumnHeader.Cells[0, 1].Text = page.Result is ModalAnalysisResult
            ? "固有モード (無次元)" : $"移動量 ({presentation.DisplayLengthUnit})";
        sheet.ColumnHeader.Cells[0, dimension == 3 ? 4 : 3].Text =
            page.Result is ModalAnalysisResult ? "回転モード (無次元)" : "回転 (rad)";
        var rows = page.Result switch
        {
            ForceAnalysisResult force => force.NodeDisplacements,
            ModalAnalysisResult modal => modal.NodeModeShapes,
            _ => (IReadOnlyList<NodeDisplacement>)[],
        };
        sheet.RowCount = rows.Count;
        for (int row = 0; row < rows.Count; row++)
        {
            NodeDisplacement entry = rows[row];
            double Scale(double value) => page.Result is ModalAnalysisResult
                ? value : presentation.DisplayLength(value);
            sheet.Cells[row, 0].Text = entry.NodeId;
            sheet.Cells[row, 1].Text = Format(Scale(entry.Components.Dx));
            sheet.Cells[row, 2].Text = Format(Scale(entry.Components.Dy));
            if (dimension == 3)
            {
                sheet.Cells[row, 3].Text = Format(Scale(entry.Components.Dz));
                sheet.Cells[row, 4].Text = Format(entry.Components.Rx);
                sheet.Cells[row, 5].Text = Format(entry.Components.Ry);
                sheet.Cells[row, 6].Text = Format(entry.Components.Rz);
            }
            else sheet.Cells[row, 3].Text = Format(entry.Components.Rz);
        }
        sheet.Protect = true;
    }

    internal static void FillReactions(SheetView sheet, CalculationResultPresentation presentation,
        CalculationResultPage page, int dimension)
    {
        if (page.MovingChildren.Count > 0 && page.Result is StaticAnalysisResult parent)
        {
            FillMovingEnvelope(sheet, [parent, .. page.MovingChildren],
                source => source.SupportReactions.Select(item =>
                    (item.NodeId, new[] { item.Components.Fx, item.Components.Fy,
                        item.Components.Fz, item.Components.Mx, item.Components.My, item.Components.Mz })),
                ["Fx", "Fy", "Fz", "Mx", "My", "Mz"],
                (_, value) => value,
                component => component < 3 ? presentation.ForceUnit : presentation.MomentUnit,
                dimension == 3 ? [0, 1, 2, 3, 4, 5] : [0, 1, 5]);
            return;
        }
        ResultReacComponent.SetSheet1(sheet);
        sheet.ColumnHeader.Cells[0, 1].Text = $"支点反力 ({presentation.ForceUnit})";
        sheet.ColumnHeader.Cells[0, dimension == 3 ? 4 : 3].Text =
            $"回転反力 ({presentation.MomentUnit})";
        IReadOnlyList<SupportReaction> rows = page.Result is ForceAnalysisResult force
            ? force.SupportReactions : [];
        sheet.RowCount = rows.Count;
        for (int row = 0; row < rows.Count; row++)
        {
            SupportReaction entry = rows[row];
            sheet.Cells[row, 0].Text = entry.NodeId;
            sheet.Cells[row, 1].Text = Format(entry.Components.Fx);
            sheet.Cells[row, 2].Text = Format(entry.Components.Fy);
            if (dimension == 3)
            {
                sheet.Cells[row, 3].Text = Format(entry.Components.Fz);
                sheet.Cells[row, 4].Text = Format(entry.Components.Mx);
                sheet.Cells[row, 5].Text = Format(entry.Components.My);
                sheet.Cells[row, 6].Text = Format(entry.Components.Mz);
            }
            else sheet.Cells[row, 3].Text = Format(entry.Components.Mz);
        }
        sheet.Protect = true;
    }

    internal static void FillSectionForces(SheetView sheet, CalculationResultPresentation presentation,
        CalculationResultPage page, int dimension)
    {
        if (page.MovingChildren.Count > 0 && page.Result is StaticAnalysisResult parent)
        {
            FillMovingEnvelope(sheet, [parent, .. page.MovingChildren],
                source => source.MemberSectionForces.SelectMany(member =>
                    member.Segments.SelectMany(segment => new[]
                    {
                        ($"{member.MemberId}/{segment.SegmentId}/{segment.StationI}", ToArray(segment.IEnd)),
                        ($"{member.MemberId}/{segment.SegmentId}/{segment.StationJ}", ToArray(segment.JEnd)),
                    })),
                ["Fx", "Fy", "Fz", "Mx", "My", "Mz"],
                (_, value) => value,
                component => component < 3 ? presentation.ForceUnit : presentation.MomentUnit,
                dimension == 3 ? [0, 1, 2, 3, 4, 5] : [0, 1, 5]);
            return;
        }
        ResultFsecComponent.SetSheet1(sheet);
        if (dimension == 3)
        {
            sheet.ColumnHeader.Cells[0, 4].Text = $"せん断力 ({presentation.ForceUnit})";
            sheet.ColumnHeader.Cells[0, 7].Text = $"曲げモーメント ({presentation.MomentUnit})";
        }
        sheet.ColumnHeader.Cells[1, 2].Text = $"({presentation.LengthUnit})";
        sheet.ColumnHeader.Cells[1, 3].Text = $"({presentation.ForceUnit})";
        sheet.ColumnHeader.Cells[1, 4].Text = $"({presentation.ForceUnit})";
        sheet.ColumnHeader.Cells[1, dimension == 3 ? 6 : 5].Text =
            $"({presentation.MomentUnit})";
        IReadOnlyList<MemberSectionForces> members = page.Result is ForceAnalysisResult force
            ? force.MemberSectionForces : [];
        var topologyMembers = presentation.ResultSet.Topology.Members.ToDictionary(
            item => item.MemberId, StringComparer.Ordinal);
        var rows = new List<(string Member, string Station, double Position, ForceComponents Force)>();
        foreach (MemberSectionForces member in members)
        {
            var stations = topologyMembers[member.MemberId].Stations.ToDictionary(
                station => station.StationId, station => station.Position, StringComparer.Ordinal);
            bool first = true;
            foreach (MemberSegmentResult segment in member.Segments)
            {
                if (first)
                    rows.Add((member.MemberId, segment.StationI, stations[segment.StationI], segment.IEnd));
                rows.Add((first ? "" : member.MemberId, segment.StationJ,
                    stations[segment.StationJ], segment.JEnd));
                first = false;
            }
        }
        sheet.RowCount = rows.Count;
        for (int row = 0; row < rows.Count; row++)
        {
            var entry = rows[row];
            sheet.Cells[row, 0].Text = entry.Member;
            sheet.Cells[row, 1].Text = entry.Station;
            sheet.Cells[row, 2].Text = Format(entry.Position);
            sheet.Cells[row, 3].Text = Format(entry.Force.Fx);
            sheet.Cells[row, 4].Text = Format(entry.Force.Fy);
            if (dimension == 3)
            {
                sheet.Cells[row, 5].Text = Format(entry.Force.Fz);
                sheet.Cells[row, 6].Text = Format(entry.Force.Mx);
                sheet.Cells[row, 7].Text = Format(entry.Force.My);
                sheet.Cells[row, 8].Text = Format(entry.Force.Mz);
            }
            else sheet.Cells[row, 5].Text = Format(entry.Force.Mz);
        }
        sheet.Protect = true;
    }

    private static string Format(double value) => value.ToString("G10", CultureInfo.InvariantCulture);

    private static double[] ToArray(ForceComponents force) =>
        [force.Fx, force.Fy, force.Fz, force.Mx, force.My, force.Mz];

    private static void FillMovingEnvelope(SheetView sheet,
        IReadOnlyList<StaticAnalysisResult> sources,
        Func<StaticAnalysisResult, IEnumerable<(string Id, double[] Values)>> extract,
        IReadOnlyList<string> components,
        Func<int, double, double> convert,
        Func<int, string> unit,
        IReadOnlyList<int> visibleComponents)
    {
        sheet.ColumnCount = 6;
        sheet.ColumnHeader.RowCount = 1;
        foreach (var (column, title) in new[] { "ID", "成分", "最大", "出典ケース", "最小", "出典ケース" }.Index())
            sheet.ColumnHeader.Cells[0, column].Text = title;
        var sourceMaps = sources.Select(source =>
            (source.CaseId, Rows: extract(source).ToDictionary(item => item.Id,
                item => item.Values, StringComparer.Ordinal))).ToArray();
        var identities = sourceMaps.SelectMany(item => item.Rows.Keys)
            .Distinct(StringComparer.Ordinal).ToArray();
        sheet.RowCount = identities.Length * visibleComponents.Count;
        int row = 0;
        foreach (string id in identities)
        foreach (int component in visibleComponents)
        {
            var values = sourceMaps.Where(source => source.Rows.ContainsKey(id))
                .Select(source => (source.CaseId, Value: convert(component, source.Rows[id][component])))
                .ToArray();
            var maximum = values.Aggregate((best, next) => next.Value > best.Value ? next : best);
            var minimum = values.Aggregate((best, next) => next.Value < best.Value ? next : best);
            sheet.Cells[row, 0].Text = id;
            sheet.Cells[row, 1].Text = $"{components[component]} ({unit(component)})";
            sheet.Cells[row, 2].Text = Format(maximum.Value);
            sheet.Cells[row, 3].Text = maximum.CaseId;
            sheet.Cells[row, 4].Text = Format(minimum.Value);
            sheet.Cells[row, 5].Text = minimum.CaseId;
            row++;
        }
        sheet.Protect = true;
    }
}

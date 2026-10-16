using FrameWebforCS.calculation;
using System.Text.Json.Nodes;

namespace FrameWebforCS.providers.printing;

/// <summary>Private 2D diagram metadata and geometry; shared result tables stay unchanged.</summary>
internal static class PrintDiagramProjection
{
    internal static void Apply(JsonObject root, JsonObject diagram, JsonObject details,
        PrintSnapshot snapshot, PrintOption option)
    {
        CalculationResultPresentation presentation = snapshot.Result ??
            throw new PrintProjectionException("A section diagram requires current calculation results.");
        details["canonicalStations"] = true;
        details["yUp"] = true;
        details["referenceLayout"] = true;
        details["caseLabels"] = CaseLabels(root, presentation, option);
        // Physical drawing scales need this conversion even for force-only diagrams.
        details["displacementLabelScale"] = PrintProjection.DisplacementLengthScale(
            presentation.ResultSet.Units.Length);

        if (diagram["disg"] is not JsonObject displacements) return;
        details["displacementStations"] = DisplacementStations(presentation, snapshot.Selection);
        if (snapshot.Selection.NodeIds is { } labelNodes)
            details["displacementLabelNodeIds"] = new JsonArray(labelNodes.Select(id =>
                (JsonNode?)JsonValue.Create(id)).ToArray());
        var pages = presentation.Pages.ToDictionary(page => page.Key, StringComparer.Ordinal);
        foreach (var (key, value) in displacements)
        {
            CalculationResultPage page = pages[key];
            if (value is JsonArray rows)
            {
                ReplaceDisplacements(rows.OfType<JsonObject>(), Displacements(page.Result));
                continue;
            }

            // Moving envelopes already identify the selected correlated source. Preserve
            // each focus mode and source, replacing only its diagram-local numeric vector.
            var sources = new[] { page.Result }.Concat(page.MovingChildren)
                .ToDictionary(result => result.CaseId, result => Displacements(result), StringComparer.Ordinal);
            foreach (JsonObject mode in value!.AsObject().Select(pair => pair.Value).OfType<JsonObject>())
                foreach (JsonObject row in mode.Select(pair => pair.Value).OfType<JsonObject>())
                    ReplaceDisplacements([row], sources[row["case"]!.GetValue<string>()]);
        }
    }

    private static JsonObject CaseLabels(JsonObject root,
        CalculationResultPresentation presentation, PrintOption option)
    {
        string family = option switch
        {
            PrintOption.CombinedSectionDiagram => "fsecCombine",
            PrintOption.PickupSectionDiagram => "fsecPickup",
            _ => "fsec",
        };
        var names = root[family + "Name"]!.AsArray().OfType<JsonArray>()
            .ToDictionary(pair => pair[0]!.GetValue<string>(),
                pair => pair[1]!.GetValue<string>(), StringComparer.Ordinal);
        var pages = presentation.Pages.ToDictionary(page => page.Key, StringComparer.Ordinal);
        var labels = new JsonObject();
        foreach (string key in root[family]!.AsObject().Select(pair => pair.Key))
        {
            string name = names.GetValueOrDefault(key) ?? "";
            string id = key;
            string prefix = option switch
            {
                PrintOption.CombinedSectionDiagram => "Combine",
                PrintOption.PickupSectionDiagram => "Pickup Table",
                _ => "Case",
            };
            if (option == PrintOption.SectionDiagram)
            {
                CalculationResultPage page = pages[key];
                id = page.Case.CaseId;
                if (!string.IsNullOrWhiteSpace(page.Case.Symbol) &&
                    (name == id || page.Case.Name == id && name == page.Label))
                    name = page.Case.Symbol + name[id.Length..];
            }
            labels[key] = $"{prefix} {id}" +
                (string.IsNullOrWhiteSpace(name) || name == id ? "" : " " + name);
        }
        return labels;
    }

    private static JsonObject DisplacementStations(CalculationResultPresentation presentation,
        PrintSelection selection)
    {
        var topology = presentation.ResultSet.Topology;
        var nodes = topology.Nodes.ToDictionary(node => node.NodeId, StringComparer.Ordinal);
        var mappings = new JsonObject();
        foreach (TopologyMember member in topology.Members.Where(member =>
            selection.MemberIds is null || selection.MemberIds.Contains(member.MemberId)))
        {
            Vector3Value start = nodes[member.NodeI].Coordinates;
            Vector3Value end = nodes[member.NodeJ].Coordinates;
            double length = Distance(start, end);
            // Match the tolerance used by the canonical topology producer. The
            // producer may reuse an original node or a different member's node;
            // public/generated ID spelling never establishes station identity.
            double tolerance = 1e-10 * Math.Max(1, length);
            Vector3Value axis = member.LocalFrame.XAxis;
            if (!double.IsFinite(length) || length <= 0 ||
                Distance(start, member.LocalFrame.Origin) > tolerance ||
                Distance(end, Along(start, axis, length)) > tolerance ||
                Math.Abs(member.Stations[0].Position) > tolerance ||
                Math.Abs(member.Stations[^1].Position - length) > tolerance)
                throw new PrintProjectionException(
                    $"Displacement station geometry for member '{member.MemberId}' disagrees with its endpoints or local frame.");

            var stations = new JsonArray();
            var mappedNodes = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < member.Stations.Count; index++)
            {
                MemberStation station = member.Stations[index];
                string nodeId;
                if (index == 0) nodeId = member.NodeI;
                else if (index == member.Stations.Count - 1) nodeId = member.NodeJ;
                else
                {
                    Vector3Value location = Along(start, axis, station.Position);
                    TopologyNode[] candidates = topology.Nodes.Where(node =>
                        Distance(node.Coordinates, location) <= tolerance).Take(2).ToArray();
                    if (candidates.Length != 1)
                        throw new PrintProjectionException(
                            $"Displacement station '{member.MemberId}/{station.StationId}' has no unique topology node at its position.");
                    nodeId = candidates[0].NodeId;
                }
                if (!mappedNodes.Add(nodeId))
                    throw new PrintProjectionException(
                        $"Displacement stations of member '{member.MemberId}' map to the same topology node '{nodeId}'.");
                stations.Add(new JsonObject { ["nodeId"] = nodeId, ["position"] = station.Position });
            }
            mappings[member.MemberId] = stations;
        }
        return mappings;
    }

    private static Vector3Value Along(Vector3Value origin, Vector3Value axis, double position) =>
        new(origin.X + axis.X * position, origin.Y + axis.Y * position, origin.Z + axis.Z * position);

    private static double Distance(Vector3Value first, Vector3Value second) => Math.Sqrt(
        (first.X - second.X) * (first.X - second.X) +
        (first.Y - second.Y) * (first.Y - second.Y) +
        (first.Z - second.Z) * (first.Z - second.Z));

    private static IReadOnlyDictionary<string, NodeDisplacement> Displacements(AnalysisResult result)
    {
        IReadOnlyList<NodeDisplacement> rows = result switch
        {
            ForceAnalysisResult force => force.NodeDisplacements,
            ModalAnalysisResult modal => modal.NodeModeShapes,
            _ => throw new PrintProjectionException("The result has no diagram displacement vectors."),
        };
        return rows.ToDictionary(row => row.NodeId, StringComparer.Ordinal);
    }

    private static void ReplaceDisplacements(IEnumerable<JsonObject> rows,
        IReadOnlyDictionary<string, NodeDisplacement> source)
    {
        foreach (JsonObject row in rows)
        {
            // Re-read the typed source instead of reversing mm/mrad scaling, which
            // would lose precision and leave the table's clockwise rotation sign.
            var value = source[row["id"]!.GetValue<string>()].Components;
            row["dx"] = value.Dx;
            row["dy"] = value.Dy;
            row["dz"] = value.Dz;
            row["rx"] = value.Rx;
            row["ry"] = value.Ry;
            row["rz"] = value.Rz;
        }
    }
}

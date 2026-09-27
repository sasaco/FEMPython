using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrameWebforCS.calculation;

public sealed class AnalysisContractException : FormatException
{
    public AnalysisContractException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public static class AnalysisResultSetJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        AllowTrailingCommas = false,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static AnalysisResultSet Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return Deserialize(Encoding.UTF8.GetBytes(json));
    }

    public static AnalysisResultSet Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            throw new AnalysisContractException("AnalysisResultSet JSON must not be empty.");
        }

        try
        {
            byte[] bytes = utf8Json.ToArray();
            using JsonDocument document = JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 256,
            });
            RejectDuplicateProperties(document.RootElement, "$", 0);

            AnalysisResultSetWire wire = document.RootElement.Deserialize<AnalysisResultSetWire>(Options)
                ?? throw new JsonException("The AnalysisResultSet root cannot be null.");
            AnalysisResultSet result = Map(wire);
            AnalysisResultSetValidator.Validate(result);
            return result;
        }
        catch (AnalysisContractException)
        {
            throw;
        }
        catch (Exception exception) when (exception is
            JsonException or
            NotSupportedException or
            OverflowException or
            ArgumentNullException or
            NullReferenceException)
        {
            throw new AnalysisContractException("The payload is not a valid AnalysisResultSet v1 document.", exception);
        }
    }

    public static async Task<AnalysisResultSet> DeserializeAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Deserialize(buffer.ToArray());
    }

    private static AnalysisResultSet Map(AnalysisResultSetWire wire)
    {
        return new AnalysisResultSet(
            wire.Kind,
            wire.SchemaVersion,
            new AnalysisUnits(
                wire.Units.System,
                wire.Units.Length,
                wire.Units.Force,
                wire.Units.Mass,
                wire.Units.Time),
            new CoordinateSystem(
                wire.CoordinateSystem.Name,
                wire.CoordinateSystem.Handedness,
                wire.CoordinateSystem.Axes),
            wire.Cases.Select(Map),
            Map(wire.Topology),
            wire.Results.Select(MapResult));
    }

    private static AnalysisCase Map(AnalysisCaseWire value)
    {
        AnalysisType analysisType = value.AnalysisType switch
        {
            "static" => AnalysisType.Static,
            "material_nonlinear" => AnalysisType.MaterialNonlinear,
            "modal" => AnalysisType.Modal,
            _ => throw new AnalysisContractException(
                $"Analysis type '{value.AnalysisType}' is not supported."),
        };
        return new AnalysisCase(value.CaseId, value.Name, value.Symbol, analysisType, value.SupportNodeIds);
    }

    private static AnalysisTopology Map(TopologyWire value)
    {
        return new AnalysisTopology(
            value.Nodes.Select(node => new TopologyNode(
                node.NodeId,
                Map(node.Coordinates),
                node.SourceNodeId,
                node.Generated)),
            value.Members.Select(member => new TopologyMember(
                member.MemberId,
                member.NodeI,
                member.NodeJ,
                Map(member.LocalFrame),
                member.Stations.Select(station => new MemberStation(station.StationId, station.Position)))),
            value.ShellElements.Select(shell => new TopologyShellElement(
                shell.ElementId,
                ParseShellElementType(shell.ElementType),
                shell.NodeIds,
                Map(shell.LocalFrame),
                shell.ResultLocations.Select(location =>
                    new ShellResultLocationDefinition(location.LocationId, location.Kind)))),
            value.SolidElements.Select(solid => new TopologySolidElement(
                solid.ElementId,
                ParseSolidElementType(solid.ElementType),
                solid.NodeIds,
                solid.CoordinateFrame,
                solid.ResultLocations.Select(location => new SolidResultLocationDefinition(
                    location.LocationId,
                    new NaturalCoordinates(
                        location.NaturalCoordinates.Xi,
                        location.NaturalCoordinates.Eta,
                        location.NaturalCoordinates.Zeta))))));
    }

    private static AnalysisResult MapResult(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("state", out JsonElement state) ||
            state.ValueKind != JsonValueKind.Object ||
            !state.TryGetProperty("kind", out JsonElement kindElement) ||
            kindElement.ValueKind != JsonValueKind.String)
        {
            throw new AnalysisContractException("Each result must contain a string state.kind discriminator.");
        }

        return kindElement.GetString() switch
        {
            "static" => MapStatic(DeserializeElement<StaticResultWire>(element)),
            "load_step" => MapLoadStep(DeserializeElement<LoadStepResultWire>(element)),
            "mode" => MapModal(DeserializeElement<ModalResultWire>(element)),
            string unknown => throw new AnalysisContractException(
                $"Result state kind '{unknown}' is not supported."),
            null => throw new AnalysisContractException("Result state kind cannot be null."),
        };
    }

    private static T DeserializeElement<T>(JsonElement element)
    {
        return element.Deserialize<T>(Options)
            ?? throw new JsonException($"A {typeof(T).Name} result cannot be null.");
    }

    private static StaticAnalysisResult MapStatic(StaticResultWire value)
    {
        if (value.State.Kind != "static" || value.State.Index != 0)
        {
            throw new AnalysisContractException("A static result must use state kind 'static' and index 0.");
        }

        return new StaticAnalysisResult(
            value.CaseId,
            value.NodeDisplacements.Select(Map),
            value.SupportReactions.Select(Map),
            value.MemberSectionForces.Select(Map),
            value.ShellResults.Select(Map),
            value.SolidResults.Select(Map),
            new WarningDiagnostics(value.Diagnostics.Warnings));
    }

    private static LoadStepAnalysisResult MapLoadStep(LoadStepResultWire value)
    {
        if (value.State.Kind != "load_step")
        {
            throw new AnalysisContractException("A load-step result must use state kind 'load_step'.");
        }

        return new LoadStepAnalysisResult(
            value.CaseId,
            new LoadStepResultState(value.State.Index, value.State.LoadFactor, value.State.IsFinal),
            value.NodeDisplacements.Select(Map),
            value.SupportReactions.Select(Map),
            value.MemberSectionForces.Select(Map),
            value.ShellResults.Select(Map),
            value.SolidResults.Select(Map),
            new LoadStepDiagnostics(
                value.Diagnostics.Warnings,
                value.Diagnostics.Iterations.Select(iteration => new IterationDiagnostic(
                    iteration.Index,
                    iteration.ResidualNorm,
                    iteration.CorrectionNorm,
                    iteration.Converged))));
    }

    private static ModalAnalysisResult MapModal(ModalResultWire value)
    {
        if (value.State.Kind != "mode")
        {
            throw new AnalysisContractException("A modal result must use state kind 'mode'.");
        }

        return new ModalAnalysisResult(
            value.CaseId,
            new ModeResultState(
                value.State.Index,
                value.State.Eigenvalue,
                value.State.Frequency,
                value.State.DegeneracyGroup),
            value.NodeModeShapes.Select(Map),
            new ModalDiagnostics(
                value.Diagnostics.Warnings,
                value.Diagnostics.Normalization,
                value.Diagnostics.EigenvalueTolerance,
                value.Diagnostics.DegeneracyRelativeTolerance));
    }

    private static NodeDisplacement Map(NodeDisplacementWire value)
    {
        return new NodeDisplacement(value.NodeId, new DisplacementComponents(
            value.Components.Dx,
            value.Components.Dy,
            value.Components.Dz,
            value.Components.Rx,
            value.Components.Ry,
            value.Components.Rz));
    }

    private static SupportReaction Map(SupportReactionWire value)
    {
        return new SupportReaction(value.NodeId, Map(value.Components));
    }

    private static MemberSectionForces Map(MemberSectionForcesWire value)
    {
        return new MemberSectionForces(
            value.MemberId,
            value.Segments.Select(segment => new MemberSegmentResult(
                segment.SegmentId,
                segment.StationI,
                segment.StationJ,
                segment.Length,
                Map(segment.IEnd),
                Map(segment.JEnd))));
    }

    private static ShellResult Map(ShellResultWire value)
    {
        return new ShellResult(
            value.ElementId,
            value.Locations.Select(location => new ShellResultLocation(
                location.LocationId,
                new MembraneForce(
                    location.MembraneForce.Nx,
                    location.MembraneForce.Ny,
                    location.MembraneForce.Nxy),
                new BendingMoment(
                    location.BendingMoment.Mx,
                    location.BendingMoment.My,
                    location.BendingMoment.Mxy),
                new TransverseShear(
                    location.TransverseShear.Qx,
                    location.TransverseShear.Qy),
                new PlaneStress(
                    location.TopStress.Sx,
                    location.TopStress.Sy,
                    location.TopStress.Txy),
                new PlaneStress(
                    location.BottomStress.Sx,
                    location.BottomStress.Sy,
                    location.BottomStress.Txy))));
    }

    private static SolidResult Map(SolidResultWire value)
    {
        return new SolidResult(
            value.ElementId,
            value.Locations.Select(location => new SolidResultLocation(
                location.LocationId,
                new Stress3D(
                    location.Stress.Sx,
                    location.Stress.Sy,
                    location.Stress.Sz,
                    location.Stress.Txy,
                    location.Stress.Tyz,
                    location.Stress.Tzx),
                new Strain3D(
                    location.Strain.Ex,
                    location.Strain.Ey,
                    location.Strain.Ez,
                    location.Strain.Gxy,
                    location.Strain.Gyz,
                    location.Strain.Gzx))));
    }

    private static ForceComponents Map(ForceComponentsWire value)
    {
        return new ForceComponents(value.Fx, value.Fy, value.Fz, value.Mx, value.My, value.Mz);
    }

    private static Vector3Value Map(Vector3Wire value) => new(value.X, value.Y, value.Z);

    private static CoordinateFrame Map(CoordinateFrameWire value)
    {
        return new CoordinateFrame(Map(value.Origin), Map(value.XAxis), Map(value.YAxis), Map(value.ZAxis));
    }

    private static ShellElementType ParseShellElementType(string value)
    {
        return value switch
        {
            "triangle3" => ShellElementType.Triangle3,
            "quadrilateral4" => ShellElementType.Quadrilateral4,
            _ => throw new AnalysisContractException($"Shell element type '{value}' is not supported."),
        };
    }

    private static SolidElementType ParseSolidElementType(string value)
    {
        return value switch
        {
            "tetra4" => SolidElementType.Tetra4,
            "wedge6" => SolidElementType.Wedge6,
            "hexa8" => SolidElementType.Hexa8,
            "tetra10" => SolidElementType.Tetra10,
            "wedge15" => SolidElementType.Wedge15,
            "hexa20" => SolidElementType.Hexa20,
            _ => throw new AnalysisContractException($"Solid element type '{value}' is not supported."),
        };
    }

    private static void RejectDuplicateProperties(JsonElement element, string path, int depth)
    {
        if (depth > 256)
        {
            throw new AnalysisContractException("The JSON nesting depth exceeds 256.");
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new AnalysisContractException(
                        $"Duplicate JSON member '{property.Name}' at {path} is not allowed.");
                }

                RejectDuplicateProperties(property.Value, $"{path}.{property.Name}", depth + 1);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                RejectDuplicateProperties(item, $"{path}[{index}]", depth + 1);
                index++;
            }
        }
    }

    private sealed class AnalysisResultSetWire
    {
        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        [JsonPropertyName("schema_version")]
        public required string SchemaVersion { get; init; }

        [JsonPropertyName("units")]
        public required UnitsWire Units { get; init; }

        [JsonPropertyName("coordinate_system")]
        public required CoordinateSystemWire CoordinateSystem { get; init; }

        [JsonPropertyName("cases")]
        public required List<AnalysisCaseWire> Cases { get; init; }

        [JsonPropertyName("topology")]
        public required TopologyWire Topology { get; init; }

        [JsonPropertyName("results")]
        public required List<JsonElement> Results { get; init; }
    }

    private sealed class UnitsWire
    {
        [JsonPropertyName("system")]
        public required string System { get; init; }

        [JsonPropertyName("length")]
        public required string Length { get; init; }

        [JsonPropertyName("force")]
        public required string Force { get; init; }

        [JsonPropertyName("mass")]
        public required string Mass { get; init; }

        [JsonPropertyName("time")]
        public required string Time { get; init; }
    }

    private sealed class CoordinateSystemWire
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("handedness")]
        public required string Handedness { get; init; }

        [JsonPropertyName("axes")]
        public required List<string> Axes { get; init; }
    }

    private sealed class AnalysisCaseWire
    {
        [JsonPropertyName("case_id")]
        public required string CaseId { get; init; }

        [JsonPropertyName("name")]
        public required string Name { get; init; }

        [JsonPropertyName("symbol")]
        public required string Symbol { get; init; }

        [JsonPropertyName("analysis_type")]
        public required string AnalysisType { get; init; }

        [JsonPropertyName("support_node_ids")]
        public required List<string> SupportNodeIds { get; init; }
    }

    private sealed class TopologyWire
    {
        [JsonPropertyName("nodes")]
        public required List<TopologyNodeWire> Nodes { get; init; }

        [JsonPropertyName("members")]
        public required List<TopologyMemberWire> Members { get; init; }

        [JsonPropertyName("shell_elements")]
        public required List<TopologyShellElementWire> ShellElements { get; init; }

        [JsonPropertyName("solid_elements")]
        public required List<TopologySolidElementWire> SolidElements { get; init; }
    }

    private sealed class TopologyNodeWire
    {
        [JsonPropertyName("node_id")]
        public required string NodeId { get; init; }

        [JsonPropertyName("coordinates")]
        public required Vector3Wire Coordinates { get; init; }

        [JsonPropertyName("source_node_id")]
        public required string? SourceNodeId { get; init; }

        [JsonPropertyName("generated")]
        public required bool Generated { get; init; }
    }

    private sealed class Vector3Wire
    {
        [JsonPropertyName("x")]
        public required double X { get; init; }

        [JsonPropertyName("y")]
        public required double Y { get; init; }

        [JsonPropertyName("z")]
        public required double Z { get; init; }
    }

    private sealed class CoordinateFrameWire
    {
        [JsonPropertyName("origin")]
        public required Vector3Wire Origin { get; init; }

        [JsonPropertyName("x_axis")]
        public required Vector3Wire XAxis { get; init; }

        [JsonPropertyName("y_axis")]
        public required Vector3Wire YAxis { get; init; }

        [JsonPropertyName("z_axis")]
        public required Vector3Wire ZAxis { get; init; }
    }

    private sealed class MemberStationWire
    {
        [JsonPropertyName("station_id")]
        public required string StationId { get; init; }

        [JsonPropertyName("position")]
        public required double Position { get; init; }
    }

    private sealed class TopologyMemberWire
    {
        [JsonPropertyName("member_id")]
        public required string MemberId { get; init; }

        [JsonPropertyName("node_i")]
        public required string NodeI { get; init; }

        [JsonPropertyName("node_j")]
        public required string NodeJ { get; init; }

        [JsonPropertyName("local_frame")]
        public required CoordinateFrameWire LocalFrame { get; init; }

        [JsonPropertyName("stations")]
        public required List<MemberStationWire> Stations { get; init; }
    }

    private sealed class ShellResultLocationDefinitionWire
    {
        [JsonPropertyName("location_id")]
        public required string LocationId { get; init; }

        [JsonPropertyName("kind")]
        public required string Kind { get; init; }
    }

    private sealed class TopologyShellElementWire
    {
        [JsonPropertyName("element_id")]
        public required string ElementId { get; init; }

        [JsonPropertyName("element_type")]
        public required string ElementType { get; init; }

        [JsonPropertyName("node_ids")]
        public required List<string> NodeIds { get; init; }

        [JsonPropertyName("local_frame")]
        public required CoordinateFrameWire LocalFrame { get; init; }

        [JsonPropertyName("result_locations")]
        public required List<ShellResultLocationDefinitionWire> ResultLocations { get; init; }
    }

    private sealed class NaturalCoordinatesWire
    {
        [JsonPropertyName("xi")]
        public required double Xi { get; init; }

        [JsonPropertyName("eta")]
        public required double Eta { get; init; }

        [JsonPropertyName("zeta")]
        public required double Zeta { get; init; }
    }

    private sealed class SolidResultLocationDefinitionWire
    {
        [JsonPropertyName("location_id")]
        public required string LocationId { get; init; }

        [JsonPropertyName("natural_coordinates")]
        public required NaturalCoordinatesWire NaturalCoordinates { get; init; }
    }

    private sealed class TopologySolidElementWire
    {
        [JsonPropertyName("element_id")]
        public required string ElementId { get; init; }

        [JsonPropertyName("element_type")]
        public required string ElementType { get; init; }

        [JsonPropertyName("node_ids")]
        public required List<string> NodeIds { get; init; }

        [JsonPropertyName("coordinate_frame")]
        public required string CoordinateFrame { get; init; }

        [JsonPropertyName("result_locations")]
        public required List<SolidResultLocationDefinitionWire> ResultLocations { get; init; }
    }

    private sealed class StaticStateWire
    {
        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        [JsonPropertyName("index")]
        public required int Index { get; init; }
    }

    private sealed class LoadStepStateWire
    {
        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        [JsonPropertyName("index")]
        public required int Index { get; init; }

        [JsonPropertyName("load_factor")]
        public required double LoadFactor { get; init; }

        [JsonPropertyName("is_final")]
        public required bool IsFinal { get; init; }
    }

    private sealed class ModeStateWire
    {
        [JsonPropertyName("kind")]
        public required string Kind { get; init; }

        [JsonPropertyName("index")]
        public required int Index { get; init; }

        [JsonPropertyName("eigenvalue")]
        public required double Eigenvalue { get; init; }

        [JsonPropertyName("frequency")]
        public required double Frequency { get; init; }

        [JsonPropertyName("degeneracy_group")]
        public required int DegeneracyGroup { get; init; }
    }

    private abstract class ForceResultWireBase
    {
        [JsonPropertyName("case_id")]
        public required string CaseId { get; init; }

        [JsonPropertyName("node_displacements")]
        public required List<NodeDisplacementWire> NodeDisplacements { get; init; }

        [JsonPropertyName("support_reactions")]
        public required List<SupportReactionWire> SupportReactions { get; init; }

        [JsonPropertyName("member_section_forces")]
        public required List<MemberSectionForcesWire> MemberSectionForces { get; init; }

        [JsonPropertyName("shell_results")]
        public required List<ShellResultWire> ShellResults { get; init; }

        [JsonPropertyName("solid_results")]
        public required List<SolidResultWire> SolidResults { get; init; }
    }

    private sealed class StaticResultWire : ForceResultWireBase
    {
        [JsonPropertyName("state")]
        public required StaticStateWire State { get; init; }

        [JsonPropertyName("diagnostics")]
        public required WarningDiagnosticsWire Diagnostics { get; init; }
    }

    private sealed class LoadStepResultWire : ForceResultWireBase
    {
        [JsonPropertyName("state")]
        public required LoadStepStateWire State { get; init; }

        [JsonPropertyName("diagnostics")]
        public required LoadStepDiagnosticsWire Diagnostics { get; init; }
    }

    private sealed class ModalResultWire
    {
        [JsonPropertyName("case_id")]
        public required string CaseId { get; init; }

        [JsonPropertyName("state")]
        public required ModeStateWire State { get; init; }

        [JsonPropertyName("node_mode_shapes")]
        public required List<NodeDisplacementWire> NodeModeShapes { get; init; }

        [JsonPropertyName("diagnostics")]
        public required ModalDiagnosticsWire Diagnostics { get; init; }
    }

    private sealed class DisplacementComponentsWire
    {
        [JsonPropertyName("dx")]
        public required double Dx { get; init; }

        [JsonPropertyName("dy")]
        public required double Dy { get; init; }

        [JsonPropertyName("dz")]
        public required double Dz { get; init; }

        [JsonPropertyName("rx")]
        public required double Rx { get; init; }

        [JsonPropertyName("ry")]
        public required double Ry { get; init; }

        [JsonPropertyName("rz")]
        public required double Rz { get; init; }
    }

    private sealed class ForceComponentsWire
    {
        [JsonPropertyName("fx")]
        public required double Fx { get; init; }

        [JsonPropertyName("fy")]
        public required double Fy { get; init; }

        [JsonPropertyName("fz")]
        public required double Fz { get; init; }

        [JsonPropertyName("mx")]
        public required double Mx { get; init; }

        [JsonPropertyName("my")]
        public required double My { get; init; }

        [JsonPropertyName("mz")]
        public required double Mz { get; init; }
    }

    private sealed class NodeDisplacementWire
    {
        [JsonPropertyName("node_id")]
        public required string NodeId { get; init; }

        [JsonPropertyName("components")]
        public required DisplacementComponentsWire Components { get; init; }
    }

    private sealed class SupportReactionWire
    {
        [JsonPropertyName("node_id")]
        public required string NodeId { get; init; }

        [JsonPropertyName("components")]
        public required ForceComponentsWire Components { get; init; }
    }

    private sealed class MemberSegmentWire
    {
        [JsonPropertyName("segment_id")]
        public required string SegmentId { get; init; }

        [JsonPropertyName("station_i")]
        public required string StationI { get; init; }

        [JsonPropertyName("station_j")]
        public required string StationJ { get; init; }

        [JsonPropertyName("length")]
        public required double Length { get; init; }

        [JsonPropertyName("i_end")]
        public required ForceComponentsWire IEnd { get; init; }

        [JsonPropertyName("j_end")]
        public required ForceComponentsWire JEnd { get; init; }
    }

    private sealed class MemberSectionForcesWire
    {
        [JsonPropertyName("member_id")]
        public required string MemberId { get; init; }

        [JsonPropertyName("segments")]
        public required List<MemberSegmentWire> Segments { get; init; }
    }

    private sealed class MembraneForceWire
    {
        [JsonPropertyName("nx")]
        public required double Nx { get; init; }

        [JsonPropertyName("ny")]
        public required double Ny { get; init; }

        [JsonPropertyName("nxy")]
        public required double Nxy { get; init; }
    }

    private sealed class BendingMomentWire
    {
        [JsonPropertyName("mx")]
        public required double Mx { get; init; }

        [JsonPropertyName("my")]
        public required double My { get; init; }

        [JsonPropertyName("mxy")]
        public required double Mxy { get; init; }
    }

    private sealed class TransverseShearWire
    {
        [JsonPropertyName("qx")]
        public required double Qx { get; init; }

        [JsonPropertyName("qy")]
        public required double Qy { get; init; }
    }

    private sealed class PlaneStressWire
    {
        [JsonPropertyName("sx")]
        public required double Sx { get; init; }

        [JsonPropertyName("sy")]
        public required double Sy { get; init; }

        [JsonPropertyName("txy")]
        public required double Txy { get; init; }
    }

    private sealed class ShellResultLocationWire
    {
        [JsonPropertyName("location_id")]
        public required string LocationId { get; init; }

        [JsonPropertyName("membrane_force")]
        public required MembraneForceWire MembraneForce { get; init; }

        [JsonPropertyName("bending_moment")]
        public required BendingMomentWire BendingMoment { get; init; }

        [JsonPropertyName("transverse_shear")]
        public required TransverseShearWire TransverseShear { get; init; }

        [JsonPropertyName("top_stress")]
        public required PlaneStressWire TopStress { get; init; }

        [JsonPropertyName("bottom_stress")]
        public required PlaneStressWire BottomStress { get; init; }
    }

    private sealed class ShellResultWire
    {
        [JsonPropertyName("element_id")]
        public required string ElementId { get; init; }

        [JsonPropertyName("locations")]
        public required List<ShellResultLocationWire> Locations { get; init; }
    }

    private sealed class Stress3DWire
    {
        [JsonPropertyName("sx")]
        public required double Sx { get; init; }

        [JsonPropertyName("sy")]
        public required double Sy { get; init; }

        [JsonPropertyName("sz")]
        public required double Sz { get; init; }

        [JsonPropertyName("txy")]
        public required double Txy { get; init; }

        [JsonPropertyName("tyz")]
        public required double Tyz { get; init; }

        [JsonPropertyName("tzx")]
        public required double Tzx { get; init; }
    }

    private sealed class Strain3DWire
    {
        [JsonPropertyName("ex")]
        public required double Ex { get; init; }

        [JsonPropertyName("ey")]
        public required double Ey { get; init; }

        [JsonPropertyName("ez")]
        public required double Ez { get; init; }

        [JsonPropertyName("gxy")]
        public required double Gxy { get; init; }

        [JsonPropertyName("gyz")]
        public required double Gyz { get; init; }

        [JsonPropertyName("gzx")]
        public required double Gzx { get; init; }
    }

    private sealed class SolidResultLocationWire
    {
        [JsonPropertyName("location_id")]
        public required string LocationId { get; init; }

        [JsonPropertyName("stress")]
        public required Stress3DWire Stress { get; init; }

        [JsonPropertyName("strain")]
        public required Strain3DWire Strain { get; init; }
    }

    private sealed class SolidResultWire
    {
        [JsonPropertyName("element_id")]
        public required string ElementId { get; init; }

        [JsonPropertyName("locations")]
        public required List<SolidResultLocationWire> Locations { get; init; }
    }

    private sealed class WarningDiagnosticsWire
    {
        [JsonPropertyName("warnings")]
        public required List<string> Warnings { get; init; }
    }

    private sealed class IterationDiagnosticWire
    {
        [JsonPropertyName("index")]
        public required int Index { get; init; }

        [JsonPropertyName("residual_norm")]
        public required double ResidualNorm { get; init; }

        [JsonPropertyName("correction_norm")]
        public required double CorrectionNorm { get; init; }

        [JsonPropertyName("converged")]
        public required bool Converged { get; init; }
    }

    private sealed class LoadStepDiagnosticsWire
    {
        [JsonPropertyName("warnings")]
        public required List<string> Warnings { get; init; }

        [JsonPropertyName("iterations")]
        public required List<IterationDiagnosticWire> Iterations { get; init; }
    }

    private sealed class ModalDiagnosticsWire
    {
        [JsonPropertyName("warnings")]
        public required List<string> Warnings { get; init; }

        [JsonPropertyName("normalization")]
        public required string Normalization { get; init; }

        [JsonPropertyName("eigenvalue_tolerance")]
        public required double EigenvalueTolerance { get; init; }

        [JsonPropertyName("degeneracy_relative_tolerance")]
        public required double DegeneracyRelativeTolerance { get; init; }
    }
}

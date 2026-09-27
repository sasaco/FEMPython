using System.Collections.ObjectModel;

namespace FrameWebforCS.calculation;

internal static class Frozen
{
    internal static IReadOnlyList<T> List<T>(IEnumerable<T> values, string paramName)
    {
        ArgumentNullException.ThrowIfNull(values, paramName);
        return new ReadOnlyCollection<T>(values.ToArray());
    }
}

public enum AnalysisType
{
    Static,
    MaterialNonlinear,
    Modal,
}

public enum ResultStateKind
{
    Static,
    LoadStep,
    Mode,
}

public enum ShellElementType
{
    Triangle3,
    Quadrilateral4,
}

public enum SolidElementType
{
    Tetra4,
    Wedge6,
    Hexa8,
    Tetra10,
    Wedge15,
    Hexa20,
}

public sealed record AnalysisUnits(string System, string Length, string Force, string Mass, string Time);

public sealed class CoordinateSystem
{
    public CoordinateSystem(string name, string handedness, IEnumerable<string> axes)
    {
        Name = name;
        Handedness = handedness;
        Axes = Frozen.List(axes, nameof(axes));
    }

    public string Name { get; }

    public string Handedness { get; }

    public IReadOnlyList<string> Axes { get; }
}

public sealed class AnalysisCase
{
    public AnalysisCase(
        string caseId,
        string name,
        string symbol,
        AnalysisType analysisType,
        IEnumerable<string> supportNodeIds)
    {
        CaseId = caseId;
        Name = name;
        Symbol = symbol;
        AnalysisType = analysisType;
        SupportNodeIds = Frozen.List(supportNodeIds, nameof(supportNodeIds));
    }

    public string CaseId { get; }

    public string Name { get; }

    public string Symbol { get; }

    public AnalysisType AnalysisType { get; }

    public IReadOnlyList<string> SupportNodeIds { get; }
}

public sealed record Vector3Value(double X, double Y, double Z);

public sealed record CoordinateFrame(
    Vector3Value Origin,
    Vector3Value XAxis,
    Vector3Value YAxis,
    Vector3Value ZAxis);

public sealed record TopologyNode(
    string NodeId,
    Vector3Value Coordinates,
    string? SourceNodeId,
    bool Generated);

public sealed record MemberStation(string StationId, double Position);

public sealed class TopologyMember
{
    public TopologyMember(
        string memberId,
        string nodeI,
        string nodeJ,
        CoordinateFrame localFrame,
        IEnumerable<MemberStation> stations)
    {
        MemberId = memberId;
        NodeI = nodeI;
        NodeJ = nodeJ;
        LocalFrame = localFrame;
        Stations = Frozen.List(stations, nameof(stations));
    }

    public string MemberId { get; }

    public string NodeI { get; }

    public string NodeJ { get; }

    public CoordinateFrame LocalFrame { get; }

    public IReadOnlyList<MemberStation> Stations { get; }
}

public sealed record ShellResultLocationDefinition(string LocationId, string Kind);

public sealed class TopologyShellElement
{
    public TopologyShellElement(
        string elementId,
        ShellElementType elementType,
        IEnumerable<string> nodeIds,
        CoordinateFrame localFrame,
        IEnumerable<ShellResultLocationDefinition> resultLocations)
    {
        ElementId = elementId;
        ElementType = elementType;
        NodeIds = Frozen.List(nodeIds, nameof(nodeIds));
        LocalFrame = localFrame;
        ResultLocations = Frozen.List(resultLocations, nameof(resultLocations));
    }

    public string ElementId { get; }

    public ShellElementType ElementType { get; }

    public IReadOnlyList<string> NodeIds { get; }

    public CoordinateFrame LocalFrame { get; }

    public IReadOnlyList<ShellResultLocationDefinition> ResultLocations { get; }
}

public sealed record NaturalCoordinates(double Xi, double Eta, double Zeta);

public sealed record SolidResultLocationDefinition(string LocationId, NaturalCoordinates NaturalCoordinates);

public sealed class TopologySolidElement
{
    public TopologySolidElement(
        string elementId,
        SolidElementType elementType,
        IEnumerable<string> nodeIds,
        string coordinateFrame,
        IEnumerable<SolidResultLocationDefinition> resultLocations)
    {
        ElementId = elementId;
        ElementType = elementType;
        NodeIds = Frozen.List(nodeIds, nameof(nodeIds));
        CoordinateFrame = coordinateFrame;
        ResultLocations = Frozen.List(resultLocations, nameof(resultLocations));
    }

    public string ElementId { get; }

    public SolidElementType ElementType { get; }

    public IReadOnlyList<string> NodeIds { get; }

    public string CoordinateFrame { get; }

    public IReadOnlyList<SolidResultLocationDefinition> ResultLocations { get; }
}

public sealed class AnalysisTopology
{
    public AnalysisTopology(
        IEnumerable<TopologyNode> nodes,
        IEnumerable<TopologyMember> members,
        IEnumerable<TopologyShellElement> shellElements,
        IEnumerable<TopologySolidElement> solidElements)
    {
        Nodes = Frozen.List(nodes, nameof(nodes));
        Members = Frozen.List(members, nameof(members));
        ShellElements = Frozen.List(shellElements, nameof(shellElements));
        SolidElements = Frozen.List(solidElements, nameof(solidElements));
    }

    public IReadOnlyList<TopologyNode> Nodes { get; }

    public IReadOnlyList<TopologyMember> Members { get; }

    public IReadOnlyList<TopologyShellElement> ShellElements { get; }

    public IReadOnlyList<TopologySolidElement> SolidElements { get; }
}

public sealed record DisplacementComponents(
    double Dx,
    double Dy,
    double Dz,
    double Rx,
    double Ry,
    double Rz);

public sealed record ForceComponents(
    double Fx,
    double Fy,
    double Fz,
    double Mx,
    double My,
    double Mz);

public sealed record NodeDisplacement(string NodeId, DisplacementComponents Components);

public sealed record SupportReaction(string NodeId, ForceComponents Components);

public sealed record MemberSegmentResult(
    string SegmentId,
    string StationI,
    string StationJ,
    double Length,
    ForceComponents IEnd,
    ForceComponents JEnd);

public sealed class MemberSectionForces
{
    public MemberSectionForces(string memberId, IEnumerable<MemberSegmentResult> segments)
    {
        MemberId = memberId;
        Segments = Frozen.List(segments, nameof(segments));
    }

    public string MemberId { get; }

    public IReadOnlyList<MemberSegmentResult> Segments { get; }
}

public sealed record MembraneForce(double Nx, double Ny, double Nxy);

public sealed record BendingMoment(double Mx, double My, double Mxy);

public sealed record TransverseShear(double Qx, double Qy);

public sealed record PlaneStress(double Sx, double Sy, double Txy);

public sealed record ShellResultLocation(
    string LocationId,
    MembraneForce MembraneForce,
    BendingMoment BendingMoment,
    TransverseShear TransverseShear,
    PlaneStress TopStress,
    PlaneStress BottomStress);

public sealed class ShellResult
{
    public ShellResult(string elementId, IEnumerable<ShellResultLocation> locations)
    {
        ElementId = elementId;
        Locations = Frozen.List(locations, nameof(locations));
    }

    public string ElementId { get; }

    public IReadOnlyList<ShellResultLocation> Locations { get; }
}

public sealed record Stress3D(double Sx, double Sy, double Sz, double Txy, double Tyz, double Tzx);

public sealed record Strain3D(double Ex, double Ey, double Ez, double Gxy, double Gyz, double Gzx);

public sealed record SolidResultLocation(string LocationId, Stress3D Stress, Strain3D Strain);

public sealed class SolidResult
{
    public SolidResult(string elementId, IEnumerable<SolidResultLocation> locations)
    {
        ElementId = elementId;
        Locations = Frozen.List(locations, nameof(locations));
    }

    public string ElementId { get; }

    public IReadOnlyList<SolidResultLocation> Locations { get; }
}

public abstract record ResultState(ResultStateKind Kind, int Index);

public sealed record StaticResultState() : ResultState(ResultStateKind.Static, 0);

public sealed record LoadStepResultState(int StepIndex, double LoadFactor, bool IsFinal)
    : ResultState(ResultStateKind.LoadStep, StepIndex);

public sealed record ModeResultState(
    int ModeIndex,
    double Eigenvalue,
    double Frequency,
    int DegeneracyGroup)
    : ResultState(ResultStateKind.Mode, ModeIndex);

public sealed class WarningDiagnostics
{
    public WarningDiagnostics(IEnumerable<string> warnings)
    {
        Warnings = Frozen.List(warnings, nameof(warnings));
    }

    public IReadOnlyList<string> Warnings { get; }
}

public sealed record IterationDiagnostic(
    int Index,
    double ResidualNorm,
    double CorrectionNorm,
    bool Converged);

public sealed class LoadStepDiagnostics
{
    public LoadStepDiagnostics(IEnumerable<string> warnings, IEnumerable<IterationDiagnostic> iterations)
    {
        Warnings = Frozen.List(warnings, nameof(warnings));
        Iterations = Frozen.List(iterations, nameof(iterations));
    }

    public IReadOnlyList<string> Warnings { get; }

    public IReadOnlyList<IterationDiagnostic> Iterations { get; }
}

public sealed class ModalDiagnostics
{
    public ModalDiagnostics(
        IEnumerable<string> warnings,
        string normalization,
        double eigenvalueTolerance,
        double degeneracyRelativeTolerance)
    {
        Warnings = Frozen.List(warnings, nameof(warnings));
        Normalization = normalization;
        EigenvalueTolerance = eigenvalueTolerance;
        DegeneracyRelativeTolerance = degeneracyRelativeTolerance;
    }

    public IReadOnlyList<string> Warnings { get; }

    public string Normalization { get; }

    public double EigenvalueTolerance { get; }

    public double DegeneracyRelativeTolerance { get; }
}

public abstract class AnalysisResult
{
    protected AnalysisResult(string caseId, ResultState state)
    {
        CaseId = caseId;
        State = state;
    }

    public string CaseId { get; }

    public ResultState State { get; }

    public ResultCoordinate Coordinate => new(CaseId, State.Kind, State.Index);
}

public abstract class ForceAnalysisResult : AnalysisResult
{
    protected ForceAnalysisResult(
        string caseId,
        ResultState state,
        IEnumerable<NodeDisplacement> nodeDisplacements,
        IEnumerable<SupportReaction> supportReactions,
        IEnumerable<MemberSectionForces> memberSectionForces,
        IEnumerable<ShellResult> shellResults,
        IEnumerable<SolidResult> solidResults)
        : base(caseId, state)
    {
        NodeDisplacements = Frozen.List(nodeDisplacements, nameof(nodeDisplacements));
        SupportReactions = Frozen.List(supportReactions, nameof(supportReactions));
        MemberSectionForces = Frozen.List(memberSectionForces, nameof(memberSectionForces));
        ShellResults = Frozen.List(shellResults, nameof(shellResults));
        SolidResults = Frozen.List(solidResults, nameof(solidResults));
    }

    public IReadOnlyList<NodeDisplacement> NodeDisplacements { get; }

    public IReadOnlyList<SupportReaction> SupportReactions { get; }

    public IReadOnlyList<MemberSectionForces> MemberSectionForces { get; }

    public IReadOnlyList<ShellResult> ShellResults { get; }

    public IReadOnlyList<SolidResult> SolidResults { get; }
}

public sealed class StaticAnalysisResult : ForceAnalysisResult
{
    public StaticAnalysisResult(
        string caseId,
        IEnumerable<NodeDisplacement> nodeDisplacements,
        IEnumerable<SupportReaction> supportReactions,
        IEnumerable<MemberSectionForces> memberSectionForces,
        IEnumerable<ShellResult> shellResults,
        IEnumerable<SolidResult> solidResults,
        WarningDiagnostics diagnostics)
        : base(
            caseId,
            new StaticResultState(),
            nodeDisplacements,
            supportReactions,
            memberSectionForces,
            shellResults,
            solidResults)
    {
        Diagnostics = diagnostics;
    }

    public WarningDiagnostics Diagnostics { get; }
}

public sealed class LoadStepAnalysisResult : ForceAnalysisResult
{
    public LoadStepAnalysisResult(
        string caseId,
        LoadStepResultState state,
        IEnumerable<NodeDisplacement> nodeDisplacements,
        IEnumerable<SupportReaction> supportReactions,
        IEnumerable<MemberSectionForces> memberSectionForces,
        IEnumerable<ShellResult> shellResults,
        IEnumerable<SolidResult> solidResults,
        LoadStepDiagnostics diagnostics)
        : base(
            caseId,
            state,
            nodeDisplacements,
            supportReactions,
            memberSectionForces,
            shellResults,
            solidResults)
    {
        Diagnostics = diagnostics;
    }

    public new LoadStepResultState State => (LoadStepResultState)base.State;

    public LoadStepDiagnostics Diagnostics { get; }
}

public sealed class ModalAnalysisResult : AnalysisResult
{
    public ModalAnalysisResult(
        string caseId,
        ModeResultState state,
        IEnumerable<NodeDisplacement> nodeModeShapes,
        ModalDiagnostics diagnostics)
        : base(caseId, state)
    {
        NodeModeShapes = Frozen.List(nodeModeShapes, nameof(nodeModeShapes));
        Diagnostics = diagnostics;
    }

    public new ModeResultState State => (ModeResultState)base.State;

    public IReadOnlyList<NodeDisplacement> NodeModeShapes { get; }

    public ModalDiagnostics Diagnostics { get; }
}

public sealed class AnalysisResultSet
{
    public const string ContractKind = "analysis_result_set";
    public const string ContractVersion = "1.0";

    public AnalysisResultSet(
        string kind,
        string schemaVersion,
        AnalysisUnits units,
        CoordinateSystem coordinateSystem,
        IEnumerable<AnalysisCase> cases,
        AnalysisTopology topology,
        IEnumerable<AnalysisResult> results)
    {
        Kind = kind;
        SchemaVersion = schemaVersion;
        Units = units;
        CoordinateSystem = coordinateSystem;
        Cases = Frozen.List(cases, nameof(cases));
        Topology = topology;
        Results = Frozen.List(results, nameof(results));
    }

    public string Kind { get; }

    public string SchemaVersion { get; }

    public AnalysisUnits Units { get; }

    public CoordinateSystem CoordinateSystem { get; }

    public IReadOnlyList<AnalysisCase> Cases { get; }

    public AnalysisTopology Topology { get; }

    public IReadOnlyList<AnalysisResult> Results { get; }
}

public readonly record struct ResultCoordinate(string CaseId, ResultStateKind StateKind, int StateIndex);

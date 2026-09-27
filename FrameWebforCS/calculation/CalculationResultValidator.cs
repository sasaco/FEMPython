namespace FrameWebforCS.calculation;

public static class AnalysisResultSetValidator
{
    private const double FrameTolerance = 1e-8;
    private const double ModalFrequencyRelativeTolerance = 1e-10;

    public static void Validate(AnalysisResultSet resultSet)
    {
        ArgumentNullException.ThrowIfNull(resultSet);
        Require(resultSet.Kind == AnalysisResultSet.ContractKind, "kind must be 'analysis_result_set'.");
        Require(resultSet.SchemaVersion == AnalysisResultSet.ContractVersion, "schema_version must be '1.0'.");
        ValidateUnits(resultSet.Units);
        ValidateCoordinateSystem(resultSet.CoordinateSystem);

        Require(resultSet.Cases.Count >= 1, "cases must contain at least one item.");
        ValidateTopology(resultSet.Topology);

        HashSet<string> nodeIds = IdSet(resultSet.Topology.Nodes.Select(node => node.NodeId));
        Dictionary<string, int> nodeOrder = resultSet.Topology.Nodes
            .Select((node, index) => (node.NodeId, index))
            .ToDictionary(value => value.NodeId, value => value.index, StringComparer.Ordinal);
        Dictionary<string, AnalysisCase> cases = new(StringComparer.Ordinal);
        foreach (AnalysisCase resultCase in resultSet.Cases)
        {
            ValidateId(resultCase.CaseId, "case_id");
            ValidateId(resultCase.Name, $"case '{resultCase.CaseId}' name");
            ValidateId(resultCase.Symbol, $"case '{resultCase.CaseId}' symbol");
            Require(cases.TryAdd(resultCase.CaseId, resultCase), $"Duplicate case_id '{resultCase.CaseId}'.");
            EnsureUniqueIds(resultCase.SupportNodeIds, $"case '{resultCase.CaseId}' support_node_ids");
            Require(
                resultCase.SupportNodeIds.All(nodeIds.Contains),
                $"Case '{resultCase.CaseId}' references an unknown support node.");
            Require(
                resultCase.SupportNodeIds.Select(id => nodeOrder[id]).SequenceEqual(
                    resultCase.SupportNodeIds.Select(id => nodeOrder[id]).Order()),
                $"Case '{resultCase.CaseId}' support nodes must follow topology order.");
        }

        ValidateResults(resultSet, cases);
    }

    private static void ValidateUnits(AnalysisUnits units)
    {
        ArgumentNullException.ThrowIfNull(units);
        ValidateId(units.System, "units.system");
        ValidateId(units.Length, "units.length");
        ValidateId(units.Force, "units.force");
        ValidateId(units.Mass, "units.mass");
        ValidateId(units.Time, "units.time");
    }

    private static void ValidateCoordinateSystem(CoordinateSystem coordinateSystem)
    {
        ArgumentNullException.ThrowIfNull(coordinateSystem);
        Require(coordinateSystem.Name == "global_cartesian", "coordinate_system.name is invalid.");
        Require(coordinateSystem.Handedness == "right", "coordinate_system.handedness is invalid.");
        Require(
            coordinateSystem.Axes.SequenceEqual(["x", "y", "z"], StringComparer.Ordinal),
            "coordinate_system.axes must be exactly [x, y, z].");
    }

    private static void ValidateTopology(AnalysisTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);
        HashSet<string> nodeIds = UniqueIdSet(topology.Nodes.Select(node => node.NodeId), "topology node");
        HashSet<string> memberIds = UniqueIdSet(topology.Members.Select(member => member.MemberId), "topology member");
        _ = memberIds;
        UniqueIdSet(topology.ShellElements.Select(element => element.ElementId), "topology shell element");
        UniqueIdSet(topology.SolidElements.Select(element => element.ElementId), "topology solid element");

        foreach (TopologyNode node in topology.Nodes)
        {
            ValidateVector(node.Coordinates, $"node '{node.NodeId}' coordinates");
            if (node.SourceNodeId is not null)
            {
                ValidateId(node.SourceNodeId, $"node '{node.NodeId}' source_node_id");
                Require(nodeIds.Contains(node.SourceNodeId),
                    $"Node '{node.NodeId}' source_node_id references an unknown node.");
            }
        }

        foreach (TopologyMember member in topology.Members)
        {
            Require(nodeIds.Contains(member.NodeI), $"Member '{member.MemberId}' has an unknown node_i.");
            Require(nodeIds.Contains(member.NodeJ), $"Member '{member.MemberId}' has an unknown node_j.");
            Require(member.NodeI != member.NodeJ,
                $"Member '{member.MemberId}' endpoints must be distinct.");
            ValidateFrame(member.LocalFrame, $"member '{member.MemberId}' local_frame");
            Require(member.Stations.Count >= 2, $"Member '{member.MemberId}' requires at least two stations.");
            UniqueIdSet(member.Stations.Select(station => station.StationId), $"member '{member.MemberId}' station");
            for (int index = 0; index < member.Stations.Count; index++)
            {
                MemberStation station = member.Stations[index];
                Require(station.StationId == $"S{index}",
                    $"Member '{member.MemberId}' station {index} must be named S{index}.");
                RequireFinite(station.Position, $"member '{member.MemberId}' station position");
                Require(station.Position >= 0, $"Member '{member.MemberId}' has a negative station position.");
                if (index > 0)
                {
                    Require(station.Position > member.Stations[index - 1].Position,
                        $"Member '{member.MemberId}' station positions must be strictly ascending.");
                }
            }
        }

        foreach (TopologyShellElement shell in topology.ShellElements)
        {
            int expectedNodes = shell.ElementType == ShellElementType.Triangle3 ? 3 : 4;
            Require(shell.NodeIds.Count == expectedNodes, $"Shell '{shell.ElementId}' has the wrong node count.");
            EnsureUniqueIds(shell.NodeIds, $"shell '{shell.ElementId}' node_ids");
            Require(shell.NodeIds.All(nodeIds.Contains), $"Shell '{shell.ElementId}' references an unknown node.");
            ValidateFrame(shell.LocalFrame, $"shell '{shell.ElementId}' local_frame");
            Require(shell.ResultLocations.Count == 1, $"Shell '{shell.ElementId}' requires one result location.");
            ShellResultLocationDefinition location = shell.ResultLocations[0];
            Require(
                location.LocationId == "element_average" && location.Kind == "element_average",
                $"Shell '{shell.ElementId}' has an unsupported result location.");
        }

        foreach (TopologySolidElement solid in topology.SolidElements)
        {
            int expectedNodes = solid.ElementType switch
            {
                SolidElementType.Tetra4 => 4,
                SolidElementType.Wedge6 => 6,
                SolidElementType.Hexa8 => 8,
                SolidElementType.Tetra10 => 10,
                SolidElementType.Wedge15 => 15,
                SolidElementType.Hexa20 => 20,
                _ => throw new AnalysisContractException("Unsupported solid element type."),
            };
            Require(solid.NodeIds.Count == expectedNodes, $"Solid '{solid.ElementId}' has the wrong node count.");
            EnsureUniqueIds(solid.NodeIds, $"solid '{solid.ElementId}' node_ids");
            Require(solid.NodeIds.All(nodeIds.Contains), $"Solid '{solid.ElementId}' references an unknown node.");
            Require(solid.CoordinateFrame == "global", $"Solid '{solid.ElementId}' must use the global frame.");
            Require(solid.ResultLocations.Count >= 1, $"Solid '{solid.ElementId}' requires result locations.");
            UniqueIdSet(
                solid.ResultLocations.Select(location => location.LocationId),
                $"solid '{solid.ElementId}' result location");
            for (int index = 0; index < solid.ResultLocations.Count; index++)
            {
                SolidResultLocationDefinition location = solid.ResultLocations[index];
                Require(location.LocationId == $"GP{index}",
                    $"Solid '{solid.ElementId}' result location {index} must be named GP{index}.");
                RequireFinite(location.NaturalCoordinates.Xi, "natural coordinate xi");
                RequireFinite(location.NaturalCoordinates.Eta, "natural coordinate eta");
                RequireFinite(location.NaturalCoordinates.Zeta, "natural coordinate zeta");
            }
        }
    }

    private static void ValidateResults(
        AnalysisResultSet resultSet,
        IReadOnlyDictionary<string, AnalysisCase> cases)
    {
        Require(resultSet.Results.Count >= 1, "results must contain at least one item.");
        HashSet<ResultCoordinate> coordinates = [];
        Dictionary<string, List<AnalysisResult>> byCase = new(StringComparer.Ordinal);
        Dictionary<string, int> caseOrder = resultSet.Cases
            .Select((item, index) => (item.CaseId, index))
            .ToDictionary(item => item.CaseId, item => item.index, StringComparer.Ordinal);
        int priorCaseIndex = -1;

        foreach (AnalysisResult result in resultSet.Results)
        {
            ValidateId(result.CaseId, "result case_id");
            if (!cases.TryGetValue(result.CaseId, out AnalysisCase? resultCase))
            {
                throw new AnalysisContractException($"Result references unknown case '{result.CaseId}'.");
            }
            Require(coordinates.Add(result.Coordinate), $"Duplicate result coordinate '{result.Coordinate}'.");
            int currentCaseIndex = caseOrder[result.CaseId];
            Require(currentCaseIndex >= priorCaseIndex, "Results are not in deterministic case-major order.");
            priorCaseIndex = currentCaseIndex;

            if (!byCase.TryGetValue(result.CaseId, out List<AnalysisResult>? values))
            {
                values = [];
                byCase.Add(result.CaseId, values);
            }

            values.Add(result);
            ValidateResultVariant(resultSet.Topology, resultCase, result);
        }

        foreach (AnalysisCase resultCase in resultSet.Cases)
        {
            Require(byCase.TryGetValue(resultCase.CaseId, out List<AnalysisResult>? results),
                $"Case '{resultCase.CaseId}' has no result.");
            ValidateCaseSequence(resultCase, results!);
        }
    }

    private static void ValidateCaseSequence(AnalysisCase resultCase, IReadOnlyList<AnalysisResult> results)
    {
        switch (resultCase.AnalysisType)
        {
            case AnalysisType.Static:
                Require(results.Count == 1 && results[0] is StaticAnalysisResult,
                    $"Static case '{resultCase.CaseId}' must contain exactly one static result.");
                break;
            case AnalysisType.MaterialNonlinear:
            {
                Require(results.All(result => result is LoadStepAnalysisResult),
                    $"Nonlinear case '{resultCase.CaseId}' may contain only load-step results.");
                for (int index = 0; index < results.Count; index++)
                {
                    Require(results[index].State.Index == index,
                        $"Nonlinear case '{resultCase.CaseId}' load-step indices must be contiguous from zero.");
                }

                LoadStepAnalysisResult[] steps = results.Cast<LoadStepAnalysisResult>().ToArray();
                Require(steps.Count(step => step.State.IsFinal) == 1,
                    $"Nonlinear case '{resultCase.CaseId}' must contain exactly one final step.");
                Require(steps[^1].State.IsFinal,
                    $"Nonlinear case '{resultCase.CaseId}' final marker must be on its last step.");
                break;
            }
            case AnalysisType.Modal:
                Require(results.All(result => result is ModalAnalysisResult),
                    $"Modal case '{resultCase.CaseId}' may contain only modal results.");
                for (int index = 0; index < results.Count; index++)
                {
                    Require(results[index].State.Index == index,
                        $"Modal case '{resultCase.CaseId}' mode indices must be contiguous from zero.");
                }

                ModeResultState[] states = results.Cast<ModalAnalysisResult>().Select(result => result.State).ToArray();
                Require(states.Select(state => state.Eigenvalue).SequenceEqual(
                    states.Select(state => state.Eigenvalue).Order()),
                    $"Modal case '{resultCase.CaseId}' eigenvalues must be ascending.");
                Require(states[0].DegeneracyGroup == 0,
                    $"Modal case '{resultCase.CaseId}' degeneracy groups must start at zero.");
                for (int index = 1; index < states.Length; index++)
                {
                    int difference = states[index].DegeneracyGroup - states[index - 1].DegeneracyGroup;
                    Require(difference is 0 or 1,
                        $"Modal case '{resultCase.CaseId}' degeneracy groups must be contiguous.");
                }

                break;
            default:
                throw new AnalysisContractException("Unsupported analysis type.");
        }
    }

    private static void ValidateResultVariant(
        AnalysisTopology topology,
        AnalysisCase resultCase,
        AnalysisResult result)
    {
        Require(result.State.Index >= 0, $"Result '{result.Coordinate}' has a negative state index.");
        if (result is ForceAnalysisResult forceResult)
        {
            ValidateForceResult(topology, resultCase, forceResult);
        }

        switch (result)
        {
            case StaticAnalysisResult staticResult:
                Require(resultCase.AnalysisType == AnalysisType.Static, "Static result is assigned to a non-static case.");
                ValidateWarnings(staticResult.Diagnostics.Warnings);
                break;
            case LoadStepAnalysisResult loadStep:
                Require(resultCase.AnalysisType == AnalysisType.MaterialNonlinear,
                    "Load-step result is assigned to a non-nonlinear case.");
                RequireFinite(loadStep.State.LoadFactor, "load_factor");
                ValidateWarnings(loadStep.Diagnostics.Warnings);
                for (int index = 0; index < loadStep.Diagnostics.Iterations.Count; index++)
                {
                    IterationDiagnostic iteration = loadStep.Diagnostics.Iterations[index];
                    Require(iteration.Index == index, "Iteration indices must be contiguous from zero.");
                    RequireFinite(iteration.ResidualNorm, "residual_norm");
                    RequireFinite(iteration.CorrectionNorm, "correction_norm");
                    Require(iteration.ResidualNorm >= 0 && iteration.CorrectionNorm >= 0,
                        "Iteration norms cannot be negative.");
                }

                break;
            case ModalAnalysisResult modal:
                Require(resultCase.AnalysisType == AnalysisType.Modal, "Modal result is assigned to a non-modal case.");
                ValidateExactCoverage(
                    modal.NodeModeShapes.Select(value => value.NodeId),
                    topology.Nodes.Select(value => value.NodeId),
                    "modal node_mode_shapes");
                foreach (NodeDisplacement shape in modal.NodeModeShapes)
                {
                    ValidateDisplacement(shape.Components);
                }

                RequireFinite(modal.State.Eigenvalue, "eigenvalue");
                RequireFinite(modal.State.Frequency, "frequency");
                Require(modal.State.Eigenvalue > 0 && modal.State.Frequency > 0,
                    "Modal eigenvalue and frequency must be positive.");
                double expectedFrequency = Math.Sqrt(modal.State.Eigenvalue) / (2 * Math.PI);
                Require(
                    Math.Abs(modal.State.Frequency - expectedFrequency) <=
                    ModalFrequencyRelativeTolerance * Math.Max(1, Math.Abs(expectedFrequency)),
                    "Modal frequency must equal sqrt(eigenvalue) / (2*pi).");
                Require(modal.State.DegeneracyGroup >= 0, "Modal degeneracy_group cannot be negative.");
                ValidateWarnings(modal.Diagnostics.Warnings);
                Require(modal.Diagnostics.Normalization == "mass", "Modal normalization must be 'mass'.");
                RequireFinite(modal.Diagnostics.EigenvalueTolerance, "eigenvalue_tolerance");
                Require(modal.Diagnostics.EigenvalueTolerance >= 0, "eigenvalue_tolerance cannot be negative.");
                Require(modal.Diagnostics.DegeneracyRelativeTolerance == 1e-8,
                    "degeneracy_relative_tolerance must be 1e-8.");
                break;
            default:
                throw new AnalysisContractException("Unsupported result variant.");
        }
    }

    private static void ValidateForceResult(
        AnalysisTopology topology,
        AnalysisCase resultCase,
        ForceAnalysisResult result)
    {
        ValidateExactCoverage(
            result.NodeDisplacements.Select(value => value.NodeId),
            topology.Nodes.Select(value => value.NodeId),
            "node_displacements");
        ValidateExactCoverage(
            result.SupportReactions.Select(value => value.NodeId),
            resultCase.SupportNodeIds,
            "support_reactions");
        ValidateExactCoverage(
            result.MemberSectionForces.Select(value => value.MemberId),
            topology.Members.Select(value => value.MemberId),
            "member_section_forces");
        ValidateExactCoverage(
            result.ShellResults.Select(value => value.ElementId),
            topology.ShellElements.Select(value => value.ElementId),
            "shell_results");
        ValidateExactCoverage(
            result.SolidResults.Select(value => value.ElementId),
            topology.SolidElements.Select(value => value.ElementId),
            "solid_results");

        foreach (NodeDisplacement displacement in result.NodeDisplacements)
        {
            ValidateDisplacement(displacement.Components);
        }

        foreach (SupportReaction reaction in result.SupportReactions)
        {
            ValidateForce(reaction.Components);
        }

        Dictionary<string, TopologyMember> members = topology.Members.ToDictionary(
            member => member.MemberId,
            StringComparer.Ordinal);
        foreach (MemberSectionForces memberResult in result.MemberSectionForces)
        {
            TopologyMember member = members[memberResult.MemberId];
            Require(memberResult.Segments.Count == member.Stations.Count - 1,
                $"Member '{member.MemberId}' result must cover consecutive stations.");
            for (int index = 0; index < memberResult.Segments.Count; index++)
            {
                MemberSegmentResult segment = memberResult.Segments[index];
                MemberStation stationI = member.Stations[index];
                MemberStation stationJ = member.Stations[index + 1];
                Require(
                    segment.SegmentId == $"{stationI.StationId}-{stationJ.StationId}" &&
                    segment.StationI == stationI.StationId &&
                    segment.StationJ == stationJ.StationId,
                    $"Member '{member.MemberId}' result segment identity is invalid.");
                RequireFinite(segment.Length, "member segment length");
                double expectedLength = stationJ.Position - stationI.Position;
                Require(
                    segment.Length >= 0 &&
                    Math.Abs(segment.Length - expectedLength) <=
                    FrameTolerance * Math.Max(1, Math.Abs(expectedLength)),
                    "Member segment length does not match its station interval.");
                ValidateForce(segment.IEnd);
                ValidateForce(segment.JEnd);
            }
        }

        Dictionary<string, TopologyShellElement> shells = topology.ShellElements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);
        foreach (ShellResult shellResult in result.ShellResults)
        {
            ValidateExactCoverage(
                shellResult.Locations.Select(location => location.LocationId),
                shells[shellResult.ElementId].ResultLocations.Select(location => location.LocationId),
                $"shell '{shellResult.ElementId}' locations");
            foreach (ShellResultLocation location in shellResult.Locations)
            {
                ValidateFiniteValues(
                    location.MembraneForce.Nx, location.MembraneForce.Ny, location.MembraneForce.Nxy,
                    location.BendingMoment.Mx, location.BendingMoment.My, location.BendingMoment.Mxy,
                    location.TransverseShear.Qx, location.TransverseShear.Qy,
                    location.TopStress.Sx, location.TopStress.Sy, location.TopStress.Txy,
                    location.BottomStress.Sx, location.BottomStress.Sy, location.BottomStress.Txy);
            }
        }

        Dictionary<string, TopologySolidElement> solids = topology.SolidElements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);
        foreach (SolidResult solidResult in result.SolidResults)
        {
            ValidateExactCoverage(
                solidResult.Locations.Select(location => location.LocationId),
                solids[solidResult.ElementId].ResultLocations.Select(location => location.LocationId),
                $"solid '{solidResult.ElementId}' locations");
            foreach (SolidResultLocation location in solidResult.Locations)
            {
                ValidateFiniteValues(
                    location.Stress.Sx, location.Stress.Sy, location.Stress.Sz,
                    location.Stress.Txy, location.Stress.Tyz, location.Stress.Tzx,
                    location.Strain.Ex, location.Strain.Ey, location.Strain.Ez,
                    location.Strain.Gxy, location.Strain.Gyz, location.Strain.Gzx);
            }
        }
    }

    private static void ValidateExactCoverage(
        IEnumerable<string> actual,
        IEnumerable<string> expected,
        string description)
    {
        string[] actualValues = actual.ToArray();
        string[] expectedValues = expected.ToArray();
        EnsureUniqueIds(actualValues, description);
        Require(actualValues.SequenceEqual(expectedValues, StringComparer.Ordinal),
            $"{description} must exactly cover topology IDs in topology order.");
    }

    private static void ValidateDisplacement(DisplacementComponents value)
    {
        ValidateFiniteValues(value.Dx, value.Dy, value.Dz, value.Rx, value.Ry, value.Rz);
    }

    private static void ValidateForce(ForceComponents value)
    {
        ValidateFiniteValues(value.Fx, value.Fy, value.Fz, value.Mx, value.My, value.Mz);
    }

    private static void ValidateVector(Vector3Value vector, string description)
    {
        ValidateFiniteValues(vector.X, vector.Y, vector.Z);
    }

    private static void ValidateFrame(CoordinateFrame frame, string description)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ValidateVector(frame.Origin, $"{description}.origin");
        ValidateVector(frame.XAxis, $"{description}.x_axis");
        ValidateVector(frame.YAxis, $"{description}.y_axis");
        ValidateVector(frame.ZAxis, $"{description}.z_axis");
        Require(Math.Abs(Dot(frame.XAxis, frame.XAxis) - 1) <= FrameTolerance,
            $"{description}.x_axis must be a unit vector.");
        Require(Math.Abs(Dot(frame.YAxis, frame.YAxis) - 1) <= FrameTolerance,
            $"{description}.y_axis must be a unit vector.");
        Require(Math.Abs(Dot(frame.ZAxis, frame.ZAxis) - 1) <= FrameTolerance,
            $"{description}.z_axis must be a unit vector.");
        Require(
            Math.Abs(Dot(frame.XAxis, frame.YAxis)) <= FrameTolerance &&
            Math.Abs(Dot(frame.XAxis, frame.ZAxis)) <= FrameTolerance &&
            Math.Abs(Dot(frame.YAxis, frame.ZAxis)) <= FrameTolerance,
            $"{description} axes must be mutually orthogonal.");
        Vector3Value cross = Cross(frame.XAxis, frame.YAxis);
        Require(
            Math.Abs(cross.X - frame.ZAxis.X) <= FrameTolerance &&
            Math.Abs(cross.Y - frame.ZAxis.Y) <= FrameTolerance &&
            Math.Abs(cross.Z - frame.ZAxis.Z) <= FrameTolerance,
            $"{description} axes must be right-handed.");
    }

    private static double Dot(Vector3Value left, Vector3Value right)
        => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    private static Vector3Value Cross(Vector3Value left, Vector3Value right)
        => new(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);

    private static void ValidateWarnings(IEnumerable<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);
        Require(warnings.All(value => value is not null), "Diagnostic warnings cannot contain null.");
    }

    private static HashSet<string> UniqueIdSet(IEnumerable<string> values, string description)
    {
        string[] materialized = values.ToArray();
        EnsureUniqueIds(materialized, description);
        return IdSet(materialized);
    }

    private static HashSet<string> IdSet(IEnumerable<string> values)
        => new(values, StringComparer.Ordinal);

    private static void EnsureUniqueIds(IEnumerable<string> values, string description)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            ValidateId(value, description);
            Require(seen.Add(value), $"Duplicate {description} ID '{value}'.");
        }
    }

    private static void ValidateId(string value, string description)
    {
        Require(!string.IsNullOrWhiteSpace(value), $"{description} must be a nonblank string.");
    }

    private static void ValidateFiniteValues(params double[] values)
    {
        foreach (double value in values)
        {
            RequireFinite(value, "numeric value");
        }
    }

    private static void RequireFinite(double value, string description)
    {
        Require(double.IsFinite(value), $"{description} must be finite.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new AnalysisContractException(message);
        }
    }
}

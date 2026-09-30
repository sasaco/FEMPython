using FrameWebforCS.calculation;
using FrameWebforCS.providers;
using FrameWebforCS.three;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrameWebforCS.providers.printing;

// Values are the legacy print.service.ts option IDs. The two commented-out controls
// (10 and 16) remain reserved, so persisted UI selections cannot change meaning.
internal enum PrintOption
{
    Input = 0, Displacement = 1, CombinedDisplacement = 2,
    PickupDisplacement = 3, Reaction = 4, CombinedReaction = 5,
    PickupReaction = 6, SectionForce = 7, CombinedSectionForce = 8,
    PickupSectionForce = 9, ReservedScreen = 10, SectionDiagram = 11,
    CombinedSectionDiagram = 12, PickupSectionDiagram = 13,
    DisplacementDiagram = 14, LoadDiagram = 15, ReservedReactionDiagram = 16,
}

internal enum PrintLayout { Single, SplitHorizontal, SplitVertical }
internal enum PrintOrientation { Vertical, Horizontal }
internal enum PrintPaper { A4, A3 }

internal sealed record PrintSelection(
    IReadOnlyList<PrintOption> Options,
    IReadOnlyList<string>? CaseIds = null,
    IReadOnlyList<string>? DerivedIds = null,
    IReadOnlyList<string>? NodeIds = null,
    IReadOnlyList<string>? MemberIds = null,
    IReadOnlyList<string>? Components = null,
    PrintLayout Layout = PrintLayout.Single,
    PrintOrientation Orientation = PrintOrientation.Vertical,
    PrintPaper Paper = PrintPaper.A4,
    double? ScaleX = null,
    double? ScaleY = null,
    string Language = "ja",
    string? Title = null,
    IReadOnlyList<string>? DisplacementComponents = null,
    IReadOnlyList<string>? ReactionComponents = null,
    IReadOnlyList<string>? SectionForceComponents = null,
    IReadOnlyList<string>? DiagramComponents = null,
    IReadOnlyList<string>? LoadDiagramComponents = null);

internal sealed record PrintSectionForcePoint(int MemberId, float Location, float Value,
    bool IsMaximum);

internal sealed record PrintDiagramRequest(
    int Order, PrintOption Option, string Mode, string CaseId, string Output, string Title,
    IReadOnlyList<PrintSectionForcePoint>? DerivedSamples = null);

internal sealed class PrintSnapshot
{
    internal PrintSnapshot(PrintSelection selection, string inputJson, long documentRevision,
        long inputRevision, CalculationResultPresentation? result,
        CalculationDerivedPresentation? derived, IReadOnlyList<PrintDiagramRequest> diagrams)
    {
        Selection = selection;
        InputJson = inputJson;
        DocumentRevision = documentRevision;
        InputRevision = inputRevision;
        Result = result;
        Derived = derived;
        DiagramRequests = diagrams;
    }

    internal PrintSelection Selection { get; }
    internal string InputJson { get; }
    internal long DocumentRevision { get; }
    internal long InputRevision { get; }
    internal CalculationResultPresentation? Result { get; }
    internal CalculationDerivedPresentation? Derived { get; }
    internal IReadOnlyList<PrintDiagramRequest> DiagramRequests { get; }
}

internal sealed class PrintProjectionException(string message) : InvalidOperationException(message);
internal sealed record PrintAvailability(PrintOption Option, bool IsAvailable, string? Reason);

/// <summary>Detaches mutable UI state and creates the legacy root expected by PrintInput.</summary>
internal static class PrintProjection
{
    private static readonly string[] DisplacementNames = ["dx", "dy", "dz", "rx", "ry", "rz"];
    private static readonly string[] ForceNames = ["fx", "fy", "fz", "mx", "my", "mz"];
    private static readonly PrintOption[] AllowedOptions =
    [
        PrintOption.Input, PrintOption.Displacement, PrintOption.CombinedDisplacement,
        PrintOption.PickupDisplacement, PrintOption.Reaction, PrintOption.CombinedReaction,
        PrintOption.PickupReaction, PrintOption.SectionForce, PrintOption.CombinedSectionForce,
        PrintOption.PickupSectionForce, PrintOption.SectionDiagram,
        PrintOption.CombinedSectionDiagram, PrintOption.PickupSectionDiagram,
        PrintOption.DisplacementDiagram, PrintOption.LoadDiagram,
    ];

    /// <summary>UI-thread metadata for enabling choices before preview generation.</summary>
    internal static IReadOnlyList<PrintAvailability> GetAvailability()
    {
        CalculationResultPresentation? current = CalculationResultStore.Instance.Current;
        bool hasDisplacement = current?.Pages.Any(page => page.Result is ModalAnalysisResult modal &&
            modal.NodeModeShapes.Count > 0 ||
            page.Result is ForceAnalysisResult force && force.NodeDisplacements.Count > 0) == true;
        bool hasReaction = current?.Pages.Any(page => page.Result is ForceAnalysisResult force &&
            force.SupportReactions.Count > 0) == true;
        bool hasSectionForce = current?.Pages.Any(page => page.Result is ForceAnalysisResult force &&
            force.MemberSectionForces.Count > 0) == true;
        IReadOnlyList<CalculationDerivedCase> combined = current?.Derived?.Combines ?? [];
        IReadOnlyList<CalculationDerivedCase> pickup = current?.Derived?.Pickups ?? [];
        bool HasRows(IReadOnlyList<CalculationDerivedCase> cases, string family) => cases.Any(item =>
            (family switch { "disg" => item.Displacements, "reac" => item.Reactions,
                _ => item.SectionForces }).Values.Any(rows => rows.Count > 0));
        bool hasLoad = FrameWebforCS.components.input.InputLoadService.Instance.getLoadJson().Count > 0;
        return AllowedOptions.Select(option =>
        {
            bool available = option switch
            {
                PrintOption.Input => true,
                PrintOption.Displacement or PrintOption.DisplacementDiagram => hasDisplacement,
                PrintOption.Reaction => hasReaction,
                PrintOption.SectionForce or PrintOption.SectionDiagram => hasSectionForce,
                PrintOption.CombinedDisplacement => HasRows(combined, "disg"),
                PrintOption.CombinedReaction => HasRows(combined, "reac"),
                PrintOption.CombinedSectionForce or PrintOption.CombinedSectionDiagram =>
                    HasRows(combined, "fsec"),
                PrintOption.PickupDisplacement => HasRows(pickup, "disg"),
                PrintOption.PickupReaction => HasRows(pickup, "reac"),
                PrintOption.PickupSectionForce or PrintOption.PickupSectionDiagram =>
                    HasRows(pickup, "fsec"),
                PrintOption.LoadDiagram => hasLoad,
                _ => false,
            };
            if (option == PrintOption.DisplacementDiagram &&
                InputDataService.Instance.dimension != 3) available = false;
            return new PrintAvailability(option, available, available ? null :
                option == PrintOption.DisplacementDiagram && InputDataService.Instance.dimension != 3
                    ? "The displacement screenshot is available only for 3D documents."
                    : option == PrintOption.LoadDiagram
                        ? "No load case is available."
                        : "No corresponding calculation result is available.");
        }).ToArray();
    }

    /// <summary>Call on the WinForms UI thread. No mutable input collection is read after this.</summary>
    internal static PrintSnapshot Capture(PrintSelection selection)
    {
        ValidateSelection(selection);
        var input = InputDataService.Instance;
        var current = CalculationResultStore.Instance.Current;
        var detached = JsonSerializer.Serialize(input.GetSaveJson() ??
            throw new PrintProjectionException("No document is available for printing."));
        var frozenSelection = selection with
        {
            Options = selection.Options.Distinct().OrderBy(option => (int)option).ToArray(),
            CaseIds = selection.CaseIds?.ToArray(), DerivedIds = selection.DerivedIds?.ToArray(),
            NodeIds = selection.NodeIds?.ToArray(), MemberIds = selection.MemberIds?.ToArray(),
            Components = selection.Components?.ToArray(),
            DisplacementComponents = selection.DisplacementComponents?.ToArray(),
            ReactionComponents = selection.ReactionComponents?.ToArray(),
            SectionForceComponents = selection.SectionForceComponents?.ToArray(),
            DiagramComponents = selection.DiagramComponents?.ToArray(),
            LoadDiagramComponents = selection.LoadDiagramComponents?.ToArray(),
        };
        IReadOnlyList<PrintDiagramRequest> diagrams = MakeDiagramRequests(frozenSelection,
            current, JsonNode.Parse(detached)!.AsObject());
        return new(frozenSelection, detached, input.DocumentRevision,
            input.CalculationInputRevision, current, current?.Derived, diagrams);
    }

    /// <summary>Must be checked on the UI thread before publishing the generated preview.</summary>
    internal static bool IsCurrent(PrintSnapshot snapshot)
    {
        var input = InputDataService.Instance;
        var result = CalculationResultStore.Instance.Current;
        return input.DocumentRevision == snapshot.DocumentRevision &&
            input.CalculationInputRevision == snapshot.InputRevision &&
            ReferenceEquals(result, snapshot.Result) &&
            ReferenceEquals(result?.Derived, snapshot.Derived);
    }

    /// <summary>Pure projection. Throws on absent data, invalid filters, images, or stale generation.</summary>
    internal static string Build(PrintSnapshot snapshot,
        IReadOnlyList<PrintDiagramImage>? images = null, Func<bool>? stillCurrent = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateSelection(snapshot.Selection);
        if (stillCurrent is not null && !stillCurrent())
            throw new PrintProjectionException("The document changed while preparing the print preview.");
        JsonObject saved = JsonNode.Parse(snapshot.InputJson)?.AsObject() ??
            throw new PrintProjectionException("The print snapshot has no input root.");
        PrintSelection choice = snapshot.Selection;
        var root = new JsonObject
        {
            ["dimension"] = saved["dimension"]?.DeepClone() ?? JsonValue.Create(3),
            ["language"] = choice.Language, ["pageSize"] = choice.Paper.ToString(),
            ["pageOrientation"] = choice.Orientation.ToString(),
            ["ver"] = "2.5.12", ["hasPrintInputData"] = choice.Options.Contains(PrintOption.Input),
            ["hasPrintCalculation"] = choice.Options.Any(IsTableResult),
        };
        if (choice.Title is not null) root["title"] = choice.Title;
        foreach (string key in new[] { "node", "member", "shell", "element", "rigid", "joint",
            "fix_node", "fix_member", "notice_points", "load", "define", "combine", "pickup" })
            root[key] = saved[key]?.DeepClone() ??
                (key is "rigid" or "notice_points" ? new JsonArray() : new JsonObject());

        if (choice.Options.Any(IsTableResult) || choice.Options.Any(IsResultDiagram))
            AddResults(root, snapshot);
        AddVectorDiagrams(root, snapshot);
        AddCapturedDiagrams(root, snapshot.DiagramRequests, images);
        if (stillCurrent is not null && !stillCurrent())
            throw new PrintProjectionException("The document changed while preparing the print preview.");
        return root.ToJsonString();
    }

    private static void ValidateSelection(PrintSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.Options is null || selection.Options.Count == 0)
            throw new PrintProjectionException("Select at least one print item.");
        foreach (PrintOption option in selection.Options)
            if (!AllowedOptions.Contains(option))
                throw new PrintProjectionException($"Print option {(int)option} is not available.");
        if (!Enum.IsDefined(selection.Layout) || !Enum.IsDefined(selection.Orientation) ||
            !Enum.IsDefined(selection.Paper))
            throw new PrintProjectionException("A print layout, orientation, or paper choice is invalid.");
        if (selection.Language is not ("ja" or "en"))
            throw new PrintProjectionException("The print language must be ja or en.");
        if (selection.Title?.Length > 256)
            throw new PrintProjectionException("The print title is too long.");
        foreach (double? scale in new[] { selection.ScaleX, selection.ScaleY })
            if (scale is { } value && (!double.IsFinite(value) || value <= 0 || value > 1_000_000))
                throw new PrintProjectionException("Diagram scales must be positive and finite.");
        foreach (var ids in new[] { selection.CaseIds, selection.DerivedIds,
            selection.NodeIds, selection.MemberIds, selection.Components,
            selection.DisplacementComponents, selection.ReactionComponents,
            selection.SectionForceComponents, selection.DiagramComponents,
            selection.LoadDiagramComponents })
            if (ids is { Count: > 10_000 } || ids?.Any(id => string.IsNullOrWhiteSpace(id)) == true)
                throw new PrintProjectionException("A print filter is invalid or too large.");
        if (selection.Components is not null && selection.Components.Any(component =>
        {
            string name = component.EndsWith("_max", StringComparison.Ordinal) ||
                component.EndsWith("_min", StringComparison.Ordinal)
                ? component[..^4] : component;
            return !DisplacementNames.Contains(name) && !ForceNames.Contains(name) &&
                !new[] { "axis", "load", "disg", "tx", "ty", "tz" }.Contains(name);
        }))
            throw new PrintProjectionException("The selected result component is unknown.");
        ValidateComponents(selection.DisplacementComponents, DisplacementNames, "displacement", true);
        ValidateComponents(selection.ReactionComponents,
            ["tx", "ty", "tz", "mx", "my", "mz"], "reaction", true);
        ValidateComponents(selection.SectionForceComponents, ForceNames, "section force", true);
        ValidateComponents(selection.DiagramComponents,
            ["fx", "fy", "fz", "mx", "my", "mz", "disg"], "diagram", false);
        ValidateComponents(selection.LoadDiagramComponents, ["axis", "load"], "load diagram", false);
    }

    private static void ValidateComponents(IReadOnlyList<string>? selected,
        IReadOnlyList<string> allowed, string family, bool allowExtrema)
    {
        if (selected is null) return;
        if (selected.Any(component =>
        {
            string name = allowExtrema && (component.EndsWith("_max", StringComparison.Ordinal) ||
                component.EndsWith("_min", StringComparison.Ordinal))
                ? component[..^4] : component;
            return !allowed.Contains(name);
        }))
            throw new PrintProjectionException($"The selected {family} component is unknown.");
    }

    private static IReadOnlyList<string>? FamilyComponents(PrintSelection selection, string family) =>
        family switch
        {
            "disg" => selection.DisplacementComponents ?? selection.Components,
            "reac" => selection.ReactionComponents ?? selection.Components,
            _ => selection.SectionForceComponents ?? selection.Components,
        };

    private static bool IsTableResult(PrintOption option) => option is >= PrintOption.Displacement and <= PrintOption.PickupSectionForce;
    private static bool IsResultDiagram(PrintOption option) => option is
        PrintOption.SectionDiagram or PrintOption.CombinedSectionDiagram or
        PrintOption.PickupSectionDiagram or PrintOption.DisplacementDiagram;

    private static bool HasPrintableRows(CalculationResultPage page, string family,
        PrintSelection selection)
    {
        IEnumerable<AnalysisResult> sources = page.MovingChildren.Count == 0
            ? [page.Result] : [page.Result, .. page.MovingChildren];
        return sources.Any(source => family switch
        {
            "disg" => source switch
            {
                ForceAnalysisResult force => force.NodeDisplacements.Any(row =>
                    selection.NodeIds is null || selection.NodeIds.Contains(row.NodeId)),
                ModalAnalysisResult modal => modal.NodeModeShapes.Any(row =>
                    selection.NodeIds is null || selection.NodeIds.Contains(row.NodeId)),
                _ => false,
            },
            "reac" => source is ForceAnalysisResult reaction &&
                reaction.SupportReactions.Any(row => selection.NodeIds is null ||
                    selection.NodeIds.Contains(row.NodeId)),
            _ => source is ForceAnalysisResult section &&
                section.MemberSectionForces.Any(row => selection.MemberIds is null ||
                    selection.MemberIds.Contains(row.MemberId)),
        });
    }

    private static void AddResults(JsonObject root, PrintSnapshot snapshot)
    {
        var selected = snapshot.Selection.Options;
        var result = snapshot.Result ?? throw new PrintProjectionException(
            "Selected result reports require current calculation results.");
        if (result.Dimension != root["dimension"]!.GetValue<int>())
            throw new PrintProjectionException("The calculation dimension differs from the current document.");
        var caseIds = snapshot.Selection.CaseIds;
        if (caseIds is not null && caseIds.Any(id => !result.Pages.Any(page => page.Key == id || page.Case.CaseId == id)))
            throw new PrintProjectionException("A selected calculation case is unavailable.");
        var pages = result.Pages.Where(page => caseIds is null || caseIds.Contains(page.Key) ||
            caseIds.Contains(page.Case.CaseId)).ToArray();
        if (pages.Length == 0) throw new PrintProjectionException("No selected result case is available.");
        if (snapshot.Selection.NodeIds is { } nodeIds && nodeIds.Any(id =>
            !result.ResultSet.Topology.Nodes.Any(node => node.NodeId == id)))
            throw new PrintProjectionException("A selected node ID is unavailable.");
        if (snapshot.Selection.MemberIds is { } memberIds && memberIds.Any(id =>
            !result.ResultSet.Topology.Members.Any(member => member.MemberId == id)))
            throw new PrintProjectionException("A selected member ID is unavailable.");
        if (selected.Contains(PrintOption.Displacement) || selected.Contains(PrintOption.SectionDiagram) ||
            selected.Contains(PrintOption.DisplacementDiagram))
            AddBaseFamily(root, "disg", pages, snapshot.Selection, result);
        if (selected.Contains(PrintOption.Reaction))
            AddBaseFamily(root, "reac", pages, snapshot.Selection, result);
        if (selected.Contains(PrintOption.SectionForce) || selected.Contains(PrintOption.SectionDiagram))
            AddBaseFamily(root, "fsec", pages, snapshot.Selection, result);
        if (selected.Any(option => option is PrintOption.CombinedDisplacement or PrintOption.PickupDisplacement or
            PrintOption.CombinedReaction or PrintOption.PickupReaction or PrintOption.CombinedSectionForce or
            PrintOption.PickupSectionForce or PrintOption.CombinedSectionDiagram or PrintOption.PickupSectionDiagram))
            AddDerivedFamilies(root, snapshot);
    }

    private static void AddBaseFamily(JsonObject root, string family,
        IReadOnlyList<CalculationResultPage> pages, PrintSelection selection,
        CalculationResultPresentation presentation)
    {
        var cases = new JsonObject();
        var names = new JsonArray();
        var members = presentation.ResultSet.Topology.Members.ToDictionary(item => item.MemberId);
        foreach (CalculationResultPage page in pages.Where(page =>
            HasPrintableRows(page, family, selection)))
        {
            if (page.MovingChildren.Count > 0 && page.Result is StaticAnalysisResult movingParent)
            {
                cases.Add(page.Key, MovingEnvelope(family,
                    [movingParent, .. page.MovingChildren], selection, members,
                    presentation.Dimension));
                names.Add(new JsonArray(JsonValue.Create(page.Key), JsonValue.Create(page.Label)));
                continue;
            }
            JsonArray rows = new();
            if (family == "disg")
            {
                IReadOnlyList<NodeDisplacement> source = page.Result switch
                {
                    ForceAnalysisResult force => force.NodeDisplacements,
                    ModalAnalysisResult modal => modal.NodeModeShapes,
                    _ => [],
                };
                foreach (NodeDisplacement item in source.Where(item =>
                    selection.NodeIds is null || selection.NodeIds.Contains(item.NodeId)))
                    rows.Add(new JsonObject { ["id"] = item.NodeId, ["dx"] = item.Components.Dx,
                        ["dy"] = item.Components.Dy, ["dz"] = item.Components.Dz,
                        ["rx"] = item.Components.Rx, ["ry"] = item.Components.Ry,
                        ["rz"] = item.Components.Rz });
            }
            else if (page.Result is ForceAnalysisResult force)
            {
                if (family == "reac")
                    foreach (SupportReaction item in force.SupportReactions.Where(item =>
                        selection.NodeIds is null || selection.NodeIds.Contains(item.NodeId)))
                        rows.Add(new JsonObject { ["id"] = item.NodeId,
                            ["tx"] = item.Components.Fx, ["ty"] = item.Components.Fy,
                            ["tz"] = item.Components.Fz, ["mx"] = item.Components.Mx,
                            ["my"] = item.Components.My, ["mz"] = item.Components.Mz });
                else
                    foreach (MemberSectionForces member in force.MemberSectionForces.Where(item =>
                        selection.MemberIds is null || selection.MemberIds.Contains(item.MemberId)))
                    {
                        var positions = members[member.MemberId].Stations.ToDictionary(
                            station => station.StationId, station => station.Position);
                        var seen = new HashSet<string>();
                        foreach (MemberSegmentResult segment in member.Segments)
                        {
                            AddStation(segment.StationI, segment.IEnd);
                            AddStation(segment.StationJ, segment.JEnd);
                        }
                        void AddStation(string stationId, ForceComponents value)
                        {
                            if (!seen.Add(stationId)) return;
                            rows.Add(new JsonObject { ["m"] = member.MemberId, ["n"] = stationId,
                                ["l"] = positions[stationId], ["fx"] = value.Fx, ["fy"] = value.Fy,
                                ["fz"] = value.Fz, ["mx"] = value.Mx, ["my"] = value.My,
                                ["mz"] = value.Mz, ["dummy"] = false });
                        }
                    }
            }
            else throw new PrintProjectionException($"{family} is unavailable for a modal case.");
            if (rows.Count == 0)
                throw new PrintProjectionException($"{family} has no printable rows in case {page.Label}.");
            cases.Add(page.Key, rows);
            names.Add(new JsonArray(JsonValue.Create(page.Key), JsonValue.Create(page.Label)));
        }
        if (cases.Count == 0)
            throw new PrintProjectionException($"{family} has no printable rows in the selected cases.");
        root[family] = cases;
        root[family + "Name"] = names;
    }

    private sealed record MovingRow(string EntityId, string? StationId, double Location,
        double[] Components, string CaseId);

    private static JsonObject MovingEnvelope(string family,
        IReadOnlyList<StaticAnalysisResult> sources, PrintSelection selection,
        IReadOnlyDictionary<string, TopologyMember> members, int dimension)
    {
        var candidates = new List<MovingRow>();
        foreach (StaticAnalysisResult source in sources)
        {
            if (family == "disg")
                foreach (NodeDisplacement row in source.NodeDisplacements.Where(row =>
                    selection.NodeIds is null || selection.NodeIds.Contains(row.NodeId)))
                    candidates.Add(new(row.NodeId, null, 0,
                        [row.Components.Dx, row.Components.Dy, row.Components.Dz,
                         row.Components.Rx, row.Components.Ry, row.Components.Rz], source.CaseId));
            else if (family == "reac")
                foreach (SupportReaction row in source.SupportReactions.Where(row =>
                    selection.NodeIds is null || selection.NodeIds.Contains(row.NodeId)))
                    candidates.Add(new(row.NodeId, null, 0,
                        [row.Components.Fx, row.Components.Fy, row.Components.Fz,
                         row.Components.Mx, row.Components.My, row.Components.Mz], source.CaseId));
            else
                foreach (MemberSectionForces member in source.MemberSectionForces.Where(member =>
                    selection.MemberIds is null || selection.MemberIds.Contains(member.MemberId)))
                {
                    var positions = members[member.MemberId].Stations.ToDictionary(
                        station => station.StationId, station => station.Position);
                    var seen = new HashSet<string>();
                    foreach (MemberSegmentResult segment in member.Segments)
                    {
                        AddStation(segment.StationI, segment.IEnd);
                        AddStation(segment.StationJ, segment.JEnd);
                    }
                    void AddStation(string stationId, ForceComponents force)
                    {
                        if (!seen.Add(stationId)) return;
                        candidates.Add(new(member.MemberId, stationId, positions[stationId],
                            [force.Fx, force.Fy, force.Fz, force.Mx, force.My, force.Mz], source.CaseId));
                    }
                }
        }
        if (candidates.Count == 0)
            throw new PrintProjectionException($"{family} has no moving-load result rows.");
        string[] focusNames = family switch
        {
            "disg" => DisplacementNames,
            "reac" => ["tx", "ty", "tz", "mx", "my", "mz"],
            _ => ForceNames,
        };
        int[] focusIndices = dimension == 2 ? [0, 1, 5] : [0, 1, 2, 3, 4, 5];
        var envelope = new JsonObject();
        foreach (int component in focusIndices)
        foreach (bool maximum in new[] { true, false })
        {
            string mode = focusNames[component] + (maximum ? "_max" : "_min");
            if (!SelectsMode(FamilyComponents(selection, family), mode, focusNames[component])) continue;
            var selectedRows = candidates.GroupBy(row => (row.EntityId, row.StationId));
            if (family == "fsec")
            {
                var rows = new JsonArray();
                foreach (var group in selectedRows)
                    rows.Add(MovingValue(group, family, component, maximum));
                envelope[mode] = rows;
            }
            else
            {
                var rows = new JsonObject();
                foreach (var group in selectedRows)
                    rows[group.Key.EntityId] = MovingValue(group, family, component, maximum);
                envelope[mode] = rows;
            }
        }
        if (envelope.Count == 0)
            throw new PrintProjectionException($"{family} has no selected moving-load components.");
        return envelope;
    }

    private static JsonObject MovingValue(IEnumerable<MovingRow> group, string family,
        int component, bool maximum)
    {
        MovingRow chosen = maximum
            ? group.Aggregate((best, next) => next.Components[component] > best.Components[component] ? next : best)
            : group.Aggregate((best, next) => next.Components[component] < best.Components[component] ? next : best);
        var value = new JsonObject { [family == "fsec" ? "m" : "id"] = chosen.EntityId,
            ["case"] = chosen.CaseId, ["comb"] = "" };
        if (family == "fsec")
        {
            value["n"] = chosen.StationId;
            value["l"] = chosen.Location;
        }
        string[] names = family switch
        {
            "disg" => DisplacementNames,
            "reac" => ["tx", "ty", "tz", "mx", "my", "mz"],
            _ => ForceNames,
        };
        for (int index = 0; index < names.Length; index++) value[names[index]] = chosen.Components[index];
        if (family == "fsec") value["dummy"] = false;
        return value;
    }

    private static bool SelectsMode(IReadOnlyList<string>? selected, string mode, string component)
    {
        if (selected is null || selected.Contains(mode) || selected.Contains(component)) return true;
        string alias = component switch { "tx" => "fx", "ty" => "fy", "tz" => "fz", _ => component };
        return selected.Contains(alias) || selected.Contains(alias + mode[component.Length..]);
    }

    private static void AddDerivedFamilies(JsonObject root, PrintSnapshot snapshot)
    {
        CalculationDerivedPresentation derived = snapshot.Derived ?? throw new PrintProjectionException(
            "DEFINE, COMBINE, and PICKUP results are still being prepared.");
        var stations = snapshot.Result!.ResultSet.Topology.Members.ToDictionary(
            member => member.MemberId,
            member => member.Stations.ToDictionary(station => station.StationId,
                station => station.Position));
        foreach (var (option, key, source, property, components) in new[]
        {
            (PrintOption.CombinedDisplacement, "disgCombine", derived.Combines, "disg", DisplacementNames),
            (PrintOption.PickupDisplacement, "disgPickup", derived.Pickups, "disg", DisplacementNames),
            (PrintOption.CombinedReaction, "reacCombine", derived.Combines, "reac", ForceNames),
            (PrintOption.PickupReaction, "reacPickup", derived.Pickups, "reac", ForceNames),
            (PrintOption.CombinedSectionForce, "fsecCombine", derived.Combines, "fsec", ForceNames),
            (PrintOption.PickupSectionForce, "fsecPickup", derived.Pickups, "fsec", ForceNames),
        })
        {
            bool diagram = option is PrintOption.CombinedSectionForce &&
                snapshot.Selection.Options.Contains(PrintOption.CombinedSectionDiagram) ||
                option is PrintOption.PickupSectionForce &&
                snapshot.Selection.Options.Contains(PrintOption.PickupSectionDiagram);
            if (!snapshot.Selection.Options.Contains(option) && !diagram) continue;
            var cases = new JsonObject();
            var names = new JsonArray();
            foreach (CalculationDerivedCase item in source.Where(item =>
                snapshot.Selection.DerivedIds is null || snapshot.Selection.DerivedIds.Contains(item.Id)))
            {
                var modes = property switch
                {
                    "disg" => item.Displacements,
                    "reac" => item.Reactions,
                    _ => item.SectionForces,
                };
                var caseObject = new JsonObject();
                foreach (var (mode, rows) in modes)
                {
                    string legacyMode = property == "reac" ? mode.Replace("fx", "tx")
                        .Replace("fy", "ty").Replace("fz", "tz") : mode;
                    if (!SelectsMode(FamilyComponents(snapshot.Selection, property), legacyMode,
                        legacyMode[..legacyMode.IndexOf('_')])) continue;
                    if (property == "fsec")
                    {
                        var values = new JsonArray();
                        foreach (CalculationDerivedRow row in rows.Where(row =>
                            snapshot.Selection.MemberIds is null || snapshot.Selection.MemberIds.Contains(row.EntityId)))
                            values.Add(DerivedRow(row, property, components, stations));
                        if (values.Count > 0) caseObject[legacyMode] = values;
                    }
                    else
                    {
                        var values = new JsonObject();
                        foreach (CalculationDerivedRow row in rows.Where(row =>
                            snapshot.Selection.NodeIds is null || snapshot.Selection.NodeIds.Contains(row.EntityId)))
                            values[row.EntityId] = DerivedRow(row, property, components, stations);
                        if (values.Count > 0) caseObject[legacyMode] = values;
                    }
                }
                if (caseObject.Count == 0) continue;
                cases[item.Id] = caseObject;
                names.Add(new JsonArray(JsonValue.Create(item.Id), JsonValue.Create(item.Name ?? item.Id)));
            }
            if (cases.Count == 0) throw new PrintProjectionException($"{key} has no selected rows.");
            root[key] = cases;
            root[key + "Name"] = names;
        }
    }

    private static JsonObject DerivedRow(CalculationDerivedRow row, string family, string[] components,
        IReadOnlyDictionary<string, Dictionary<string, double>> stations)
    {
        var value = new JsonObject { [family == "fsec" ? "m" : "id"] = row.EntityId,
            ["case"] = row.SourceCaseId, ["comb"] = row.Provenance };
        if (family == "fsec")
        {
            if (row.StationId is null || !stations.TryGetValue(row.EntityId, out var member) ||
                !member.TryGetValue(row.StationId, out double location))
                throw new PrintProjectionException("A derived section force has no matching station.");
            value["n"] = row.StationId;
            value["l"] = location;
        }
        for (int index = 0; index < components.Length; index++)
        {
            string source = components[index];
            string target = family == "reac" && index < 3 ? new[] { "tx", "ty", "tz" }[index] : source;
            value[target] = row.Components[source];
        }
        return value;
    }

    private static void AddVectorDiagrams(JsonObject root, PrintSnapshot snapshot)
    {
        var choice = snapshot.Selection;
        if (root["dimension"]!.GetValue<int>() != 2) return;
        foreach (var (option, key, config, outputs) in new[]
        {
            (PrintOption.LoadDiagram, "PrintLoad", "diagramInput", new[] { "axis", "load" }),
            (PrintOption.SectionDiagram, "PrintDiagram", "diagramResult", new[] { "mz", "fy", "fx", "disg" }),
            (PrintOption.CombinedSectionDiagram, "CombPrintDiagram", "diagramResult", new[] { "mz", "fy", "fx" }),
            (PrintOption.PickupSectionDiagram, "PickPrintDiagram", "diagramResult", new[] { "mz", "fy", "fx" }),
        })
        {
            if (!choice.Options.Contains(option)) continue;
            var layout = choice.Layout switch
            {
                PrintLayout.Single => "single", PrintLayout.SplitHorizontal => "splitHorizontal",
                _ => "splitVertical",
            };
            IReadOnlyList<string>? filter = option == PrintOption.LoadDiagram
                ? choice.LoadDiagramComponents ?? choice.Components
                : choice.DiagramComponents ?? choice.Components;
            string[] selected = filter is null ? outputs : outputs.Where(filter.Contains).ToArray();
            if (selected.Length == 0) throw new PrintProjectionException($"{key} has no selected components.");
            var details = new JsonObject { ["layout"] = layout,
                ["output"] = new JsonArray(selected.Select(value =>
                    (JsonNode?)JsonValue.Create(value)).ToArray()) };
            if (choice.ScaleX is { } sx) details["scaleX"] = 1 / sx;
            if (choice.ScaleY is { } sy) details["scaleY"] = 1 / sy;
            var diagram = new JsonObject { [config] = details,
                ["pageOrientation"] = choice.Orientation.ToString() };
            if (option == PrintOption.SectionDiagram && selected.Contains("disg") &&
                root["disg"] is { } displacements)
            {
                diagram["disg"] = displacements.DeepClone();
                diagram["disgName"] = root["disgName"]?.DeepClone();
            }
            root[key] = diagram;
        }
    }

    private static IReadOnlyList<PrintDiagramRequest> MakeDiagramRequests(PrintSelection selection,
        CalculationResultPresentation? result, JsonObject saved)
    {
        var requests = new List<PrintDiagramRequest>();
        if ((saved["dimension"]?.GetValue<int>() ?? 3) == 2)
        {
            if (selection.Options.Contains(PrintOption.DisplacementDiagram))
                throw new PrintProjectionException("The displacement screenshot requires a 3D document.");
            return requests.AsReadOnly();
        }
        foreach (PrintOption option in selection.Options.Where(option => option is >= PrintOption.SectionDiagram and <= PrintOption.LoadDiagram))
        {
            string mode = option switch
            {
                PrintOption.LoadDiagram => "print_load", PrintOption.SectionDiagram => "fsec",
                PrintOption.CombinedSectionDiagram => "comb_fsec",
                PrintOption.PickupSectionDiagram => "pick_fsec", _ => "disg",
            };
            IReadOnlyList<string> ids = option switch
            {
                PrintOption.LoadDiagram => (saved["load"] as JsonObject)?.Select(item => item.Key).ToArray() ?? [],
                PrintOption.CombinedSectionDiagram => result?.Derived?.Combines.Select(item => item.Id).ToArray() ?? [],
                PrintOption.PickupSectionDiagram => result?.Derived?.Pickups.Select(item => item.Id).ToArray() ?? [],
                _ => result?.Pages.Where(page => HasPrintableRows(page,
                    option == PrintOption.DisplacementDiagram ? "disg" : "fsec", selection))
                    .Select(item => item.Key).ToArray() ?? [],
            };
            ids = ids.Where(id => option switch
            {
                PrintOption.LoadDiagram => true, // legacy 3D print iterates every named load case
                PrintOption.CombinedSectionDiagram or PrintOption.PickupSectionDiagram =>
                    selection.DerivedIds is null || selection.DerivedIds.Contains(id),
                _ => selection.CaseIds is null || selection.CaseIds.Contains(id) ||
                    result?.Pages.Any(page => page.Key == id &&
                        selection.CaseIds.Contains(page.Case.CaseId)) == true,
            }).ToArray();
            if (ids.Count == 0) throw new PrintProjectionException($"{option} has no available case.");
            string[] defaultOutputs = option == PrintOption.LoadDiagram ? ["load"] :
                option == PrintOption.DisplacementDiagram ? ["disg"] :
                ["fx", "fy", "fz", "mx", "my", "mz"];
            IReadOnlyList<string>? filter = option == PrintOption.LoadDiagram
                ? selection.LoadDiagramComponents ?? selection.Components
                : selection.DiagramComponents ?? selection.Components;
            string[] outputs = filter is null ? defaultOutputs :
                defaultOutputs.Where(filter.Contains).ToArray();
            if (outputs.Length == 0) throw new PrintProjectionException($"{option} has no selected component.");
            foreach (string id in ids)
                foreach (string output in outputs)
                    requests.Add(new(requests.Count, option, mode, id, output,
                        $"{option}: {id} {output}",
                        option is PrintOption.CombinedSectionDiagram or PrintOption.PickupSectionDiagram
                            ? MakeDerivedSamples(result!, option, id, output) : null));
        }
        if (requests.Count > 120) throw new PrintProjectionException("Too many selected diagrams.");
        return requests.AsReadOnly();
    }

    private static IReadOnlyList<PrintSectionForcePoint> MakeDerivedSamples(
        CalculationResultPresentation result, PrintOption option, string id, string output)
    {
        var derived = result.Derived ?? throw new PrintProjectionException(
            "Derived section forces are unavailable for diagram capture.");
        IReadOnlyList<CalculationDerivedCase> source = option == PrintOption.CombinedSectionDiagram
            ? derived.Combines : derived.Pickups;
        CalculationDerivedCase item = source.FirstOrDefault(item => item.Id == id) ??
            throw new PrintProjectionException($"Derived case '{id}' is unavailable.");
        var positions = result.ResultSet.Topology.Members.ToDictionary(
            member => member.MemberId,
            member => member.Stations.ToDictionary(station => station.StationId,
                station => station.Position));
        var points = new List<PrintSectionForcePoint>();
        foreach (bool isMaximum in new[] { true, false })
        {
            string mode = output + (isMaximum ? "_max" : "_min");
            if (!item.SectionForces.TryGetValue(mode, out var rows)) continue;
            foreach (CalculationDerivedRow row in rows)
            {
                string idText = row.EntityId.StartsWith("member", StringComparison.Ordinal)
                    ? row.EntityId[6..] : row.EntityId;
                if (!int.TryParse(idText, out int memberId) || memberId <= 0 ||
                    row.StationId is null ||
                    !positions.TryGetValue(row.EntityId, out var stations) ||
                    !stations.TryGetValue(row.StationId, out double location) ||
                    !row.Components.TryGetValue(output, out double value) ||
                    !double.IsFinite(location) || !double.IsFinite(value))
                    throw new PrintProjectionException($"Derived diagram {id}/{mode} has invalid station data.");
                points.Add(new(memberId, checked((float)location), checked((float)value), isMaximum));
            }
        }
        if (points.Count == 0)
            throw new PrintProjectionException($"Derived diagram {id}/{output} has no samples.");
        return points.AsReadOnly();
    }

    private static void AddCapturedDiagrams(JsonObject root,
        IReadOnlyList<PrintDiagramRequest> requests, IReadOnlyList<PrintDiagramImage>? images)
    {
        if (requests.Count == 0) return;
        if (images is null || images.Count != requests.Count)
            throw new PrintProjectionException("Every selected diagram must be captured before printing.");
        var screens = new JsonArray();
        for (int index = 0; index < requests.Count; index++)
        {
            PrintDiagramImage image = images[index];
            if (image.Request != requests[index] || image.PngBytes is null ||
                image.PngBytes.Length is < 8 or > 16_777_216 ||
                !image.PngBytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                throw new PrintProjectionException($"Diagram {index + 1} has no valid PNG capture.");
            screens.Add(new JsonObject { ["mode"] = image.Request.Mode,
                ["title1"] = image.Request.Title,
                ["result"] = new JsonArray(new JsonObject { ["Judge"] = true,
                    ["src"] = "data:image/png;base64," + Convert.ToBase64String(image.PngBytes),
                    ["title"] = image.Request.Title, ["type"] = image.Request.Output,
                    ["max_three"] = "", ["min_three"] = "",
                    ["disgSubInfo1"] = "", ["disgSubInfo2"] = "" }) });
        }
        root["PrintScreenData"] = screens;
    }
}

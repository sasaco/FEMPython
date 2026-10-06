namespace FrameWebforCS.calculation;

/// <summary>A single selectable result state. IDs remain those supplied by the v1 contract.</summary>
internal sealed record CalculationResultPage(
    string Key,
    string Label,
    AnalysisCase Case,
    AnalysisResult Result,
    IReadOnlyList<StaticAnalysisResult> MovingChildren);

internal sealed class CalculationResultPresentation
{
    internal CalculationResultPresentation(AnalysisResultSet resultSet,
        CalculationDerivedPresentation? derived = null, int dimension = 3)
    {
        ArgumentNullException.ThrowIfNull(resultSet);
        if (dimension is not (2 or 3)) throw new ArgumentOutOfRangeException(nameof(dimension));
        AnalysisResultSetValidator.Validate(resultSet);
        ResultSet = resultSet;
        Derived = derived;
        Dimension = dimension;
        var cases = resultSet.Cases.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        var staticResults = resultSet.Results.OfType<StaticAnalysisResult>()
            .ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        var pages = new List<CalculationResultPage>();
        var movingGroups = resultSet.Cases
            .Where(parent => parent.AnalysisType == AnalysisType.Static &&
                ContainsLl(parent.Symbol) && staticResults.ContainsKey(parent.CaseId))
            .ToDictionary(parent => parent.CaseId,
                parent => (IReadOnlyList<StaticAnalysisResult>)resultSet.Cases
                    .Where(child => child.CaseId.StartsWith(parent.CaseId + ".", StringComparison.Ordinal) &&
                        child.AnalysisType == AnalysisType.Static && ContainsLl(child.Symbol) &&
                        staticResults.ContainsKey(child.CaseId))
                    .Select(child => staticResults[child.CaseId]).ToArray(), StringComparer.Ordinal);
        var groupedChildren = new HashSet<string>(StringComparer.Ordinal);
        foreach (var children in movingGroups.Values)
            foreach (StaticAnalysisResult child in children) groupedChildren.Add(child.CaseId);
        foreach (AnalysisResult result in resultSet.Results)
        {
            if (groupedChildren.Contains(result.CaseId)) continue;
            AnalysisCase resultCase = cases[result.CaseId];
            string label = resultCase.Name.Length == 0 ? result.CaseId : resultCase.Name;
            label = result.State switch
            {
                LoadStepResultState step => $"{label} (Step {step.StepIndex}, {step.LoadFactor:G6})",
                ModeResultState mode => $"{label} (Mode {mode.ModeIndex}, {mode.Frequency:G6} Hz)",
                _ => label,
            };
            string key = $"{result.CaseId}/{result.State.Kind}/{result.State.Index}";
            IReadOnlyList<StaticAnalysisResult> movingChildren = result is StaticAnalysisResult &&
                movingGroups.TryGetValue(result.CaseId, out var group) ? group : [];
            pages.Add(new(key, label, resultCase, result, movingChildren));
        }
        Pages = pages.AsReadOnly();
    }

    internal AnalysisResultSet ResultSet { get; }
    internal CalculationDerivedPresentation? Derived { get; private set; }
    internal int Dimension { get; }
    internal void UpdateDerived(CalculationDerivedPresentation? derived) => Derived = derived;
    internal IReadOnlyList<CalculationResultPage> Pages { get; }
    internal string LengthUnit => UnitLabel(ResultSet.Units.Length);
    internal string ForceUnit => UnitLabel(ResultSet.Units.Force);
    internal string MomentUnit => ResultSet.Units.Length is "m" or "mm" &&
        ResultSet.Units.Force is "N" or "kN"
        ? $"{ResultSet.Units.Force}·{ResultSet.Units.Length}"
        : "単位未指定";

    internal double DisplayLength(double value) => ResultSet.Units.Length switch
    {
        "m" => value * 1000,
        "mm" => value,
        _ => value,
    };

    internal string DisplayLengthUnit => ResultSet.Units.Length is "m" or "mm" ? "mm" : "単位未指定";

    private static bool ContainsLl(string symbol) =>
        symbol.Contains("LL", StringComparison.OrdinalIgnoreCase);

    private static string UnitLabel(string unit) => unit is "m" or "mm" or "N" or "kN"
        ? unit : "単位未指定";
}

/// <summary>UI-thread publication point. Preparation and validation finish before Commit.</summary>
internal sealed class CalculationResultStore
{
    internal static CalculationResultStore Instance { get; } = new();
    private long _derivedGeneration;
    private CalculationResultStore()
    {
        providers.InputDataService.Instance.FileReplaced += _ => Clear();
        providers.InputDataService.Instance.DimensionChanged += _ => Clear();
        components.input.InputCombineService.Instance.RowsChanged += OnDerivedInputChanged;
    }
    internal CalculationResultPresentation? Current { get; private set; }
    internal long? CurrentInputRevision { get; private set; }
    internal event EventHandler? Changed;

    internal void Commit(CalculationResultPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        _derivedGeneration++;
        Current = presentation;
        CurrentInputRevision = providers.InputDataService.Instance.CalculationInputRevision;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void Clear()
    {
        _derivedGeneration++;
        Current = null;
        CurrentInputRevision = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async void OnDerivedInputChanged(object? sender, EventArgs args)
    {
        CalculationResultPresentation? current = Current;
        if (current is null) return;
        long generation = ++_derivedGeneration;
        current.UpdateDerived(null);
        Changed?.Invoke(this, EventArgs.Empty);
        try
        {
            var input = components.input.InputCombineService.Instance;
            CalculationDerivedInputSnapshot snapshot = CalculationDerivedInputSnapshot.Capture(
                input.DefineRows, input.CombineRows, input.PickupRows);
            CalculationDerivedPresentation derived = await Task.Run(() =>
                CalculationDerivedPresenter.Build(current.ResultSet, current.Dimension, snapshot));
            if (!ReferenceEquals(Current, current) || generation != _derivedGeneration) return;
            current.UpdateDerived(derived);
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception error)
        {
            System.Diagnostics.Trace.WriteLine($"Derived result refresh failed: {error}");
        }
    }
}

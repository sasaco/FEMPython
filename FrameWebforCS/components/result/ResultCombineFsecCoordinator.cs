using FrameWebforCS.providers;
using FrameWebforCS.components.input;
using System.Collections.Immutable;
using System.Globalization;

namespace FrameWebforCS.components.result;

internal enum CombineFsecState { Loading, Valid, Invalid }

internal sealed class ResultCombineFsecCoordinator
{
    private static readonly Lazy<ResultCombineFsecCoordinator> _instance =
        new(() => new ResultCombineFsecCoordinator());
    public static ResultCombineFsecCoordinator Instance => _instance.Value;

    private readonly object _sync = new();
    private CombineFsecState _state = CombineFsecState.Invalid;
    private ResultCombineFsecSnapshot? _snapshot;
    private long _revision;
    private int _dimension = 3;
    private bool _hasCompletedLoad;
    private string? _error;
    public event EventHandler? Changed;

    private ResultCombineFsecCoordinator()
    {
        ResultFsecService.Instance.Changed += (_, _) => OnSourceChanged();
        InputCombineService.Instance.RowsChanged += (_, _) => OnSourceChanged();
        InputLoadService.Instance.CasesChanged += (_, _) => OnSourceChanged();
    }

    public CombineFsecState State { get { lock (_sync) return _state; } }
    public long Revision { get { lock (_sync) return _revision; } }
    public ResultCombineFsecSnapshot? Snapshot { get { lock (_sync) return _snapshot; } }
    public string? Error { get { lock (_sync) return _error; } }

    public void BeginLoad()
    {
        lock (_sync)
        {
            _revision++;
            _state = CombineFsecState.Loading;
            _snapshot = null;
            _error = null;
        }
        DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
    }

    public void CompleteLoad(int dimension)
    {
        if (dimension is not (2 or 3))
        {
            FailLoad();
            throw new ArgumentOutOfRangeException(nameof(dimension));
        }
        lock (_sync)
        {
            _dimension = dimension;
            _hasCompletedLoad = true;
        }
        RefreshCore(allowFromInvalid: true);
    }

    public void FailLoad()
    {
        lock (_sync)
        {
            _revision++;
            _state = CombineFsecState.Invalid;
            _snapshot = null;
            _hasCompletedLoad = false;
            _error = null;
        }
        DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
    }

    public void Refresh() => RefreshCore(allowFromInvalid: false);

    private void OnSourceChanged()
    {
        lock (_sync)
            if (!_hasCompletedLoad || _state == CombineFsecState.Loading) return;
        RefreshCore(allowFromInvalid: false);
    }

    private void RefreshCore(bool allowFromInvalid)
    {
        long revision;
        int dimension;
        lock (_sync)
        {
            if (_state == CombineFsecState.Loading && !allowFromInvalid) return;
            if (_state == CombineFsecState.Invalid && !allowFromInvalid && !_hasCompletedLoad) return;
            revision = ++_revision;
            dimension = _dimension;
            _snapshot = null;
            _state = CombineFsecState.Invalid;
            _error = null;
        }
        DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);

        ResultCombineFsecSnapshot candidate;
        try { candidate = Capture(revision, dimension); }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (_revision != revision || _state == CombineFsecState.Loading) return;
                _error = exception.Message;
            }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
            return;
        }
        lock (_sync)
        {
            if (_revision != revision || _state == CombineFsecState.Loading) return;
            _snapshot = candidate;
            _state = CombineFsecState.Valid;
        }
        DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
    }

    private static ResultCombineFsecSnapshot Capture(long revision, int dimension)
    {
        var source = ResultFsecService.Instance.getFsec();
        var combine = InputCombineService.Instance;
        if (source.Count > ResultCombineFsecAggregator.MaxDefinitions ||
            combine.DefineRows.Count > ResultCombineFsecAggregator.MaxDefinitions ||
            combine.CombineRows.Count > ResultCombineFsecAggregator.MaxCombinations)
            throw new InvalidOperationException("Result source exceeds snapshot limits.");
        long segmentCount = 0;
        foreach (var result in source.Values)
        {
            foreach (var member in result.Values)
            {
                segmentCount = checked(segmentCount + member.Count);
                ResultCombineFsecAggregator.ValidateBudget(segmentCount * 2, 0, 0);
            }
        }
        long terms = combine.DefineRows.Values.Sum(row => (long)row.Coefficients.Count) +
            combine.CombineRows.Values.Sum(row => (long)row.Coefficients.Count);
        ResultCombineFsecAggregator.ValidateBudget(segmentCount * 2, terms, 0);
        var members = InputMembersService.Instance.Members;
        var cases = source.Select(result =>
        {
            var rows = ImmutableArray.CreateBuilder<FsecRowSnapshot>();
            foreach (var member in result.Value)
            {
                clsMember? memberInfo = null;
                if (int.TryParse(member.Key, NumberStyles.None, CultureInfo.InvariantCulture,
                    out int memberNumber) && memberNumber >= 1 && memberNumber <= members.Count)
                    memberInfo = members[memberNumber - 1];
                double location = 0;
                int index = 0;
                int pointCount = member.Value.Count;
                foreach (clsFsec segment in member.Value.Values)
                {
                    double length = segment.L ?? 0;
                    if (!double.IsFinite(length) || length < 0)
                        throw new InvalidOperationException($"Member '{member.Key}' has an invalid segment length.");
                    string startNode = index == 0 ? memberInfo?.ni ?? string.Empty : string.Empty;
                    string endNode = index == pointCount - 1 ? memberInfo?.nj ?? string.Empty : string.Empty;
                    rows.Add(new FsecRowSnapshot(member.Key, index == 0 ? member.Key : string.Empty,
                        startNode, location, new FsecVector(LegacyRound(segment.fxi ?? 0, 100),
                            LegacyRound(segment.fyi ?? 0, 100), LegacyRound(segment.fzi ?? 0, 100),
                            LegacyRound(segment.mxi ?? 0, 100), LegacyRound(segment.myi ?? 0, 100),
                            LegacyRound(segment.mzi ?? 0, 100))));
                    location += LegacyRound(length, 1_000);
                    rows.Add(new FsecRowSnapshot(member.Key, string.Empty,
                        endNode, location, new FsecVector(LegacyRound(segment.fxj ?? 0, 100),
                            LegacyRound(segment.fyj ?? 0, 100), LegacyRound(segment.fzj ?? 0, 100),
                            LegacyRound(segment.mxj ?? 0, 100), LegacyRound(segment.myj ?? 0, 100),
                            LegacyRound(segment.mzj ?? 0, 100))));
                    index++;
                }
            }
            return new FsecCaseSnapshot(result.Key, rows.ToImmutable());
        }).ToImmutableArray();
        long largestCaseRows = cases.IsEmpty ? 0 :
            cases.Max(item => (long)ResultCombineFsecAggregator.CountStationRows(item.Rows));
        int modeCount = dimension == 3 ? 12 : 6;
        ResultCombineFsecAggregator.ValidateBudget(segmentCount * 2, terms,
            checked(largestCaseRows * modeCount * combine.CombineRows.Count * 10L));

        var definitions = combine.DefineRows.Values.Select(row =>
            new DefineDisgSnapshot(row.Id, row.Coefficients.Values.ToImmutableArray()))
            .ToImmutableArray();
        var combinations = combine.CombineRows.Values.Select(row =>
            new CombineDisgSnapshot(row.Id, row.name,
                row.Coefficients.Select(coefficient =>
                    new CombineDisgTerm(coefficient.Key, coefficient.Value)).ToImmutableArray()))
            .ToImmutableArray();
        var caseIds = InputLoadService.Instance.CaseIds
            .Select(id => int.TryParse(id, NumberStyles.None,
                CultureInfo.InvariantCulture, out int value) && value > 0 ? value : 0)
            .Where(value => value > 0).ToImmutableArray();
        return new ResultCombineFsecSnapshot(revision, dimension, cases,
            definitions, combinations, caseIds);
    }

    private static double LegacyRound(double value, double factor) =>
        Math.Floor(value * factor + 0.5) / factor;
}

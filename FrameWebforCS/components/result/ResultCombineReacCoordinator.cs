using FrameWebforCS.providers;
using FrameWebforCS.components.input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace FrameWebforCS.components.result
{
    internal enum CombineReacState { Loading, Valid, Invalid }

    internal sealed class ResultCombineReacCoordinator
    {
        private static readonly Lazy<ResultCombineReacCoordinator> _instance =
            new(() => new ResultCombineReacCoordinator());
        public static ResultCombineReacCoordinator Instance => _instance.Value;

        private readonly object _sync = new();
        private CombineReacState _state = CombineReacState.Invalid;
        private ResultCombineReacSnapshot? _snapshot;
        private long _revision;
        private int _dimension = 3;
        private bool _hasCompletedLoad;
        private string? _error;
        public event EventHandler? Changed;

        private ResultCombineReacCoordinator()
        {
            ResultReacService.Instance.Changed += (_, _) => OnSourceChanged();
            InputCombineService.Instance.RowsChanged += (_, _) => OnSourceChanged();
            InputLoadService.Instance.CasesChanged += (_, _) => OnSourceChanged();
        }

        public CombineReacState State { get { lock (_sync) return _state; } }
        public long Revision { get { lock (_sync) return _revision; } }
        public ResultCombineReacSnapshot? Snapshot { get { lock (_sync) return _snapshot; } }
        public string? Error { get { lock (_sync) return _error; } }

        public void BeginLoad()
        {
            lock (_sync)
            {
                _revision++;
                _state = CombineReacState.Loading;
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
                _state = CombineReacState.Invalid;
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
            {
                if (!_hasCompletedLoad || _state == CombineReacState.Loading) return;
            }
            RefreshCore(allowFromInvalid: false);
        }

        private void RefreshCore(bool allowFromInvalid)
        {
            ResultCombineReacSnapshot? candidate;
            long targetRevision;
            int dimension;
            lock (_sync)
            {
                if (_state == CombineReacState.Loading && !allowFromInvalid) return;
                if (_state == CombineReacState.Invalid && !allowFromInvalid && !_hasCompletedLoad) return;
                targetRevision = ++_revision;
                dimension = _dimension;
                _snapshot = null;
                _state = CombineReacState.Invalid;
                _error = null;
            }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
            try
            {
                candidate = Capture(targetRevision, dimension);
            }
            catch (Exception exception)
            {
                // A partially copied generation must never be published.
                lock (_sync)
                {
                    if (_revision != targetRevision || _state == CombineReacState.Loading) return;
                    _error = exception.Message;
                }
                DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
                return;
            }
            lock (_sync)
            {
                if (_revision != targetRevision || _state == CombineReacState.Loading) return;
                _snapshot = candidate;
                _state = CombineReacState.Valid;
            }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private static ResultCombineReacSnapshot Capture(long revision, int dimension)
        {
            var sourceReac = ResultReacService.Instance.getReac();
            var sourceCombine = InputCombineService.Instance;
            var sourceLoad = InputLoadService.Instance;
            int defineCount = sourceCombine.DefineRows.Count;
            int combineCount = sourceCombine.CombineRows.Count;
            if (defineCount > ResultCombineReacAggregator.MaxDefinitions ||
                combineCount > ResultCombineReacAggregator.MaxCombinations ||
                sourceReac.Count > ResultCombineReacAggregator.MaxDefinitions)
                throw new InvalidOperationException("Result source exceeds snapshot limits.");
            long nodes = 0;
            foreach (var caseValue in sourceReac.Values)
            {
                nodes = checked(nodes + caseValue.Count);
                if (caseValue.Count > ResultCombineReacAggregator.MaxNodes ||
                    nodes > ResultCombineReacAggregator.MaxNodes)
                    throw new InvalidOperationException("Result nodes exceed snapshot limits.");
            }
            long terms = sourceCombine.DefineRows.Values.Sum(row => (long)row.Coefficients.Count) +
                sourceCombine.CombineRows.Values.Sum(row => (long)row.Coefficients.Count);
            if (terms > ResultCombineReacAggregator.MaxScalarOperations)
                throw new InvalidOperationException("Combination terms exceed snapshot limits.");
            int modeCount = dimension == 3 ? 12 : 6;
            long nodeCount = sourceReac.Count == 0 ? 0 : sourceReac.Values.Max(value => value.Count);
            long potentialCells = checked(nodeCount * modeCount * combineCount * 8L);
            if (potentialCells > ResultCombineReacAggregator.MaxOutputCells)
                throw new InvalidOperationException("Combination output exceeds snapshot limits.");

            var reactions = sourceReac.Select(result => new ReacCaseSnapshot(result.Key,
                result.Value.Select(node => new ReacNodeSnapshot(node.Key, new ReacVector(
                    node.Value.tx ?? 0, node.Value.ty ?? 0, node.Value.tz ?? 0,
                    node.Value.mx ?? 0, node.Value.my ?? 0, node.Value.mz ?? 0)))
                .ToImmutableArray())).ToImmutableArray();
            var definitions = sourceCombine.DefineRows.Values.Select(row =>
                new DefineReacSnapshot(row.Id, row.Coefficients.Values.ToImmutableArray()))
                .ToImmutableArray();
            var combinations = sourceCombine.CombineRows.Values.Select(row =>
                new CombineReacSnapshot(row.Id, row.name,
                    row.Coefficients.Select(coefficient =>
                        new CombineReacTerm(coefficient.Key, coefficient.Value)).ToImmutableArray()))
                .ToImmutableArray();
            var caseIds = sourceLoad.CaseIds
                .Select(id => int.TryParse(id, NumberStyles.None,
                    CultureInfo.InvariantCulture, out int value) && value > 0 ? value : 0)
                .Where(value => value > 0).ToImmutableArray();
            return new ResultCombineReacSnapshot(revision, dimension,
                reactions, definitions, combinations, caseIds);
        }

    }
}

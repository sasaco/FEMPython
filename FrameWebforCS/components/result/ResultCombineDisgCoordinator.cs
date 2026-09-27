using FrameWebforCS.providers;
using FrameWebforCS.components.input;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace FrameWebforCS.components.result
{
    internal enum CombineDisgState { Loading, Valid, Invalid }

    internal sealed class ResultCombineDisgCoordinator
    {
        private static readonly Lazy<ResultCombineDisgCoordinator> _instance =
            new(() => new ResultCombineDisgCoordinator());
        public static ResultCombineDisgCoordinator Instance => _instance.Value;

        private readonly object _sync = new();
        private CombineDisgState _state = CombineDisgState.Invalid;
        private ResultCombineDisgSnapshot? _snapshot;
        private long _revision;
        private int _dimension = 3;
        private bool _hasCompletedLoad;
        private string? _error;
        public event EventHandler? Changed;

        private ResultCombineDisgCoordinator()
        {
            ResultDisgService.Instance.Changed += (_, _) => OnSourceChanged();
            InputCombineService.Instance.RowsChanged += (_, _) => OnSourceChanged();
            InputLoadService.Instance.CasesChanged += (_, _) => OnSourceChanged();
        }

        public CombineDisgState State { get { lock (_sync) return _state; } }
        public long Revision { get { lock (_sync) return _revision; } }
        public ResultCombineDisgSnapshot? Snapshot { get { lock (_sync) return _snapshot; } }
        public string? Error { get { lock (_sync) return _error; } }

        public void BeginLoad()
        {
            lock (_sync)
            {
                _revision++;
                _state = CombineDisgState.Loading;
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
                _state = CombineDisgState.Invalid;
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
                if (!_hasCompletedLoad || _state == CombineDisgState.Loading) return;
            }
            RefreshCore(allowFromInvalid: false);
        }

        private void RefreshCore(bool allowFromInvalid)
        {
            ResultCombineDisgSnapshot? candidate;
            long targetRevision;
            int dimension;
            lock (_sync)
            {
                if (_state == CombineDisgState.Loading && !allowFromInvalid) return;
                if (_state == CombineDisgState.Invalid && !allowFromInvalid && !_hasCompletedLoad) return;
                targetRevision = ++_revision;
                dimension = _dimension;
                _snapshot = null;
                _state = CombineDisgState.Invalid;
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
                    if (_revision != targetRevision || _state == CombineDisgState.Loading) return;
                    _error = exception.Message;
                }
                DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
                return;
            }
            lock (_sync)
            {
                if (_revision != targetRevision || _state == CombineDisgState.Loading) return;
                _snapshot = candidate;
                _state = CombineDisgState.Valid;
            }
            DocumentReplacementNotifications.Publish(Changed, this, EventArgs.Empty);
        }

        private static ResultCombineDisgSnapshot Capture(long revision, int dimension)
        {
            var sourceDisg = ResultDisgService.Instance.getDisg();
            var sourceCombine = InputCombineService.Instance;
            var sourceLoad = InputLoadService.Instance;
            int defineCount = sourceCombine.DefineRows.Count;
            int combineCount = sourceCombine.CombineRows.Count;
            if (defineCount > ResultCombineDisgAggregator.MaxDefinitions ||
                combineCount > ResultCombineDisgAggregator.MaxCombinations ||
                sourceDisg.Count > ResultCombineDisgAggregator.MaxDefinitions)
                throw new InvalidOperationException("Result source exceeds snapshot limits.");
            long nodes = 0;
            foreach (var caseValue in sourceDisg.Values)
            {
                nodes = checked(nodes + caseValue.Count);
                if (caseValue.Count > ResultCombineDisgAggregator.MaxNodes ||
                    nodes > ResultCombineDisgAggregator.MaxNodes)
                    throw new InvalidOperationException("Result nodes exceed snapshot limits.");
            }
            long terms = sourceCombine.DefineRows.Values.Sum(row => (long)row.Coefficients.Count) +
                sourceCombine.CombineRows.Values.Sum(row => (long)row.Coefficients.Count);
            if (terms > ResultCombineDisgAggregator.MaxScalarOperations)
                throw new InvalidOperationException("Combination terms exceed snapshot limits.");
            int modeCount = dimension == 3 ? 12 : 6;
            long nodeCount = sourceDisg.Count == 0 ? 0 : sourceDisg.Values.Max(value => value.Count);
            long potentialCells = checked(nodeCount * modeCount * combineCount * 8L);
            if (potentialCells > ResultCombineDisgAggregator.MaxOutputCells)
                throw new InvalidOperationException("Combination output exceeds snapshot limits.");

            // Match the legacy base worker: auxiliary notice/load nodes are hidden,
            // and metres/radians become millimetres/milliradians before derivation.
            var displacements = sourceDisg.Select(result => new DisgCaseSnapshot(result.Key,
                result.Value.Select(node => (node, Id: DisplayNodeId(node.Key)))
                    .Where(item => !item.Id.Contains('n') && !item.Id.Contains('l'))
                    .Select(item => new DisgNodeSnapshot(item.Id, new DisgVector(
                        Scale(item.node.Value.dx), Scale(item.node.Value.dy),
                        Scale(item.node.Value.dz), Scale(item.node.Value.rx),
                        Scale(item.node.Value.ry), Scale(item.node.Value.rz))))
                    .ToImmutableArray())).ToImmutableArray();
            var definitions = sourceCombine.DefineRows.Values.Select(row =>
                new DefineDisgSnapshot(row.Id, row.Coefficients.Values.ToImmutableArray()))
                .ToImmutableArray();
            var combinations = sourceCombine.CombineRows.Values.Select(row =>
                new CombineDisgSnapshot(row.Id, row.name,
                    row.Coefficients.Select(coefficient =>
                        new CombineDisgTerm(coefficient.Key, coefficient.Value)).ToImmutableArray()))
                .ToImmutableArray();
            var caseIds = sourceLoad.CaseIds
                .Select(id => int.TryParse(id, NumberStyles.None,
                    CultureInfo.InvariantCulture, out int value) && value > 0 ? value : 0)
                .Where(value => value > 0).ToImmutableArray();
            return new ResultCombineDisgSnapshot(revision, dimension,
                displacements, definitions, combinations, caseIds);
        }

        private static string DisplayNodeId(string sourceId)
        {
            int marker = sourceId.IndexOf("node", StringComparison.Ordinal);
            return marker < 0 ? sourceId : sourceId.Remove(marker, 4);
        }

        private static double Scale(double? value)
        {
            double result = (value ?? 0) * 1_000;
            if (!double.IsFinite(result))
                throw new InvalidOperationException("Displacement exceeds the display range.");
            return result;
        }

    }
}

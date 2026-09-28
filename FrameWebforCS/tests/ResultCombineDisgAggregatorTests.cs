using FrameWebforCS.components.result;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class ResultCombineDisgAggregatorTests
{
    private static ResultCombineDisgSnapshot Snapshot(
        ImmutableArray<DefineDisgSnapshot> definitions,
        ImmutableArray<CombineDisgSnapshot> combinations,
        ImmutableArray<int> staticCaseIds = default,
        int dimension = 2)
    {
        return new ResultCombineDisgSnapshot(1, dimension,
            ImmutableArray.Create(
                new DisgCaseSnapshot("1", ImmutableArray.Create(
                    new DisgNodeSnapshot("10", new DisgVector(2, 4, 1, 0, 0, 6)),
                    new DisgNodeSnapshot("20", new DisgVector(-3, 1, 1, 0, 0, -2)))),
                new DisgCaseSnapshot("2", ImmutableArray.Create(
                    new DisgNodeSnapshot("10", new DisgVector(-5, 1, 2, 0, 0, -8)),
                    new DisgNodeSnapshot("20", new DisgVector(7, -4, 2, 0, 0, 9))))),
            definitions, combinations, staticCaseIds.IsDefault ? ImmutableArray<int>.Empty : staticCaseIds);
    }

    [Fact]
    public void DefineEnvelopeSelectsWholeVectorAndCombineUsesIdsRatherThanRows()
    {
        var snapshot = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("12", ImmutableArray.Create(1, -2))),
            ImmutableArray.Create(new CombineDisgSnapshot("40", "test",
                ImmutableArray.Create(new CombineDisgTerm(12, 2.5)))));
        var result = ResultCombineDisgAggregator.Calculate(snapshot);
        var combination = Assert.Single(result.Cases);
        Assert.Equal("40", combination.Id);
        Assert.Equal("test", combination.Name);
        var max10 = combination.Rows["dx_max"].Single(row => row.Id == "10");
        Assert.Equal(12.5, max10.Dx);
        Assert.Equal(-2.5, max10.Dy); // selected -case 2, not componentwise envelope
        Assert.Equal("-12", max10.Case);
        var min10 = combination.Rows["dx_min"].Single(row => row.Id == "10");
        Assert.Equal(5, min10.Dx);
        Assert.Equal("+12", min10.Case);
        Assert.Equal(6, combination.Rows.Count);
    }

    [Fact]
    public void NegativeCoefficientAndTiesRetainFirstSourceCase()
    {
        var snapshot = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("1", ImmutableArray.Create(1, 1))),
            ImmutableArray.Create(new CombineDisgSnapshot("3", null,
                ImmutableArray.Create(new CombineDisgTerm(1, -2)))));
        var result = ResultCombineDisgAggregator.Calculate(snapshot);
        var node = result.Cases[0].Rows["dx_max"].Single(row => row.Id == "10");
        Assert.Equal(-4, node.Dx);
        Assert.Equal("-1", node.Case);
        Assert.Equal(-8, node.Dy);
    }

    [Fact]
    public void NegativeCombineCoefficientAlwaysUsesLegacyNegativeSourceLabel()
    {
        var snapshot = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("12", ImmutableArray.Create(-2))),
            ImmutableArray.Create(new CombineDisgSnapshot("40", null,
                ImmutableArray.Create(new CombineDisgTerm(12, -1)))));
        var node = ResultCombineDisgAggregator.Calculate(snapshot)
            .Cases[0].Rows["dx_max"].Single(row => row.Id == "10");
        Assert.Equal(-5, node.Dx);
        Assert.Equal("-12", node.Case);
    }

    [Fact]
    public void NumericNodeIdsFollowJavaScriptObjectKeyOrder()
    {
        var source = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("1", ImmutableArray.Create(1))),
            ImmutableArray.Create(new CombineDisgSnapshot("1", null,
                ImmutableArray.Create(new CombineDisgTerm(1, 1)))));
        var reordered = source with
        {
            Displacements = ImmutableArray.Create(new DisgCaseSnapshot("1",
                ImmutableArray.Create(
                    new DisgNodeSnapshot("20", new DisgVector(1, 0, 0, 0, 0, 0)),
                    new DisgNodeSnapshot("10", new DisgVector(2, 0, 0, 0, 0, 0)),
                    new DisgNodeSnapshot("B", new DisgVector(3, 0, 0, 0, 0, 0)),
                    new DisgNodeSnapshot("02", new DisgVector(4, 0, 0, 0, 0, 0)))))
        };
        var ids = ResultCombineDisgAggregator.Calculate(reordered)
            .Cases[0].Rows["dx_max"].Select(row => row.Id).ToArray();
        Assert.Equal(new[] { "10", "20", "B", "02" }, ids);
    }

    [Fact]
    public void StaticCaseFallbackOnlyAppliesWhenDefineIsAbsent()
    {
        var combine = ImmutableArray.Create(new CombineDisgSnapshot("5", null,
            ImmutableArray.Create(new CombineDisgTerm(2, 1))));
        var fallback = Snapshot(ImmutableArray<DefineDisgSnapshot>.Empty, combine,
            ImmutableArray.Create(2, 999));
        Assert.Equal(-5, ResultCombineDisgAggregator.Calculate(fallback)
            .Cases[0].Rows["dx_max"].Single(row => row.Id == "10").Dx);
        var explicitDefine = Snapshot(ImmutableArray.Create(
            new DefineDisgSnapshot("1", ImmutableArray.Create(1))), combine,
            ImmutableArray.Create(2));
        Assert.Empty(ResultCombineDisgAggregator.Calculate(explicitDefine)
            .Cases[0].Rows["dx_max"]);
    }

    [Fact]
    public void ThreeDimensionalModesAndCancellationAreObserved()
    {
        var snapshot = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("1", ImmutableArray.Create(1))),
            ImmutableArray.Create(new CombineDisgSnapshot("1", null,
                ImmutableArray.Create(new CombineDisgTerm(1, 1)))), dimension: 3);
        Assert.Equal(12, ResultCombineDisgAggregator.Calculate(snapshot).Cases[0].Rows.Count);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            ResultCombineDisgAggregator.Calculate(snapshot, cancellation.Token));
    }

    [Fact]
    public void ZeroCaseProvidesNodeShapeAndMissingOperandsAreSkipped()
    {
        var snapshot = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("2", ImmutableArray.Create(999, 0))),
            ImmutableArray.Create(new CombineDisgSnapshot("7", null,
                ImmutableArray.Create(new CombineDisgTerm(2, 3), new CombineDisgTerm(9, 4)))));
        var rows = ResultCombineDisgAggregator.Calculate(snapshot).Cases[0].Rows["dx_max"];
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => { Assert.Equal(0, row.Dx); Assert.Equal("", row.Case); });
    }

    [Fact]
    public void NegativeCombineCoefficientLabelsZeroSourceCaseLikeLegacyWorker()
    {
        var snapshot = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("2", ImmutableArray.Create(0))),
            ImmutableArray.Create(new CombineDisgSnapshot("7", null,
                ImmutableArray.Create(new CombineDisgTerm(2, -3)))));
        var rows = ResultCombineDisgAggregator.Calculate(snapshot).Cases[0].Rows["dx_max"];
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => { Assert.Equal(0, row.Dx); Assert.Equal("-2", row.Case); });
    }

    [Fact]
    public void InconsistentReferencedNodeSetsAreRejected()
    {
        var source = Snapshot(
            ImmutableArray.Create(new DefineDisgSnapshot("1", ImmutableArray.Create(1, 2))),
            ImmutableArray.Create(new CombineDisgSnapshot("1", null,
                ImmutableArray.Create(new CombineDisgTerm(1, 1)))));
        var missingNode = source with
        {
            Displacements = source.Displacements.SetItem(1,
                new DisgCaseSnapshot("2", ImmutableArray.Create(
                    new DisgNodeSnapshot("10", new DisgVector(1, 1, 1, 1, 1, 1)))))
        };
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineDisgAggregator.Calculate(missingNode));
    }

    [Fact]
    public void CombinationCountLimitRejectsTheFirstExcessCase()
    {
        var cases = Enumerable.Range(1, ResultCombineDisgAggregator.MaxCombinations)
            .Select(id => new CombineDisgSnapshot(id.ToString(), null,
                ImmutableArray<CombineDisgTerm>.Empty)).ToImmutableArray();
        var withinLimit = Snapshot(ImmutableArray<DefineDisgSnapshot>.Empty, cases);
        Assert.Equal(ResultCombineDisgAggregator.MaxCombinations,
            ResultCombineDisgAggregator.Calculate(withinLimit).Cases.Count);
        var excess = withinLimit with
        {
            Combines = cases.Add(new CombineDisgSnapshot("excess", null,
                ImmutableArray<CombineDisgTerm>.Empty))
        };
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineDisgAggregator.Calculate(excess));
    }

    [Fact]
    public void BudgetBoundariesRejectOnlyValuesAboveThePublishedLimits()
    {
        ResultCombineDisgAggregator.ValidateBudget(
            ResultCombineDisgAggregator.MaxNodes,
            ResultCombineDisgAggregator.MaxScalarOperations,
            ResultCombineDisgAggregator.MaxOutputCells);
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineDisgAggregator.ValidateBudget(
                ResultCombineDisgAggregator.MaxNodes + 1L, 0, 0));
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineDisgAggregator.ValidateBudget(
                0, ResultCombineDisgAggregator.MaxScalarOperations + 1, 0));
        Assert.Throws<InvalidOperationException>(() =>
            ResultCombineDisgAggregator.ValidateBudget(
                0, 0, ResultCombineDisgAggregator.MaxOutputCells + 1));
    }

    [Fact]
    public void DisplacementCandidateParseIsAtomicAndRejectsNonFiniteValues()
    {
        var service = ResultDisgService.Instance;
        service.clear();
        using var valid = JsonDocument.Parse(
            """{"result":{"1":{"disg":{"10":{"dx":2,"dy":null,"rz":-1}}}}}""");
        service.setDisgJson(valid.RootElement);
        Assert.Equal(2, service.getDisg()["1"]["10"].dx);
        using var invalid = JsonDocument.Parse(
            """{"result":{"1":{"disg":{"10":{"dx":3}}},"2":{"disg":{"20":{"dx":"bad"}}}}}""");
        Assert.Throws<JsonException>(() => service.setDisgJson(invalid.RootElement));
        Assert.Equal(2, service.getDisg()["1"]["10"].dx);
        service.clear();
    }
}

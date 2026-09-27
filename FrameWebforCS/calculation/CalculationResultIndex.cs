using System.Collections.ObjectModel;

namespace FrameWebforCS.calculation;

public sealed class ResultIndex
{
    private readonly IReadOnlyDictionary<ResultCoordinate, AnalysisResult> byCoordinate;

    public ResultIndex(AnalysisResultSet resultSet)
    {
        ArgumentNullException.ThrowIfNull(resultSet);
        AnalysisResultSetValidator.Validate(resultSet);

        Dictionary<ResultCoordinate, AnalysisResult> index = [];
        foreach (AnalysisResult result in resultSet.Results)
        {
            if (!index.TryAdd(result.Coordinate, result))
            {
                throw new AnalysisContractException($"Duplicate result coordinate '{result.Coordinate}'.");
            }
        }

        byCoordinate = new ReadOnlyDictionary<ResultCoordinate, AnalysisResult>(index);
        Ordered = Frozen.List(resultSet.Results, nameof(resultSet));
    }

    public IReadOnlyList<AnalysisResult> Ordered { get; }

    public int Count => byCoordinate.Count;

    public AnalysisResult this[ResultCoordinate coordinate] => byCoordinate[coordinate];

    public bool TryGet(ResultCoordinate coordinate, out AnalysisResult? result)
        => byCoordinate.TryGetValue(coordinate, out result);

    public IReadOnlyList<AnalysisResult> ForCase(string caseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        return Frozen.List(Ordered.Where(result => result.CaseId == caseId), nameof(caseId));
    }
}

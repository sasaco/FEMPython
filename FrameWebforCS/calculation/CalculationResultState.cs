namespace FrameWebforCS.calculation;

/// <summary>
/// Owns the last fully validated result. A rejected candidate never replaces the current value.
/// </summary>
public sealed class AnalysisResultState
{
    public AnalysisResultSet? Current { get; private set; }

    public ResultIndex? Index { get; private set; }

    public void Commit(AnalysisResultSet candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ResultIndex candidateIndex = new(candidate);
        Current = candidate;
        Index = candidateIndex;
    }

    public void CommitJson(ReadOnlySpan<byte> utf8Json)
    {
        AnalysisResultSet candidate = AnalysisResultSetJson.Deserialize(utf8Json);
        Commit(candidate);
    }
}

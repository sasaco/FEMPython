using FrameWebforCS.calculation;
using Xunit;

namespace FrameWebforCS.Tests;

// GLOSSARY: 部材の状態. nonlinear-states.json is the engine's result for the first 15 control
// targets of the P7 model; the footing's rotational spring passes its 1st point at step 13.
public sealed class NonlinearResponseResultTests
{
    private static string Fixture(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        return File.ReadAllText(Path.Combine(root, "FrameWeb", "tests", "data", "contracts", "positive", name + ".json"));
    }

    [Fact]
    public void LoadStepsCarryTheMemberAndSpringStates()
    {
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(Fixture("nonlinear-states"));
        LoadStepAnalysisResult[] steps = result.Results.Cast<LoadStepAnalysisResult>().ToArray();

        Assert.Equal(16, steps.Length);
        NonlinearResponse final = steps[^1].NonlinearResponse!;
        NonlinearMemberState member = Assert.Single(final.Members);
        Assert.Equal("6", member.MemberId);
        NonlinearComponentState moment = member.Components["moment_z"];
        Assert.False(string.IsNullOrEmpty(moment.Branch));
        NonlinearSupportState support = Assert.Single(final.Supports);
        Assert.Equal("9", support.NodeId);
        Assert.Equal(new int?[] { 0, 1 }, new[] { 12, 13 }.Select(index =>
            steps[index].NonlinearResponse!.Supports[0].Components["rz"].PointsPassed));
    }

    [Fact]
    public void ResultsWrittenBeforeTheStatesExistedStillRead()
    {
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(Fixture("nonlinear-steps"));
        Assert.All(result.Results.Cast<LoadStepAnalysisResult>(), step => Assert.Null(step.NonlinearResponse));
    }
}

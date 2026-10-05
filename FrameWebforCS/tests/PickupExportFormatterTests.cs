using FrameWebforCS.calculation;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class PickupExportFormatterTests
{
    [Fact]
    public void TwoDimensionalPickupUsesPikColumnsAndFixedWidths()
    {
        string[] lines = PickupExportFormatter.Format(Presentation(2)).Split('\n');

        Assert.StartsWith("PickUpNo,着目力,部材No", lines[0]);
        Assert.Equal(8, lines.Length); // Header, M/S/N at both stations, final newline.
        Assert.Equal(100, lines[1].Length);
        Assert.Equal("1", lines[1][..5].Trim());
        Assert.Equal("M", lines[1][5..10].Trim());
        Assert.Equal("M1", lines[1][10..15].Trim());
        Assert.Equal("40", lines[1][15..20].Trim());
        Assert.Equal("41", lines[1][20..25].Trim());
        Assert.Equal("ITAN", lines[1][25..30].Trim());
        Assert.Equal("0.000", lines[1][30..40].Trim());
        Assert.Equal("1.00", lines[1][40..50].Trim()); // JS toFixed(2) of binary64 1.005.
        Assert.Equal("-2.00", lines[1][70..80].Trim());
        Assert.Equal("JTAN", lines[2][25..30].Trim());
        Assert.Equal("S", lines[3][5..10].Trim());
        Assert.Equal("N", lines[5][5..10].Trim());
    }

    [Fact]
    public void ThreeDimensionalPickupUsesCsvColumnsAndCorrelatedVectors()
    {
        string[] lines = PickupExportFormatter.Format(Presentation(3)).Split('\n');

        Assert.StartsWith("PickUpNo,着目断面力,部材No", lines[0]);
        Assert.Equal(14, lines.Length); // Header, six modes at both stations, final newline.
        string[] first = lines[1].Split(',');
        Assert.Equal(19, first.Length);
        Assert.Equal(["1", "fx", "M1", "40", "41", "ITAN", "0"], first[..7]);
        Assert.Equal(["10", "20", "30", "40", "50", "1.005"], first[7..13]);
        Assert.Equal(["-10", "-20", "-30", "-40", "-50", "-2"], first[13..19]);
        Assert.Equal("JTAN", lines[2].Split(',')[5]);
        Assert.Equal("mz", lines[11].Split(',')[1]);
    }

    [Fact]
    public void CsvProtectsTextCellsFromSpreadsheetFormulas()
    {
        CalculationResultPresentation presentation = Presentation(3, "=1+1");

        string first = PickupExportFormatter.Format(presentation).Split('\n')[1];

        Assert.StartsWith("'=1+1,fx,M1", first);
        Assert.StartsWith("' =1+1,fx,M1",
            PickupExportFormatter.Format(Presentation(3, " =1+1")).Split('\n')[1]);
    }

    [Fact]
    public void IncompleteDerivedResultCannotBeExported()
    {
        CalculationResultPresentation presentation = Presentation(2);
        presentation.UpdateDerived(null);

        Assert.Throws<InvalidOperationException>(() => PickupExportFormatter.Format(presentation));
    }

    private static CalculationResultPresentation Presentation(int dimension, string pickupId = "1")
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException();
        string path = Path.Combine(root, "FrameWeb", "tests", "data", "contracts", "positive", "single-static.json");
        AnalysisResultSet resultSet = AnalysisResultSetJson.Deserialize(File.ReadAllText(path));

        var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>();
        foreach (string focus in dimension == 2
            ? new[] { "mz", "fy", "fx" }
            : new[] { "fx", "fy", "fz", "mx", "my", "mz" })
        {
            modes[focus + "_max"] = [Row("S0", "40", [10, 20, 30, 40, 50, 1.005]),
                Row("S1", "40", [11, 21, 31, 41, 51, 2])];
            modes[focus + "_min"] = [Row("S0", "41", [-10, -20, -30, -40, -50, -2]),
                Row("S1", "41", [-11, -21, -31, -41, -51, -3])];
        }
        var pickup = new CalculationDerivedCase(pickupId, null,
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(),
            new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>(), modes);
        return new CalculationResultPresentation(resultSet,
            new CalculationDerivedPresentation([], [], [pickup]), dimension);
    }

    private static CalculationDerivedRow Row(string station, string source, double[] values)
    {
        string[] names = ["fx", "fy", "fz", "mx", "my", "mz"];
        return new CalculationDerivedRow("M1", station,
            names.Select((name, index) => (name, value: values[index]))
                .ToDictionary(item => item.name, item => item.value), source, source);
    }
}

using FrameWebforCS.calculation;
using FrameWebforCS.components.printing;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using System.Reflection;
using Xunit;

namespace PrintUi.Tests;

[CollectionDefinition("Print dialog selection", DisableParallelization = true)]
public sealed class PrintDialogSelectionCollection { }

[Collection("Print dialog selection")]
public sealed class SectionDiagramSelectionTests
{
    [Theory]
    [InlineData("断面力図")]
    [InlineData("COMBINE 断面力図")]
    [InlineData("PICKUP 断面力図")]
    public void UntouchedTwoDimensionalSectionDiagramUsesVisibleAutomaticSplitSettings(string label)
    {
        WithDialog(2, dialog =>
        {
            SelectOption(dialog, label, true);

            PrintSelection selection = dialog.ReadSelection();
            Assert.Equal(PrintLayout.SplitHorizontal, selection.Layout);
            Assert.Null(selection.ScaleX);
            Assert.Null(selection.ScaleY);
            Assert.Equal(PrintLayout.SplitHorizontal, Control<ComboBox>(dialog, "_layout").SelectedItem);
            Assert.Empty(Control<TextBox>(dialog, "_scaleX").Text);
            Assert.Empty(Control<TextBox>(dialog, "_scaleY").Text);
            Assert.Contains("自動", Control<TextBox>(dialog, "_scaleX").PlaceholderText);
            Assert.Contains("自動", Control<TextBox>(dialog, "_scaleY").PlaceholderText);
        });
    }

    [Theory]
    [InlineData(true, (int)PrintLayout.Single)]
    [InlineData(true, (int)PrintLayout.SplitVertical)]
    [InlineData(false, (int)PrintLayout.Single)]
    [InlineData(false, (int)PrintLayout.SplitVertical)]
    public void ExplicitDiagramSettingsSurviveOptionChanges(bool settingsBeforeSelection, int layoutValue)
    {
        WithDialog(2, dialog =>
        {
            void SetExplicitSettings()
            {
                Control<TextBox>(dialog, "_scaleX").Text = "150";
                Control<TextBox>(dialog, "_scaleY").Text = "300";
                CommitLayout(dialog, (PrintLayout)layoutValue);
            }
            void AssertExplicitSettings()
            {
                PrintSelection selection = dialog.ReadSelection();
                Assert.Equal((PrintLayout)layoutValue, selection.Layout);
                Assert.Equal(150, selection.ScaleX);
                Assert.Equal(300, selection.ScaleY);
            }

            if (settingsBeforeSelection) SetExplicitSettings();
            SelectOption(dialog, "断面力図", true);
            if (!settingsBeforeSelection) SetExplicitSettings();
            AssertExplicitSettings();
            SelectOption(dialog, "断面力図", false);
            AssertExplicitSettings();
            SelectOption(dialog, "COMBINE 断面力図", true);
            AssertExplicitSettings();
            SelectOption(dialog, "PICKUP 断面力図", true);
            AssertExplicitSettings();
        });
    }

    [Fact]
    public void OnlyEditedAxisKeepsItsValueWhenDiagramDefaultsChange()
    {
        WithDialog(2, dialog =>
        {
            Control<TextBox>(dialog, "_scaleX").Text = "150";
            SelectOption(dialog, "断面力図", true);
            PrintSelection diagram = dialog.ReadSelection();
            Assert.Equal(150, diagram.ScaleX);
            Assert.Null(diagram.ScaleY);
            Assert.Equal(PrintLayout.SplitHorizontal, diagram.Layout);

            SelectOption(dialog, "断面力図", false);
            PrintSelection input = dialog.ReadSelection();
            Assert.Equal(150, input.ScaleX);
            Assert.Equal(1, input.ScaleY);
            Assert.Equal(PrintLayout.Single, input.Layout);
        });
    }

    [Fact]
    public void ExplicitAutomaticScaleSurvivesReturningToInputOnly()
    {
        WithDialog(2, dialog =>
        {
            Control<TextBox>(dialog, "_scaleX").Clear();
            SelectOption(dialog, "断面力図", true);
            SelectOption(dialog, "断面力図", false);
            PrintSelection selection = dialog.ReadSelection();
            Assert.Null(selection.ScaleX);
            Assert.Equal(1, selection.ScaleY);
        });
    }

    [Fact]
    public void LastSectionDiagramDeselectionRestoresUntouchedLegacyControls()
    {
        WithDialog(2, dialog =>
        {
            SelectOption(dialog, "断面力図", true);
            SelectOption(dialog, "PICKUP 断面力図", true);
            SelectOption(dialog, "断面力図", false);
            Assert.Equal(PrintLayout.SplitHorizontal, dialog.ReadSelection().Layout);
            Assert.Null(dialog.ReadSelection().ScaleX);
            SelectOption(dialog, "PICKUP 断面力図", false);

            AssertLegacySettings(dialog);
            Assert.Equal("1", Control<TextBox>(dialog, "_scaleX").Text);
            Assert.Equal("1", Control<TextBox>(dialog, "_scaleY").Text);
            Assert.Equal(PrintLayout.Single, Control<ComboBox>(dialog, "_layout").SelectedItem);
        });
    }

    [Theory]
    [InlineData("_scaleX", "0")]
    [InlineData("_scaleY", "-1")]
    [InlineData("_scaleX", "NaN")]
    [InlineData("_scaleY", "Infinity")]
    [InlineData("_scaleX", "1000001")]
    [InlineData("_scaleY", "invalid")]
    public void InvalidExplicitScaleIsNotReplacedByDiagramDefaults(string axis, string value)
    {
        WithDialog(2, dialog =>
        {
            Control<TextBox>(dialog, axis).Text = value;
            SelectOption(dialog, "断面力図", true);
            Assert.Throws<InvalidOperationException>(() => dialog.ReadSelection());
            Assert.Equal(value, Control<TextBox>(dialog, axis).Text);
        });
    }

    [Theory]
    [InlineData("断面力図")]
    [InlineData("COMBINE 断面力図")]
    [InlineData("PICKUP 断面力図")]
    public void ThreeDimensionalSectionDiagramPreservesLegacySettings(string label)
    {
        WithDialog(3, dialog =>
        {
            SelectOption(dialog, label, true);
            AssertLegacySettings(dialog);
        });
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void InitialInputOnlySelectionKeepsLegacySettings(int dimension)
    {
        WithDialog(dimension, dialog =>
        {
            Assert.Equal([PrintOption.Input], dialog.ReadSelection().Options);
            AssertLegacySettings(dialog);
        });
    }

    private static void AssertLegacySettings(PrintDialogForm dialog)
    {
        PrintSelection selection = dialog.ReadSelection();
        Assert.Equal(PrintLayout.Single, selection.Layout);
        Assert.Equal(1, selection.ScaleX);
        Assert.Equal(1, selection.ScaleY);
    }

    private static void CommitLayout(PrintDialogForm dialog, PrintLayout layout)
    {
        ComboBox control = Control<ComboBox>(dialog, "_layout");
        control.SelectedItem = layout;
        // A user can explicitly accept Single while it is still the default selection.
        typeof(ComboBox).GetMethod("OnSelectionChangeCommitted", BindingFlags.Instance |
            BindingFlags.NonPublic)!.Invoke(control, [EventArgs.Empty]);
    }

    private static void SelectOption(PrintDialogForm dialog, string label, bool selected)
    {
        var options = Assert.IsType<CheckedListBox>(Assert.Single(dialog.Controls.Find("printOptions", true)));
        int index = Enumerable.Range(0, options.Items.Count)
            .Single(index => options.Items[index].ToString() == label);
        options.SetItemChecked(index, selected);
        Assert.Equal(selected, options.GetItemChecked(index));
    }

    private static T Control<T>(PrintDialogForm dialog, string field) where T : Control =>
        Assert.IsType<T>(typeof(PrintDialogForm).GetField(field,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog));

    private static void WithDialog(int dimension, Action<PrintDialogForm> action)
    {
        RunSta(() =>
        {
            InputDataService input = InputDataService.Instance;
            CalculationResultStore store = CalculationResultStore.Instance;
            int originalDimension = input.dimension;
            CalculationResultPresentation? originalResult = store.Current;
            try
            {
                input.dimension = dimension;
                store.Commit(Presentation(dimension));
                using var dialog = new PrintDialogForm(_ => []);
                action(dialog);
            }
            finally
            {
                input.dimension = originalDimension;
                if (originalResult is null) store.Clear();
                else store.Commit(originalResult);
            }
        });
    }

    private static CalculationResultPresentation Presentation(int dimension)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException();
        string fixture = File.ReadAllText(Path.Combine(root, "FrameWeb", "tests", "data",
            "contracts", "positive", "single-static.json"));
        var forces = new Dictionary<string, double>
        {
            ["fx"] = 10, ["fy"] = 0, ["fz"] = 0, ["mx"] = 0, ["my"] = 0, ["mz"] = 0,
        };
        var modes = new Dictionary<string, IReadOnlyList<CalculationDerivedRow>>
        {
            ["fx_max"] = [new("M1", "S0", forces, "D", "D")],
        };
        var derivedCase = new CalculationDerivedCase("1", "Result", new Dictionary<string,
            IReadOnlyList<CalculationDerivedRow>>(), new Dictionary<string,
            IReadOnlyList<CalculationDerivedRow>>(), modes);
        return new CalculationResultPresentation(AnalysisResultSetJson.Deserialize(fixture),
            new CalculationDerivedPresentation([], [derivedCase], [derivedCase]), dimension);
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA test did not finish.");
        if (failure is not null) throw failure;
    }
}

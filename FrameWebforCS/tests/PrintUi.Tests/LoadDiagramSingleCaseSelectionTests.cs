using FrameWebforCS.components.printing;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using System.Text.Json;
using Xunit;

namespace PrintUi.Tests;

/// <summary>荷重図を分割配置で出すときに 1 頁に 1 ケースにするケースの欄（FrameG の 2 段書きの単独のケース）。</summary>
// Opening a document changes the shared InputDataService, so this runs alone like the other selection tests.
[Collection("Print dialog selection")]
public sealed class LoadDiagramSingleCaseSelectionTests
{
    private const string Document = """
        {"dimension":2,"node":{"1":{"x":0,"y":0},"2":{"x":1,"y":0}},
         "member":{"1":{"ni":"1","nj":"2","e":"1"}},
         "element":{"1":{"1":{"E":10000,"A":1,"Iz":1}}},
         "load":{"1":{"name":"固定死荷重","symbol":"D1"},"2":{"name":"付加死荷重","symbol":"D2"},"3":{"name":"群集荷重"}}}
        """;

    [Fact]
    public void ListsTheLoadCasesAndSendsTheCheckedOnesForASplitLoadDiagram()
    {
        WithDialog(Document, dialog =>
        {
            var cases = (CheckedListBox)Assert.Single(dialog.Controls.Find("loadDiagramSingleCases", true));
            Assert.Equal(["1 固定死荷重", "2 付加死荷重", "3 群集荷重"], cases.Items.Cast<object>().Select(item => item.ToString()!).ToArray());
            Assert.False(cases.Enabled);

            Check(dialog, PrintOption.LoadDiagram);
            Assert.False(cases.Enabled);   // 1 段書きでは選べない
            Layout(dialog).SelectedItem = PrintLayout.SplitHorizontal;
            Assert.True(cases.Enabled);
            cases.SetItemChecked(0, true);
            cases.SetItemChecked(1, true);
            Assert.Equal(["1", "2"], dialog.ReadSelection().LoadDiagramSingleCaseIds);

            Layout(dialog).SelectedItem = PrintLayout.SplitVertical;
            Assert.Equal(["1", "2"], dialog.ReadSelection().LoadDiagramSingleCaseIds);
            Layout(dialog).SelectedItem = PrintLayout.Single;
            Assert.False(cases.Enabled);
            Assert.Null(dialog.ReadSelection().LoadDiagramSingleCaseIds);
        });
    }

    [Fact]
    public void WithoutTheLoadDiagramTheCheckedCasesAreNotSent()
    {
        WithDialog(Document, dialog =>
        {
            var cases = (CheckedListBox)Assert.Single(dialog.Controls.Find("loadDiagramSingleCases", true));
            Layout(dialog).SelectedItem = PrintLayout.SplitHorizontal;
            Check(dialog, PrintOption.LoadDiagram);
            cases.SetItemChecked(2, true);
            Assert.Equal(["3"], dialog.ReadSelection().LoadDiagramSingleCaseIds);
            Check(dialog, PrintOption.LoadDiagram, false);
            Assert.False(cases.Enabled);
            Assert.Null(dialog.ReadSelection().LoadDiagramSingleCaseIds);
            // 何も選ばなければ送らない（全ケースを 2 段に詰める）
            Check(dialog, PrintOption.LoadDiagram);
            cases.SetItemChecked(2, false);
            Assert.Null(dialog.ReadSelection().LoadDiagramSingleCaseIds);
        });
    }

    [Fact]
    public void TheListIsNotShownForAThreeDimensionalDocument()
    {
        WithDialog(Document.Replace("\"dimension\":2", "\"dimension\":3", StringComparison.Ordinal), dialog =>
        {
            Assert.Empty(dialog.Controls.Find("loadDiagramSingleCases", true));
            Check(dialog, PrintOption.LoadDiagram);
            Layout(dialog).SelectedItem = PrintLayout.SplitHorizontal;
            Assert.Null(dialog.ReadSelection().LoadDiagramSingleCaseIds);
        });
    }

    private static void Check(PrintDialogForm dialog, PrintOption option, bool value = true)
    {
        var options = (CheckedListBox)Assert.Single(dialog.Controls.Find("printOptions", true));
        // The items are private records; read their value by reflection.
        int index = Enumerable.Range(0, options.Items.Count).Single(i =>
            Equals(options.Items[i].GetType().GetProperty("Value")!.GetValue(options.Items[i]), option));
        options.SetItemChecked(index, value);
    }

    private static ComboBox Layout(PrintDialogForm dialog) =>
        Assert.IsType<ComboBox>(typeof(PrintDialogForm).GetField("_layout",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(dialog));

    private static void WithDialog(string json, Action<PrintDialogForm> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            InputDataService input = InputDataService.Instance;
            string original = JsonSerializer.Serialize(input.GetSaveJson());
            int dimension = input.dimension;
            try
            {
                using (var document = JsonDocument.Parse(json)) input.JsonDataOpen(document.RootElement);
                using var dialog = new PrintDialogForm(_ => []);
                action(dialog);
            }
            catch (Exception error) { failure = error; }
            finally
            {
                using (var document = JsonDocument.Parse(original)) input.JsonDataOpen(document.RootElement);
                input.dimension = dimension;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA test did not finish.");
        if (failure is not null) throw failure;
    }
}

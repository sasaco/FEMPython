using System.Runtime.ExceptionServices;
using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.components.menu;
using FrameWebforCS.providers;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class BridgeLoadUiTests
{
    [Fact]
    public void EditorExposesThreeSheetsStableSelectionAndSignedIntensityUnits()
    {
        RunSta(() =>
        {
            try
            {
                BridgeLoadInputTests.Open(BridgeLoadInputTests.Document);
                using var editor = new InputBridgeLoadComponent();
                var spread = editor.Controls.OfType<FpSpread>().Single();
                Assert.Equal(new[] { "荷重伝達面", "載荷形状", "橋面荷重強度" }, spread.Sheets.Cast<SheetView>().Select(s => s.SheetName));
                editor.SelectEntity(new BridgeSelection("load", 11, "9"));
                Assert.Equal(2, editor.ActiveSheetIndex);
                Assert.Equal("9", InputLoadService.Instance.SelectedCaseId);
                Assert.Equal("kN/m", InputBridgeLoadService.Instance.Loads[0].Units);
                Assert.Equal("kN/m²", InputBridgeLoadService.Instance.Loads[1].Units);
                editor.SelectEntity(new BridgeSelection("path", 2, PointIndex: 1));
                Assert.Equal(1, editor.ActiveSheetIndex);
                Assert.Equal(3, spread.ActiveSheet.ActiveRowIndex);
            }
            finally { BridgeLoadInputTests.Open("{}"); }
        });
    }

    [Fact]
    public void SidebarAndCachedEditorAreOnlyAvailableInThreeDimensions()
    {
        RunSta(() =>
        {
            try
            {
                BridgeLoadInputTests.Open(BridgeLoadInputTests.Document);
                using var sidebar = new SidebarComponent();
                using var editor = new InputBridgeLoadComponent();
                var tree = sidebar.Controls.OfType<TreeView>().Single();
                Assert.Single(tree.Nodes.Find("bridge_load", true));
                Assert.True(editor.Enabled);
                InputDataService.Instance.SetDimension(2);
                Assert.Empty(tree.Nodes.Find("bridge_load", true)); Assert.False(editor.Enabled);
                Assert.True(InputBridgeLoadService.Instance.HasData);
                InputDataService.Instance.SetDimension(3);
                Assert.Single(tree.Nodes.Find("bridge_load", true)); Assert.True(editor.Enabled);
            }
            finally { BridgeLoadInputTests.Open("{}"); }
        });
    }

    private static void RunSta(System.Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception caught) { error = caught; } });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}

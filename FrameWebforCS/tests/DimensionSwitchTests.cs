using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.components.menu;
using FrameWebforCS.providers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class DimensionSwitchTests
{
    [Fact]
    public void MenuChecksAndPanelEntryFollowSuccessfulSelectionsAndFileLoads()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            Load("{}");
            using var menu = new MenuComponent();
            using var sidebar = new SidebarComponent();
            var parent = MenuItem(menu, "toolStripMenuItem1");
            var twoD = MenuItem(menu, "dToolStripMenuItem2");
            var threeD = MenuItem(menu, "dToolStripMenuItem3");
            var tree = Assert.Single(sidebar.Controls.OfType<TreeView>());

            AssertMenu(3);
            Assert.NotNull(tree.Nodes.Find("shell", true).SingleOrDefault());
            Assert.NotNull(tree.Nodes.Find("solid", true).SingleOrDefault());

            twoD.PerformClick();
            AssertMenu(2);
            Assert.Empty(tree.Nodes.Find("shell", true));
            Assert.Empty(tree.Nodes.Find("solid", true));
            twoD.PerformClick();
            AssertMenu(2);

            Load("""{"dimension":3}""");
            AssertMenu(3);
            Assert.NotNull(tree.Nodes.Find("shell", true).SingleOrDefault());
            Assert.NotNull(tree.Nodes.Find("solid", true).SingleOrDefault());
            Load("""{"dimension":2}""");
            AssertMenu(2);
            using (var invalid = JsonDocument.Parse("""{"dimension":4}"""))
                Assert.Throws<JsonException>(() => input.JsonDataOpen(invalid.RootElement));
            AssertMenu(2);

            threeD.PerformClick();
            AssertMenu(3);

            void AssertMenu(int dimension)
            {
                Assert.Equal(dimension, input.dimension);
                Assert.Equal(dimension == 3 ? "3D" : "2D", parent.Text);
                Assert.Equal(dimension == 2, twoD.Checked);
                Assert.Equal(dimension == 3, threeD.Checked);
                Assert.False(parent.Checked);
            }
        });
    }

    [Fact]
    public void CachedInputGridsSwitchColumnsWithoutLosingHiddenCoordinates()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            var routing = AppRoutingModule.Instance;
            Load("""{"dimension":3,"node":{"1":{"x":1,"y":2,"z":7}}}""");
            UserControl[] controls =
            [
                new InputNodesComponent(), new InputMembersComponent(),
                new InputElementsComponent(), new InputFixNodeComponent(),
                new InputFixMemberComponent(), new InputJointComponent(),
                new InputLoadComponent()
            ];
            routing.myComponents.AddRange(controls);
            using var host = new Form();
            host.Controls.Add(controls[0]);
            try
            {
                AssertColumns([3, 6, 8, 7, 5, 7, 16]);
                Assert.Equal(nameof(clsMember.Cg), Spread(controls[1]).Sheets[0].Columns[4].DataField);
                Assert.Equal(nameof(clsElement.Torsion), Spread(controls[2]).Sheets[0].Columns[4].DataField);
                Assert.Equal("Tz", Spread(controls[3]).Sheets[0].Columns[3].DataField);
                Assert.Equal("Tz", Spread(controls[4]).Sheets[0].Columns[3].DataField);
                Assert.Equal("Zi", Spread(controls[5]).Sheets[0].Columns[3].DataField);
                Assert.Equal("tz", Spread(controls[6]).Sheets[1].Columns[12].DataField);
                Spread(controls[0]).Sheets[0].SetActiveCell(0, 2);
                Spread(controls[1]).Sheets[0].SetActiveCell(0, 4);
                Spread(controls[6]).Sheets[1].SetActiveCell(0, 12);
                input.SetDimension(2);
                AssertColumns([2, 5, 5, 4, 3, 3, 13]);
                Assert.Equal(nameof(clsElement.InertiaZ), Spread(controls[2]).Sheets[0].Columns[3].DataField);
                Assert.Equal("Rz", Spread(controls[3]).Sheets[0].Columns[3].DataField);
                Assert.Equal("Ty", Spread(controls[4]).Sheets[0].Columns[2].DataField);
                Assert.Equal("Zi", Spread(controls[5]).Sheets[0].Columns[1].DataField);
                Assert.Equal("rz", Spread(controls[6]).Sheets[1].Columns[12].DataField);
                Assert.Equal(7, InputNodesService.Instance.Nodes[0].Z);
                host.Controls.Clear();
                host.Controls.Add(controls[1]);
                input.SetDimension(3);
                AssertColumns([3, 6, 8, 7, 5, 7, 16]);
                Assert.Equal("Z", Spread(controls[0]).Sheets[0].Columns[2].DataField);
                Assert.Equal(7, InputNodesService.Instance.Nodes[0].Z);
                host.Controls.Clear();
                host.Controls.Add(controls[6]);
                input.SetDimension(2);
                AssertColumns([2, 5, 5, 4, 3, 3, 13]);
                Assert.Equal("rz", Spread(controls[6]).Sheets[1].Columns[12].DataField);
                using var saved = JsonDocument.Parse(JsonSerializer.Serialize(input.GetSaveJson()));
                Assert.Equal(2, saved.RootElement.GetProperty("dimension").GetInt32());
                Assert.Equal(7, saved.RootElement.GetProperty("node").GetProperty("1")
                    .GetProperty("z").GetSingle());
                input.JsonDataOpen(saved.RootElement);
                Assert.Equal(2, input.dimension);
                AssertColumns([2, 5, 5, 4, 3, 3, 13]);
                input.SetDimension(3);
                Assert.Equal(7, InputNodesService.Instance.Nodes[0].Z);
            }
            finally
            {
                foreach (var control in controls)
                {
                    routing.myComponents.Remove(control);
                    control.Dispose();
                }
            }

            void AssertColumns(int[] counts)
            {
                for (int i = 0; i < controls.Length; i++)
                {
                    var spread = Spread(controls[i]);
                    Assert.Equal(counts[i], spread.Sheets[i == 6 ? 1 : 0].ColumnCount);
                }
            }
        });
    }

    private static ToolStripMenuItem MenuItem(MenuComponent menu, string name) =>
        (ToolStripMenuItem)typeof(MenuComponent).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(menu)!;

    private static FpSpread Spread(UserControl component) =>
        Assert.Single(component.Controls.OfType<FpSpread>());

    private static void Load(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }

    private static void RunSta(System.Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { error = ex; }
            finally
            {
                try { Load("{}"); }
                catch (Exception ex) { error ??= ex; }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45)), "Dimension UI STA test timed out.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}

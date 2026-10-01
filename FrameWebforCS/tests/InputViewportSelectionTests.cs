using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.three;
using SingleFormsDemo;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Reflection;
using System.Text.Json;
using THREE;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputViewportSelectionTests
{
    [Fact]
    public void ColdElementRouteOpensMemberDetailWithoutConstructingMemberGrid()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            var routing = AppRoutingModule.Instance;
            var priorMembers = routing.myComponents.OfType<InputMembersComponent>().ToArray();
            foreach (var member in priorMembers) routing.myComponents.Remove(member);
            using var elements = new InputElementsComponent();
            routing.myComponents.Add(elements);
            try
            {
                routing.NotifyInputMode("element");
                using var viewport = new ThreeService(new SceneService());
                using (var document = JsonDocument.Parse("""
                    {"node":{"1":{"x":0},"2":{"x":10}},
                     "member":{"7":{"ni":"1","nj":"2","e":"4"}}}
                    """)) input.JsonDataOpen(document.RootElement);
                viewport.FlushPending();
                viewport.ShowMemberSelectionDetail("element", 7);

                Assert.True(elements.DetailVisible);
                Assert.Equal(7, elements.DetailMemberId);
                Assert.Equal("element", routing.ActiveModeKey);
                Assert.Empty(routing.myComponents.OfType<InputMembersComponent>());
                var detail = Assert.Single(elements.Controls.OfType<MemberDetailPanel>());
                Assert.Contains("長さ 10", string.Join(" ", detail.Controls
                    .OfType<TableLayoutPanel>().SelectMany(layout => layout.Controls.OfType<Label>())
                    .Select(label => label.Text)));
            }
            finally
            {
                routing.myComponents.Remove(elements);
                routing.myComponents.AddRange(priorMembers);
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
                routing.NotifyInputMode("node");
            }
        });
    }

    [Fact]
    public void ElementGridSelectionHighlightsEveryMemberWithThatElementAndShowsLocalAxes()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            var routing = AppRoutingModule.Instance;
            using var elements = new InputElementsComponent();
            var spread = elements.Controls.OfType<FpSpread>().Single();
            var scene = new SceneService();
            routing.myComponents.Add(elements);
            try
            {
                routing.NotifyInputMode("element");
                using var viewport = new ThreeService(scene);
                using (var document = JsonDocument.Parse("""
                    {"node":{"1":{"x":0},"2":{"x":10},"3":{"y":10},"4":{"x":10,"y":10}},
                     "member":{"11":{"ni":"1","nj":"2","e":"5"},
                               "42":{"ni":"3","nj":"4","e":"5"},
                               "90":{"ni":"1","nj":"3","e":"9"}}}
                    """)) input.JsonDataOpen(document.RootElement);
                viewport.FlushPending();
                Assert.Equal(3, viewport.MemberCount);

                var memberRoot = scene.scene.Children.Single(child => child.Name == "members");
                RaiseEnterCell(spread, 4, 1);
                viewport.FlushPending();

                Assert.Equal("element", viewport.SelectedKind);
                Assert.Equal(["member11", "member42"], memberRoot.Children.OfType<Mesh>()
                    .Where(mesh => mesh.Material.Color!.Value.GetHex() == 0xFF0000)
                    .Select(mesh => mesh.Name).Order().ToArray());
                Assert.Equal(["member11axis", "member42axis"], memberRoot.Children.OfType<Group>()
                    .Select(group => group.Name).Order().ToArray());
                Assert.Equal(0x000000, memberRoot.Children.OfType<Mesh>()
                    .Single(mesh => mesh.Name == "member90").Material.Color!.Value.GetHex());

                spread.ActiveSheetIndex = 1;
                RaiseEnterCell(spread, 8, 1);
                viewport.FlushPending();
                Assert.Equal(["member90"], memberRoot.Children.OfType<Mesh>()
                    .Where(mesh => mesh.Material.Color!.Value.GetHex() == 0xFF0000)
                    .Select(mesh => mesh.Name).ToArray());
                Assert.Equal(["member90axis"], memberRoot.Children.OfType<Group>()
                    .Select(group => group.Name).ToArray());

                routing.NotifyInputMode("node");
                viewport.FlushPending();
                Assert.Null(viewport.SelectedKind);
                Assert.Empty(memberRoot.Children.OfType<Group>());
            }
            finally
            {
                routing.myComponents.Remove(elements);
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
                routing.NotifyInputMode("node");
            }
        });
    }

    [Fact]
    public void MemberAndRigidGridSelectionsKeepStableRowsAndDoNotEchoViewportSelection()
    {
        RunSta(() =>
        {
            using var component = new InputMembersComponent();
            var spread = Assert.IsAssignableFrom<FpSpread>(component.Controls
                .Cast<System.Windows.Forms.Control>().Single(c => c is FpSpread));
            component.CreateControl();
            var selected = new List<(string Kind, int Row, string Axis)>();
            component.GridSelectionChanged += (kind, row, axis) => selected.Add((kind, row, axis));

            Assert.True(component.SelectGridRow("members", 7));
            Assert.Equal(6, spread.ActiveSheet.ActiveRowIndex);
            Assert.Empty(selected);
            RaiseEnterCell(spread, 8, 1);
            Assert.Contains(("members", 9, ""), selected);
            Assert.Single(selected);

            Assert.True(component.SelectGridRow("rigid_zone", 11, "j"));
            Assert.Equal(1, spread.ActiveSheetIndex);
            Assert.Equal(10, spread.ActiveSheet.ActiveRowIndex);
            Assert.Equal(4, spread.ActiveSheet.ActiveColumnIndex);
            Assert.DoesNotContain(selected, item => item.Kind == "rigid_zone");
            RaiseEnterCell(spread, 12, 3);
            Assert.Contains(("rigid_zone", 13, "i"), selected);
            Assert.Equal(2, selected.Count);
        });
    }

    [Fact]
    public void ViewportMemberDetailShowsTypedDataAndAppliesValidatedEdits()
    {
        RunSta(() =>
        {
            var input = InputDataService.Instance;
            using var component = new InputMembersComponent();
            try
            {
                using (var document = JsonDocument.Parse("""
                    {"node":{"1":{"x":0},"2":{"x":10},"3":{"x":20}},
                     "member":{"7":{"ni":"1","nj":"2","e":"4","cg":15},
                               "8":{"ni":"2","nj":"3","e":"5"}}}
                    """)) input.JsonDataOpen(document.RootElement);
                var material = InputElementsService.Instance.GetRows(1)[3];
                material.ElasticModulus = 210;
                material.ShearModulus = 81;
                material.Expansion = 0.000012f;
                material.Area = 0.5f;
                material.Torsion = 0.02f;
                material.InertiaY = 0.03f;
                material.InertiaZ = 0.04f;
                material.Name = "Steel";
                component.ShowMemberDetail(7);
                Assert.True(component.DetailVisible);
                Assert.Equal(7, component.DetailMemberId);
                var panel = Assert.Single(component.Controls.OfType<Panel>());
                Assert.Contains("長さ 10", string.Join(" ", panel.Controls
                    .OfType<TableLayoutPanel>().SelectMany(layout => layout.Controls.OfType<Label>())
                    .Select(label => label.Text)));
                var fields = panel.Controls.OfType<TableLayoutPanel>()
                    .SelectMany(layout => layout.Controls.OfType<TextBox>())
                    .ToDictionary(field => field.Name);
                Assert.Equal("210", fields["memberDetail_E"].Text);
                Assert.Equal("81", fields["memberDetail_G"].Text);
                Assert.Equal(0.000012f, float.Parse(fields["memberDetail_Xp"].Text,
                    CultureInfo.InvariantCulture));
                Assert.Equal("0.5", fields["memberDetail_A"].Text);
                Assert.Equal("0.02", fields["memberDetail_J"].Text);
                Assert.Equal("0.03", fields["memberDetail_Iy"].Text);
                Assert.Equal("0.04", fields["memberDetail_Iz"].Text);
                Assert.Equal("Steel", fields["memberDetail_n"].Text);
                Assert.All(new[] { "E", "G", "Xp", "A", "J", "Iy", "Iz", "n" },
                    key => Assert.True(fields["memberDetail_" + key].ReadOnly));

                component.ShowMemberDetail(8);
                Assert.Equal(8, component.DetailMemberId);
                Assert.All(new[] { "E", "G", "Xp", "A", "J", "Iy", "Iz", "n" },
                    key => Assert.Equal("", fields["memberDetail_" + key].Text));
                component.ShowMemberDetail(7);

                Assert.False(component.ApplyMemberDetail(0, 3, 4, 15));
                Assert.Equal(1, InputMembersService.Instance.GetDisplayMember(7)!.Value.Ni);
                Assert.True(component.ApplyMemberDetail(2, 3, 5, 30));
                Assert.Equal(new DisplayMember(2, 3, 5, 30),
                    InputMembersService.Instance.GetDisplayMember(7));
                Assert.Equal("", fields["memberDetail_E"].Text);
            }
            finally
            {
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
                Assert.False(component.DetailVisible);
                Assert.Null(component.DetailMemberId);
            }
        });
    }

    [Fact]
    public void MemberSheetPublishesLegacyDisplayModeOnActivationAndSwitch()
    {
        RunSta(() =>
        {
            using var members = new InputMembersComponent();
            var spread = members.Controls.OfType<FpSpread>().Single();
            var modes = new List<string>();
            members.ActiveMemberDisplayModeChanged += modes.Add;
            Assert.Equal("member", members.ActiveMemberDisplayMode);

            members.CreateControl();
            Assert.Contains("member", modes);
            modes.Clear();
            spread.ActiveSheetIndex = 1;
            Assert.Equal("rigid", members.ActiveMemberDisplayMode);
            Assert.Equal(["rigid"], modes);
            spread.ActiveSheetIndex = 0;
            Assert.Equal("member", members.ActiveMemberDisplayMode);
            Assert.Equal(["rigid", "member"], modes);
        });
    }

    [Fact]
    public void ConstraintSheetSwitchChangesServiceCaseAndSelectionAxis()
    {
        RunSta(() =>
        {
            using var fixNode = new InputFixNodeComponent();
            var spread = fixNode.Controls.OfType<FpSpread>().Single();
            fixNode.CreateControl();
            var selected = new List<(int Row, string Axis)>();
            fixNode.GridSelectionChanged += (row, axis) => selected.Add((row, axis));
            Assert.True(fixNode.SelectGridRow(5, "ty", "2"));
            Assert.Equal("2", InputFixNodeService.Instance.SelectedCaseId);
            Assert.Equal(1, spread.ActiveSheetIndex);
            Assert.Equal(4, spread.ActiveSheet.ActiveRowIndex);
            Assert.Equal(2, spread.ActiveSheet.ActiveColumnIndex);
            Assert.Empty(selected);
            RaiseEnterCell(spread, 6, 4);
            Assert.Contains((7, "rx"), selected);
        });
    }

    [Fact]
    public void LoadCaseAndGridColumnFollowSelectionWithoutFeedback()
    {
        RunSta(() =>
        {
            var service = InputLoadService.Instance;
            try
            {
                using (var document = JsonDocument.Parse("""
                    {"load":{"1":{"input_rows":[1,2,3,4,5]}}}
                    """)) service.setLoadJson(document.RootElement);
                using var load = new InputLoadComponent();
                var spread = load.Controls.OfType<FpSpread>().Single();
                load.CreateControl();
                var selected = new List<(int Row, string Column)>();
                load.GridSelectionChanged += (row, column) => selected.Add((row, column));
                Assert.True(load.SelectGridRow(3, "ty"));
                Assert.Equal(1, spread.ActiveSheetIndex);
                Assert.Equal(2, spread.ActiveSheet.ActiveRowIndex);
                Assert.Equal(11, spread.ActiveSheet.ActiveColumnIndex);
                Assert.Empty(selected);
                RaiseEnterCell(spread, 4, 7);
                Assert.Contains((5, "p1"), selected);
            }
            finally
            {
                service.clear();
                service.SelectCase("1");
            }
        });
    }

    [Fact]
    public void LoadSheetPublishesLegacyDisplayModeOnActivationAndSwitch()
    {
        RunSta(() =>
        {
            using var load = new InputLoadComponent();
            var spread = load.Controls.OfType<FpSpread>().Single();
            var modes = new List<string>();
            load.ActiveLoadDisplayModeChanged += modes.Add;
            Assert.Equal("load_names", load.ActiveLoadDisplayMode);

            load.CreateControl();
            Assert.Contains("load_names", modes);
            modes.Clear();
            spread.ActiveSheetIndex = 1;
            Assert.Equal("load_values", load.ActiveLoadDisplayMode);
            Assert.Equal(["load_values"], modes);
            spread.ActiveSheetIndex = 0;
            Assert.Equal("load_names", load.ActiveLoadDisplayMode);
            Assert.Equal(["load_values", "load_names"], modes);
        });
    }

    [Fact]
    public void FixMemberAndJointSelectionsUseCaseAndFieldKey()
    {
        RunSta(() =>
        {
            var springService = InputFixMemberService.Instance;
            try
            {
                using (var document = JsonDocument.Parse("""
                    {"fix_member":{"3":[{"row":4,"m":"4","tx":10},
                                         {"row":7,"m":"7","ty":20}]}}
                    """)) springService.setFixMemberJson(document.RootElement);
                using var fixMember = new InputFixMemberComponent();
                var memberSpread = fixMember.Controls.OfType<FpSpread>().Single();
                var memberSelections = new List<(int Row, string Axis)>();
                fixMember.GridSelectionChanged += (row, axis) => memberSelections.Add((row, axis));
                Assert.True(fixMember.SelectGridRow(4, "tz", "3"));
                Assert.Equal("3", springService.SelectedCaseId);
                Assert.NotNull(memberSpread.ActiveSheet.DataSource);
                Assert.Equal(0, memberSpread.ActiveSheet.ActiveRowIndex);
                Assert.Equal(4, memberSpread.ActiveSheet.ActiveColumnIndex);
                Assert.Empty(memberSelections);
                RaiseEnterCell(memberSpread, 1, 3);
                Assert.Equal([(7, "ty")], memberSelections);
            }
            finally { springService.clear(); }

            using var joint = new InputJointComponent();
            var jointSpread = joint.Controls.OfType<FpSpread>().Single();
            var jointSelections = new List<(int Row, string Axis)>();
            joint.GridSelectionChanged += (row, axis) => jointSelections.Add((row, axis));
            Assert.True(joint.SelectGridRow(9, "yj", "4"));
            Assert.Equal("4", InputJointService.Instance.SelectedCaseId);
            Assert.Equal(8, jointSpread.ActiveSheet.ActiveRowIndex);
            Assert.Equal(5, jointSpread.ActiveSheet.ActiveColumnIndex);
            Assert.Empty(jointSelections);
            RaiseEnterCell(jointSpread, 10, 4);
            Assert.Equal([(11, "xj")], jointSelections);
        });
    }

    [Fact]
    public void NoticePointSelectionUsesDisplayedPointAndStopsAfterDisposal()
    {
        RunSta(() =>
        {
            var notice = new InputNoticePointsComponent();
            var spread = notice.Controls.OfType<FpSpread>().Single();
            var selected = new List<(int Row, string Axis)>();
            notice.GridSelectionChanged += (row, axis) => selected.Add((row, axis));
            Assert.True(notice.SelectGridRow(2, "L3"));
            Assert.Equal(1, spread.ActiveSheet.ActiveRowIndex);
            Assert.Equal(4, spread.ActiveSheet.ActiveColumnIndex);
            Assert.Empty(selected);
            RaiseEnterCell(spread, 5, 6);
            Assert.Equal([(6, "L5")], selected);
            notice.Dispose();
            RaiseEnterCell(spread, 6, 7);
            Assert.Single(selected);
        });
    }

    private static void RunSta(System.Action body)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            int previousDimension = InputDataService.Instance.dimension;
            try
            {
                // These cases assert the three-dimensional column layout.
                InputDataService.Instance.dimension = 3;
                body();
            }
            catch (Exception ex) { error = ex; }
            finally { InputDataService.Instance.dimension = previousDimension; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45)), "Input selection STA test timed out.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static void RaiseEnterCell(FpSpread spread, int row, int column)
    {
        // SetActiveCell changes the model but does not raise the focus event in a
        // headless test. Raise the control event to exercise the real subscription.
        var view = new SpreadView(spread);
        var callback = typeof(FpSpread).GetMethod("OnEnterCell",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        callback.Invoke(spread, [new EnterCellEventArgs(view, row, column)]);
    }
}

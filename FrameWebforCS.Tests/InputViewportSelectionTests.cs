using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Runtime.ExceptionServices;
using System.Reflection;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputViewportSelectionTests
{
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
            using var fixMember = new InputFixMemberComponent();
            var memberSpread = fixMember.Controls.OfType<FpSpread>().Single();
            var memberSelections = new List<(int Row, string Axis)>();
            fixMember.GridSelectionChanged += (row, axis) => memberSelections.Add((row, axis));
            Assert.True(fixMember.SelectGridRow(4, "tz", "3"));
            Assert.Equal("3", InputFixMemberService.Instance.SelectedCaseId);
            Assert.Equal(3, memberSpread.ActiveSheet.ActiveRowIndex);
            Assert.Equal(3, memberSpread.ActiveSheet.ActiveColumnIndex);
            Assert.Empty(memberSelections);
            RaiseEnterCell(memberSpread, 6, 2);
            Assert.Equal([(7, "ty")], memberSelections);

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

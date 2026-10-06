using FrameWebforCS.components.printing;
using FrameWebforCS.providers.printing;
using PDF_Manager;
using System.Runtime.InteropServices;
using Xunit;

namespace PrintUi.Tests;

public sealed class PrintDialogFormTests
{
    [Fact]
    public void BridgeChoicesAndInputCaseControlsAreHiddenInTwoDimensions()
    {
        RunSta(() =>
        {
            var input = FrameWebforCS.providers.InputDataService.Instance;
            int original = input.dimension;
            try
            {
                input.dimension = 2;
                using var dialog = new PrintDialogForm(_ => []);
                var options = (CheckedListBox)Assert.Single(dialog.Controls.Find("printOptions", true));
                Assert.DoesNotContain(options.Items.Cast<object>(), item => item.ToString()!.Contains("橋面"));
                Assert.Empty(dialog.Controls.Find("bridgeInputCases", true));
                Assert.Empty(dialog.Controls.Find("bridgeDiagramViews", true));
            }
            finally { input.dimension = original; }
        });
    }

    [Fact]
    public void DefaultSelectionUsesLegacyInputOptionAndPageSettings()
    {
        RunSta(() =>
        {
            using var dialog = new PrintDialogForm(_ => []);
            PrintSelection selection = dialog.ReadSelection();
            Assert.Equal([PrintOption.Input], selection.Options);
            Assert.Equal(PrintPaper.A4, selection.Paper);
            Assert.Equal(PrintOrientation.Vertical, selection.Orientation);
            Assert.Equal(PrintLayout.Single, selection.Layout);
            Assert.Equal("ja", selection.Language);
            Assert.Equal(1, selection.ScaleX);
            Assert.Equal(1, selection.ScaleY);
        });
    }

    [Fact]
    public void AllReachableOptionsAreListedAndComponentFiltersStayIndependent()
    {
        RunSta(() =>
        {
            using var dialog = new PrintDialogForm(_ => []);
            var options = (CheckedListBox)Assert.Single(dialog.Controls.Find("printOptions", true));
            Assert.Equal(FrameWebforCS.providers.InputDataService.Instance.dimension == 3 ? 19 : 15,
                options.Items.Count);
            Assert.DoesNotContain(options.Items.Cast<object>(), item =>
                item.ToString()!.Contains("画面印刷") || item.ToString()!.Contains("反力図"));
            var displacement = (CheckedListBox)Assert.Single(
                dialog.Controls.Find("displacementComponents", true));
            var reaction = (CheckedListBox)Assert.Single(
                dialog.Controls.Find("reactionComponents", true));
            var section = (CheckedListBox)Assert.Single(
                dialog.Controls.Find("sectionForceComponents", true));
            var diagrams = (CheckedListBox)Assert.Single(
                dialog.Controls.Find("resultDiagramComponents", true));
            var loads = (CheckedListBox)Assert.Single(
                dialog.Controls.Find("loadDiagramComponents", true));
            displacement.SetItemChecked(displacement.Items.IndexOf("dx_max"), true);
            reaction.SetItemChecked(reaction.Items.IndexOf("ty_min"), true);
            section.SetItemChecked(section.Items.IndexOf("mz_max"), true);
            diagrams.SetItemChecked(diagrams.Items.IndexOf("fx"), true);
            loads.SetItemChecked(loads.Items.IndexOf("axis"), true);
            var selection = dialog.ReadSelection();
            Assert.Equal(["dx_max"], selection.DisplacementComponents);
            Assert.Equal(["ty_min"], selection.ReactionComponents);
            Assert.Equal(["mz_max"], selection.SectionForceComponents);
            Assert.Equal(["fx"], selection.DiagramComponents);
            Assert.Equal(["axis"], selection.LoadDiagramComponents);
        });
    }

    [Fact]
    public void SavingDisplayedBytesReplacesDestinationWithoutTemporaryFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "FrameWebPrintSaveTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var bytes = DirectPdfGenerator.Generate("""
                {"dimension":3,"title":"Save identity","hasPrintInputData":false,"hasPrintCalculation":false}
                """);
            string destination = Path.Combine(folder, "selected.pdf");
            File.WriteAllText(destination, "old");
            PrintDialogForm.SaveAtomically(destination, bytes);
            Assert.Equal(bytes, File.ReadAllBytes(destination));
            Assert.Single(Directory.GetFiles(folder));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void PreviewCapacityRetainsActivePdfAndRejectsLockedRetiredFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "FrameWebPrintCapacityTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string active = Path.Combine(folder, "active.pdf");
            string retired = Path.Combine(folder, "retired.pdf");
            File.WriteAllBytes(active, [1, 2, 3]);
            File.WriteAllBytes(retired, [4, 5, 6]);
            using (var locked = new FileStream(retired, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Throws<IOException>(() => PrintDialogForm.EnsurePreviewCapacity(folder, active, 3));
                Assert.True(File.Exists(active));
                Assert.True(File.Exists(retired));
            }
            PrintDialogForm.EnsurePreviewCapacity(folder, active, 3);
            Assert.True(File.Exists(active));
            Assert.False(File.Exists(retired));
            // A failed navigation candidate is inactive and is removed on the next attempt.
            File.WriteAllBytes(retired, [7, 8, 9]);
            PrintDialogForm.EnsurePreviewCapacity(folder, active, 3);
            Assert.Single(Directory.GetFiles(folder, "*.pdf"));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void OrphanCleanupRequiresOldAppDirectoryAndReleasedLease()
    {
        string folder = Path.Combine(Path.GetTempPath(), "FrameWebPrint-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string leasePath = Path.Combine(folder, "session.lock");
        File.WriteAllText(leasePath, "");
        File.WriteAllBytes(Path.Combine(folder, "old.pdf"), [1, 2, 3]);
        Directory.SetLastWriteTimeUtc(folder, DateTime.UtcNow.AddDays(-2));
        try
        {
            using (var lease = new FileStream(leasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert.False(PrintDialogForm.TryCleanupOrphanDirectory(folder, DateTime.UtcNow.AddDays(-1)));
            Assert.True(PrintDialogForm.TryCleanupOrphanDirectory(folder, DateTime.UtcNow.AddDays(-1)));
            Assert.False(Directory.Exists(folder));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void PopupHasOwnerAndBlocksOwnerWhileOpen()
    {
        RunSta(() =>
        {
            using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000) };
            using var dialog = new PrintDialogForm(_ => []) { StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000) };
            bool? ownerBlocked = null;
            Form? actualOwner = null;
            using var timer = new System.Windows.Forms.Timer { Interval = 150 };
            timer.Tick += (_, _) =>
            {
                actualOwner = dialog.Owner;
                ownerBlocked = !IsWindowEnabled(owner.Handle);
                timer.Stop();
                dialog.Close();
            };
            owner.Show();
            timer.Start();
            dialog.ShowDialog(owner);
            Assert.Same(owner, actualOwner);
            Assert.True(ownerBlocked);
            Assert.True(owner.Enabled);
        });
    }

    [Fact]
    public void InputOnlyPreviewDisplaysGeneratedPdfAndSavesSameBytes()
    {
        RunSta(() =>
        {
            using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000) };
            using var dialog = new PrintDialogForm(_ => throw new Xunit.Sdk.XunitException(
                "Input-only preview must not request GL capture."))
            {
                StartPosition = FormStartPosition.Manual, Location = new Point(-3000, -3000)
            };
            Exception? failure = null;
            byte[]? displayed = null;
            string? status = null;
            int maximumRetainedFiles = 0;
            int successfulPreviews = 0;
            using var timeout = new System.Windows.Forms.Timer { Interval = 20000 };
            timeout.Tick += (_, _) =>
            {
                timeout.Stop();
                failure = new TimeoutException("Print preview did not complete in 20 seconds.");
                dialog.Close();
            };
            dialog.Shown += async (_, _) =>
            {
                try
                {
                    await dialog.InitializePreviewAsync();
                    for (int generation = 0; generation < 3; generation++)
                    {
                        await dialog.GenerateAsync(showErrorDialog: false);
                        if (dialog.StatusText.StartsWith("PDF を表示中", StringComparison.Ordinal))
                            successfulPreviews++;
                        if (dialog.PreviewDirectory is { } folder)
                            maximumRetainedFiles = Math.Max(maximumRetainedFiles,
                                Directory.GetFiles(folder, "*.pdf").Length);
                    }
                    displayed = dialog.DisplayedPdf;
                    status = dialog.StatusText;
                }
                catch (Exception error) { failure = error; }
                finally { timeout.Stop(); dialog.Close(); }
            };
            owner.Show();
            timeout.Start();
            dialog.ShowDialog(owner);
            if (failure is not null) throw failure;
            Assert.NotNull(displayed);
            Assert.True(displayed.Length > 100, status);
            Assert.InRange(maximumRetainedFiles, 1, 2);
            Assert.Equal(3, successfulPreviews);
            string target = Path.Combine(Path.GetTempPath(), "FrameWebPrintUi-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                PrintDialogForm.SaveAtomically(target, displayed);
                Assert.Equal(displayed, File.ReadAllBytes(target));
            }
            finally { if (File.Exists(target)) File.Delete(target); }
        });
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr window);

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

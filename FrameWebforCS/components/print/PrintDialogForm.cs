using FrameWebforCS.calculation;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using FrameWebforCS.three;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PdfSharpCore.Pdf.IO;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrameWebforCS.components.printing;

/// <summary>The desktop's single owner-bound print workflow. All document and GL reads stay on the UI thread.</summary>
internal sealed class PrintDialogForm : Form
{
    private const int MaximumRetainedPreviewFiles = 2;
    private const long MaximumRetainedPreviewBytes = 256L * 1024 * 1024;
    private static readonly (PrintOption Option, string Label)[] Choices =
    [
        (PrintOption.Input, "入力データ"),
        (PrintOption.Displacement, "変位"),
        (PrintOption.CombinedDisplacement, "COMBINE 変位"),
        (PrintOption.PickupDisplacement, "PICKUP 変位"),
        (PrintOption.Reaction, "反力"),
        (PrintOption.CombinedReaction, "COMBINE 反力"),
        (PrintOption.PickupReaction, "PICKUP 反力"),
        (PrintOption.SectionForce, "断面力"),
        (PrintOption.CombinedSectionForce, "COMBINE 断面力"),
        (PrintOption.PickupSectionForce, "PICKUP 断面力"),
        (PrintOption.SectionDiagram, "断面力図"),
        (PrintOption.CombinedSectionDiagram, "COMBINE 断面力図"),
        (PrintOption.PickupSectionDiagram, "PICKUP 断面力図"),
        (PrintOption.DisplacementDiagram, "3D 変位図"),
        (PrintOption.LoadDiagram, "荷重図"),
    ];

    private static readonly string[] DisplacementChoices = ExpandModes(
        ["dx", "dy", "dz", "rx", "ry", "rz"]);
    private static readonly string[] ReactionChoices = ExpandModes(
        ["tx", "ty", "tz", "mx", "my", "mz"]);
    private static readonly string[] SectionForceChoices = ExpandModes(
        ["fx", "fy", "fz", "mx", "my", "mz"]);

    private readonly Func<IReadOnlyList<PrintDiagramRequest>, IReadOnlyList<PrintDiagramImage>> _capture;
    private readonly CheckedListBox _options = new() { Name = "printOptions", Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly HashSet<int> _unavailableOptions = [];
    private readonly CheckedListBox _cases = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly CheckedListBox _derived = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly CheckedListBox _displacementComponents = new() { Name = "displacementComponents", Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly CheckedListBox _reactionComponents = new() { Name = "reactionComponents", Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly CheckedListBox _sectionComponents = new() { Name = "sectionForceComponents", Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly CheckedListBox _diagramComponents = new() { Name = "resultDiagramComponents", Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly CheckedListBox _loadDiagramComponents = new() { Name = "loadDiagramComponents", Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly TextBox _nodes = new() { Dock = DockStyle.Fill, PlaceholderText = "空欄: 全ノード。例: 1, 2" };
    private readonly TextBox _members = new() { Dock = DockStyle.Fill, PlaceholderText = "空欄: 全部材。例: 1, 2" };
    private readonly TextBox _title = new() { Dock = DockStyle.Fill };
    private readonly TextBox _scaleX = new() { Dock = DockStyle.Fill, Text = "1" };
    private readonly TextBox _scaleY = new() { Dock = DockStyle.Fill, Text = "1" };
    private readonly ComboBox _paper = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _orientation = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _layout = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _language = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly WebView2 _preview = new() { Dock = DockStyle.Fill };
    private readonly Button _generate = new() { Text = "プレビュー更新", AutoSize = true };
    private readonly Button _cancel = new() { Text = "生成中止", AutoSize = true, Enabled = false };
    private readonly Button _save = new() { Text = "PDF 保存", AutoSize = true, Enabled = false };
    private readonly Button _print = new() { Text = "印刷", AutoSize = true, Enabled = false };
    private readonly Button _previous = new() { Text = "前ページ", AutoSize = true, Enabled = false };
    private readonly Button _next = new() { Text = "次ページ", AutoSize = true, Enabled = false };
    private readonly Label _status = new() { AutoSize = true, Text = "項目を選択してプレビューを更新してください。" };
    private readonly Label _pageLabel = new() { AutoSize = true, Text = "0 / 0", TextAlign = ContentAlignment.MiddleCenter };
    private CancellationTokenSource? _generation;
    private byte[]? _displayedPdf;
    private string? _displayedPath;
    private string? _tempFolder;
    private FileStream? _tempLease;
    private int _pageCount;
    private int _pageNumber;
    private bool _webReady;
    private bool _closing;

    internal PrintDialogForm(Func<IReadOnlyList<PrintDiagramRequest>, IReadOnlyList<PrintDiagramImage>> capture)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        Text = "印刷";
        Width = 1300;
        Height = 850;
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        BuildControls();
        PopulateChoices();
        _options.ItemCheck += (_, args) =>
        {
            if (_unavailableOptions.Contains(args.Index)) args.NewValue = args.CurrentValue;
        };
        Shown += async (_, _) => await InitializePreviewAsync();
        FormClosing += (_, _) => { _closing = true; _generation?.Cancel(); };
        FormClosed += (_, _) => CleanupPreviewFiles();
        _ = Task.Run(CleanupOrphanPreviewDirectories);
    }

    internal byte[]? DisplayedPdf => _displayedPdf;
    internal string StatusText => _status.Text;
    internal string? PreviewDirectory => _tempFolder;

    private void BuildControls()
    {
        var root = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
        Controls.Add(root);
        Shown += (_, _) =>
        {
            root.SplitterDistance = 345;
            root.Panel1MinSize = 300;
            root.Panel2MinSize = 480;
        };
        var settings = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
            ColumnCount = 1, Padding = new Padding(8), AutoSize = false };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Panel1.Controls.Add(settings);
        AddSection(settings, "印刷項目", _options, 290);
        var selectAll = new Button { Text = "計算結果をすべて選択", AutoSize = true };
        selectAll.Click += (_, _) =>
        {
            for (int i = 0; i < _options.Items.Count; i++)
                if (!_unavailableOptions.Contains(i)) _options.SetItemChecked(i, true);
        };
        AddRow(settings, selectAll, 32);
        AddSection(settings, "計算ケース (未選択時は全件)", _cases, 115);
        AddSection(settings, "COMBINE / PICKUP ケース (未選択時は全件)", _derived, 100);
        AddSection(settings, "変位成分・最大/最小 (未選択時は全成分)", _displacementComponents, 105);
        AddSection(settings, "反力成分・最大/最小 (未選択時は全成分)", _reactionComponents, 105);
        AddSection(settings, "断面力成分・最大/最小 (未選択時は全成分)", _sectionComponents, 105);
        AddSection(settings, "結果図の成分 (未選択時は全成分)", _diagramComponents, 90);
        AddSection(settings, "荷重図の成分 (未選択時は全成分)", _loadDiagramComponents, 55);
        AddSection(settings, "ノード ID (カンマ区切り)", _nodes, 28);
        AddSection(settings, "部材 ID (カンマ区切り)", _members, 28);
        AddSection(settings, "タイトル", _title, 28);
        AddSection(settings, "用紙", _paper, 28);
        AddSection(settings, "向き", _orientation, 28);
        AddSection(settings, "図面配置", _layout, 28);
        AddSection(settings, "X 倍率", _scaleX, 28);
        AddSection(settings, "Y 倍率", _scaleY, 28);
        AddSection(settings, "言語", _language, 28);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        root.Panel2.Controls.Add(right);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false,
            AutoScroll = true, Padding = new Padding(3) };
        toolbar.Controls.AddRange([_generate, _cancel, _previous, _pageLabel, _next, _save, _print]);
        right.Controls.Add(toolbar, 0, 0);
        right.Controls.Add(_preview, 0, 1);
        right.Controls.Add(_status, 0, 2);
        _generate.Click += async (_, _) => await GenerateAsync();
        _cancel.Click += (_, _) => _generation?.Cancel();
        _save.Click += (_, _) => SaveDisplayedPdf();
        _print.Click += (_, _) => PrintDisplayedPdf();
        _previous.Click += (_, _) => NavigatePage(_pageNumber - 1);
        _next.Click += (_, _) => NavigatePage(_pageNumber + 1);
    }

    private static void AddSection(TableLayoutPanel table, string caption, Control control, int height)
    {
        AddRow(table, new Label { Text = caption, AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 27);
        AddRow(table, control, height);
    }

    private static void AddRow(TableLayoutPanel table, Control control, int height)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        table.Controls.Add(control, 0, row);
    }

    private void PopulateChoices()
    {
        var presentation = CalculationResultStore.Instance.Current;
        var availability = PrintProjection.GetAvailability().ToDictionary(item => item.Option);
        foreach (var (option, label) in Choices)
        {
            bool unavailable = !availability[option].IsAvailable;
            int index = _options.Items.Add(new ChoiceItem<PrintOption>(option,
                unavailable ? label + " (" + (option switch
                {
                    PrintOption.DisplacementDiagram when InputDataService.Instance.dimension != 3
                        => "3D のみ",
                    PrintOption.LoadDiagram => "荷重ケースなし",
                    _ => "該当する計算結果なし",
                }) + ")" : label));
            if (option == PrintOption.Input) _options.SetItemChecked(index, true);
            if (unavailable) _unavailableOptions.Add(index);
        }
        if (presentation is not null)
        {
            foreach (var page in presentation.Pages)
                _cases.Items.Add(new ChoiceItem<string>(page.Key, page.Label));
            if (presentation.Derived is { } derived)
                foreach (var item in derived.Combines.Concat(derived.Pickups)
                    .DistinctBy(item => item.Id))
                    _derived.Items.Add(new ChoiceItem<string>(item.Id, item.Name ?? item.Id));
        }
        _displacementComponents.Items.AddRange(DisplacementChoices);
        _reactionComponents.Items.AddRange(ReactionChoices);
        _sectionComponents.Items.AddRange(SectionForceChoices);
        _diagramComponents.Items.AddRange(["disg", "fx", "fy", "fz", "mx", "my", "mz"]);
        _loadDiagramComponents.Items.AddRange(["axis", "load"]);
        _paper.Items.AddRange([PrintPaper.A4, PrintPaper.A3]);
        _orientation.Items.AddRange([PrintOrientation.Vertical, PrintOrientation.Horizontal]);
        _layout.Items.AddRange([PrintLayout.Single, PrintLayout.SplitHorizontal, PrintLayout.SplitVertical]);
        _language.Items.AddRange(["日本語", "English"]);
        _paper.SelectedIndex = _orientation.SelectedIndex = _layout.SelectedIndex = _language.SelectedIndex = 0;
    }

    internal PrintSelection ReadSelection()
    {
        var options = _options.CheckedItems.Cast<ChoiceItem<PrintOption>>()
            .Select(item => item.Value).ToArray();
        if (options.Length == 0) throw new InvalidOperationException("印刷項目を選択してください。");
        double scaleX = ReadScale(_scaleX.Text, "X");
        double scaleY = ReadScale(_scaleY.Text, "Y");
        return new PrintSelection(options,
            CaseIds: CheckedValues(_cases), DerivedIds: CheckedValues(_derived),
            NodeIds: ParseIds(_nodes.Text), MemberIds: ParseIds(_members.Text),
            DisplacementComponents: CheckedComponents(_displacementComponents),
            ReactionComponents: CheckedComponents(_reactionComponents),
            SectionForceComponents: CheckedComponents(_sectionComponents),
            DiagramComponents: CheckedComponents(_diagramComponents),
            LoadDiagramComponents: CheckedComponents(_loadDiagramComponents),
            Layout: (PrintLayout)_layout.SelectedItem!,
            Orientation: (PrintOrientation)_orientation.SelectedItem!,
            Paper: (PrintPaper)_paper.SelectedItem!, ScaleX: scaleX, ScaleY: scaleY,
            Language: _language.SelectedIndex == 0 ? "ja" : "en", Title: _title.Text);
    }

    private static string[]? CheckedValues(CheckedListBox list) => list.CheckedItems.Count == 0
        ? null : list.CheckedItems.Cast<ChoiceItem<string>>().Select(item => item.Value).ToArray();

    private static string[]? CheckedComponents(CheckedListBox list) => list.CheckedItems.Count == 0
        ? null : list.CheckedItems.Cast<string>().ToArray();

    private static string[] ExpandModes(string[] names) => names.SelectMany(name =>
        new[] { name, name + "_max", name + "_min" }).ToArray();

    private static string[]? ParseIds(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        string[] ids = input.Split(',', StringSplitOptions.TrimEntries);
        if (ids.Any(id => id.Length == 0)) throw new InvalidOperationException("ID をカンマで区切って入力してください。");
        return ids.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static double ReadScale(string input, string axis)
    {
        if (!double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out double scale) ||
            !double.IsFinite(scale) || scale <= 0 || scale > 1_000_000)
            throw new InvalidOperationException($"{axis} 倍率は 0 より大きい数を入力してください。");
        return scale;
    }

    internal async Task InitializePreviewAsync()
    {
        try
        {
            await _preview.EnsureCoreWebView2Async();
            if (_closing) return;
            _preview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webReady = true;
        }
        catch (Exception error)
        {
            _status.Text = "PDF プレビューを開始できません: " + error.Message;
            _generate.Enabled = false;
            _print.Enabled = false;
        }
    }

    internal async Task GenerateAsync(bool showErrorDialog = true)
    {
        if (_closing || !_webReady) return;
        _generation?.Cancel();
        _generation?.Dispose();
        _generation = new CancellationTokenSource();
        CancellationToken cancellation = _generation.Token;
        _generate.Enabled = false;
        _cancel.Enabled = true;
        _status.Text = "PDF を生成しています...";
        try
        {
            PrintSnapshot snapshot = PrintProjection.Capture(ReadSelection());
            IReadOnlyList<PrintDiagramImage> images = snapshot.DiagramRequests.Count == 0
                ? [] : _capture(snapshot.DiagramRequests);
            cancellation.ThrowIfCancellationRequested();
            byte[] bytes = await Task.Run(() =>
            {
                string json = PrintProjection.Build(snapshot, images);
                cancellation.ThrowIfCancellationRequested();
                return PDF_Manager.DirectPdfGenerator.Generate(json, cancellation);
            }, cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (_closing || IsDisposed || !PrintProjection.IsCurrent(snapshot))
                throw new InvalidOperationException("文書が変更されたため、このプレビューは破棄しました。");
            await DisplayAsync(bytes, cancellation);
            _status.Text = $"PDF を表示中 ({_pageCount} ページ)";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (!_closing && !IsDisposed) _status.Text = "生成を中止しました。前のプレビューを保持しています。";
        }
        catch (Exception error)
        {
            if (!_closing && !IsDisposed)
            {
                _status.Text = "生成できませんでした。前のプレビューを保持しています。";
                if (showErrorDialog)
                    MessageBox.Show(this, error.Message, "印刷プレビュー エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!_closing && !IsDisposed)
            {
                _generate.Enabled = _webReady;
                _cancel.Enabled = false;
            }
        }
    }

    private async Task DisplayAsync(byte[] bytes, CancellationToken cancellation)
    {
        using var document = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        int pageCount = document.PageCount;
        if (pageCount < 1) throw new InvalidDataException("PDF にページがありません。");
        if (_tempFolder is null)
        {
            _tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),
                "FrameWebPrint-" + Guid.NewGuid().ToString("N"))).FullName;
            _tempLease = new FileStream(Path.Combine(_tempFolder, "session.lock"),
                FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        }
        EnsurePreviewCapacity(_tempFolder, _displayedPath, bytes.Length);
        string nextPath = Path.Combine(_tempFolder, Guid.NewGuid().ToString("N") + ".pdf");
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnNavigation(object? sender, CoreWebView2NavigationCompletedEventArgs args)
            => completion.TrySetResult(args.IsSuccess);
        try
        {
            await File.WriteAllBytesAsync(nextPath, bytes, cancellation);
            cancellation.ThrowIfCancellationRequested();
            _preview.NavigationCompleted += OnNavigation;
            _preview.CoreWebView2.Navigate(new Uri(nextPath).AbsoluteUri);
            if (!await completion.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellation))
                throw new InvalidOperationException("PDF を表示できませんでした。");
            if (_closing || IsDisposed) return;
            _displayedPdf = bytes;
            _displayedPath = nextPath;
            _pageCount = pageCount;
            _pageNumber = 1;
            UpdatePageControls();
            _save.Enabled = _print.Enabled = true;
            PruneInactivePreviewFiles(_tempFolder, nextPath);
        }
        catch
        {
            if (!_closing && !IsDisposed)
                _preview.CoreWebView2.Navigate(_displayedPath is null ? "about:blank" :
                    new Uri(_displayedPath).AbsoluteUri + "#page=" + _pageNumber);
            PruneInactivePreviewFiles(_tempFolder, _displayedPath);
            throw;
        }
        finally
        {
            _preview.NavigationCompleted -= OnNavigation;
            // A locked retired file may remain, but capacity rejects a further generation.
        }
    }

    /// <summary>Never create a third retained PDF while the viewer still holds an old file open.</summary>
    internal static void EnsurePreviewCapacity(string folder, string? activePath, int nextBytes)
    {
        PruneInactivePreviewFiles(folder, activePath);
        FileInfo[] retained = Directory.GetFiles(folder, "*.pdf").Select(path => new FileInfo(path)).ToArray();
        if (nextBytes < 0 || retained.Length >= MaximumRetainedPreviewFiles ||
            retained.Sum(file => file.Length) + nextBytes > MaximumRetainedPreviewBytes)
            throw new IOException("前のプレビュー PDF を解放できません。印刷画面を閉じて開き直してください。");
    }

    private static void PruneInactivePreviewFiles(string folder, string? activePath)
    {
        foreach (string path in Directory.GetFiles(folder, "*.pdf"))
        {
            if (string.Equals(path, activePath, StringComparison.OrdinalIgnoreCase)) continue;
            try { File.Delete(path); }
            catch (IOException) { /* WebView2 may still hold this retired file. */ }
            catch (UnauthorizedAccessException) { /* The next generation will enforce capacity. */ }
        }
    }

    private void NavigatePage(int page)
    {
        if (!_webReady || _displayedPath is null || page < 1 || page > _pageCount) return;
        _pageNumber = page;
        _preview.CoreWebView2.Navigate(new Uri(_displayedPath).AbsoluteUri + "#page=" + page);
        UpdatePageControls();
    }

    private void UpdatePageControls()
    {
        _pageLabel.Text = $"{_pageNumber} / {_pageCount}";
        _previous.Enabled = _pageNumber > 1;
        _next.Enabled = _pageNumber < _pageCount;
    }

    private void SaveDisplayedPdf()
    {
        if (_displayedPdf is null) return;
        using var dialog = new SaveFileDialog { Filter = "PDF ファイル (*.pdf)|*.pdf",
            DefaultExt = "pdf", AddExtension = true, OverwritePrompt = true,
            RestoreDirectory = true, FileName = "FrameWeb.pdf" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        string destination = dialog.FileName;
        try
        {
            SaveAtomically(destination, _displayedPdf);
            _status.Text = "PDF を保存しました。";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, error.Message, "PDF 保存エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal static void SaveAtomically(string destination, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentNullException.ThrowIfNull(bytes);
        string temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destination))!,
            "." + Path.GetFileName(destination) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private void PrintDisplayedPdf()
    {
        if (_displayedPdf is null || _displayedPath is null || !_webReady) return;
        try
        {
            // The system dialog supplies printer, page range, paper, orientation, and copies.
            // It prints the PDF already loaded in this preview, not a regenerated document.
            _preview.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.System);
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, "印刷エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CleanupPreviewFiles()
    {
        _preview.Dispose();
        _tempLease?.Dispose();
        _tempLease = null;
        if (_tempFolder is null) return;
        string folder = _tempFolder;
        _ = Task.Run(async () =>
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                try { Directory.Delete(folder, recursive: true); return; }
                catch (DirectoryNotFoundException) { return; }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(500);
                }
            }
            System.Diagnostics.Trace.WriteLine($"Could not remove print preview directory: {folder}");
        });
    }

    private static void CleanupOrphanPreviewDirectories()
    {
        try
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-1);
            foreach (string folder in Directory.EnumerateDirectories(Path.GetTempPath(),
                "FrameWebPrint-*", SearchOption.TopDirectoryOnly)
                .Where(folder => Directory.GetLastWriteTimeUtc(folder) < cutoff).Take(32))
                TryCleanupOrphanDirectory(folder, cutoff);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.WriteLine($"Print orphan scan failed: {error.Message}");
        }
    }

    internal static bool TryCleanupOrphanDirectory(string folder, DateTime cutoffUtc)
    {
        var directory = new DirectoryInfo(folder);
        if (!directory.Exists || (directory.Attributes & FileAttributes.ReparsePoint) != 0 ||
            !directory.Name.StartsWith("FrameWebPrint-", StringComparison.Ordinal) ||
            !Guid.TryParseExact(directory.Name["FrameWebPrint-".Length..], "N", out _) ||
            directory.LastWriteTimeUtc >= cutoffUtc) return false;
        string leasePath = Path.Combine(directory.FullName, "session.lock");
        if (!File.Exists(leasePath)) return false;
        try
        {
            using (new FileStream(leasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                // Refuse to remove a directory with unexpected content or nested entries.
                if (directory.EnumerateFileSystemInfos().Any(entry =>
                    entry is not FileInfo || entry.Name != "session.lock" &&
                    !entry.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))) return false;
                foreach (FileInfo pdf in directory.EnumerateFiles("*.pdf")) pdf.Delete();
            }
            File.Delete(leasePath);
            directory.Delete();
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed record ChoiceItem<T>(T Value, string Label)
    {
        public override string ToString() => Label;
    }
}

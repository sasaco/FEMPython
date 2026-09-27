using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using FrameWebforCS.calculation;
using System.Globalization;

namespace FrameWebforCS.components.result;

public partial class ResultCombineFsecComponent : UserControl
{
    private static readonly (string Key, string Title)[] Modes3D =
    [
        ("fx_max", "軸方向力 最大"), ("fx_min", "軸方向力 最小"),
        ("fy_max", "Y方向のせん断力 最大"), ("fy_min", "Y方向のせん断力 最小"),
        ("fz_max", "Z方向のせん断力 最大"), ("fz_min", "Z方向のせん断力 最小"),
        ("mx_max", "ねじりモーメント 最大"), ("mx_min", "ねじりモーメント 最小"),
        ("my_max", "Y軸回りの曲げモーメント 最大"), ("my_min", "Y軸回りの曲げモーメント 最小"),
        ("mz_max", "Z軸回りの曲げモーメント 最大"), ("mz_min", "Z軸回りの曲げモーメント 最小")
    ];

    private static readonly (string Key, string Title)[] Modes2D =
    [
        ("fx_max", "軸方向力 最大"), ("fx_min", "軸方向力 最小"),
        ("fy_max", "せん断力 最大"), ("fy_min", "せん断力 最小"),
        ("mz_max", "曲げモーメント 最大"), ("mz_min", "曲げモーメント 最小")
    ];

    private readonly ResultCombineFsecCoordinator _coordinator = ResultCombineFsecCoordinator.Instance;
    private readonly Func<ResultCombineFsecSnapshot, CancellationToken, ResultCombineFsecOutput> _calculate;
    private Control? _uiDispatcher;
    private ResultCombineFsecOutput? _output;
    private ResultCombineFsecSnapshot? _pending;
    private CancellationTokenSource? _runningCancellation;
    internal Task? CurrentCalculationTask { get; private set; }
    internal bool IsCalculationRunning => _runningCancellation != null;
    private long _generation;
    private long _publishedRevision = -1;
    private int _materializedSheet = -1;
    private bool _rebuilding;
    private bool _disposed;
    private CalculationDerivedViewRenderer? _canonical;

    public ResultCombineFsecComponent() : this(ResultCombineFsecAggregator.Calculate) { }

    internal ResultCombineFsecComponent(
        Func<ResultCombineFsecSnapshot, CancellationToken, ResultCombineFsecOutput> calculate)
    {
        _calculate = calculate ?? throw new ArgumentNullException(nameof(calculate));
        InitializeComponent();
        ResultDimensionNotice.Attach(this);
        fpSpread1.EditModeOn += fpSpread1.faSpread_EditModeOn;
        // This handle outlives the view handle, so in-flight work can complete
        // on the UI thread even while WinForms recreates the view handle.
        _uiDispatcher = new Control();
        _ = _uiDispatcher.Handle;
        _canonical = new(CalculationDerivedStage.Combine, CalculationDerivedQuantity.SectionForce,
            fpSpread1, modeSelector, statusLabel, Modes3D, Modes2D, ConfigureSheet);
        CalculationResultStore.Instance.Changed += OnCanonicalChanged;

        modeSelector.SelectedIndexChanged += (_, _) => MaterializeSelectedSheet();
        fpSpread1.ActiveSheetChanged += (_, _) => MaterializeSelectedSheet();
        // AppRoutingModule calls setActiveSheet before showing the floating form.
        VisibleChanged += (_, _) => { if (Visible) PublishViewportPage(); };
        HandleCreated += (_, _) => RefreshFromCoordinator();
        HandleDestroyed += (_, _) =>
        {
            CancelRunning();
        };
        _coordinator.Changed += OnCoordinatorChanged;
        RefreshFromCoordinator();
    }

    public void setActiveSheet(int index)
    {
        // Sidebar option=1 is the route category, not a result sheet index.
        if (fpSpread1.Sheets.Count > 0 && fpSpread1.ActiveSheetIndex < 0)
            fpSpread1.ActiveSheetIndex = 0;
        RefreshFromCoordinator();
        PublishViewportPage();
    }

    public virtual Dictionary<string, object> getCombineFsec() =>
        InputDataService.Instance.getCombineFsec();

    private void OnCoordinatorChanged(object? sender, EventArgs e)
    {
        if (_disposed || _uiDispatcher is not { } dispatcher) return;
        if (dispatcher.InvokeRequired)
        {
            try { dispatcher.BeginInvoke((System.Action)RefreshFromCoordinator); }
            catch (InvalidOperationException) { }
            return;
        }
        RefreshFromCoordinator();
    }

    private void OnCanonicalChanged(object? sender, EventArgs e) => OnCoordinatorChanged(sender, e);

    private void RefreshFromCoordinator()
    {
        if (_disposed) return;
        if (_canonical?.ShowCurrent() == true)
        {
            ++_generation;
            CancelRunning();
            _pending = null;
            _output = null;
            return;
        }
        long generation = ++_generation;
        CancelRunning();
        _pending = null;

        if (_coordinator.State != CombineFsecState.Valid ||
            _coordinator.Snapshot is not { } snapshot)
        {
            _output = null;
            _publishedRevision = -1;
            ClearDisplay();
            statusLabel.Text = _coordinator.State == CombineFsecState.Loading
                ? "組合せ断面力を読み込み中..."
                : _coordinator.Error is { } error
                    ? $"組合せ断面力を表示できません: {error}"
                    : "組合せ断面力を表示できません。結果を読み込んでください。";
            return;
        }

        if (_publishedRevision == snapshot.Revision && _output != null) return;
        _output = null;
        _publishedRevision = -1;
        ClearDisplay();
        statusLabel.Text = "組合せ断面力を集計中...";
        _pending = snapshot;
        if (IsHandleCreated && _runningCancellation == null)
            StartPending(generation);
    }

    private void StartPending(long generation)
    {
        if (_disposed || !IsHandleCreated || _runningCancellation != null ||
            _pending is not { } snapshot) return;
        _pending = null;
        var cancellation = new CancellationTokenSource();
        _runningCancellation = cancellation;
        CurrentCalculationTask = CalculateAsync(snapshot, generation, cancellation);
    }

    private async Task CalculateAsync(
        ResultCombineFsecSnapshot snapshot, long generation, CancellationTokenSource cancellation)
    {
        ResultCombineFsecOutput? result = null;
        Exception? error = null;
        try
        {
            result = await Task.Run(
                () => _calculate(snapshot, cancellation.Token),
                cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex) { error = ex; }

        if (_disposed || _uiDispatcher is not { } dispatcher)
        {
            cancellation.Dispose();
            return;
        }
        try
        {
            dispatcher.BeginInvoke((System.Action)(() =>
                CompleteCalculation(snapshot, generation, cancellation, result, error)));
        }
        catch (InvalidOperationException) { cancellation.Dispose(); }
    }

    private void CompleteCalculation(
        ResultCombineFsecSnapshot snapshot, long generation, CancellationTokenSource cancellation,
        ResultCombineFsecOutput? result, Exception? error)
    {
        if (!ReferenceEquals(_runningCancellation, cancellation))
        {
            cancellation.Dispose();
            return;
        }
        _runningCancellation = null;
        cancellation.Dispose();
        if (_disposed) return;

        if (IsHandleCreated && generation == _generation &&
            _coordinator.State == CombineFsecState.Valid &&
            _coordinator.Revision == snapshot.Revision)
        {
            if (error != null)
            {
                _output = null;
                ClearDisplay();
                statusLabel.Text = $"組合せ断面力の集計に失敗しました: {error.Message}";
            }
            else if (result != null)
            {
                try
                {
                    _output = result;
                    PublishResult(result, snapshot.Dimension);
                    _publishedRevision = snapshot.Revision;
                }
                catch (Exception ex)
                {
                    _output = null;
                    _publishedRevision = -1;
                    ClearDisplay();
                    statusLabel.Text = $"組合せ断面力の表示に失敗しました: {ex.Message}";
                }
            }
        }
        if (IsHandleCreated && _pending != null) StartPending(_generation);
    }

    private void PublishResult(ResultCombineFsecOutput result, int dimension)
    {
        _rebuilding = true;
        try
        {
            fpSpread1.Sheets.Clear();
            _materializedSheet = -1;
            modeSelector.Items.Clear();
            foreach (var mode in dimension == 3 ? Modes3D : Modes2D)
                modeSelector.Items.Add(new ModeChoice(mode.Key, mode.Title));

            foreach (CombineFsecCaseResult item in result.Cases)
            {
                SheetView sheet = fpSpread1.AddNewSheetView();
                sheet.SheetName = string.IsNullOrWhiteSpace(item.Name)
                    ? item.Id : $"{item.Id} {item.Name}";
                ConfigureSheet(sheet, dimension);
                sheet.RowCount = 0;
            }
            if (modeSelector.Items.Count > 0) modeSelector.SelectedIndex = 0;
            if (fpSpread1.Sheets.Count > 0) fpSpread1.ActiveSheetIndex = 0;
            Width = dimension == 3 ? 900 : 650;
        }
        finally { _rebuilding = false; }

        statusLabel.Text = result.Cases.Count == 0
            ? "組合せ結果がありません。"
            : $"{result.Cases.Count} 件の組合せ断面力";
        MaterializeSelectedSheet();
    }

    private void MaterializeSelectedSheet()
    {
        if (_canonical?.IsShowing == true) { _canonical.Materialize(); return; }
        if (_rebuilding || _output == null || modeSelector.SelectedItem is not ModeChoice mode)
            return;
        int sheetIndex = fpSpread1.ActiveSheetIndex;
        if (sheetIndex < 0 || sheetIndex >= _output.Cases.Count) return;

        if (_materializedSheet >= 0 && _materializedSheet != sheetIndex &&
            _materializedSheet < fpSpread1.Sheets.Count)
            fpSpread1.Sheets[_materializedSheet].RowCount = 0;

        SheetView sheet = fpSpread1.Sheets[sheetIndex];
        IReadOnlyList<CombineFsecRowResult> rows =
            _output.Cases[sheetIndex].Rows.TryGetValue(mode.Key, out var selected)
                ? selected : Array.Empty<CombineFsecRowResult>();
        sheet.RowCount = rows.Count;
        bool is3D = sheet.ColumnCount == 10;
        for (int row = 0; row < rows.Count; row++)
        {
            CombineFsecRowResult value = rows[row];
            sheet.Cells[row, 0].Text = value.MemberDisplay;
            sheet.Cells[row, 1].Text = value.NodeId;
            sheet.Cells[row, 2].Text = value.Location.ToString("F3", CultureInfo.InvariantCulture);
            sheet.Cells[row, 3].Text = Format(value.Fx);
            sheet.Cells[row, 4].Text = Format(value.Fy);
            if (is3D)
            {
                sheet.Cells[row, 5].Text = Format(value.Fz);
                sheet.Cells[row, 6].Text = Format(value.Mx);
                sheet.Cells[row, 7].Text = Format(value.My);
                sheet.Cells[row, 8].Text = Format(value.Mz);
                sheet.Cells[row, 9].Text = value.Case;
            }
            else
            {
                sheet.Cells[row, 5].Text = Format(value.Mz);
                sheet.Cells[row, 6].Text = value.Case;
            }
        }
        _materializedSheet = sheetIndex;
        PublishViewportPage();
    }

    private void PublishViewportPage()
    {
        if (_canonical?.IsShowing == true) { _canonical.PublishPage(); return; }
        if (!Visible || _output == null || modeSelector.SelectedItem is not ModeChoice mode) return;
        int index = fpSpread1.ActiveSheetIndex;
        if (index < 0 || index >= _output.Cases.Count) return;
        var selected = _output.Cases[index];
        var samples = ProjectViewportSamples(selected, mode.Key);
        FrameWebforCS.three.ThreeResultsService.PublishDerivedFsec("comb_fsec",
            new Dictionary<string, IReadOnlyList<FrameWebforCS.three.SectionForceSample>>
            { [selected.Id] = samples }, _coordinator.Revision);
        FrameWebforCS.three.ThreeResultsService.PublishPage("comb_fsec", selected.Id, mode.Key);
    }

    internal static IReadOnlyList<FrameWebforCS.three.SectionForceSample> ProjectViewportSamples(
        CombineFsecCaseResult selected, string modeKey)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentException.ThrowIfNullOrWhiteSpace(modeKey);
        string component = modeKey.Split('_')[0];
        var samples = new List<FrameWebforCS.three.SectionForceSample>();
        foreach (var (key, envelope) in new[]
        {
            ($"{component}_max", FrameWebforCS.three.SectionForceEnvelope.Max),
            ($"{component}_min", FrameWebforCS.three.SectionForceEnvelope.Min)
        })
        {
            if (!selected.Rows.TryGetValue(key, out var rows)) continue;
            foreach (var row in rows)
            {
                string memberText = row.MemberId.StartsWith("member", StringComparison.Ordinal)
                    ? row.MemberId[6..] : row.MemberId;
                if (!int.TryParse(memberText, NumberStyles.None, CultureInfo.InvariantCulture, out int memberId) || memberId <= 0)
                    continue;
                double value = component switch
                {
                    "fx" => row.Fx, "fy" => row.Fy, "fz" => row.Fz,
                    "mx" => row.Mx, "my" => row.My, "mz" => row.Mz,
                    _ => 0
                };
                if (double.IsFinite(value) && double.IsFinite(row.Location))
                    samples.Add(new(memberId, (float)row.Location, (float)value, envelope));
            }
        }
        return samples;
    }

    private static string Format(double value) => ResultPickupFsecAggregator.Format(value);

    private void ClearDisplay()
    {
        _rebuilding = true;
        try
        {
            fpSpread1.Sheets.Clear();
            modeSelector.Items.Clear();
            _materializedSheet = -1;
        }
        finally { _rebuilding = false; }
        if (Visible)
        {
            FrameWebforCS.three.ThreeResultsService.PublishDerivedFsec("comb_fsec",
                new Dictionary<string, IReadOnlyList<FrameWebforCS.three.SectionForceSample>>(), _coordinator.Revision);
            FrameWebforCS.three.ThreeResultsService.PublishPage("comb_fsec", null);
        }
    }

    private static void ConfigureSheet(SheetView sheet, int dimension)
    {
        sheet.ColumnCount = dimension == 3 ? 10 : 7;
        var header = sheet.ColumnHeader;
        header.RowCount = 2;
        header.Cells[0, 0].Text = "部材";
        header.Cells[1, 0].Text = "No";
        header.Cells[0, 1].Text = "節点";
        header.Cells[1, 1].Text = "No";
        header.Cells[0, 2].Text = "着目位置";
        header.Cells[1, 2].Text = "(m)";
        header.Cells[0, 3].Text = "軸方向力";
        header.Cells[1, 3].Text = "(kN)";
        header.Cells[0, 4].Text = dimension == 3 ? "せん断力(kN)" : "せん断力";
        header.Cells[1, 4].Text = dimension == 3 ? "Y軸方向" : "(kN)";
        if (dimension == 3)
        {
            header.Cells[1, 5].Text = "Z軸方向";
            header.Cells[0, 4].ColumnSpan = 2;
            header.Cells[0, 6].Text = "ねじりモーメント";
            header.Cells[1, 6].Text = "(kN・m)";
            header.Cells[0, 7].Text = "曲げモーメント(kN・m)";
            header.Cells[1, 7].Text = "Y軸回り";
            header.Cells[1, 8].Text = "Z軸回り";
            header.Cells[0, 7].ColumnSpan = 2;
        }
        else
        {
            header.Cells[0, 5].Text = "曲げモーメント";
            header.Cells[1, 5].Text = "(kN・m)";
        }
        int caseColumn = sheet.ColumnCount - 1;
        header.Cells[0, caseColumn].Text = "組み合わせ";
        sheet.Columns[0].Width = 50;
        sheet.Columns[1].Width = 50;
        for (int i = 0; i < sheet.ColumnCount; i++)
            sheet.Columns[i].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
        for (int i = 2; i < caseColumn; i++) sheet.Columns[i].Width = 80;
        sheet.Columns[caseColumn].Width = 200;
        sheet.Protect = true;
    }

    private void DisposeCombineResources()
    {
        _disposed = true;
        CalculationResultStore.Instance.Changed -= OnCanonicalChanged;
        _coordinator.Changed -= OnCoordinatorChanged;
        CancelRunning();
        _runningCancellation?.Dispose();
        _runningCancellation = null;
        _uiDispatcher?.Dispose();
        _uiDispatcher = null;
        _pending = null;
    }

    private void CancelRunning()
    {
        try { _runningCancellation?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    private sealed record ModeChoice(string Key, string Title)
    {
        public override string ToString() => Title;
    }
}


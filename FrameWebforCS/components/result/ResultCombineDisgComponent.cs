using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using System.Globalization;

namespace FrameWebforCS.components.result;

public partial class ResultCombineDisgComponent : UserControl
{
    private static readonly (string Key, string Title)[] Modes3D =
    [
        ("dx_max", "X方向の移動量 最大"), ("dx_min", "X方向の移動量 最小"),
        ("dy_max", "Y方向の移動量 最大"), ("dy_min", "Y方向の移動量 最小"),
        ("dz_max", "Z方向の移動量 最大"), ("dz_min", "Z方向の移動量 最小"),
        ("rx_max", "X軸回りの回転角 最大"), ("rx_min", "X軸回りの回転角 最小"),
        ("ry_max", "Y軸回りの回転角 最大"), ("ry_min", "Y軸回りの回転角 最小"),
        ("rz_max", "Z軸回りの回転角 最大"), ("rz_min", "Z軸回りの回転角 最小")
    ];

    private static readonly (string Key, string Title)[] Modes2D =
    [
        ("dx_max", "X方向の移動量 最大"), ("dx_min", "X方向の移動量 最小"),
        ("dy_max", "Y方向の移動量 最大"), ("dy_min", "Y方向の移動量 最小"),
        ("rz_max", "Z軸回りの回転角 最大"), ("rz_min", "Z軸回りの回転角 最小")
    ];

    private readonly ResultCombineDisgCoordinator _coordinator = ResultCombineDisgCoordinator.Instance;
    private readonly Func<ResultCombineDisgSnapshot, CancellationToken, ResultCombineDisgOutput> _calculate;
    private Control? _uiDispatcher;
    private ResultCombineDisgOutput? _output;
    private ResultCombineDisgSnapshot? _pending;
    private CancellationTokenSource? _runningCancellation;
    internal Task? CurrentCalculationTask { get; private set; }
    internal bool IsCalculationRunning => _runningCancellation != null;
    private long _generation;
    private long _publishedRevision = -1;
    private int _materializedSheet = -1;
    private bool _rebuilding;
    private bool _disposed;

    protected virtual bool UsesLegacyPickup => false;

    public ResultCombineDisgComponent() : this(ResultCombineDisgAggregator.Calculate) { }

    internal ResultCombineDisgComponent(
        Func<ResultCombineDisgSnapshot, CancellationToken, ResultCombineDisgOutput> calculate)
    {
        _calculate = calculate ?? throw new ArgumentNullException(nameof(calculate));
        InitializeComponent();
        fpSpread1.EditModeOn += fpSpread1.faSpread_EditModeOn;
        if (UsesLegacyPickup)
        {
            modePanel.Visible = false;
            statusLabel.Visible = false;
            BuildLegacyPickupSheets();
            return;
        }

        // This handle outlives the view handle, so in-flight work can complete
        // on the UI thread even while WinForms recreates the view handle.
        _uiDispatcher = new Control();
        _ = _uiDispatcher.Handle;

        modeSelector.SelectedIndexChanged += (_, _) => MaterializeSelectedSheet();
        fpSpread1.ActiveSheetChanged += (_, _) => MaterializeSelectedSheet();
        // AppRoutingModule calls setActiveSheet before showing the floating form.
        VisibleChanged += (_, _) => { if (Visible) MaterializeSelectedSheet(); };
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
        if (UsesLegacyPickup)
        {
            if (index >= 0 && index < fpSpread1.Sheets.Count)
                fpSpread1.ActiveSheetIndex = index;
            return;
        }

        // Sidebar option=1 is the route category, not a result sheet index.
        if (fpSpread1.Sheets.Count > 0 && fpSpread1.ActiveSheetIndex < 0)
            fpSpread1.ActiveSheetIndex = 0;
        RefreshFromCoordinator();
    }

    public virtual Dictionary<string, object> getCombineDisg() =>
        InputDataService.Instance.getCombineDisg();

    private void BuildLegacyPickupSheets()
    {
        foreach (var item in getCombineDisg())
        {
            SheetView sheet = fpSpread1.AddNewSheetView();
            sheet.SheetName = item.Key;
            ConfigureSheet(sheet, InputDataService.Instance.dimension);
        }
        if (fpSpread1.Sheets.Count > 0) fpSpread1.ActiveSheetIndex = 0;
        Width = InputDataService.Instance.dimension == 3 ? 900 : 650;
    }

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

    private void RefreshFromCoordinator()
    {
        if (_disposed || UsesLegacyPickup) return;
        long generation = ++_generation;
        CancelRunning();
        _pending = null;

        if (_coordinator.State != CombineDisgState.Valid ||
            _coordinator.Snapshot is not { } snapshot)
        {
            _output = null;
            _publishedRevision = -1;
            ClearDisplay();
            statusLabel.Text = _coordinator.State == CombineDisgState.Loading
                ? "組合せ変位を読み込み中..."
                : "組合せ変位を表示できません。結果を読み込んでください。";
            return;
        }

        if (_publishedRevision == snapshot.Revision && _output != null) return;
        _output = null;
        _publishedRevision = -1;
        ClearDisplay();
        statusLabel.Text = "組合せ変位を集計中...";
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
        ResultCombineDisgSnapshot snapshot, long generation, CancellationTokenSource cancellation)
    {
        ResultCombineDisgOutput? result = null;
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
        ResultCombineDisgSnapshot snapshot, long generation, CancellationTokenSource cancellation,
        ResultCombineDisgOutput? result, Exception? error)
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
            _coordinator.State == CombineDisgState.Valid &&
            _coordinator.Revision == snapshot.Revision)
        {
            if (error != null)
            {
                _output = null;
                ClearDisplay();
                statusLabel.Text = $"組合せ変位の集計に失敗しました: {error.Message}";
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
                    statusLabel.Text = $"組合せ変位の表示に失敗しました: {ex.Message}";
                }
            }
        }
        if (IsHandleCreated && _pending != null) StartPending(_generation);
    }

    private void PublishResult(ResultCombineDisgOutput result, int dimension)
    {
        _rebuilding = true;
        try
        {
            fpSpread1.Sheets.Clear();
            _materializedSheet = -1;
            modeSelector.Items.Clear();
            foreach (var mode in dimension == 3 ? Modes3D : Modes2D)
                modeSelector.Items.Add(new ModeChoice(mode.Key, mode.Title));

            foreach (CombineDisgCaseResult item in result.Cases)
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
            : $"{result.Cases.Count} 件の組合せ結果";
        MaterializeSelectedSheet();
    }

    private void MaterializeSelectedSheet()
    {
        if (_rebuilding || _output == null || modeSelector.SelectedItem is not ModeChoice mode)
            return;
        int sheetIndex = fpSpread1.ActiveSheetIndex;
        if (sheetIndex < 0 || sheetIndex >= _output.Cases.Count) return;

        if (_materializedSheet >= 0 && _materializedSheet != sheetIndex &&
            _materializedSheet < fpSpread1.Sheets.Count)
            fpSpread1.Sheets[_materializedSheet].RowCount = 0;

        SheetView sheet = fpSpread1.Sheets[sheetIndex];
        IReadOnlyList<CombineDisgNodeResult> rows =
            _output.Cases[sheetIndex].Rows.TryGetValue(mode.Key, out var selected)
                ? selected : Array.Empty<CombineDisgNodeResult>();
        sheet.RowCount = rows.Count;
        bool is3D = sheet.ColumnCount == 8;
        for (int row = 0; row < rows.Count; row++)
        {
            CombineDisgNodeResult value = rows[row];
            sheet.Cells[row, 0].Text = value.Id;
            sheet.Cells[row, 1].Text = Format(value.Dx);
            sheet.Cells[row, 2].Text = Format(value.Dy);
            if (is3D)
            {
                sheet.Cells[row, 3].Text = Format(value.Dz);
                sheet.Cells[row, 4].Text = Format(value.Rx);
                sheet.Cells[row, 5].Text = Format(value.Ry);
                sheet.Cells[row, 6].Text = Format(value.Rz);
                sheet.Cells[row, 7].Text = value.Case;
            }
            else
            {
                sheet.Cells[row, 3].Text = Format(value.Rz);
                sheet.Cells[row, 4].Text = value.Case;
            }
        }
        _materializedSheet = sheetIndex;
        if (Visible && _output != null && modeSelector.SelectedItem is ModeChoice selectedMode)
            FrameWebforCS.three.ThreeResultsService.PublishPage(
                "comb_disg", _output.Cases[sheetIndex].Id, selectedMode.Key);
    }

    private static string Format(double value) =>
        (Math.Floor(value * 10_000 + 0.5) / 10_000)
            .ToString("F4", CultureInfo.InvariantCulture);

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
    }

    private static void ConfigureSheet(SheetView sheet, int dimension)
    {
        var header = sheet.ColumnHeader;
        header.RowCount = 2;
        if (dimension == 3)
        {
            sheet.ColumnCount = 8;
            header.Cells[0, 0].Text = "節点";
            header.Cells[1, 0].Text = "No";
            header.Cells[0, 1].Text = "移動量(mm)";
            header.Cells[1, 1].Text = "X方向";
            header.Cells[1, 2].Text = "Y方向";
            header.Cells[1, 3].Text = "Z方向";
            header.Cells[0, 1].ColumnSpan = 3;
            header.Cells[0, 4].Text = "回転(‰rad)";
            header.Cells[1, 4].Text = "X軸回り";
            header.Cells[1, 5].Text = "Y軸回り";
            header.Cells[1, 6].Text = "Z軸回り";
            header.Cells[0, 4].ColumnSpan = 3;
        }
        else
        {
            sheet.ColumnCount = 5;
            header.Cells[0, 0].Text = "節点";
            header.Cells[1, 0].Text = "No";
            header.Cells[0, 1].Text = "移動量(mm)";
            header.Cells[1, 1].Text = "X方向";
            header.Cells[1, 2].Text = "Y方向";
            header.Cells[0, 1].ColumnSpan = 2;
            header.Cells[0, 3].Text = "回転";
            header.Cells[1, 3].Text = "(‰rad)";
        }
        int caseColumn = sheet.ColumnCount - 1;
        header.Cells[0, caseColumn].Text = "組み合わせ";
        sheet.Columns[0].Width = 50;
        for (int i = 0; i < sheet.ColumnCount; i++)
            sheet.Columns[i].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
        for (int i = 1; i < caseColumn; i++) sheet.Columns[i].Width = 80;
        sheet.Columns[caseColumn].Width = 200;
        sheet.Protect = true;
    }

    private void DisposeCombineResources()
    {
        _disposed = true;
        if (!UsesLegacyPickup) _coordinator.Changed -= OnCoordinatorChanged;
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

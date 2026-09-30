using FarPoint.Win.Spread;
using FrameWebforCS.providers;
using FrameWebforCS.calculation;
using System.Globalization;

namespace FrameWebforCS.components.result;

public partial class ResultCombineReacComponent : UserControl
{
    private static readonly (string Key, string Title)[] Modes3D =
    [
        ("tx_max", "X方向の支点反力 最大"), ("tx_min", "X方向の支点反力 最小"),
        ("ty_max", "Y方向の支点反力 最大"), ("ty_min", "Y方向の支点反力 最小"),
        ("tz_max", "Z方向の支点反力 最大"), ("tz_min", "Z方向の支点反力 最小"),
        ("mx_max", "X軸回りの回転反力 最大"), ("mx_min", "X軸回りの回転反力 最小"),
        ("my_max", "Y軸回りの回転反力 最大"), ("my_min", "Y軸回りの回転反力 最小"),
        ("mz_max", "Z軸回りの回転反力 最大"), ("mz_min", "Z軸回りの回転反力 最小")
    ];

    private static readonly (string Key, string Title)[] Modes2D =
    [
        ("tx_max", "X方向の支点反力 最大"), ("tx_min", "X方向の支点反力 最小"),
        ("ty_max", "Y方向の支点反力 最大"), ("ty_min", "Y方向の支点反力 最小"),
        ("mz_max", "Z軸回りの回転反力 最大"), ("mz_min", "Z軸回りの回転反力 最小")
    ];

    private readonly ResultCombineReacCoordinator _coordinator = ResultCombineReacCoordinator.Instance;
    private readonly Func<ResultCombineReacSnapshot, CancellationToken, ResultCombineReacOutput> _calculate;
    private Control? _uiDispatcher;
    private ResultCombineReacOutput? _output;
    private ResultCombineReacSnapshot? _pending;
    private CancellationTokenSource? _runningCancellation;
    internal Task? CurrentCalculationTask { get; private set; }
    internal bool IsCalculationRunning => _runningCancellation != null;
    private long _generation;
    private long _publishedRevision = -1;
    private int _materializedSheet = -1;
    private readonly Dictionary<SheetView, ResultModeGroupDataModel> _groups = new();
    private bool _rebuilding;
    private bool _disposed;
    private CalculationDerivedViewRenderer? _canonical;

    public ResultCombineReacComponent() : this(ResultCombineReacAggregator.Calculate) { }

    internal ResultCombineReacComponent(
        Func<ResultCombineReacSnapshot, CancellationToken, ResultCombineReacOutput> calculate)
    {
        _calculate = calculate ?? throw new ArgumentNullException(nameof(calculate));
        InitializeComponent();
        ResultDimensionNotice.Attach(this);
        fpSpread1.EditModeOn += fpSpread1.faSpread_EditModeOn;
        // This handle outlives the view handle, so in-flight work can complete
        // on the UI thread even while WinForms recreates the view handle.
        _uiDispatcher = new Control();
        _ = _uiDispatcher.Handle;
        _canonical = new(CalculationDerivedStage.Combine, CalculationDerivedQuantity.Reaction,
            fpSpread1, statusLabel, Modes3D, Modes2D, ConfigureSheet);
        CalculationResultStore.Instance.Changed += OnCanonicalChanged;

        fpSpread1.ActiveSheetChanged += (_, _) => MaterializeSelectedSheet();
        fpSpread1.EnterCell += OnEnterCell;
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
        // Sidebar option=1 is the route category, not a result sheet index.
        if (fpSpread1.Sheets.Count > 0 && fpSpread1.ActiveSheetIndex < 0)
            fpSpread1.ActiveSheetIndex = 0;
        RefreshFromCoordinator();
    }

    // Keep the existing extension point for callers that subclassed this view.
    public virtual Dictionary<string, object> getCombineReac() =>
        InputDataService.Instance.getCombineReac();

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
            _groups.Clear();
            _materializedSheet = -1;
            return;
        }
        long generation = ++_generation;
        CancelRunning();
        _pending = null;

        if (_coordinator.State != CombineReacState.Valid ||
            _coordinator.Snapshot is not { } snapshot)
        {
            _output = null;
            _publishedRevision = -1;
            ClearDisplay();
            statusLabel.Text = _coordinator.State == CombineReacState.Loading
                ? "組合せ反力を読み込み中..."
                : "組合せ反力を表示できません。結果を読み込んでください。";
            return;
        }

        if (_publishedRevision == snapshot.Revision && _output != null) return;
        _output = null;
        _publishedRevision = -1;
        ClearDisplay();
        statusLabel.Text = "組合せ反力を集計中...";
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
        ResultCombineReacSnapshot snapshot, long generation, CancellationTokenSource cancellation)
    {
        ResultCombineReacOutput? result = null;
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
        ResultCombineReacSnapshot snapshot, long generation, CancellationTokenSource cancellation,
        ResultCombineReacOutput? result, Exception? error)
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
            _coordinator.State == CombineReacState.Valid &&
            _coordinator.Revision == snapshot.Revision)
        {
            if (error != null)
            {
                _output = null;
                ClearDisplay();
                statusLabel.Text = $"組合せ反力の集計に失敗しました: {error.Message}";
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
                    statusLabel.Text = $"組合せ反力の表示に失敗しました: {ex.Message}";
                }
            }
        }
        if (IsHandleCreated && _pending != null) StartPending(_generation);
    }

    private void PublishResult(ResultCombineReacOutput result, int dimension)
    {
        _rebuilding = true;
        try
        {
            fpSpread1.Sheets.Clear();
            _groups.Clear();
            _materializedSheet = -1;
            foreach (CombineReacCaseResult item in result.Cases)
            {
                SheetView sheet = fpSpread1.AddNewSheetView();
                sheet.SheetName = string.IsNullOrWhiteSpace(item.Name)
                    ? item.Id : $"{item.Id} {item.Name}";
                ConfigureSheet(sheet, dimension);
                sheet.RowCount = 0;
            }
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
        if (_canonical?.IsShowing == true) { _canonical.Materialize(); return; }
        if (_rebuilding || _output == null) return;
        int sheetIndex = fpSpread1.ActiveSheetIndex;
        if (sheetIndex < 0 || sheetIndex >= _output.Cases.Count) return;

        if (_materializedSheet >= 0 && _materializedSheet != sheetIndex &&
            _materializedSheet < fpSpread1.Sheets.Count)
        {
            SheetView previous = fpSpread1.Sheets[_materializedSheet];
            ResultModeGroupDataModel.Clear(previous);
            _groups.Remove(previous);
        }

        SheetView sheet = fpSpread1.Sheets[sheetIndex];
        if (_materializedSheet != sheetIndex)
        {
            bool is3D = _coordinator.Snapshot?.Dimension == 3;
            var modes = is3D ? Modes3D : Modes2D;
            int modeColumn = is3D ? 8 : 5;
            sheet.ColumnCount = modeColumn + 1;
            int totalRows = modes.Sum(mode =>
                _output.Cases[sheetIndex].Rows.TryGetValue(mode.Key, out var rows) ? rows.Count : 0);
            sheet.RowCount = totalRows;
            int row = 0;
            for (int modeIndex = 0; modeIndex < modes.Length; modeIndex++)
            {
                if (!_output.Cases[sheetIndex].Rows.TryGetValue(modes[modeIndex].Key, out var rows))
                    continue;
                foreach (CombineReacNodeResult value in rows)
                {
                    sheet.Cells[row, 0].Text = value.Id;
                    sheet.Cells[row, 1].Text = Format(value.Tx);
                    sheet.Cells[row, 2].Text = Format(value.Ty);
                    if (is3D)
                    {
                        sheet.Cells[row, 3].Text = Format(value.Tz);
                        sheet.Cells[row, 4].Text = Format(value.Mx);
                        sheet.Cells[row, 5].Text = Format(value.My);
                        sheet.Cells[row, 6].Text = Format(value.Mz);
                        sheet.Cells[row, 7].Text = value.Case;
                    }
                    else
                    {
                        sheet.Cells[row, 3].Text = Format(value.Mz);
                        sheet.Cells[row, 4].Text = value.Case;
                    }
                    sheet.Cells[row, modeColumn].Text = ResultModeGroupDataModel.Token(modeIndex);
                    row++;
                }
            }
            _groups[sheet] = ResultModeGroupDataModel.Attach(sheet, modeColumn, modes);
            _materializedSheet = sheetIndex;
        }
        PublishViewportPage(sheet, sheet.ActiveRowIndex);
    }

    private void OnEnterCell(object? sender, EnterCellEventArgs e)
    {
        if (_canonical?.IsShowing == true) { _canonical.PublishPage(e.Row); return; }
        if (_rebuilding || _output == null) return;
        PublishViewportPage(fpSpread1.ActiveSheet, e.Row);
    }

    private void PublishViewportPage(SheetView sheet, int displayRow)
    {
        if (!Visible || _output == null) return;
        int index = fpSpread1.ActiveSheetIndex;
        if (index < 0 || index >= _output.Cases.Count) return;
        string? mode = _groups.TryGetValue(sheet, out var grouped)
            ? grouped.GetModeKey(displayRow) : null;
        mode ??= (_coordinator.Snapshot?.Dimension == 3 ? Modes3D : Modes2D)[0].Key;
        FrameWebforCS.three.ThreeResultsService.PublishPage(
            "comb_reac", _output.Cases[index].Id, mode);
    }

    private static string Format(double value) =>
        (Math.Floor(value * 100 + 0.5) / 100)
            .ToString("F2", CultureInfo.InvariantCulture);

    private void ClearDisplay()
    {
        _rebuilding = true;
        try
        {
            fpSpread1.Sheets.Clear();
            _groups.Clear();
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
            header.Cells[0, 1].Text = "支点反力(kN)";
            header.Cells[1, 1].Text = "X方向";
            header.Cells[1, 2].Text = "Y方向";
            header.Cells[1, 3].Text = "Z方向";
            header.Cells[0, 1].ColumnSpan = 3;
            header.Cells[0, 4].Text = "回転反力(kN・m)";
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
            header.Cells[0, 1].Text = "支点反力(kN)";
            header.Cells[1, 1].Text = "X方向";
            header.Cells[1, 2].Text = "Y方向";
            header.Cells[0, 1].ColumnSpan = 2;
            header.Cells[0, 3].Text = "回転反力";
            header.Cells[1, 3].Text = "(kN・m)";
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
        CalculationResultStore.Instance.Changed -= OnCanonicalChanged;
        _coordinator.Changed -= OnCoordinatorChanged;
        CancelRunning();
        _runningCancellation?.Dispose();
        _runningCancellation = null;
        _uiDispatcher?.Dispose();
        _uiDispatcher = null;
        _pending = null;
        fpSpread1.EnterCell -= OnEnterCell;
    }

    private void CancelRunning()
    {
        try { _runningCancellation?.Cancel(); }
        catch (ObjectDisposedException) { }
    }
}

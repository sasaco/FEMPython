using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using FrameWebforCS.calculation;
using System.Collections.Immutable;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FrameWebforCS.components.result;

internal abstract class ResultPickupTableComponent<TSnapshot> : UserControl where TSnapshot : class
{
    private readonly Func<TSnapshot?> _snapshot;
    private readonly Func<long> _revision;
    private readonly Action<EventHandler> _detach;
    private readonly Func<TSnapshot, ImmutableArray<PickupSelection>, CancellationToken, PickupTableOutput> _calculate;
    private readonly (string Key, string Title)[] _modes3D, _modes2D;
    private readonly string[] _columns3D, _columns2D;
    private readonly Control _dispatcher = new();
    private readonly myFpSpread fpSpread1 = new() { Name = "fpSpread1", Dock = DockStyle.Fill };
    private readonly ComboBox modeSelector = new() { Name = "modeSelector", DropDownStyle = ComboBoxStyle.DropDownList,
        Location = new Point(88, 5), Width = 270 };
    private readonly Label statusLabel = new() { Name = "statusLabel", Dock = DockStyle.Bottom,
        Height = 24, Padding = new Padding(8, 2, 8, 2), AutoEllipsis = true };
    private PickupTableOutput? _output;
    private (TSnapshot Snapshot, ImmutableArray<PickupSelection> Pickups, long Revision)? _pending;
    private CancellationTokenSource? _running;
    private long _generation, _publishedRevision = -1;
    private int _materializedSheet = -1;
    private bool _rebuilding, _disposed;
    private readonly CalculationDerivedViewRenderer _canonical;

    protected ResultPickupTableComponent(Func<TSnapshot?> snapshot, Func<long> revision,
        Action<EventHandler> attach, Action<EventHandler> detach,
        Func<TSnapshot, ImmutableArray<PickupSelection>, CancellationToken, PickupTableOutput> calculate,
        (string Key, string Title)[] modes3D, (string Key, string Title)[] modes2D,
        string[] columns3D, string[] columns2D)
    {
        _snapshot = snapshot;
        _revision = revision;
        _detach = detach;
        _calculate = calculate;
        _modes3D = modes3D;
        _modes2D = modes2D;
        _columns3D = columns3D;
        _columns2D = columns2D;
        AutoScaleMode = AutoScaleMode.Dpi;
        Size = new Size(800, 566);
        var panel = new Panel { Dock = DockStyle.Top, Height = 34 };
        panel.Controls.Add(new Label { Text = "着目項目", AutoSize = true, Location = new Point(10, 9) });
        panel.Controls.Add(modeSelector);
        Controls.Add(fpSpread1);
        Controls.Add(statusLabel);
        Controls.Add(panel);
        ResultDimensionNotice.Attach(this);
        fpSpread1.EditModeOn += fpSpread1.faSpread_EditModeOn;
        modeSelector.SelectedIndexChanged += (_, _) => MaterializeSelectedSheet();
        fpSpread1.ActiveSheetChanged += (_, _) => MaterializeSelectedSheet();
        // AppRoutingModule calls setActiveSheet before showing the floating form.
        VisibleChanged += (_, _) => { if (Visible) MaterializeSelectedSheet(); };
        _ = _dispatcher.Handle;
        _canonical = new(CalculationDerivedStage.Pickup,
            this is ResultPickupFsecComponent ? CalculationDerivedQuantity.SectionForce :
            this is ResultPickupReacComponent ? CalculationDerivedQuantity.Reaction :
                CalculationDerivedQuantity.Displacement,
            fpSpread1, modeSelector, statusLabel, modes3D, modes2D);
        CalculationResultStore.Instance.Changed += OnCanonicalChanged;
        attach(OnSourceChanged);
        HandleCreated += (_, _) => Refresh();
        HandleDestroyed += (_, _) => CancelRunning();
        Refresh();
    }

    // Sidebar option=2 denotes this route, not the third result sheet.
    public void setActiveSheet(int index) => Refresh();
    internal Task? CurrentCalculationTask { get; private set; }
    internal bool IsCalculationRunning => _running != null;

    private void OnSourceChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;
        if (_dispatcher.InvokeRequired)
        {
            try { _dispatcher.BeginInvoke((System.Action)Refresh); }
            catch (InvalidOperationException) { }
        }
        else Refresh();
    }

    private void OnCanonicalChanged(object? sender, EventArgs e) => OnSourceChanged(sender, e);

    private void Refresh()
    {
        if (_disposed) return;
        if (_canonical.ShowCurrent())
        {
            ++_generation;
            CancelRunning();
            _pending = null;
            _output = null;
            return;
        }
        ++_generation;
        CancelRunning();
        _pending = null;
        long revision = _revision();
        TSnapshot? snapshot = _snapshot();
        if (revision != _revision())
        {
            // A source changed while it was being captured. Its Changed event
            // will request a fresh snapshot on this UI thread.
            _output = null;
            _publishedRevision = -1;
            ClearDisplay();
            return;
        }
        if (snapshot is null)
        {
            _output = null;
            _publishedRevision = -1;
            ClearDisplay();
            statusLabel.Text = "ピックアップ結果を表示できません。結果を読み込んでください。";
            return;
        }
        if (_output != null && _publishedRevision == revision) return;
        try
        {
            ImmutableArray<PickupSelection> pickups = CapturePickups();
            _output = null;
            _publishedRevision = -1;
            ClearDisplay();
            statusLabel.Text = "ピックアップ結果を集計中...";
            _pending = (snapshot, pickups, revision);
            if (IsHandleCreated && _running is null) StartPending();
        }
        catch (Exception ex)
        {
            _output = null;
            _publishedRevision = -1;
            ClearDisplay();
            statusLabel.Text = $"ピックアップの入力が不正です: {ex.Message}";
        }
    }

    private static ImmutableArray<PickupSelection> CapturePickups()
    {
        var rows = InputCombineService.Instance.PickupRows;
        if (rows.Count > ResultPickupAggregator.MaxPickups)
            throw new InvalidOperationException("PICKUP count exceeds the supported limit.");
        return rows.Values.Select(row => new PickupSelection(row.Id, row.name,
            row.Coefficients.Values.Where(id => id > 0).ToImmutableArray())).ToImmutableArray();
    }

    private void StartPending()
    {
        if (_disposed || !IsHandleCreated || _running != null || _pending is not { } pending) return;
        _pending = null;
        var cancellation = new CancellationTokenSource();
        _running = cancellation;
        CurrentCalculationTask = CalculateAsync(pending, _generation, cancellation);
    }

    private async Task CalculateAsync(
        (TSnapshot Snapshot, ImmutableArray<PickupSelection> Pickups, long Revision) pending,
        long generation, CancellationTokenSource cancellation)
    {
        PickupTableOutput? result = null;
        Exception? error = null;
        try
        {
            result = await Task.Run(() => _calculate(pending.Snapshot, pending.Pickups,
                cancellation.Token), cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex) { error = ex; }
        if (_disposed) { cancellation.Dispose(); return; }
        try
        {
            _dispatcher.BeginInvoke((System.Action)(() => Complete(pending.Revision,
                generation, cancellation, result, error)));
        }
        catch (InvalidOperationException) { cancellation.Dispose(); }
    }

    private void Complete(long revision, long generation, CancellationTokenSource cancellation,
        PickupTableOutput? result, Exception? error)
    {
        if (!ReferenceEquals(_running, cancellation)) { cancellation.Dispose(); return; }
        _running = null;
        cancellation.Dispose();
        if (_disposed) return;
        if (IsHandleCreated && generation == _generation && revision == _revision() &&
            _snapshot() != null)
        {
            if (error != null)
            {
                _output = null;
                ClearDisplay();
                statusLabel.Text = $"ピックアップの集計に失敗しました: {error.Message}";
            }
            else if (result != null)
            {
                try { _output = result; Publish(result); _publishedRevision = revision; }
                catch (Exception ex)
                {
                    _output = null;
                    _publishedRevision = -1;
                    ClearDisplay();
                    statusLabel.Text = $"ピックアップの表示に失敗しました: {ex.Message}";
                }
            }
        }
        if (_pending != null) StartPending();
    }

    private void Publish(PickupTableOutput output)
    {
        _rebuilding = true;
        try
        {
            fpSpread1.Sheets.Clear();
            modeSelector.Items.Clear();
            _materializedSheet = -1;
            foreach (var mode in output.Dimension == 3 ? _modes3D : _modes2D)
                modeSelector.Items.Add(new ModeChoice(mode.Key, mode.Title));
            foreach (PickupTableCase item in output.Cases)
            {
                SheetView sheet = fpSpread1.AddNewSheetView();
                sheet.SheetName = string.IsNullOrWhiteSpace(item.Name) ? item.Id : $"{item.Id} {item.Name}";
                ConfigureSheet(sheet, output.Dimension == 3 ? _columns3D : _columns2D);
                sheet.RowCount = 0;
            }
            if (modeSelector.Items.Count > 0) modeSelector.SelectedIndex = 0;
            if (fpSpread1.Sheets.Count > 0) fpSpread1.ActiveSheetIndex = 0;
            Width = (output.Dimension == 3 ? _columns3D : _columns2D).Length * 88 + 100;
        }
        finally { _rebuilding = false; }
        statusLabel.Text = output.Cases.Count == 0 ? "ピックアップ結果がありません。" :
            $"{output.Cases.Count} 件のピックアップ結果";
        MaterializeSelectedSheet();
    }

    private static void ConfigureSheet(SheetView sheet, string[] headers)
    {
        sheet.ColumnCount = headers.Length;
        sheet.ColumnHeader.RowCount = 2;
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.ColumnHeader.Cells[0, i].Text = headers[i];
            sheet.ColumnHeader.Cells[1, i].Text = " ";
            sheet.Columns[i].Width = i == headers.Length - 1 ? 200 : i == 0 ? 50 : 80;
            sheet.Columns[i].CellType = new FarPoint.Win.Spread.CellType.TextCellType();
        }
        sheet.Protect = true;
    }

    private void MaterializeSelectedSheet()
    {
        if (_canonical.IsShowing) { _canonical.Materialize(); return; }
        if (_rebuilding || _output is null || modeSelector.SelectedItem is not ModeChoice mode) return;
        int index = fpSpread1.ActiveSheetIndex;
        if (index < 0 || index >= _output.Cases.Count) return;
        if (_materializedSheet >= 0 && _materializedSheet != index &&
            _materializedSheet < fpSpread1.Sheets.Count)
            fpSpread1.Sheets[_materializedSheet].RowCount = 0;
        SheetView sheet = fpSpread1.Sheets[index];
        IReadOnlyList<string[]> rows = _output.Cases[index].Rows.TryGetValue(mode.Key, out var value)
            ? value : Array.Empty<string[]>();
        sheet.RowCount = rows.Count;
        for (int row = 0; row < rows.Count; row++)
            for (int col = 0; col < sheet.ColumnCount && col < rows[row].Length; col++)
                sheet.Cells[row, col].Text = rows[row][col];
        _materializedSheet = index;
        PublishViewportPage(mode.Key, index);
    }

    private void PublishViewportPage(string component, int index)
    {
        if (_canonical.IsShowing) { _canonical.PublishPage(); return; }
        if (!Visible || _output is null || index < 0 || index >= _output.Cases.Count) return;
        var selected = _output.Cases[index];
        string viewportMode = this switch
        {
            ResultPickupFsecComponent => "pick_fsec",
            ResultPickupReacComponent => "pik_reac",
            _ => "pik_disg"
        };
        if (this is ResultPickupFsecComponent)
        {
            var samples = new List<FrameWebforCS.three.SectionForceSample>();
            if (selected.Rows.TryGetValue(component, out var rows))
                foreach (var row in rows)
                {
                    if (row.Length < 6 ||
                        !int.TryParse(row[0].Replace("member", "", StringComparison.Ordinal),
                            NumberStyles.None, CultureInfo.InvariantCulture, out int memberId) || memberId <= 0 ||
                        !float.TryParse(row[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float location))
                        continue;
                    int field = component[..2] switch
                    {
                        "fx" => 3, "fy" => 4, "fz" => 5,
                        "mx" => 6, "my" => 7,
                        "mz" => _output.Dimension == 3 ? 8 : 5,
                        _ => -1
                    };
                    if (field >= 0 && field < row.Length &&
                        float.TryParse(row[field], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                        samples.Add(new(memberId, location, value));
                }
            FrameWebforCS.three.ThreeResultsService.PublishDerivedFsec(viewportMode,
                new Dictionary<string, IReadOnlyList<FrameWebforCS.three.SectionForceSample>>
                { [selected.Id] = samples }, _revision());
        }
        FrameWebforCS.three.ThreeResultsService.PublishPage(viewportMode, selected.Id, component);
    }

    private void ClearDisplay()
    {
        _rebuilding = true;
        try { fpSpread1.Sheets.Clear(); modeSelector.Items.Clear(); _materializedSheet = -1; }
        finally { _rebuilding = false; }
        if (Visible)
        {
            string mode = this switch
            {
                ResultPickupFsecComponent => "pick_fsec",
                ResultPickupReacComponent => "pik_reac",
                _ => "pik_disg"
            };
            if (this is ResultPickupFsecComponent)
                FrameWebforCS.three.ThreeResultsService.PublishDerivedFsec(mode,
                    new Dictionary<string, IReadOnlyList<FrameWebforCS.three.SectionForceSample>>(), _revision());
            FrameWebforCS.three.ThreeResultsService.PublishPage(mode, null);
        }
    }

    private void CancelRunning()
    {
        try { _running?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            CalculationResultStore.Instance.Changed -= OnCanonicalChanged;
            _detach(OnSourceChanged);
            CancelRunning();
            _running?.Dispose();
            _running = null;
            _pending = null;
            _dispatcher.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed record ModeChoice(string Key, string Title)
    {
        public override string ToString() => Title;
    }
}

internal sealed class ResultPickupDisgComponent : ResultPickupTableComponent<ResultCombineDisgSnapshot>
{
    public ResultPickupDisgComponent() : base(
        () => ResultCombineDisgCoordinator.Instance.Snapshot,
        () => ResultCombineDisgCoordinator.Instance.Revision,
        handler => ResultCombineDisgCoordinator.Instance.Changed += handler,
        handler => ResultCombineDisgCoordinator.Instance.Changed -= handler,
        ResultPickupDisgAggregator.Calculate,
        ResultPickupAggregator.Disg3D, ResultPickupAggregator.Disg2D,
        ["節点 No", "X方向 移動量(mm)", "Y方向 移動量(mm)", "Z方向 移動量(mm)",
            "X軸回り 回転(‰rad)", "Y軸回り 回転(‰rad)", "Z軸回り 回転(‰rad)", "組み合わせ"],
        ["節点 No", "X方向 移動量(mm)", "Y方向 移動量(mm)",
            "Z軸回り 回転(‰rad)", "組み合わせ"])
    { }
}

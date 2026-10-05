using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.calculation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace FrameWebforCS
{
    public partial class MenuComponent : UserControl
    {
        private InputDataService _input = InputDataService.Instance;
        private PythonCalculationRuntime? _calculationRuntime;
        private CancellationTokenSource? _calculationCancellation;
        private bool _calculationRunning;
        private bool _closing;
        private Task? _closeCalculationTask;

        public MenuComponent()
        {
            InitializeComponent();
            _input.DimensionChanged += OnDimensionChanged;
            _input.FileReplaced += OnFileReplaced;
            計算ToolStripMenuItem.Click += CalculationToolStripMenuItem_Click;
            印刷ToolStripMenuItem.Click += PrintToolStripMenuItem_Click;
            Disposed += (_, _) =>
            {
                _closing = true;
                _calculationCancellation?.Cancel();
                _input.DimensionChanged -= OnDimensionChanged;
                _input.FileReplaced -= OnFileReplaced;
            };
            SyncDimensionMenu();
        }

        private void Dimension2DToolStripMenuItem_Click(object sender, EventArgs e) => SelectDimension(2);

        private void Dimension3DToolStripMenuItem_Click(object sender, EventArgs e) => SelectDimension(3);

        private void SelectDimension(int dimension)
        {
            if (_input.dimension == dimension)
            {
                SyncDimensionMenu();
                return;
            }

            try
            {
                AppRoutingModule.Instance.PrepareDimensionChange();
                _input.SetDimension(dimension);
            }
            catch (Exception ex)
            {
                SyncDimensionMenu();
                MessageBox.Show(this, "解析次元を変更できませんでした。\n" + ex.Message,
                    "解析次元の変更エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDimensionChanged(int _) => SyncDimensionMenu();

        private void PrintToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_closing || FindForm() is not AppComponent owner) return;
            owner.ShowPrintDialog();
        }

        private void OnFileReplaced(long _)
        {
            _calculationCancellation?.Cancel();
            SyncDimensionMenu();
        }

        internal Task CloseCalculationAsync() => _closeCalculationTask ??= CloseCalculationCoreAsync();

        private async Task CloseCalculationCoreAsync()
        {
            _closing = true;
            _calculationCancellation?.Cancel();
            if (_calculationRuntime is not null)
                await _calculationRuntime.DisposeAsync();
        }

        private async void CalculationToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_calculationRunning)
            {
                _calculationCancellation?.Cancel();
                計算ToolStripMenuItem.Text = "計算中止待ち...";
                return;
            }

            if (_closing) return;
            string snapshotJson;
            CalculationDerivedInputSnapshot derivedInput;
            try
            {
                snapshotJson = _input.CaptureCalculationSnapshotJson();
                derivedInput = CalculationDerivedInputSnapshot.Capture(
                    InputCombineService.Instance.DefineRows,
                    InputCombineService.Instance.CombineRows,
                    InputCombineService.Instance.PickupRows);
            }
            catch (Exception error)
            {
                MessageBox.Show(this, "計算入力を確認してください。\n" + error.Message,
                    "計算入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            long revision = _input.CalculationInputRevision;
            int dimension = _input.dimension;
            _calculationRunning = true;
            _calculationCancellation = new CancellationTokenSource();
            CancellationToken cancellation = _calculationCancellation.Token;
            計算ToolStripMenuItem.Text = "入力準備中 (クリックで中止)";
            bool accepted = false;
            try
            {
                CalculationRequest request = await Task.Run(() =>
                    CalculationRequestBuilder.FromSavedJson(snapshotJson), cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (_closing || IsDisposed || _input.CalculationInputRevision != revision) return;
                // A valid calculation starts a new result generation. A later failure must not
                // leave either visible legacy results or stale legacy result fields in saved JSON.
                accepted = true;
                _input.ClearLegacyResultsForCalculation();
                CalculationResultStore.Instance.Clear();
                計算ToolStripMenuItem.Text = "計算中 (クリックで中止)";
                _calculationRuntime ??= new PythonCalculationRuntime();
                string json = await _calculationRuntime.CalculateAsync(request.Json, cancellation);
                CalculationResultPresentation prepared = await Task.Run(() =>
                {
                    AnalysisResultSet resultSet = AnalysisResultSetJson.Deserialize(json);
                    CalculationDerivedPresentation derived = CalculationDerivedPresenter.Build(
                        resultSet, dimension, derivedInput);
                    return new CalculationResultPresentation(resultSet, derived, dimension);
                }, cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (_closing || IsDisposed || _input.CalculationInputRevision != revision)
                    return;
                CalculationResultStore.Instance.Commit(prepared);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // The in-process solve can finish later; its result is discarded by the runtime.
            }
            catch (Exception error)
            {
                if (!_closing && !IsDisposed)
                    MessageBox.Show(this,
                        (accepted ? "計算できませんでした。\n" : "計算入力を確認してください。\n") + error.Message,
                        accepted ? "計算エラー" : "計算入力エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Cancellation cannot interrupt an active native FEM solve. Keep the command
                // occupied until its Python worker has actually released the interpreter.
                if (_calculationRuntime is not null)
                    await _calculationRuntime.WaitForIdleAsync();
                _calculationCancellation?.Dispose();
                _calculationCancellation = null;
                _calculationRunning = false;
                if (!_closing && !IsDisposed) 計算ToolStripMenuItem.Text = "計算";
            }
        }

        private void SyncDimensionMenu()
        {
            bool is3D = _input.dimension == 3;
            toolStripMenuItem1.Text = is3D ? "3D" : "2D";
            dToolStripMenuItem2.Checked = !is3D;
            dToolStripMenuItem3.Checked = is3D;
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "jsonファイル(*.json)|*.json|すべてのファイル(*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.DefaultExt = "json";
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.Title = "保存先を選択してください";
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                var jsonData = _input.GetSaveJson(); 
                string json = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(saveFileDialog1.FileName, json, new UTF8Encoding(false));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                MessageBox.Show(
                    "ファイルを保存できませんでした。\n" + ex.Message,
                    "ファイル保存エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "対応ファイル(*.json;*.frd;*.ndt)|*.json;*.frd;*.ndt|すべてのファイル(*.*)|*.*";
            openFileDialog1.Title = "開くファイルを選択してください";
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            //ダイアログを表示する
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //OKボタンがクリックされたとき、選択されたファイル名を表示する
                Console.WriteLine(openFileDialog1.FileName);
                try
                {
                    using JsonDocument jsonData = InputFileLoader.Open(openFileDialog1.FileName);

                    _input.JsonDataOpen(jsonData.RootElement);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "ファイルを読み込めませんでした。\n" + ex.Message,
                        "ファイル読み込みエラー",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }


        }

        private void renewToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}

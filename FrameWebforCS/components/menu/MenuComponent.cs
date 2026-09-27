using FrameWebforCS.components.input;
using FrameWebforCS.providers;
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

        public MenuComponent()
        {
            InitializeComponent();
            _input.DimensionChanged += OnDimensionChanged;
            _input.FileReplaced += OnFileReplaced;
            Disposed += (_, _) =>
            {
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

        private void OnFileReplaced(long _) => SyncDimensionMenu();

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
            openFileDialog1.Filter = "jsonファイル(*.json;*.frd;*.ndt)|*.json;*.frd;*.ndt|すべてのファイル(*.*)|*.*";
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
                    using FileStream jsonFile = File.OpenRead(openFileDialog1.FileName);
                    using JsonDocument jsonData = JsonDocument.Parse(jsonFile);

                    _input.JsonDataOpen(jsonData.RootElement);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
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

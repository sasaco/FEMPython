using Convert_Manager.FrameG;
using Convert_Test;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GConvert_Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // memo: Shift-JISを扱うためのおまじない
        }

        private void ConvertFRD(string[] FileNames)
        {
            var ConvertedList = new List<resultMessage>();

            foreach(var fileName in FileNames)
            {
                // 拡張子を変更する
                string newName = Path.ChangeExtension(fileName, ".json");

                // 読み込みたいテキストを開く
                using (StreamReader st = new StreamReader(fileName))
                {
                    var conv = new ConvertManager(st.BaseStream);
                    SaveFile(newName, conv.getJsonString());

                    var rM = new resultMessage();
                    rM.filename = newName;

                    if (conv._member.message.Length > 0)
                        rM.message.Add(conv._member.message);

                    if (conv._joint.message.Length > 0)
                        rM.message.Add(conv._joint.message);

                    if (conv._gouiki.message.Length > 0)
                        rM.message.Add(conv._gouiki.message);

                    if (conv._load.message.Length > 0)
                        rM.message.Add(conv._load.message);

                    ConvertedList.Add(rM);
                }
            }

            // 終了ダイアログ
            var f2 = new Form2(ConvertedList);

            f2.ShowDialog();
            f2.Dispose();
        }

        /// <summary>
        /// FrameWeb の 2D JSON を FrameG の .frd に変換する。元の .frd を上書きしないよう、
        /// 同じ名前の .frd があるときは「名前_FrameWeb.frd」に書き出す。
        /// </summary>
        private void ExportFRD(string[] FileNames)
        {
            var ConvertedList = new List<resultMessage>();

            foreach (var fileName in FileNames)
            {
                var rM = new resultMessage();
                rM.filename = fileName;

                var result = FrdExporter.Export(File.ReadAllText(fileName),
                    new FrdExportOptions { Title = Path.GetFileNameWithoutExtension(fileName) });
                foreach (var diagnostic in result.Diagnostics)
                    rM.message.Add((diagnostic.Severity == FrdDiagnosticSeverity.Error ? "エラー " : "警告 ") + diagnostic);

                if (result.Succeeded)
                {
                    string newName = Path.ChangeExtension(fileName, ".frd");
                    if (File.Exists(newName))
                        newName = Path.Combine(Path.GetDirectoryName(fileName),
                            Path.GetFileNameWithoutExtension(fileName) + "_FrameWeb.frd");
                    File.WriteAllBytes(newName, result.Frd);
                    rM.filename = newName;
                }
                else
                {
                    rM.failed = true;
                }

                ConvertedList.Add(rM);
            }

            // 終了ダイアログ
            var f2 = new Form2(ConvertedList);

            f2.ShowDialog();
            f2.Dispose();
        }

        /// <summary>ドロップされたファイルを拡張子で振り分けて変換する。</summary>
        private void ConvertFiles(string[] FileNames)
        {
            var frd = FileNames.Where(name => Path.GetExtension(name).Equals(".frd", StringComparison.OrdinalIgnoreCase)).ToArray();
            var json = FileNames.Where(name => Path.GetExtension(name).Equals(".json", StringComparison.OrdinalIgnoreCase)).ToArray();
            var others = FileNames.Except(frd).Except(json).ToArray();

            if (frd.Length > 0)
                ConvertFRD(frd);
            if (json.Length > 0)
                ExportFRD(json);
            if (others.Length > 0)
            {
                var f2 = new Form2(others.Select(name => new resultMessage
                {
                    filename = name,
                    failed = true,
                    message = { "変換できるのは .frd と .json だけです" }
                }).ToList());
                f2.ShowDialog();
                f2.Dispose();
            }
        }

        private void SaveFile(string filename, string contents)
        {
            //'書き込むファイルが既に存在している場合は、上書きする
            using (var sw = new System.IO.StreamWriter(filename, false, System.Text.Encoding.UTF8))
            {
                // resultの内容を書き込む
                sw.Write(contents);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                ConvertFRD(openFileDialog1.FileNames);
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog2.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                ExportFRD(openFileDialog2.FileNames);
            }
        }

        //panel1のDragEnterイベントハンドラ
        private void panel1_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            //コントロール内にドラッグされたとき実行される
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                //ドラッグされたデータ形式を調べ、ファイルのときはコピーとする
                e.Effect = DragDropEffects.Copy;
            else
                //ファイル以外は受け付けない
                e.Effect = DragDropEffects.None;
        }

        //panel1のDragDropイベントハンドラ
        private void panel1_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            //コントロール内にドロップされたとき実行される
            //ドロップされたすべてのファイル名を取得する
            string[] fileName =
                (string[])e.Data.GetData(DataFormats.FileDrop, false);
            //拡張子で .frd → .json と .json → .frd を振り分ける
            ConvertFiles(fileName);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}

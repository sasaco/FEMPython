using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Convert_Test
{
    public class resultMessage
    {
        public string filename = null;
        public List<string> message = new List<string>();
        public bool failed = false; // 変換できず、ファイルを書き出していない
    }

    public partial class Form2 : Form
    {
        public Form2(List<resultMessage> value)
        {
            InitializeComponent();

            // 行の追加
            DataGridViewImageColumn column = new DataGridViewImageColumn();
            column.Name = "state";
            column.HeaderText = "";
            column.ValuesAreIcons = true;
            column.ImageLayout = DataGridViewImageCellLayout.Normal;//.Stretch;//.Zoom; //イメージを縦横の比率を維持して拡大、縮小表示する
            this.dataGridView1.Columns.Add(column);

            this.dataGridView1.Columns.Add("filename", "ファイル名");

            int msgCount = 0;
            foreach (var v in value)
                msgCount = Math.Max(msgCount, v.message.Count);

            for (var i = 0; i < msgCount; i++)
                this.dataGridView1.Columns.Add("info" + (i + 1), "");

            // 列の追加
            this.dataGridView1.Rows.Add(value.Count);

            var columnWidth = new List<int>() { 50, 0 };
            // 内容を
            for (var r = 0; r < value.Count; r++)
            {
                var v = value[r];

                // アイコン
                if (v.failed)
                    this.dataGridView1[0, r].Value = SystemIcons.Error;
                else if (0 < v.message.Count)
                    this.dataGridView1[0, r].Value = SystemIcons.Warning;
                // ファイル名
                this.dataGridView1[1, r].Value = v.filename;
                columnWidth[1] = Math.Max(columnWidth[1], v.filename.Length * 9);

                // エラーメッセージ
                for (var c = 0; c < v.message.Count; c++)
                {
                    this.dataGridView1[2 + c, r].Value = v.message[c];
                    if (columnWidth.Count < 3 + c)
                        columnWidth.Add(0);
                    columnWidth[2 + c] = Math.Max(columnWidth[2 + c], v.message[c].Length * 9);
                }
            }

            // サイズを決定する
            var Height = (this.dataGridView1.Rows.Count + 1) * 12 + 100;
            this.dataGridView1.Height = Height;

            var Width = 0;
            for (var i=0; i<columnWidth.Count; i++)
            {
                var w = columnWidth[i];
                this.dataGridView1.Columns[i].Width = w;
                Width += w;
            }
            this.dataGridView1.Width = Width;


            //ヘッダーとすべてのセルの内容に合わせて、列の幅を自動調整する
            this.dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            //ヘッダーとすべてのセルの内容に合わせて、行の高さを自動調整する
            //this.dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }


        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}

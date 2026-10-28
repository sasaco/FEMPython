using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    public class FixNode
    {
        public string n;    // 節点番号
        public double tx;
        public double ty;
        public double tz;
        public double rx;
        public double ry;
        public double rz;
    }

    internal class InputFixNode : PrintableBaseB
    {
        public const string KEY = "fix_node";

        private Dictionary<int, List<FixNode>> fixnodes = new Dictionary<int, List<FixNode>>();

        public InputFixNode(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            // データを取得する．
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();

            // データを抽出する
            for (var i = 0; i < target.Count; i++)
            {
                var key = dataManager.parseInt(target.ElementAt(i).Key);  // タイプ番号
                JArray FixN = JArray.FromObject(target.ElementAt(i).Value);

                var _fixnode = new List<FixNode>();

                for (int j = 0; j < FixN.Count; j++)
                {
                    JToken item = FixN[j];

                    var fn = new FixNode();

                    fn.n = dataManager.toString(item["n"]).Trim(); // 節点番号の後の空白を除去
                    fn.tx = dataManager.parseDouble(item["tx"]);
                    fn.ty = dataManager.parseDouble(item["ty"]);
                    fn.tz = dataManager.parseDouble(item["tz"]);
                    fn.rx = dataManager.parseDouble(item["rx"]);
                    fn.ry = dataManager.parseDouble(item["ry"]);
                    fn.rz = dataManager.parseDouble(item["rz"]);

                    _fixnode.Add(fn);

                }
                this.fixnodes.Add(key, _fixnode);
            }
        }


        #region 印刷処理
        // タイトル
        private string title;
        // 2次元か3次元か
        private int dimension;
        // テーブル
        private Table myTable;

        /// <summary>
        /// 印刷前の初期化処理
        /// </summary>
        private void printInit(PdfDocument mc, PrintData data)
        {
            this.dimension = data.dimension;

            if (this.dimension == 3)
            {   // 3次元

                //テーブルの作成
                this.myTable = new Table(4, 7);

                // テーブルの幅
                this.myTable.ColWidth[0] = 30.0; // 格点No
                this.myTable.ColWidth[1] = 75.0;
                this.myTable.ColWidth[2] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[3] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[4] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[5] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[6] = this.myTable.ColWidth[1];



                // 表題
                this.myTable[3, 1] = "(kN/m)";
                this.myTable[3, 2] = "(kN/m)";
                this.myTable[3, 3] = "(kN/m)";
                this.myTable[3, 4] = "(kN・m/rad)";
                this.myTable[3, 5] = "(kN・m/rad)";
                this.myTable[3, 6] = "(kN・m/rad)";

                switch (data.language)
                {
                    case "en":
                        this.title = "Support DATA";
                        this.myTable[1, 0] = "Node";
                        this.myTable[2, 0] = "No";
                        this.myTable[2, 1] = "TX　";
                        this.myTable[1, 2] = "Displacement Restraint";
                        this.myTable[2, 2] = "TY　";
                        this.myTable[2, 3] = "TZ　";

                        this.myTable[2, 4] = "MX　";
                        this.myTable[1, 5] = "Rotational Restraint";
                        this.myTable[2, 5] = "MY　";
                        this.myTable[2, 6] = "MZ　";
                        break;

                    case "cn":
                        this.title = "支点";
                        this.myTable[1, 0] = "节点";
                        this.myTable[2, 0] = "编码　";
                        this.myTable[2, 1] = "X方向　";
                        this.myTable[1, 2] = "位移约束";
                        this.myTable[2, 2] = "Y方向　";
                        this.myTable[2, 3] = "Z方向　";

                        this.myTable[2, 4] = "围绕X轴　";
                        this.myTable[1, 5] = "旋转约束";
                        this.myTable[2, 5] = "围绕Y轴　";
                        this.myTable[2, 6] = "围绕Z轴　";
                        break;

                    default:
                        this.title = "支点データ";
                        this.myTable[1, 0] = "格点";
                        this.myTable[2, 0] = "No";
                        this.myTable[2, 1] = "X方向　";
                        this.myTable[1, 2] = "変位拘束";
                        this.myTable[2, 2] = "Y方向　";
                        this.myTable[2, 3] = "Z方向　";

                        this.myTable[2, 4] = "X軸回り　";
                        this.myTable[1, 5] = "回転拘束";
                        this.myTable[2, 5] = "Y軸回り　";
                        this.myTable[2, 6] = "Z軸回り　";
                        break;
                }
                this.myTable.AlignX[1, 0] = "R";
                this.myTable.AlignX[2, 0] = "R";
                this.myTable.AlignX[2, 1] = "R";
                this.myTable.AlignX[2, 2] = "R";
                this.myTable.AlignX[2, 3] = "R";
                this.myTable.AlignX[2, 4] = "R";
                this.myTable.AlignX[2, 5] = "R";
                this.myTable.AlignX[2, 6] = "R";
                this.myTable.AlignX[3, 1] = "R";
                this.myTable.AlignX[3, 2] = "R";
                this.myTable.AlignX[3, 3] = "R";
                this.myTable.AlignX[3, 4] = "R";
                this.myTable.AlignX[3, 5] = "R";
                this.myTable.AlignX[3, 6] = "R";


            }
            else
            {   // 2次元

                //テーブルの作成
                this.myTable = new Table(3, 8);

                // テーブルの幅
                this.myTable.ColWidth[0] = 30.0; // 格点No
                this.myTable.ColWidth[1] = 70.0;
                this.myTable.ColWidth[2] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[3] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[4] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[5] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[6] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[7] = this.myTable.ColWidth[1];

                // 表題
                this.myTable[2, 1] = "(kN/m)";
                this.myTable[2, 2] = "(kN/m)";
                this.myTable[2, 3] = "(kN・m/rad)";
                switch (data.language)
                {
                    case "en":
                        this.title = "Support Data";
                        this.myTable[1, 0] = "Node";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "TX";
                        this.myTable[1, 2] = "TY";
                        this.myTable[1, 3] = "MZ";
                        break;

                    case "cn":
                        this.title = "支点";
                        this.myTable[1, 0] = "节点";
                        this.myTable[2, 0] = "编码";
                        this.myTable[1, 1] = "TX";
                        this.myTable[1, 2] = "TY";
                        this.myTable[1, 3] = "MZ";
                        break;

                    default:
                        this.title = "支点データ";
                        this.myTable[1, 0] = "節点";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "TX";
                        this.myTable[1, 2] = "TY";
                        this.myTable[1, 3] = "MZ";
                        break;
                }
                this.myTable[1, 4] = this.myTable[1, 0];
                this.myTable[2, 4] = this.myTable[2, 0];
                this.myTable[1, 5] = this.myTable[1, 1];
                this.myTable[2, 5] = this.myTable[2, 1];
                this.myTable[1, 6] = this.myTable[1, 2];
                this.myTable[2, 6] = this.myTable[2, 2];
                this.myTable[1, 7] = this.myTable[1, 3];
                this.myTable[2, 7] = this.myTable[2, 3];

                this.myTable.AlignX[1, 0] = "R";
                this.myTable.AlignX[2, 0] = "R";
                this.myTable.AlignX[1, 4] = "R";
                this.myTable.AlignX[2, 4] = "R";

                this.myTable.AlignX[1, 1] = "R";
                this.myTable.AlignX[1, 2] = "R";
                this.myTable.AlignX[1, 3] = "R";
                this.myTable.AlignX[1, 4] = "R";
                this.myTable.AlignX[1, 5] = "R";
                this.myTable.AlignX[1, 6] = "R";
                this.myTable.AlignX[1, 7] = "R";
                this.myTable.AlignX[2, 1] = "R";
                this.myTable.AlignX[2, 2] = "R";
                this.myTable.AlignX[2, 3] = "R";
                this.myTable.AlignX[2, 4] = "R";
                this.myTable.AlignX[2, 5] = "R";
                this.myTable.AlignX[2, 6] = "R";
                this.myTable.AlignX[2, 7] = "R";
            }
        }


        /// <summary>
        /// 1ページに入れるコンテンツを集計する 3次元の場合
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents3D(List<FixNode> target)
        {
            int r = this.myTable.Rows;
            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable.Clone();
            table.ReDim(row: r + rows);

            for (var i = 0; i < rows; i++)
            {
                FixNode item = target[i];

                table[r, 0] = printManager.toString(item.n);
                table.AlignX[r, 0] = "R";

                table[r, 1] = printManager.toString(item.tx);
                table.AlignX[r, 1] = "R";

                table[r, 2] = printManager.toString(item.ty);
                table.AlignX[r, 2] = "R";

                table[r, 3] = printManager.toString(item.tz);
                table.AlignX[r, 3] = "R";

                table[r, 4] = printManager.toString(item.rx);
                table.AlignX[r, 4] = "R";

                table[r, 5] = printManager.toString(item.ry);
                table.AlignX[r, 5] = "R";

                table[r, 6] = printManager.toString(item.rz);
                table.AlignX[r, 6] = "R";

                r++;
            }

            table.RowHeight[4] = printManager.LineSpacing2; // 表題と body の間

            return table;
        }

        /// <summary>
        /// 1ページに入れるコンテンツを集計する 2次元の場合
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents2D(List<FixNode> item1, List<FixNode> item2)
        {
            int header_rows = this.myTable.Rows;
            int rows = Math.Max(item1.Count, item2.Count);

            // 行コンテンツを生成
            var table = this.myTable.Clone();
            table.ReDim(row: header_rows + rows);

            // 表の左側
            int r = header_rows;
            for (var i = 0; i < item1.Count; i++)
            {
                FixNode item = item1[i];

                table[r, 0] = printManager.toString(item.n);
                table.AlignX[r, 0] = "R";

                table[r, 1] = printManager.toString(item.tx);
                table.AlignX[r, 1] = "R";

                table[r, 2] = printManager.toString(item.ty);
                table.AlignX[r, 2] = "R";

                table[r, 3] = printManager.toString(item.rz);
                table.AlignX[r, 3] = "R";
                r++;
            }

            // 表の右側
            r = header_rows;
            for (var i = 0; i < item2.Count; i++)
            {
                FixNode item = item2[i];

                table[r, 4] = printManager.toString(item.n);
                table.AlignX[r, 4] = "R";

                table[r, 5] = printManager.toString(item.tx);
                table.AlignX[r, 5] = "R";

                table[r, 6] = printManager.toString(item.ty);
                table.AlignX[r, 6] = "R";

                table[r, 7] = printManager.toString(item.rz);
                table.AlignX[r, 7] = "R";
                r++;
            }

            table.RowHeight[3] = printManager.LineSpacing2; // 表題と body の間

            return table;

        }

        #region PrintableBaseB.printPDF()用メソッド定義
        // Empty sheets (a frame supported by member springs only) print no table.
        protected override bool HasAnyData() => fixnodes.Values.Any(sheet => sheet.Count > 0);
        protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo)
        {
            // タイトル などの初期化
            printInit(mc, data);

            titles = new[] { title, };
            headerRows = myTable.Rows;
            nupInfo = Table.OneUpInfo;
        }
        protected override IEnumerable<Table> GetTables(PdfDocument mc, PrintData data, int indexPage)
        {
            foreach (var tmp0 in fixnodes.Where(sheet => sheet.Value.Count > 0).Chunk(dimension == 3 ? 1 : 2))
            {
                Table table;
                if (dimension == 3)
                {
                    table = getPageContents3D(tmp0.ElementAt(0).Value);
                    table[0, 0] = $"Type{tmp0.ElementAt(0).Key}";
                }
                else
                {
                    if (tmp0.Count() > 1)
                    {
                        table = getPageContents2D(tmp0.ElementAt(0).Value, tmp0.ElementAt(1).Value);
                        table[0, 2] = $"Type{tmp0.ElementAt(0).Key}";
                        table[0, 6] = $"Type{tmp0.ElementAt(1).Key}";
                    }
                    else
                    {
                        table = getPageContents2D(tmp0.ElementAt(0).Value, new List<FixNode>());
                        table[0, 2] = $"Type{tmp0.ElementAt(0).Key}";
                        table[0, 6] = "";

                        // 表の右の表題を消す
                        for (int r = 1; r <= 2; r++)
                            for (int c = 4; c <= 7; c++)
                                table[r, c] = "";
                    }
                }

                yield return table;
            }
        }
        protected override bool RequiresNewpage(PdfDocument mc, int[] printableRows, int tableRows, int headerRows, ref int judgeCount)
        {
            if (++judgeCount > 1)
            {
                // 2回目以降の判定。データ行が1行も入らない場合は改ページ
                return printableRows[0] < headerRows + 1;
            }
            else
            {
                // 1回目の判定
                // 現在のページに何も出力されていない場合は改ページしない
                if (mc.currentPos.Equals(mc.initialPos))
                {
                    return false;
                }
                // データ行の半分以上が出力できなければ改ページ
                return printableRows[0] < headerRows + (tableRows - headerRows + 1) / 2;
            }
        }
        #endregion

        #endregion

        #region 他のモジュールのヘルパー関数

        // 格点データの取得
        public IReadOnlyDictionary<int, List<FixNode>> FixNodes => fixnodes;

        #endregion
    }
}

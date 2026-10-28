using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    public class LoadMember
    {
        public string m1;
        public string m2;
        public string direction;
        public string mark;
        public string L1;
        public double L2;
        public double P1;
        public double P2;
        public int row;
        public string ms; // FrameG の M/S: 局所 y の荷重を曲げ用 (M) とせん断用 (S) に分ける印
    }

    public class LoadNode
    {
        public string n;
        public double tx;
        public double ty;
        public double tz;
        public double rx;
        public double ry;
        public double rz;
    }

    public class Load
    {
        public LoadMember[] load_member;
        public LoadNode[] load_node;
        public LoadName[] loadname;
        public int fix_member;
    }


    internal class InputLoad : PrintableBaseB
    {
        public const string KEY = "load";

        public Dictionary<int, Load> loads = new Dictionary<int, Load>();
        private InputLoadName LoadName = null;


        public InputLoad(Dictionary<string, object> value, List<int> loadnames)
        {
            if (!value.ContainsKey(KEY))
                return;

            // loadデータを取得する
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();

              // データを抽出する
            foreach (var index in loadnames)
            {
                var lo = new Load();

                var item = JObject.FromObject(target[index.ToString()]);

                if (item.ContainsKey("load_member"))
                {   // 部材荷重
                    var LoadM = new List<LoadMember>();
                    foreach (JToken member in item["load_member"])
                    {
                        var lm = new LoadMember();

                        lm.m1 = dataManager.toString(member["m1"]);
                        lm.m2 = dataManager.toString(member["m2"]);
                        lm.direction = dataManager.toString(member["direction"]);
                        lm.mark = dataManager.toString(member["mark"]);
                        lm.L1 = dataManager.toString(member["L1"]);
                        lm.ms = dataManager.toString(member["ms"]);
                        lm.L2 = dataManager.parseDouble(member["L2"]);
                        lm.P1 = dataManager.parseDouble(member["P1"]);
                        lm.P2 = dataManager.parseDouble(member["P2"]);
                        lm.row = dataManager.parseInt(member["rwo"]);

                        LoadM.Add(lm);
                    }
                    lo.load_member = LoadM.ToArray();
                }

                if (item.ContainsKey("load_node"))
                {   // 節点荷重
                    var LoadN = new List<LoadNode>();
                    foreach (JToken node in item["load_node"])
                    {
                        var ln = new LoadNode();

                        ln.n = dataManager.toString(node["n"]);
                        ln.tx = dataManager.parseDouble(node["tx"]);
                        ln.ty = dataManager.parseDouble(node["ty"]);
                        ln.tz = dataManager.parseDouble(node["tz"]);
                        ln.rx = dataManager.parseDouble(node["rx"]);
                        ln.ry = dataManager.parseDouble(node["ry"]);
                        ln.rz = dataManager.parseDouble(node["rz"]);

                        LoadN.Add(ln);
                    }
                    lo.load_node = LoadN.ToArray();
                }

                if (item.ContainsKey("name"))
                {   // 節点荷重
                    var loadNames = new List<LoadName>();
                    foreach (JToken name in item["name"])
                    {
                        var ln = new LoadName();

                        ln.name = dataManager.toString(name["name"]);

                        loadNames.Add(ln);
                    }
                    lo.loadname = loadNames.ToArray();
                }

                lo.fix_member = dataManager.parseInt(item["fix_member"]);

                this.loads.Add(index, lo);
            }

        }

        ///印刷処理

        ///タイトル
        private string title;
        ///２次元か３次元か
        private int dimension;
        ///テーブル(Member)
        private Table myTable1;
        ///テーブル(Node)
        private Table myTable2;
        ///テーブル(印刷用)
        private Table _table;
        ///テーブル(印刷用)
        private Table table;
        ///節点情報
        private InputNode Node = null;
        ///材料情報
        private InputElement Element = null;

        // ヘッダー作成（部材荷重）
        private void printLoadMember(PrintData data)
        {
            ///テーブルの作成
            this.myTable1 = new Table(3, 9);

            ///テーブルの幅
            this.myTable1.ColWidth[0] = 40.0;//スタート
            this.myTable1.ColWidth[1] = 50.0;//エンド
            this.myTable1.ColWidth[2] = 60.0;//方向
            this.myTable1.ColWidth[3] = 50.0;//マーク
            this.myTable1.ColWidth[4] = 60.0;//L1
            this.myTable1.ColWidth[5] = 75.0;//L2
            this.myTable1.ColWidth[6] = 75.0;//P1
            this.myTable1.ColWidth[7] = 75.0;//P2

            myTable1.RowHeight[1] = printManager.LineSpacing2;

            // 表題
            this.myTable1[2, 4] = "L1　";
            this.myTable1[2, 5] = "L2　";
            this.myTable1[2, 6] = "P1　";
            this.myTable1[2, 7] = "P2　";
            switch (data.language)
            {
                case "en":
                    this.title = "Load Strength";
                    this.myTable1[1, 0] = "Member Load";
                    this.myTable1[2, 0] = "Start";
                    this.myTable1[2, 1] = "End";
                    this.myTable1[2, 2] = "Direction";
                    this.myTable1[2, 3] = "Mark";
                    break;

                case "cn":
                    this.title = "载重负荷";
                    this.myTable1[1, 0] = "要件荷重";
                    this.myTable1[2, 0] = "開始";
                    this.myTable1[2, 1] = "結尾";
                    this.myTable1[2, 2] = "方向";
                    this.myTable1[2, 3] = "标记";
                    break;

                default:
                    this.title = "実荷重データ";
                    this.myTable1[1, 0] = "要素荷重";
                    this.myTable1[2, 0] = "スタート";
                    this.myTable1[2, 1] = "エンド";
                    this.myTable1[2, 2] = "方向";
                    this.myTable1[2, 3] = "マーク";
                    break;
            }

            //表題の文字位置
            this.myTable1.AlignX[0, 0] = "L";    // 左寄せ
            this.myTable1.AlignX[1, 0] = "L";    // 左寄せ
            this.myTable1.AlignX[2, 0] = "L";    // 左寄せ
            this.myTable1.AlignX[2, 1] = "R";
            this.myTable1.AlignX[2, 4] = "R";
            this.myTable1.AlignX[2, 5] = "R";
            this.myTable1.AlignX[2, 6] = "R";
            this.myTable1.AlignX[2, 7] = "R";

        }

        private void printLoadPoint(PrintData data)
        {
            if (dimension == 3)//３次元
            {
                ///テーブルの作成
                this.myTable2 = new Table(3, 9);

                ///テーブルの幅
                this.myTable2.ColWidth[0] = 40.0;//節点荷重
                this.myTable2.ColWidth[1] = 50.0;//節点番号
                this.myTable2.ColWidth[2] = 60.0;//X
                this.myTable2.ColWidth[3] = 50.0;//Y
                this.myTable2.ColWidth[4] = 60.0;//Z
                this.myTable2.ColWidth[5] = 75.0;//RX
                this.myTable2.ColWidth[6] = 75.0;//RY
                this.myTable2.ColWidth[7] = 75.0;//RZ

                myTable2.RowHeight[1] = printManager.LineSpacing2;

                // 表題
                this.myTable2[2, 2] = "X";
                this.myTable2[2, 3] = "Y";
                this.myTable2[2, 4] = "Z　";
                this.myTable2[2, 5] = "RX　";
                this.myTable2[2, 6] = "RY　";
                this.myTable2[2, 7] = "RZ　";


                //表題の文字位置
                this.myTable2.AlignX[0, 0] = "L";    // 左寄せ
                this.myTable2.AlignX[1, 0] = "L";    // 左寄せ
                this.myTable2.AlignX[2, 0] = "L";    // 左寄せ
                this.myTable2.AlignX[2, 1] = "R";
                this.myTable2.AlignX[2, 4] = "R";
                this.myTable2.AlignX[2, 5] = "R";
                this.myTable2.AlignX[2, 6] = "R";
                this.myTable2.AlignX[2, 7] = "R";

            }
            else//2次元
            {
                ///テーブルの作成
                this.myTable2 = new Table(3, 9);

                ///テーブルの幅
                this.myTable2.ColWidth[0] = 40.0;//節点荷重
                this.myTable2.ColWidth[1] = 50.0;//節点番号
                this.myTable2.ColWidth[2] = 60.0;//X
                this.myTable2.ColWidth[3] = 50.0;//Y
                this.myTable2.ColWidth[4] = 60.0;//R

                myTable2.RowHeight[1] = printManager.LineSpacing2;

                // 表題
                this.myTable2[2, 2] = "X";
                this.myTable2[2, 3] = "Y";
                this.myTable2[2, 4] = "R　";

                //表題の文字位置
                this.myTable2.AlignX[0, 0] = "L";    // 左寄せ
                this.myTable2.AlignX[1, 0] = "L";    // 左寄せ
                this.myTable2.AlignX[2, 0] = "L";    // 左寄せ
                this.myTable2.AlignX[2, 1] = "R";
                this.myTable2.AlignX[2, 4] = "R";

            }
            // 表題（2次元と3次元共通部分）
            switch (data.language)
            {
                case "en":
                    this.myTable2[1, 0] = "Node Load";
                    this.myTable2[2, 1] = "Node No";
                    break;

                case "cn":
                    this.myTable2[1, 0] = "节点载重";
                    this.myTable2[2, 1] = "节点编码";
                    break;

                default:
                    this.myTable2[1, 0] = "節点荷重";
                    this.myTable2[2, 1] = "節点番号";
                    break;
            }
        }

        ///印刷前の初期化処理
        ///
        private void printInit(PdfDocument mc, PrintData data)
        {
            this.dimension = data.dimension;

            //部材荷重のヘッダー作成
            printLoadMember(data);

            ////節点荷重のヘッダー作成
            printLoadPoint(data);
        }

        /// <summary>
        /// 1ページに入れるコンテンツを集計する
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContentsMember(Dictionary<int, Load> target)
        {
            int r = this.myTable1.Rows;
            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable1.Clone();
            table.ReDim(row: r + rows);

            table.RowHeight[r] = printManager.LineSpacing2;

            for (var i = 0; i < rows; i++)
            {
                if (target.Count <= i)
                    break;

                int No = target.ElementAt(i).Key;
                Load item = target.ElementAt(i).Value;

                if (item.load_member == null)
                    break;

                rows = +item.load_member.Length;
                table.ReDim(row: r + rows);

                for (int ii = 0; ii < item.load_member.Length; ii++)
                {
                    LoadMember lm = item.load_member[ii];

                    int j = 0;
                    table[r, j] = printManager.toString(lm.m1);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(lm.m2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(lm.direction);
                    j++;
                    table[r, j] = printManager.toString(lm.mark);
                    j++;
                    table[r, j] = printManager.toString(lm.L1, 3);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(lm.L2, 3);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(lm.P1, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(lm.P2, 2);
                    table.AlignX[r, j] = "R";
                    j++;

                    r++;
                }
            }
            table.ClearDraft();

            if (table.Rows == this.myTable1.Rows)
            {
                table = null;
            }
            return table;
        }
        private Table getPageContentsPoint(Dictionary<int, Load> target)
        {
            int r = this.myTable2.Rows;
            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable2.Clone();
            table.ReDim(row: r + rows);

            for (var i = 0; i < rows; i++)
            {
                if (target.Count <= i)
                    break;

                int No = target.ElementAt(i).Key;
                Load item = target.ElementAt(i).Value;

                if (item.load_node == null)
                {
                    table = null;
                    break;
                }

                rows = +item.load_node.Length;
                table.ReDim(row: r + rows);

                if (dimension == 3)//3次元
                {
                    for (int ii = 0; ii < item.load_node.Length; ii++)
                    {
                        LoadNode lln = item.load_node[ii];

                        int j = 1;
                        table[r, j] = printManager.toString(lln.n);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.tx);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.ty);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.tz);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.rx);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.ry);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.rz);
                        table.AlignX[r, j] = "R";
                        j++;

                        r++;
                    }
                }
                else//２次元
                {
                    for (int ii = 0; ii < item.load_node.Length; ii++)
                    {
                        LoadNode lln = item.load_node[ii];

                        int j = 1;
                        table[r, j] = printManager.toString(lln.n);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.tx);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.ty);
                        table.AlignX[r, j] = "R";
                        j++;
                        table[r, j] = printManager.toString(lln.rz);
                        j++;

                        r++;
                    }
                }
            }
            if (table != null)
            {
                table.ClearDraft();
                if (table.Rows == this.myTable2.Rows)
                {
                    table = null;
                }
            }
            return table;
        }

        #region PrintableBaseB.printPDF()用メソッド定義
        protected override bool HasAnyData() => loads.Any();
        protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo)
        {
            // 要素を取得できる状態にする
            this.LoadName = (InputLoadName)data.printDatas[InputLoadName.KEY];

            // タイトル などの初期化
            printInit(mc, data);

            titles = new[] { title, };
            headerRows = myTable1.Rows;
            nupInfo = Table.OneUpInfo;
        }
        protected override IEnumerable<Table> GetTables(PdfDocument mc, PrintData data, int indexPage)
        {
            for (var i = 0; i < loads.Count; i++)
            {
                var tmp2 = loads.ElementAt(i);
                var No = tmp2.Key;

                foreach (var table in new[]
                {
                    getPageContentsMember(new Dictionary<int, Load> { { tmp2.Key, tmp2.Value }, }),
                    getPageContentsPoint(new Dictionary<int, Load> { { tmp2.Key, tmp2.Value }, }),
                })
                {
                    if (table is null)
                    {
                        continue;
                    }

                    table[0, 0] = "Case" + No + " " + printManager.toString(LoadName.loadnames[No].name);

                    yield return table;
                }
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
                // ケース(Case)全体が出力できなければ改ページ
                return printableRows[0] < tableRows;
            }
        }
        #endregion
    }
}

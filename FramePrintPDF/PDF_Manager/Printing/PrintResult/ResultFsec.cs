using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{

    public class Fsec
    {
        public string m;    // 要素番号
        public string n;    // 節点番号
        public double l;    // 着目点距離
        public double fx;
        public double fy;
        public double fz;
        public double mx;
        public double my;
        public double mz;
        public bool dummy;  // 自動で設けた着目点ならtrue、それ以外はtrue

        // 組み合わせで使う
        public string caseStr = null;
        public string comb = null;
        public string stationLabel = null;
        public string memberCaption = null;
        public int occurrence; // Optional print-only foundation occurrence; legacy rows use zero.

        // インデックスから値を返す関数（3次元）
        public double getValue3D(int Index)
        {
            if (Index == 0)
                return this.fx;
            if (Index == 1)
                return this.fy;
            if (Index == 2)
                return this.fz;
            if (Index == 3)
                return this.mx;
            if (Index == 4)
                return this.my;
            if (Index == 5)
                return this.mz;
            return double.NaN;
        }

        // インデックスから値を返す関数（2次元）
        public double getValue2D(int Index)
        {
            if (Index == 0)
                return this.fx;
            if (Index == 1)
                return this.fy;
            if (Index == 2)
                return this.mz;

            return double.NaN;
        }
        public double getValue2D(string key)
        {
            if (key == "fx")
                return this.fx;
            if (key == "fy")
                return this.fy;
            if (key == "mz")
                return this.mz;

            return double.NaN;
        }
    }


    internal class ResultFsec : PrintableBaseB, IPrintable
    {
        public const string KEY = "fsec";

        public Dictionary<string, object> fsecs = new Dictionary<string, object>();
        private Dictionary<string, string> fsecnames = new Dictionary<string, string>();


        public ResultFsec(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            // データを取得する．
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();

            // データを抽出する
            for (var i = 0; i < target.Count; i++)
            {
                var key = dataManager.toString(target.ElementAt(i).Key);  // ケース番号
                var val = JToken.FromObject(target.ElementAt(i).Value);

                if (val.Type == JTokenType.Array)
                {
                    JArray Fsec = (JArray)val;
                    var _fsec = new List<Fsec>();
                    for (int j = 0; j < Fsec.Count; j++)
                    {
                        JToken item = Fsec[j];

                        var fs = new Fsec();

                        fs.n = dataManager.toString(item["n"]);
                        fs.m = dataManager.toString(item["m"]);
                        fs.stationLabel = dataManager.toString(item["stationLabel"]);
                        fs.memberCaption = dataManager.toString(item["memberCaption"]);
                        fs.occurrence = dataManager.parseInt(item["occurrence"]);
                        fs.l = dataManager.parseDouble(item["l"]);
                        fs.fx = dataManager.parseDouble(item["fx"]);
                        fs.fy = dataManager.parseDouble(item["fy"]);
                        fs.fz = dataManager.parseDouble(item["fz"]);
                        fs.mx = dataManager.parseDouble(item["mx"]);
                        fs.my = dataManager.parseDouble(item["my"]);
                        fs.mz = dataManager.parseDouble(item["mz"]);
                        fs.dummy = dataManager.toBool(item["dummy"]); // dummyが存在しない場合はfalse

                        _fsec.Add(fs);

                    }
                    this.fsecs.Add(key, _fsec);

                }
                else if (val.Type == JTokenType.Object)
                {   // LL：連行荷重の時
                    var Fsec = ((JObject)val).ToObject<Dictionary<string, object>>();
                    this.fsecs.Add(key, Fsec);
                }

            }
            if (!value.ContainsKey("fsecName"))
                return;

            // データを取得する．
            var targetName = JArray.FromObject(value["fsecName"]);

            for (int i = 0; i < target.Count; i++)
            {
                // タイトルを入れる
                var load = targetName[i];
                string[] loadNew = new string[2];

                loadNew[0] = load[0].ToString();
                loadNew[1] = load[1].ToString();

                fsecnames.Add(loadNew[0], loadNew[1]);

            }
        }

        ///印刷処理

        ///タイトル
        private string title;
        ///２次元か３次元か
        private int dimension;
        ///テーブル
        private Table myTable;


        ///印刷前の初期化処理
        ///
        private void printInit(PdfDocument mc, PrintData data)
        {
            this.dimension = data.dimension;

            if (this.dimension == 3)
            {///3次元

                ///テーブルの作成
                this.myTable = new Table(4, 9);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 15.0;//要素No
                this.myTable.ColWidth[1] = 40.0;//節点No
                this.myTable.ColWidth[2] = 60.0;//着目位置
                this.myTable.ColWidth[3] = 65.0;//軸方向力
                this.myTable.ColWidth[4] = 65.0;//Y方向のせん断力
                this.myTable.ColWidth[5] = 65.0;//Z方向のせん断力
                this.myTable.ColWidth[6] = 65.0;//ねじりﾓｰﾒﾝﾄ
                this.myTable.ColWidth[7] = 65.0;//Y軸周りの曲げモーメント
                this.myTable.ColWidth[8] = 65.0;//Z軸周りの曲げモーメント

                this.myTable.RowHeight[1] = printManager.LineSpacing2;

                this.myTable.AlignX[0, 0] = "L";
                this.myTable.AlignX[1, 0] = "L";
                this.myTable.AlignX[1, 1] = "R";
                this.myTable.AlignX[1, 2] = "R";
                this.myTable.AlignX[1, 3] = "R";
                this.myTable.AlignX[1, 4] = "R";
                this.myTable.AlignX[1, 5] = "R";
                this.myTable.AlignX[1, 6] = "R";
                this.myTable.AlignX[1, 7] = "R";
                this.myTable.AlignX[1, 8] = "R";
                this.myTable.AlignX[2, 0] = "R";
                this.myTable.AlignX[2, 1] = "R";
                this.myTable.AlignX[2, 2] = "R";
                this.myTable.AlignX[2, 3] = "R";
                this.myTable.AlignX[2, 4] = "R";
                this.myTable.AlignX[2, 5] = "R";
                this.myTable.AlignX[2, 6] = "R";
                this.myTable.AlignX[2, 7] = "R";
                this.myTable.AlignX[2, 8] = "R";
                this.myTable.AlignX[3, 1] = "R";
                this.myTable.AlignX[3, 2] = "R";
                this.myTable.AlignX[3, 3] = "R";
                this.myTable.AlignX[3, 4] = "R";
                this.myTable.AlignX[3, 5] = "R";
                this.myTable.AlignX[3, 6] = "R";
                this.myTable.AlignX[3, 7] = "R";
                this.myTable.AlignX[3, 8] = "R";

                // 表題
                this.myTable[3, 2] = "(m)";
                this.myTable[3, 3] = "(kN)";
                this.myTable[3, 4] = "(kN)";
                this.myTable[3, 5] = "(kN)";
                this.myTable[3, 6] = "(kN・m)";
                this.myTable[3, 7] = "(kN・m)";
                this.myTable[3, 8] = "(kN・m)";
                switch (data.language)
                {
                    case "en":
                        this.title = "SectionForce";
                        this.myTable[1, 0] = "Member";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "Node";
                        this.myTable[2, 1] = "No";
                        this.myTable[1, 2] = "Station";
                        this.myTable[2, 2] = "Location";
                        this.myTable[1, 3] = "Axial";
                        this.myTable[2, 3] = "Force";
                        this.myTable[1, 4] = "Y";
                        this.myTable[2, 4] = "Shear";
                        this.myTable[1, 5] = "Z";
                        this.myTable[2, 5] = "Shear";
                        this.myTable[1, 6] = "X";
                        this.myTable[2, 6] = "Torsion";
                        this.myTable[1, 7] = "Y";
                        this.myTable[2, 7] = "Momemt";
                        this.myTable[1, 8] = "Z";
                        this.myTable[2, 8] = "Momemt";
                        break;

                    case "cn":
                        this.title = "截面力";
                        this.myTable[1, 0] = "构件";
                        this.myTable[2, 0] = "编码";
                        this.myTable[1, 1] = "节点";
                        this.myTable[2, 1] = "编码";
                        this.myTable[1, 2] = "着眼";
                        this.myTable[2, 2] = "位置";
                        this.myTable[2, 3] = "轴向力";
                        this.myTable[1, 4] = "Y轴方向的";
                        this.myTable[2, 4] = "剪力";
                        this.myTable[1, 5] = "Z轴方向的";
                        this.myTable[2, 5] = "剪力";
                        this.myTable[1, 6] = "扭转";
                        this.myTable[2, 6] = "力矩";
                        this.myTable[1, 7] = "绕Y轴的";
                        this.myTable[2, 7] = "弯矩";
                        this.myTable[1, 8] = "绕Z轴的";
                        this.myTable[2, 8] = "弯矩";
                        break;

                    default:
                        this.title = "断面力データ";
                        this.myTable[1, 0] = "部材";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "節点";
                        this.myTable[2, 1] = "No";
                        this.myTable[1, 2] = "着目";
                        this.myTable[2, 2] = "位置";
                        this.myTable[2, 3] = "軸方向力";
                        this.myTable[1, 4] = "Y軸方向の";
                        this.myTable[2, 4] = "せん断力";
                        this.myTable[1, 5] = "Z軸方向の";
                        this.myTable[2, 5] = "せん断力";
                        this.myTable[1, 6] = "X軸周りの";
                        this.myTable[2, 6] = "ﾓｰﾒﾝﾄ";
                        this.myTable[1, 7] = "Y軸周りの";
                        this.myTable[2, 7] = "曲げﾓｰﾒﾝﾄ";
                        this.myTable[1, 8] = "Z軸周りの";
                        this.myTable[2, 8] = "曲げﾓｰﾒﾝﾄ";
                        break;
                }

                //表題の文字位置
            }
            else
            {//2次元

                ///テーブルの作成
                this.myTable = new Table(3, 6);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 15.0;//要素No
                this.myTable.ColWidth[1] = 40.0;//節点No
                this.myTable.ColWidth[2] = 65.0;//着目位置
                this.myTable.ColWidth[3] = 72.5;//X軸周りの回転量
                this.myTable.ColWidth[4] = 72.5;
                this.myTable.ColWidth[5] = 72.5;

                this.myTable.RowHeight[1] = printManager.LineSpacing2;

                this.myTable.AlignX[0, 0] = "L";
                this.myTable.AlignX[1, 0] = "L";
                this.myTable.AlignX[1, 1] = "R";
                this.myTable.AlignX[1, 2] = "R";
                this.myTable.AlignX[1, 3] = "R";
                this.myTable.AlignX[1, 4] = "R";
                this.myTable.AlignX[1, 5] = "R";
                this.myTable.AlignX[2, 0] = "R";
                this.myTable.AlignX[2, 1] = "R";
                this.myTable.AlignX[2, 2] = "R";
                this.myTable.AlignX[2, 3] = "R";
                this.myTable.AlignX[2, 4] = "R";
                this.myTable.AlignX[2, 5] = "R";

                // 表題
                this.myTable[2, 2] = "(m)";
                this.myTable[2, 3] = "(kN)";
                this.myTable[2, 4] = "(kN)";
                this.myTable[2, 5] = "(kN・m)";
                switch (data.language)
                {
                    case "en":
                        this.title = "SectionForce";
                        this.myTable[1, 0] = "Member";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "Node";
                        this.myTable[2, 1] = "No";
                        this.myTable[1, 2] = "Location";
                        this.myTable[1, 3] = "Axial";
                        this.myTable[1, 4] = "Shear";
                        this.myTable[1, 5] = "Momemt";
                        break;

                    case "cn":
                        this.title = "截面力";
                        this.myTable[1, 0] = "构件";
                        this.myTable[2, 0] = "编码";
                        this.myTable[1, 1] = "节点";
                        this.myTable[2, 1] = "编码";
                        this.myTable[1, 2] = "着眼位置";
                        this.myTable[1, 3] = "轴向力";
                        this.myTable[1, 4] = "剪力";
                        this.myTable[1, 5] = "弯矩";
                        break;

                    default:
                        this.title = "断面力データ";
                        this.myTable[1, 0] = "部材";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "節点";
                        this.myTable[2, 1] = "No";
                        this.myTable[1, 2] = "着目位置";
                        this.myTable[1, 3] = "軸方向力";
                        this.myTable[1, 4] = "せん断力";
                        this.myTable[1, 5] = "曲げﾓｰﾒﾝﾄ";
                        break;
                }

            }
        }

        /// <summary>
        /// 1ページに入れるコンテンツを集計する
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents(List<Fsec> target)
        {
            int r = this.myTable.Rows;

            //int columns = 2;
            int count = this.myTable.Columns;
            //int c = count / columns;

            int rows = target.Count;


            // 行コンテンツを生成
            var table = this.myTable.Clone();
            table.ReDim(row: r + rows);

            //table.RowHeight[r] = printManager.LineSpacing2;

            if (dimension == 3)　　//３次元
            {
                for (var i = 0; i < rows; i++)
                {
                    var item = target[i];

                    // 自動で設けた着目点は表出力対象外
                    if (item.dummy)
                    {
                        continue;
                    }

                    int j = 0;
                    table[r, j] = printManager.toString(item.m);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.n);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.l, 3);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.fx, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.fy, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.fz, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.mx, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.my, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.mz, 2);
                    table.AlignX[r, j] = "R";
                    j++;

                    var x = item.m;

                    if (x != "")
                    {
                        table.RowHeight[r] = printManager.LineSpacing2;
                    }
                    else if (i == 0)
                    {
                        table.RowHeight[r] = printManager.LineSpacing2;
                    }


                    r++;
                }
            }

            else　　//２次元
            {
                //int Rows = target.Count / columns;

                for (var i = 0; i < rows; i++)
                {
                    var item = target[i];

                    // 自動で設けた着目点は表出力対象外
                    if (item.dummy)
                    {
                        continue;
                    }

                    int j = 0;
                    table[r, j] = printManager.toString(item.m);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.n);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.l, 3);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.fx, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.fy, 2);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.mz, 2);
                    table.AlignX[r, j] = "R";
                    j++;

                    var x = item.m;

                    if (x != "")
                    {
                        table.RowHeight[r] = printManager.LineSpacing2;
                    }
                    else if (i == 0)
                    {
                        table.RowHeight[r] = printManager.LineSpacing2;
                    }

                    r++;

                }
            }
            return table;
        }

        #region PrintableBaseB.printPDF()用メソッド定義
        public new void printPDF(PdfDocument mc, PrintData data, ref int indexPage)
        {
            if (data.dimension != 2 || fsecs.Values.Any(value => !(value is List<Fsec>)))
            { base.printPDF(mc, data, ref indexPage); return; }
            bool next = false;
            foreach (var entry in fsecs)
            {
                ReferenceResultLayout.SingleForces(mc, data, ref indexPage,
                    ReferenceResultLayout.FrameGCaseTitle(entry.Key, fsecnames[entry.Key], " : "),
                    ((List<Fsec>)entry.Value).Where(row => !row.dummy).ToList(), next);
                next = true;
            }
        }

        protected override bool HasAnyData() => fsecs.Any();
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
            if (fsecnames.Count == 0) yield break;
            for (var j = 0; j < fsecs.Count; j++)
            {
                var key = fsecs.ElementAt(j).Key;  // ケース番号

                var caseNo = fsecnames.ElementAt(j).Key;
                var caseName = fsecnames.ElementAt(j).Value;
                var ds = fsecs.ElementAt(j).Value;

                // LL：連行荷重の時 -----------------------------------------------------------
                if (ds is Dictionary<string, object>)
                {
                    // 行間が狭いので、改行無効化の取り消し
                    mc.addCurrentY(+printManager.FontHeight); // Table.LineSpacing3

                    // ResultFsecCombine クラスに印刷してもらう
                    var dct1 = new Dictionary<string, object>() { { key, ds } };
                    var dct2 = new List<string[]>() { new string[] { caseNo, caseName } };
                    var dct3 = new Dictionary<string, object>(){
                        { "fsecCombine", dct1 },
                        { "fsecCombineName", dct2 }
                        };
                    var dg = new ResultFsecCombine(dct3);
                    dg.printPDF(mc, data, ref indexPage);
                    continue;
                }
                // ---------------------------------------------------------------------------

                var table = getPageContents(ds as List<Fsec>);
                table[0, 0] = caseNo + " " + caseName;

                yield return table;
            }
        }
        protected override int[] EstimatePrintableTableRows(PdfDocument mc, Table table, int titleRow, bool requiresPageTitle = true)
        {
            var printableRows = table.EstimatePrintableRows(mc, titleRow, requiresPageTitle);

            // 全体が出力できるならテーブル分割は不要
            if (printableRows[0] >= table.Rows)
            {
                return printableRows;
            }

            // 各部材データの切れ目
            var memberEndIndeces = Enumerable.Range(0, table.Rows)
                .Where(i => i > myTable.Rows) // 等号を含んでいないことに注意
                .Where(i => !string.IsNullOrEmpty(table[i, 0])) // 各部材データの先頭
                .Select(i => i - 1)
                .Append(table.Rows - 1); // 最後の部材データの末尾

            foreach (var p in memberEndIndeces.Select((index, i) => (index, i)).Reverse())
            {
                // 印刷可能な行数に納まる部材データの切れ目を探し、
                if (printableRows[0] >= p.index + 1)
                {
                    // その次の部材データが次ページに納まるなら、この切れ目で改ページさせる
                    var nextMemberLength = memberEndIndeces.ElementAt(p.i + 1) - p.index;
                    if (printableRows[1] >= nextMemberLength)
                    {
                        printableRows[0] = p.index + 1;
                        return printableRows;
                    }
                    // 納まらないなら諦める(次の部材データの途中で改ページが発生)
                    break;
                }
            }

            return printableRows;
        }
        protected override bool RequiresNewpage(PdfDocument mc, int[] printableRows, int tableRows, int headerRows, ref int judgeCount)
        {
            if (++judgeCount == 1)
            {
                // 1回目はページ先頭でなければ改ページ
                return !mc.currentPos.Equals(mc.initialPos);
            }
            else
            {
                // 2回目以降はデータ行が1行も入らない場合は改ページ
                return printableRows[0] < headerRows + 1;
            }
        }
        #endregion
    }
}


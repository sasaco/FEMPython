using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PDF_Manager.Printing
{
    public class FsecCombine
    {
        public List<Fsec> fx_max = new List<Fsec>();
        public List<Fsec> fx_min = new List<Fsec>();
        public List<Fsec> fy_max = new List<Fsec>();
        public List<Fsec> fy_min = new List<Fsec>();
        public List<Fsec> fz_max = new List<Fsec>();
        public List<Fsec> fz_min = new List<Fsec>();
        public List<Fsec> mx_max = new List<Fsec>();
        public List<Fsec> mx_min = new List<Fsec>();
        public List<Fsec> my_max = new List<Fsec>();
        public List<Fsec> my_min = new List<Fsec>();
        public List<Fsec> mz_max = new List<Fsec>();
        public List<Fsec> mz_min = new List<Fsec>();

        public void Add(string key, Fsec value)
        {
            // key と同じ名前の変数を取得する
            Type type = this.GetType();
            FieldInfo field = type.GetField(key);
            if (field == null)
            {
                throw new Exception(String.Format("FsecCombineクラスの変数{0} に値{1}を登録しようとしてエラーが発生しました", key, value));
            }
            var val = (List<Fsec>)field.GetValue(this);

            // 変数に値を追加する
            val.Add(value);

            // 変数を更新する
            field.SetValue(this, val);
        }

        public List<Fsec> getValue3(int Index)
        {
            if (Index == 0)
                return this.fx_max;
            if (Index == 1)
                return this.fx_min;
            if (Index == 2)
                return this.fy_max;
            if (Index == 3)
                return this.fy_min;
            if (Index == 4)
                return this.fz_max;
            if (Index == 5)
                return this.fz_min;
            if (Index == 6)
                return this.mx_max;
            if (Index == 7)
                return this.mx_min;
            if (Index == 8)
                return this.my_max;
            if (Index == 9)
                return this.my_min;
            if (Index == 10)
                return this.mz_max;
            if (Index == 11)
                return this.mz_min;

            return null;
        }

        public List<Fsec> getValue2(int Index)
        {
            if (Index == 0)
                return this.fx_max;
            if (Index == 1)
                return this.fx_min;
            if (Index == 2)
                return this.fy_max;
            if (Index == 3)
                return this.fy_min;
            if (Index == 4)
                return this.mz_max;
            if (Index == 5)
                return this.mz_min;

            return null;
        }

    }

    class ResultFsecCombine : PrintableBaseC, IPrintable
    {
        public const string KEY = "fsecCombine";

        public Dictionary<string, FsecCombine> Fsecs = new Dictionary<string, FsecCombine>();
        private Dictionary<string, string> fsecnames = new Dictionary<string, string>();


        public ResultFsecCombine(Dictionary<string, object> value, string key = ResultFsecCombine.KEY)
        {
            reportPrefix = key == "fsecPickup" ? "PICK UP " : "COMBINE ";
            if (!value.ContainsKey(key))
                return;

            // データを取得する．
            var target = JObject.FromObject(value[key]).ToObject<Dictionary<string, object>>();


            // データを抽出する
            for (var i = 0; i < target.Count; i++)
            {
                var No = dataManager.toString(target.ElementAt(i).Key);  // ケース番号
                var val = JToken.FromObject(target.ElementAt(i).Value);

                var Fsc = ((JObject)val).ToObject<Dictionary<string, object>>();
                var _Fsec = ResultFsecCombine.getFsecCombine(Fsc);

                this.Fsecs.Add(No, _Fsec);
            }

            // データを取得する．
            string nameKey = key + "Name";
            if (!value.ContainsKey(nameKey))
                return;

            var targetName = JArray.FromObject(value[nameKey]);

            //LLか基本形かを判定しながら1行1行確認
            for (int i = 0; i < target.Count; i++)
            {
                // タイトルを入れる
                var load = targetName[i];
                string[] loadNew = new string[2];

                loadNew[0] = load[0].ToString();
                loadNew[1] = load[1].ToString();

                fsecnames.Add(loadNew[0], loadNew[1]);
            }

            //タイトル
            this.title = key.Replace("fsec", "");

        }

        public static FsecCombine getFsecCombine(Dictionary<string, object> Fsc)
        {
            var _Fsec = new FsecCombine();

            for (int i = 0; i < Fsc.Count; i++)
            {
                JArray elist = JArray.FromObject(Fsc.ElementAt(i).Value);
                var k = Fsc.ElementAt(i).Key; // fx_min fx_max...

                for (int j = 0; j < elist.Count; j++)
                {
                    var item = elist[j];
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
                    fs.caseStr = dataManager.toString(item["case"]);
                    fs.comb = dataManager.toString(item["comb"]);

                    _Fsec.Add(k, fs);
                }


            }
            return _Fsec;
        }

        ///印刷処理

        ///タイトル
        private string title;
        ///２次元か３次元か
        private int dimension;
        ///テーブル
        private Table myTable;
        ///言語
        private string language;

        ///印刷前の初期化処理
        ///
        private void printInit(PdfDocument mc, PrintData data)
        {
            this.dimension = data.dimension;
            this.language = data.language;

            if (this.dimension == 3)
            {///3次元

                ///テーブルの作成
                this.myTable = new Table(5, 11);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 15.0;//節点No
                this.myTable.ColWidth[1] = 30.0;//部材No
                this.myTable.ColWidth[2] = 40.0;//着目位置
                this.myTable.ColWidth[3] = 55.0;//軸方向力
                this.myTable.ColWidth[4] = 55.0;//Y方向のせん断力
                this.myTable.ColWidth[5] = 55.0;//Z方向のせん断力
                this.myTable.ColWidth[6] = 55.0;//ねじりモーメント
                this.myTable.ColWidth[7] = 55.0;//Y軸周りの曲げモーメント
                this.myTable.ColWidth[8] = 55.0;//Z軸周りの曲げモーメント
                this.myTable.ColWidth[9] = 10.0;//調整
                this.myTable.ColWidth[10] = 55.0;//組合せ


                this.myTable.RowHeight[1] = printManager.LineSpacing2;
                this.myTable.RowHeight[2] = printManager.LineSpacing2;

                this.myTable.AlignX[0, 0] = "L";
                this.myTable.AlignX[1, 0] = "L";
                this.myTable.AlignX[2, 0] = "L";
                this.myTable.AlignX[2, 1] = "R";
                this.myTable.AlignX[2, 2] = "R";
                this.myTable.AlignX[2, 3] = "R";
                this.myTable.AlignX[2, 4] = "R";
                this.myTable.AlignX[2, 5] = "R";
                this.myTable.AlignX[2, 6] = "R";
                this.myTable.AlignX[2, 7] = "R";
                this.myTable.AlignX[2, 8] = "R";
                this.myTable.AlignX[3, 0] = "R";
                this.myTable.AlignX[3, 1] = "R";
                this.myTable.AlignX[3, 2] = "R";
                this.myTable.AlignX[3, 3] = "R";
                this.myTable.AlignX[3, 4] = "R";
                this.myTable.AlignX[3, 5] = "R";
                this.myTable.AlignX[3, 6] = "R";
                this.myTable.AlignX[3, 7] = "R";
                this.myTable.AlignX[3, 8] = "R";
                this.myTable.AlignX[4, 1] = "R";
                this.myTable.AlignX[4, 2] = "R";
                this.myTable.AlignX[4, 3] = "R";
                this.myTable.AlignX[4, 4] = "R";
                this.myTable.AlignX[4, 5] = "R";
                this.myTable.AlignX[4, 6] = "R";
                this.myTable.AlignX[4, 7] = "R";
                this.myTable.AlignX[4, 8] = "R";


                // 表題
                this.myTable[4, 2] = "(m)";
                this.myTable[4, 3] = "(kN)";
                this.myTable[4, 4] = "(kN)";
                this.myTable[4, 5] = "(kN)";
                this.myTable[4, 6] = "(kN・m)";
                this.myTable[4, 7] = "(kN・m)";
                this.myTable[4, 8] = "(kN・m)";

                switch (data.language)
                {
                    case "en":
                        this.title += " Force";
                        this.myTable[2, 0] = "Member";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "Node";
                        this.myTable[3, 1] = "No";
                        this.myTable[2, 2] = "Station";
                        this.myTable[3, 2] = "Location";
                        this.myTable[2, 3] = "Axial";
                        this.myTable[3, 3] = "Force";
                        this.myTable[2, 4] = "Y";
                        this.myTable[3, 4] = "Shear";
                        this.myTable[2, 5] = "Z";
                        this.myTable[3, 5] = "Shear";
                        this.myTable[2, 6] = "X";
                        this.myTable[3, 6] = "Torsion";
                        this.myTable[2, 7] = "Y";
                        this.myTable[3, 7] = "Momemt";
                        this.myTable[2, 8] = "Z";
                        this.myTable[3, 8] = "Momemt";
                        this.myTable[2, 10] = "Combination";
                        break;

                    case "cn":
                        this.title += "截面力";
                        this.myTable[2, 0] = "构件";
                        this.myTable[3, 0] = "编码";
                        this.myTable[2, 1] = "节点";
                        this.myTable[3, 1] = "编码";
                        this.myTable[2, 2] = "着眼";
                        this.myTable[3, 2] = "位置";
                        this.myTable[3, 3] = "轴向力";
                        this.myTable[2, 4] = "Y轴方向的";
                        this.myTable[3, 4] = "剪力";
                        this.myTable[2, 5] = "Z轴方向的";
                        this.myTable[3, 5] = "剪力";
                        this.myTable[2, 6] = "扭转";
                        this.myTable[3, 6] = "力矩";
                        this.myTable[2, 7] = "绕Y轴的";
                        this.myTable[3, 7] = "弯矩";
                        this.myTable[2, 8] = "绕Z轴的";
                        this.myTable[3, 8] = "弯矩";
                        this.myTable[2, 10] = "组合";
                        break;

                    default:
                        this.title += "断面力";
                        this.myTable[2, 0] = "部材";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "節点";
                        this.myTable[3, 1] = "No";
                        this.myTable[2, 2] = "着目";
                        this.myTable[3, 2] = "位置";
                        this.myTable[3, 3] = "軸方向力";
                        this.myTable[2, 4] = "Y軸方向の";
                        this.myTable[3, 4] = "せん断力";
                        this.myTable[2, 5] = "Z軸方向の";
                        this.myTable[3, 5] = "せん断力";
                        this.myTable[2, 6] = "X軸周りの";
                        this.myTable[3, 6] = "ﾓｰﾒﾝﾄ";
                        this.myTable[2, 7] = "Y軸周りの";
                        this.myTable[3, 7] = "曲げﾓｰﾒﾝﾄ";
                        this.myTable[2, 8] = "Z軸周りの";
                        this.myTable[3, 8] = "曲げﾓｰﾒﾝﾄ";
                        this.myTable[2, 10] = "組み合わせ";
                        break;
                }

                //表題の文字位置
            }
            else
            {//2次元

                ///テーブルの作成
                this.myTable = new Table(4, 8);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 15.0;//部材No
                this.myTable.ColWidth[1] = 30.0;//節点No
                this.myTable.ColWidth[2] = 60.0;//着目位置
                this.myTable.ColWidth[3] = 60.0;//軸方向力
                this.myTable.ColWidth[4] = 60.0;//せん断力
                this.myTable.ColWidth[5] = 60.0;//曲げモーメント
                this.myTable.ColWidth[6] = 30.0;//調整用
                this.myTable.ColWidth[7] = 200.0;//組合せ

                this.myTable.RowHeight[1] = printManager.LineSpacing2;
                this.myTable.RowHeight[2] = printManager.LineSpacing2;

                this.myTable.AlignX[0, 0] = "L";
                this.myTable.AlignX[1, 0] = "L";
                this.myTable.AlignX[2, 0] = "L";
                this.myTable.AlignX[2, 1] = "R";
                this.myTable.AlignX[2, 2] = "R";
                this.myTable.AlignX[2, 3] = "R";
                this.myTable.AlignX[2, 4] = "R";
                this.myTable.AlignX[2, 5] = "R";
                this.myTable.AlignX[3, 0] = "R";
                this.myTable.AlignX[3, 1] = "R";
                this.myTable.AlignX[3, 2] = "R";
                this.myTable.AlignX[3, 3] = "R";
                this.myTable.AlignX[3, 4] = "R";
                this.myTable.AlignX[3, 5] = "R";

                // 表題
                this.myTable[3, 2] = "(m)";
                this.myTable[3, 3] = "(kN)";
                this.myTable[3, 4] = "(kN)";
                this.myTable[3, 5] = "(kN・m)";
                switch (data.language)
                {
                    case "en":
                        this.title += " Force";
                        this.myTable[2, 0] = "Member";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "Node";
                        this.myTable[3, 1] = "No";
                        this.myTable[2, 2] = "Station";
                        this.myTable[3, 2] = "Location";
                        this.myTable[2, 3] = "Axial";
                        this.myTable[3, 3] = "Force";
                        this.myTable[2, 4] = "Shear";
                        this.myTable[2, 5] = "Momemt";
                        this.myTable[2, 7] = "Combination";
                        break;

                    case "cn":
                        this.title += "截面力";
                        this.myTable[2, 0] = "构件";
                        this.myTable[3, 0] = "编码";
                        this.myTable[2, 1] = "节点";
                        this.myTable[3, 1] = "编码";
                        this.myTable[2, 2] = "着眼位置";
                        this.myTable[2, 3] = "轴向力";
                        this.myTable[2, 4] = "剪力";
                        this.myTable[2, 5] = "弯矩";
                        this.myTable[2, 7] = "组合";
                        break;

                    default:
                        this.title += "断面力";
                        this.myTable[2, 0] = "部材";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "節点";
                        this.myTable[3, 1] = "No";
                        this.myTable[2, 2] = "着目位置";
                        this.myTable[2, 3] = "軸方向力";
                        this.myTable[2, 4] = "せん断力";
                        this.myTable[2, 5] = "曲げﾓｰﾒﾝﾄ";
                        this.myTable[2, 7] = "組合せ";
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

            int rows = target.Count;


            // 行コンテンツを生成
            var table = this.myTable.Clone();
            table.ReDim(row: r + rows); // １データ中で改行する場合があるので、多めに取っておく

            table.RowHeight[r] = printManager.LineSpacing2;

            if (dimension == 3)　　//３次元
            {
                for (var i = 0; i < rows; i++)
                {
                    var item = target[i];

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
                    j++;
                    if (item.caseStr != null)
                    {
                        var provenance = ReferenceResultLayout.Provenance(item.caseStr, item.comb);
                        int len = provenance.Length;
                        var str = provenance;

                        if (len > 10)
                        {
                            var lines = str.SubstringAtCount(10);
                            var rowCount = this.myTable.Rows + r + (rows - i) + lines.Length;
                            if (table.Rows < rowCount)
                            {   // 改行した後の
                                table.ReDim(row: rowCount);
                            }
                            foreach (var n in lines)
                            {
                                table[r, j] = n;
                                table.AlignX[r, j] = "R";
                                r++;
                            }
                        }
                        else
                        {
                            table[r, j] = provenance;
                            table.AlignX[r, j] = "R";
                            r++;
                        }
                    }

                }
            }

            else　　//２次元
            {
                for (var i = 0; i < rows; i++)
                {
                    var item = target[i];

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
                    j++;
                    if (item.caseStr != null)
                    {
                        var provenance = ReferenceResultLayout.Provenance(item.caseStr, item.comb);
                        int len = provenance.Length;
                        var str = provenance;

                        if (len > 30)
                        {
                            var lines = str.SubstringAtCount(30);
                            var rowCount = this.myTable.Rows + r + (rows - i) + lines.Length;
                            if (table.Rows < rowCount)
                            {   // 改行した後の
                                table.ReDim(row: rowCount);
                            }
                            foreach (var n in lines)
                            {
                                table[r, j] = n;
                                table.AlignX[r, j] = "L";
                                r++;
                            }
                        }
                        else
                        {
                            table[r, j] = provenance;
                            table.AlignX[r, j] = "L";
                            r++;
                        }
                    }

                }

            }

            table.ReDim(row: r);// １データ中で改行する場合を考慮して、多めに取っておいたので、適正サイズにする
            return table;
        }

        #region PrintableBaseC.printPDF()用メソッド定義
        private readonly string reportPrefix;
        public new void printPDF(PdfDocument mc, PrintData data, ref int indexPage)
        {
            if (data.dimension != 2)
            { base.printPDF(mc, data, ref indexPage); return; }
            foreach (var entry in Fsecs)
            {
                // Reference order is bending moment, shear, then axial force.
                foreach (var focus in new[] { (Name: "MOMENT", Max: 4), (Name: "SHARE", Max: 2), (Name: "AXIAL", Max: 0) })
                {
                    var max = entry.Value.getValue2(focus.Max);
                    var min = entry.Value.getValue2(focus.Max + 1);
                    if (!max.Any() && !min.Any()) continue;
                    // FrameG spells the shear page "SHARE"; the report prints "SHEAR" in the same field.
                    string force = focus.Name == "SHARE" ? "SHEAR" : focus.Name;
                    fsecnames.TryGetValue(entry.Key, out string name);
                    foreach (var extremum in new[] { (Rows: max, Name: "MAX."), (Rows: min, Name: "MIN.") })
                    {
                        if (!extremum.Rows.Any()) continue;
                        FrameGEnvelopeLayout.Print(mc, data, ref indexPage, reportPrefix == "PICK UP ",
                            entry.Key, name, $"{force,-6} {extremum.Name}", extremum.Rows);
                    }
                }
            }
        }

        protected readonly struct FsecContext : IContext
        {
            public FsecContext(ResultFsecCombine instance, int j)
            {
                this.instance = instance;
                this.j = j;
            }

            private readonly ResultFsecCombine instance;
            private readonly int j;

            public IEnumerable<Table> GetTables()
            {
                var dim = instance.dimension == 3 ? 12 : 6;
                var value = instance.Fsecs.ElementAt(j).Value;
                var caseNo = instance.fsecnames.ElementAt(j).Key;
                var caseName = instance.fsecnames.ElementAt(j).Value;
                var valueKey = instance.setValueKey(instance.dimension, instance.language);

                for (var k = 0; k < dim; k++)
                {
                    var tmp0 = (instance.dimension == 3) ? value.getValue3(k) : value.getValue2(k);
                    if (!tmp0.Any())
                        continue;

                    var table = instance.getPageContents(tmp0);
                    table[0, 0] = caseNo + " " + caseName;
                    table[1, 0] = valueKey[k];

                    yield return table;
                }
            }

            public bool IsLast() => j >= instance.Fsecs.Count - 1;
        }
        protected override bool HasAnyData() => Fsecs.Any();
        protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo)
        {
            // タイトル などの初期化
            printInit(mc, data);

            titles = new[] { title, };
            headerRows = myTable.Rows;
            nupInfo = Table.OneUpInfo;
        }
        protected override IEnumerable<IContext> GetContexts()
        {
            for (var k = 0; k < Fsecs.Count; k++)
            {
                yield return new FsecContext(this, k);
            }
        }
        #endregion

        private List<string> setValueKey(int dimension, string language)
        {
            var ValueKey = new List<string>();

            if (dimension == 3)  //３次元
            {
                switch (language)
                {
                    case "en":
                        ValueKey.Add("Axial force Max");
                        ValueKey.Add("Axial force Min");
                        ValueKey.Add("Y Shear force Max");
                        ValueKey.Add("Y Shear force Min");
                        ValueKey.Add("Z Shear force Max");
                        ValueKey.Add("Z Shear force Min");
                        ValueKey.Add("Torsion momemt Max");
                        ValueKey.Add("Torsion momemt Min");
                        ValueKey.Add("Y Bending moment Max");
                        ValueKey.Add("Y Bending moment Min");
                        ValueKey.Add("Z Bending moment Max");
                        ValueKey.Add("Z Bending moment Min");
                        break;

                    case "cn":
                        ValueKey.Add("轴向力　最大");
                        ValueKey.Add("轴向力　最小");
                        ValueKey.Add("Y轴方向的剪力　最大");
                        ValueKey.Add("Y轴方向的剪力　最小");
                        ValueKey.Add("Z轴方向的剪力　最大");
                        ValueKey.Add("Z轴方向的剪力　最小");
                        ValueKey.Add("扭转力矩　最大");
                        ValueKey.Add("扭转力矩　最小");
                        ValueKey.Add("绕Y轴的弯矩　最大");
                        ValueKey.Add("绕Y轴的弯矩　最小");
                        ValueKey.Add("绕Z轴的弯矩　最大");
                        ValueKey.Add("绕Z轴的弯矩　最小");
                        break;

                    default:
                        ValueKey.Add("軸方向力　最大");
                        ValueKey.Add("軸方向力　最小");
                        ValueKey.Add("Y方向のせん断力　最大");
                        ValueKey.Add("Y方向のせん断力　最小");
                        ValueKey.Add("Z方向のせん断力　最大");
                        ValueKey.Add("Z方向のせん断力　最小");
                        ValueKey.Add("ねじりモーメント　最大");
                        ValueKey.Add("ねじりモーメント　最小");
                        ValueKey.Add("Y軸周りの曲げモーメント　最大");
                        ValueKey.Add("Y軸周りの曲げモーメント　最小");
                        ValueKey.Add("Z軸周りの曲げモーメント　最大");
                        ValueKey.Add("Z軸周りの曲げモーメント　最小");
                        break;
                }
            }
            else
            {
                switch (language)
                {
                    case "en":
                        ValueKey.Add("Axial force Max");
                        ValueKey.Add("Axial force Min");
                        ValueKey.Add("Shear force Max");
                        ValueKey.Add("Shear force Min");
                        ValueKey.Add("Bending moment Max");
                        ValueKey.Add("Bending moment Min");
                        break;

                    case "cn":
                        ValueKey.Add("轴向力　最大");
                        ValueKey.Add("轴向力　最小");
                        ValueKey.Add("剪力　最大");
                        ValueKey.Add("剪力　最小");
                        ValueKey.Add("弯矩　最大");
                        ValueKey.Add("弯矩　最小");
                        break;

                    default:
                        ValueKey.Add("軸方向力　最大");
                        ValueKey.Add("軸方向力　最小");
                        ValueKey.Add("せん断力　最大");
                        ValueKey.Add("せん断力　最小");
                        ValueKey.Add("曲げモーメント　最大");
                        ValueKey.Add("曲げモーメント　最小");
                        break;
                }

            }
            return ValueKey;
        }

    }
}

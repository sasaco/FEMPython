using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PDF_Manager.Printing
{
    public class DisgCombine
    {
        public List<Disg> dx_max = new List<Disg>();
        public List<Disg> dx_min = new List<Disg>();
        public List<Disg> dy_max = new List<Disg>();
        public List<Disg> dy_min = new List<Disg>();
        public List<Disg> dz_max = new List<Disg>();
        public List<Disg> dz_min = new List<Disg>();
        public List<Disg> rx_max = new List<Disg>();
        public List<Disg> rx_min = new List<Disg>();
        public List<Disg> ry_max = new List<Disg>();
        public List<Disg> ry_min = new List<Disg>();
        public List<Disg> rz_max = new List<Disg>();
        public List<Disg> rz_min = new List<Disg>();

        public void Add(string key, Disg value)
        {
            // key と同じ名前の変数を取得する
            Type type = this.GetType();
            FieldInfo field = type.GetField(key);
            if (field == null)
            {
                throw new Exception(String.Format("DisgCombineクラスの変数{0} に値{1}を登録しようとしてエラーが発生しました", key, value));
            }
            var val = (List<Disg>)field.GetValue(this);

            // 変数に値を追加する
            val.Add(value);

            // 変数を更新する
            field.SetValue(this, val);
        }

        public List<Disg> getValue3(int Index)
        {
            if (Index == 0)
            {
                return this.dx_max;
            }
            if (Index == 1)
            {
                return this.dx_min;
            }
            if (Index == 2)
            {
                return this.dy_max;
            }
            if (Index == 3)
            {
                return this.dy_min;
            }
            if (Index == 4)
            {
                return this.dz_max;
            }
            if (Index == 5)
            {
                return this.dz_min;
            }
            if (Index == 6)
            {
                return this.rx_max;
            }
            if (Index == 7)
            {
                return this.rx_min;
            }
            if (Index == 8)
            {
                return this.ry_max;
            }
            if (Index == 9)
            {
                return this.ry_min;
            }
            if (Index == 10)
            {
                return this.rz_max;
            }
            if (Index == 11)
            {
                return this.rz_min;
            }

            return null;

        }

        public List<Disg> getValue2(int Index)
        {
            if (Index == 0)
            {
                return this.dx_max;
            }
            if (Index == 1)
            {
                return this.dx_min;
            }
            if (Index == 2)
            {
                return this.dy_max;
            }
            if (Index == 3)
            {
                return this.dy_min;
            }
            if (Index == 4)
            {
                return this.rz_max;
            }
            if (Index == 5)
            {
                return this.rz_min;
            }
            return null;

        }


    }


    //文字数分割用メソッド
    public static class StringExtensions
    {
        public static string[] SubstringAtCount(this string self, int count)
        {
            var result = new List<string>();
            var length = (int)Math.Ceiling((double)self.Length / count);

            for (int i = 0; i < length; i++)
            {
                int start = count * i;
                if (self.Length <= start)
                {
                    break;
                }
                if (self.Length < start + count)
                {
                    result.Add(self.Substring(start));
                }
                else
                {
                    result.Add(self.Substring(start, count));
                }
            }

            return result.ToArray();
        }
    }

    internal class ResultDisgCombine : PrintableBaseC
    {
        public const string KEY = "disgCombine";

        private Dictionary<string, DisgCombine> disgs = new Dictionary<string, DisgCombine>();
        private Dictionary<string, string> disgnames = new Dictionary<string, string>();
        private Dictionary<string, string> disgcasenames = new Dictionary<string, string>();

        public ResultDisgCombine(Dictionary<string, object> value, string key = ResultDisgCombine.KEY)
        {
            if (!value.ContainsKey(key))
                return;

            // データを取得する．
            var target = JObject.FromObject(value[key]).ToObject<Dictionary<string, object>>();


            // データを抽出する
            for (var i = 0; i < target.Count; i++)
            {
                var No = dataManager.toString(target.ElementAt(i).Key);  // ケース番号
                var val = JToken.FromObject(target.ElementAt(i).Value);

                var Dis = ((JObject)val).ToObject<Dictionary<string, object>>();
                var _disg = ResultDisgCombine.getDisgCombine(Dis);

                this.disgs.Add(No, _disg);
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

                disgnames.Add(loadNew[0], loadNew[1]);
            }

            //タイトル
            this.title = key.Replace("disg", "");

        }

        public static DisgCombine getDisgCombine(Dictionary<string, object> Dis)
        {
            var _disg = new DisgCombine();

            for (int i = 0; i < Dis.Count; i++)
            {
                var elist = JObject.FromObject(Dis.ElementAt(i).Value).ToObject<Dictionary<string, object>>();
                var k = Dis.ElementAt(i).Key;

                for (int j = 0; j < elist.Count; j++)
                {
                    var item = JObject.FromObject(elist.ElementAt(j).Value);

                    var ds = new Disg();

                    ds.id = dataManager.toString(elist.ElementAt(j).Key);
                    ds.dx = dataManager.parseDouble(item["dx"]);
                    ds.dy = dataManager.parseDouble(item["dy"]);
                    ds.dz = dataManager.parseDouble(item["dz"]);
                    ds.rx = dataManager.parseDouble(item["rx"]);
                    ds.ry = dataManager.parseDouble(item["ry"]);
                    ds.rz = dataManager.parseDouble(item["rz"]);
                    ds.caseStr = dataManager.toString(item["case"]);
                    ds.comb = dataManager.toString(item["comb"]);

                    _disg.Add(k, ds);
                }
            }
            return _disg;
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
                this.myTable = new Table(5, 9);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 15.0;//節点No
                this.myTable.ColWidth[1] = 60.0;//X方向の移動量
                this.myTable.ColWidth[2] = 60.0;//Y方向の移動量
                this.myTable.ColWidth[3] = 60.0;//Z方向の移動量
                this.myTable.ColWidth[4] = 60.0;//X軸周りの回転量
                this.myTable.ColWidth[5] = 60.0;//Y軸周りの回転量
                this.myTable.ColWidth[6] = 60.0;//Z軸周りの回転量
                this.myTable.ColWidth[7] = 10.0;//調整用
                this.myTable.ColWidth[8] = 80.0;//組合せ

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
                this.myTable.AlignX[3, 0] = "L";
                this.myTable.AlignX[3, 1] = "R";
                this.myTable.AlignX[3, 2] = "R";
                this.myTable.AlignX[3, 3] = "R";
                this.myTable.AlignX[3, 4] = "R";
                this.myTable.AlignX[3, 5] = "R";
                this.myTable.AlignX[3, 6] = "R";
                this.myTable.AlignX[3, 7] = "R";
                this.myTable.AlignX[4, 1] = "R";
                this.myTable.AlignX[4, 2] = "R";
                this.myTable.AlignX[4, 3] = "R";
                this.myTable.AlignX[4, 4] = "R";
                this.myTable.AlignX[4, 5] = "R";
                this.myTable.AlignX[4, 6] = "R";
                this.myTable.AlignX[4, 7] = "R";

                // 表題
                this.myTable[4, 1] = "(mm)";
                this.myTable[4, 2] = "(mm)";
                this.myTable[4, 3] = "(mm)";
                this.myTable[4, 4] = "(mmrad)";
                this.myTable[4, 5] = "(mmrad)";
                this.myTable[4, 6] = "(mmrad)";
                switch (data.language)
                {
                    case "en":
                        this.title += " displacement";
                        this.myTable[2, 0] = "Node";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "Movement";
                        this.myTable[3, 1] = "X ";
                        this.myTable[2, 2] = "";
                        this.myTable[3, 2] = "Y";
                        this.myTable[2, 3] = "";
                        this.myTable[3, 3] = "Z";
                        this.myTable[2, 4] = "Rotational angle";
                        this.myTable[3, 4] = "X";
                        this.myTable[2, 5] = "";
                        this.myTable[3, 5] = "Y";
                        this.myTable[2, 6] = "";
                        this.myTable[3, 6] = "Z";
                        this.myTable[2, 8] = "Combination";
                        break;

                    case "cn":
                        this.title += "位移量";
                        this.myTable[2, 0] = "节点";
                        this.myTable[3, 0] = "编码";
                        this.myTable[2, 1] = "X方向的";
                        this.myTable[3, 1] = "移动量";
                        this.myTable[2, 2] = "Y方向的";
                        this.myTable[3, 2] = "移动量";
                        this.myTable[2, 3] = "Z方向的";
                        this.myTable[3, 3] = "移动量";
                        this.myTable[2, 4] = "绕X轴的";
                        this.myTable[3, 4] = "旋转量";
                        this.myTable[2, 5] = "绕Y轴的";
                        this.myTable[3, 5] = "旋转量";
                        this.myTable[2, 6] = "绕Z轴的";
                        this.myTable[3, 6] = "旋转量";
                        this.myTable[2, 8] = "组合";
                        break;

                    default:
                        this.title += "変位量";
                        this.myTable[2, 0] = "節点";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "X方向の";
                        this.myTable[3, 1] = "移動量";
                        this.myTable[2, 2] = "Y方向の";
                        this.myTable[3, 2] = "移動量";
                        this.myTable[2, 3] = "Z方向の";
                        this.myTable[3, 3] = "移動量";
                        this.myTable[2, 4] = "X軸周りの";
                        this.myTable[3, 4] = "回転量";
                        this.myTable[2, 5] = "Y軸周りの";
                        this.myTable[3, 5] = "回転量";
                        this.myTable[2, 6] = "Z軸周りの";
                        this.myTable[3, 6] = "回転量";
                        this.myTable[2, 8] = "組合せ";
                        break;
                }

                //表題の文字位置
            }
            else
            {//2次元

                ///テーブルの作成
                this.myTable = new Table(5, 6);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 15.0;//節点No
                this.myTable.ColWidth[1] = 60.0;//X方向の移動量
                this.myTable.ColWidth[2] = 60.0;//Y方向の移動量
                this.myTable.ColWidth[3] = 60.0;//回転量
                this.myTable.ColWidth[4] = 30.0;//調整用
                this.myTable.ColWidth[5] = 200.0;//組合せ

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

                this.myTable.AlignX[3, 0] = "L";
                this.myTable.AlignX[3, 1] = "R";
                this.myTable.AlignX[3, 2] = "R";
                this.myTable.AlignX[3, 3] = "R";
                this.myTable.AlignX[3, 4] = "R";
                this.myTable.AlignX[3, 5] = "R";

                this.myTable.AlignX[4, 1] = "R";
                this.myTable.AlignX[4, 2] = "R";
                this.myTable.AlignX[4, 3] = "R";
                this.myTable.AlignX[4, 4] = "R";
                this.myTable.AlignX[4, 5] = "R";

                // 表題
                this.myTable[4, 1] = "(mm)";
                this.myTable[4, 2] = "(mm)";
                this.myTable[4, 3] = "(mmrad)";
                switch (data.language)
                {
                    case "en":
                        this.title += " displacement";
                        this.myTable[2, 0] = "Node";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "Movement";
                        this.myTable[3, 1] = "X";
                        this.myTable[2, 2] = "";
                        this.myTable[3, 2] = "Y";
                        this.myTable[2, 3] = "Rotate";
                        this.myTable[3, 3] = "angle";
                        this.myTable[2, 5] = "Combination";
                        break;

                    case "cn":
                        this.title += "位移量";
                        this.myTable[2, 0] = "节点";
                        this.myTable[3, 0] = "编码";
                        this.myTable[2, 1] = "X方向的";
                        this.myTable[3, 1] = "移动量";
                        this.myTable[2, 2] = "Y方向的";
                        this.myTable[3, 2] = "移动量";
                        this.myTable[3, 3] = "移动量";
                        this.myTable[2, 5] = "组合";
                        break;

                    default:
                        this.title += "変位量";
                        this.myTable[2, 0] = "節点";
                        this.myTable[3, 0] = "No";
                        this.myTable[2, 1] = "X方向の";
                        this.myTable[3, 1] = "移動量";
                        this.myTable[2, 2] = "Y方向の";
                        this.myTable[3, 2] = "移動量";
                        this.myTable[3, 3] = "回転量";
                        this.myTable[2, 5] = "組合せ";
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
        private Table getPageContents(List<Disg> target)
        {
            int r = this.myTable.Rows;

            int columns = 2;
            int count = this.myTable.Columns;
            int c = count / columns;

            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable.Clone();
            if (dimension == 3)　　//３次元
            {
                table.ReDim(row: r + rows);
                table.RowHeight[r] = printManager.LineSpacing2;

                for (var i = 0; i < rows; i++)
                {
                    var item = target[i];

                    int j = 0;
                    table[r, j] = printManager.toString(item.id);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.dx, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.dy, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.dz, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.rx, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.ry, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.rz, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    j++;
                    if(item.caseStr != null)
                    {
                        int len = item.caseStr.Length;
                        var str = item.caseStr;

                        if(len > 24)
                        {
                            var lines = str.SubstringAtCount(24);
                            var rowCount = this.myTable.Rows + r + (rows - i) + lines.Length;
                            if (table.Rows < rowCount)
                            {   // 改行した後の
                                table.ReDim(row: rowCount);
                            }
                            foreach (var n in lines)
                            {
                                table[r, j] = printManager.toString(n, 4);
                                table.AlignX[r, j] = "L";
                                r++;
                            }
                        }
                        else
                        {
                            table[r, j] = printManager.toString(item.caseStr, 4);
                            table.AlignX[r, j] = "L";
                            r++;
                        }
                    }
                }
            }

            else　　//２次元
            {
                table.ReDim(row: r + rows);
                table.RowHeight[r] = printManager.LineSpacing2;

                int Rows = target.Count / columns;

                for (var i = 0; i < rows; i++)
                {
                    var item = target[i];

                    int j = 0;
                    table[r, j] = printManager.toString(item.id);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.dx, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.dy, 4);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.rz, 4);
                    table.AlignX[r, j] = "R";

                    j = 5;
                    if (item.caseStr != null)
                    {
                        int len = item.caseStr.Length;
                        var str = item.caseStr;

                        if (len > 50)
                        {
                            var lines = str.SubstringAtCount(50);
                            var rowCount = this.myTable.Rows + r + (rows - i) + lines.Length;
                            if (table.Rows < rowCount)
                            {   // 改行した後の
                                table.ReDim(row: rowCount);
                            }
                            foreach (var n in lines)
                            {
                                table[r, j] = printManager.toString(n, 4);
                                table.AlignX[r, j] = "L";
                                r++;
                            }
                        }
                        else
                        {
                            table[r, j] = printManager.toString(item.caseStr, 4);
                            table.AlignX[r, j] = "L";
                            r++;
                        }
                    }
                }

            }

            return table;
        }

        #region PrintableBaseC.printPDF()用メソッド定義
        protected readonly struct DisgContext : IContext
        {
            public DisgContext(ResultDisgCombine instance, int j)
            {
                this.instance = instance;
                this.j = j;
            }

            private readonly ResultDisgCombine instance;
            private readonly int j;

            public IEnumerable<Table> GetTables()
            {
                var dim = instance.dimension == 3 ? 12 : 6;
                var value = instance.disgs.ElementAt(j).Value;
                var caseNo = instance.disgnames.ElementAt(j).Key;
                var caseName = instance.disgnames.ElementAt(j).Value;
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

            public bool IsLast() => j >= instance.disgs.Count - 1;
        }
        protected override bool HasAnyData() => disgs.Any();
        protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo)
        {
            // タイトル などの初期化
            printInit(mc, data);
            DisplacementTableLayout.FitNodeIds(mc, myTable,
                disgs.Values.SelectMany(value => Enumerable.Range(0, dimension == 3 ? 12 : 6)
                    .SelectMany(index => dimension == 3 ? value.getValue3(index) : value.getValue2(index)))
                    .Select(row => row.id), dimension == 3 ? 6 : 3, hasCombination: true);

            titles = new[] { title, };
            headerRows = myTable.Rows;
            nupInfo = Table.OneUpInfo;
        }
        protected override IEnumerable<IContext> GetContexts()
        {
            for (var k = 0; k < disgs.Count; k++)
            {
                yield return new DisgContext(this, k);
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
                        ValueKey.Add("Movement X Max");
                        ValueKey.Add("Movement X Min");
                        ValueKey.Add("Movement Y Max");
                        ValueKey.Add("Movement Y Min");
                        ValueKey.Add("Movement Z Max");
                        ValueKey.Add("Movement Z Min");
                        ValueKey.Add("Rotational angle X Max");
                        ValueKey.Add("Rotational angle X Min");
                        ValueKey.Add("Rotational angle Y Max");
                        ValueKey.Add("Rotational angle Y Min");
                        ValueKey.Add("Rotational angle Z Max");
                        ValueKey.Add("Rotational angle Z Min");
                        break;

                    case "cn":
                        ValueKey.Add("X方向的移动量　最大");
                        ValueKey.Add("X方向的移动量　最小");
                        ValueKey.Add("Y方向的移动量　最大");
                        ValueKey.Add("Y方向的移动量　最小");
                        ValueKey.Add("Z方向的移动量　最大");
                        ValueKey.Add("Z方向的移动量　最小");
                        ValueKey.Add("绕X轴的旋转量　最大");
                        ValueKey.Add("绕X轴的旋转量　最小");
                        ValueKey.Add("绕Y轴的旋转量　最大");
                        ValueKey.Add("绕Y轴的旋转量　最小");
                        ValueKey.Add("绕Z轴的旋转量　最大");
                        ValueKey.Add("绕Z轴的旋转量　最小");
                        break;

                    default:
                        ValueKey.Add("X方向の移動量　最大");
                        ValueKey.Add("X方向の移動量　最小");
                        ValueKey.Add("Y方向の移動量　最大");
                        ValueKey.Add("Y方向の移動量　最小");
                        ValueKey.Add("Z方向の移動量　最大");
                        ValueKey.Add("Z方向の移動量　最小");
                        ValueKey.Add("X軸周りの回転量　最大");
                        ValueKey.Add("X軸周りの回転量　最小");
                        ValueKey.Add("Y軸周りの回転量　最大");
                        ValueKey.Add("Y軸周りの回転量　最小");
                        ValueKey.Add("Z軸周りの回転量　最大");
                        ValueKey.Add("Z軸周りの回転量　最小");
                        break;
                }

            }
            else
            {
                switch (language)
                {
                    case "en":
                        ValueKey.Add("Movement X Max");
                        ValueKey.Add("Movement X Min");
                        ValueKey.Add("Movement Y Max");
                        ValueKey.Add("Movement Y Min");
                        ValueKey.Add("Rotational angle Max");
                        ValueKey.Add("Rotational angle Min");
                        break;

                    case "cn":
                        ValueKey.Add("X方向的移动量　最大");
                        ValueKey.Add("X方向的移动量　最小");
                        ValueKey.Add("Y方向的移动量　最大");
                        ValueKey.Add("Y方向的移动量　最小");
                        ValueKey.Add("旋转量　最大");
                        ValueKey.Add("旋转量　最小");
                        break;

                    default:
                        ValueKey.Add("X方向の移動量　最大");
                        ValueKey.Add("X方向の移動量　最小");
                        ValueKey.Add("Y方向の移動量　最大");
                        ValueKey.Add("Y方向の移動量　最小");
                        ValueKey.Add("回転量　最大");
                        ValueKey.Add("回転量　最小");
                        break;
                }

            }

            return ValueKey;
        }

    }
}

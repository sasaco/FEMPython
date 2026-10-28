using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    public class Element
    {
        public double E;
        public double G;
        public double Xp;
        public double A;
        public double J;
        public double Iy;
        public double Iz;
        public string name;
    }

    internal class InputElement : PrintableBaseB
    {
        public const string KEY = "element";

        private Dictionary<int, Dictionary<string, Element>> elements = new Dictionary<int, Dictionary<string, Element>>();

        // Materials by TYPE (sheet) number.
        internal IReadOnlyDictionary<int, Dictionary<string, Element>> Sheets => elements;

        /// <summary>
        /// データを読み込む
        /// </summary>
        /// <param name="dataManager"></param>
        /// <param name="value"></param>
        public InputElement(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            // elementデータを取得する．
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();

            // データを抽出する
            for (var i = 0; i < target.Count; i++)
            {
                var key = dataManager.parseInt(target.ElementAt(i).Key);
                var Elem = JObject.FromObject(target.ElementAt(i).Value).ToObject<Dictionary<string, object>>();

                var _element = new Dictionary<string, Element>();

                for (int j = 0; j < Elem.Count; j++)
                {
                    var id = Elem.ElementAt(j).Key;
                    var item = JObject.FromObject(Elem.ElementAt(j).Value);

                    var e = new Element();

                    e.name = dataManager.toString(item["n"]);
                    e.E = dataManager.parseDouble(item["E"]);
                    e.G = dataManager.parseDouble(item["G"]);
                    e.Xp = dataManager.parseDouble(item["Xp"]);
                    e.A = dataManager.parseDouble(item["A"]);
                    e.J = dataManager.parseDouble(item["J"]);
                    e.Iy = dataManager.parseDouble(item["Iy"]);
                    e.Iz = dataManager.parseDouble(item["Iz"]);

                    _element.Add(id, e);

                }
                this.elements.Add(key, _element);
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
            var X1 = printManager.H1PosX; //表題を印字するX位置  px ピクセル

            this.dimension = data.dimension;
            if (this.dimension == 3)
            {   // 3次元

                //テーブルの作成
                this.myTable = new Table(3, 8);

                // テーブルの幅
                this.myTable.ColWidth[0] = 45.0; // 材料番号 
                for (int i = 1; i < 8; ++i)
                    this.myTable.ColWidth[i] = 60.0;

                switch (data.language)
                {
                    case "en":
                        this.title = "Material Data";
                        this.myTable[1, 0] = "No";
                        this.myTable[1, 1] = "Area";
                        this.myTable[2, 1] = "A(m2)";
                        this.myTable[1, 2] = "Elastic";
                        this.myTable[2, 2] = "E(kN/m2)";
                        this.myTable[1, 3] = "Shear Elastic";
                        this.myTable[2, 3] = "G(kN/m2)";
                        this.myTable[1, 4] = "CTE";
                        this.myTable[1, 5] = "　　Inertia";
                        this.myTable[2, 5] = "IY(m4)";
                        this.myTable[2, 6] = "IZ(m4)";
                        this.myTable[1, 7] = "Torsion Constant";
                        break;

                    case "cn":
                        this.title = "材料";
                        this.myTable[1, 0] = "No";
                        this.myTable[1, 1] = "截面面积";
                        this.myTable[2, 1] = "A(m2)";
                        this.myTable[1, 2] = "弹性系数";
                        this.myTable[2, 2] = "E(kN/m2)";
                        this.myTable[1, 3] = "剪力弹性系数";
                        this.myTable[2, 3] = "G(kN/m2)";
                        this.myTable[1, 4] = "膨胀系数";
                        this.myTable[1, 5] = "　　截面二次力矩";
                        this.myTable[2, 5] = "IY(m4)";
                        this.myTable[2, 6] = "IZ(m4)";
                        this.myTable[1, 7] = "扭转常数";
                        break;

                    default:
                        this.title = "材料データ";
                        this.myTable[1, 0] = "No";
                        this.myTable[1, 1] = "断面積";
                        this.myTable[2, 1] = "A(m2)";
                        this.myTable[1, 2] = "弾性係数";
                        this.myTable[2, 2] = "E(kN/m2)";
                        this.myTable[1, 3] = "せん断弾性係数";
                        this.myTable[2, 3] = "G(kN/m2)";
                        this.myTable[1, 4] = "膨張係数";
                        this.myTable[1, 5] = "　　断面二次モーメント";
                        this.myTable[2, 5] = "IY(m4)";
                        this.myTable[2, 6] = "IZ(m4)";
                        this.myTable[1, 7] = "ねじり剛性";
                        break;
                }

                // 表題の文字位置
                this.myTable.AlignX[0, 0] = "L";    // 左寄せ
                this.myTable.AlignX[1, 5] = "L";    // 左寄せ

            }
            else
            {   // 2次元

                //テーブルの作成
                this.myTable = new Table(3, 6);

                // テーブルの幅
                this.myTable.ColWidth[0] = 15.0; // 材料番号 
                this.myTable.ColWidth[1] = 70.0;
                this.myTable.ColWidth[2] = 70.0;
                this.myTable.ColWidth[3] = 70.0;
                this.myTable.ColWidth[4] = 100.0;

                switch (data.language)
                {
                    case "en":
                        this.title = "Material Data";
                        this.myTable[1, 0] = "No";
                        this.myTable[1, 1] = "　　Area";
                        this.myTable[2, 1] = "　　A(m2)";
                        this.myTable[1, 2] = "　　Elastic";
                        this.myTable[2, 2] = "　　E(kN/m2)";
                        this.myTable[1, 3] = "　CTE";
                        this.myTable[1, 4] = "　　Inertia";
                        this.myTable[2, 4] = "　　I(m4)";
                        this.myTable[1, 5] = "    Name of Material";
                        break;

                    case "cn":
                        this.title = "材料";
                        this.myTable[1, 0] = "编码";
                        this.myTable[1, 1] = "截面面积";
                        this.myTable[2, 1] = "A(m2)";
                        this.myTable[1, 2] = "弹性系数";
                        this.myTable[2, 2] = "E(kN/m2)";
                        this.myTable[1, 3] = "膨胀系数";
                        this.myTable[1, 4] = "截面二次力矩";
                        this.myTable[2, 4] = "I(m4)";
                        this.myTable[1, 5] = "    材料名称";
                        break;

                    default:
                        this.title = "材料データ";
                        this.myTable[1, 0] = "No";
                        this.myTable[1, 1] = "断面積";
                        this.myTable[2, 1] = "A(m2)";
                        this.myTable[1, 2] = "弾性係数";
                        this.myTable[2, 2] = "E(kN/m2)";
                        this.myTable[1, 3] = "膨張係数";
                        this.myTable[1, 4] = "断面二次ﾓｰﾒﾝﾄ";
                        this.myTable[2, 4] = "I(m4)";
                        this.myTable[1, 5] = "    材料名称";
                        break;
                }


                // 表題の文字位置
                this.myTable.AlignX[0, 0] = "L";    // 左寄せ
                this.myTable.AlignX[1, 5] = "L";    // 左寄せ

            }

        }


        /// <summary>
        /// 1ページに入れるコンテンツを集計する
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents(Dictionary<string, Element> target)
        {
            int r = this.myTable.Rows;
            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable.Clone();

            if (this.dimension == 3)
            {
                table.ReDim(row: r + rows * 2);
                table.RowHeight[r] = printManager.LineSpacing2;

                for (var i = 0; i < rows; i++)
                {
                    string No = target.ElementAt(i).Key;
                    Element item = target.ElementAt(i).Value;

                    // 1行目
                    table[r, 0] = No;
                    table.AlignX[r, 0] = "R";

                    table[r, 1] = "　" + printManager.toString(this.GetElementName(No)); // 材料名称
                    table.AlignX[r, 1] = "L";
                    r++;

                    // 2行目
                    table[r, 1] = (item.A < 999) ? printManager.toString(item.A, 4) : printManager.toString(item.A);
                    table.AlignX[r, 1] = "R";

                    table[r, 2] = printManager.toString(item.E, 2, "E");
                    table.AlignX[r, 2] = "R";

                    table[r, 3] = printManager.toString(item.G, 2, "E");
                    table.AlignX[r, 3] = "R";

                    table[r, 4] = printManager.toString(item.Xp, 2, "E");
                    table.AlignX[r, 4] = "R";

                    table[r, 5] = (item.Iy < 999) ? printManager.toString(item.Iy, 6) : printManager.toString(item.Iy);
                    table.AlignX[r, 5] = "R";

                    table[r, 6] = (item.Iz < 999) ? printManager.toString(item.Iz, 6) : printManager.toString(item.Iz);
                    table.AlignX[r, 6] = "R";

                    table[r, 7] = (item.J < 999) ? printManager.toString(item.J, 6) : printManager.toString(item.J);
                    table.AlignX[r, 7] = "R";

                    r++;
                }
            }
            else
            {
                table.ReDim(row: r + rows);
                table.RowHeight[r] = printManager.LineSpacing2;

                for (var i = 0; i < rows; i++)
                {
                    string No = target.ElementAt(i).Key;
                    Element item = target.ElementAt(i).Value;

                    table[r, 0] = No;
                    table.AlignX[r, 0] = "R";

                    table[r, 1] = printManager.toString(item.A, 4);
                    table.AlignX[r, 1] = "R";

                    table[r, 2] = printManager.toString(item.E, 2, "E");
                    table.AlignX[r, 2] = "R";

                    table[r, 3] = printManager.toString(item.Xp, 2, "E");
                    table.AlignX[r, 3] = "R";

                    table[r, 4] = printManager.toString(item.Iz, 6);
                    table.AlignX[r, 4] = "R";

                    table[r, 5] = "    " + printManager.toString(this.GetElementName(No)); // 材料名称
                    table.AlignX[r, 5] = "L";

                    r++;
                }

            }

            table.RowHeight[1] = printManager.LineSpacing2; // 表題と body の間
            //table.RowHeight[2] = printManager.LineSpacing2; // 表題と body の間

            return table;
        }

        #region PrintableBaseB.printPDF()用メソッド定義
        protected override bool HasAnyData() => elements.Any();
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
            foreach (var tmp0 in elements)
            {
                var table = getPageContents(tmp0.Value);
                table[0, 0] = string.Format("Type{0}", tmp0.Key); // タイプ番号

                yield return table;
            }
        }
        #endregion

        #endregion



        /// <summary>
        /// 材料名を取得する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetElementName(string id)
        {
            string name = "";

            if (id == "" || id == null)
                return name;

            foreach (var _element in this.elements)
            {
                var Elem = _element.Value;
                if (Elem.ContainsKey(id))
                {
                    var item = Elem[id];
                    name = item.name;

                    if (name != "" || name != null)
                        break;  // 有効な名前が見つかったら forループを抜ける
                }
            }

            return name;
        }

    }

}

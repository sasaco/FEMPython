using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    public class Rigid
    {
        public string m; // 部材番号
        public double Ilength; // I端からの距離(m)
        public double Jlength; // J端からの距離(m)
        public string e; // 材料番号
    }


    internal class InputRigid : PrintableBaseA
    {
        public const string KEY = "rigid";

        public List<Rigid> rigids = new List<Rigid>();

        public InputRigid(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            // rigidデータを取得する
            var target = value[KEY] as JArray;

            // データを抽出する
            foreach (var item in target)
            {
                rigids.Add(new Rigid
                {
                    m = dataManager.toString(item["m"]),
                    Ilength = dataManager.parseDouble(item["Ilength"]),
                    Jlength = dataManager.parseDouble(item["Jlength"]),
                    e = dataManager.toString(item["e"]),
                });
            }
        }



        #region 印刷処理
        // タイトル
        private string title;
        // テーブル
        private Table myTable;
        // 部材情報
        private InputMember Member = null;
        // 材料情報
        private InputElement Element = null;


        /// <summary>
        /// 印刷前の初期化処理
        /// </summary>
        private void printInit(PdfDocument mc, PrintData data)
        {
            //テーブルの作成
            this.myTable = new Table(2, 7);

            // テーブルの幅
            this.myTable.ColWidth[0] = 20.0; // No
            this.myTable.ColWidth[1] = 60.0; // L
            this.myTable.ColWidth[2] = 50.0; // 剛域部材長さ I端側
            this.myTable.ColWidth[3] = 50.0; // 剛域部材長さ J端側
            this.myTable.ColWidth[4] = 70.0; // 材料番号    
            this.myTable.ColWidth[5] = 70.0; // 材料名称

            switch (data.language) // @TODO: 英語と中国語
            {
                case "en":
                    this.title = "Rigid zone Data";
                    this.myTable[1, 0] = "No";
                    this.myTable[0, 1] = "L ";
                    this.myTable[1, 1] = "(m)";
                    this.myTable[0, 3] = "Rigid member distance(m)";
                    this.myTable[1, 2] = "Node-I side";
                    this.myTable[1, 3] = "Node-J side";
                    this.myTable[0, 4] = "Material";
                    this.myTable[1, 4] = "No";
                    this.myTable[0, 5] = "    Name of Material";
                    // 見出しの幅が広いので列幅を拡げておく
                    this.myTable.ColWidth[2] = 70;
                    this.myTable.ColWidth[3] = 70;
                    break;

                case "cn":
                    this.title = "刚性区域数据";
                    this.myTable[1, 0] = "编码";
                    this.myTable[0, 1] = "构件长";
                    this.myTable[1, 1] = "(m)";
                    this.myTable[0, 3] = "刚性区构件距离(m)";
                    this.myTable[1, 2] = "I端侧";
                    this.myTable[1, 3] = "J端侧";
                    this.myTable[0, 4] = "材料编码";
                    this.myTable[0, 5] = "    材料名称";
                    break;

                default:
                    this.title = "剛域データ";
                    this.myTable[1, 0] = "No";
                    this.myTable[0, 1] = "L ";
                    this.myTable[1, 1] = "(m)";
                    this.myTable[0, 3] = "剛域部材長さ(m)";
                    this.myTable[1, 2] = "I端側";
                    this.myTable[1, 3] = "J端側";
                    this.myTable[0, 4] = "材料番号";
                    this.myTable[0, 5] = "    材料名称";
                    break;
            }

            // 表題の文字位置(デフォルトは中央寄せ)
            this.myTable.AlignX[0, 1] = "R";    // 右寄せ L
            this.myTable.AlignX[1, 1] = "R";    // 右寄せ (m)
            this.myTable.AlignX[0, 3] = "R";    // 左寄せ 剛域部材長さ(m)
            this.myTable.AlignX[1, 2] = "R";    // 右寄せ I端側
            this.myTable.AlignX[1, 3] = "R";    // 右寄せ J端側
            this.myTable.AlignX[0, 5] = "L";    // 左寄せ 材料名称
        }


        /// <summary>
        /// 1ページに入れるコンテンツを集計する
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents(IReadOnlyList<Rigid> target)
        {
            int r = this.myTable.Rows;
            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable.Clone();
            table.ReDim(row: r + rows);

            table.RowHeight[r] = printManager.LineSpacing2;

            for (var i = 0; i < rows; i++)
            {
                var no = target.ElementAt(i).m;
                var length = Member.GetMemberLength(no).ToString("F3");
                var iLength = target.ElementAt(i).Ilength.ToString("F2");
                var jLength = target.ElementAt(i).Jlength.ToString("F2");
                var e = target.ElementAt(i).e;
                var eName = Element.GetElementName(e);

                var j = 0;
                table[r, j] = no; // 部材番号
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = length; // 部材長
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = iLength; // I端からの距離
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = jLength; // J端からの距離
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = e + "  "; // 材料番号
                //table.AlignX[r, j] = "R";
                j++;
                table[r, j] = "    " + eName; // 部材名称
                table.AlignX[r, j] = "L";

                r++;
            }

            return table;
        }

        #region PrintableBaseA.printPDF()用メソッド定義
        protected override bool HasAnyData() => rigids.Count > 0;
        protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo)
        {
            // 部材長を取得できる状態にする
            Member = (InputMember)data.printDatas[InputMember.KEY];
            // The member table may not have been printed (FrameG's input copy prints its own).
            Member.UseNodes((InputNode)data.printDatas[InputNode.KEY]);

            // 材料名称を取得できる状態にする
            Element = (InputElement)data.printDatas[InputElement.KEY];

            printInit(mc, data);

            titles = new[] { title, };
            headerRows = myTable.Rows;
            nupInfo = Table.OneUpInfo;
        }
        protected override Table GetTable() => getPageContents(rigids);
        #endregion

        #endregion
    }
}


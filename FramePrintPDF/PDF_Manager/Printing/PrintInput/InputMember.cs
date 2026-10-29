using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    public class Member
    {
        public string ni; // 節点番号
        public string nj;
        public string e;  // 材料番号
        public double cg; // コードアングル

        // 他のモジュールで使う変数
        public double L = double.NaN;       // 要素の長さ
        public double[,] t = null;         // 座標変換マトリックス
        public double radian = double.NaN;   // 角度
    }


    internal class InputMember : PrintableBaseA
    {
        public const string KEY = "member";

        public Dictionary<string, Member> members = new Dictionary<string, Member>();

        public InputMember(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            // memberデータを取得する
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();

            // データを抽出する
            for (var i = 0; i < target.Count; i++)
            {
                var key = target.ElementAt(i).Key;
                var item = JObject.FromObject(target.ElementAt(i).Value);

                var m = new Member();
                m.ni = dataManager.toString(item["ni"]);
                m.nj = dataManager.toString(item["nj"]);
                m.e = dataManager.toString(item["e"]);
                m.cg = dataManager.parseDouble(item["cg"]);
                this.members.Add(key, m);
            }
        }



        #region 印刷処理
        // タイトル
        private string title;
        // 2次元か3次元か
        private int dimension;
        // テーブル
        private Table myTable;
        // 節点情報
        private InputNode Node = null;
        // 材料情報
        private InputElement Element = null;


        /// <summary>
        /// 印刷前の初期化処理
        /// </summary>
        private void printInit(PdfDocument mc, PrintData data)
        {
            this.dimension = data.dimension;

            if (this.dimension == 3)
            {   // 3次元

                //テーブルの作成
                this.myTable = new Table(2, 7);

                // テーブルの幅
                this.myTable.ColWidth[0] = 20.0; // 要素No
                this.myTable.ColWidth[1] = 45.0; // 格点No
                this.myTable.ColWidth[2] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[3] = 60.0; // 部材長
                this.myTable.ColWidth[4] = 70.0; // 材料番号    
                this.myTable.ColWidth[5] = 70.0; // コードアングル    
                this.myTable.ColWidth[6] = 70.0; // 材料名称

                switch (data.language)
                {
                    case "en":
                        this.title = "Member Data";
                        this.myTable[1, 0] = "No";
                        this.myTable[0, 2] = "　Node";
                        this.myTable[1, 1] = "Node-I";
                        this.myTable[1, 2] = "Node-J";
                        this.myTable[0, 3] = "Distance";
                        this.myTable[1, 3] = "(m)";
                        this.myTable[0, 4] = "Material";
                        this.myTable[1, 4] = "No";
                        this.myTable[0, 5] = "Angle of";
                        this.myTable[1, 5] = "Rotation";
                        this.myTable[0, 6] = "    Name of Material";
                        break;

                    case "cn":
                        this.title = "构件";
                        this.myTable[1, 0] = "编码";
                        this.myTable[0, 2] = "　节点";
                        this.myTable[1, 1] = "I端";
                        this.myTable[1, 2] = "J端";
                        this.myTable[0, 3] = "构件长";
                        this.myTable[1, 3] = "(m)";
                        this.myTable[0, 4] = "材料编码";
                        this.myTable[0, 5] = "转动角度";
                        this.myTable[1, 5] = "(°)";
                        this.myTable[0, 6] = "    材料名称";
                        break;

                    default:
                        this.title = "部材データ";
                        this.myTable[1, 0] = "No";
                        this.myTable[0, 2] = "　節点";
                        this.myTable[1, 1] = "I端";
                        this.myTable[1, 2] = "J端";
                        this.myTable[0, 3] = "L ";
                        this.myTable[1, 3] = "(m)";
                        this.myTable[0, 4] = "材料番号";
                        this.myTable[0, 5] = "コードアングル";
                        this.myTable[1, 5] = "(°)";
                        this.myTable[0, 6] = "    材料名称";
                        break;
                }

                // 表題の文字位置
                this.myTable.AlignX[0, 6] = "L";    // 左寄せ
                this.myTable.AlignX[0, 2] = "L";    // 左寄せ
                this.myTable.AlignX[1, 1] = "R";    // 右寄せ
                this.myTable.AlignX[1, 2] = "R";    // 右寄せ
                this.myTable.AlignX[0, 3] = "R";    // 右寄せ
                this.myTable.AlignX[1, 3] = "R";    // 右寄せ
            }
            else
            {   // 2次元

                //テーブルの作成
                this.myTable = new Table(2, 6);

                // テーブルの幅
                this.myTable.ColWidth[0] = 20.0; // 要素No
                this.myTable.ColWidth[1] = 45.0; // 格点No
                this.myTable.ColWidth[2] = this.myTable.ColWidth[1];
                this.myTable.ColWidth[3] = 60.0; // 部材長
                this.myTable.ColWidth[4] = 70.0; // 材料番号    
                this.myTable.ColWidth[5] = 70.0; // 材料名称

                switch (data.language)
                {
                    case "en":
                        this.title = "Member Data";
                        this.myTable[1, 0] = "No";
                        this.myTable[0, 1] = "　Node";
                        this.myTable[1, 1] = "Node-I";
                        this.myTable[1, 2] = "Node-J";
                        this.myTable[0, 3] = "Distance";
                        this.myTable[1, 3] = "(m)";
                        this.myTable[0, 4] = "Material";
                        this.myTable[1, 4] = "No";
                        this.myTable[0, 5] = "    Name of Material";
                        break;

                    case "cn":
                        this.title = "构件";
                        this.myTable[1, 0] = "No";
                        this.myTable[0, 1] = "　节点";
                        this.myTable[1, 1] = "I端";
                        this.myTable[1, 2] = "J端";
                        this.myTable[0, 3] = "构件长";
                        this.myTable[1, 3] = "(m)";
                        this.myTable[0, 4] = "材料编码";
                        this.myTable[0, 5] = "    材料名称";
                        break;

                    default:
                        this.title = "部材データ";
                        this.myTable[1, 0] = "No";
                        this.myTable[0, 2] = "　節点";
                        this.myTable[1, 1] = "I端";
                        this.myTable[1, 2] = "J端";
                        this.myTable[0, 3] = "L ";
                        this.myTable[1, 3] = "(m)";
                        this.myTable[0, 4] = "材料番号";
                        this.myTable[0, 5] = "    材料名称";
                        break;
                }

                // 表題の文字位置
                this.myTable.AlignX[0, 5] = "L";    // 左寄せ
                this.myTable.AlignX[0, 2] = "L";    // 左寄せ
                this.myTable.AlignX[1, 1] = "R";    // 右寄せ
                this.myTable.AlignX[1, 2] = "R";    // 右寄せ
                this.myTable.AlignX[0, 3] = "R";    // 右寄せ
                this.myTable.AlignX[1, 3] = "R";    // 右寄せ
            }
        }


        /// <summary>
        /// 1ページに入れるコンテンツを集計する
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents(Dictionary<string, Member> target)
        {
            int r = this.myTable.Rows;
            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable.Clone();
            table.ReDim(row: r + rows);

            table.RowHeight[r] = printManager.LineSpacing2;

            for (var i = 0; i < rows; i++)
            {
                string No = target.ElementAt(i).Key;
                Member item = target.ElementAt(i).Value;

                int j = 0;
                table[r, j] = No;
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = printManager.toString(item.ni);
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = printManager.toString(item.nj);
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = printManager.toString(this.GetMemberLength(No), 3);
                table.AlignX[r, j] = "R";
                j++;
                table[r, j] = printManager.toString(item.e) + "  ";
                //table.AlignX[r, j] = "R";
                j++;
                if (this.dimension == 3)
                {
                    table[r, j] = printManager.toString(item.cg, 3);
                    table.AlignX[r, j] = "R";
                    j++;
                }
                table[r, j] = "    " + printManager.toString(this.Element.GetElementName(item.e));
                table.AlignX[r, j] = "L";

                r++;
            }

            return table;
        }

        #region PrintableBaseA.printPDF()用メソッド定義
        protected override bool HasAnyData() => members.Count > 0;
        protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo)
        {
            // 部材長を取得できる状態にする
            Node = (InputNode)data.printDatas[InputNode.KEY];

            // 材料名称を取得できる状態にする
            Element = (InputElement)data.printDatas[InputElement.KEY];

            printInit(mc, data);

            titles = new[] { title, };
            headerRows = myTable.Rows;
            nupInfo = Table.OneUpInfo;
        }
        protected override Table GetTable() => getPageContents(members);
        #endregion

        #endregion


        #region 他のモジュールのヘルパー関数

        // 部材長に使う格点。部材の表を印刷しないとき（FrameG の入力の写し）に他の表が渡す。
        internal void UseNodes(InputNode node) => Node = node;

        /// <summary>
        /// 部材にの長さを取得する
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="memberNo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public double GetMemberLength(string memberNo)
        {
            var memb = this.GetMember(memberNo);

            if (memb == null)
                return double.NaN;

            if (memb.ni == null || memb.nj == null)
            {
                return double.NaN;
            }

            Vector3 iPos = this.Node.GetNodePos(memb.ni);
            Vector3 jPos = this.Node.GetNodePos(memb.nj);
            if (iPos == null || jPos == null)
            {
                return double.NaN;
            }

            double result = Math.Sqrt(Math.Pow(iPos.x - jPos.x, 2) + Math.Pow(iPos.y - jPos.y, 2) + Math.Pow(iPos.z - jPos.z, 2));

            return result;
        }

        /// <summary>
        /// 部材情報を取得する
        /// </summary>
        /// <param name="No">部材番号</param>
        /// <returns></returns>
        public Member GetMember(string No)
        {
            if (!this.members.ContainsKey(No))
            {
                return null;
            }
            return this.members[No];
        }

        #endregion
    }
}


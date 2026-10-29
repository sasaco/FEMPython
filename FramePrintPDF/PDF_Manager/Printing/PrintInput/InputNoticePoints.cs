using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    public class NoticePoint
    {
        public string m;   // 要素番号
        public double[] Points;
    }

    internal class InputNoticePoints : PrintableBaseA
    {
        public const string KEY = "notice_points";

        private List<NoticePoint> noticepoints = new List<NoticePoint>();

        internal IReadOnlyList<NoticePoint> Rows => noticepoints;

        // 要素情報
        private InputMember Member = null;

        public InputNoticePoints(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            //nodeデータを取得する
            JArray target = JArray.FromObject(value[KEY]);

            for (int i = 0; i < target.Count; i++)
            {
                JToken item = target[i];

                var np = new NoticePoint();

                np.m = dataManager.toString(item["m"]);

                var itemPoints = item["Points"];
                var _points = new List<double>();

                for (int j = 0; j < itemPoints.Count(); j++)
                {
                    var d = dataManager.parseDouble(itemPoints[j]);
                    _points.Add(d);
                }

                np.Points = _points.ToArray();

                this.noticepoints.Add(np);
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


        /// <summary>
        /// 印刷前の初期化処理
        /// </summary>
        private void printInit(PdfDocument mc, PrintData data)
        {
            this.dimension = data.dimension;


            //テーブルの作成
            this.myTable = new Table(2, 12);

            // テーブルの幅
            this.myTable.ColWidth[0] = 15.0; // 格点No
            this.myTable.ColWidth[1] = 40.0;
            this.myTable.ColWidth[2] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[3] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[4] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[5] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[6] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[7] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[8] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[9] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[10] = this.myTable.ColWidth[1];
            this.myTable.ColWidth[11] = this.myTable.ColWidth[1];

            // 表題
            this.myTable[1, 2] = "L1";
            this.myTable[1, 3] = "L2";
            this.myTable[1, 4] = "L3";
            this.myTable[1, 5] = "L4";
            this.myTable[1, 6] = "L5";
            this.myTable[1, 7] = "L6";
            this.myTable[1, 8] = "L7";
            this.myTable[1, 9] = "L8";
            this.myTable[1, 10] = "L9";
            this.myTable[1, 11] = "L10";

            switch (data.language)
            {
                case "en":
                    this.title = "Location";
                    this.myTable[0, 0] = "Member";
                    this.myTable[1, 0] = "No";
                    this.myTable[1, 1] = "Distance";
                    break;

                case "cn":
                    this.title = "焦距点";
                    this.myTable[0, 0] = "构件";
                    this.myTable[1, 0] = "编码";
                    this.myTable[1, 1] = "构件长";
                    break;

                default:
                    this.title = "着目点データ";
                    this.myTable[0, 0] = "部材";
                    this.myTable[1, 0] = "No";
                    this.myTable[1, 1] = "部材長";
                    break;
            }
            this.myTable.AlignX[0, 0] = "L";    // 左寄せ
            this.myTable.AlignX[1, 0] = "L";    // 右寄せ
        }


        /// <summary>
        /// 1ページに入れるコンテンツを集計する 3次元の場合
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents3D(List<NoticePoint> target)
        {
            int r = this.myTable.Rows;
            int rows = target.Count;

            // 行コンテンツを生成
            var table = this.myTable.Clone();
            table.ReDim(row: r + rows);

            for (var i = 0; i < target.Count; i++)
            {

                NoticePoint item = target[i];

                for (var j = 0; j < 3; j++)
                {
                    table[r, 0] = printManager.toString(item.m);
                    table.AlignX[r, j] = "R";
                    j++;
                    if (item.m != null)
                    {
                        table[r, j] = printManager.toString(this.Member.GetMemberLength(item.m), 3);
                        table.AlignX[r, j] = "R";
                    }
                    j++;

                    for (var k = 0; k < item.Points.Count(); k++)
                    {
                        if (k >= 10)
                        {
                            if (k % 10 == 0)
                            {
                                r++;
                                j = 2;
                                rows++;
                            }
                            table.ReDim(row: this.myTable.Rows + rows);
                        }
                        table[r, j] = printManager.toString(item.Points[k], 3);
                        table.AlignX[r, j] = "R";
                        j++;
                    }
                }
                r++;

            }
            table.RowHeight[2] = printManager.LineSpacing2; // 表題と body の間

            return table;
        }

        private List<NoticePoint> ReSize(List<NoticePoint> target)
        {
            int rows = target.Count;

            // 行コンテンツを生成
            var list = new List<NoticePoint>();

            var point_list1 = new List<Double>();
            var point_list2 = new List<Double>();

            foreach (Double Point in target[0].Points)
            {
                point_list1.Add(Point);
            }

            var list_rows = point_list1.Count / 10 + 1;

            for (var i = 0; i < list_rows; i++)
            {

                if (point_list1.Count == 0)
                    break;

                var _np = new NoticePoint();

                if (i == 0)
                {
                    _np.m = target[i].m;
                }

                for (int j = 0; j < 10; j++)
                {
                    point_list2.Add(point_list1.First());
                    point_list1.Remove(point_list1.First());

                    if (point_list1.Count == 0)
                        break;
                }

                _np.Points = point_list2.ToArray();
                point_list2.Clear();

                list.Add(_np);
            }

            return list;
        }

        #region PrintableBaseA.printPDF()用メソッド定義
        protected override bool HasAnyData() => noticepoints.Count > 0;
        protected override void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo)
        {
            // 要素を取得できる状態にする
            Member = (InputMember)data.printDatas[InputMember.KEY];
            // The member table may not have been printed (FrameG's input copy prints its own).
            Member.UseNodes((InputNode)data.printDatas[InputNode.KEY]);

            printInit(mc, data);

            titles = new[] { title, };
            headerRows = myTable.Rows;
            nupInfo = Table.OneUpInfo;
        }
        protected override Table GetTable() => getPageContents3D(noticepoints);
        #endregion

        #endregion
    }
}

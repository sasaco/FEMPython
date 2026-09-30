using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;


namespace PDF_Manager.Printing
{
    public class FixMember
    {
        public int row;
        public string m;   // 部材番号
        public double? length; // I端からのバネ区間長（m）。旧データでは未指定
        public double tx;
        public double ty;
        public double tz;
        public double tr;
    }

    internal class InputFixMember : PrintableBaseB
    {
        public const string KEY = "fix_member";

        private Dictionary<int, List<FixMember>> fixmembers = new Dictionary<int, List<FixMember>>();

        public InputFixMember(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            // データを取得する．
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();

            // データを抽出する
            for (var i = 0; i < target.Count; i++)
            {
                var key = dataManager.parseInt(target.ElementAt(i).Key);  // タイプ番号
                JArray FixM = JArray.FromObject(target.ElementAt(i).Value);

                var _fixnode = new List<FixMember>();

                for (int j = 0; j < FixM.Count; j++)
                {
                    JToken item = FixM[j];

                    var fm = new FixMember();

                    fm.row = int.TryParse(item["row"]?.ToString(), out int row) ? row : j + 1;
                    fm.m = dataManager.toString(item["m"]);
                    if (item["length"] is JToken length && length.Type != JTokenType.Null)
                    {
                        fm.length = length.Value<double>();
                        if (double.IsNaN(fm.length.Value) || double.IsInfinity(fm.length.Value) || fm.length <= 0)
                            throw new FormatException("fix_member.length must be a finite positive number.");
                    }
                    fm.tx = dataManager.parseDouble(item["tx"]);
                    fm.ty = dataManager.parseDouble(item["ty"]);
                    fm.tz = dataManager.parseDouble(item["tz"]);
                    fm.tr = dataManager.parseDouble(item["tr"]);

                    _fixnode.Add(fm);

                }
                this.fixmembers.Add(key, _fixnode.OrderBy(fm => fm.row).ToList());
            }

        }
        ///印刷処理

        ///タイトル
        private string title;
        ///２次元か３次元か
        private int dimension;
        ///テーブル
        private Table myTable;
        ///保存用テーブル
        private Table _table;


        ///印刷前の初期化処理
        ///
        private void printInit(PdfDocument mc, PrintData data)
        {
            this.dimension = data.dimension;

            if (this.dimension == 3)
            {
                ///テーブルの作成
                this.myTable = new Table(4, 6);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 20.0;//節点No
                this.myTable.ColWidth[1] = 55.0;//バネ区間長
                this.myTable.ColWidth[2] = 90.0;//部材軸方向
                this.myTable.ColWidth[3] = 90.0;//部材Y軸
                this.myTable.ColWidth[4] = 90.0;//部材Z軸
                this.myTable.ColWidth[5] = 90.0;//回転拘束

                this.myTable.RowHeight[1] = printManager.LineSpacing2;

                for (int col = 0; col < this.myTable.Columns; col++)
                    for (int row = 1; row < this.myTable.Rows; row++)
                        this.myTable.AlignX[row, col] = "R";
                this.myTable.AlignX[0, 0] = "L";
                this.myTable.AlignX[1, 0] = "L";

                this.myTable[3, 1] = "(m)";
                this.myTable[3, 2] = "(kN/m/m)";
                this.myTable[3, 3] = "(kN/m/m)";
                this.myTable[3, 4] = "(kN/m/m)";
                this.myTable[3, 5] = "(kN・m/rad/m)";

                switch (data.language)
                {
                    case "en":
                        this.title = "Spring";
                        this.myTable[1, 0] = "Member";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "Length";
                        this.myTable[1, 3] = "Displacement Restraint";
                        this.myTable[2, 2] = "Kv";
                        this.myTable[2, 3] = "Ky";
                        this.myTable[2, 4] = "Kz";
                        this.myTable[1, 5] = "Rotational Restraint";
                        break;

                    case "cn":
                        this.title = "弹簧";
                        this.myTable[1, 0] = "构件";
                        this.myTable[2, 0] = "编码";
                        this.myTable[1, 1] = "构件长度";
                        this.myTable[1, 3] = "位移约束";
                        this.myTable[2, 2] = "构件轴向";
                        this.myTable[2, 3] = "构件Y轴";
                        this.myTable[2, 4] = "构件Z轴";
                        this.myTable[1, 5] = "旋转约束";
                        break;

                    default:
                        this.title = "バネデータ";
                        this.myTable[1, 0] = "部材";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "部材長";
                        this.myTable[1, 3] = "変位拘束";
                        this.myTable[2, 2] = "部材軸方向";
                        this.myTable[2, 3] = "部材Y軸";
                        this.myTable[2, 4] = "部材Z軸";
                        this.myTable[1, 5] = "回転拘束";
                        break;
                }

            }
            else
            {
                ///テーブルの作成
                this.myTable = new Table(4, 8);

                ///テーブルの幅
                this.myTable.ColWidth[0] = 20.0;//部材No
                this.myTable.ColWidth[1] = 50.0;//バネ区間長
                this.myTable.ColWidth[2] = 80.0;//部材軸方向
                this.myTable.ColWidth[3] = 80.0;//部材直角方向
                for (int col = 0; col < 4; col++)
                    this.myTable.ColWidth[col + 4] = this.myTable.ColWidth[col];

                this.myTable.RowHeight[1] = printManager.LineSpacing2;

                for (int col = 0; col < this.myTable.Columns; col++)
                    for (int row = 1; row < this.myTable.Rows; row++)
                        this.myTable.AlignX[row, col] = "R";
                this.myTable.AlignX[0, 0] = "L";
                this.myTable.AlignX[1, 0] = "L";
                this.myTable.AlignX[1, 4] = "L";

                this.myTable[3, 1] = "(m)";
                this.myTable[3, 2] = "(kN/m/m)";
                this.myTable[3, 3] = "(kN/m/m)";
                for (int col = 0; col < 4; col++)
                    this.myTable[3, col + 4] = this.myTable[3, col];
                switch (data.language)
                {
                    case "en":
                        this.title = "Spring";
                        this.myTable[1, 0] = "Member";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "Length";
                        this.myTable[1, 3] = "Displacement Restraint";
                        this.myTable[2, 2] = "Ku";
                        this.myTable[2, 3] = "Kv";
                        break;

                    case "cn":
                        this.title = "弹簧";
                        this.myTable[1, 0] = "构件";
                        this.myTable[2, 0] = "编码";
                        this.myTable[1, 1] = "构件长度";
                        this.myTable[1, 3] = "位移约束";
                        this.myTable[2, 2] = "构件轴向";
                        this.myTable[2, 3] = "垂直于轴的方向";
                        break;

                    default:
                        this.title = "バネデータ";
                        this.myTable[1, 0] = "部材";
                        this.myTable[2, 0] = "No";
                        this.myTable[1, 1] = "部材長";
                        this.myTable[1, 3] = "変位拘束";
                        this.myTable[2, 2] = "部材軸方向";
                        this.myTable[2, 3] = "軸直角方向";
                        break;
                }
                for (int col = 0; col < 4; col++)
                {
                    this.myTable[1, col + 4] = this.myTable[1, col];
                    this.myTable[2, col + 4] = this.myTable[2, col];
                }

            }
        }

        /// <summary>
        /// 1ページに入れるコンテンツを集計する
        /// </summary>
        /// <param name="target">印刷対象の配列</param>
        /// <param name="rows">行数</param>
        /// <returns>印刷する用の配列</returns>
        private Table getPageContents(List<FixMember> target)
        {
            if (this.dimension == 3)
            {
                // 行コンテンツを生成
                var table = this.myTable.Clone();
                int r = this.myTable.Rows;
                int rows = target.Count;

                table.ReDim(row: r + rows);

                table.RowHeight[r] = printManager.LineSpacing2;

                for (var i = 0; i < rows; i++)
                {
                    var item = target[i];

                    int j = 0;
                    table[r, j] = printManager.toString(item.m);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.length, 3);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.tx, 3);
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.ty, 2, "E");
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.tz, 2, "E");
                    table.AlignX[r, j] = "R";
                    j++;
                    table[r, j] = printManager.toString(item.tr, 2);
                    table.AlignX[r, j] = "R";
                    j++;

                    r++;
                }

                return table;

            }
            else
            {

                int r = this.myTable.Rows;

                int columns = 2;
                var table = this.myTable.Clone();

                if (target.Count == 1)
                {
                    columns = 1;
                    table.ReDim(col: 4);
                }

                int count = this.myTable.Columns;
                int c = count / columns;

                //int rows = target.Count;
                //// 行コンテンツを生成
                //if (columns >= 1)
                //{
                //    rows = rows / columns;
                //    if (rows % columns != 0)
                //    {
                //        rows++;
                //    }
                //}

                //add rows
                int Rows = (target.Count + columns - 1) / columns;
                table.ReDim(row: r + Rows);

                table.RowHeight[r] = printManager.LineSpacing2;

                //int Rows = target.Count / columns;

                for (var i = 0; i < Rows; i++)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        var index = i + j * Rows; //左側：j=0 ∴index = i, 右側：j=1, ∴index = i+rows
                        if (target.Count <= index)
                            continue;

                        var item = target[index];

                        table[r + i, 0 + c * j] = printManager.toString(item.m);
                        table.AlignX[r + i, 0 + c * j] = "R";
                        table[r + i, 1 + c * j] = printManager.toString(item.length, 3);
                        table.AlignX[r + i, 1 + c * j] = "R";
                        table[r + i, 2 + c * j] = printManager.toString(item.tx, 3);
                        table.AlignX[r + i, 2 + c * j] = "R";
                        table[r + i, 3 + c * j] = printManager.toString(item.ty, 2, "E");
                        table.AlignX[r + i, 3 + c * j] = "R";
                    }
                }

                return table;


            }
        }

        #region PrintableBaseB.printPDF()用メソッド定義
        protected override bool HasAnyData() => fixmembers.Any();
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
            foreach (var tmp0 in fixmembers)
            {
                var table = getPageContents(tmp0.Value);
                table[0, 0] = tmp0.Key.ToString();

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
                // ケース全体が出力できなければ改ページ
                return printableRows[0] < tableRows;
            }
        }
        #endregion

        #region 他のモジュールのヘルパー関数

        // バネデータの取得
        public IReadOnlyDictionary<int, List<FixMember>> FixMembers => fixmembers;

        #endregion

        /*
            private Dictionary<string, object> value = new Dictionary<string, object>();
            List<string> title = new List<string>();
            List<List<string[]>> data = new List<List<string[]>>();


            value = value_;
            // elementデータを取得する．
            var target = JObject.FromObject(value["fix_member"]).ToObject<Dictionary<string, object>>();

            // 集まったデータはすべてここに格納する
            title = new List<string>();
            data = new List<List<string[]>>();


            for (int i = 0; i < target.Count; i++)
            {
                JArray Elem = JArray.FromObject(target.ElementAt(i).Value);

                // タイトルを入れる．
                switch (mc.language)
                {
                    case "ja":
                        title.Add("タイプ" + target.ElementAt(i).Key);
                        break;
                    case "en":
                        title.Add("Type" + target.ElementAt(i).Key);
                        break;

                }

                List<string[]> table = new List<string[]>();

                if (mc.dimension == 3)
                {
                    for (int j = 0; j < Elem.Count; j++)
                    {
                        JToken item = Elem[j];

                        string[] line = new String[5];

                        line[0] = dataManager.TypeChange(item["m"]);
                        line[1] = dataManager.TypeChange(item["tx"]);
                        line[2] = dataManager.TypeChange(item["ty"]);
                        line[3] = dataManager.TypeChange(item["tz"]);
                        line[4] = dataManager.TypeChange(item["tr"]);

                        table.Add(line);
                    }
                    data.Add(table);
                }
                else if (mc.dimension == 2)
                {
                    int bottomCell = mc.bottomCell * 2;

                    // 全部の行数
                    var row = Elem.Count;

                    var page = 0;
                    //var body = new ArrayList()

                    while (true)
                    {
                        if (row > bottomCell)
                        {
                            List<string[]> body = new List<string[]>();
                            var half = bottomCell / 2;
                            for (var l = 0; l < half; l++)
                            {
                                //　各行の配列開始位置を取得する（左段/右段)
                                var j = bottomCell * page + l;
                                var k = bottomCell * page + bottomCell / 2 + l;
                                //　各行のデータを取得する（左段/右段)
                                var targetValue_l = Elem[j];

                                string[] line = new String[6];
                                line[0] = dataManager.TypeChange(targetValue_l["m"]);
                                line[1] = dataManager.TypeChange(targetValue_l["tx"], 3);
                                line[2] = dataManager.TypeChange(targetValue_l["ty"], 3);                

                                var targetValue_r = Elem[k];
                                line[3] = dataManager.TypeChange(targetValue_r["m"]);
                                line[4] = dataManager.TypeChange(targetValue_r["tx"], 3);
                                line[5] = dataManager.TypeChange(targetValue_r["ty"], 3);
                          
                                body.Add(line);
                            }
                            data.Add(body);
                            row -= bottomCell;
                            page++;
                        }
                        else
                        {
                            List<string[]> body = new List<string[]>();

                            row = row % 2 == 0 ? row / 2 : row / 2 + 1;

                            for (var l = 0; l < row; l++)
                            {
                                //　各行の配列開始位置を取得する（左段/右段)
                                var j = bottomCell * page + l;
                                var k = j + row;
                                //　各行のデータを取得する（左段)
                                var targetValue_l = Elem[j];

                                string[] line = new String[6];
                                line[0] = dataManager.TypeChange(targetValue_l["m"]);
                                line[1] = dataManager.TypeChange(targetValue_l["tx"], 3);
                                line[2] = dataManager.TypeChange(targetValue_l["ty"], 3);

                                try
                                {
                                    //　各行のデータを取得する（右段)
                                    var targetValue_r = Elem[k];
                                    line[3] = dataManager.TypeChange(targetValue_r["m"]);
                                    line[4] = dataManager.TypeChange(targetValue_r["tx"], 3);
                                    line[5] = dataManager.TypeChange(targetValue_r["ty"], 3);

                                    body.Add(line);
                                }
                                catch
                                {
                                    line[3] = "";
                                    line[4] = "";
                                    line[5] = "";
                                    body.Add(line);
                                }
                            }
                            data.Add(body);
                            break;
                        }
                    }
                }
            }
                */
        /*
        public void FixMemberPDF(PdfDoc mc)
        {
            // 全行の取得
            int count = 20;
            for (int i = 0; i < title.Count; i++)
            {
                count += (data[i].Count + 7) * mc.single_Yrow ;
            }
            // 改ページ判定
            mc.DataCountKeep(count);

            //　ヘッダー
            string[,] header_content3D = {
                { "部材", "", "変位拘束", "", "回転拘束",},
                { "No", "部材軸方向", "部材Y軸", "部材Z軸", "" },
                {"","(kN/m/m)","(kN/m/m)","(kN/m/m)","(kN・m/rad/m)"}
            };
            string[,] header_content2D = {
                { "部材", "変位拘束", "","部材", "変位拘束", "" },
                { "No", "部材軸方向", "部材Y軸","No", "部材軸方向", "部材Y軸"},
                {"","(kN/m/m)","(kN/m/m)","","(kN/m/m)","(kN/m/m)"}
            };

            // ヘッダーのx方向の余白
            int[,] header_Xspacing3D ={
                { 10, 105, 140, 210, 280},
                { 10, 70, 140, 210, 280},
                { 10, 70, 140, 210, 280},
            };
            int[,] header_Xspacing2D ={
                { 10, 105, 140, 210, 305, 340},
                { 10, 70, 140, 210, 270, 340},
                { 10, 70, 140, 210, 270, 340},
            };

            // ボディーのx方向の余白　-1
            int[,] body_Xspacing3D = {
                { 17, 90, 160, 230, 300 }
            };
            int[,] body_Xspacing2D = {
                { 17, 90, 160, 230, 290, 360 }
            };

            string[,] header_content = mc.dimension == 3 ? header_content3D : header_content2D;
            int[,] header_Xspacing = mc.dimension == 3 ? header_Xspacing3D : header_Xspacing2D;
            int[,] body_Xspacing = mc.dimension == 3 ? body_Xspacing3D : body_Xspacing2D;
            mc.header_content = header_content;
            mc.header_Xspacing = header_Xspacing;
            mc.body_Xspacing = body_Xspacing;
       
            // タイトルの印刷
            switch (mc.language)
            {
                case "ja":
                    mc.PrintContent("バネデータ", 0);
                    break;
                case "en":
                    mc.PrintContent("MemberSpring DATA", 0);
                    //　ヘッダー
                    header_content3D[0, 0] = "Member";
                    header_content3D[0, 2] = "Displacement Restraint";
                    header_content3D[0, 4] = "Rotational Restraint";
     
                    header_content3D[1, 1] = "Kv";
                    header_content3D[1, 2] = "Ky";
                    header_content3D[1, 3] = "Kz";
                    header_content3D[1, 4] = "Kr";


                    header_content2D[0, 0] = "Member";
                    header_content2D[0, 1] = "Displacement Restraint";
                    header_content2D[0, 3] = "Member";
                    header_content2D[0, 4] = "Displacement Restraint";

                    header_content2D[1, 1] = "Kv";
                    header_content2D[1, 2] = "Ky";
                    header_content2D[1, 4] = "Kv";
                    header_content2D[1, 5] = "Ky";
                    break;
            }
            mc.CurrentRow(2);
            mc.CurrentColumn(0);


            for (int i = 0; i < data.Count; i++)
            {
                //  1タイプ内でページをまたぐかどうか
                mc.TypeCount(i, 6, data[i].Count, title[i]);

                // タイプの印刷
                mc.CurrentColumn(0);
                mc.PrintContent(title[i], 0);
                mc.CurrentRow(2);


                // ヘッダーの印刷
                mc.Header(header_content, header_Xspacing);

                for (int j = 0; j < data[i].Count; j++)
                {
                    for (int l = 0; l < data[i][j].Length; l++)
                    {
                        mc.CurrentColumn(body_Xspacing[0, l]); //x方向移動
                        mc.PrintContent(data[i][j][l]); // print
                    }
                    if (!(i == data.Count - 1 && j == data[i].Count - 1))
                    {
                        mc.CurrentRow(1); // y方向移動
                    }
                }
            }

        }
        */
    }

}

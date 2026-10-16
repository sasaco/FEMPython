using Newtonsoft.Json.Linq;
using PDF_Manager.Comon;
using PDF_Manager.Printing.Comon;
using PdfSharpCore.Drawing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PDF_Manager.Printing
{
    class DiagramResult : IPrintable
    {
        public const string KEY = "diagramResult";

        // 軸線を作成するのに必要な情報
        private DiagramFrame Frame = null;

        // 荷重情報
        //private InputLoadName LoadName = null;
        //// 断面力情報
        //private ResultFsec Fsec = null;
        //// 組合せ断面力情報
        //private ResultFsecCombine combFsec = null;
        //// ピックアップ断面力情報
        //private ResultFsecPickup pickFsec = null;

        // 出力情報
        private List<string> output = new List<string>() { "mz", "fy", "fx", "disg" };
        private CanonicalSectionDiagram canonical;

        public DiagramResult(Dictionary<string, object> value) 
        {
            if (!value.ContainsKey(KEY))
                return;

            //荷重図の設定データを取得する
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();
            if (CanonicalSectionDiagram.Enabled(target))
                canonical = new CanonicalSectionDiagram(target, value);

            // 骨組の描画クラスを生成する
            this.Frame = new DiagramFrame(target);

            // 出力する内容を決定する
            if (target.ContainsKey("output")) {
                var obj = target["output"];
                IEnumerable enumerable = obj as IEnumerable;
                if (enumerable != null)
                {
                    this.output = new List<string>();
                    foreach (var element in enumerable)
                    {
                        this.output.Add(element.ToString());
                    }
                }
            }
            if (this.output.Contains("disg")) {
                if (!value.ContainsKey(ResultDisg.KEY)) {
                    this.output.Remove("disg");
                } 
            }

        }

        /// <summary>
        /// 荷重図の作成
        /// </summary>
        /// <param name="_mc">キャンパス</param>
        /// <param name="class_set">入力データ</param>
        public void printPDF(PdfDocument mc, PrintData data, ref int indexPage)
        {
            if (canonical != null)
            {
                canonical.Print(mc, data, ref indexPage);
                return;
            }
            this.Frame.printInit(mc, data);
            this.Frame.SetLayout(mc);
            this.Frame.CalculateDiagramRect(out var topLeft, out var bottomRight);
            this.Frame.CalculateCenterPos(topLeft, bottomRight);
            this.Frame.CalculateScale(topLeft, bottomRight);

            // 断面力図を描く
            var Fsec = (ResultFsec)data.printDatas[ResultFsec.KEY];             // 断面力を取得
            var Disg = (ResultDisg)data.printDatas[ResultDisg.KEY];             // 変位量を取得
            var LoadName = (InputLoadName)data.printDatas[InputLoadName.KEY];   // 荷重名称を取得
            this.printFsec(mc, Fsec, Disg, LoadName, ref indexPage);

            // 組合せ断面力図を描く
            var combFsec = (ResultFsecCombine)data.printDatas[ResultFsecCombine.KEY];   // 組合せ断面力を取得
            var combName = (InputCombine)data.printDatas[InputCombine.KEY];             // 組合せ名称を取得
            this.printCombFsec(mc, combFsec, combName, ref indexPage);

            // ピックアップ断面力図を描く
            var pickFsec = (ResultFsecPickup)data.printDatas[ResultFsecPickup.KEY];     // ピックアップ断面力を取得
            var pickbName = (InputPickup)data.printDatas[InputPickup.KEY];              // ピックアップ名称を取得
            this.printPicFsec(mc, pickFsec, pickbName, ref indexPage);
        }

        /// <summary>
        /// ピックアップ断面力図を描く
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="combFsec"></param>
        /// <param name="pickbName"></param>
        /// 
        /// <remarks>組合せ断面力図を描く関数を利用する</remarks>
        private void printPicFsec(PdfDocument mc, ResultFsecCombine combFsec, InputPickup pickbName, ref int indexPage)
        {
            InputCombine combName = new InputCombine();
            foreach(var p in pickbName.pickups){
                var c = new Combine();
                c.name = p.Value.name;
                combName.combines.Add(p.Key, c);
            }
            this.printCombFsec(mc, combFsec, combName, ref indexPage);
        }


        /// <summary>
        /// 組合せ断面力図を描く
        /// </summary>
        /// <param name="mc">キャンパス</param>
        /// <param name="Fsec">断面力クラス</param>
        private void printCombFsec(PdfDocument mc, ResultFsecCombine combFsec, InputCombine combName, ref int indexPage)
        {
            var count = 0;
            var lastItem = false;

            // 断面力図を描く
            foreach (var fsec in combFsec.Fsecs)
            {
                count++;
                if (count == combFsec.Fsecs.Count) lastItem = true;

                // LoadName から同じキーの情報を
                string combNo = fsec.Key;
                var cfs = (FsecCombine)fsec.Value;
                int index = Convert.ToInt32(combNo);

                // ケース番号を印刷
                if (combName.combines.ContainsKey(index))
                {
                    var ln = combName.combines[index];
                    if (ln.name != null)
                        Text.PrtTextLoadName(mc, string.Format("CASE : {0}  {1}", combNo, ln.name));
                }

                // 何の図を出すのか決めている
                int k = 1;
                switch (this.Frame.mode)
                {
                    case Layout.SplitHorizontal:
                    case Layout.SplitVertical:
                        k = 2;
                        break;
                }
                for (int i = 0; i < this.output.Count; i += k)
                {
                    for (int j = 0; j < k; ++j)
                    {
                        if (this.output.Count <= i + j)
                            continue;
                        var key = this.output[i + j];
                        // タイトルを印刷`
                        this.printTitle(mc, key, j, 35, this.Frame.mode);
                        // 骨組の描写
                        this.Frame.printFrame(j, true);

                        List<Fsec> fs;
                        if (key == "mz")
                        {
                            fs = cfs.mz_min;
                            fs.AddRange(cfs.mz_max);
                        }
                        else if (key == "fy")
                        {
                            fs = cfs.fy_min;
                            fs.AddRange(cfs.fy_max);
                        }
                        else if (key == "fx")
                        {
                            fs = cfs.fx_min;
                            fs.AddRange(cfs.fx_max);
                        }
                        else
                        {
                            continue;
                        }
                        // 骨組みを印字する
                        this.printFsec(fs, key);
                    }
                    // 改ページ
                    if (!(lastItem && i == this.output.Count - 1))
                        mc.NewPage(ref indexPage);
                }

            }
        }



        /// <summary>
        /// 基本ケース断面力図を描く
        /// </summary>
        /// <param name="mc">キャンパス</param>
        /// <param name="Fsec">断面力クラス</param>
        /// <param name="Disg">変位量クラス</param>
        private void printFsec(PdfDocument mc, ResultFsec Fsec, ResultDisg Disg, InputLoadName LoadName, ref int indexPage)
        {
            var count = 0;
            var lastItem = false;

            // ケース数を調べる

            // 断面力図を描く
            foreach (var fsec in Fsec.fsecs)
            {
                count++;
                if (count == Fsec.fsecs.Count) lastItem = true;

                // LoadName から同じキーの情報を
                 string caseNo = fsec.Key;
                var fmp = fsec.Value;

                int index = Convert.ToInt32(caseNo);

                // ケース番号を印刷
                LoadName ln = null;
                if (LoadName.loadnames.TryGetValue(index, out ln))
                    if (ln != null)
                        Text.PrtTextLoadName(mc, string.Format("CASE : {0}  {1} :{2}", caseNo, ln.name, ln.symbol));
                if (ln == null)
                    ln = new LoadName() { name = "" };

                // LL：連行荷重の時 -----------------------------------------------------------
                if (fmp.GetType().Name == new Dictionary<string, object>().GetType().Name)
                {
                    // printCombFsec 関数に印刷してもらう
                    var dct1 = new Dictionary<string, object>() { { caseNo, fmp } };
                    var dct2 = new List<string[]>() { new string[] { caseNo, ln.name } };
                    var dct3 = new Dictionary<string, object>(){
                        { "fsecCombine", dct1 },
                        { "fsecCombineName", dct2 }
                    };

                    var combFsec = new ResultFsecCombine(dct3);
                    var combName = new InputCombine(dct3);

                    this.printCombFsec(mc, combFsec, combName, ref indexPage);
                    continue;
                }
                // ---------------------------------------------------------------------------


                var fs = (List<Fsec>)fmp;


                // 何の図を出すのか決めている
                int k = 1;
                switch (this.Frame.mode)
                {
                    case Layout.SplitHorizontal:
                    case Layout.SplitVertical:
                        k = 2;

                        break;
                }
                for (int i = 0; i < this.output.Count; i += k)
                {
                    for (int j = 0; j < k; ++j)
                    {
                        if (this.output.Count <= i + j)
                            continue;
                        var key = this.output[i + j];
                        // タイトルを印刷
                        if(k != 2)
                            this.printTitle(mc, key, j, 20, this.Frame.mode);
                        else
                            this.printTitle(mc, key, j, 40, this.Frame.mode);
                        // 骨組の描写
                        this.Frame.printFrame(j, true);
                        if(key == "disg")
                        {
                            // 変位図を印字する
                            if (!Disg.disgs.ContainsKey(caseNo))
                                continue;
                            var ds = (List<Disg>)Disg.disgs[caseNo];
                            this.printDisg(ds, key);
                        }
                        else
                        {
                            // 断面力を印字する
                            this.printFsec(fs, key);
                        }
                    }
                    // 改ページ
                    if (!(lastItem && i == this.output.Count - 1))
                        mc.NewPage(ref indexPage);
                }

            }
        }
        
        /// <summary>
        /// 変位図を印字する
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="key"></param>
        private void printDisg(List<Disg> ds, string key)
        {
            // 変位量の描写用のペン設定
            this.Frame.canvas.mc.xpen = new XPen(XColors.Red, 0.2);

            // 断面力の縮尺を計算する
            var max_value = 0.0;
            foreach (Disg d in ds)
                max_value = Math.Max(Math.Max(Math.Abs(d.dx), Math.Abs(d.dy)), max_value);

            // 断面力の最大値を 50pt とする
            var margin = Math.Min(printManager.padding.Left, Math.Min(
                                  printManager.padding.Right, Math.Min(
                                  printManager.padding.Bottom,
                                  printManager.padding_Top)));
            var disgScaleX = margin / max_value;
            var disgScaleY = margin / max_value;

            // 要素を取得できる状態にする
            foreach (var mm in this.Frame.Member.members.Values)
            {
                // 文字
                // 文字の角度を決定する
                var radian = (-1 * mm.radian) - Math.PI / 2;

                var pi = this.Frame.Node.GetNodePos(mm.ni);
                var pj = this.Frame.Node.GetNodePos(mm.nj);

                var di = ds.Find(n => n.id == mm.ni);
                var dj = ds.Find(n => n.id == mm.nj);

                if (di == null || dj == null)
                    continue;

                // 要素長さ
                var L = mm.L;

                // 座標変換マトリクス
                var t = mm.t;

                // 変位図は 20分割
                var Division = 20;
                if (di.rx == dj.rx && di.ry == dj.ry && di.rz == dj.rz)
                    Division = 1;

                //// 全体座標系での節点変位を取得
                var xi = pi.x + di.dx * disgScaleX;
                var yi = (this.Frame.isOlderVer2 * pi.y) + di.dy * disgScaleY;

                var xj = pj.x + dj.dx * disgScaleX;
                var yj = (this.Frame.isOlderVer2 * pj.y) + dj.dy * disgScaleY;

                // 変位を要素座標系に変換
                double[,] MatrixTimesMatrix(double[,] A, double[,] B)
                {
                    double[,] product = new double[A.GetLength(0), B.GetLength(1)];
                    for (int i = 0; i < A.GetLength(0); i++)
                    {
                        for (int j = 0; j < B.GetLength(1); j++)
                        {
                            for (int k = 0; k < A.GetLength(1); k++)
                            {
                                product[i, j] += A[i, k] * B[k, j];
                            }
                        }
                    }
                    return product;
                }

                double[,] dge;
                dge = new double[,]{ { di.dx, dj.dx }, { di.dy, dj.dy }, { di.rz, dj.rz } };

                double[,] de = new double[3, 2];
                de = MatrixTimesMatrix(t, dge);

                // 補間点の節点変位の計算
                Vector3[] positions = new Vector3[Division+1];
                for (var j = 0; j <= Division; j++)
                {
                    double n = (double) j / Division;
                    double xhe = (1 - n) * de[0,0] + n * de[0,1];
                    double yhe = (1 - 3 * Math.Pow(n, 2) + 2 * Math.Pow(n, 3)) * de[1,0] + L * (n - 2 * Math.Pow(n, 2) + Math.Pow(n, 3)) * de[2,0]
                      + (3 * Math.Pow(n, 2) - 2 * Math.Pow(n, 3)) * de[1,1] + L * (0 - Math.Pow(n, 2) + Math.Pow(n, 3)) * de[2,1];

                    // 全体座標系への変換
                    double xhg = t[0,0] * xhe + t[1,0] * yhe;
                    double yhg = t[0,1] * xhe + t[1,1] * yhe;

                    positions[j] = new Vector3 { x = xhg, y = yhg };
                }

                double xx = double.NaN;
                double yy = double.NaN;
                double max = double.NaN;
                double y_ = double.NaN;
                double x_ = double.NaN;

                for (int k = 0; k <= Division; k++)
                {
                    double xhg = positions[k].x;
                    double yhg = positions[k].y * this.Frame.isOlderVer2;

                    // 補間点の変位を座標値に付加
                    double n = (double) k / Division;
                    double xk = (1 - n) * pi.x + n * pj.x;
                    double yk = (1 - n) * pi.y + n * pj.y;
                    double x2 = (xk - this.Frame.CenterPos.X) * this.Frame.ScaleX + xhg * disgScaleX;// ;
                    double y2 = -((this.Frame.isOlderVer2 * yk) - this.Frame.CenterPos.Y) * this.Frame.ScaleY - yhg * disgScaleY;// ;

                    // 2点を結ぶ直線を引く
                    if (!double.IsNaN(xx))
                        this.Frame.canvas.printLine(xx, yy, x2, y2);

                    if (k == 0)
                    {
                        if(radian != 0)
                        {
                            max = Math.Abs(yhg);
                        }
                        else
                        {
                            max = Math.Abs(xhg);
                        }
                        y_ = y2;
                        x_ = x2;
                    }
                    else
                    {
                        if(radian != 0)
                        {
                            max = Math.Max(Math.Abs(yhg), Math.Abs(max));
                            if (max == Math.Abs(yhg))
                            {
                                y_ = y2;
                                x_ = x2;
                            }
                        }
                        else
                        {
                            max = Math.Max(Math.Abs(xhg), Math.Abs(max));
                            if (max == Math.Abs(xhg))
                            {
                                y_ = y2;
                                x_ = x2;
                            }
                        }
                    }
                    if(k == Division)
                        this.Frame.canvas.printText(x_, y_, string.Format("{0:0.00}", max), radian, this.Frame.canvas.mc.font_got);

                    xx = x2;
                    yy = y2;
                }
            }
        }

        /// <summary>
        /// 断面力を印字する
        /// </summary>
        /// <param name="fsec"></param>
        internal static List<Fsec> DrawingRows(List<Fsec> rows)
        {
            var drawing = new List<Fsec>(rows.Count);
            foreach (var row in rows)
                if (row.occurrence <= 1) drawing.Add(row);
            return drawing;
        }

        private void printFsec(List<Fsec> fsec, string key)
        {
            // Tables retain both report occurrences. Draw each canonical occurrence once;
            // unmarked legacy rows and distinct envelope/one-sided vectors remain intact.
            fsec = DrawingRows(fsec);
            // 断面力の描写用のペン設定
            this.Frame.canvas.mc.xpen = new XPen(XColors.Blue, 0.2);

            // 断面力の縮尺を計算する
            var max_value = 0.0;
            foreach (Fsec f in fsec)
                max_value = Math.Max(Math.Abs(f.getValue2D(key)), max_value);

            // 断面力の最大値を 50pt とする
            var margin = Math.Min(printManager.padding.Left, Math.Min(
                                  printManager.padding.Right, Math.Min(
                                  printManager.padding.Bottom, 
                                  printManager.padding_Top)));
            var fsecScale = margin / max_value;

            // 描画中の要素情報
            Member m = null;
            Vector3 pi = new Vector3();  // 着目点位置
            double xx = double.NaN;
            double yy = double.NaN;

            foreach (Fsec f in fsec)
            {
                // 部材情報をセットする
                if (f.m.Length > 0)
                {
                    m = this.Frame.Member.GetMember(f.m);//任意の要素を取得

                    if (m == null)
                        continue;   // 有効な部材じゃない

                    // 断面力の線を書かないフラグ
                    xx = double.NaN;
                    yy = double.NaN;

                    //要素の節点i,jの情報を取得
                    pi = this.Frame.Node.GetNodePos(m.ni);   // 描画中の要素のi端座標情報
                }

                // 断面力の位置を決定する
                Vector3 pos = new Vector3(); 
                if (f.n.Length <= 0)
                {  // 部材途中の着目点位置
                    pos.x = pi.x + f.l * m.t[0, 0];
                    pos.y = pi.y + f.l * m.t[0, 1];
                }
                else
                {   // 部材端点位置
                    pos = this.Frame.Node.GetNodePos(f.n);
                }
                if (pos == null)
                    continue;   


                //荷重の大きさを取得
                var Value = f.getValue2D(key); 
                
                //荷重の大きさ(線の長さ)の時の座標計算
                var fxg = -m.t[1, 0] * Value;
                var fyg = -m.t[1, 1] * Value * this.Frame.isOlderVer2;

                //n スケール調整
                var x1 = (pos.x - this.Frame.CenterPos.X) * this.Frame.ScaleX;
                var y1 = -((this.Frame.isOlderVer2 * pos.y) - this.Frame.CenterPos.Y) * this.Frame.ScaleY;
                var x2 = x1 - fxg * fsecScale;
                var y2 = y1 + fyg * fsecScale;

                // 2点を結ぶ直線を引く
                if (!double.IsNaN(xx))
                    this.Frame.canvas.printLine(xx, yy, x2, y2);

                // 部材から垂線を引く
                if (Value != 0)
                {
                    this.Frame.canvas.printLine(x1, y1, x2, y2);
                    // 文字
                    // 自動で設けた着目点は出力対象外
                    if (!f.dummy)
                    {
                        // 文字の角度を決定する
                        var radian = this.Frame.isOlderVer2 * (-1 * m.radian) - Math.PI / 2;

                        this.Frame.canvas.printText(x2, y2, string.Format("{0:0.00}", Value), radian, this.Frame.canvas.mc.font_got);
                    }
                }
                xx = x2;
                yy = y2;
            }
        }


        /// <summary>
        /// 曲げモーメント図、せん断力図などのタイトルを印刷する
        /// </summary>
        private void printTitle(PdfDocument mc, string key, int index, int? add = 0, Layout? pageLayout = null)
        {
            // タイトルを印字する位置の設定
            var center = this.Frame.canvas.Center(index);
            var Area = this.Frame.canvas.areaSize;           
            mc.currentPos.Y = center.Y - Area.Height / 2;
            mc.currentPos.Y -= printManager.padding_Top;           

            mc.currentPos.Y += printManager.FontHeight + printManager.LineSpacing2 + Convert.ToInt32(add);
            mc.currentPos.X = center.X - Area.Width / 2;
            if (pageLayout == Layout.SplitHorizontal || pageLayout == Layout.Default)
            {
                mc.TitleArea = new XPoint(mc.currentPos.X, mc.currentPos.Y);
            }
            else
                mc.TitleArea = new XPoint(mc.currentPos.X, printManager.FontHeight + printManager.LineSpacing2 + Convert.ToInt32(add));
            // タイトルを印字する
            switch (this.Frame.language)
            {
                case "en":
                    if (key == "mz")
                        Text.PrtText(mc, "Bending moment");
                    if (key == "fy")
                        Text.PrtText(mc, "Shear force");
                    if (key == "fx")
                        Text.PrtText(mc, "Axcial force");
                    if (key == "disg")
                        Text.PrtText(mc, "Displacement");
                    break;

                case "cn":
                    if (key == "mz")
                        Text.PrtText(mc, "弯 矩 图");
                    if (key == "fy")
                        Text.PrtText(mc, "剪 力 图");
                    if (key == "fx")
                        Text.PrtText(mc, "轴 向 力 图");
                    if (key == "disg")
                        Text.PrtText(mc, "位 移 图");
                    break;

                default:
                    if (key == "mz")
                        Text.PrtText(mc, "曲げモーメント図");
                    if (key == "fy")
                        Text.PrtText(mc, "せん断力図");
                    if (key == "fx")
                        Text.PrtText(mc, "軸方向力図");
                    if (key == "disg")
                        Text.PrtText(mc, "変 位 図");
                    break;
            }



        }
    }
}

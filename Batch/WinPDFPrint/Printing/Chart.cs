using C1.Win.C1Chart;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WinPDFPrint.Printing
{
    public class Chart
    {
        /// <summary>グラフの幅</summary>
        public float width = 450;     
        /// <summary>グラフの高</summary>
        public float height = 400;     
        /// <summary>グラフの余白</summary>
        public float padingX = 60;
        public float padingY = 30;

        public string AxisYtitle = "Kh";
        public string AxisXtitle = "δ(mm)";

        private float TopScale = 1.4F;          //荷重～変位曲線の上部の空きで、表全体に対する縮尺
        private float RightScale = 1.0F;        //荷重～変位曲線の右側の空きで、表全体に対する縮尺

        private float LineThickness = 1.5F;         //荷重～変位曲線の線幅
        private float SubsidyLineThickness = 0.5F;  //降伏点などの補助線の線幅
        private float NoticeLineThickness = 0.3F;   //引き出しの線幅
        private float DefaultNoticeLineLength = 30; //引き出しの初期長さ
        private float GridMajorThickness = 1;       //軸の線幅
        private float GridMinorThickness = 0.2F;    //目盛の線幅
        private int GridMinorLineInterval = 3;      //目盛の点線の細かさ

        private Dictionary<int, string> _NoticeStr1; // 荷重～変位曲線から引き出して描く(key=step, value=内容)
        private Dictionary<int, string> _NoticeStr2; // 荷重～変位曲線の上に描く(key=step, value=内容)
        private List<NoticeStrEx> _NoticeStrEx;      // 荷重～変位曲線から引き出して描く

        private List<LineInfo> _Lines;              //荷重～変位曲線に追加する直線
        private List<LineInfo> _UnCrossingLines;    //引き出しが交差してはいけない直線群
        private int UnCrossingCheckLimitCount = 150; //引き出しの思考錯誤の制限回数

        private const float DEFAULT_RAD_RIGHT_DOWN = 300;
        private const float DEFAULT_RAD_LEFT_UP = 150;
        private const float DEFAULT_RAD_RIGHT_UP = 30;
        private const float DEFAULT_RAD_LEFT_DOWN = 240;

        #region アクセサメソッド
        private PointF[] _points;
        private float maxX_points
        {
            get
            {
                float result = 0;
                foreach (PointF p in _points)
                    result = Math.Max(result, Math.Abs(p.X));
                return result;
            }
        }
        private float maxY_points
        {
            get
            {
                float result = 0;
                foreach (PointF p in _points)
                    result = Math.Max(result, Math.Abs(p.Y));

                foreach (var l in _Lines) {
                    result = Math.Max(result, l._p1.Y * 0.9f);
                    result = Math.Max(result, l._p2.Y * 0.9f);
                }
                return result;
            }
        }

        /// <summary>右上がり↗ の引き出しにおいて「右下に引き出す」「左上に引き出す」を分けるY の値</summary>
        private float m_SeparateYRiseNoticeLine;       
        public float SeparateYRiseNoticeLine
        {
            get
            {
                // 値がセットされていなければセットして返す
                if (m_SeparateYRiseNoticeLine == -1) 
                {    
                    m_SeparateYRiseNoticeLine = maxY_points / 2;
                }
                return m_SeparateYRiseNoticeLine;
            }
            set{
                m_SeparateYRiseNoticeLine = value; 
            }
        }

        /// <summary>右下がり↘ の引き出しにおいて「右上に引き出す」「左下に引き出す」を分けるY の値</summary>
        private float m_SeparateYDropNoticeLine;
        private float SeparateYDropNoticeLine
        {
            get{

                // 値がセットされていなければセットして返す
                if (m_SeparateYDropNoticeLine == -1)    
                {

                    if (_points.Count() <= 0 || _points ==null) { return 0; }

                    int j = 0;
                    float mx = 0;
                    for (int i = 0; i < _points.Count() - 1; i++)
                    {
                        if (mx < _points[i].Y)
                        {
                            mx = Math.Max(mx, _points[i].Y);
                            j = i;
                        }
                    }
                    float mn = float.MaxValue;
                    for (int i = j; i < _points.Count() - 1; i++)
                    {
                        if (mn > _points[i].Y)
                        {
                            mn = Math.Min(mn, _points[i].Y);
                        }
                    }
                    if (mx > mn)
                        m_SeparateYDropNoticeLine = mn + ((mx - mn) / 3);
                }

                return m_SeparateYDropNoticeLine;
            }
            set
            {
                m_SeparateYDropNoticeLine = value;
            }
        }
        #endregion
    
        #region "引き出しの向き"
        enum LeaderDirection {
            RightUp = 1 ,   // 右上
            LeftUP      ,   // 左上
            RightDown   ,   // 右下
            LeftDown    ,   // 左下
        }
        #endregion

        /// <summary>コンストラクタ</summary>
        public Chart()
        {
            _NoticeStr1 = new Dictionary<int, string>();
            _NoticeStr2 = new Dictionary<int, string>();
            _NoticeStrEx = new List<NoticeStrEx>();
            _Lines = new List<LineInfo>();
            _UnCrossingLines = new List<LineInfo>();
            SeparateYRiseNoticeLine = -1;
            SeparateYDropNoticeLine = -1;
        }

        /// <summary>チャートに荷重～変位曲線データを追加</summary>
        /// <param name="points">荷重～変位曲線データ</param>
        /// <param name="NoticeStr">引き出し情報(step, 引き出し文字列)</param>
        public void AddData(PointF[] points, Dictionary<int, string> NoticeStr)
        {

            
            if (points.Count() <= 0)
                return;
            if (NoticeStr == null)
                NoticeStr = new Dictionary<int, string>();

            this._points = new PointF[points.Count()];
            for (int i = 0; i < points.Count(); i++)
            {
                this._points[i] = points[i];
            }
            foreach ( var ns in NoticeStr)
            {
                if( ns.Key >= 0 )
                {
                    this._NoticeStr1.Add(ns.Key,ns.Value);
                }else{
                    this._NoticeStr2.Add(-ns.Key, ns.Value);
                }
            }
        }

        public void AddDataEx(PointF point, float StepF, string NoticeStr)
        {
            this._NoticeStrEx.Add(new NoticeStrEx(point, StepF, NoticeStr));
        }

        /// <summary>チャートに直線を追加</summary>
        /// <param name="p1">直線の始点</param>
        /// <param name="p2">直線の終点</param>
        /// <param name="NoticeStrY">始点(p1)位置のY軸に表示する文字</param>
        /// <param name="NoticeStrX">始点(p1)位置のX軸に表示する文字</param>
        /// <param name="CrossingLines">[省略可]/引き出しが交差してはいけない直線として登録するか</param>
        public void AddLine(PointF p1, PointF p2, string NoticeStrY, string NoticeStrX, bool CrossingLines = false, float dY = 0)
        {
            _Lines.Add(new LineInfo(p1, p2, NoticeStrY, NoticeStrX, dY));
            //引き出しが交差してはいけない直線として登録
            if (CrossingLines == true) 
                _UnCrossingLines.Add(new LineInfo(p1, p2, "", ""));
        }

        //チャートを印刷
        internal void PrintChart(PdfDocument _myCanvas)
        {
            #region 最後に元に戻す値をスタック --------------------------------------------------------------------------------------------------------------
            _myCanvas.CurrentY += _myCanvas.referenceSize.Height * 2;
            float CurX = _myCanvas.CurrentX;
            float CurY = _myCanvas.CurrentY;
            #endregion

            #region パラメータのチェック -------------------------------------------------------------------------------------------------------------------------
            if (this._points == null)
                return;
            if (this._points.Count() < 1)
                return;
            if (this._NoticeStr1 == null)
                this._NoticeStr1 = new Dictionary<int, string>();
            if (this._NoticeStr2 == null)
                this._NoticeStr2 = new Dictionary<int, string>();
            #endregion

            #region 外形を描く(基準位置の決定) ------------------------------------------------------------------------------------------------------------------------------
            RectangleF position = new RectangleF(_myCanvas.CurrentX, _myCanvas.CurrentY, this.width, this.height);
            float X1 = position.Left + this.padingX;
            float X2 = position.Right; // -this.pading;
            float Y1 = position.Bottom - this.padingY;
            float Y2 = position.Top;   // -this.pading;

            Shape.DrawLine(_myCanvas, new PointF(X1, Y1), new PointF(X1, Y2), GridMajorThickness);
            Shape.DrawLine(_myCanvas, new PointF(X1, Y1), new PointF(X2, Y1), GridMajorThickness);
            Shape.DrawLine(_myCanvas, new PointF(X1, Y2), new PointF(X2, Y2), GridMinorThickness);
            Shape.DrawLine(_myCanvas, new PointF(X2, Y1), new PointF(X2, Y2), GridMinorThickness);

            _myCanvas.CurrentX = X1 - Text.MeasureString(_myCanvas, AxisYtitle).Width;
            _myCanvas.CurrentY = Y2 - _myCanvas.referenceSize.Height * 1.5F;
            Text.PrtText(_myCanvas, AxisYtitle);
            _myCanvas.CurrentX = X2 + _myCanvas.referenceSize.Width;
            _myCanvas.CurrentY = Y1;
            Text.PrtText(_myCanvas, AxisXtitle);

            _myCanvas.CurrentX = CurX;
            _myCanvas.CurrentY = CurY;

            #endregion

            #region 軸目盛を描く(縮尺の決定) -----------------------------------------------------------------------------------------------------------
            float maxX = maxX_points;
            float maxY = maxY_points;
            //Y軸目盛の最大値を決定する
            float TopY = maxY * TopScale;   //最大値
            //最大値の少数桁数を調べる
            int indexY = -5;
            float keyY = (float)Math.Pow(10,indexY);
            do {
                indexY++;
                keyY = (float)Math.Pow(10, indexY);
            } while (TopY > keyY);
            keyY *= 0.05F;
            int GridMinorNumY = (int)RoundUp(TopY / keyY, 0);   //目盛数
            float MaxGridValueY = GridMinorNumY * keyY;         //目盛の最大値
            float IntervalY = MaxGridValueY / GridMinorNumY;    //目盛間隔
            float scaleY = (Y1 - Y2) / MaxGridValueY;               //グラフの大きさに対するスケール
            //Y軸目盛を描く
            for (int i = 1; i < GridMinorNumY; i++){
                float gMinorY = Y1 - (IntervalY * i * scaleY);
                Shape.DrawDashLine(_myCanvas, new PointF(X1, gMinorY), new PointF(X2, gMinorY), GridMinorThickness, GridMinorLineInterval);
                if (i % 2 != 1 ){
                    string sy = (IntervalY * i).ToString("F3");
                    SizeF printsize = _myCanvas.MeasureString(sy +" ");
                    _myCanvas.CurrentX = X1 - printsize.Width - _myCanvas.referenceSize.Width;
                    _myCanvas.CurrentY = gMinorY -( _myCanvas.referenceSize.Height / 2);
                    Text.PrtText(_myCanvas, sy);
                }
            }
            string syn = MaxGridValueY.ToString();
            SizeF printsizen = _myCanvas.MeasureString(syn);
            _myCanvas.CurrentX = X1 - printsizen.Width - _myCanvas.referenceSize.Width;
            _myCanvas.CurrentY = Y2 - (_myCanvas.referenceSize.Height / 2);
            Text.PrtText(_myCanvas, syn);

            //X軸目盛の最大値を決定する
            float RightX = maxX * RightScale;   //最大値
            //分割の目安 GridMinorNum に近いものをさがす
            int indexX = -3;
            float keyX = (float)Math.Pow(10, indexX);
            do{
                indexX++;
                keyX = (float)Math.Pow(10, indexX);
            } while (RightX > keyX);
            keyX *= 0.05F;
            int GridMinorNumX = (int)RoundUp(RightX / keyX, 0); //目盛数
            float MaxGridValueX = GridMinorNumX * keyX;         //目盛の最大値
            float IntervalX = MaxGridValueX / GridMinorNumX;    //目盛間隔
            float scaleX = (X2 - X1) / MaxGridValueX;                   //グラフの大きさに対するスケール
            //X軸目盛を描く
            for (int i = 1; i < GridMinorNumX; i++){
                float gMinorX = X1 + (IntervalX * i * scaleX);
                Shape.DrawDashLine(_myCanvas, new PointF(gMinorX, Y1), new PointF(gMinorX, Y2), GridMinorThickness, GridMinorLineInterval);
                if (i % 2 != 1 ){
                    string sx = (IntervalX * i).ToString();
                    SizeF printsize = _myCanvas.MeasureString(sx);
                    _myCanvas.CurrentX = gMinorX - printsize.Width / 2;
                    _myCanvas.CurrentY = Y1; 
                    Text.PrtText(_myCanvas, sx);
                }
            }
            #endregion

            #region 荷重～変位曲線を描く------------------------------------------------------------------------------------------------------
            for (int i = 1; i < this._points.Count(); i++){
                PointF p1 = this._points[i - 1];
                PointF p2 = this._points[i];
                float px1 = X1 + (p1.X * scaleX);
                float px2 = X1 + (p2.X * scaleX);
                float py1 = Y1 - (p1.Y * scaleY);
                float py2 = Y1 - (p2.Y * scaleY);
                Shape.DrawLine(_myCanvas, new PointF(px1, py1), new PointF(px2, py2), LineThickness);
            }
            #endregion

            #region 外部登録された交差判定線をスケーリングする------------------------------------------------------------------------------------------------------
 
            foreach (var ucl in _UnCrossingLines)
            {
                ucl._p1.X = X1 + (ucl._p1.X * scaleX);
                ucl._p1.Y = Y1 + (ucl._p1.Y * scaleY);
                ucl._p2.X = X1 + (ucl._p2.X * scaleX);
                ucl._p2.Y = Y1 + (ucl._p2.Y * scaleY);
            }
 
            #endregion

            #region 引き出しを描く-------------------------------------------------------------------------------------------------------------

            SortedList<int, NoticeInfo> lstRightDown = new SortedList<int, NoticeInfo>();   //  右上がり↗ 右下に引き出す
            SortedList<int, NoticeInfo> lstLeftUp = new SortedList<int, NoticeInfo>();      //  右上がり↗ 左上に引き出す
            SortedList<int, NoticeInfo> lstRightUp = new SortedList<int, NoticeInfo>();     //  右下がり↘ 右上に引き出す
            SortedList<int, NoticeInfo> lstLeftDown = new SortedList<int, NoticeInfo>();    //  右下がり↘ 左下に引き出す

            int step;
            float rad;
            float LineLength;
            PointF cp = new PointF(X1, Y2);

            #region 右上がり↗ と 右下がり↘ を分ける。
            Dictionary<int, NoticeInfo> RiseList = new Dictionary<int, NoticeInfo>();       /*右上がり↗ */
            Dictionary<int, NoticeInfo> DropList = new Dictionary<int, NoticeInfo>();       /*右下がり↘ */
            foreach (var n in this._NoticeStr1)
            {
                step = n.Key;          //ステップ数
                NoticeInfo Value = new NoticeInfo(n.Value);
                //勾配を決定するための２点を選択する。
                PointF pt1, pt2;
                pt1 = getSlopPoint(n.Key, _points, 1);
                pt2 = getSlopPoint(n.Key, _points, 2);
                //荷重変位曲線の勾配を計算する。
                double dX = (pt2.X - pt1.X) * scaleX;
                double dY = (pt2.Y - pt1.Y) * scaleY;
                Value.Slope1 = (float)Math.Atan(dY / dX);
                //引き出し線の原点(pn1)をセットする
                Value.pn0 = this._points[step];
                Value.pn1 = Value.pn0;
                Value.pn1.X *= scaleX;
                Value.pn1.Y *= scaleY;
                // 引き出しの方向を決定
                switch (getLeaderDirection(Value.pn0.Y, Value.Slope1 > 0))
                {
                    case LeaderDirection.LeftUP:    // 左上
                        SortedListAdd(lstLeftUp, step, Value);
                        break;
                    case LeaderDirection.RightDown: // 右下
                        SortedListAdd(lstRightDown, step, Value);
                        break;
                    case LeaderDirection.LeftDown:  // 左下
                        SortedListAdd(lstLeftDown, step, Value);
                        break;
                    case LeaderDirection.RightUp:   // 右上
                        SortedListAdd(lstRightUp, step, Value);
                        break;
                }
            }
            foreach (var n in this._NoticeStrEx)
            {
                step = (int)Math.Round(n.stepF, 0);          //ステップ数
                NoticeInfo ValueEx = new NoticeInfo();
                ValueEx.NoticeStr = n.str;
                //勾配を決定するための２点を選択する。
                PointF ptEx1 = getSlopPoint(step, _points, 1);
                PointF ptEx2 = getSlopPoint(step, _points, 2);
                //荷重変位曲線の勾配を計算する。
                double dX = (ptEx2.X - ptEx1.X) * scaleX;
                double dY = (ptEx2.Y - ptEx1.Y) * scaleY;
                ValueEx.Slope1 = (float)Math.Atan(dY / dX);
                //引き出し線の原点(pn1)をセットする
                ValueEx.pn0 = n.p;
                ValueEx.pn1 = ValueEx.pn0;
                ValueEx.pn1.X *= scaleX;
                ValueEx.pn1.Y *= scaleY;
                // 引き出しの方向を決定
                switch (getLeaderDirection(ValueEx.pn0.Y, ValueEx.Slope1 > 0))
                {
                    case LeaderDirection.LeftUP:    // 左上
                        SortedListAdd(lstLeftUp, step, ValueEx);
                        break;
                    case LeaderDirection.RightDown: // 右下
                        SortedListAdd(lstRightDown, step, ValueEx);
                        break;
                    case LeaderDirection.LeftDown:  // 左下
                        SortedListAdd(lstLeftDown, step, ValueEx);
                        break;
                    case LeaderDirection.RightUp:   // 右上
                        SortedListAdd(lstRightUp, step, ValueEx);
                        break;
                }
            }
            #endregion

            #region 右上がり↗ の引き出し

            #region 右下に引き出す場合
            for (int i = lstRightDown.Count - 1; i >= 0; i--)
            {
                step = lstRightDown.Keys[i];              //ステップ数
                NoticeInfo value = lstRightDown.Values[i];    //文字列
                rad = DEFAULT_RAD_RIGHT_DOWN;                       //引き出し線の勾配の初期値
                LineLength = DefaultNoticeLineLength;                   //引き出し線の長さの初期値
                //引き出し線の終点(pn2)の位置を決定する。
                for (int cnt = 0; cnt <= UnCrossingCheckLimitCount; cnt++)
                {
                    value.Slope2 = rad * (float)Math.PI / 180;  //引き出し線の勾配
                    value.pn2.X = value.pn1.X + LineLength * (float)Math.Cos(value.Slope2);  //終点文字列下線の始点X
                    value.pn2.Y = value.pn1.Y + LineLength * (float)Math.Sin(value.Slope2);  //終点文字列下線の始点Y
                    //文字列下線の終点(pn3)の位置を決定する。
                    float StrLength = Text.MeasureString(_myCanvas, value.NoticeStr).Width; //文字列の長さ
                    value.pn3 = new PointF(value.pn2.X + StrLength, value.pn2.Y);
                    value.pn4 = value.pn2;
                    //交差判定
                    PointF cp1 = IsCrossingLines(value.pn1, value.pn2);
                    PointF cp2 = IsCrossingLines(value.pn2, value.pn3);

                    if (cp1 == cp || cp2 == cp)
                    {//前回の交差ポイントと同じところで交差していたら回転する。
                        rad -= 10; //-10度回転
                        LineLength = DefaultNoticeLineLength;
                    }
                    else if (cp1 != PointF.Empty)
                    {//引き出し線が交差した場合
                        rad -= 10; //-10度回転
                    }
                    else if (cp2 != PointF.Empty)
                    {//文字列下線が交差した場合
                        LineLength += DefaultNoticeLineLength * 0.5F;
                    }
                    else
                    {//決定
                        break;
                    }
                    if (cp1 != PointF.Empty)
                        cp = cp1;
                    if (cp2 != PointF.Empty)
                        cp = cp2;
                }
                //交差判定Listに追加
                PointF pn4 = new PointF(value.pn2.X, value.pn2.Y - (_myCanvas.referenceSize.Height * 1.2f));
                PointF pn5 = new PointF(value.pn3.X, pn4.Y);
                _UnCrossingLines.Add(new LineInfo(value.pn1, value.pn2, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, value.pn3, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, pn4, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn3, pn5, "", ""));

            }
            #endregion

            #region 左上に引き出す場合
            for (int i = 0; i < lstLeftUp.Count; i++)
            {
                step = lstLeftUp.Keys[i];              //ステップ数
                NoticeInfo value = lstLeftUp.Values[i];    //文字列
                rad = DEFAULT_RAD_LEFT_UP;                        //引き出し線の勾配の初期値
                LineLength = DefaultNoticeLineLength;                 //引き出し線の長さの初期値
                //引き出し線の終点(pn2)の位置を決定する。
                for (int cnt = 0; cnt <= UnCrossingCheckLimitCount; cnt++)
                {
                    value.Slope2 = rad * (float)Math.PI / 180;  //引き出し線の勾配
                    value.pn2.X = value.pn1.X + LineLength * (float)Math.Cos(value.Slope2);  //終点文字列下線の始点X
                    value.pn2.Y = value.pn1.Y + LineLength * (float)Math.Sin(value.Slope2);  //終点文字列下線の始点Y
                    //文字列下線の終点(pn3)の位置を決定する。
                    float StrLength = Text.MeasureString(_myCanvas, value.NoticeStr).Width; //文字列の長さ
                    value.pn3 = new PointF(value.pn2.X - StrLength, value.pn2.Y);
                    value.pn4 = value.pn3;
                    //交差判定
                    PointF cp1 = IsCrossingLines(value.pn1, value.pn2);
                    PointF cp2 = IsCrossingLines(value.pn2, value.pn3);

                    if (cp1 == cp || cp2 == cp)
                    {//前回の交差ポイントと同じところで交差していたら回転する。
                        rad -= 10; //-10度回転
                        LineLength = DefaultNoticeLineLength;
                    }
                    else if (cp1 != PointF.Empty)
                    {//引き出し線が交差した場合
                        rad -= 10; //-10度回転
                    }
                    else if (cp2 != PointF.Empty)
                    {//文字列下線が交差した場合
                        LineLength += DefaultNoticeLineLength * 0.25F;
                    }
                    else
                    {//決定
                        break;
                    }
                    if (cp1 != PointF.Empty)
                        cp = cp1;
                    if (cp2 != PointF.Empty)
                        cp = cp2;
                }
                //交差判定Listに追加
                PointF pn4 = new PointF(value.pn2.X, value.pn2.Y + (_myCanvas.referenceSize.Height * 1.2f));
                PointF pn5 = new PointF(value.pn3.X, pn4.Y);
                _UnCrossingLines.Add(new LineInfo(value.pn1, value.pn2, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, value.pn3, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, pn4, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn3, pn5, "", ""));
            }

            #endregion

            #endregion

            #region 右下がり↘ の引き出し

            #region 右上に引き出す場合

            for (int i = lstRightUp.Count - 1; i >= 0; i--)
            {
                step = lstRightUp.Keys[i];              //ステップ数
                NoticeInfo value = lstRightUp.Values[i];    //文字列
                rad = DEFAULT_RAD_RIGHT_UP;            //引き出し線の勾配の初期値 60°
                LineLength = DefaultNoticeLineLength;     //引き出し線の長さの初期値
                //引き出し線の終点(pn2)の位置を決定する。
                for (int cnt = 0; cnt <= UnCrossingCheckLimitCount; cnt++)
                {
                    value.Slope2 = rad * (float)Math.PI / 180; //引き出し線の勾配
                    value.pn2.X = value.pn1.X + LineLength * (float)Math.Cos(value.Slope2); //終点文字列下線の始点X
                    value.pn2.Y = value.pn1.Y + LineLength * (float)Math.Sin(value.Slope2); //終点文字列下線の始点Y
                    //文字列下線の終点(pn3)の位置を決定する。
                    float StrLength = Text.MeasureString(_myCanvas, value.NoticeStr).Width; //文字列の長さ
                    value.pn3 = new PointF(value.pn2.X + StrLength, value.pn2.Y);
                    value.pn4 = value.pn2;
                    //交差判定
                    PointF cp1 = IsCrossingLines(value.pn1, value.pn2);
                    PointF cp2 = IsCrossingLines(value.pn2, value.pn3);

                    if (cp1 == cp || cp2 == cp)
                    {//前回の交差ポイントと同じところで交差していたら回転する。
                        rad += 10; //+10度回転
                        LineLength = DefaultNoticeLineLength;
                    }
                    else if (cp1 != PointF.Empty)
                    {//引き出し線が交差した場合
                        rad += 10; //+10度回転
                    }
                    else if (cp2 != PointF.Empty)
                    {//文字列下線が交差した場合
                        LineLength += DefaultNoticeLineLength * 0.25F;
                    }
                    else
                    {//決定
                        break;
                    }
                    if (cp1 != PointF.Empty)
                        cp = cp1;
                    if (cp2 != PointF.Empty)
                        cp = cp2;
                }
                //交差判定Listに追加
                PointF pn4 = new PointF(value.pn2.X, value.pn2.Y + (_myCanvas.referenceSize.Height * 1.2f));
                PointF pn5 = new PointF(value.pn3.X, pn4.Y);
                _UnCrossingLines.Add(new LineInfo(value.pn1, value.pn2, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, value.pn3, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, pn4, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn3, pn5, "", ""));
            }
            #endregion

            #region 左下に引き出す場合

            for (int i = 0; i < lstLeftDown.Count; i++)
            {
                step = lstLeftDown.Keys[i];              //ステップ数
                NoticeInfo value = lstLeftDown.Values[i];    //文字列
                //引き出し線の終点(pn2)の位置を決定する。
                rad = DEFAULT_RAD_LEFT_DOWN;                                 //引き出し線の勾配の初期値 60°
                LineLength = DefaultNoticeLineLength;     //引き出し線の長さの初期値
                for (int cnt = 0; cnt <= UnCrossingCheckLimitCount; cnt++)
                {
                    value.Slope2 = rad * (float)Math.PI / 180; //引き出し線の勾配
                    value.pn2.X = value.pn1.X + LineLength * (float)Math.Cos(value.Slope2); //終点文字列下線の始点X
                    value.pn2.Y = value.pn1.Y + LineLength * (float)Math.Sin(value.Slope2); //終点文字列下線の始点Y
                    //文字列下線の終点(pn3)の位置を決定する。
                    float StrLength = Text.MeasureString(_myCanvas, value.NoticeStr).Width; //文字列の長さ
                    value.pn3 = new PointF(value.pn2.X - StrLength, value.pn2.Y);
                    value.pn4 = value.pn3;
                    //交差判定
                    PointF cp1 = IsCrossingLines(value.pn1, value.pn2);
                    PointF cp2 = IsCrossingLines(value.pn2, value.pn3);

                    if (cp1 == cp || cp2 == cp)
                    {//前回の交差ポイントと同じところで交差していたら回転する。
                        rad += 10; //-10度回転
                        LineLength = DefaultNoticeLineLength;
                    }
                    else if (cp1 != PointF.Empty)
                    {//引き出し線が交差した場合
                        rad += 10; //-10度回転
                    }
                    else if (cp2 != PointF.Empty)
                    {//文字列下線が交差した場合
                        LineLength += DefaultNoticeLineLength * 0.5F;
                    }
                    else
                    {//決定
                        break;
                    }
                    if (cp1 != PointF.Empty)
                        cp = cp1;
                    if (cp2 != PointF.Empty)
                        cp = cp2;
                }
                //交差判定Listに追加
                PointF pn4 = new PointF(value.pn2.X, value.pn2.Y - (_myCanvas.referenceSize.Height * 1.2f));
                PointF pn5 = new PointF(value.pn3.X, pn4.Y);
                _UnCrossingLines.Add(new LineInfo(value.pn1, value.pn2, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, value.pn3, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn2, pn4, "", ""));
                _UnCrossingLines.Add(new LineInfo(value.pn3, pn5, "", ""));
            }
            #endregion

            #endregion

            //右上がり↗ の引き出しを描く
            foreach (var n in lstLeftUp)
                DrowNoticeLine(_myCanvas, n.Value, X1, Y1);
            foreach (var n in lstRightDown)
                DrowNoticeLine(_myCanvas, n.Value, X1, Y1);
            //右下がり↘ の引き出しを描く
            foreach (var n in lstLeftDown)
                DrowNoticeLine(_myCanvas, n.Value, X1, Y1);
            foreach (var n in lstRightUp)
                DrowNoticeLine(_myCanvas, n.Value, X1, Y1);

            //荷重～変位曲線の上に描く
            if (this._NoticeStr2.Count > 0)
            {
                foreach (var n in this._NoticeStr2)
                {
                    var fs = _myCanvas.FontSize;
                    _myCanvas.FontSize = 8;
                    int istep = n.Key;          //ステップ数
                    float dX = X1 + (_points[istep].X * scaleX) - 4;
                    float dY = Y1 - (_points[istep].Y * scaleY) - 6;
                    DrowNoticePoint(_myCanvas, n.Value, dX, dY);
                    _myCanvas.FontSize = fs;
                }
            }

            #endregion

            #region チャートに直線を追加 -------------------------------------------------------------------------------------------------------------

            foreach (var l in _Lines){
                float px1 = X1 + (l._p1.X * scaleX);
                float px2 = X1 + (l._p2.X * scaleX);
                float py1 = Y1 - (l._p1.Y * scaleY);
                float py2 = Y1 - (l._p2.Y * scaleY);
                Shape.DrawLine(_myCanvas, new PointF(px1, py1), new PointF(px2, py2), SubsidyLineThickness);

                //始点(p1)位置のY軸に文字を表示する
                if (l._NoticeStrY.Trim().Length > 0)
                {
                    _myCanvas.CurrentX = position.Left;
                    _myCanvas.CurrentY = py1 - (_myCanvas.referenceSize.Height / 2);
                    _myCanvas.CurrentY += l._dY;
                    Text.PrtText(_myCanvas, l._NoticeStrY);
                }
                //始点(p1)位置のX軸に文字を表示する
                if (l._NoticeStrX.Trim().Length > 0)
                {
                    SizeF printsize = Text.MeasureString(_myCanvas, l._NoticeStrX);
                    _myCanvas.CurrentX = px1 - printsize.Width / 2;
                    _myCanvas.CurrentY = position.Bottom - (_myCanvas.referenceSize.Height * 2);
                    _myCanvas.CurrentY += l._dY;
                    Text.PrtText(_myCanvas, l._NoticeStrX);
                }
            }

            #endregion

            #region 終了処理 -------------------------------------------------------------------------------------------
            // カレントポジションを図の外側へ 
            _myCanvas.CurrentX = CurX + this.width + this.padingY;
            _myCanvas.CurrentY = CurY + this.height + this.padingY;
            #endregion
        }

        private void SortedListAdd(SortedList<int, NoticeInfo> targetList, int key, NoticeInfo value )
        {
            int index = targetList.IndexOfKey(key);
            //同じキーが既に登録されている場合
            if (index != -1)
            {
                NoticeInfo tmp = targetList[key];
                tmp.NoticeStr += string.Format(", {0}",value.NoticeStr);
                targetList.RemoveAt(index);
                targetList.Add(key, tmp);
            }
            else
            {
                targetList.Add(key, value);
            }
        }
        
        /// <summary>引き出し記号を描く</summary>
        private void DrowNoticePoint(PdfDocument _myCanvas, string NoticeStr, float OriginX, float OriginY)
        {
            _myCanvas.CurrentX = OriginX;
            _myCanvas.CurrentY = OriginY;
            Text.PrtText(_myCanvas, NoticeStr);
        }

        /// <summary>引き出し線を描く</summary>
        private void DrowNoticeLine(PdfDocument _myCanvas, NoticeInfo Info, float OriginX, float OriginY)
        {
            PointF p1 = new PointF();
            PointF p2 = new PointF();
            //基点～終点(文字列下線の始点)
            p1.X = OriginX + Info.pn1.X;
            p2.X = OriginX + Info.pn2.X;
            p1.Y = OriginY - Info.pn1.Y;
            p2.Y = OriginY - Info.pn2.Y;
            Shape.DrawLine(_myCanvas, p1, p2, NoticeLineThickness);
            //終点(文字列下線の始点)～文字列下線の終点
            p1.X = OriginX + Info.pn2.X;
            p2.X = OriginX + Info.pn3.X;
            p1.Y = OriginY - Info.pn2.Y;
            p2.Y = OriginY - Info.pn3.Y;
            Shape.DrawLine(_myCanvas, p1, p2, NoticeLineThickness);
            //印字（文字列の基点は pn3です）
            float CurX = _myCanvas.CurrentX;
            float CurY = _myCanvas.CurrentY;
            _myCanvas.CurrentX = OriginX + Info.pn4.X;
            _myCanvas.CurrentY = OriginY - Info.pn4.Y - (_myCanvas.referenceSize.Height * 1.2f);
            Text.PrtText(_myCanvas, Info.NoticeStr);
            _myCanvas.CurrentX = CurX;
            _myCanvas.CurrentY = CurY;
        }

        /// <summary>ラウンドアップ</summary>
        private double RoundUp(float dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Ceiling(dValue * dCoef) / dCoef :
                                System.Math.Floor(dValue * dCoef) / dCoef;
        }

        /// <summary>２点間が交差してはいけない直線群(_UnCrossingLines)と交わっていないか判定する。</summary>
        private PointF IsCrossingLines(PointF _p1, PointF _p2)
        {
            foreach (var p in _UnCrossingLines){
                PointF p1 = _p1;
                PointF p2 = _p2;
                PointF p4 = p._p1;
                PointF p5 = p._p2;

                PointF pp = IntersectionStandard(p1, p2, p4, p5);
                if (pp != PointF.Empty)
                {
                    //一度交差したデータは、先頭に登録しておく
                    _UnCrossingLines.Remove(p);
                    _UnCrossingLines.Insert(0, p);
                    return pp;
                }
            }
            return PointF.Empty;
        }
 
        /// <summary>座標 p1,p2 を結ぶ線分と座標 p3,p4 を結ぶ線分が交差しているかを調べる
        /// ただし、線分が重なっている場合(4点が一直線上にある)、「交差していない」、と判定します。 </summary>
        private PointF IntersectionStandard(PointF a, PointF b, PointF c, PointF d)
        {
            double r, s;
            double denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            //分母が０の場合平行
            if (denominator == 0)
                return PointF.Empty;

            double numeratorR = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
            r = numeratorR / denominator;

            double numeratorS = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);
            s = numeratorS / denominator;

            //交差しない
            if (r < 0 || r > 1 || s < 0 || s > 1)
                return PointF.Empty;

            PointF point = new PointF();
            point.X = (float)(a.X + (r * (b.X - a.X)));
            point.Y = (float)(a.Y + (r * (b.Y - a.Y)));

            //Console.Write(String.Format("X1={0}, Y1={1} - X2={2}, Y2={3}\n", a.X, a.Y, b.X, b.Y));
            //Console.Write(String.Format("X3={0}, Y3={1} - X4={2}, Y4={3}\n", c.X, c.Y, d.X, d.Y));
            //Console.Write(String.Format("X={0}, Y={1} で交差しました。\n", point.X, point.Y));

            return point;
        }

        /// <summary>座標 p1,p2 を結ぶ線分と座標 p3,p4 を結ぶ線分が交差しているかを調べる
        /// ただし、線分が重なっている場合(4点が一直線上にある)、「交差していない」、と判定します。 </summary>
        private PointF getPointOfContact(PointF a, PointF b, PointF c, PointF d)
        {
            double r, s;
            double denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);

            PointF point = new PointF(); 

            //分母が０の場合平行
            if (denominator == 0) { 
                point.X = 0;
                point.Y = 0;
            }
            else
            {

                double numeratorR = (a.Y - c.Y) * (d.X - c.X) - (a.X - c.X) * (d.Y - c.Y);
                r = numeratorR / denominator;

                double numeratorS = (a.Y - c.Y) * (b.X - a.X) - (a.X - c.X) * (b.Y - a.Y);
                s = numeratorS / denominator;

                //交差しない
                if (r < 0 || r > 1 || s < 0 || s > 1) { 
                    point.X = 0;
                    point.Y = 0;
                }

                point.X = (float)(a.X + (r * (b.X - a.X)));
                point.Y = (float)(a.Y + (r * (b.Y - a.Y)));
            }
 
            return point;
        }

        /// <summary>
        /// 引き出し線の方向を取得
        /// </summary>
        /// <param name="fltYValue">判定値</param>
        /// <param name="isLeft">頂点より左：true, 右:false</param>
        /// <returns>引き出し線の方向</returns>
        private LeaderDirection getLeaderDirection(float fltYValue, bool isLeft)
        {

            if (isLeft) //　頂点より左側
            {
                if (fltYValue <= SeparateYRiseNoticeLine)
                {
                    return LeaderDirection.RightDown;
                }
                else
                {
                    return LeaderDirection.LeftUP;
                }
                 
            }
            else // 頂点より右側
            {
                if (fltYValue >= SeparateYDropNoticeLine)
                {
                    return LeaderDirection.RightUp;
                }
                else
                {
                    return LeaderDirection.LeftDown;
                }
            }
            
        }

        /// <summary>
        /// 勾配を決めるポイントを取得
        /// </summary>
        /// <param name="step">ステップ数</param>
        /// <param name="points"></param>
        /// <param name="order">1つ目の点：１ / 2つ目の点：２ / ・・・・</param>
        /// <returns></returns>
        private PointF getSlopPoint(int step, PointF[] points, int order)
        {
            
            int cnt = step + (order - 1);
                        
            if (step >= this._points.Count() - 1)
            {
                cnt = step -1 ;
            }

            return points[cnt];
        }

    }

    internal class NoticeStrEx
    {
        internal PointF p;
        internal float stepF;
        internal string str;
        internal NoticeStrEx(PointF _p, float _stepF, string _str)
        {
            p = _p;
            stepF = _stepF;
            str = _str;
        }
    }

    internal class LineInfo
    {
        internal PointF _p1;
        internal PointF _p2;
        internal string _NoticeStrY;
        internal string _NoticeStrX;
        internal float _dY;

        /// <summary>コンストラクタ</summary>
        /// <param name="p1">直線の始点</param>
        /// <param name="p2">直線の終点</param>
        /// <param name="NoticeStrY">始点(p1)位置のY軸に表示する文字</param>
        /// <param name="NoticeStrX">始点(p1)位置のX軸に表示する文字</param>
        /// <param name="dY">印字位置の補正値</param>
        internal LineInfo(PointF p1, PointF p2, string NoticeStrY = "", string NoticeStrX = "", float dY = 0)
        {
            _p1 = p1;
            _p2 = p2;
            _NoticeStrY = NoticeStrY;
            _NoticeStrX = NoticeStrX;
            _dY = dY;
        }
    }

    internal class NoticeInfo
    {
        internal string NoticeStr;
        internal float Slope1;
        internal float Slope2;
        internal PointF pn0 = new PointF();  //基点(オリジナル座標）
        internal PointF pn1 = new PointF();  //基点(グラフ座標）
        internal PointF pn2 = new PointF();  //終点(文字列下線の始点)
        internal PointF pn3 = new PointF();  //文字列下線の終点
        internal PointF pn4 = new PointF();  //文字列の基点

        public NoticeInfo(string _NoticeStr)
        {
            this.NoticeStr = _NoticeStr;
        }

        public NoticeInfo()
        {
            // TODO: Complete member initialization
        }
    }
   
}

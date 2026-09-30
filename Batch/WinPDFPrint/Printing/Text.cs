using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinPDFPrint.Printing
{
    internal class Text
    {
        /// <summary>角度付きテキスト</summary>
        static public void PrtText(PdfDocument mc, string str, float angle)
        {
            var oldAngle = mc.RotateAngle;
            mc.RotateAngle = angle;
            PrtText(mc, str);
            mc.RotateAngle = oldAngle;
        }

 

        static public void PrtText(PdfDocument mc, string str)
        {
            if (str == null)
                return;

            // 引数は変えないようにする
            string stacTxt = "";
            while (str.Length > 0)
            {
                #region ○番号 number enclosed within a circle
                if (str.Substring(0, Math.Min(2, str.Length)) == "○{")
                {
                    DrawString(mc, stacTxt);
                    stacTxt = "";

                    List<string> target = ExtractTxt(str, "{*}");
                    str = target[1] as string;
                    string txt = target[0] as string;

                    float cX = mc.CurrentX;
                    float cY = mc.CurrentY;

                    SizeF printSize = MeasureString(mc, txt);
                    //文字の大きさを保存
                    float wt = printSize.Width;
                    float ht = printSize.Height;
                    //円はちょっと大きく
                    printSize.Width *= 1.02F; 
                    printSize.Height *= 1.2F;  
                    //幅は高さ以上とする
                    printSize.Width = Math.Max(printSize.Width, printSize.Height);
                    //円の大きさを保存
                    float wc = printSize.Width;
                    float hc = printSize.Height;
                    //円を描く
                    PointF printPoint = new PointF(mc.CurrentX, mc.CurrentY);
                    Shape.Drawcircle(mc, printPoint, printSize, 0.25F);
                    //幅は高さ以上としたので、その差分だけ文字の印字位置をずらす。
                    mc.CurrentX += (wc - wt) / 2;
                    //文字を描く
                    PrtText(mc, txt);
                    mc.CurrentX = cX + printSize.Width;
                    mc.CurrentY = cY;
                }
                #endregion

                #region 下付き sub script
                else if (str.Substring(0, Math.Min(2, str.Length)) == "_{")
                {
                    DrawString(mc, stacTxt);
                    stacTxt = "";

                    List<string> target = ExtractTxt(str, "{*}");
                    str = target[1] as string;

                    float fs = mc.FontSize;
                    float cY = mc.CurrentY;
                    mc.CurrentY += (mc.referenceSize.Height * 0.3F);
                    mc.FontSize *= 0.7F;

                    PrtText(mc, target[0] as string);

                    mc.CurrentY = cY;
                    mc.FontSize = fs;
                }
                #endregion

                #region 上付き super script
                else if (str.Substring(0, Math.Min(2, str.Length)) == "^{")
                {
                    DrawString(mc, stacTxt);
                    stacTxt = "";

                    List<string> target = ExtractTxt(str, "{*}");
                    str = target[1] as string;

                    float fs = mc.FontSize;
                    mc.FontSize *= 0.7F;

                    PrtText(mc, target[0] as string);

                    mc.FontSize = fs;
                }
                #endregion

                #region ルート root
                else if (str.Substring(0, Math.Min(6, str.Length)) == "#root{")
                {
                    DrawString(mc, stacTxt);
                    stacTxt = "";

                    List<string> target = ExtractTxt(str, "{*}");
                    str = target[1] as string;

                    // 後に描くルート記号の基点位置を設定.
                    mc.rootX1 = mc.CurrentX;
                    mc.rootX2 = float.MaxValue;
                    mc.rootY1 = 0;
                    mc.rootY2 = float.MaxValue;

                    SizeF stringSize = mc.MeasureString("√");
                    mc.CurrentX += stringSize.Width;

                    PrtText(mc, target[0] as string);

                    mc.rootX2 = mc.CurrentX;

                    // ルートを描く
                    float h = stringSize.Height;
                    float w = stringSize.Width;
                    float UX = (w / 20);
                    float UY = (h / 20) - (h / 200);
                    float cy1 = mc.rootY1 - (0.7F * h);
                    float cx1 = mc.rootX1;
                    float cy2 = mc.rootY2;
                    float cx2 = mc.rootX2;

                    PointF[] root = 
                    {
                        new PointF(cx1 ,            cy1 + (17 * UY)),
                        new PointF(cx1 + ( 4 * UX), cy1 + ( 8 * UY)),
                        new PointF(cx1 + (10 * UX), cy1 + (19 * UY)),
                        new PointF(cx1 + (19 * UX), cy2),
                        new PointF(cx2,             cy2)
                    };
                    Shape.DrawLine(mc, root[0], root[1], 0.25F);
                    Shape.DrawLine(mc, root[1], root[2], 0.50F);
                    Shape.DrawLine(mc, root[2], root[3], 0.25F);
                    Shape.DrawLine(mc, root[3], root[4], 0.25F);
                }
                #endregion

                #region 分数 fraction
                else if (str.Substring(0, Math.Min(6, str.Length)) == "#frac{")
                {
                    DrawString(mc, stacTxt);
                    stacTxt = "";

                    List<string> target;
                    float cX = mc.CurrentX;
                    float cY = mc.CurrentY;

                    //*** 分子　numerator
                    target = ExtractTxt(str, "{*}");
                    str = target[1] as string;

                    mc.CurrentY -= mc.referenceSize.Height / 2;

                    PrtText(mc, target[0] as string);

                    float nX = mc.CurrentX;
                    mc.CurrentX = cX;
                    mc.CurrentY = cY;

                    //*** 分母　denominator
                    target = ExtractTxt(str, "{*}");
                    str = target[1] as string;

                    mc.CurrentY += mc.referenceSize.Height / 2;

                    PrtText(mc, target[0] as string);

                    float dX = mc.CurrentX;
                    mc.CurrentX = cX;
                    mc.CurrentY = cY;

                    //*** 横棒 separater
                    float mX = Math.Max(nX, dX);
                    float mY = cY + mc.referenceSize.Height/1.5F;
                    Shape.DrawLine(mc, new PointF(cX, mY), new PointF(mX, mY), 0.25F);
                    mc.CurrentX = mX;
                }
                #endregion

                else
                {
                    stacTxt += str.Substring(0, 1);
                    str = str.Substring(1);
                }
            }
            DrawString(mc, stacTxt);
        }

        #region DrawString
        static private void DrawString(PdfDocument _mc, string _strTxt)
        {
            if (_strTxt.Length == 0)
                return;

            //全角文字の印字にバグがあるようです。したがって
            //文字列を１文字ずつ検索し、半角なら stacTxt に格納し一度に印字、全角なら一文字づつ印字する。 
            string stacTxt = "";
            foreach (char c in _strTxt){
                string targetStr = c.ToString();
                if (isZenkaku(targetStr)){
                    if (stacTxt != ""){
                        PrintString(_mc, stacTxt);
                        stacTxt = "";
                    }
                    PrintString(_mc, targetStr);
                }
                else{
                    stacTxt += targetStr;
                }
            }
            if (stacTxt != ""){
                PrintString(_mc, stacTxt);
            }

            //カーソルを最後尾へ
            _mc.rootY1 = Math.Max(_mc.rootY1, _mc.CurrentY + _mc.referenceSize.Height);
            _mc.rootY2 = Math.Min(_mc.rootY2, _mc.CurrentY );
        }

        static private void PrintString(PdfDocument _mc, string _strTxt)
        {
            PointF pf = new PointF(_mc.CurrentX, _mc.CurrentY);
            _mc.DrawString(_strTxt, _mc.font, _mc.fontColor, pf);
            _mc.CurrentX += _mc.MeasureString(_strTxt).Width;
        }

        public static SizeF MeasureString(PdfDocument _mc, string _strTxt)
        {
            SizeF result = new SizeF();
            PdfDocument tmc = new PdfDocument();//_mc.Clone();
            float CurX = tmc.CurrentX;
            float CurY = tmc.CurrentY;

            Text.PrtText(tmc, _strTxt);

            result.Width = tmc.CurrentX - CurX;
            result.Height = tmc.referenceSize.Height;// tmc.CurrentY - CurY;
            return result;
        }
        public static bool isZenkaku(string str)
        {
            //空白 がうまく判定できないので例外処理
            if (String.IsNullOrWhiteSpace(str) == true)
                return false;

            Encoding sjisEnc = Encoding.GetEncoding("Shift_JIS");
            int num = sjisEnc.GetByteCount(str);
            return num == str.Length * 2;
        }
        #endregion

        #region The portion corresponding to conditions is searched and an applicable '*' portion is extracted.
        static public List<string> ExtractTxt(string str, string Condition)
        {
            string[] s = Condition.Split('*');
            int iExtFlg = 0;
            string stacTxt = "";

            while (str.Length > 0)
            {
                if (str.Substring(0, s[0].Length) == s[0])
                {
                    iExtFlg += 1;
                    if (iExtFlg == 1)
                        str = str.Substring(s[0].Length);
                }

                if (iExtFlg >= 1)
                {
                    stacTxt += str.Substring(0, 1);
                    str = str.Substring(1);
                }
                else
                {
                    str = str.Substring(1);
                }

                if (str.Substring(0, s[1].Length) == s[1])
                {
                    if (iExtFlg == 1)
                    {
                        str = str.Substring(s[1].Length);
                        break;
                    }
                    iExtFlg -= 1;
                }

            }
            List<string> result = new List<string>();
            result.Add(stacTxt);
            result.Add(str);
            return result;
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinPDFPrint.Printing
{
    internal class Shape
    {
        /// <summary>
        /// 直線を描く
        /// </summary>
        /// <param name="_myCanvas"></param>
        /// <param name="_pt1"></param>
        /// <param name="_pt2"></param>
        /// <param name="_PenWidth"></param>
        static public void DrawLine(PdfDocument _myCanvas, PointF _pt1, PointF _pt2, float _PenWidth)
        {
            Pen pen = new Pen(Color.Black, _PenWidth);
            _myCanvas.DrawLine(pen, _pt1, _pt2);
        }
        static public void DrawLine(PdfDocument _myCanvas, PointF _pt1, PointF _pt2, float _PenWidth, Color col)
        {
            Pen pen = new Pen(col, _PenWidth);
            _myCanvas.DrawLine(pen, _pt1, _pt2);
        }

        /// <summary>
        /// 破線を描く
        /// </summary>
        /// <param name="_myCanvas"></param>
        /// <param name="_pt1"></param>
        /// <param name="_pt2"></param>
        /// <param name="_PenWidth"></param>
        /// <param name="_Interval">破線の距離</param>
        static public void DrawDashLine(PdfDocument _myCanvas, PointF _pt1, PointF _pt2, float _PenWidth, float _Interval)
        {
            Pen pen = new Pen(Color.Black, _PenWidth);
            float LenX = _pt2.X - _pt1.X;
            float LenY = _pt2.Y - _pt1.Y;
            float Length = (float)Math.Sqrt(Math.Pow(LenX, 2) + Math.Pow(LenY, 2));
            int num1 = (int)Math.Round(Length / _Interval, 0);
            int num2 = (num1 % 2 == 1) ? num1 + 1 : num1;
            int num3 = num2 / 2;
            float IntX = LenX / num2;
            float IntY = LenY / num2;

            PointF p1 = _pt1;
            PointF p2 = new PointF();
            for (int i = 0; i < num3; i++){
                p2.X = p1.X + IntX;
                p2.Y = p1.Y + IntY;
                _myCanvas.DrawLine(pen, p1, p2);
                p1.X = p2.X + IntX;
                p1.Y = p2.Y + IntY;
            }
        }

        /// <summary>
        /// 円を描く
        /// </summary>
        /// <param name="_myCanvas"></param>
        /// <param name="_pt0"></param>
        /// <param name="_size"></param>
        /// <param name="_PenWidth"></param>
        static public void Drawcircle(PdfDocument _myCanvas, PointF _pt0, SizeF _size, float _PenWidth)
        {
            Pen pen = new Pen(Color.Black, _PenWidth);
            _myCanvas.DrawEllipse(pen, _pt0.X, _pt0.Y, _size.Width, _size.Height);
        }

        /// <summary>
        /// 塗りつぶし円を描く
        /// </summary>
        /// <param name="_myCanvas"></param>
        /// <param name="_pt0"></param>
        /// <param name="_size"></param>
        /// <param name="brush"></param>
        static public void Drawcircle(PdfDocument _myCanvas, PointF _pt0, SizeF _size, Brush brush)
        {
            _myCanvas.FillEllipse(brush, _pt0.X, _pt0.Y, _size.Width, _size.Height);
        }


    }
}

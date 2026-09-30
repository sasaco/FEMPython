using C1.C1Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WinPDFPrint
{
    public class PrintPreview : IDisposable
    {
        private Printing.PdfDocument _myCanvas;
        private float _Margine;

        public PrintPreview()
        {
            _myCanvas = new Printing.PdfDocument();
            _myCanvas.FontType = FontTypeEnum.Embedded;
            _myCanvas.Clear();
            _myCanvas.DocumentInfo.Title = "PrintPreview";

            //ページ初期設定
            _Margine = 72;
            _myCanvas.CurrentX = _Margine;
            _myCanvas.CurrentY = _Margine;
            _myCanvas.PaperKind = PaperKind.A4;
            _myCanvas.Landscape = false;
        }
        public void Dispose()
        {
            _myCanvas.Dispose();
        }

        #region class PdfDocument
        public SizeF GetPageSize()
        {
            RectangleF rect = _myCanvas.PageRectangle;
            SizeF result = new SizeF(( rect.Right-rect.Left ), ( rect.Bottom-rect.Top ));
            return result;
        }
        
        
        public void NewPage()
        {
            _myCanvas.NewPage();
            _myCanvas.CurrentX = _Margine;
            _myCanvas.CurrentY = _Margine;
        }
        public void NewPage(bool Orientation = false)
        {
            NewPage();
            SetPaperOrientation(Orientation);
        }
        public void DeletePage()
        {
            _myCanvas.Pages.RemoveAt(_myCanvas.CurrentPage);
        }
        public void DeletePage(int PageIndex)
        {
            _myCanvas.Pages.RemoveAt(PageIndex);
        }
        public int GetPageCount()
        {
            return _myCanvas.Pages.Count;
        }
        public void SetPaperOrientation(bool Orientation = false)
        {
            _myCanvas.Landscape = Orientation;
        }
        public void SetMargine(float MargineF)
        {
            _Margine = MargineF;
        }
        public void enter(float LineSpacing)
        {
            _myCanvas.CurrentX = _Margine;
            _myCanvas.CurrentY += _myCanvas.MeasureString("H").Height * LineSpacing;
        }
        public void SetCurrentPos(PointF CurrentPos)
        {
            _myCanvas.CurrentX = CurrentPos.X;
            _myCanvas.CurrentY = CurrentPos.Y;
        }
        public PointF GetCurrentPos()
        {
            return new PointF(_myCanvas.CurrentX, _myCanvas.CurrentY);
        }
        public float FontSize
        {
            get { return _myCanvas.FontSize; }
            set
            {
                _myCanvas.FontSize = value;
            }
        }
        public Brush FontColor
        {
            get { return _myCanvas.fontColor; }
            set
            {
                _myCanvas.fontColor = value;
            }
        }

        #endregion

        #region class Shape

        public void DrawLine(PointF _pt1, PointF _pt2, float _PenWidth)
        {
            Printing.Shape.DrawLine(_myCanvas, _pt1, _pt2, _PenWidth);
        }
        public void DrawLine(PointF _pt1, PointF _pt2, float _PenWidth, Color col)
        {
            Printing.Shape.DrawLine(_myCanvas, _pt1, _pt2, _PenWidth, col);
        }
        public void DrawDashLine(PointF _pt1, PointF _pt2, float _PenWidth, float _Interval)
        {
            Printing.Shape.DrawDashLine(_myCanvas, _pt1, _pt2, _PenWidth,  _Interval);
        }
        /// <summary>
        /// 矢印付きの線
        /// </summary>
        /// <param name="_pt1"></param>
        /// <param name="_pt2"></param>
        /// <param name="_PenWidth"></param>
        public void DrawArrowLine(PointF _pt1, PointF _pt2, float _PenWidth, float _pt1Size, float _pt2Size)
        {
            //直線を描画
            Printing.Shape.DrawLine(_myCanvas, _pt1, _pt2, _PenWidth);
            //矢印を描画
            double ArrowAngle = 30 *  Math.PI / 180;
            double LineAngle = 90 * Math.PI / 180;
            if (_pt2.X - _pt1.X != 0) {
                LineAngle = Math.Atan(( _pt2.Y - _pt1.Y ) / ( _pt2.X - _pt1.X ));
            }
            //起点側(pt1)矢印を描画
            if (_pt1Size > 0)
            {
                PointF _pt11 = new PointF(_pt1.X + Convert.ToSingle(_pt1Size * Math.Cos(LineAngle + ArrowAngle)), 
                                          _pt1.Y + Convert.ToSingle(_pt1Size * Math.Sin(LineAngle + ArrowAngle)));
                PointF _pt12 = new PointF(_pt1.X + Convert.ToSingle(_pt1Size * Math.Cos(LineAngle - ArrowAngle)),
                                          _pt1.Y + Convert.ToSingle(_pt1Size * Math.Sin(LineAngle - ArrowAngle)));
                Printing.Shape.DrawLine(_myCanvas, _pt1, _pt11, _PenWidth);
                Printing.Shape.DrawLine(_myCanvas, _pt1, _pt12, _PenWidth);
            }
            //終点側(pt2)矢印を描画
            if (_pt2Size > 0)
            {
                PointF _pt21 = new PointF(_pt2.X - Convert.ToSingle(_pt2Size * Math.Cos(LineAngle + ArrowAngle)),
                                          _pt2.Y - Convert.ToSingle(_pt2Size * Math.Sin(LineAngle + ArrowAngle)));
                PointF _pt22 = new PointF(_pt2.X - Convert.ToSingle(_pt2Size * Math.Cos(LineAngle - ArrowAngle)),
                                          _pt2.Y - Convert.ToSingle(_pt2Size * Math.Sin(LineAngle - ArrowAngle)));
                Printing.Shape.DrawLine(_myCanvas, _pt2, _pt21, _PenWidth);
                Printing.Shape.DrawLine(_myCanvas, _pt2, _pt22, _PenWidth);
            }
        }
        public void DrawCircle(PointF _pt0, SizeF _size, float _PenWidth)
        {
            //_pt0は,円の中心なので補正する。
            PointF _pt1 = new PointF(_pt0.X - _size.Width / 2, _pt0.Y - _size.Height / 2);
            Printing.Shape.Drawcircle(_myCanvas, _pt1, _size, _PenWidth);
        }
        public void DrawCircle(PointF _pt0, SizeF _size, Brush _BrushColor)
        {
            //_pt0は,円の中心なので補正する。
            PointF _pt1 = new PointF(_pt0.X - _size.Width / 2, _pt0.Y - _size.Height / 2);
            Printing.Shape.Drawcircle(_myCanvas, _pt1, _size, _BrushColor);
        }
        #endregion

        #region class Img
        public void PrtImg( Image _img)
        {
            Printing.Img.PrtImg(_myCanvas, _img);
        }
        #endregion

        #region class Text
        public void PrtText(string str)
        {
            Printing.Text.PrtText(_myCanvas, str);
        }
        public void PrtText(string str, float angle)
        {
            Printing.Text.PrtText(_myCanvas, str, angle);
        }
        public SizeF GetPrintingTxtSize(string str)
        {
            return Printing.Text.MeasureString(_myCanvas, str);
        }
        #endregion

        #region class Table
        public void PrintTable(Printing.Table _table)
        {
            _table.PrintTable(_myCanvas);
        }
        #endregion

        #region class Chart
        public void PrintChart(Printing.Chart _chart)
        {
            _chart.PrintChart(_myCanvas);
        }
        #endregion

        public void SavePDF(string fileName)
        {
            _myCanvas.Save(fileName);
        }


    }
}

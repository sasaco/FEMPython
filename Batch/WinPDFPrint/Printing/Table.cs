using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinPDFPrint.Printing
{
    public class Table
    {
        #region This Class Palamator
        /*
         * Cols=5 →       1             2             3             4             5...
         * Rows=4
         * ↓     Point[0, 0]   Point[0, 1]   Point[0, 2]   Point[0, 3]   Point[0, 4]   Point[0, 5]
         *           ・──────・──────・──────・──────・──────・
         *  1        │  Cell[1,1] │  Cell[1,2] │  Cell[1,3] │  Cell[1,4] │  Cell[1,5] │
         *           │ Align[1,1] │ Align[1,2] │ Align[1,3] │ Align[1,4] │ Align[1,5] │　　　　
         *           ├──────┼──────┼──────┼──────┼──────┤
         *  2        │  Cell[2,1] │  Cell[2,2] │  Cell[2,3] │  Cell[2,4] │  Cell[2,5] │
         *           │ Align[2,1] │ Align[2,2] │ Align[2,3] │ Align[2,4] │ Align[2,5] │　　　　
         *           ├──────┼──────┼──────┼──────┼──────┤
         *  3        │  Cell[3,1] │  Cell[3,2] │  Cell[3,3] │  Cell[3,4] │  Cell[3,5] │
         *           │ Align[3,1] │ Align[3,2] │ Align[3,3] │ Align[3,4] │ Align[3,5] │　　　　
         *           ├──────┼──────┼──────┼──────┼──────┤
         *  4        │  Cell[4,1] │  Cell[4,2] │  Cell[4,3] │  Cell[4,4] │  Cell[4,5] │
         *  :        │ Align[4,1] │ Align[4,2] │ Align[4,3] │ Align[4,4] │ Align[4,5] │　　　　
         *           ・──────・──────・──────・──────・──────・
         *        Point[4, 0]   Point[4, 1]   Point[4, 2]   Point[4, 3]   Point[4, 4]   Point[4, 5]
         *                                                                              
         *
         *           ┌-HolLW[0,1]-┬-HolLW[0,2]-┬-HolLW[0,3]-┬-HolLW[0,4]-┬-HolLW[0,5]-┐
         *           │            │            │            │            │            │　　　　
         * VtcLW[1,0]┥  VtcLW[1,1]┥  VtcLW[1,2]┥  VtcLW[1,3]┥  VtcLW[1,4]┥  VtcLW[1,5]┥　RowHeight[1] 　　　　
         *           │            │            │            │            │            │　　　　
         *           ├-HolLW[1,1]-┼-HolLW[1,2]-┼-HolLW[1,3]-┼-HolLW[1,4]-┼-HolLW[1,5]-┤
         *           │            │            │            │            │            │　　　　
         * VtcLW[2,0]┥  VtcLW[2,1]┥  VtcLW[2,2]┥  VtcLW[2,3]┥  VtcLW[2,4]┥  VtcLW[2,5]┥　RowHeight[2] 　　　　
         *           │            │            │            │            │            │　　　　
         *           ├-HolLW[2,1]-┼-HolLW[2,2]-┼-HolLW[2,3]-┼-HolLW[2,4]-┼-HolLW[2,5]-┤
         *           │            │            │            │            │            │　　　　
         * VtcLW[3,0]┥  VtcLW[3,1]┥  VtcLW[3,2]┥  VtcLW[3,3]┥  VtcLW[3,4]┥  VtcLW[3,5]┥　RowHeight[3] 　　　　
         *           │            │            │            │            │            │　　　　
         *           ├-HolLW[3,1]-┼-HolLW[3,2]-┼-HolLW[3,3]-┼-HolLW[3,4]-┼-HolLW[3,5]-┤
         *           │            │            │            │            │            │　　　　
         * VtcLW[4,0]┥  VtcLW[4,1]┥  VtcLW[4,2]┥  VtcLW[4,3]┥  VtcLW[4,4]┥  VtcLW[4,5]┥　RowHeight[4]　　　　
         *           │            │            │            │            │            │　　　　
         *           └-HolLW[4,1]-┴-HolLW[4,2]-┴-HolLW[4,3]-┴-HolLW[4,4]-┴-HolLW[4,4]-┘
         * 
         *           │            │            │            │            │            │　　　　
         *           └──────┴──────┴──────┴──────┴──────┘
         *              ColWidth[1]   ColWidth[2]   ColWidth[3]   ColWidth[4]   ColWidth[5]
         */
        #endregion

        private string[,] Cell;
        private int CellRows;
        private int CellCols;

        public int Rows { get { return this.CellRows; } }
        public int Cols { get { return this.CellCols; } }
        public string[,] AlignY;    // T:Top, B:Bottm, C:Center
        public string[,] AlignX;    // R:Right, L:Left, C:Center
        public float[,] HolLW;     // Holizonal Line Width
        public float[,] VtcLW;     // Vertical  Line Width
        public float[] RowHeight;
        public float[] ColWidth;
        public Table()
        {

        }
        public Table(int _rows, int _cols)
        {
            //** Row, Col Count
            CellRows = _rows;
            CellCols = _cols;
            //** Init Cell
            this.Cell = new string[_rows + 1, _cols + 1];
            for (int i = 1; i <= _rows; ++i)
                for (int j = 1; j <= _cols; ++j)
                    this.Cell[i, j] = "";

            //** Init Align
            this.AlignX = new string[_rows + 1, _cols + 1];
            for (int i = 1; i <= _rows; ++i)
                for (int j = 1; j <= _cols; ++j)
                    this.AlignX[i, j] = "C";
            this.AlignY = new string[_rows + 1, _cols + 1];
            for (int i = 1; i <= _rows; ++i)
                for (int j = 1; j <= _cols; ++j)
                    this.AlignY[i, j] = "C";

            //** Init RowHeight
            this.RowHeight = new float[_rows + 1];
            this.RowHeight[0] = 0;
            for (int i = 1; i <= _rows; ++i)
                this.RowHeight[i] = -1;  // 0 > Fit Strings Size

            //** Init ColWidth
            this.ColWidth = new float[_cols + 1];
            this.ColWidth[0] = 0;
            for (int j = 1; j <= _cols; ++j)
                this.ColWidth[j] = -1;   // 0 > Fit Strings Size

            //** Init Holizonal Line Width
            this.HolLW = new float[_rows + 1, _cols + 1];
            for (int i = 0; i <= _rows; ++i)
                this.SetHolLW(i, 0.2F);
            this.SetHolLW(0, 1);
            this.SetHolLW(_rows, 1);

            //** Init Holizonal Line Width
            this.VtcLW = new float[_rows + 1, _cols + 1];
            for (int j = 0; j <= _cols; ++j)
                this.SetVtcLW(j, 0.2F);
            this.SetVtcLW(0, 1);
            this.SetVtcLW(_cols, 1);
        }
        public string this[int row, int col]
        {
            set
            {
                if (value != null)
                    this.Cell[row, col] = value;
            }
            get { return this.Cell[row, col]; }
        }

        internal void PrintTable(PdfDocument _mc)
        {
            #region Base Info
            SizeF stringSize = _mc.MeasureString("W");
            ///////////////////////////////////////////////
            for (int i = 1; i <= CellRows; ++i)
            {
                if (RowHeight[i] < 0)
                    RowHeight[i] = stringSize.Height * 1.4F;
            }
            ///////////////////////////////////////////////
            for (int j = 1; j <= CellCols; ++j)
            {
                if (ColWidth[j] < 0)
                {
                    for (int i = 1; i <= CellRows; ++i)
                        ColWidth[j] = Math.Max(ColWidth[j], (Cell[i, j] + " ").Length * stringSize.Width);
                }
            }
            ///////////////////////////////////////////////
            PointF[,] point = new PointF[CellRows + 1, CellCols + 1];
            try
            {
                float y1 = _mc.CurrentY;
                for (int i = 0; i <= CellRows; ++i)
                {
                    y1 += RowHeight[i];
                    float x1 = _mc.CurrentX;
                    for (int j = 0; j <= CellCols; ++j)
                    {
                        x1 += ColWidth[j];
                        point[i, j].Y = y1;
                        point[i, j].X = x1;
                    }
                }
            }
            catch { Text.PrtText(_mc, "Error: PrintTable() - Case1"); }
            #endregion

            #region Draw Lines
            try
            {
                //for (int i = 0; i < CellRows; ++i)
                    for (int i = 0; i <= CellRows; ++i)
                        for (int j = 0; j < CellCols; ++j)
                        if (HolLW[i, j + 1] > 0)
                            Shape.DrawLine(_mc, point[i, j], point[i, j + 1], HolLW[i, j + 1]);

                //for (int i = 0; i < CellRows - 1; ++i)
                    for (int i = 0; i < CellRows; ++i)
                        for (int j = 0; j <= CellCols; ++j)
                        if (VtcLW[i + 1, j] > 0)
                            Shape.DrawLine(_mc, point[i, j], point[i + 1, j], VtcLW[i + 1, j]);

            }
            catch {
                Text.PrtText(_mc, "Error: PrintTable() - Case2");
            }
            #endregion

            #region Draw String
            try
            {
                for (int i = 1; i <= CellRows; ++i)
                {
                    for (int j = 1; j <= CellCols; ++j)
                    {
                        if (this.Cell[i, j].Length > 0)
                        {
                            SizeF textSize1 = _mc.MeasureString(this.Cell[i, j]);
                            SizeF textSize2 = _mc.MeasureString(" ");
                            float XPos = 0;
                            float YPos = 0;
                            switch (AlignX[i, j])
                            {
                                case "L":
                                    XPos = point[i, j - 1].X + textSize2.Width;
                                    break;
                                case "R":
                                    XPos = point[i, j].X - (textSize1.Width + textSize2.Width);
                                    break;
                                case "C":
                                    XPos = point[i, j - 1].X + (ColWidth[j] - textSize1.Width) / 2;
                                    break;
                            }
                            switch (AlignY[i, j])
                            {
                                case "T":
                                    YPos = point[i - 1, j].Y - RowHeight[i];
                                    break;
                                case "B":
                                    YPos = point[i, j].Y;
                                    break;
                                case "C":
                                    YPos = point[i , j].Y -(( RowHeight[i]+ textSize1.Height )/2) ;
                                    break;
                            }
                            _mc.CurrentX = XPos;
                            _mc.CurrentY = YPos;
                            Text.PrtText(_mc, this.Cell[i, j]);
                        }
                    }
                }
            }
            catch { Text.PrtText(_mc, "Error: PrintTable() - Case3"); }
            #endregion

            _mc.CurrentX = point[CellRows, CellCols].X;
            _mc.CurrentY = point[CellRows, CellCols].Y;
        }

        public float GetTableHeight()
        {
            float result = 0;
            for (int i = 0; i <= CellRows; ++i)
                result += RowHeight[i];
            return result;
        }
        public float GetTableWidth()
        {
            float result = 0;
            for (int j = 0; j <= CellCols; ++j)
                result += ColWidth[j];
            return result;
        }
        public void SetHolLW(int row, float LineWidth)
        {
            for (int j = 0; j <= Cols; ++j)
                this.HolLW[row, j] = LineWidth;
        }
        public void SetVtcLW(int col, float LineWidth)
        {
            for (int i = 0; i <= Rows; ++i)
                this.VtcLW[i, col] = LineWidth;
        }
    }
}

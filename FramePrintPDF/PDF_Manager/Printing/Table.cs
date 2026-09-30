using PDF_Manager.Printing.Comon;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace PDF_Manager.Printing
{
    public class Table
    {
        #region This Class Palamator
        /*
         * Cols=5 →       0             1             2             3             4...
         * Rows=4
         * ↓     Point[0, 0]   Point[0, 1]   Point[0, 2]   Point[0, 3]   Point[0, 4]   Point[0, 5]
         *           ・──────・──────・──────・──────・──────・
         *  0        │  Cell[0,0] │  Cell[0,1] │  Cell[0,2] │  Cell[0,3] │  Cell[0,4] │
         *           │ Align[0,0] │ Align[0,1] │ Align[0,2] │ Align[0,3] │ Align[0,4] │　　　　
         *           ├──────┼──────┼──────┼──────┼──────┤
         *  1        │  Cell[1,0] │  Cell[1,1] │  Cell[1,2] │  Cell[1,3] │  Cell[1,4] │
         *           │ Align[1,0] │ Align[1,1] │ Align[1,2] │ Align[1,3] │ Align[1,4] │　　　　
         *           ├──────┼──────┼──────┼──────┼──────┤
         *  2        │  Cell[2,0] │  Cell[2,1] │  Cell[2,2] │  Cell[2,3] │  Cell[2,4] │
         *           │ Align[2,0] │ Align[2,1] │ Align[2,2] │ Align[2,3] │ Align[2,4] │　　　　
         *           ├──────┼──────┼──────┼──────┼──────┤
         *  3        │  Cell[3,0] │  Cell[3,1] │  Cell[3,2] │  Cell[3,3] │  Cell[3,4] │
         *  :        │ Align[3,0] │ Align[3,1] │ Align[3,2] │ Align[3,3] │ Align[3,4] │　　　　
         *           ・──────・──────・──────・──────・──────・
         *        Point[4, 0]   Point[4, 1]   Point[4, 2]   Point[4, 3]   Point[4, 4]   Point[4, 5]
         *                                                                              
         *
         *           ┌-HolLW[0,0]-┬-HolLW[0,1]-┬-HolLW[0,2]-┬-HolLW[0,3]-┬-HolLW[0,4]-┐
         *           │            │            │            │            │            │　　　　
         * VtcLW[0,0]┥  VtcLW[0,1]┥  VtcLW[0,2]┥  VtcLW[0,3]┥  VtcLW[0,4]┥  VtcLW[0,5]┥　RowHeight[0] 　　　　
         *           │            │            │            │            │            │　　　　
         *           ├-HolLW[1,0]-┼-HolLW[1,1]-┼-HolLW[1,2]-┼-HolLW[1,3]-┼-HolLW[1,4]-┤
         *           │            │            │            │            │            │　　　　
         * VtcLW[1,0]┥  VtcLW[1,1]┥  VtcLW[1,2]┥  VtcLW[1,3]┥  VtcLW[1,4]┥  VtcLW[1,5]┥　RowHeight[1] 　　　　
         *           │            │            │            │            │            │　　　　
         *           ├-HolLW[2,0]-┼-HolLW[2,1]-┼-HolLW[2,2]-┼-HolLW[2,3]-┼-HolLW[2,4]-┤
         *           │            │            │            │            │            │　　　　
         * VtcLW[2,0]┥  VtcLW[2,1]┥  VtcLW[2,2]┥  VtcLW[2,3]┥  VtcLW[2,4]┥  VtcLW[2,5]┥　RowHeight[2] 　　　　
         *           │            │            │            │            │            │　　　　
         *           ├-HolLW[3,0]-┼-HolLW[3,1]-┼-HolLW[3,2]-┼-HolLW[3,3]-┼-HolLW[3,4]-┤
         *           │            │            │            │            │            │　　　　
         * VtcLW[3,0]┥  VtcLW[3,1]┥  VtcLW[3,2]┥  VtcLW[3,3]┥  VtcLW[3,4]┥  VtcLW[3,5]┥　RowHeight[3]　　　　
         *           │            │            │            │            │            │　　　　
         *           └-HolLW[4,0]-┴-HolLW[4,1]-┴-HolLW[4,2]-┴-HolLW[4,3]-┴-HolLW[4,4]-┘
         * 
         *           │            │            │            │            │            │　　　　
         *           └──────┴──────┴──────┴──────┴──────┘
         *              ColWidth[0]   ColWidth[1]   ColWidth[2]   ColWidth[3]   ColWidth[4]
         */
        #endregion

        private string[,] Cell;
        private int CellRows;
        private int CellCols;

        public int Rows { get { return this.CellRows; } }
        public int Columns { get { return this.CellCols; } }

        public object Alignx { get; internal set; }

        public string[,] AlignY;    // T:Top, B:Bottm, C:Center
        public string[,] AlignX;    // R:Right, L:Left, C:Center
        public double[,] HolLW;     // Holizonal Line Width
        public double[,] VtcLW;     // Vertical  Line Width
        public double[] RowHeight;
        public double[] ColWidth;

        // 印刷に関する設定
        public double LineSpacing3; // 小さい（テーブル内などの）改行高さ pt ポイント


        private const double DEFAULT_LINE_WIDTH = 0; // デフォルトの線幅

        public Table(int _rows, int _cols)
        {
            // 印刷に関する初期設定
            this.LineSpacing3 = printManager.FontHeight;

            // 表題部分を初期化する
            this.CellRows = 0;
            this.CellCols = 0;
            this.ReDim(_rows, _cols);
        }

        /// <summary>
        /// 行を追加する
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        public void ReDim(int row = -1, int col = -1)
        {
            if (row < 0)
                row = this.Rows;
            if (col < 0)
                col = this.Columns;


            // 昔の情報を取っておく
            var oldCell = (this.Cell != null) ? this.Cell.Clone() as string[,] : null;
            var oldAlignX = (this.AlignX != null) ? this.AlignX.Clone() as string[,] : null;
            var oldAlignY = (this.AlignY != null) ? this.AlignY.Clone() as string[,] : null;
            var oldRowHeight = (this.RowHeight != null) ? this.RowHeight.Clone() as double[] : null;
            var oldColWidth = (this.ColWidth != null) ? this.ColWidth.Clone() as double[] : null;
            var oldHolLW = (this.HolLW != null) ? this.HolLW.Clone() as double[,] : null;
            var oldVtcLW = (this.VtcLW != null) ? this.VtcLW.Clone() as double[,] : null;

            this.CellRows = Math.Min(row, this.Rows);
            this.CellCols = Math.Min(col, this.CellCols);

            //** Init Cell
            this.Cell = new string[row, col];
            for (int i = 0; i < this.CellRows; ++i)
                for (int j = 0; j < this.CellCols; ++j)
                    this.Cell[i, j] = oldCell[i, j];

            //** Init Align
            this.AlignX = new string[row, col];
            for (int i = 0; i < this.CellRows; ++i)
                for (int j = 0; j < this.CellCols; ++j)
                    this.AlignX[i, j] = oldAlignX[i, j];

            this.AlignY = new string[row, col];
            for (int i = 0; i < this.CellRows; ++i)
                for (int j = 0; j < this.CellCols; ++j)
                    this.AlignY[i, j] = oldAlignY[i, j];

            //** Init RowHeight
            this.RowHeight = new double[row];
            for (int i = 0; i < this.CellRows; ++i)
                this.RowHeight[i] = oldRowHeight[i];

            //** Init ColWidth
            this.ColWidth = new double[col];
            for (int j = 0; j < this.CellCols; ++j)
                this.ColWidth[j] = oldColWidth[j];

            //** Init Holizonal Line Width
            this.HolLW = new double[row + 1, col];
            for (int i = 0; i < this.CellRows; ++i)
                this.SetHolLW(i, this.HolLW[i, 0]);
            for (int i = this.CellRows; i <= row; ++i)
                for (int j = 0; j < col; ++j)
                    this.HolLW[i, j] = Table.DEFAULT_LINE_WIDTH;

            //** Init Vertical Line Width
            this.VtcLW = new double[row, col + 1];
            for (int j = 0; j < this.CellCols; ++j)
                this.SetVtcLW(j, this.VtcLW[0, j]);
            for (int j = this.CellCols; j <= col; ++j)
                for (int i = 0; i < row; ++i)
                    this.VtcLW[i, j] = Table.DEFAULT_LINE_WIDTH;

            //** Row, Col Count
            this.CellRows = row;
            this.CellCols = col;

        }

        /// <summary>
        /// セルの値を取得・設定する
        /// </summary>
        /// <param name="row">行番号 0～</param>
        /// <param name="col">列番号 0～</param>
        /// <returns></returns>
        public string this[int row, int col]
        {
            set
            {
                if (this.Cell.GetLength(0) <= row)
                {
                    return;
                }
                this.Cell[row, col] = value;
            }
            get
            {
                try
                {
                    return this.Cell[row, col];
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 現在の Table の簡易コピーを作成します。
        /// </summary>
        /// <returns></returns>
        public Table Clone()
        {
            // MemberwiseCloneメソッドを使用
            return (Table)this.MemberwiseClone();
        }

        /// <summary>
        /// テーブルの高さ
        /// </summary>
        /// <returns>pt</returns>
        public double GetTableHeight()
        {
            double result = 0;
            for (int i = 0; i < this.CellRows; ++i)
                if (this.RowHeight[i] == double.NaN || this.RowHeight[i] <= 0)
                    result += this.LineSpacing3;
                else
                    result += this.RowHeight[i];
            return result;
        }

        /// <summary>
        /// テーブルの幅
        /// </summary>
        /// <returns>pt</returns>
        public double GetTableWidth()
        {
            double result = 0;
            for (int i = 0; i < this.CellCols; ++i)
                if (this.ColWidth[i] == double.NaN || this.ColWidth[i] <= 0)
                    result += this.LineSpacing3;
                else
                    result += this.ColWidth[i];
            return result;
        }

        /// <summary>
        /// 横罫線幅の設定
        /// </summary>
        /// <param name="row">行番号</param>
        /// <param name="LineWidth">線幅</param>
        public void SetHolLW(int row, double LineWidth)
        {
            for (int j = 0; j < this.Columns; ++j)
                this.HolLW[row, j] = LineWidth;
        }

        /// <summary>
        /// 縦罫線幅の設定
        /// </summary>
        /// <param name="col">列番号</param>
        /// <param name="LineWidth">線幅</param>
        public void SetVtcLW(int col, double LineWidth)
        {
            for (int i = 0; i < this.Rows; ++i)
                this.VtcLW[i, col] = LineWidth;
        }

        /// <summary>
        /// 何行印刷できるか調べる
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="H1">デフォルト（改ページした場合）の印字位置</param>
        /// <returns>
        /// return[0] = 1ページ目の印刷可能行数, 
        /// return[1] = 2ページ目以降の印刷可能行数
        /// </returns>
        internal int[] getPrintRowCount(PdfDocument mc, int titles_count = 1)
        {
            // 表題の印字高さ + 改行高
            double H2 = this.GetTableHeight();

            // 1行当りの高さ + 改行高
            double H3 = this.LineSpacing3;


            //// 2ページ目以降（ページ全体を使ってよい場合）の行数
            //double Hx = mc.currentPageSize.Height;
            //// 高さはタイトルの分だけ小さくなる
            //Hx -= printManager.titlePos.Y;
            //Hx -= printManager.FontHeight;
            //Hx -= printManager.LineSpacing2;
            //Hx -= printManager.H1;
            //Hx -= H2;

            //int rows2 = (int)(Hx / H3); // 切り捨て

            //Hx = mc.currentPageSize.Height;
            //// 1ページ目（現在位置から）の行数
            //Hx -= mc.currentPos.Y;
            //// 高さはマージンの分だけ小さくなる
            //Hx -= mc.Margine.Top;
            //// 高さはタイトルの分だけ小さくなる
            //Hx -= printManager.titlePos.Y;
            //Hx -= printManager.FontHeight;
            //Hx -= printManager.LineSpacing2;

            //int rows1 = (int)(Hx / H3); // 切り捨て
            double SettingHeight = mc.Margine.Top;
            SettingHeight += printManager.titlePos.Y;
            SettingHeight += printManager.FontHeight;
            SettingHeight += printManager.LineSpacing2;

            //２枚目以降の時
            double Hx = mc.currentPageSize.Height;

            // mc.currentPos.Yの再現
            Hx -= SettingHeight;
            // 高さはマージンの分だけ小さくなる
            Hx -= mc.Margine.Bottom;

            int rows2 = (int)(Hx / H3); // 切り捨て

            //１枚目の時
            Hx = mc.currentPageSize.Height;
            // 1ページ目（現在位置から）の行数
            Hx -= mc.currentPos.Y;
            // 高さはマージンの分だけ小さくなる
            Hx -= mc.Margine.Bottom;

            //if (mc.currentPos.Y > SettingHeight)
            //{
            //    Hx -= printManager.LineSpacing1;
            //}

            int rows1 = (int)(Hx / H3); // 切り捨て

            return new int[] { rows1, rows2 };
        }

        /// <summary>
        /// テーブル先頭から何行が印刷できるか調べる
        /// [0]：現在のページへの印刷行数
        /// [1]：空ページへの印刷行数
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="titleRows">タイトルの行数</param>
        /// <param name="requiresPageTitle">タイトル行を印刷するか。デフォルトはtrue</param>
        /// <returns></returns>
        internal int[] EstimatePrintableRows(PdfDocument mc, int titleRows, bool requiresPageTitle = true)
        {
            // printManager.printTableContents()で印刷されるタイトルの高さ
            var titleHeight0 = printManager.LineSpacing2 * (requiresPageTitle ? titleRows : 0);
            var titleHeight1 = printManager.LineSpacing2 * titleRows;

            return new[]
            {
                mc.currentPage.Height - mc.currentPos.Y - mc.Margine.Bottom - titleHeight0,
                mc.currentPage.Height - mc.initialPos.Y - mc.Margine.Bottom - titleHeight1,
            }.Select(textHeight =>
            {
                var rows = 0;
                for (var r = 0; r < RowHeight.Length; r++)
                {
                    mc.CheckBudget();
                    var height = RowHeight[r];
                    if (double.IsNaN(height) || height <= 0)
                    {
                        height = LineSpacing3;
                    }
                    if ((textHeight -= height) < 0)
                    {
                        break;
                    }
                    rows++;
                }
                return rows;
            }).ToArray();
        }

        /// <summary>
        /// 先頭の<paramref name="rows"/>行で構成されるテーブルを生成する
        /// </summary>
        /// <param name="rows">行数</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal Table Subtable(int rows)
        {
            if (rows > CellRows)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), $"{nameof(rows)} > {CellRows}");
            }

            // 新しいテーブルを生成
            var table = new Table(rows, CellCols);

            // 新しいテーブルにrows行分のデータをコピー
            Array.Copy(Cell, table.Cell, rows * CellCols);
            Array.Copy(AlignX, table.AlignX, rows * CellCols);
            Array.Copy(AlignY, table.AlignY, rows * CellCols);
            Array.Copy(RowHeight, table.RowHeight, rows);
            Array.Copy(ColWidth, table.ColWidth, CellCols);
            Array.Copy(HolLW, table.HolLW, (rows + 1) * CellCols);
            Array.Copy(VtcLW, table.VtcLW, rows * (CellCols + 1));

            return table;
        }

        /// <summary>
        /// 多段組み情報
        /// </summary>
        internal readonly struct NupInfo
        {
            /// <summary>
            /// 先頭列のインデックス
            /// </summary>
            public readonly int ColS;
            /// <summary>
            /// 末尾列のインデックス
            /// </summary>
            public readonly int ColE;

            /// <summary>
            /// 多段組み情報の生成
            /// </summary>
            /// <param name="colS">先頭列のインデックス</param>
            /// <param name="colE">末尾列のインデックス</param>
            public NupInfo(int colS, int colE) => (ColS, ColE) = (colS, colE);
        }
        /// <summary>
        /// 多段組みしない場合のための多段組み情報
        /// </summary>
        internal static readonly NupInfo[] OneUpInfo = new[] { new NupInfo(0, 0), };

        /// <summary>
        /// 多段組み
        /// </summary>
        /// <param name="nupInfo">多段組み情報の配列</param>
        /// <param name="headerRows">ヘッダの行数</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal Table Nup(NupInfo[] nupInfo, int headerRows)
        {
            if (nupInfo is null)
                throw new ArgumentNullException(nameof(nupInfo));
            if (nupInfo.Length < 1)
                throw new ArgumentException($"{nameof(nupInfo)}.Length", $"{nameof(nupInfo)}.Length < 1");

            if (nupInfo.Length == 1)
            {
                return this;
            }

            headerRows = Math.Max(headerRows, 0);
            if (headerRows >= CellRows)
            {
                return this;
            }

            var newRows = (CellRows - headerRows + nupInfo.Length - 1) / nupInfo.Length + headerRows;

            var table = new Table(newRows, CellCols);

            // newRows行分のデータ(ヘッダ行込み)をコピー
            Array.Copy(Cell, table.Cell, newRows * CellCols);
            Array.Copy(AlignX, table.AlignX, newRows * CellCols);
            Array.Copy(AlignY, table.AlignY, newRows * CellCols);
            Array.Copy(RowHeight, table.RowHeight, newRows);
            Array.Copy(ColWidth, table.ColWidth, CellCols);
            Array.Copy(HolLW, table.HolLW, (newRows + 1) * CellCols);
            Array.Copy(VtcLW, table.VtcLW, newRows * (CellCols + 1));

            // 残りのデータは多段組み
            for (var row = newRows; row < CellRows; row++)
            {
                var n = (row - headerRows) / (newRows - headerRows);
                var r = (row - headerRows) % (newRows - headerRows) + headerRows;

                for (var col = nupInfo[0].ColS; col <= nupInfo[0].ColE; col++)
                {
                    var c = col - nupInfo[0].ColS + nupInfo[n].ColS;

                    table.Cell[r, c] = Cell[row, col];
                    table.AlignX[r, c] = AlignX[row, col];
                    table.AlignY[r, c] = AlignY[row, col];
                    table.HolLW[r + 1, c] = HolLW[row + 1, col];
                    table.VtcLW[r, c + 1] = VtcLW[row, col + 1];
                }
            }

            return table;
        }

        /// <summary>
        /// テーブルの先頭の<paramref name="rows"/>行からヘッダ行以外を削除する。
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="headerRows">ヘッダの行数</param>
        /// <exception cref="NotImplementedException"></exception>
        internal void RemoveRows(int rows, int headerRows)
        {
            if (rows > CellRows)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), $"{nameof(rows)} > {CellRows}");
            }

            headerRows = Math.Max(headerRows, 0);
            if (headerRows >= rows)
            {
                return;
            }

            // テーブルの全データをoldTableに移動
            var oldTable = Clone();
            CellRows = CellCols = 0;
            Cell = null;
            AlignX = AlignY = null;
            HolLW = VtcLW = null;
            RowHeight = ColWidth = null;

            ReDim(oldTable.CellRows - (rows - headerRows), oldTable.CellCols);

            // ヘッダ行のコピー
            Array.Copy(oldTable.Cell, Cell, headerRows * CellCols);
            Array.Copy(oldTable.AlignX, AlignX, headerRows * CellCols);
            Array.Copy(oldTable.AlignY, AlignY, headerRows * CellCols);
            Array.Copy(oldTable.RowHeight, RowHeight, headerRows);
            Array.Copy(oldTable.HolLW, HolLW, (headerRows + 1) * CellCols); // ヘッダ行とデータ行の間の水平罫線のコピーはこちらで実施
            Array.Copy(oldTable.VtcLW, VtcLW, headerRows * (CellCols + 1));

            // データ行のコピー
            Array.Copy(oldTable.Cell, rows * CellCols, Cell, headerRows * CellCols, (CellRows - headerRows) * CellCols);
            Array.Copy(oldTable.AlignX, rows * CellCols, AlignX, headerRows * CellCols, (CellRows - headerRows) * CellCols);
            Array.Copy(oldTable.AlignY, rows * CellCols, AlignY, headerRows * CellCols, (CellRows - headerRows) * CellCols);
            Array.Copy(oldTable.RowHeight, rows, RowHeight, headerRows, CellRows - headerRows);
            Array.Copy(oldTable.HolLW, (rows + 1) * CellCols, HolLW, (headerRows + 1) * CellCols, (CellRows - headerRows) * CellCols); // ヘッダとデータ行の間の水平罫線のコピーは対象外
            Array.Copy(oldTable.VtcLW, rows * (CellCols + 1), VtcLW, headerRows * (CellCols + 1), (CellRows - headerRows) * (CellCols + 1));

            // セル幅のコピー
            Array.Copy(oldTable.ColWidth, ColWidth, CellCols);

            // ヘッダ行の直後の行の高さを継承させる
            if (CellRows > headerRows)
                RowHeight[headerRows] = oldTable.RowHeight[headerRows];
        }

        /// <summary>
        /// 印刷する
        /// </summary>
        /// <param name="_mc"></param>
        internal void PrintTable(PdfDocument _mc, XFont font = null)
        {
            _mc.CheckBudget();
            #region Base Info
            
            XSize[,] textSize1 = new XSize[this.Rows, this.Columns];
            for (int i = 0; i < this.CellRows; ++i)
            {
                _mc.CheckBudget();
                for (int j = 0; j < this.CellCols; ++j)
                {
                    if (this.Cell[i, j] == null)
                        continue;
                    textSize1[i, j] = _mc.MeasureString(this.Cell[i, j]);
                }
            }

            ///////////////////////////////////////////////
            for (int i = 0; i < this.CellRows; ++i)
            {
                if (this.RowHeight[i] == double.NaN || this.RowHeight[i] <= 0)
                    this.RowHeight[i] = this.LineSpacing3;
            }

            ///////////////////////////////////////////////
            for (int j = 0; j < this.CellCols; ++j)
            {
                if (this.ColWidth[j] == double.NaN || this.ColWidth[j] <= 0)
                    for (int i = 0; i < this.CellRows; ++i)
                        this.ColWidth[j] = Math.Max(this.ColWidth[j], textSize1[i, j].Width);
            }
            ///////////////////////////////////////////////
            XPoint[,] point = new XPoint[this.CellRows + 1, this.CellCols + 1];
            try
            {
                double y1 = _mc.currentPos.Y;
                for (int i = 0; i <= this.CellRows; ++i)
                {
                    _mc.CheckBudget();
                    if (0 < i)
                        y1 += this.RowHeight[i - 1];

                    double x1 = _mc.currentPos.X;
                    for (int j = 0; j <= this.CellCols; ++j)
                    {
                        if(0 < j)
                            x1 += this.ColWidth[j - 1];
                        point[i, j].Y = y1;
                        point[i, j].X = x1;
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { Text.PrtText(_mc, "Error: PrintTable() - Base Info"); }
            #endregion

            #region Draw Lines
            try
            {
                int i, j;
                try
                {
                    for (i = 0; i < this.CellRows; ++i)
                    {
                        _mc.CheckBudget();
                        for (j = 0; j < this.CellCols; ++j)
                            if (HolLW[i, j] != double.NaN)
                                if (0 < HolLW[i, j])
                                    Shape.DrawLine(_mc, point[i, j], point[i, j + 1], HolLW[i, j]);
                    }

                    for (i = 0; i < CellRows; ++i)
                    {
                        _mc.CheckBudget();
                        for (j = 0; j < CellCols; ++j)
                            if (VtcLW[i, j] != double.NaN)
                                if (0 < VtcLW[i, j])
                                    Shape.DrawLine(_mc, point[i, j], point[i + 1, j], VtcLW[i, j]);
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    Text.PrtText(_mc, "Error: PrintTable() - Draw Lines");
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
            #endregion

            #region Draw String
            try
            {
                for (int i = 0; i < CellRows; ++i)
                {
                    _mc.CheckBudget();
                    for (int j = 0; j < CellCols; ++j)
                    {
                        if (this.Cell[i, j] == null)
                            continue;

                        if (this.Cell[i, j].Length == 0)
                            continue;

                        double XPos = 0;
                        double YPos = 0;

                        XStringFormat align;

                        switch (AlignY[i, j])
                        {
                            case "T": YPos = point[i, j].Y;
                                break;
                            case "C": YPos = (point[i, j].Y + point[i + 1, j].Y) / 2;
                                break;
                            default:  YPos = point[i + 1, j].Y;
                                break;
                        }
                        switch (AlignX[i, j])
                        {
                            case "L":
                                XPos = point[i, j].X;
                                switch (AlignY[i, j]){
                                    case "T": align = XStringFormats.TopLeft;   
                                        break;
                                    case "C": align = XStringFormats.CenterLeft;
                                        break;
                                    default: align = XStringFormats.BottomLeft;
                                        break;
                                }
                                break;
                            case "R":
                                XPos = point[i, j + 1].X;
                                switch (AlignY[i, j]) {
                                    case "T": align = XStringFormats.TopRight;
                                        break;
                                    case "C": align = XStringFormats.CenterRight;
                                        break;
                                    default: align = XStringFormats.BottomRight;
                                        break;
                                }
                                break;
                            default: // case "C"
                                XPos = (point[i, j].X + point[i, j + 1].X) / 2;
                                switch (AlignY[i, j]) {
                                    case "T": align = XStringFormats.TopCenter;
                                        break;
                                    case "C": align = XStringFormats.Center;
                                        break;
                                    default: align = XStringFormats.BottomCenter;
                                        break;
                                }
                                break;
                        }
                        _mc.currentPos.X = XPos;
                        _mc.currentPos.Y = YPos;
                        Text.PrtText(_mc, this.Cell[i, j], font, align: align);
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { Text.PrtText(_mc, "Error: PrintTable() - Draw String"); }
            #endregion

            _mc.currentPos.X = point[this.CellRows, this.CellCols].X;
            _mc.currentPos.Y = point[this.CellRows, this.CellCols].Y;
        }

        /// <summary>
        /// テーブルにデータを追加する
        /// </summary>
        /// <param name="target"></param>
        public void Add(Table target)
        {
            var oldRowsCount = this.Rows;
            var newRowsCount = this.Rows + target.Rows;
            this.ReDim(row: newRowsCount);

            for (int r = oldRowsCount; r< newRowsCount; r++)
            {
                for(int c= 0; c < this.Columns; c++)
                {
                    this[r,c] = target[r- oldRowsCount, c];
                    this.AlignX[r, c] = target.AlignX[r - oldRowsCount, c];
                    this.RowHeight[r] = target.RowHeight[r - oldRowsCount];
                }
            }

            this.RowHeight[oldRowsCount] = printManager.LineSpacing1;
        }

        /// <summary>
        /// 行を削除する
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        public void ClearDraft()
        {
            // 昔の情報を取っておく
            var oldCell = (this.Cell != null) ? this.Cell.Clone() as string[,] : null;
            var oldAlignX = (this.AlignX != null) ? this.AlignX.Clone() as string[,] : null;
            var oldAlignY = (this.AlignY != null) ? this.AlignY.Clone() as string[,] : null;
            var oldRowHeight = (this.RowHeight != null) ? this.RowHeight.Clone() as double[] : null;
            var oldColWidth = (this.ColWidth != null) ? this.ColWidth.Clone() as double[] : null;
            var oldHolLW = (this.HolLW != null) ? this.HolLW.Clone() as double[,] : null;
            var oldVtcLW = (this.VtcLW != null) ? this.VtcLW.Clone() as double[,] : null;

            var oldRows = this.Rows;
            var oldCols = this.Columns;

            var nullnumber = new List<int>();

            for (int i = 0; i < this.CellRows; ++i)// 消した時の行数の算定
            {
                int nullcount = 0;

                for (int j = 0; j < this.CellCols; ++j)
                {
                    if(i == 0)
                    {

                    }
                    else if (this.Cell[i, j] == "" || this.Cell[i, j] == null)
                    {
                        nullcount++;
                    }
                }

                if (nullcount == oldCols)
                {
                    nullnumber.Add(i);
                }
            }

            //** Init Cell

            int newRows = oldRows - nullnumber.Count;

            this.Cell = new string[newRows, oldCols];
            this.AlignX = new string[newRows, oldCols];
            this.AlignY = new string[newRows, oldCols];
            this.RowHeight = new double[newRows];
            this.ColWidth = new double[oldCols];
            this.HolLW = new double[newRows + 1, oldCols];
            this.VtcLW = new double[newRows, oldCols + 1];

            //** Row, Col Count
            this.CellRows = newRows;
            this.CellCols = oldCols;

            int _nullcount = 0;

            for (int i = 0; i < newRows; ++i)// 白紙を詰めたTableの作成
            {
                for (int j = 0; j < this.CellCols; ++j)
                {
                    if(nullnumber.Count > 0 && _nullcount < nullnumber.Count)
                    {
                        while(i + _nullcount == nullnumber[_nullcount])
                        {
                            _nullcount++;
                            if (nullnumber.Count == _nullcount)
                                break;
                        }
                    }
                    this.Cell[i, j] = oldCell[i+ _nullcount, j];
                    this.AlignX[i, j] = oldAlignX[i+ _nullcount, j];
                    this.AlignY[i, j] = oldAlignY[i+ _nullcount, j];
                    this.HolLW[i, j] = oldHolLW[i+ _nullcount, j];
                    this.VtcLW[i, j] = oldVtcLW[i+ _nullcount, j];

                    if(j == 0)
                    {
                        this.RowHeight[i] = oldRowHeight[i + _nullcount];
                        this.SetHolLW(i, this.HolLW[i, 0]);
                    }
                }

                this.VtcLW[i, CellCols] = oldVtcLW[i + _nullcount, CellCols];
            }

            for (int j = 0; j < this.CellCols; ++j)
            {
                this.ColWidth[j] = oldColWidth[j];
                this.SetVtcLW(j, this.VtcLW[0, j]);

                this.HolLW[CellRows, j] = oldHolLW[CellRows + _nullcount, j];
            }
        }

        /// <summary>
        /// Tableを入りきるように分割する
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        internal List<Table> SplitTable(int rows, Table myTable)
        {
            var _page = new List<Table>();

            var tmp1 = this.Clone();

            var roop_count = tmp1.Rows / rows + 1;

            var Rows = 0;

            for (int i = 0; i < roop_count; ++i)
            {
                if (tmp1.Rows <= 0)
                    break;

                var tmp2 = myTable.Clone();

                var r = tmp2.Rows;

                tmp2.ReDim(row: r + rows);

                var _r = myTable.Rows;

                if (i == 0)
                {
                    tmp2[0, 0] = tmp1[0, 0];
                }

                for (int ii = 0; ii < rows; ii++)
                {
                    for (int c = 0; c < tmp1.Columns; c++)
                    {
                        var range = tmp1[Rows+ _r, c];
                        if (range == null)
                            continue;

                        tmp2[r, c] = range;
                        tmp2.AlignX[r, c] = tmp1.AlignX[Rows+ _r, c];
                        tmp2.RowHeight[r] = tmp1.RowHeight[Rows+ _r];

                        tmp1[r, c] = null;
                    }
                    r++;
                    Rows ++;
                }
                if (tmp2.Rows <= _r)
                    _r = tmp2.Rows - 1;
                tmp2.RowHeight[_r] = printManager.LineSpacing2;

                _page.Add(tmp2);
                if(tmp1.Rows - Rows < rows)
                {
                    rows = tmp1.Rows - Rows -_r;
                }
            }
           return _page;
        }

    }
}

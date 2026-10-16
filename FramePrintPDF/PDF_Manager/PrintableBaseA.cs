using PDF_Manager.Printing;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;

namespace PDF_Manager
{
    /// <summary>
    /// 計算ケースを持たない下記8種のキー向けの印刷処理を定義したクラス
    ///     combine,
    ///     define,
    ///     member,
    ///     loadName,
    ///     node,
    ///     notice_points,
    ///     pickup,
    ///     shell
    /// </summary>
    abstract class PrintableBaseA : IPrintable
    {
        /// <summary>
        /// 印刷対象データが存在するか
        /// </summary>
        /// <returns></returns>
        protected abstract bool HasAnyData();
        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="data"></param>
        /// <param name="titles">タイトル行の配列</param>
        /// <param name="headerRows">ヘッダの行数</param>
        /// <param name="nupInfo">多段組み情報</param>
        protected abstract void PrintInit(PdfDocument mc, PrintData data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo);
        /// <summary>
        /// 全てのデータからテーブルを生成(ヘッダ行は多段組みした状態、データ行は多段組みしていない状態)
        /// </summary>
        /// <returns></returns>
        protected abstract Table GetTable();
        /// <summary>
        /// 改ページ判定
        /// </summary>
        /// <param name="printableRows">ページに出力できる行数(ヘッダを含む)</param>
        /// <param name="tableRows">テーブルの行数(ヘッダを含む)</param>
        /// <param name="headerRows">ヘッダの行数</param>
        /// <returns></returns>
        protected virtual bool RequiresNewpage(int[] printableRows, int tableRows, int headerRows)
        {
            // データ行が1行も入らない場合は改ページ
            return printableRows[0] < headerRows + 1;
        }

        /// <summary>
        /// 印刷
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="data"></param>
        public void printPDF(PdfDocument mc, PrintData data, ref int indexPage)
        {
            // データがなければ何もしない
            if (!HasAnyData())
                return;

            // タイトル などの初期化
            PrintInit(mc, data, out string[] titles, out int headerRows, out Table.NupInfo[] nupInfo);

            // 全てのデータからテーブルを生成(ヘッダ行は多段組みした状態、データ行は多段組みしていない状態)
            var table = GetTable();

            while (table.Rows - headerRows > 0)
            {
                // 印刷可能な行数
                var rows = table.EstimatePrintableRows(mc, titles.Length);

                // 必要に応じて改ページ
                if (RequiresNewpage(rows, table.Rows, headerRows))
                {
                    mc.NewPage(ref indexPage);
                    continue;
                }

                // 多段組みに必要な全てのデータ行の数
                rows[0] = Math.Min((rows[0] - headerRows) * nupInfo.Length + headerRows, table.Rows);

                // 多段組みに必要な全ての行データを含むテーブルを抽出(ヘッダ行は多段組みした状態、データ行は多段組みしていない状態)
                var subtable = table.Subtable(rows[0]);

                // データ行の多段組み
                if (nupInfo.Length > 1)
                {
                    subtable = subtable.Nup(nupInfo, headerRows);
                }

                //set font 
                var font = mc.TableFont(data.language);

                // 表の印刷
                printManager.printTableContents(mc, subtable, titles, font: font);

                // 抽出したデータ行を削除(ヘッダ行は残す)
                table.RemoveRows(rows[0], headerRows);
            }
        }
    }
}

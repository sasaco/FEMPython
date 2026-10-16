using PDF_Manager.Printing;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager
{
    /// <summary>
    /// 計算ケースあり、計算条件なしの下記8種のキー向けの印刷処理を定義したクラス
    ///     element,
    ///     fix_member,
    ///     fix_node,
    ///     joint,
    ///     load,
    ///     disg,
    ///     fsec,
    ///     reac
    /// </summary>
    abstract class PrintableBaseB : IPrintable
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
        /// 全てのデータからテーブルのシーケンスを生成(ヘッダ行は多段組みした状態、データ行は多段組みしていない状態)
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected abstract IEnumerable<Table> GetTables(PdfDocument mc, PrintData data, int indexPage);
        /// <summary>
        /// 印刷可能な行数を調べる
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="table">印刷対象のテーブル</param>
        /// <param name="titles">タイトルの行数</param>
        /// <param name="requiresPageTitle">タイトルを表示するか。デフォルトはtrue</param>
        /// <returns></returns>
        protected virtual int[] EstimatePrintableTableRows(PdfDocument mc, Table table, int titleRows, bool requiresPageTitle = true)
        {
            return table.EstimatePrintableRows(mc, titleRows, requiresPageTitle);
        }
        /// <summary>
        /// 改ページ判定
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="printableRows">ページに出力できる行数(ヘッダを含む)</param>
        /// <param name="tableRows">テーブルの行数(ヘッダを含む)</param>
        /// <param name="headerRows">ヘッダの行数</param>
        /// <param name="judgeCount">改ページの判定回数</param>
        /// <returns></returns>
        protected virtual bool RequiresNewpage(PdfDocument mc, int[] printableRows, int tableRows, int headerRows, ref int judgeCount)
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

            var requiresPageTitle = true;

            // 集計開始
            foreach (var table in GetTables(mc, data, indexPage))
            {
                if (table == null) continue;
                // 改ページの判定回数
                var judgeCount = 0;

                while (table.Rows - headerRows > 0)
                {
                    // 印刷可能な行数
                    var rows = EstimatePrintableTableRows(mc, table, titles.Length, requiresPageTitle);

                    // 必要に応じて改ページ
                    if (RequiresNewpage(mc, rows, table.Rows, headerRows, ref judgeCount))
                    {
                        mc.NewPage(ref indexPage);

                        requiresPageTitle = true;
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
                    printManager.printTableContents(mc, subtable, titles, requiresPageTitle, font);

                    // 行間が広いので、最後の改行を無効化
                    mc.addCurrentY(-printManager.FontHeight); // Table.LineSpacing3

                    // 抽出したデータ行を削除(ヘッダ行は残す)
                    table.RemoveRows(rows[0], headerRows);

                    requiresPageTitle = true;
                }

                requiresPageTitle = false;
            }

            // 行間が狭いので、無効化した改行の復元
            mc.addCurrentY(+printManager.FontHeight); // Table.LineSpacing3
        }
    }
}

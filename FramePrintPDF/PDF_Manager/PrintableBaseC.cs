using PDF_Manager.Printing;
using PDF_Manager.Printing.Comon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager
{
    /// <summary>
    /// 計算ケースと計算条件を持つ下記6種のキー向けの印刷処理を定義したクラス
    ///     DisgCombine,
    ///     DisgPickup,
    ///     FsecCombine,
    ///     FsecPickup,
    ///     ReacCombine,
    ///     ReacPickup
    /// </summary>
    abstract class PrintableBaseC : IPrintable
    {
        /// <summary>
        /// ResultDisgCombine.disgs、ResultFsecCombine.Fsecs、ResultReacCombine.reacsの1要素に対応するクラスのインタフェース
        /// </summary>
        protected interface IContext
        {
            /// <summary>
            /// テーブルのシーケンスを生成
            /// </summary>
            /// <returns></returns>
            IEnumerable<Table> GetTables();
            /// <summary>
            /// ResultDisgCombine.disgs、ResultFsecCombine.Fsecs、ResultReacCombine.reacsそれぞれの最後に該当するインスタンスか
            /// </summary>
            /// <returns></returns>
            bool IsLast();
        }

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
        /// ResultDisgCombine.disgs、ResultFsecCombine.Fsecs、ResultReacCombine.reacsそれぞれの1要素に対応するクラスインスタンスのシーケンスを生成
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<IContext> GetContexts();
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
            if (++judgeCount > 1)
            {
                // 2回目以降の判定。データ行が1行も入らない場合は改ページ
                return printableRows[0] < headerRows + 1;
            }
            else
            {
                // 1回目の判定
                if (printableRows[0] >= tableRows)
                {
                    // 全体が入るなら改ページ不要
                    return false;
                }
                else if (printableRows[0] < headerRows + 1)
                {
                    // データ行が1行も入らない場合は改ページ
                    return true;
                }
                else
                {
                    // 残り行数が少ない(1ページ分の10％未満)場合は改ページ
                    var pageHeight = mc.currentPage.Height - mc.Margine.Top - mc.Margine.Bottom;
                    var textHeight = mc.currentPage.Height - mc.currentPos.Y - mc.Margine.Bottom;
                    return textHeight < 0.1 * pageHeight;
                }
            }
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

            // 集計開始
            foreach (var context in GetContexts())
            {
                var requiresPageTitle = true;

                foreach (var table in context.GetTables())
                {
                    // 改ページの判定回数
                    var judgeCount = 0;

                    while (table.Rows - headerRows > 0)
                    {
                        // 印刷可能な行数
                        var rows = table.EstimatePrintableRows(mc, titles.Length, requiresPageTitle);

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

                // 計算ケースが変わる場合は改ページ
                if (context.GetTables().Any() && !context.IsLast())
                {
                    mc.NewPage(ref indexPage);
                }
            }
        }
    }
}

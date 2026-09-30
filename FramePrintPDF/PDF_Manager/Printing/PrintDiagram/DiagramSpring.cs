using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    /// <summary>
    /// 全ての部材のバネを描画する
    /// </summary>
    internal class DiagramSpring
    {
        /// <summary>
        /// バネ関連テキストの描画色
        /// </summary>
        private static readonly XBrush SpringTextColor = XBrushes.Black;
        /// <summary>
        /// バネの描画色
        /// </summary>
        private static readonly XColor SpringPenColor = XColors.Black;
        /// <summary>
        /// バネの描画線の太さ(単位はポイント)
        /// </summary>
        private const double SpringPenWidth = 0.1;

        /// <summary>
        /// 全ての部材のバネの描画情報を生成する
        /// </summary>
        /// <param name="inputFixMember">JSONファイルから読み込んだバネ情報のコレクション</param>
        /// <param name="typeNo">描画対象のケース番号</param>
        /// <param name="memberDic">部材情報の辞書</param>
        /// <param name="centerPos">描画領域の中心座標</param>
        /// <param name="topLeft">描画領域の左上角の座標</param>
        /// <param name="bottomRight">描画領域の右下角の座標</param>
        public DiagramSpring(InputFixMember inputFixMember, int typeNo, IReadOnlyDictionary<string, XMember> memberDic, XPoint centerPos, XPoint topLeft, XPoint bottomRight)
        {
            this.memberDic = memberDic;

            foreach (var member in memberDic.Values)
            {
                member.ClearSprings();
            }

            if (!inputFixMember.FixMembers.TryGetValue(typeNo, out var fixMembers))
            {
                return;
            }

            foreach (var kv in memberDic)
            {
                var m = kv.Key;
                var member = kv.Value;
                var entries = fixMembers.Where(fm => fm.m == m).OrderBy(fm => fm.row).ToList();
                if (entries.Count == 0 || member.Lenngth <= 0)
                    continue;

                // Old files can contain several additive, whole-member rows without a length.
                if (entries.All(fm => !fm.length.HasValue))
                {
                    foreach (var entry in entries.Where(HasStiffness))
                        member.AddSpring(new XSpring(entry, member, centerPos, topLeft, bottomRight,
                            0, member.Lenngth));
                    continue;
                }

                double cursor = 0;
                double tolerance = 1e-6 * Math.Max(1, member.Lenngth);
                for (int index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    if (!entry.length.HasValue && index != entries.Count - 1)
                        throw new FormatException($"fix_member {m}: only the final interval may omit length.");
                    double spanLength = entry.length ?? member.Lenngth - cursor;
                    if (spanLength <= 0 || cursor + spanLength > member.Lenngth + tolerance)
                        throw new FormatException($"fix_member {m}: interval is outside the member.");
                    spanLength = Math.Min(spanLength, member.Lenngth - cursor);
                    if (HasStiffness(entry))
                        member.AddSpring(new XSpring(entry, member, centerPos, topLeft, bottomRight,
                            cursor, spanLength));
                    cursor += spanLength;
                }
            }

            // XCanvasへの描画

            var springs = memberDic.Values.SelectMany(m => m.Springs);
            if (!springs.Any())
            {
                return;
            }

            canvas = new XCanvas();

            foreach (var spring in springs)
            {
                spring.Print(canvas);
            }
        }

        /// <summary>
        /// 全てのバネが描画される領域の左上角と右下角の座標を計算する
        /// </summary>
        /// <param name="topLeft">左上角の座標</param>
        /// <param name="bottomRight">右下角の座標</param>
        public void AdjustDiagramRect(ref XPoint topLeft, ref XPoint bottomRight)
        {
            var springs = memberDic.Values.SelectMany(m => m.Springs);
            if (springs.Any())
            {
                topLeft.X = Math.Min(topLeft.X, springs.Min(s => s.TopLeft.X));
                topLeft.Y = Math.Max(topLeft.Y, springs.Max(s => s.TopLeft.Y));
                bottomRight.X = Math.Max(bottomRight.X, springs.Max(s => s.BottomRight.X));
                bottomRight.Y = Math.Min(bottomRight.Y, springs.Min(s => s.BottomRight.Y));
            }
        }

        /// <summary>
        /// 全てのバネをPDF出力する
        /// </summary>
        /// <param name="frame"></param>
        public void Print(DiagramFrame frame) => canvas?.Print(frame, SpringPenColor, SpringPenWidth, SpringTextColor);

        private readonly IReadOnlyDictionary<string, XMember> memberDic;
        private readonly XCanvas canvas;

        private static bool HasStiffness(FixMember row) =>
            new[] { row.tx, row.ty, row.tz, row.tr }
                .Any(value => !double.IsNaN(value) && !double.IsInfinity(value) && value != 0);
    }
}

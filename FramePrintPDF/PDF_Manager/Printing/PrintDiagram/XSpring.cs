using PdfSharpCore.Drawing;
using System;
using System.Diagnostics;
using System.Linq;

namespace PDF_Manager.Printing
{
    /// <summary>
    /// 部材ごとのバネの描画情報を保持する
    /// </summary>
    internal class XSpring : XDrawable
    {
        // 水平部材用

        /// <summary>
        /// 部材とバネの間隔(単位はポイント)
        /// </summary>
        private const double HorizontalSpringGap = 6;
        /// <summary>
        /// バネを並べて表示する際のバネ間の最小間隔(単位はポイント)
        /// </summary>
        private const double HorizontalSpringPitch = 16;
        /// <summary>
        /// 縦向きのバネを構成する節点のx座標とy座標を交互に並べたもの
        /// </summary>
        private readonly double[] HorizontalSpringParams1 = new double[] { -3, 13, 3, 13, -3, 16, 3, 16, -3, 19, 3, 19, }; // 縦向きのバネ(を構成する節点のx座標とy座標を交互に並べたもの)
        /// <summary>
        /// 横向きのバネを構成する節点のx座標とy座標を交互に並べたもの
        /// </summary>
        private readonly double[] HorizontalSpringParams2 = new double[] { 5, 12, 5, 16, 8, 12, 8, 16, 11, 12, 11, 16, }; // 横向きのバネ(同上)

        // 鉛直部材用(左側配置)

        /// <summary>
        /// 部材とバネの間隔(単位はポイント)
        /// </summary>
        private const double VerticalLeftSpringGap = 6;
        /// <summary>
        /// バネを並べて表示する際のバネ間の最小間隔(単位はポイント)
        /// </summary>
        private const double VerticalLeftSpringPitch = 16;
        /// <summary>
        /// 縦向きのバネを構成する節点のx座標とy座標を交互に並べたもの
        /// </summary>
        private readonly double[] VerticalLeftSpringParams1 = new double[] { 13, -3, 13, 3, 16, -3, 16, 3, 19, -3, 19, 3, }; // 横向きのバネ(同上)
        /// <summary>
        /// 横向きのバネを構成する節点のx座標とy座標を交互に並べたもの
        /// </summary>
        private readonly double[] VerticalLeftSpringParams2 = new double[] { 16, 5, 20, 5, 16, 8, 20, 8, 16, 11, 20, 11, }; // 縦向きのバネ(同上)

        // 鉛直部材用(右側配置)

        /// <summary>
        /// 部材とバネの間隔(単位はポイント)
        /// </summary>
        private const double VerticalRightSpringGap = 6;
        /// <summary>
        /// バネを並べて表示する際のバネ間の最小間隔(単位はポイント)
        /// </summary>
        private const double VerticalRightSpringPitch = 16;
        /// <summary>
        /// 縦向きのバネを構成する節点のx座標とy座標を交互に並べたもの
        /// </summary>
        private readonly double[] VerticalRightSpringParams1 = new double[] { 13, -3, 13, 3, 16, -3, 16, 3, 19, -3, 19, 3, }; // 横向きのバネ(同上)
        /// <summary>
        /// 横向きのバネを構成する節点のx座標とy座標を交互に並べたもの
        /// </summary>
        private readonly double[] VerticalRightSpringParams2 = new double[] { 12, 5, 16, 5, 12, 8, 16, 8, 12, 11, 16, 11, }; // 縦向きのバネ(同上)

        /// <summary>
        /// 部材に対する描画位置(上下左右)
        /// </summary>
        public XLocationType LocationType { get; }
        /// <summary>
        /// 部材とバネ底辺の間隔(単位はポイント)
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// バネの描画領域左上角の座標
        /// </summary>
        public XPoint TopLeft { get; }
        /// <summary>
        /// バネの描画領域右下角の座標
        /// </summary>
        public XPoint BottomRight { get; }

        /// <summary>
        /// バネの描画情報を生成する
        /// </summary>
        /// <param name="fixMember">JSONファイルから読み込んだバネ情報</param>
        /// <param name="member">部材情報</param>
        /// <param name="centerPos">描画領域の中心座標</param>
        /// <param name="topLeft">描画領域の左上角の座標</param>
        /// <param name="bottomRight">描画領域の右下角の座標</param>
        public XSpring(FixMember fixMember, XMember member, XPoint centerPos, XPoint topLeft, XPoint bottomRight,
            double startDistance, double spanLength)
            : base(null, null)
        {
            Debug.Assert(fixMember.m == member.No);
            double tolerance = 1e-6 * Math.Max(1, member.Lenngth);
            if (member.Lenngth <= 0 || startDistance < 0 || spanLength <= 0 ||
                startDistance + spanLength > member.Lenngth + tolerance)
                throw new ArgumentOutOfRangeException(nameof(spanLength));

            // 実物の長辺を200mm四方に縮小した状態における1pt相当の長さ(単位は実物と同じ)
            var coef = Math.Max(bottomRight.X - topLeft.X, topLeft.Y - bottomRight.Y) / (200 * XUnit.FromMillimeter(1));

            var ni = member.Ni; // 節点座標
            var nj = member.Nj;
            double directionX = (nj.Pos.X - ni.Pos.X) / member.Lenngth;
            double directionY = (nj.Pos.Y - ni.Pos.Y) / member.Lenngth;

            if (member.MemberType == XMemberType.Horizontal)
            {
                // 水平部材

                var scale = Math.Min(coef, spanLength / HorizontalSpringPitch);
                var pitch = scale * HorizontalSpringPitch;
                var len = spanLength;
                var nrepeat = Math.Max((int)(len / pitch), 1);
                var xmin = Math.Min(HorizontalSpringParams1.Chunk(2).Min(s => s.ElementAt(0)), HorizontalSpringParams2.Chunk(2).Min(s => s.ElementAt(0)));
                var ymin = Math.Min(HorizontalSpringParams1.Chunk(2).Min(s => s.ElementAt(1)), HorizontalSpringParams2.Chunk(2).Min(s => s.ElementAt(1)));
                var xoffset = (len - pitch * nrepeat) / 2 - scale * xmin;
                var yoffset = -scale * (ymin - HorizontalSpringGap);

                for (var i = 0; i < nrepeat; ++i)
                {
                    var nps1 = HorizontalSpringParams1.Chunk(2).Select(s => new XPoint(
                        ni.Pos.X + directionX * (startDistance + xoffset + pitch * i + scale * s.ElementAt(0)),
                        ni.Pos.Y - yoffset - scale * s.ElementAt(1)));
                    AddLines(nps1.Take(nps1.Count() - 1).Zip(nps1.Skip(1)).Select(pp => (pp.First, pp.Second)));

                    var nps2 = HorizontalSpringParams2.Chunk(2).Select(s => new XPoint(
                        ni.Pos.X + directionX * (startDistance + xoffset + pitch * i + scale * s.ElementAt(0)),
                        ni.Pos.Y - yoffset - scale * s.ElementAt(1)));
                    AddLines(nps2.Take(nps2.Count() - 1).Zip(nps2.Skip(1)).Select(pp => (pp.First, pp.Second)));
                }

                LocationType = XLocationType.GD; // 部材の下側
                Height = -HorizontalSpringGap + HorizontalSpringParams1.Concat(HorizontalSpringParams2).Chunk(2).Max(s => s.ElementAt(1));
            }
            else if (member.MemberType == XMemberType.Vertical)
            {
                // 鉛直部材

                if (ni.Pos.X < centerPos.X)
                {
                    // 部材の左側にバネを描画

                    var scale = Math.Min(coef, spanLength / VerticalLeftSpringPitch);
                    var pitch = scale * VerticalLeftSpringPitch;
                    var len = spanLength;
                    var nrepeat = Math.Max((int)(len / pitch), 1);
                    var xmax = Math.Max(VerticalLeftSpringParams1.Chunk(2).Max(s => s.ElementAt(0)), VerticalLeftSpringParams2.Chunk(2).Max(s => s.ElementAt(0)));
                    var ymin = Math.Min(VerticalLeftSpringParams1.Chunk(2).Min(s => s.ElementAt(1)), VerticalLeftSpringParams2.Chunk(2).Min(s => s.ElementAt(1)));
                    var xoffset = -scale * (xmax - -VerticalLeftSpringGap);
                    var yoffset = (len - pitch * nrepeat) / 2 - scale * ymin;

                    for (var i = 0; i < nrepeat; ++i)
                    {
                        var nps1 = VerticalLeftSpringParams1.Chunk(2).Select(s => new XPoint(
                            ni.Pos.X + xoffset + scale * s.ElementAt(0),
                            ni.Pos.Y + directionY * (startDistance + yoffset + pitch * i + scale * s.ElementAt(1))));
                        AddLines(nps1.Take(nps1.Count() - 1).Zip(nps1.Skip(1)).Select(pp => (pp.First, pp.Second)));

                        var nps2 = VerticalLeftSpringParams2.Chunk(2).Select(s => new XPoint(
                            ni.Pos.X + xoffset + scale * s.ElementAt(0),
                            ni.Pos.Y + directionY * (startDistance + yoffset + pitch * i + scale * s.ElementAt(1))));
                        AddLines(nps2.Take(nps2.Count() - 1).Zip(nps2.Skip(1)).Select(pp => (pp.First, pp.Second)));
                    }

                    LocationType = XLocationType.GL;
                    Height = -VerticalLeftSpringGap + VerticalLeftSpringParams1.Concat(VerticalLeftSpringParams2).Chunk(2).Max(s => s.ElementAt(0));
                }
                else
                {
                    // 部材の右側にバネを描画

                    var scale = Math.Min(coef, spanLength / VerticalRightSpringPitch);
                    var pitch = scale * VerticalRightSpringPitch;
                    var len = spanLength;
                    var nrepeat = Math.Max((int)(len / pitch), 1);
                    var xmin = Math.Min(VerticalRightSpringParams1.Chunk(2).Min(s => s.ElementAt(0)), VerticalRightSpringParams2.Chunk(2).Min(s => s.ElementAt(0)));
                    var ymin = Math.Min(VerticalRightSpringParams1.Chunk(2).Min(s => s.ElementAt(1)), VerticalRightSpringParams2.Chunk(2).Min(s => s.ElementAt(1)));
                    var xoffset = -scale * (xmin - VerticalRightSpringGap);
                    var yoffset = (len - pitch * nrepeat) / 2 - scale * ymin;

                    for (var i = 0; i < nrepeat; ++i)
                    {
                        var nps1 = VerticalRightSpringParams1.Chunk(2).Select(s => new XPoint(
                            ni.Pos.X + xoffset + scale * s.ElementAt(0),
                            ni.Pos.Y + directionY * (startDistance + yoffset + pitch * i + scale * s.ElementAt(1))));
                        AddLines(nps1.Take(nps1.Count() - 1).Zip(nps1.Skip(1)).Select(pp => (pp.First, pp.Second)));

                        var nps2 = VerticalRightSpringParams2.Chunk(2).Select(s => new XPoint(
                            ni.Pos.X + xoffset + scale * s.ElementAt(0),
                            ni.Pos.Y + directionY * (startDistance + yoffset + pitch * i + scale * s.ElementAt(1))));
                        AddLines(nps2.Take(nps2.Count() - 1).Zip(nps2.Skip(1)).Select(pp => (pp.First, pp.Second)));
                    }

                    LocationType = XLocationType.GR;
                    Height = -VerticalRightSpringGap + VerticalRightSpringParams1.Concat(VerticalRightSpringParams2).Chunk(2).Max(s => s.ElementAt(0));
                }
            }
            else
            {
                // 斜め部材では水平部材用の形状を局所軸に沿って配置する。
                var scale = Math.Min(coef, spanLength / HorizontalSpringPitch);
                var pitch = scale * HorizontalSpringPitch;
                var nrepeat = Math.Max((int)(spanLength / pitch), 1);
                var xmin = Math.Min(HorizontalSpringParams1.Chunk(2).Min(s => s.ElementAt(0)), HorizontalSpringParams2.Chunk(2).Min(s => s.ElementAt(0)));
                var ymin = Math.Min(HorizontalSpringParams1.Chunk(2).Min(s => s.ElementAt(1)), HorizontalSpringParams2.Chunk(2).Min(s => s.ElementAt(1)));
                var axialOffset = (spanLength - pitch * nrepeat) / 2 - scale * xmin;
                var radialOffset = -scale * (ymin - HorizontalSpringGap);
                XPoint At(double axial, double radial) => new XPoint(
                    ni.Pos.X + directionX * axial + directionY * radial,
                    ni.Pos.Y + directionY * axial - directionX * radial);

                for (int i = 0; i < nrepeat; i++)
                {
                    var nps1 = HorizontalSpringParams1.Chunk(2).Select(s => At(
                        startDistance + axialOffset + pitch * i + scale * s.ElementAt(0),
                        radialOffset + scale * s.ElementAt(1)));
                    AddLines(nps1.Take(nps1.Count() - 1).Zip(nps1.Skip(1)).Select(pp => (pp.First, pp.Second)));

                    var nps2 = HorizontalSpringParams2.Chunk(2).Select(s => At(
                        startDistance + axialOffset + pitch * i + scale * s.ElementAt(0),
                        radialOffset + scale * s.ElementAt(1)));
                    AddLines(nps2.Take(nps2.Count() - 1).Zip(nps2.Skip(1)).Select(pp => (pp.First, pp.Second)));
                }

                LocationType = XLocationType.GD;
                Height = -HorizontalSpringGap + HorizontalSpringParams1.Concat(HorizontalSpringParams2).Chunk(2).Max(s => s.ElementAt(1));
            }

            CalculateDiagramRect(out var _topLeft, out var _bottomRight);
            TopLeft = _topLeft;
            BottomRight = _bottomRight;
        }

        /// <summary>
        /// バネを <paramref name="canvas"/> に描画する
        /// </summary>
        /// <param name="canvas"></param>
        public new void Print(ICanvas canvas) => base.PrintDrawables(canvas);
    }
}

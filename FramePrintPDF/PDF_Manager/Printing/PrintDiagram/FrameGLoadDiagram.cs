using Newtonsoft.Json.Linq;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PDF_Manager.Printing
{
    /// <summary>
    /// FrameG の体裁の荷重図（2D、見本 原宿ボックス 0404 の見本_荷重図.pdf）。
    /// 寸法は見本の pt。図の置き場（枠の中心と大きさ・見出し）は用紙の幅と高さに対する比で持ち、
    /// 横向きや A3 の用紙にも同じ規則で置く。荷重・寸法・文字の大きさと間隔は用紙によらず pt のまま。
    /// </summary>
    internal sealed class FrameGLoadDiagram
    {
        // 文字（MS ゴシック。値と寸法 8.16 pt、単位 5.2 pt、見出し 9.66 pt、頁の題 11.2 pt）
        private const double ValueSize = 8.16;
        private const double UnitSize = 5.2;
        private const double HeadingSize = 9.66;
        private const double PageTitleSize = 11.2;
        private const double TextStretch = 1.0104;         // 見本の文字の送り（見出しの半角 4.88 pt = 9.66 pt の 1.0104 倍）
        private const double HalfWidth = ValueSize / 2 * TextStretch;   // 半角 1 文字の送り
        private const double UnitRise = 1.4;               // 単位の文字は値より 1.4 pt 上（回転した文字では外側）
        private const double LabelCentring = 17.48;        // 値と単位の組を部材の中央に置くときの単位の分

        // 線の太さ
        private const double MemberPen = 0.84, RigidMemberPen = 1.44, RigidZonePen = 1.68;
        private const double NodePen = 0.6, NodeDiameter = 1.8;
        private const double BandPen = 0.48, ArrowPen = 0.36, NodalShaftPen = 0.72, DimensionPen = 0.36;

        // 分布荷重の帯（FrameG の F2部材荷重高：K / ln(B·10/ln10/P) に (P/B)^0.2·1.2、鉛直の部材の台形は (P/B)^0.8·1.2·1.3·1.3）
        private const double BandUnit = 16.62;             // 部材荷重長さmax·Twip·FMN（pt）
        private const double BandCap = 20.0;               // 400 twip
        private const double HorizontalBandGap = 4.2;      // 水平の部材から帯の内側まで
        private const double VerticalBandGap = 6.84;       // 鉛直の部材から帯の内側まで
        private const double HorizontalPitch = 8.4, VerticalPitch = 7.8;
        private const double HeadLength = 3.3, HeadHalf = 0.9;
        private const double LabelLift = 1.16;             // 帯の外側の線から値の基線まで
        private const double ExtraLabelLine = 8.8;         // Ws・Wm の 2 行目
        private const double LowerLabelDrop = 7.2;         // 下に置く値の基線
        private const double SideLabelLeft = 3.0, SideLabelRight = 8.66;   // 鉛直の帯の外側から回転した値の基線まで（字面は基線の左）

        // 寸法線
        private const double DimensionGap = 6.0;           // 水平：帯の外側から寸法線まで 帯の高さ + 6
        private const double ExtraDimensionLine = 10.7;    // 水平：Ws・Wm の 2 行目があるとき
        private const double NextLayerGap = 15.6;          // 寸法線から次の帯の内側まで
        private const double DimensionTick = 8.52;
        private const double DimensionHead = 3.24, DimensionHeadHalf = 0.84;
        private const double DimensionTextLift = 1.16;
        private const double SideTickLeft = 22.7, SideTickRight = 28.8;
        private const double SideDimensionLeft = 15.6, SideDimensionRight = 18.12;   // 回転した値のとき帯の外側から
        private const double EndLabelInner = 5.3, EndLabelOuter = 3.4;               // 台形の端の値（横書き）と帯の外側の間
        private const double EndLabelDimensionLeft = 0.5, EndLabelDimensionRight = 12.5;
        private const double EndLabelAbove = 1.5, EndLabelBelow = 6.5;
        private const double FreeEndBelow = 2.62, FreeEndShiftLeft = 9.66, FreeEndShiftRight = 4.32;

        // 節点荷重
        private const double NodalGap = 7.08, NodalLength = 23.4, NodalNextGap = 16.2;
        private const double NodalHead = 7.08, NodalHeadHalf = 1.26;
        private const double NodalLabelLeft = 5.84, NodalLabelRight = 11.88;

        // 部材軸方向の分布荷重（破線の矢印と q =）
        private const double AxialOffset = 4.92, AxialLength = 47.04, AxialDash = 3.72, AxialSpace = 1.92;
        private const double AxialHead = 3.36, AxialHeadHalf = 0.84;
        private const double AxialLabelInside = 8.74, AxialLabelOutside = 3.0;
        private const double AxialRangeTick = 2.0;     // 載る範囲の両端の線（破線から両側へ）

        // 温度荷重
        private const double TemperatureLift = 11.1;

        // 支点の回転ばね（弧の半径）
        private const double SupportRotationRadius = 4.5;

        // ばね（Z の形と M の形の組）
        private const double SpringHorizontalStart = 5.64, SpringHorizontalEnd = 9.24, SpringHorizontalPitch = 9.8;
        private const double SpringVerticalPitch = 9.6, SpringStroke = 3.6;

        // 図の置き場（A4 縦の見本の pt を用紙の幅・高さで割った比）
        private const double HeadingX = 78.0 / 595, HeadingY = 83.8 / 842, LowerHeadingY = 443.3 / 842;
        private const double FirstHeadingDrop = 8.4;       // 頁の題「荷 重 図」がある頁の 1 つ目の見出し
        private const double PageTitleX = 69.6 / 595, PageTitleY = 79.6 / 842;
        private static readonly Slot SingleSlot = new Slot(312.48 / 595, 456.12 / 842, 335.28 / 595, 0.32);
        private static readonly Slot[] SplitSlots =
        {
            new Slot(298.98 / 595, 298.86 / 842, 223.56 / 595, 134.04 / 842),
            new Slot(298.98 / 595, 672.06 / 842, 223.56 / 595, 134.04 / 842),
        };
        private const double MaxStretch = 1.15;            // 2 段書きで横を縦より大きくしてよい倍率（見本 1.13）

        private sealed class Slot
        {
            public readonly double X, Y, Width, Height;
            public Slot(double x, double y, double width, double height) { X = x; Y = y; Width = width; Height = height; }
        }

        private readonly PdfDocument mc;
        private readonly string language;
        private readonly XFont valueFont = Font(ValueSize);
        private readonly XFont unitFont = Font(UnitSize);
        private readonly Dictionary<string, XPoint> nodes = new Dictionary<string, XPoint>();   // y 下向き（m）
        private readonly Dictionary<string, Member> members = new Dictionary<string, Member>();
        private readonly InputFixMember fixMember;
        private readonly InputFixNode fixNode;
        private readonly int yDown;   // データの y の正が頁の下なら 1（Ver2 以降の 2D は y 下向き）、上なら -1
        private XPoint modelCentre;
        private double scaleX, scaleY;
        private XPoint pageCentre;

        private sealed class Member
        {
            public string Id, I, J;
            public bool Rigid;            // 部材名が「剛域」（FrameG の剛域の部材）
            public double RigidI, RigidJ; // 剛域データの長さ（m）
        }

        internal FrameGLoadDiagram(PdfDocument mc, PrintData data)
        {
            this.mc = mc;
            language = data.language;
            yDown = data.isOlderVer2 ? -1 : 1;   // DiagramFrame と同じ：Ver2 より新しいデータは y 下向き
            foreach (var pair in ((InputNode)data.printDatas[InputNode.KEY]).Nodes)
                nodes.Add(pair.Key, new XPoint(Finite(pair.Value.x), yDown * Finite(pair.Value.y)));
            var element = (InputElement)data.printDatas[InputElement.KEY];
            foreach (var pair in ((InputMember)data.printDatas[InputMember.KEY]).members)
            {
                if (!nodes.ContainsKey(pair.Value.ni) || !nodes.ContainsKey(pair.Value.nj)) continue;
                members.Add(pair.Key, new Member
                {
                    Id = pair.Key, I = pair.Value.ni, J = pair.Value.nj,
                    Rigid = element.GetElementName(pair.Value.e) == "剛域",
                });
            }
            if (data.printDatas.TryGetValue(InputRigid.KEY, out var rigid))
                foreach (var row in ((InputRigid)rigid).rigids)
                    if (row.m != null && members.TryGetValue(row.m, out var member))
                    {
                        member.RigidI = Finite(row.Ilength);
                        member.RigidJ = Finite(row.Jlength);
                    }
            fixMember = (InputFixMember)data.printDatas[InputFixMember.KEY];
            fixNode = (InputFixNode)data.printDatas[InputFixNode.KEY];
        }

        private static double Finite(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;

        /// <summary>
        /// 荷重図の全ケース。<paramref name="split"/> が真なら 1 頁に上下 2 ケース（FrameG の 2 段書き）、
        /// <paramref name="singleCases"/>（ケースの添字）は 1 頁に 1 ケース（FrameG の書式の「n.k」の k ケース）。
        /// </summary>
        internal void Print(JObject loads, InputLoadName names, bool split, ICollection<int> singleCases, ref int indexPage)
        {
            if (nodes.Count == 0 || members.Count == 0) return;
            // FrameG の図の頁に頁頭の題名（TITLE :）は無い。
            mc.BeginOwnHeaderPages(ref indexPage);
            try
            {
                bool pageUsed = false, upperUsed = false, titled = false;
                int index = 0;
                foreach (var pair in loads)
                {
                    mc.CheckBudget();
                    bool single = !split || singleCases.Contains(index);
                    int slot;
                    if (single || !upperUsed)
                    {
                        if (pageUsed) mc.NewPage(ref indexPage);
                        slot = 0;
                        upperUsed = !single;
                    }
                    else
                    {
                        slot = 1;
                        upperUsed = false;
                    }
                    pageUsed = true;
                    bool pageTitle = !titled;
                    titled = true;
                    int.TryParse(pair.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int caseNo);
                    names.loadnames.TryGetValue(caseNo, out var name);
                    DrawCase(pair.Key, name, pair.Value as JObject ?? new JObject(), single ? SingleSlot : SplitSlots[slot],
                        slot == 1, pageTitle);
                    index++;
                }
            }
            finally { mc.EndOwnHeaderPages(); }
        }

        private double PageWidth => mc.currentPage.Width.Point;
        private double PageHeight => mc.currentPage.Height.Point;

        private void DrawCase(string key, LoadName name, JObject load, Slot slot, bool lower, bool pageTitle)
        {
            Fit(slot);

            // 見出し：FrameG は「 Ｃase」& Str(n)（正の数は前に空白）& 名称 &「　」& 記号
            double headingY = (lower ? LowerHeadingY : HeadingY) * PageHeight;
            if (pageTitle)
            {
                string title = language == "en" ? " Load Diagram" : language == "cn" ? " 载 重 图" : " 荷 重 図";
                Text(title, Font(PageTitleSize), PageTitleX * PageWidth, PageTitleY * PageHeight);
                headingY += FirstHeadingDrop;
            }
            // en・cn は同じ配置で、全角の「Ｃ」と「　」だけ置き換える
            string heading = language == "en" ? $" Case {key}  {name?.name}  {name?.symbol}"
                : language == "cn" ? $" Case {key}  {name?.name}　{name?.symbol}"
                : $" Ｃase {key}  {name?.name}　{name?.symbol}";
            Text(heading, Font(HeadingSize), HeadingX * PageWidth, headingY);

            DrawFrame();
            DrawSupports(load["fix_node"]?.Value<int?>() ?? name?.fix_node ?? 0);
            DrawSprings(load["fix_member"]?.Value<int?>() ?? name?.fix_member ?? 0);
            DrawLoads(load);
        }

        // ---- 縮尺と座標 ----

        private void Fit(Slot slot)
        {
            double minX = nodes.Values.Min(p => p.X), maxX = nodes.Values.Max(p => p.X);
            double minY = nodes.Values.Min(p => p.Y), maxY = nodes.Values.Max(p => p.Y);
            double width = maxX - minX, height = maxY - minY;
            double boxWidth = slot.Width * PageWidth, boxHeight = slot.Height * PageHeight;
            double sx = width > 0 ? boxWidth / width : double.PositiveInfinity;
            double sy = height > 0 ? boxHeight / height : double.PositiveInfinity;
            if (double.IsInfinity(sx) && double.IsInfinity(sy)) sx = sy = 1;
            else if (double.IsInfinity(sx)) sx = sy;
            else if (double.IsInfinity(sy)) sy = sx;
            // FrameG は横と縦の縮尺を別に決める。縦は横より大きくしない。横は縦の MaxStretch 倍まで。
            if (sy > sx) sy = sx;
            else sx = Math.Min(sx, sy * MaxStretch);
            scaleX = sx;
            scaleY = sy;
            modelCentre = new XPoint((minX + maxX) / 2, (minY + maxY) / 2);
            pageCentre = new XPoint(slot.X * PageWidth, slot.Y * PageHeight);
        }

        private XPoint Page(XPoint model) =>
            new XPoint(pageCentre.X + (model.X - modelCentre.X) * scaleX, pageCentre.Y + (model.Y - modelCentre.Y) * scaleY);

        private XPoint NodePoint(string id) => Page(nodes[id]);

        private XPoint FrameCentre
        {
            get
            {
                double minX = nodes.Values.Min(p => p.X), maxX = nodes.Values.Max(p => p.X);
                double minY = nodes.Values.Min(p => p.Y), maxY = nodes.Values.Max(p => p.Y);
                return Page(new XPoint((minX + maxX) / 2, (minY + maxY) / 2));
            }
        }

        // ---- 骨組 ----

        private void DrawFrame()
        {
            var gfx = mc.gfx;
            foreach (var m in members.Values)
                gfx.DrawLine(new XPen(XColors.Black, m.Rigid ? RigidMemberPen : MemberPen), NodePoint(m.I), NodePoint(m.J));
            var rigidPen = new XPen(XColors.Black, RigidZonePen);
            foreach (var m in members.Values)
            {
                XPoint i = NodePoint(m.I), j = NodePoint(m.J);
                double length = Distance(nodes[m.I], nodes[m.J]);
                if (length <= 0) continue;
                if (m.RigidI > 0) gfx.DrawLine(rigidPen, i, Lerp(i, j, Math.Min(1, m.RigidI / length)));
                if (m.RigidJ > 0) gfx.DrawLine(rigidPen, j, Lerp(j, i, Math.Min(1, m.RigidJ / length)));
            }
            var nodePen = new XPen(XColors.Black, NodePen);
            foreach (var id in nodes.Keys)
            {
                var p = NodePoint(id);
                gfx.DrawEllipse(nodePen, p.X - NodeDiameter / 2, p.Y - NodeDiameter / 2, NodeDiameter, NodeDiameter);
            }
        }

        /// <summary>
        /// 支点（FrameWeb の値：1 は固定、0 は自由、それ以外はばね定数。support.py と同じ）。並進がすべて固定なら固定・ピン、
        /// 一方だけ固定ならローラー。ばねの向きには三角形とばねの記号（旧来の荷重図の XSupport と同じ形）、回転ばねは弧。
        /// </summary>
        private void DrawSupports(int type)
        {
            if (!fixNode.FixNodes.TryGetValue(type, out var rows)) return;
            var pen = new XPen(XColors.Black, ArrowPen);
            foreach (var row in rows)
            {
                if (row.n == null || !nodes.ContainsKey(row.n)) continue;
                bool Fixed(double v) => v == 1;
                bool Spring(double v) => v != 0 && v != 1 && double.IsFinite(v);
                var p = NodePoint(row.n);
                if (Fixed(row.tx) && Fixed(row.ty) && Fixed(row.rz))
                {
                    // 固定：節点の下に短い線とハッチ
                    mc.gfx.DrawLine(new XPen(XColors.Black, MemberPen), p.X - 5, p.Y + 1.5, p.X + 5, p.Y + 1.5);
                    for (double x = -4; x <= 5; x += 2.25)
                        mc.gfx.DrawLine(pen, p.X + x, p.Y + 1.5, p.X + x - 2, p.Y + 4);
                    continue;
                }
                bool roller = Fixed(row.tx) && row.ty == 0 || Fixed(row.ty) && row.tx == 0;
                if (Fixed(row.tx) && Fixed(row.ty) || roller || Fixed(row.ty) && Spring(row.tx))
                {
                    // ピン・ローラー：三角形（ローラーは下に線を足す）。鉛直が固定で水平がばねなら三角形と横のばね
                    mc.gfx.DrawPolygon(pen, new[] { p, new XPoint(p.X - 3.5, p.Y + 6), new XPoint(p.X + 3.5, p.Y + 6) });
                    if (roller) mc.gfx.DrawLine(pen, p.X - 4.5, p.Y + 7.5, p.X + 4.5, p.Y + 7.5);
                }
                // ばね：鉛直のばねは節点の下、水平のばねは骨組の外側
                if (Spring(row.ty)) SupportSpring(pen, p, new XPoint(0, 1));
                if (Spring(row.tx)) SupportSpring(pen, p, new XPoint(p.X < FrameCentre.X ? -1 : 1, 0));
                if (Spring(row.rz))
                    mc.gfx.DrawArc(pen, p.X - SupportRotationRadius, p.Y - SupportRotationRadius,
                        2 * SupportRotationRadius, 2 * SupportRotationRadius, 200, 140);
            }
        }

        /// <summary>ばねの支点：節点から <paramref name="d"/> の向きに三角形とばねの折れ線（旧来の XSupport.SpringParams の形）。</summary>
        private void SupportSpring(XPen pen, XPoint p, XPoint d)
        {
            var n = new XPoint(-d.Y, d.X);
            XPoint At(double across, double along) => new XPoint(p.X + n.X * across + d.X * along, p.Y + n.Y * across + d.Y * along);
            mc.gfx.DrawPolygon(pen, new[] { At(0, 0.35), At(-3.5, 5.35), At(3.5, 5.35) });
            mc.gfx.DrawLines(pen, new[] { At(-3.5, 6.85), At(3.5, 6.85), At(-3.5, 8.35), At(3.5, 8.35), At(-3.5, 9.85), At(3.5, 9.85) });
        }

        // ---- ばね ----

        /// <summary>
        /// 部材の地盤ばね。同じ部材の行は I 端から順に区間を積む（length が空の最後の行は残りの全長、すべて空なら各行が全長。
        /// ソルバーの legacy_beam.py の _spring_intervals と同じ）。ばね定数が 0 の区間は描かない。
        /// </summary>
        private void DrawSprings(int type)
        {
            if (!fixMember.FixMembers.TryGetValue(type, out var rows)) return;
            var pen = new XPen(XColors.Black, ArrowPen);
            foreach (var memberRows in rows.Where(r => r.m != null && members.ContainsKey(r.m)).GroupBy(r => r.m))
            {
                var m = members[memberRows.Key];
                double length = Distance(nodes[m.I], nodes[m.J]);
                if (length <= 0) continue;
                var list = memberRows.ToList();
                bool whole = list.All(r => !(r.length is double value && value > 0));
                double cursor = 0;
                for (int k = 0; k < list.Count; k++)
                {
                    var row = list[k];
                    double from = whole ? 0 : cursor;
                    double to = whole ? length : row.length is double span && span > 0 ? Math.Min(length, cursor + span) : length;
                    cursor = to;
                    if (row.tx == 0 && row.ty == 0 || to <= from) continue;
                    DrawSpringRun(pen, m, from / length, to / length);
                }
            }
        }

        /// <summary>部材 <paramref name="m"/> の I 端からの比 a〜b にばねの記号を並べる。</summary>
        private void DrawSpringRun(XPen pen, Member m, double a, double b)
        {
            XPoint i = NodePoint(m.I), j = NodePoint(m.J);
            XPoint pa = Lerp(i, j, a), pb = Lerp(i, j, b);
            double length = Distance(pa, pb);
            if (length <= 0) return;
            var u = Unit(pa, pb);
            var s = Outward(i, j);
            bool horizontal = Math.Abs(u.X) >= Math.Abs(u.Y);
            // 並べる始まりは左（水平）・上（鉛直）
            XPoint start = pa;
            if (horizontal ? u.X < 0 : u.Y < 0) { start = pb; u = new XPoint(-u.X, -u.Y); }
            if (horizontal)
            {
                // 水平の部材：始まりから 5.64、終わりの 9.24 手前までに Z と M の組（間隔は 9.8 に近い等分）
                double span = length - SpringHorizontalStart - SpringHorizontalEnd;
                int count = Math.Max(1, (int)Math.Round(span / SpringHorizontalPitch) + 1);
                double pitch = count > 1 ? span / (count - 1) : 0;
                for (int k = 0; k < count; k++)
                {
                    double t = SpringHorizontalStart + k * pitch;
                    Polyline(pen, start, u, s, HorizontalZ, t);
                    Polyline(pen, start, u, s, HorizontalM, t);
                }
            }
            else
            {
                // 鉛直の部材：9.6 以下の等分
                int count = Math.Max(1, (int)Math.Ceiling(length / SpringVerticalPitch - 1e-9));
                double pitch = length / count;
                for (int k = 0; k < count; k++)
                {
                    double t = (pitch - SpringStroke) / 2 + k * pitch;
                    Polyline(pen, start, u, s, VerticalZ, t);
                    Polyline(pen, start, u, s, VerticalM, t);
                }
            }
        }

        // ばねの記号（部材に沿う位置 t、部材から外への距離 d）。Z は部材に沿う 3 本、M は部材に直角な 3 本を折れ線で。
        private static readonly double[,] HorizontalZ = { { 0, 4.8 }, { 3.6, 4.8 }, { 0, 6.6 }, { 3.6, 6.6 }, { 0, 8.4 }, { 3.6, 8.4 } };
        private static readonly double[,] HorizontalM = { { 4.68, 4.2 }, { 4.68, 6.6 }, { 6.48, 4.2 }, { 6.48, 6.6 }, { 8.28, 4.2 }, { 8.28, 6.6 } };
        private static readonly double[,] VerticalZ = { { 0, 7.2 }, { 3.6, 7.2 }, { 0, 5.4 }, { 3.6, 5.4 }, { 0, 3.6 }, { 3.6, 3.6 } };
        private static readonly double[,] VerticalM = { { 4.8, 5.4 }, { 4.8, 3.0 }, { 6.6, 5.4 }, { 6.6, 3.0 }, { 8.4, 5.4 }, { 8.4, 3.0 } };

        private void Polyline(XPen pen, XPoint start, XPoint u, XPoint s, double[,] shape, double t)
        {
            var points = new XPoint[shape.GetLength(0)];
            for (int k = 0; k < points.Length; k++) points[k] = At(start, u, s, t + shape[k, 0], shape[k, 1]);
            mc.gfx.DrawLines(pen, points);
        }

        private static XPoint At(XPoint start, XPoint u, XPoint s, double t, double d) =>
            new XPoint(start.X + u.X * t + s.X * d, start.Y + u.Y * t + s.Y * d);

        // ---- 荷重 ----

        private sealed class MemberLoad
        {
            public int Row;
            public List<Member> Chain;
            public string Direction, Mark, MS;
            public double P1, P2;
            // P1・P2 の位置（m、範囲の始まりから）。分布荷重は Start〜End に載り、範囲の外は描くときに切る。
            public double Start, End;
        }

        /// <summary>同じ直線・同じ側に並ぶ荷重の段（部材に近い方から）。</summary>
        private sealed class Group
        {
            public XPoint Start, U, S;       // 直線の始まり（頁）・向き・荷重を描く側
            public double Length;             // 直線の長さ（頁）
            public bool Vertical;
            public bool Nodal;
            public List<Layer> Layers = new List<Layer>();
        }

        private sealed class Layer
        {
            public List<Band> Bands = new List<Band>();
        }

        private sealed class Band
        {
            public double ChainStart, ChainEnd, From, To;   // 直線に沿う位置（頁 pt）
            public double H1, H2;
            public double P1, P2;
            public string[] Labels;                          // 外側から
            public bool Trapezoid;
            public bool Signed;                              // 符号の変わる荷重の片側：値に符号を付ける
            public bool NeedsDimension;
        }

        private void DrawLoads(JObject load)
        {
            var rows = MemberLoads(load).ToList();
            var groups = new List<Group>();

            // 節点荷重：節点の外側に矢印（同じ直線の分布荷重はその外に積む）
            if (load["load_node"] is JArray nodal)
                foreach (JObject row in nodal.OfType<JObject>())
                {
                    string n = row["n"]?.ToString();
                    if (n == null || !nodes.ContainsKey(n)) continue;
                    double tx = Number(row["tx"]), ty = Number(row["ty"]), rz = Number(row["rz"]);
                    if (tx != 0) DrawNodal(groups, n, new XPoint(Math.Sign(tx), 0), Math.Abs(tx));
                    if (ty != 0) DrawNodal(groups, n, new XPoint(0, Math.Sign(ty) * yDown), Math.Abs(ty));
                    if (rz != 0) DrawNodalMoment(n, rz);
                }

            // M/S の対（同じ範囲・向き）は 1 つの帯に Ws・Wm を並べる。帯の高さは M の値。
            var used = new HashSet<MemberLoad>();
            double reference = ReferenceLoad(rows);
            foreach (var row in rows)
            {
                if (used.Contains(row)) continue;
                used.Add(row);
                MemberLoad partner = null;
                if (row.MS == "M" || row.MS == "S")
                {
                    partner = rows.FirstOrDefault(r => !used.Contains(r) && r.MS != row.MS && (r.MS == "M" || r.MS == "S") &&
                        r.Mark == row.Mark && r.Direction == row.Direction && r.Start == row.Start && r.End == row.End &&
                        r.Chain.SequenceEqual(row.Chain));
                    if (partner != null) used.Add(partner);
                }
                switch (row.Mark)
                {
                    case "2": DrawDistributed(groups, row, partner, reference); break;
                    case "1": DrawConcentrated(row); break;
                    case "9": DrawTemperature(row); break;
                    case "11": DrawMemberMoment(row); break;
                }
            }
            foreach (var group in groups) DrawGroup(group);
        }

        private static double Number(JToken token) =>
            token == null || !double.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                || double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;

        /// <summary>
        /// 部材荷重の行を、範囲の始まりからの位置（m）に直す。保存形式の意味（FrameWebforCS の CalculationRequest・
        /// LoadMemberExpansion、旧来の DiagramMemberLoad と同じ）：
        /// <list type="bullet">
        /// <item>m2 が負なら m1〜|m2| を 1 本の部材とみなす。正の範囲は部材ごとに同じ L1・L2。</item>
        /// <item>負の L1（「-0」を含む）は、すぐ前の行（行番号が 1 つ前）の荷重の終わりから |L1| 先。前の行が無いか、分布荷重で
        /// 部材の範囲が違えば、範囲の始まりより手前（描くときに切る）。</item>
        /// <item>負の L2 は荷重の幅（始まりから |L2|）。正の L2 は分布荷重では範囲の終わりからの距離、集中荷重では 2 つ目の位置。範囲の手前から始まる分布荷重では、正の L2 も終わりの位置（FrameWeb の展開と同じ）。</item>
        /// </list>
        /// 2D の図に描けない向き（z・gz・r など）の荷重は描かない。
        /// </summary>
        private IEnumerable<MemberLoad> MemberLoads(JObject load)
        {
            if (!(load["load_member"] is JArray array)) yield break;
            int previousRow = int.MinValue;
            double previousEnd = 0;
            string previousRange = null;
            foreach (JObject row in array.OfType<JObject>().OrderBy(r => r["row"]?.Value<int?>() ?? 0))
            {
                int rowNumber = row["row"]?.Value<int?>() ?? 0;
                bool continuation = previousRow != int.MinValue && previousRow + 1 == rowNumber;
                previousRow = rowNumber;
                string m1 = row["m1"]?.ToString(), m2 = row["m2"]?.ToString();
                if (string.IsNullOrEmpty(m1)) m1 = m2;
                if (string.IsNullOrEmpty(m2)) m2 = m1;
                if (!int.TryParse(m1, out int first) || !int.TryParse(m2, out int last)) continue;
                string mark = row["mark"]?.ToString()?.Trim() ?? "";
                string direction = row["direction"]?.ToString()?.Trim().ToLowerInvariant() ?? "";
                if (mark != "9" && !PlaneDirections.Contains(direction)) continue;
                double l1 = Number(row["L1"]), l2 = Number(row["L2"]);
                bool relativeStart = l1 < 0 || (row["L1"]?.ToString() ?? "").TrimStart().StartsWith("-", StringComparison.Ordinal);
                string range = first + "," + last;
                var chains = new List<List<Member>>();
                if (last < 0)
                {
                    var chain = new List<Member>();
                    for (int id = Math.Abs(first); id <= -last; id++)
                        if (members.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var m)) chain.Add(m);
                    if (chain.Count > 0) chains.Add(chain);
                }
                else
                    for (int id = Math.Min(first, last); id <= Math.Max(first, last); id++)
                        if (members.TryGetValue(id.ToString(CultureInfo.InvariantCulture), out var m)) chains.Add(new List<Member> { m });
                double rowEnd = previousEnd;
                foreach (var chain in chains)
                {
                    double length = chain.Sum(m => Distance(nodes[m.I], nodes[m.J]));
                    bool point = mark == "1" || mark == "11";
                    bool carried = relativeStart && continuation && (point || range == previousRange);
                    double start = carried ? previousEnd + Math.Abs(l1) : relativeStart ? -Math.Abs(l1) : l1;
                    // 範囲の手前から始まる分布荷重の正の L2 は、FrameWeb の展開（数値の位置）と同じく終わりの位置
                    double end = l2 < 0 ? start + Math.Abs(l2) : point || start < 0 ? l2 : length - l2;
                    rowEnd = end;
                    yield return new MemberLoad
                    {
                        Row = rowNumber, Chain = chain, Direction = direction, Mark = mark,
                        MS = row["ms"]?.ToString()?.Trim().ToUpperInvariant() ?? "",
                        P1 = Number(row["P1"]), P2 = Number(row["P2"]), Start = start, End = end,
                    };
                }
                previousEnd = rowEnd;
                previousRange = range;
            }
        }

        private static readonly string[] PlaneDirections = { "x", "y", "gx", "gy" };

        /// <summary>
        /// 帯の高さの基準 B（FrameG の BU荷重MAX）。部材に直角な分布荷重の最大値。見本のケース 1 では下床版（最も下の水平の部材）の
        /// 12.30 を除いた 8.60 が基準になる（12.30 の帯は B より高い。見本 20.04 pt）。下の部材にしか荷重が無ければその最大値。
        /// </summary>
        private double ReferenceLoad(List<MemberLoad> rows)
        {
            var perpendicular = rows.Where(r => r.Mark == "2" && r.Direction != "x").ToList();
            if (perpendicular.Count == 0) return 1;
            double bottom = nodes.Values.Max(p => p.Y);
            bool Lower(MemberLoad r) => r.Chain.All(m => Math.Abs(nodes[m.I].Y - bottom) < 1e-9 && Math.Abs(nodes[m.J].Y - bottom) < 1e-9);
            var source = perpendicular.Where(r => !Lower(r)).ToList();
            if (source.Count == 0) source = perpendicular;
            double max = source.Max(r => Math.Max(Math.Abs(r.P1), Math.Abs(r.P2)));
            return max > 0 ? max : 1;
        }

        private static double BandHeight(double p, double reference, bool steep, bool horizontal)
        {
            p = Math.Abs(p);
            if (p == 0) return 0;
            double log = Math.Log(reference * 10 / Math.Log(10) / p);
            if (log <= 1e-6) return BandCap;
            double h = steep
                ? BandUnit * Math.Pow(p / reference, 0.8) * 1.2 * 1.3 * 1.3 / log
                : BandUnit * Math.Pow(p / reference, 0.2) * 1.2 / log;
            // 小さな荷重（B の 1/3 未満）は (B/P)^0.2 倍（FrameG は 100 twip 未満のとき。見本のケース 1 の 2.30 はこれで 7.2 pt）、1/10 未満で 80 twip 未満なら 80 twip
            if (horizontal && reference / p > 3) h *= Math.Pow(reference / p, 0.2);
            if (horizontal && reference / p > 10 && h < 4.0) h = 4.0;
            return steep ? h : Math.Min(h, BandCap);   // 鉛直の部材の台形（見本のケース 4 の 41.00 は 22.9 pt）は上限なし
        }

        private sealed class ChainGeometry
        {
            public XPoint Start, End, U;
            public double Length, ModelLength;
            public double FreeFrom, FreeTo;   // 剛域の部材を除いた範囲（頁）
        }

        private ChainGeometry Geometry(List<Member> chain)
        {
            // 部材の向きを揃えて端から端へ
            var first = chain[0];
            var last = chain[chain.Count - 1];
            string startId = first.I, endId = last.J;
            if (chain.Count > 1)
            {
                var second = chain[1];
                if (first.I == second.I || first.I == second.J) startId = first.J;
                var before = chain[chain.Count - 2];
                endId = last.I == before.I || last.I == before.J ? last.J : last.I;
            }
            XPoint a = NodePoint(startId), b = NodePoint(endId);
            var g = new ChainGeometry { Start = a, End = b, U = Unit(a, b), Length = Distance(a, b), ModelLength = Distance(nodes[startId], nodes[endId]) };
            double from = 0, to = g.Length;
            for (int k = 0; k < chain.Count && chain[k].Rigid; k++)
                from = Math.Max(from, Math.Max(Along(g, NodePoint(chain[k].I)), Along(g, NodePoint(chain[k].J))));
            for (int k = chain.Count - 1; k >= 0 && chain[k].Rigid; k--)
                to = Math.Min(to, Math.Min(Along(g, NodePoint(chain[k].I)), Along(g, NodePoint(chain[k].J))));
            if (to <= from) { from = 0; to = g.Length; }
            g.FreeFrom = from;
            g.FreeTo = to;
            return g;
        }

        private static double Along(ChainGeometry g, XPoint p) => (p.X - g.Start.X) * g.U.X + (p.Y - g.Start.Y) * g.U.Y;

        /// <summary>荷重の向き（頁、単位ベクトル）。local y はデータの座標で部材の向きを +90° 回した向き。</summary>
        private XPoint Force(ChainGeometry g, string direction, double sign)
        {
            switch (direction)
            {
                case "x": return new XPoint(g.U.X * sign, g.U.Y * sign);
                case "gx": return new XPoint(sign, 0);
                case "gy": return new XPoint(0, sign * yDown);
                default: return new XPoint(-g.U.Y * yDown * sign, g.U.X * yDown * sign);
            }
        }

        private static double T(Group g, XPoint p) => (p.X - g.Start.X) * g.U.X + (p.Y - g.Start.Y) * g.U.Y;

        private Group FindGroup(List<Group> groups, XPoint start, XPoint end, XPoint side)
        {
            // 直線は向きを揃える（左から右、上から下）
            var u = Unit(start, end);
            if (Math.Abs(u.X) >= Math.Abs(u.Y) ? u.X < 0 : u.Y < 0) { var t = start; start = end; end = t; u = new XPoint(-u.X, -u.Y); }
            foreach (var g in groups)
            {
                if (Math.Abs(g.U.X * u.Y - g.U.Y * u.X) > 1e-6) continue;
                if (Math.Abs((start.X - g.Start.X) * g.U.Y - (start.Y - g.Start.Y) * g.U.X) > 0.5) continue;
                if (g.S.X * side.X + g.S.Y * side.Y < 0.5) continue;
                // 直線の範囲を広げる
                double a = (start.X - g.Start.X) * g.U.X + (start.Y - g.Start.Y) * g.U.Y;
                double b = (end.X - g.Start.X) * g.U.X + (end.Y - g.Start.Y) * g.U.Y;
                if (a < 0) { ShiftGroup(g, a); }
                g.Length = Math.Max(g.Length, b - Math.Min(a, 0));
                return g;
            }
            var group = new Group { Start = start, U = u, S = side, Length = Distance(start, end), Vertical = Math.Abs(u.Y) > Math.Abs(u.X) };
            groups.Add(group);
            return group;
        }

        private static void ShiftGroup(Group g, double a)
        {
            g.Start = new XPoint(g.Start.X + g.U.X * a, g.Start.Y + g.U.Y * a);
            foreach (var layer in g.Layers)
                foreach (var band in layer.Bands)
                {
                    band.ChainStart -= a; band.ChainEnd -= a; band.From -= a; band.To -= a;
                }
        }

        /// <summary>荷重の位置 Start〜End（m）を範囲 0〜長さに切り、切った端の値を直線で補う。載る部分が無ければ偽。</summary>
        private static bool Clip(MemberLoad row, double length, out double from, out double to, out double p1, out double p2)
        {
            from = Math.Max(0, Math.Min(row.Start, row.End));
            to = Math.Min(length, Math.Max(row.Start, row.End));
            double At(double x) => row.End != row.Start ? row.P1 + (row.P2 - row.P1) * (x - row.Start) / (row.End - row.Start) : row.P1;
            p1 = At(from);
            p2 = At(to);
            return to > from + 1e-9;
        }

        private void DrawDistributed(List<Group> groups, MemberLoad row, MemberLoad partner, double reference)
        {
            var g = Geometry(row.Chain);
            if (g.Length <= 0 || g.ModelLength <= 0) return;
            if (!Clip(row, g.ModelLength, out double from, out double to, out double p1, out double p2)) return;
            double scale = g.Length / g.ModelLength;
            if (p1 == 0 && p2 == 0) return;
            if (p1 * p2 < 0)
            {
                // 符号が変わる荷重：0 になる点で分け、両側に符号つきの値で描く
                double zero = from + (to - from) * Math.Abs(p1) / (Math.Abs(p1) + Math.Abs(p2));
                DrawBand(groups, row, null, reference, g, from * scale, zero * scale, p1, 0, true);
                DrawBand(groups, row, null, reference, g, zero * scale, to * scale, 0, p2, true);
                return;
            }
            DrawBand(groups, row, partner, reference, g, from * scale, to * scale, p1, p2, false);
        }

        /// <summary>
        /// 一つの向きの分布荷重（from・to は範囲の始まりからの頁の長さ）。部材に平行な荷重（x、部材に平行な gx・gy）は破線の矢印。
        /// </summary>
        private void DrawBand(List<Group> groups, MemberLoad row, MemberLoad partner, double reference, ChainGeometry g,
            double from, double to, double p1, double p2, bool signed)
        {
            double p = Math.Abs(p1) >= Math.Abs(p2) ? p1 : p2;
            var f = Force(g, row.Direction, Math.Sign(p));
            if (row.Direction == "x" || Math.Abs(f.X * g.U.Y - f.Y * g.U.X) < 1e-6)
            {
                DrawAxial(g, from, to, p1, p2, f);
                return;
            }
            var side = new XPoint(-f.X, -f.Y);
            // 荷重の側を直線の法線に揃える
            var normal = new XPoint(-g.U.Y, g.U.X);
            if (normal.X * side.X + normal.Y * side.Y < 0) normal = new XPoint(-normal.X, -normal.Y);
            var group = FindGroup(groups, g.Start, g.End, normal);
            double t0 = T(group, g.Start), t1 = T(group, g.End);
            double a = T(group, Lerp(g.Start, g.End, from / g.Length)), b = T(group, Lerp(g.Start, g.End, to / g.Length));
            bool reversed = t1 < t0;
            if (reversed) { (a, b) = (b, a); (p1, p2) = (p2, p1); }
            double chainA = Math.Min(t0, t1), chainB = Math.Max(t0, t1);
            bool trapezoid = Math.Round(p1, 2) != Math.Round(p2, 2);
            bool steep = trapezoid && group.Vertical && Math.Max(Math.Abs(p1), Math.Abs(p2)) / reference > 0.2;
            var band = new Band
            {
                ChainStart = chainA, ChainEnd = chainB, From = a, To = b, P1 = p1, P2 = p2, Trapezoid = trapezoid, Signed = signed,
                H1 = BandHeight(trapezoid ? p1 : p, reference, steep, !group.Vertical),
                H2 = BandHeight(trapezoid ? p2 : p, reference, steep, !group.Vertical),
                NeedsDimension = row.Chain.Count > 1 || from > 1e-6 || to < g.Length - 1e-6,
            };
            if (partner != null)
            {
                // M/S の対：帯は M の値、文字は Ws（外）と Wm。両端の値が違えば「始まり～終わり」
                var m = row.MS == "M" ? row : partner;
                var s = row.MS == "M" ? partner : row;
                Clip(m, g.ModelLength, out _, out _, out double m1, out double m2);
                Clip(s, g.ModelLength, out _, out _, out double s1, out double s2);
                if (reversed) { (m1, m2) = (m2, m1); (s1, s2) = (s2, s1); }
                band.H1 = BandHeight(m1, reference, false, !group.Vertical);
                band.H2 = BandHeight(m2, reference, false, !group.Vertical);
                band.Trapezoid = false;
                band.Labels = new[] { "  Ws=" + Range(s1, s2), "  Wm=" + Range(m1, m2) };
            }
            else if (!trapezoid) band.Labels = new[] { "  " + Value(p, signed) };
            AddToLayer(group, band);
        }

        /// <summary>両端が同じなら 1 つの値、違えば「始まり～終わり」。</summary>
        private static string Range(double p1, double p2) =>
            Math.Round(p1, 2) == Math.Round(p2, 2) ? Value(p1) : Value(p1, p1 * p2 < 0) + "～" + Value(p2, p1 * p2 < 0);

        private static void AddToLayer(Group group, Band band)
        {
            foreach (var layer in group.Layers.Where(l => l.Bands.Count > 0))
                if (layer.Bands.All(o => o.ChainEnd <= band.ChainStart + 1e-6 || band.ChainEnd <= o.ChainStart + 1e-6))
                {
                    layer.Bands.Add(band);
                    return;
                }
            var added = new Layer();
            added.Bands.Add(band);
            group.Layers.Add(added);
        }

        private void DrawGroup(Group group)
        {
            double offset = group.Nodal ? NodalGap + NodalLength + NodalNextGap : (group.Vertical ? VerticalBandGap : HorizontalBandGap);
            foreach (var layer in group.Layers.Where(l => l.Bands.Count > 0))
            {
                double outer = offset;
                foreach (var band in layer.Bands)
                {
                    DrawBand(group, band, offset);
                    outer = Math.Max(outer, offset + Math.Max(band.H1, band.H2));
                }
                double dimension = DrawLabelsAndDimension(group, layer, offset, outer);
                offset = dimension + NextLayerGap;
            }
        }

        private XPoint G(Group g, double t, double d) => At(g.Start, g.U, g.S, t, d);

        private void DrawBand(Group g, Band band, double inner)
        {
            var gfx = mc.gfx;
            var outline = new XPen(XColors.Black, BandPen);
            var thin = new XPen(XColors.Black, ArrowPen);
            double h1 = band.H1, h2 = band.H2;
            double Height(double t) => band.To > band.From ? h1 + (h2 - h1) * (t - band.From) / (band.To - band.From) : h1;
            gfx.DrawLine(outline, G(g, band.From, inner + h1), G(g, band.To, inner + h2));
            gfx.DrawLine(outline, G(g, band.From, inner), G(g, band.To, inner));
            double length = band.To - band.From;
            int count = Math.Max(1, (int)Math.Round(length / (g.Vertical ? VerticalPitch : HorizontalPitch)));
            for (int k = 0; k <= count; k++)
            {
                double t = band.From + length * k / count;
                bool end = k == 0 || k == count;
                double h = Height(t);
                var pen = end ? outline : thin;
                XPoint tail = G(g, t, inner + h), tip = G(g, t, inner);
                gfx.DrawLine(pen, tail, tip);
                if (h > HeadLength / 2)
                {
                    gfx.DrawLine(pen, G(g, t + HeadHalf, inner + HeadLength), tip);
                    gfx.DrawLine(pen, G(g, t - HeadHalf, inner + HeadLength), tip);
                }
            }
        }

        /// <summary>帯の値と寸法線。寸法線の位置（部材からの距離）を返す。</summary>
        private double DrawLabelsAndDimension(Group g, Layer layer, double inner, double outer)
        {
            bool left = g.Vertical && g.S.X < 0;
            bool below = !g.Vertical && g.S.Y > 0;
            int lines = layer.Bands.Max(b => b.Labels?.Length ?? 1);
            double dimension;
            if (!g.Vertical)
            {
                double h = layer.Bands.Max(b => Math.Max(b.H1, b.H2));
                dimension = outer + h + DimensionGap + (lines - 1) * ExtraDimensionLine;
                if (below) dimension += LowerLabelDrop;
            }
            else if (layer.Bands.Any(b => b.Trapezoid))
            {
                double reserved = layer.Bands.Where(b => b.Trapezoid)
                    .Max(b => Math.Max(Value(b.P1, b.Signed).Length, Value(b.P2, b.Signed).Length) + 5) * HalfWidth;
                dimension = outer + (left ? EndLabelInner + reserved + EndLabelDimensionLeft : EndLabelOuter + reserved + EndLabelDimensionRight);
            }
            else dimension = outer + (left ? SideDimensionLeft : SideDimensionRight);

            foreach (var band in layer.Bands)
            {
                double c = (band.From + band.To) / 2;
                if (band.Labels != null)
                {
                    for (int k = 0; k < band.Labels.Length; k++)
                    {
                        // 外側の行から：Ws の行は Wm の行の外
                        int fromBand = band.Labels.Length - 1 - k;
                        double bandOuter = inner + Math.Max(band.H1, band.H2);
                        if (!g.Vertical)
                        {
                            double depth = below ? bandOuter + LowerLabelDrop + fromBand * ExtraLabelLine
                                : bandOuter + LabelLift + fromBand * ExtraLabelLine;
                            var centre = G(g, c, depth);
                            ValueWithUnit(band.Labels[k], "kN/m", centre.X, centre.Y, false);
                        }
                        else
                        {
                            var centre = G(g, c, bandOuter);
                            double x = left ? centre.X - SideLabelLeft - fromBand * ExtraLabelLine
                                : centre.X + SideLabelRight + fromBand * ExtraLabelLine;
                            ValueWithUnit(band.Labels[k], "kN/m", x, centre.Y, true);
                        }
                    }
                }
                else if (band.Trapezoid) EndLabels(g, layer, band, inner, left, below);
            }

            if (!layer.Bands.Any(b => b.NeedsDimension)) return Math.Max(dimension, outer);
            DrawDimension(g, layer, dimension, left, below);
            return dimension;
        }

        /// <summary>台形の荷重の値を両端に横書き。並ぶ帯と接する端は内側、離れた端は外側に置く。</summary>
        private void EndLabels(Group g, Layer layer, Band band, double inner, bool left, bool below)
        {
            foreach (bool start in new[] { true, false })
            {
                double t = start ? band.From : band.To;
                double p = start ? band.P1 : band.P2;
                if (p == 0) continue;
                double h = start ? band.H1 : band.H2;
                var end = G(g, t, inner + h);
                bool shared = layer.Bands.Any(o => o != band && Math.Abs((start ? o.To : o.From) - t) < 0.5);
                string text = Value(p, band.Signed);
                double width = text.Length * HalfWidth;
                double unitWidth = mc.gfx.MeasureString(" kN/m", unitFont).Width * TextStretch * TextStretch;
                if (g.Vertical)
                {
                    // 始まり（上の端）は外なら上、内なら下。終わり（下の端）は外なら下、内なら上
                    bool aboveText = start ? !shared : shared;
                    double y = aboveText ? end.Y - EndLabelAbove : end.Y + EndLabelBelow;
                    double x = left ? end.X - EndLabelInner - (width + unitWidth - 1.0) : end.X + EndLabelOuter;
                    if (!start && !shared)
                    {
                        // 下の離れた端：見本（ケース 4 の 41.00）は端の線に掛かり、骨組の側へ寄る（規則は未解明、見本の値）
                        y = end.Y + FreeEndBelow;
                        x += left ? FreeEndShiftLeft : -FreeEndShiftRight;
                    }
                    DrawValue(text, " kN/m", x, y, false);
                }
                else
                {
                    double y = below ? end.Y + LowerLabelDrop : end.Y - LabelLift;
                    double x = start ? end.X : end.X - width - unitWidth;
                    DrawValue(text, " kN/m", x, y, false);
                }
            }
        }

        private void DrawDimension(Group g, Layer layer, double dimension, bool left, bool below)
        {
            var gfx = mc.gfx;
            var pen = new XPen(XColors.Black, DimensionPen);
            var marks = new SortedSet<double>();
            foreach (var band in layer.Bands)
            {
                marks.Add(Math.Round(band.ChainStart, 3)); marks.Add(Math.Round(band.From, 3));
                marks.Add(Math.Round(band.To, 3)); marks.Add(Math.Round(band.ChainEnd, 3));
            }
            var list = marks.ToList();
            for (int k = list.Count - 1; k > 0; k--) if (list[k] - list[k - 1] < 0.05) list.RemoveAt(k);
            double tick = g.Vertical ? (left ? SideTickLeft : SideTickRight) : DimensionTick;
            foreach (double t in list)
                gfx.DrawLine(pen, G(g, t, dimension), G(g, t, dimension - tick));
            double unitsPerPoint = 1 / Math.Sqrt((g.U.X * scaleX) * (g.U.X * scaleX) + (g.U.Y * scaleY) * (g.U.Y * scaleY));
            for (int k = 1; k < list.Count; k++)
            {
                double a = list[k - 1], b = list[k];
                gfx.DrawLine(pen, G(g, a, dimension), G(g, b, dimension));
                gfx.DrawLine(pen, G(g, a + DimensionHead, dimension + DimensionHeadHalf), G(g, a, dimension));
                gfx.DrawLine(pen, G(g, a + DimensionHead, dimension - DimensionHeadHalf), G(g, a, dimension));
                gfx.DrawLine(pen, G(g, b - DimensionHead, dimension + DimensionHeadHalf), G(g, b, dimension));
                gfx.DrawLine(pen, G(g, b - DimensionHead, dimension - DimensionHeadHalf), G(g, b, dimension));
                double metres = (b - a) * unitsPerPoint;
                string text = metres.ToString("0.00#", CultureInfo.InvariantCulture);
                double width = text.Length * HalfWidth;
                var centre = G(g, (a + b) / 2, dimension);
                if (!g.Vertical)
                {
                    double y = below ? centre.Y + LowerLabelDrop : centre.Y - DimensionTextLift;
                    Text(text, valueFont, centre.X - width / 2, y);
                }
                else Rotated(text, valueFont, centre.X - 1.0, centre.Y + width / 2);
            }
        }

        /// <summary>荷重の値。向きは図の側で分かるので絶対値（見本）。符号の変わる荷重だけ符号を付ける。</summary>
        private static string Value(double value, bool signed = false) =>
            (signed ? value : Math.Abs(value)).ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>帯の中央 (x, y) に値と単位。横書きは y が基線、回転は x が基線。</summary>
        private void ValueWithUnit(string text, string unit, double x, double y, bool rotated)
        {
            double width = text.Length * HalfWidth;
            double start = (width + LabelCentring) / 2;
            if (!rotated) DrawValue(text, " " + unit, x - start, y, false);
            else DrawValue(text, " " + unit, x, y + start, true);
        }

        /// <summary>値と小さな単位。横書きは (x, y) が始まりの基線、回転は文字が下から上へ。</summary>
        private void DrawValue(string text, string unit, double x, double y, bool rotated)
        {
            double width = text.Length * HalfWidth;
            if (!rotated)
            {
                Text(text, valueFont, x, y);
                Text(unit, unitFont, x + width - 1.0, y - UnitRise);
            }
            else
            {
                Rotated(text, valueFont, x, y);
                Rotated(unit, unitFont, x - UnitRise, y - width + 1.0);
            }
        }

        /// <summary>
        /// 見本の文字（PDF の大きさ <paramref name="size"/>、送りはその TextStretch 倍）。字の大きさを 1/TextStretch にして横に
        /// TextStretch² 倍する（PDF の文字の大きさは行列の縦横の幾何平均なので <paramref name="size"/> のまま）。
        /// </summary>
        private static XFont Font(double size) => new XFont("MS Gothic", size / TextStretch);

        /// <summary>横書きの文字（(x, y) が基線の始まり）。FrameG の印字は文字の送りが 1 % ほど広い。</summary>
        private void Text(string text, XFont font, double x, double y) => Draw(text, font, x, y, 0);

        /// <summary>文字を 90° 回して下から上へ（基線の始まりが (x, y)）。</summary>
        private void Rotated(string text, XFont font, double x, double y) => Draw(text, font, x, y, -90);

        private void Draw(string text, XFont font, double x, double y, double angle)
        {
            var point = new XPoint(x, y);
            var state = mc.gfx.Save();
            if (angle != 0) mc.gfx.RotateAtTransform(angle, point);
            mc.gfx.ScaleAtTransform(TextStretch * TextStretch, 1, point);
            mc.gfx.DrawString(text, font, XBrushes.Black, point, XStringFormats.BaseLineLeft);
            mc.gfx.Restore(state);
        }

        /// <summary>
        /// 部材に平行な分布荷重：破線の矢印と「q =」（見本）。矢印は載る範囲の中央（範囲が全長なら剛域の部材を除いた中央に先）。
        /// 範囲が全長でなければ両端に短い線を引く。両端の値が違えば「q =始まり～終わり」。
        /// </summary>
        private void DrawAxial(ChainGeometry g, double from, double to, double p1, double p2, XPoint f)
        {
            var gfx = mc.gfx;
            var pen = new XPen(XColors.Black, ArrowPen);
            var centre = FrameCentre;
            var normal = new XPoint(-g.U.Y, g.U.X);
            bool whole = from <= 1e-6 && to >= g.Length - 1e-6;
            double middle = whole ? (g.FreeFrom + g.FreeTo) / 2 : (from + to) / 2;
            var mid = Lerp(g.Start, g.End, middle / g.Length);
            if ((centre.X - mid.X) * normal.X + (centre.Y - mid.Y) * normal.Y < 0) normal = new XPoint(-normal.X, -normal.Y);
            double length = whole ? AxialLength : Math.Min(AxialLength, to - from);
            var line = new XPoint(mid.X + normal.X * AxialOffset, mid.Y + normal.Y * AxialOffset);
            // 全長なら先が中央（見本）、一部なら矢印の中央が範囲の中央
            var tip = whole ? line : new XPoint(line.X + f.X * length / 2, line.Y + f.Y * length / 2);
            if (!whole)
                foreach (double t in new[] { from, to })
                {
                    var end = Lerp(g.Start, g.End, t / g.Length);
                    gfx.DrawLine(pen, new XPoint(end.X + normal.X * (AxialOffset - AxialRangeTick), end.Y + normal.Y * (AxialOffset - AxialRangeTick)),
                        new XPoint(end.X + normal.X * (AxialOffset + AxialRangeTick), end.Y + normal.Y * (AxialOffset + AxialRangeTick)));
                }
            var tail = new XPoint(tip.X - f.X * length, tip.Y - f.Y * length);
            double head = Math.Min(AxialHead, length / 2);
            double shaft = length - head;
            for (double t = 0; t < shaft - 1e-6; t += AxialDash + AxialSpace)
                gfx.DrawLine(pen, Lerp(tail, tip, t / length), Lerp(tail, tip, Math.Min(t + AxialDash, shaft) / length));
            var side = new XPoint(-f.Y, f.X);
            var back = new XPoint(tip.X - f.X * head, tip.Y - f.Y * head);
            gfx.DrawLine(pen, new XPoint(back.X + side.X * AxialHeadHalf, back.Y + side.Y * AxialHeadHalf), tip);
            gfx.DrawLine(pen, new XPoint(back.X - side.X * AxialHeadHalf, back.Y - side.Y * AxialHeadHalf), tip);

            string text = "q =" + Range(p1, p2);
            if (Math.Abs(g.U.Y) > Math.Abs(g.U.X))
            {
                // 回転した文字は基線の左に字面がある。骨組の内側（右）なら破線の右、内側が左なら破線の左
                double x = normal.X > 0 ? line.X + AxialLabelInside : line.X - AxialLabelOutside;
                DrawValue(text, " kN/m", x, Math.Max(tip.Y, tail.Y), true);
            }
            else
            {
                double y = normal.Y > 0 ? line.Y + LowerLabelDrop + 2 : line.Y - 2;
                DrawValue(text, " kN/m", Math.Min(tip.X, tail.X), y, false);
            }
        }

        private void DrawTemperature(MemberLoad row)
        {
            var g = Geometry(row.Chain);
            if (g.Length <= 0) return;
            var mid = Lerp(g.Start, g.End, 0.5);
            var centre = FrameCentre;
            var normal = new XPoint(-g.U.Y, g.U.X);
            if ((centre.X - mid.X) * normal.X + (centre.Y - mid.Y) * normal.Y > 0) normal = new XPoint(-normal.X, -normal.Y);
            string text = Math.Abs(row.P1).ToString("0.0", CultureInfo.InvariantCulture) + "℃";
            if (row.P1 < 0) text = "-" + text;
            double width = mc.gfx.MeasureString(text, valueFont).Width * TextStretch * TextStretch;
            if (Math.Abs(g.U.X) >= Math.Abs(g.U.Y))
            {
                double y = normal.Y < 0 ? mid.Y - TemperatureLift : mid.Y + TemperatureLift + LowerLabelDrop;
                Text(text, valueFont, mid.X - width / 2, y);
            }
            else
            {
                double x = normal.X < 0 ? mid.X - TemperatureLift + 7 : mid.X + TemperatureLift;
                Rotated(text, valueFont, x, mid.Y + width / 2);
            }
        }

        private void DrawConcentrated(MemberLoad row)
        {
            var g = Geometry(row.Chain);
            if (g.Length <= 0 || g.ModelLength <= 0) return;
            foreach (var (distance, value) in new[] { (row.Start, row.P1), (row.End, row.P2) })
            {
                // 範囲の外の点は載らない（FrameWeb の展開と同じ）
                if (value == 0 || distance < 0 || distance > g.ModelLength + 1e-9) continue;
                var f = Force(g, row.Direction, Math.Sign(value));
                Arrow(Lerp(g.Start, g.End, distance / g.ModelLength), f, Math.Abs(value), "kN");
            }
        }

        private void DrawMemberMoment(MemberLoad row)
        {
            var g = Geometry(row.Chain);
            if (g.Length <= 0 || g.ModelLength <= 0) return;
            foreach (var (distance, value) in new[] { (row.Start, row.P1), (row.End, row.P2) })
            {
                if (value == 0 || distance < 0 || distance > g.ModelLength + 1e-9) continue;
                Moment(Lerp(g.Start, g.End, distance / g.ModelLength), value);
            }
        }

        private void DrawNodal(List<Group> groups, string node, XPoint f, double value)
        {
            var point = NodePoint(node);
            Arrow(point, f, value, "kN");
            // 節点を通り荷重に直角な部材の直線に節点荷重の段を作る（分布荷重はその外に積む）
            var side = new XPoint(-f.X, -f.Y);
            foreach (var m in members.Values.Where(m => m.I == node || m.J == node))
            {
                XPoint a = NodePoint(m.I), b = NodePoint(m.J);
                var u = Unit(a, b);
                if (Math.Abs(u.X * f.X + u.Y * f.Y) > 0.1) continue;
                var group = FindGroup(groups, a, b, side);
                group.Nodal = true;
            }
        }

        /// <summary>節点などに向かう矢印（先は点から NodalGap 手前）と、回転・横書きの値。</summary>
        private void Arrow(XPoint point, XPoint f, double value, string unit)
        {
            var gfx = mc.gfx;
            var tip = new XPoint(point.X - f.X * NodalGap, point.Y - f.Y * NodalGap);
            var tail = new XPoint(tip.X - f.X * NodalLength, tip.Y - f.Y * NodalLength);
            gfx.DrawLine(new XPen(XColors.Black, NodalShaftPen), tail, tip);
            var thin = new XPen(XColors.Black, ArrowPen);
            var side = new XPoint(-f.Y, f.X);
            var back = new XPoint(tip.X - f.X * NodalHead, tip.Y - f.Y * NodalHead);
            gfx.DrawLine(thin, new XPoint(back.X + side.X * NodalHeadHalf, back.Y + side.Y * NodalHeadHalf), tip);
            gfx.DrawLine(thin, new XPoint(back.X - side.X * NodalHeadHalf, back.Y - side.Y * NodalHeadHalf), tip);
            string text = "  " + Value(value);
            var centre = FrameCentre;
            if (Math.Abs(f.Y) >= Math.Abs(f.X))
            {
                // 骨組の中心より左の節点は矢印の左、右は矢印の右に（文字は下から上へ）
                double x = point.X <= centre.X ? tip.X - NodalLabelLeft : tip.X + NodalLabelRight;
                DrawValue(text, " " + unit, x, Math.Max(tip.Y, tail.Y) - 0.14, true);
            }
            else
            {
                double width = text.Length * HalfWidth;
                DrawValue(text, " " + unit, Math.Min(tip.X, tail.X) + (NodalLength - width) / 2 - 4, tip.Y - 2.5, false);
            }
        }

        private void Moment(XPoint point, double value)
        {
            const double radius = 9;
            var pen = new XPen(XColors.Black, NodalShaftPen);
            double start = value > 0 ? 200 : -20, sweep = value > 0 ? -220 : 220;
            mc.gfx.DrawArc(pen, point.X - radius, point.Y - radius, 2 * radius, 2 * radius, start, sweep);
            double end = (start + sweep) * Math.PI / 180;
            var tip = new XPoint(point.X + radius * Math.Cos(end), point.Y + radius * Math.Sin(end));
            var tangent = new XPoint(-Math.Sin(end) * Math.Sign(sweep), Math.Cos(end) * Math.Sign(sweep));
            var thin = new XPen(XColors.Black, ArrowPen);
            var side = new XPoint(-tangent.Y, tangent.X);
            var back = new XPoint(tip.X - tangent.X * 4, tip.Y - tangent.Y * 4);
            mc.gfx.DrawLine(thin, new XPoint(back.X + side.X * NodalHeadHalf, back.Y + side.Y * NodalHeadHalf), tip);
            mc.gfx.DrawLine(thin, new XPoint(back.X - side.X * NodalHeadHalf, back.Y - side.Y * NodalHeadHalf), tip);
            DrawValue("  " + Value(value), " kN·m", point.X + radius, point.Y - radius - 1, false);
        }

        private void DrawNodalMoment(string node, double value) => Moment(NodePoint(node), value);

        // ---- 幾何 ----

        private static double Distance(XPoint a, XPoint b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

        private static XPoint Lerp(XPoint a, XPoint b, double t) => new XPoint(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

        private static XPoint Unit(XPoint a, XPoint b)
        {
            double d = Distance(a, b);
            return d > 0 ? new XPoint((b.X - a.X) / d, (b.Y - a.Y) / d) : new XPoint(1, 0);
        }

        /// <summary>部材から骨組の外へ向かう単位の法線（ばねを描く側）。</summary>
        private XPoint Outward(XPoint a, XPoint b)
        {
            var u = Unit(a, b);
            var n = new XPoint(-u.Y, u.X);
            var centre = FrameCentre;
            var mid = Lerp(a, b, 0.5);
            return (centre.X - mid.X) * n.X + (centre.Y - mid.Y) * n.Y > 0 ? new XPoint(-n.X, -n.Y) : n;
        }
    }
}

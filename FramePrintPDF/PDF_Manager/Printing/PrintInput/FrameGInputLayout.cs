using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PdfSharpCore.Drawing;

namespace PDF_Manager.Printing
{
    // FrameG's input copy (原宿ボックス 0404, sample pages 5-3 to 5-6): the 2D input sections in FrameG's
    // order. Positions are FrameG's in points: text by its baseline and left edge, numbers by their right edge.
    // The member mark (ﾏｰｸ) and the notice point names are not in the saved input, so their cells stay empty.
    internal sealed class FrameGInputLayout
    {
        internal static bool Applies(PdfDocument document, PrintData data) => data.dimension == 2;

        private const double Top = 87.5;              // the first baseline under the page title
        private const double Line = 8.975;            // FrameG's line pitch
        private const double GroupLine = 13.4625;     // one and a half lines before a row of another member group
        private const double Half = PdfDocument.ReferencePortraitFontSize / 2; // half-width character advance
        private const double RuleLeft = 98.4, RuleRight = 525.36, RuleBelow = 4.96;

        private readonly PdfDocument document;
        private readonly PrintData data;
        private readonly XFont font;
        private int page;
        private double y;          // baseline of the last line printed
        private bool pageStarted;  // something has been printed on the current page

        private FrameGInputLayout(PdfDocument document, PrintData data, int page)
        {
            this.document = document;
            this.data = data;
            this.page = page;
            // FrameG's coordinates are A4 portrait points on any paper.
            font = new XFont(data.language == "cn" ? "SimSun" : "MS Mincho", PdfDocument.ReferencePortraitFontSize);
            pageStarted = !document.currentPos.Equals(document.initialPos);
            y = document.currentPos.Y;
        }

        internal static void Print(PdfDocument document, PrintData data, ref int page)
        {
            var layout = new FrameGInputLayout(document, data, page);
            layout.Print();
            page = layout.page;
        }

        private double Bottom => document.currentPage.Height - document.Margine.Bottom;

        private string L(string ja, string en, string cn) => data.language switch { "en" => en, "cn" => cn, _ => ja };

        private T Section<T>(string key) where T : class => data.printDatas.TryGetValue(key, out var value) ? value as T : null;

        private void Print()
        {
            var nodes = Section<InputNode>(InputNode.KEY)?.Nodes ?? new Dictionary<string, Vector3>();
            var members = Section<InputMember>(InputMember.KEY)?.members ?? new Dictionary<string, Member>();
            var sheets = Section<InputElement>(InputElement.KEY)?.Sheets;
            var materials = sheets == null || sheets.Count == 0 ? new Dictionary<string, Element>() : sheets[sheets.Keys.Min()];
            var rigids = Section<InputRigid>(InputRigid.KEY)?.rigids ?? new List<Rigid>();

            Nodes(nodes);
            Members(nodes, members, materials);
            // FrameG has at most one rigid zone row per member, with its own A and I. Other rows keep the
            // generic table, which prints every row with its material number.
            if (rigids.GroupBy(rigid => rigid.m).Any(rows => rows.Count() > 1) ||
                rigids.Any(rigid => !members.ContainsKey(rigid.m ?? "") || !materials.ContainsKey(rigid.e ?? "")))
                Legacy(InputRigid.KEY);
            else
                Rigid(nodes, members, materials, rigids);
            // FrameG's member table shows the first TYPE's material of each member. Other TYPEs, or a member
            // whose material is not in the first TYPE, add the generic material table; the generic member table
            // then gives the material numbers when they are not the member numbers.
            bool missing = members.Values.Any(member => !string.IsNullOrEmpty(member.e) && !materials.ContainsKey(member.e));
            if (missing || sheets?.Count > 1)
            {
                if (missing || members.Any(member => member.Value.e != member.Key))
                    Legacy(InputMember.KEY);
                Legacy(InputElement.KEY);
            }
            // FrameG's sample has no supports or joints; they keep their own tables here.
            Legacy(InputFixNode.KEY);
            Legacy(InputJoint.KEY);
            Springs(members, materials);
            NoticePoints(nodes, members, materials);
            Legacy(InputShell.KEY);
            var names = Section<InputLoadName>(InputLoadName.KEY)?.loadnames ?? new Dictionary<int, LoadName>();
            LoadNames(names);
            Loads(names, Section<InputLoad>(InputLoad.KEY)?.loads ?? new Dictionary<int, Load>());
            Defines(Section<InputDefine>(InputDefine.KEY)?.Defines ?? new Dictionary<int, Define>());
            Combines(Section<InputCombine>(InputCombine.KEY)?.combines ?? new Dictionary<int, Combine>());
            Pickups(Section<InputPickup>(InputPickup.KEY)?.pickups ?? new Dictionary<int, Pickup>());
            if (pageStarted)
                document.currentPos = new XPoint(document.Margine.Left, y + Line);
        }

        #region drawing

        private void Draw(string text, double x, double baseline, XStringFormat format)
        {
            if (string.IsNullOrEmpty(text)) return;
            document.CheckBudget();
            document.gfx.DrawString(text, font, XBrushes.Black, new XPoint(x, baseline), format);
        }

        private void Left(string text, double x, double baseline) => Draw(text, x, baseline, XStringFormats.BaseLineLeft);
        private void Right(string text, double x, double baseline) => Draw(text, x, baseline, XStringFormats.BaseLineRight);
        private void Center(string text, double x, double baseline) => Draw(text, x, baseline, XStringFormats.BaseLineCenter);

        private void Rule(double baseline) =>
            document.gfx.DrawLine(new XPen(XColors.Black, 0.12), RuleLeft, baseline + RuleBelow, RuleRight, baseline + RuleBelow);

        private void NewPage()
        {
            document.NewPage(ref page);
            pageStarted = false;
        }

        // The baseline of a section heading: after the previous section, or on a new page when the heading,
        // its header and the first block (need, from the heading) do not fit.
        private double Heading(double gapBefore, double need)
        {
            double heading = pageStarted ? y + gapBefore : Top;
            if (pageStarted && heading + need > Bottom)
            {
                NewPage();
                heading = Top;
            }
            pageStarted = true;
            return heading;
        }

        private readonly struct Row
        {
            public readonly double Gap;     // from the previous row's last line
            public readonly double Height;  // lines below the baseline that belong to the row
            public readonly Action<double> Draw;
            public Row(double gap, double height, Action<double> draw) { Gap = gap; Height = height; Draw = draw; }
        }

        // Rows under a header that is printed again (with the heading) on each new page.
        private void Table(double gapBefore, Action<double> header, double firstRow, IReadOnlyList<Row> rows)
        {
            if (rows.Count == 0) return;
            double heading = Heading(gapBefore, firstRow + rows[0].Height);
            header(heading);
            for (int i = 0; i < rows.Count; i++)
            {
                double baseline = i == 0 ? heading + firstRow : y + rows[i].Gap;
                if (i > 0 && baseline + rows[i].Height > Bottom)
                {
                    NewPage();
                    pageStarted = true;
                    header(Top);
                    baseline = Top + firstRow;
                }
                rows[i].Draw(baseline);
                y = baseline + rows[i].Height;
            }
        }

        // A section FrameG's sample does not show keeps the generic table, below what is printed so far.
        private void Legacy(string key)
        {
            if (!data.printDatas.TryGetValue(key, out var section)) return;
            int before = page;
            var start = pageStarted ? new XPoint(document.Margine.Left, y + 2 * Line) : document.initialPos;
            document.currentPos = start;
            section.printPDF(document, data, ref page);
            if (page == before && document.currentPos.Equals(start)) return;
            pageStarted = true;
            y = document.currentPos.Y;
        }

        #endregion

        #region values

        private static string Fixed(double value, int digits) =>
            double.IsNaN(value) ? "" : ReferenceResultLayout.Number(value, digits);

        // Load intensities and spring constants are left blank when zero, as FrameG prints them.
        private static string NonZero(double value, int digits) =>
            double.IsNaN(value) || value == 0 ? "" : Fixed(value, digits);

        private static string Plain(double value) =>
            double.IsNaN(value) || value == 0 ? "" : value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Exponent(double value) =>
            double.IsNaN(value) ? "" : value.ToString("0.00E+0", CultureInfo.InvariantCulture);

        // FrameG's area (0.0000) and inertia (0.000000) fields: a whole number too wide for the field, like a
        // rigid zone's 1000, is printed as an integer one character in from the field's left edge. Any other
        // value keeps its decimals and extends to the left.
        private void Field(double value, int digits, int width, double right, double baseline)
        {
            string text = Fixed(value, digits);
            if (text.Length > width && value == Math.Round(value))
                Left(value.ToString("0", CultureInfo.InvariantCulture), right - (width - 1) * Half, baseline);
            else
                Right(text, right, baseline);
        }

        // A name in the lines that fit its column.
        private List<string> Wrapped(string text, double width) =>
            string.IsNullOrEmpty(text) ? new List<string> { "" } : ReferenceResultLayout.Wrap(document, font, text, width);

        private static void DrawLines(IReadOnlyList<string> lines, double x, double baseline, Action<string, double, double> draw)
        {
            for (int i = 0; i < lines.Count; i++)
                draw(lines[i], x, baseline + i * Line);
        }

        private static IEnumerable<KeyValuePair<string, T>> ById<T>(IEnumerable<KeyValuePair<string, T>> items) =>
            items.OrderBy(item => int.TryParse(item.Key, out int id) ? id : int.MaxValue).ThenBy(item => item.Key, StringComparer.Ordinal);

        // C1, C2, ... columns in their number order.
        private static IEnumerable<KeyValuePair<int, T>> Columns<T>(IEnumerable<KeyValuePair<string, T>> items) =>
            items.Select(item => new KeyValuePair<int, T>(int.TryParse(item.Key.TrimStart('C'), out int column) ? column : 0, item.Value))
                .Where(item => item.Key > 0).OrderBy(item => item.Key);

        private static double Length(IDictionary<string, Vector3> nodes, Member member) =>
            member != null && nodes.TryGetValue(member.ni ?? "", out var i) && nodes.TryGetValue(member.nj ?? "", out var j)
                ? Math.Sqrt(Math.Pow(j.x - i.x, 2) + Math.Pow(j.y - i.y, 2)) : double.NaN;

        private static Element Material(IDictionary<string, Element> materials, Member member) =>
            member != null && materials.TryGetValue(member.e ?? "", out var element) ? element : null;

        // FrameG leaves half a line between member groups (rows of another ﾏｰｸ). The mark is not in the
        // saved input; the member name (material name) stands in for it.
        private static string Group(IDictionary<string, Element> materials, Member member) => Material(materials, member)?.name ?? "";

        #endregion

        #region sections

        private void Nodes(Dictionary<string, Vector3> nodes)
        {
            void Header(double h)
            {
                Left(L("格 点 DATA", "NODE DATA", "节 点 DATA"), 78.4, h);
                Left(L("格点", "Node", "节点"), 89.5, h + 18.0);
                Left("No.", 95.5, h + 27.0);
                Left("X (m)", 140.8, h + 27.0);
                Left("Y (m)", 195.0, h + 27.0);
            }
            // FrameWeb's 2D y points down; FrameG's input copy shows y up.
            var rows = ById(nodes).Select(node => new Row(Line, 0, b =>
            {
                Right(node.Key, 105.5, b);
                Right(Fixed(node.Value.x, 3), 166.8, b);
                Right(Fixed(-node.Value.y, 3), 221.0, b);
            })).ToList();
            Table(31.5, Header, 44.9, rows);
        }

        // Rows of members; a member name wider than its column continues on the following lines.
        private List<Row> GroupedRows(IEnumerable<KeyValuePair<string, Member>> members, IDictionary<string, Element> materials,
            double nameWidth, Action<KeyValuePair<string, Member>, double, List<string>> draw)
        {
            var rows = new List<Row>();
            string group = null;
            foreach (var member in members)
            {
                string next = Group(materials, member.Value);
                var name = Wrapped(Material(materials, member.Value)?.name, nameWidth);
                rows.Add(new Row(group == null || group == next ? Line : GroupLine, (name.Count - 1) * Line, b => draw(member, b, name)));
                group = next;
            }
            return rows;
        }

        private void Members(Dictionary<string, Vector3> nodes, Dictionary<string, Member> members, IDictionary<string, Element> materials)
        {
            void Header(double h)
            {
                Left(L("部 材 DATA", "MEMBER DATA", "构 件 DATA"), 78.4, h);
                double h1 = h + 17.8, h2 = h + 26.8;
                Left(L("部材", "Member", "构件"), 94.0, h1);
                Left(L("格点番号", "Nodes", "节点编码"), 136.7, h1);
                Left(L("部材長", "Length", "构件长"), 198.5, h1);
                Left(L("ﾏｰｸ", "Mark", "标记"), 241.2, h1);
                Left(L("部材名", "Name", "构件名"), 272.9, h1);
                Left(L("部材数", "Count", "构件数"), 313.6, h1);
                Left(L("弾性係数", "Elastic", "弹性系数"), 356.9, h1);
                Left(L("膨張係数", "Expansion", "膨胀系数"), 407.8, h1);
                Left(L("断 面 積", "Area", "截面面积"), 453.7, h1);
                Left(L("断面二次ﾓｰﾒﾝﾄ", "Inertia", "截面二次力矩"), 493.9, h1);
                Left("No.", 105.7, h2);
                Left("I-TAN", 127.2, h2);
                Left("J-TAN", 157.7, h2);
                Left("(m)", 202.8, h2);
                Left("E(kN/m2)", 357.7, h2);
                Left("ESP", 417.4, h2);
                Left("A (m2)", 458.3, h2);
                Left("I (m4)", 505.9, h2);
            }
            // The name is centred between the mark and E columns.
            var rows = GroupedRows(ById(members), materials, 72.0, (member, b, name) =>
            {
                Right(member.Key, 105.5, b);
                Right(member.Value.ni, 146.0, b);
                Right(member.Value.nj, 176.5, b);
                Right(Fixed(Length(nodes, member.Value), 3), 223.3, b);
                Element material = Material(materials, member.Value);
                if (material == null) return;
                DrawLines(name, 286.4, b, Center);
                Right(Exponent(material.E), 390.4, b);
                Right(Exponent(material.Xp), 438.0, b);
                Field(material.A, 4, 6, 481.9, b);
                Field(material.Iz, 6, 8, 536.4, b);
            });
            Table(31.5, Header, 44.8, rows);
        }

        // One row per member, with the member's rigid zones (at most one row each; the caller checks).
        private void Rigid(Dictionary<string, Vector3> nodes, Dictionary<string, Member> members, IDictionary<string, Element> materials,
            List<Rigid> rigids)
        {
            if (rigids.Count == 0) return;
            void Header(double h)
            {
                Left(L("部 材 DATA（剛域データ）", "MEMBER DATA (RIGID ZONES)", "构 件 DATA（刚性区域数据）"), 78.4, h);
                Left(L("(( 剛 域 デ ー タ ))", "(( RIGID ZONE ))", "(( 刚 性 区 域 数 据 ))"), 298.0, h + 4.3);
                double h1 = h + 17.9, h2 = h + 26.9;
                Left(L("部材", "Member", "构件"), 92.9, h1);
                Left(L("部材長", "Length", "构件长"), 134.0, h1);
                Left(L("ﾏｰｸ", "Mark", "标记"), 166.7, h1);
                Left(L("部材名", "Name", "构件名"), 198.5, h1);
                Left(L("剛域部材長(m)", "Rigid length(m)", "刚性区构件距离(m)"), 254.8, h1);
                Left(L("断 面 積", "Area", "截面面积"), 341.9, h1);
                Left(L("断面二次ﾓｰﾒﾝﾄ", "Inertia", "截面二次力矩"), 388.8, h1);
                Left("No.", 99.0, h2);
                Left("(m)", 138.5, h2);
                Left(L("I端側", "I-end", "I端侧"), 247.7, h2);
                Left(L("J端側", "J-end", "J端侧"), 286.1, h2);
                Left("A (m2)", 346.4, h2);
                Left("I (m4)", 402.8, h2);
            }
            // The name is centred between the mark and the rigid lengths.
            var rows = GroupedRows(ById(members), materials, 52.0, (member, b, name) =>
            {
                Right(member.Key, 107.1, b);
                Right(Fixed(Length(nodes, member.Value), 3), 157.2, b);
                DrawLines(name, 211.5, b, Center);
                Rigid rigid = rigids.FirstOrDefault(item => item.m == member.Key);
                if (rigid == null) return;
                Right(NonZero(rigid.Ilength, 3), 271.8, b);
                Right(NonZero(rigid.Jlength, 3), 305.6, b);
                Element zone = materials[rigid.e];
                Field(zone.A, 4, 6, 350.9 + 5 * Half, b);
                Field(zone.Iz, 6, 8, 411.8 + 7 * Half, b);
            });
            Table(27.0, Header, 44.9, rows);
        }

        // Two TYPEs side by side; a pair is kept on one page, with the heading repeated on a new page.
        private void Springs(Dictionary<string, Member> members, IDictionary<string, Element> materials)
        {
            var sheets = Section<InputFixMember>(InputFixMember.KEY)?.Sheets;
            if (sheets == null) return;
            var types = sheets.Where(sheet => sheet.Value.Count > 0).OrderBy(sheet => sheet.Key).ToList();
            if (types.Count == 0) return;
            // FrameG has one spring per member and TYPE over the whole member. Partial spring intervals
            // (FrameWeb's length), repeated members or springs on unknown members keep the generic table,
            // which prints every row.
            if (types.Any(sheet => sheet.Value.Any(row => row.length.HasValue || !members.ContainsKey(row.m)) ||
                sheet.Value.GroupBy(row => row.m).Any(rows => rows.Count() > 1)))
            {
                Legacy(InputFixMember.KEY);
                return;
            }
            // FrameG lists every member that has a spring in any TYPE.
            var springMembers = new HashSet<string>(types.SelectMany(sheet => sheet.Value.Select(row => row.m)));
            var listed = ById(members).Where(member => springMembers.Contains(member.Key)).ToList();
            const double typeGap = 27.0, firstRow = 40.3, headingToType = 8.95;
            double height = firstRow + listed.Select((member, i) => i == 0 ? 0 :
                Group(materials, member.Value) == Group(materials, listed[i - 1].Value) ? Line : GroupLine).Sum();
            double heading = Heading(27.0, headingToType + height);
            for (int pair = 0; pair < types.Count; pair += 2)
            {
                var shown = types.Skip(pair).Take(2).ToList();
                double type = pair == 0 ? heading + headingToType : y + typeGap;
                void Header(double? h, double t)
                {
                    if (h is double top) Left(L("部材バネ DATA", "MEMBER SPRING DATA", "构件弹簧 DATA"), 78.4, top);
                    double h1 = t + 13.4, h2 = t + 22.4;
                    Left(L("部材", "Member", "构件"), 97.4, h1);
                    Left(L("部材名", "Name", "构件名"), 127.2, h1);
                    Left(L("部材数", "Count", "构件数"), 167.9, h1);
                    Left("No.", 99.0, h2);
                    for (int i = 0; i < shown.Count; i++)
                    {
                        double shift = i * 128.8;
                        Left($"( TYPE {shown[i].Key,2} )", 259.9 + shift, t);
                        Left(L("部材直角方向", "Transverse", "垂直于轴的方向"), 225.7 + shift, h1);
                        Left(L("部材軸方向", "Axial", "构件轴向"), 294.1 + shift, h1);
                        Left("Kv(kN/m/m)", 230.3 + shift, h2);
                        Left("Ku(kN/m/m)", 291.2 + shift, h2);
                    }
                }
                if (pair == 0) Header(heading, type);
                if (pair > 0 && type + height > Bottom)
                {
                    NewPage();
                    pageStarted = true;
                    type = Top + headingToType;
                    Header(Top, type);
                }
                else if (pair > 0) Header(null, type);
                double baseline = type + firstRow;
                for (int i = 0; i < listed.Count; i++)
                {
                    var member = listed[i];
                    if (i > 0)
                        baseline = y + (Group(materials, member.Value) == Group(materials, listed[i - 1].Value) ? Line : GroupLine);
                    if (i > 0 && baseline > Bottom)
                    {
                        // A pair longer than a page continues with its header.
                        NewPage();
                        pageStarted = true;
                        type = Top + headingToType;
                        Header(Top, type);
                        baseline = type + firstRow;
                    }
                    Right(member.Key, 108.85, baseline);
                    Center(Material(materials, member.Value)?.name, 140.5, baseline);
                    for (int t = 0; t < shown.Count; t++)
                    {
                        FixMember spring = shown[t].Value.FirstOrDefault(row => row.m == member.Key);
                        if (spring == null) continue;
                        Right(Plain(spring.ty), 268.3 + t * 128.8, baseline);
                        Right(Plain(spring.tx), 322.6 + t * 128.8, baseline);
                    }
                    y = baseline;
                }
            }
        }

        private void NoticePoints(Dictionary<string, Vector3> nodes, Dictionary<string, Member> members, IDictionary<string, Element> materials)
        {
            const int perLine = 10;
            const double pitch = 37.27;
            void Header(double h)
            {
                Left(L("着目点 DATA", "NOTICE POINT DATA", "焦距点 DATA"), 78.4, h);
                Left(L("部材No.", "Member No.", "构件No."), 87.4, h + 17.9);
                Left(L("部材名", "Name", "构件名"), 127.2, h + 17.9);
                for (int k = 1; k <= perLine; k++)
                    Left($"L{k,2}", 180.2 + pitch * (k - 1), h + 17.9);
            }
            var points = Section<InputNoticePoints>(InputNoticePoints.KEY)?.Rows ?? new List<NoticePoint>();
            var rigids = Section<InputRigid>(InputRigid.KEY)?.rigids ?? new List<Rigid>();
            // Each line of points has a line of point names under it. The entered names are not in the saved
            // input; FrameG adds the rigid zone ends of the member as points named 剛域.
            var rows = points.Where(row => !string.IsNullOrEmpty(row.m)).Select(row =>
            {
                members.TryGetValue(row.m, out var member);
                var named = row.Points.Where(point => !double.IsNaN(point)).Select(point => (Point: point, Name: "")).ToList();
                double length = Length(nodes, member);
                foreach (Rigid rigid in rigids.Where(item => item.m == row.m && !double.IsNaN(length)))
                    foreach (double end in new[] { rigid.Ilength, length - rigid.Jlength })
                        if (end > 0 && end < length && !named.Any(point => Math.Abs(point.Point - end) < 0.0005))
                            named.Add((end, L("剛域", "Rigid", "刚性区")));
                named = named.OrderBy(point => point.Point).ToList();
                return new Row(13.5, (2 * Lines(named.Count, perLine) - 1) * Line, b =>
                {
                    Right(row.m, 105.5, b);
                    Center(Material(materials, member)?.name, 137.3, b);
                    for (int i = 0; i < named.Count; i++)
                    {
                        double right = 197.2 + pitch * (i % perLine), baseline = b + 2 * Line * (i / perLine);
                        Right(Fixed(named[i].Point, 3), right, baseline);
                        Center(named[i].Name, right - 11.3, baseline + Line);
                    }
                });
            }).ToList();
            Table(27.0, Header, 35.9, rows);
        }

        private void LoadNames(Dictionary<int, LoadName> names)
        {
            void Header(double h)
            {
                Left(L("基本荷重 DATA", "BASIC LOAD DATA", "基本载重 DATA"), 78.4, h);
                Left(L("構造系条件", "Structure", "结构条件"), 421.9, h + 13.6);
                Left(L("(非線形解析)", "(Nonlinear)", "(非线性分析)"), 493.1, h + 13.6);
                Left("Case", 89.9, h + 18.0);
                double h2 = h + 27.0;
                Left("No.", 98.9, h2);
                Left(L("割増係数", "Factor", "附加系数"), 126.5, h2);
                Left(L("実荷重", "Load", "实载重"), 164.5, h2);
                Left(L("記号", "Symbol", "符号"), 197.0, h2);
                Left(L("荷　重　名　称", "Load name", "载　重　名　称"), 247.8, h2);
                Left(L("支点", "Supp", "支点"), 407.2, h2);
                Left(L("断面", "Sect", "截面"), 427.4, h2);
                Left(L("バネ", "Spr", "弹簧"), 447.7, h2);
                Left(L("結合", "Join", "连接"), 468.1, h2);
                Left(L("部 材", "Memb", "构 件"), 496.2, h2);
                Left(L("地 盤", "Soil", "地 基"), 526.7, h2);
            }
            string Condition(int value) => value > 0 ? value.ToString(CultureInfo.InvariantCulture) : "";
            // A name runs up to the support column (FrameG's longest in the sample ends at 408.8).
            var rows = names.OrderBy(name => name.Key).Select(name => (Case: name, Lines: Wrapped(name.Value.name, 186.0))).Select(item =>
                new Row(Line, (item.Lines.Count - 1) * Line, b =>
            {
                var name = item.Case;
                string id = name.Key.ToString(CultureInfo.InvariantCulture);
                Right(id, 105.6, b);
                Right(Fixed(name.Value.rate, 4), 153.2, b);
                Right(id, 180.0, b); // FrameWeb's case is its own real load
                Left(name.Value.symbol, 197.0, b);
                DrawLines(item.Lines, 227.5, b, Left);
                Right(Condition(name.Value.fix_node), 419.0, b);
                Right(Condition(name.Value.element), 439.3, b);
                Right(Condition(name.Value.fix_member), 459.6, b);
                Right(Condition(name.Value.joint), 480.0, b);
            })).ToList();
            Table(31.3, Header, 44.9, rows);
        }

        // FrameG's member load mark, V/H (global direction) or M/S column, and the intensities as FrameG holds them.
        private static (string Mark, string Column, double P1, double P2) FrameGLoad(LoadMember load)
        {
            bool known = int.TryParse(load.mark, out int mark);
            switch (load.direction)
            {
                case "gy": return (load.mark, "V", load.P1, load.P2);
                case "gx": return (load.mark, "H", -load.P1, -load.P2); // the converter turned FrameG's H over
                case "x": return (known && (mark == 1 || mark == 2) ? (mark + 4).ToString(CultureInfo.InvariantCulture) : load.mark, "", load.P1, load.P2);
                case "y": case "": case null: return (load.mark, load.ms ?? "", load.P1, load.P2);
                // Directions FrameG has no column for (z, gz) are shown as written in the generic load table.
                default: return (load.mark, load.direction, load.P1, load.P2);
            }
        }

        private static string Serial(int number) => "(" + number.ToString(CultureInfo.InvariantCulture).PadLeft(4, '.') + ")";

        // Each case's member loads, then its nodal loads. A type's header is printed when the type changes and at
        // the top of each page; a case is kept whole when it fits on a page, and otherwise continues on the next
        // page under its type's header and case caption again.
        private void Loads(Dictionary<int, LoadName> names, Dictionary<int, Load> loads)
        {
            int memberSerial = 0, nodeSerial = 0;
            var blocks = new List<(bool Member, int Case, List<Action<double>> Rows)>();
            foreach (var load in loads.OrderBy(load => load.Key))
            {
                if (load.Value.load_member?.Length > 0)
                    blocks.Add((true, load.Key, load.Value.load_member.Select(row => (Action<double>)(baseline =>
                    {
                        var (mark, column, p1, p2) = FrameGLoad(row);
                        Right(row.m1, 169.9, baseline);
                        Right(row.m2, 193.6, baseline);
                        Right(mark, 227.5, baseline);
                        Right(double.TryParse(row.L1, NumberStyles.Float, CultureInfo.InvariantCulture, out double l1) ? Fixed(l1, 3) : row.L1, 275.1, baseline);
                        Right(Fixed(row.L2, 3), 319.3, baseline);
                        Right(NonZero(p1, 2), 369.95, baseline);
                        Right(NonZero(p2, 2), 420.8, baseline);
                        Left(column, 443.3, baseline);
                        Left(Serial(++memberSerial), 488.6, baseline);
                    })).ToList()));
                if (load.Value.load_node?.Length > 0)
                    blocks.Add((false, load.Key, load.Value.load_node.Select(row => (Action<double>)(baseline =>
                    {
                        Right(row.n, 186.7, baseline);
                        Right(NonZero(row.tx, 2), 261.4, baseline);
                        Right(NonZero(row.ty, 2), 329.2, baseline);
                        Right(NonZero(row.rz, 2), 397.0, baseline);
                        Left(Serial(++nodeSerial), 488.6, baseline);
                    })).ToList()));
            }
            if (blocks.Count == 0) return;
            const double nextLine = 18.0, continuedHeader = 96.5, memberLabel = 31.4, nodeLabel = 22.3, firstRow = 13.5;
            List<string> Caption(int caseId)
            {
                names.TryGetValue(caseId, out var name);
                return Wrapped(L("Ｃase", "Case", "Case") + " " + caseId.ToString(CultureInfo.InvariantCulture) + "  " + name?.name,
                    RuleRight - 91.9);
            }
            void TypeHeader(bool member, double start)
            {
                if (member)
                {
                    Left(L("部材荷重", "Member load", "构件载重"), 102.1, start);
                    Left(L("部材番号", "Member No.", "构件编码"), 157.0, start);
                    Left("V/H", 441.1, start);
                    double h2 = start + Line;
                    Left(L("ｽﾀｰﾄ", "Start", "开始"), 155.4, h2);
                    Left(L("ｴﾝﾄﾞ", "End", "结尾"), 182.5, h2);
                    Left(L("ﾏｰｸ", "Mark", "标记"), 220.9, h2);
                    Left("L1", 262.6, h2);
                    Left("L2", 306.6, h2);
                    Left("P1", 357.5, h2);
                    Left("P2", 408.4, h2);
                    Left("M/S", 441.1, h2);
                    Rule(h2);
                }
                else
                {
                    Left(L("節点荷重", "Nodal load", "节点载重"), 102.1, start);
                    Left(L("節点番号", "Node No.", "节点编码"), 157.0, start);
                    Left("F.X", 241.2, start);
                    Left("F.Y", 309.0, start);
                    Left("M", 379.0, start);
                    Rule(start);
                }
            }
            double LabelOffset(bool member, bool header) => header ? (member ? memberLabel : nodeLabel) : 0;
            var first = blocks[0];
            double heading = Heading(27.0, 17.8 + LabelOffset(first.Member, true) + (Caption(first.Case).Count - 1) * Line +
                firstRow + (first.Rows.Count - 1) * Line + nextLine);
            Left(L("実荷重 DATA", "LOAD DATA", "实载重 DATA"), 78.4, heading);
            bool? type = null;
            for (int index = 0; index < blocks.Count; index++)
            {
                var (member, caseId, rows) = blocks[index];
                var caption = Caption(caseId);
                double start = index == 0 ? heading + 17.8 : y + nextLine;
                bool header = type != member;
                double last = start + LabelOffset(member, header) + (caption.Count - 1) * Line + firstRow + (rows.Count - 1) * Line;
                // FrameG keeps a case whole and leaves room for the line after it.
                if (index > 0 && last + nextLine > Bottom)
                {
                    NewPage();
                    pageStarted = true;
                    start = continuedHeader;
                    header = true;
                }
                double Begin()
                {
                    if (header) TypeHeader(member, start);
                    type = member;
                    double label = start + LabelOffset(member, header);
                    DrawLines(caption, 91.9, label, Left);
                    return label + (caption.Count - 1) * Line + firstRow;
                }
                double baseline = Begin();
                foreach (var row in rows)
                {
                    if (baseline > Bottom)
                    {
                        // A case longer than a page.
                        NewPage();
                        pageStarted = true;
                        start = continuedHeader;
                        header = true;
                        baseline = Begin();
                    }
                    row(baseline);
                    y = baseline;
                    baseline += Line;
                }
            }
        }

        // Cells of a row FrameG continues on the following lines when there are more than its columns.
        private void Cells(IReadOnlyList<string> cells, int perLine, double right, double pitch, double baseline, double lineStep)
        {
            for (int i = 0; i < cells.Count; i++)
                Right(cells[i], right + pitch * (i % perLine), baseline + lineStep * (i / perLine));
        }

        private static int Lines(int count, int perLine) => Math.Max(1, (count + perLine - 1) / perLine);

        private void Defines(IReadOnlyDictionary<int, Define> defines)
        {
            const int perLine = 10;
            const double pitch = 28.1;
            void Header(double h)
            {
                Left("DEFINE DATA", 78.4, h);
                Left("Define No.", 84.5, h + 18.0);
                for (int k = 1; k <= perLine; k++)
                    Right("C" + k.ToString(CultureInfo.InvariantCulture), 163.2 + pitch * (k - 1), h + 18.0);
            }
            var rows = defines.OrderBy(define => define.Key).Select(define =>
            {
                var columns = Columns(define.Value.no).ToList();
                int width = columns.Count == 0 ? 1 : columns.Last().Key;
                return new Row(11.66, (Lines(width, perLine) - 1) * Line, b =>
                {
                    Right(define.Key.ToString(CultureInfo.InvariantCulture), 125.8, b);
                    foreach (var column in columns)
                        Right(column.Value.ToString(CultureInfo.InvariantCulture),
                            163.0 + pitch * ((column.Key - 1) % perLine), b + Line * ((column.Key - 1) / perLine));
                });
            }).ToList();
            Table(44.8, Header, 31.4, rows);
        }

        // The DEFINE numbers of a combination, with their factors on the line below.
        private void Combines(Dictionary<int, Combine> combines)
        {
            const int perLine = 8;
            const double pitch = 32.87;
            void Header(double h)
            {
                Left("COMBINE DATA", 78.4, h);
                Left("Comb", 77.5, h + 17.8);
                double h2 = h + 26.8;
                Left("No.", 82.0, h2);
                Left(L("割増係数", "Factor", "附加系数"), 102.8, h2);
                Left(L("荷 重 名 称", "Load name", "载 重 名 称"), 160.6, h2);
                for (int k = 1; k <= perLine; k++)
                    Left($"C{k,2}", 305.5 + pitch * (k - 1), h2);
            }
            var rows = combines.OrderBy(combine => combine.Key).Select(combine =>
            {
                var terms = Columns(combine.Value.coef).ToList();
                var name = Wrapped(combine.Value.name, 150.0); // before the DEFINE numbers
                return new Row(13.5, Math.Max(2 * Lines(terms.Count, perLine) - 1, name.Count - 1) * Line, b =>
                {
                    Right(combine.Key.ToString(CultureInfo.InvariantCulture), 92.0, b);
                    Right(Fixed(1, 4), 132.9, b); // FrameWeb has no factor for the whole combination
                    DrawLines(name, 153.0, b, Left);
                    Cells(terms.Select(term => term.Key.ToString(CultureInfo.InvariantCulture)).ToList(), perLine, 322.4, pitch, b, 2 * Line);
                    Cells(terms.Select(term => Fixed(term.Value, 3)).ToList(), perLine, 322.6, pitch, b + Line, 2 * Line);
                });
            }).ToList();
            Table(29.7, Header, 44.7, rows);
        }

        private void Pickups(Dictionary<int, Pickup> pickups)
        {
            const int perLine = 10;
            const double pitch = 28.48;
            void Header(double h)
            {
                Left("PickUp DATA", 78.4, h);
                Left("PickUp", 99.1, h + 18.0);
                double h2 = h + 27.0;
                Left("No.", 112.3, h2);
                Left(L("荷 重 名 称", "Load name", "载 重 名 称"), 153.0, h2);
                for (int k = 1; k <= perLine; k++)
                    Left($"C{k,2}", 295.3 + pitch * (k - 1), h2);
            }
            var rows = pickups.OrderBy(pickup => pickup.Key).Select(pickup =>
            {
                var combines = Columns(pickup.Value.com).Select(item => item.Value.ToString(CultureInfo.InvariantCulture)).ToList();
                var name = Wrapped(pickup.Value.name, 148.0); // before the COMBINE numbers
                return new Row(13.48, (Math.Max(Lines(combines.Count, perLine), name.Count) - 1) * Line, b =>
                {
                    Right(pickup.Key.ToString(CultureInfo.InvariantCulture), 125.8, b);
                    DrawLines(name, 146.2, b, Left);
                    Cells(combines, perLine, 308.7, pitch, b, Line);
                });
            }).ToList();
            Table(31.5, Header, 44.8, rows);
        }

        #endregion
    }
}

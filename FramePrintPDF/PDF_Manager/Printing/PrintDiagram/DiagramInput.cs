using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace PDF_Manager.Printing
{
    class DiagramInput : IPrintable
    {
        public const string KEY = "diagramInput";

        // 軸線を作成するのに必要な情報
        private DiagramFrame Frame = null;

        // 出力情報
        private readonly List<string> output = new List<string>
        {
            "axis", // 軸線図
            "load", // 荷重図
        };

        // 分割なしページに出力する荷重ケース番号
        private readonly List<int> single_layout_cases = new List<int>();

        // 荷重の行（FrameG の体裁は M/S の区別 ms も読む）
        private readonly JObject loadRows;

        public DiagramInput(Dictionary<string, object> value)
        {
            if (!value.ContainsKey(KEY))
                return;

            //荷重図の設定データを取得する
            var target = JObject.FromObject(value[KEY]).ToObject<Dictionary<string, object>>();
            if (value.TryGetValue(InputLoad.KEY, out object rows) && rows != null)
                loadRows = JObject.FromObject(rows);

            // 骨組の描画クラスを生成する
            Frame = new DiagramFrame(target);

            // 出力する内容を決定する
            if (target.TryGetValue("output", out object obj) && obj is IEnumerable<object> enumerable)
            {
                output.Clear();
                output.AddRange(enumerable.Select(e => e.ToString()));
            }

            // 分割なしページに出力する荷重ケース番号を取り込み
            if (target.TryGetValue("single_layout_cases", out object obj2) && obj2 is IEnumerable<object> enumerable2)
            {
                single_layout_cases.Clear();
                single_layout_cases.AddRange(enumerable2.Select(s => int.TryParse(s.ToString(), out int val) ? (int?)val : null).Where(s => s.HasValue).Select(s => s.Value));
            }
        }

        /// <summary>
        /// 荷重図の作成
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="data">入力データ</param>
        public void printPDF(PdfDocument mc, PrintData data, ref int indexPage)
        {
            Frame.printInit(mc, data);

#if true
            // @TODO: DiagramFrame.printInit()内で処理した方がいいと思うが、DiagramResult.printPDF()からもprintInit()が呼び出されるので当面はここで処理する
            nodeDic.Clear();
            foreach (var kv in Frame.Node.Nodes)
            {
                var nodeNo = kv.Key;

                nodeDic.Add(nodeNo, new XNode(nodeNo, Frame.Node, Frame.isOlderVer2));
            }

            memberDic.Clear();
            foreach (var kv in Frame.Member.members)
            {
                var memberNo = kv.Key;
                var member = kv.Value;

                memberDic.Add(memberNo, new XMember(memberNo, member, nodeDic));
            }

            // 部材を挟んだ反対側の節点を各接点に登録する
            foreach (var node in nodeDic.Values)
            {
                node.SetAnothers(memberDic.Values);
            }

            var memberList = memberDic.Values.ToList();
            var memberGroupList = new List<XMemberGroup>();
            while (memberList.Any())
            {
                memberGroupList.Add(new XMemberGroup(ref memberList, nodeDic.Values));
            }
            memberGroups = memberGroupList.AsEnumerable();
#endif

            var isAxisPrinted = false;

            // 軸線図
            if (output.Contains("axis"))
            {
                printAxis(mc, data, ref indexPage);

                isAxisPrinted = true;
            }

            // 荷重図
            if (output.Contains("load"))
            {
                if (isAxisPrinted)
                {
                    mc.NewPage(ref indexPage);
                }

                printLoad(mc, data, ref indexPage);

            }
        }

        private readonly Dictionary<string, XNode> nodeDic = new Dictionary<string, XNode>();
        private readonly Dictionary<string, XMember> memberDic = new Dictionary<string, XMember>();
        private IEnumerable<XMemberGroup> memberGroups;

        /// <summary>
        /// 軸線図の作成
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="data">入力データ</param>
        private void printAxis(PdfDocument mc, PrintData data, ref int indexPage)
        {
            Frame.SetLayout(mc, Layout.Default); // 軸線図は常に用紙分割なし

            Frame.CalculateDiagramRect(out var topLeft, out var bottomRight);
            Frame.CalculateCenterPos(topLeft, bottomRight);

            var typeNo = 1;
            dynamic sup;
            sup = Frame.FixNode.FixNodes.Count > 0 ? new DiagramSupport(Frame.FixNode.FixNodes[typeNo], nodeDic, topLeft, bottomRight) : null;
            if (sup != null) sup!.AdjustDiagramRect(ref topLeft, ref bottomRight);

            var dim = new DiagramDimension(memberGroups, Frame.CenterPos, topLeft, bottomRight, Frame.canvas.mc.font_got, Frame.canvas.mc.gfx.MeasureString);
            dim.AdjustDiagramRect(ref topLeft, ref bottomRight);

            Frame.CalculateVerticalCenterPos(topLeft, bottomRight);
            Frame.CalculateScale(topLeft, bottomRight);

            Frame.PrintTitle("軸 線 図");

            Frame.printFrame(); // 骨組図の描画
            if (sup != null) sup.Print(Frame); // 支点の描画
            dim.Print(Frame); // 寸法の描画
        }

        /// <summary>
        /// 荷重図の描画
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="data">入力データ</param>
        private void printLoad(PdfDocument mc, PrintData data, ref int indexPage)
        {
            if (data.dimension == 2 && data.referenceResultLayout)
            {
                // FrameG の体裁（原宿ボックス 0404）：上下 2 段書き、single_layout_cases は 1 頁に 1 ケース
                new FrameGLoadDiagram(mc, data).Print(loadRows ?? new JObject(),
                    (InputLoadName)data.printDatas[InputLoadName.KEY], Frame.mode != Layout.Default, single_layout_cases, ref indexPage);
                return;
            }

            Frame.SetLayout(mc);

            var loadData = (InputLoad)data.printDatas[InputLoad.KEY];
            var loadNameData = (InputLoadName)data.printDatas[InputLoadName.KEY];

            foreach (var loadMember in loadData.loads.Values.SelectMany(ld => ld.load_member ?? Enumerable.Empty<LoadMember>()).Where(lm => lm != null))
            {
                if (string.IsNullOrEmpty(loadMember.m1))
                {
                    loadMember.m1 = loadMember.m2;
                }
                if (string.IsNullOrEmpty(loadMember.m2))
                {
                    loadMember.m2 = loadMember.m1;
                }
            }

            var pc = new PageControl(mc, Frame, single_layout_cases);
            for (var loadIndex = 0; loadIndex < loadData.loads.Count(); ++loadIndex)
            {
                var fix_member = loadData.loads.Values.ElementAt(loadIndex).fix_member;
                var load_nodes = loadData.loads.Values.ElementAt(loadIndex).load_node; // 節点荷重データ
                var load_members = loadData.loads.Values.ElementAt(loadIndex).load_member; // 部材荷重データ

                pc.Begin(loadIndex, ref indexPage);

                Frame.CalculateDiagramRect(out var topLeft, out var bottomRight);
                Frame.CalculateCenterPos(topLeft, bottomRight);

                var typeNo = 1;
                dynamic sup;
                sup = Frame.FixNode.FixNodes.Count > 0 ? new DiagramSupport(Frame.FixNode.FixNodes[typeNo], nodeDic, topLeft, bottomRight) : null;
                var spr = new DiagramSpring(Frame.FixMember, fix_member, this.memberDic, Frame.CenterPos, topLeft, bottomRight);

                var memberLoad = new DiagramMemberLoad(load_members, memberGroups, this.memberDic, topLeft, bottomRight, Frame.isOlderVer2,
                    Frame.canvas.mc.font_got, Frame.canvas.mc.gfx.MeasureString);
                var nodalLoad = new DiagramNodalLoad(load_nodes, this.nodeDic, topLeft, bottomRight, Frame.isOlderVer2,
                    Frame.canvas.mc.font_got, Frame.canvas.mc.gfx.MeasureString);
                if(sup != null) sup.AdjustDiagramRect(ref topLeft, ref bottomRight);
                spr.AdjustDiagramRect(ref topLeft, ref bottomRight);
                memberLoad.AdjustDiagramRect(ref topLeft, ref bottomRight);
                nodalLoad.AdjustDiagramRect(ref topLeft, ref bottomRight);

                Frame.CalculateVerticalCenterPos(topLeft, bottomRight);
                Frame.CalculateScale(topLeft, bottomRight);

                var loadName = loadNameData.loadnames.Values.ElementAt(loadIndex);
                var Key = loadNameData.loadnames.Keys.ElementAt(loadIndex);
                Frame.PrintTitle($"荷 重 図  Case {Key}  {loadName.name}  {loadName.symbol}", pc.CurrentArea);

                Frame.printFrame(pc.CurrentArea); // 骨組図の描画
                if (sup != null) sup!.Print(Frame); // 支点の描画
                spr.Print(Frame); // バネの描画
                memberLoad.Print(Frame); // 部材荷重の描画
                nodalLoad.Print(Frame); // 節点荷重の描画

                pc.End();
            }
        }

        /// <summary>
        /// 荷重図出力時の改ページ制御
        /// </summary>
        private class PageControl
        {
            private readonly PdfDocument mc;
            private readonly DiagramFrame frame;
            private readonly List<int> single_layout_cases;
            private int seq = 0;
            private bool single_layout = false;

            public PageControl(PdfDocument mc, DiagramFrame frame, List<int> single_layout_cases)
            {
                this.mc = mc;
                this.frame = frame;
                this.single_layout_cases = single_layout_cases;
            }

            /// <summary>
            /// <paramref name="loadIndex"/>で指定された荷重データの荷重図描画を開始
            /// </summary>
            /// <param name="loadIndex">荷重データのインデックス(0～)</param>
            public void Begin(int loadIndex, ref int indexPage)
            {
                // 必要に応じて改ページ
                NewPageIfNecessary(loadIndex, ref indexPage);
            }

            /// <summary>
            /// 荷重図描画を終了
            /// </summary>
            public void End()
            {
                // do nothing
            }

            /// <summary>
            /// 荷重図を描画する紙面上の位置
            /// </summary>
            public int CurrentArea => seq - 1;

            /// <summary>
            /// 必要に応じて改ページ
            /// </summary>
            /// <param name="loadIndex">荷重データのインデックス(0～)</param>
            private void NewPageIfNecessary(int loadIndex, ref int indexPage)
            {
                ++seq;

                if (single_layout_cases.Contains(loadIndex))
                {
                    // 分割なしページに出力する場合

                    if (seq > 1)
                    {
                        // 直前のページに何かを出力済みの場合は改ページ
                        mc.NewPage(ref indexPage);

                        // 直前が分割なしページではなかった場合はレイアウトを変更
                        if (!single_layout)
                        {
                            single_layout = true;
                            frame.SetLayout(mc, Layout.Default);
                        }
                        seq = 1;
                    }
                }
                else if (single_layout)
                {
                    // 直前が分割なしページへの出力で、今回が分割されたページへの出力の場合

                    // 改ページ
                    mc.NewPage(ref indexPage);

                    // レイアウト変更
                    single_layout = false;
                    frame.SetLayout(mc);
                    seq = 1;
                }
                else
                {
                    // 直前も今回も分割されたページへの出力の場合

                    var casesInPage = frame.mode switch
                    {
                        Layout.SplitHorizontal => 2,
                        Layout.SplitVertical => 2,
                        Layout.SplitHorizontal3 => 3,
                        Layout.SplitVertical3 => 3,
                        Layout.SplitHorizontal4 => 4,
                        Layout.SplitVertical4 => 4,
                        _ => 1,
                    };
                    if (seq > casesInPage)
                    {
                        // ページが埋まっていたら改ページ
                        mc.NewPage(ref indexPage);

                        seq = 1;
                    }
                }
            }
        }

        /*
        /// <summary>
        /// 節点の印字
        /// </summary>
        private void printNode()
        {
            XPoint p = new XPoint(200,300);
            XSize z = new XSize(10, 10);

            Shape.Drawcircle(this.mc, p, z);
        }

        */
    }
}

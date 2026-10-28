using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Convert_Manager.FrameWebForJS
{
    public class LoadMember
    {
        public int row;

        public string m1;
        public string m2;
        public string direction;
        public string mark;
        public string L1;
        public double L2;
        public double P1;
        public double P2;
        // FrameG の M/S：M は曲げ用、S はせん断用の荷重。空なら両方に使う。
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string ms;

        public LoadMember Clone()
        {
            return (LoadMember)MemberwiseClone();
        }

        public bool Enable()
        {
            var val1 = m1.Trim().Length
                     + m2.Trim().Length
                     + L1.Trim().Length;
            var val2 = (L2 != 0);
            var val3 = (P1 != 0);
            var val4 = (P2 != 0);

            if (0 < val1 || val2 || val3 || val4 )
            {
                return true;
            }
            return false;
        }
    }

    public class LoadNode
    {
        public int row;

        public string n;
        public double tx;
        public double ty;
        //public double tz;
        //public double rx;
        //public double ry;
        public double rz;

        public LoadNode Clone()
        {
            return (LoadNode)MemberwiseClone();
        }

        public bool Enable()
        {
            var val1 = n.Trim().Length;
            var val2 = (tx != 0);
            var val3 = (ty != 0);
            var val4 = (rz != 0);
            if (0 < val1 || val2 || val3 || val4)
            {
                return true;
            }
            return false;
        }
    }

    public class Load
    {
        public int fix_node;
        public int fix_member;
        public int element;
        public int joint;
        public string symbol;
        public double LL_pitch;
        public double rate;
        public string name;
        public LoadNode[] load_node = new LoadNode[0];
        public LoadMember[] load_member = new LoadMember[0];

        [NonSerialized]
        public int inputCaseNo; // 実荷重番号
    }


    public class load
    {
        public const string KEY = "load";
        private const string wFile = "K_JituBuzai.tmp";     // 実荷重強度
        private const string wFile1 = "Kajyu.tmp";          // 荷重名称
        private const string wFile2 = "Ku_MixName.tmp";     // 合成荷重
        private const string wFile3 = "Ku_Mix.tmp";         // 

        private Dictionary<string, Load> LoadList = new Dictionary<string, Load>();

        public string message = "";

        public load(Dictionary<string, string> wdata)
        {
            var tmpLoadList = new Dictionary<string, Load>();

            /// 荷重名称の読み込み
            readKajyu(wdata, tmpLoadList);

            /// 荷重強度の読み込み
            readK_JituBuzai(wdata, tmpLoadList);

            /// 合成荷重の読み込み
            readMix(wdata, tmpLoadList);

            /// FrameG -> FrameWebforJS に変換
            this.exchangeData(tmpLoadList);
        }


        /// <summary>
        /// 合成荷重の読み込み
        /// </summary>
        /// <param name="wdata"></param>
        /// <param name="tmpLoadList"></param>
        private void readMix(Dictionary<string, string> wdata, Dictionary<string, Load> tmpLoadList)
        {
            /// 番号
            if (!wdata.ContainsKey(load.wFile2))
                return;
            if (!wdata.ContainsKey(load.wFile3))
                return;

            var str2 = wdata[load.wFile2];
            var str3 = wdata[load.wFile3];


            while (str2.Length > 9)
            {
                int index = tmpLoadList.Count + 1;

                var lod = new Load();
                var lmLlist = new List<LoadMember>();
                var lnLlist = new List<LoadNode>();

                // 全体の割り増し係数
                var tmp = comon.byteSubstr(ref str3, 10).Trim();
                lod.rate = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 1;

                // 荷重名称
                lod.symbol = comon.byteSubstr(ref str2, 5);
                lod.name = comon.byteSubstr(ref str2, 46).Trim();

                // 構造系条件
                tmp = comon.byteSubstr(ref str2, 3).Trim();
                lod.fix_node = (0 < tmp.Length) ? Convert.ToInt32(tmp) : 1;

                tmp = comon.byteSubstr(ref str2, 3).Trim();
                lod.element = (0 < tmp.Length) ? Convert.ToInt32(tmp) : 1;

                tmp = comon.byteSubstr(ref str2, 3).Trim();
                lod.fix_member = (0 < tmp.Length) ? Convert.ToInt32(tmp) : 1;

                tmp = comon.byteSubstr(ref str2, 3).Trim();
                lod.joint = (0 < tmp.Length) ? Convert.ToInt32(tmp) : 1;

                // 合成荷重係数
                var tmp1 = comon.byteSubstr(ref str3, 240);
                var tmp2 = comon.byteSubstr(ref str3, 120);
                for (int i = 0; i < 30; i++)
                {
                    var tmp3 = comon.byteSubstr(ref tmp1, 8).Trim();
                    var tmp4 = comon.byteSubstr(ref tmp2, 4).Trim();
                    if (0 < tmp4.Length)
                    {
                        // 基本ケースから荷重おｗコピーする
                        if (!tmpLoadList.ContainsKey(tmp4))
                            continue;

                        var coef = Convert.ToDouble(tmp3);

                        if (coef == 0)
                            continue;

                        var l = tmpLoadList[tmp4];
                        var mlo = l.load_member.Select(x => x.Clone()).ToArray();
                        var nlo = l.load_node.Select(x => x.Clone()).ToArray();

                        foreach(var ml in mlo)
                        {
                            ml.P1 *= coef;
                            ml.P2 *= coef;
                            lmLlist.Add(ml);
                        }

                        foreach (var nl in nlo)
                        {
                            nl.tx *= coef;
                            nl.ty *= coef;
                            nl.rz *= coef;
                            lnLlist.Add(nl);
                        }
                    }
                }

                lod.load_member = lmLlist.ToArray();
                lod.load_node = lnLlist.ToArray();

                if (lod.load_member.Length + lod.load_node.Length > 0)
                {
                    tmpLoadList.Add(index.ToString(), lod);
                }
            }

        }

        
        /// <summary>
        /// nNo 以上の節点番号を+1する
        /// </summary>
        /// <param name="nNo"></param>
        internal void addNewNode(string nNo)
        {
            int iNo = Convert.ToInt32(nNo);
            foreach (var lo1 in this.LoadList)
            {
                foreach (var lo2 in lo1.Value.load_node)
                {
                    int k = Convert.ToInt32(lo2.n);
                    if (iNo <= k)
                    {
                        lo2.n = (k + 1).ToString();
                    }

                }

            }
        }


        /// <summary>
        /// 分割した部材に合わせて着目点を修正する
        /// </summary>
        /// <param name="newMember"></param>
        internal void addNewMember(Dictionary<string, Member> newMember, node _node, member _member)
        {
            // 分割前の要素
            var k1 = newMember.First();
            var i1 = Convert.ToInt32(k1.Key);
            var len1 = k1.Value.Length(_node);

            // 分割後の要素
            var k2 = newMember.Last();
            var i2 = Convert.ToInt32(k2.Key);
            var len2 = k2.Value.Length(_node);


            // 分割した部材に合わせて着目点を修正する
            var temp1 = new Dictionary<string, Load>();
            foreach (var lo1 in this.LoadList)
            {
                // 要素荷重以外を複製
                var temp2 = new Load()
                {
                    fix_node = lo1.Value.fix_node,
                    fix_member = lo1.Value.fix_member,
                    element    = lo1.Value.element,
                    joint      = lo1.Value.joint,     
                    symbol      = lo1.Value.symbol,
                    LL_pitch    = lo1.Value.LL_pitch,
                    rate        = lo1.Value.rate,   
                    name        = lo1.Value.name,    
                    load_node   = lo1.Value.load_node,
                    inputCaseNo = lo1.Value.inputCaseNo
                };

                var load_member = new List<LoadMember>();

                foreach (var lo2 in lo1.Value.load_member)
                {
                    int m1 = Convert.ToInt32(lo2.m1);
                    int m2 = 0;
                    int.TryParse(lo2.m2, out m2);
                    int a2 = Math.Abs(m2);
                    int c2 = Math.Sign(m2);

                    if (i1 < m1 && i1 < a2)
                    {   // 分割前の要素より大きい部材番号の場合
                        lo2.m1 = (m1 + 1).ToString();
                        lo2.m2 = (c2 * (a2 + 1)).ToString();
                    }
                    else if (i1 <= a2)
                    {   // 末尾の部材だけ分割前の要素より大きい部材番号の場合
                        lo2.m2 = (c2 * (a2 + 1)).ToString();
                    }
                    else if (i1 == m1)
                    {   // 先頭の部材が分割対象部材と一致している場合、末尾に要素を付ける
                        lo2.m2 = (-1 * (m1 + 1)).ToString();
                    }
                    else if (i1 < m1)
                    {
                        lo2.m1 = (m1 + 1).ToString();
                    }

                   load_member.Add(lo2);
                }

                temp2.load_member = load_member.ToArray();
                temp1.Add(lo1.Key, temp2);
            }
            this.LoadList = temp1;
        }




        /// <summary>
        /// 荷重強度の読み込み
        /// </summary>
        private void readK_JituBuzai(Dictionary<string, string> wdata, Dictionary<string, Load> tmpLoadList)
        {
            if (!wdata.ContainsKey(load.wFile))
                return;

            var str = wdata[load.wFile];

            // 1行の抽出
            var lst1 = new List<string>();
            while (str.Length > 0)
            {
                string line = comon.byteSubstr(ref str, 124);
                lst1.Add(line);
            }

            for (var i = 0; i < lst1.Count; i++)
            {
                str = lst1[i];

                /// 部材
                string tmp = comon.byteSubstr(ref str, 3).Trim();
                var CaseNo = (0 < tmp.Length) ? Convert.ToInt32(tmp) : -1;

                // 要素荷重
                var fm = new LoadMember();

                tmp = comon.byteSubstr(ref str, 3);
                fm.m1 = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 3);
                fm.m2 = gDecodeMinusIntegerStr(tmp.Trim());

                tmp = comon.byteSubstr(ref str, 12);
                fm.mark = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 12);
                fm.L1 = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 12).Trim();
                fm.L2 = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 12).Trim();
                fm.P1 = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 12).Trim();
                fm.P2 = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 12);
                fm.direction = tmp.Trim(); // S V M H

                // 節点荷重
                var fn = new LoadNode();

                tmp = comon.byteSubstr(ref str, 3);
                fn.n = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 12).Trim();
                fn.tx = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 11).Trim();
                fn.ty = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 12).Trim();
                fn.rz = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                /// 登録（同じ荷重が複数のケースに登録されることがある）
                var keys = new List<string>();
                foreach (var a in tmpLoadList)
                {
                    if (a.Value.inputCaseNo == CaseNo)
                    {
                        keys.Add(a.Key);
                    }
                }

                foreach (var key in keys)
                {
                    var ll = tmpLoadList[key];

                    var ln = new List<LoadNode>(tmpLoadList[key].load_node);
                    var lm = new List<LoadMember>(tmpLoadList[key].load_member);

                    // 要素荷重
                    if (fm.Enable())
                        lm.Add(fm.Clone());

                    // 節点荷重
                    if (fn.Enable())
                        ln.Add(fn.Clone());

                    // 登録
                    ll.load_node = ln.ToArray();
                    ll.load_member = lm.ToArray();
                }
            }
        }



        /// <summary>
        /// 荷重名称の読み込み
        /// </summary>
        /// <param name="wdata"></param>
        /// <param name="tmpLoadList"></param>
        private void readKajyu(Dictionary<string, string> wdata, Dictionary<string, Load> tmpLoadList)
        {
            if (!wdata.ContainsKey(load.wFile1))
                return;

            var str = wdata[load.wFile1];

            // 1行の抽出
            var lst1 = new List<string>();
            while (str.Length > 0)
            {
                string line = comon.byteSubstr(ref str, 88);
                lst1.Add(line);
            }

            for (var i = 0; i < lst1.Count; i++)
            {
                str = lst1[i];

                /// 部材
                var lo = new Load();

                string tmp = comon.byteSubstr(ref str, 10).Trim();
                lo.rate = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 1;

                tmp = comon.byteSubstr(ref str, 4).Trim();
                lo.inputCaseNo = (0 < tmp.Length) ? Convert.ToInt32(tmp) : -1;

                tmp = comon.byteSubstr(ref str, 5);
                lo.symbol = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 41);
                lo.name = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 3).Trim();
                if (!int.TryParse(tmp, out lo.fix_node))
                    lo.fix_node = 1;

                tmp = comon.byteSubstr(ref str, 3).Trim();
                if (!int.TryParse(tmp, out lo.element))
                    lo.element = 1;

                tmp = comon.byteSubstr(ref str, 3).Trim();
                if (!int.TryParse(tmp, out lo.fix_member))
                    lo.fix_member = 1;

                tmp = comon.byteSubstr(ref str, 3).Trim();
                if (!int.TryParse(tmp, out lo.joint))
                    lo.joint = 1;

                tmpLoadList.Add((i + 1).ToString(), lo);
            }
        }

        /// <summary>
        /// FrameG -> FrameWebforJS に変換
        /// </summary>
        /// <param name="tmpLoadList"></param>
        private void exchangeData(Dictionary<string, Load> tmpLoadList)
        {

            LoadList = tmpLoadList;

            // row に入力する
            foreach(var a in LoadList)
            {
                // 要素荷重の変換
                int i = 1;
                foreach (var c in a.Value.load_member)
                {
                    switch (c.mark)
                    {
                        case "1":
                        case "2":
                            // y 下向きに鏡に映すと、局所 y と鉛直（V）の符号はそのまま。水平（H）は x なので従来どおり反転する。
                            if (c.direction == "V")
                                c.direction = "gy";
                            else if (c.direction == "H")
                            {
                                c.direction = "gx";
                                c.P1 *= -1;
                                c.P2 *= -1;
                            }
                            else
                            {
                                // V/H と同じ欄の M・S は、局所 y の荷重を曲げ用とせん断用に分ける印。
                                if (c.direction == "M" || c.direction == "S")
                                    c.ms = c.direction;
                                c.direction = "y";
                            }
                            break;

                        case "5":
                            if (c.direction == "V")
                                c.direction = "gy";
                            else if (c.direction == "H")
                                c.direction = "gx";
                            else
                                c.direction = "x";
                            c.mark = "1";
                            break;

                        case "6":
                            if (c.direction == "V")
                                c.direction = "gy";
                            else if (c.direction == "H")
                                c.direction = "gx";
                            else
                                c.direction = "x";
                            c.mark = "2";
                            break;

                        case "11":
                            c.direction = "z";
                            break;

                        case "9":
                            c.direction = "";
                            break;

                        default:
                            message = "対応していない荷重の種類が入力されています";
                            break;
                    }
                    c.row = i;
                    i++;
                }

                // 節点荷重の変換
                i = 1;
                foreach (var c in a.Value.load_node)
                {
                    // FrameG の y 上向きの座標を鏡に映した FrameWeb（y 下向き）では、鉛直力と曲げの符号はそのまま。
                    c.row = i;
                    i++;
                }
            }
        }


        /// <summary>
        /// 英数字で表現された文字列をマイナス整数の文字列に変換する
        /// 実荷重の固定長文字列”st部材番号(n)”で桁数が足りなくなったために用意した
        /// </summary>
        /// <param name="rsValStr"></param>
        /// <returns></returns>
        private string gDecodeMinusIntegerStr(string rsValStr) 
        {
            string result = "";

            string SVAL = rsValStr.Trim();

            if (SVAL.Length == 0)
                return SVAL;

            char sTmp = SVAL[0];

            if (!char.IsWhiteSpace(sTmp) && char.IsLetter(sTmp)){
                var a1 = (int)sTmp;
                var a2 = (int)'A';
                var a3 = (int)'1';
                var a = a1 - a2 + a3;
                result = "-";
                result += Microsoft.VisualBasic.Strings.Chr(a);
                result += SVAL.Substring(1);
            }
            else {
                result = SVAL;
            }

            return result;
        }


        public Dictionary<string, Load> GetLoad()
        {
            return LoadList;
        }

    }
}

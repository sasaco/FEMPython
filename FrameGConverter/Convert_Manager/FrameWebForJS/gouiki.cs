using System;
using System.Collections.Generic;
using System.Text;

namespace Convert_Manager.FrameWebForJS
{
    public class Gouiki
    {
        public double iDistance = 0;
        public double jDistance = 0;
        public double A = 0;
        public double I = 0;
    }

    public class gouiki
    {
        private const string wFile = "Buzai_G.tmp";

        private Dictionary<string, Gouiki> GouikiList = new Dictionary<string, Gouiki>();

        public string message = "";

        public gouiki(Dictionary<string, string> wdata)
        {
            if (!wdata.ContainsKey(gouiki.wFile))
                return;

            var str = wdata[gouiki.wFile];

            // 1行の抽出
            var lst1 = new List<string>();
            while (str.Length > 0)
            {
                string line = comon.byteSubstr(ref str, 56);
                lst1.Add(line);
            }

            int i = 1;
            foreach (var line in lst1)
            {
                str = line;

                /// 部材
                var g = new Gouiki();

                string tmp = comon.byteSubstr(ref str, 14).Trim();
                g.jDistance = (0 < tmp.Length) ? Convert.ToDouble(tmp): 0;

                tmp = comon.byteSubstr(ref str, 14).Trim();
                g.iDistance = (0 < tmp.Length) ? Convert.ToDouble(tmp): 0;

                tmp = comon.byteSubstr(ref str, 14).Trim();
                g.A = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 14).Trim();
                g.I = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                GouikiList.Add(i.ToString(), g);
                i++;
            }

        }

        /// <summary>
        /// 剛域で部材を分割する
        /// </summary>
        internal void exChange( node _node,
                                member _member, 
                                fix_node _fix_node, 
                                fix_member _fix_member, 
                                joint _joint, 
                                notice_points _notice_point, 
                                load _load)
        {
            var InputData = new Dictionary<string, object>()
            {
                { "node",  _node },
                { "member", _member },
                { "fix_node", _fix_node },
                { "fix_member", _fix_member },
                { "joint", _joint },
                { "notice_points", _notice_point },
                { "load", _load }
            };

            bool flg = true;
            while (flg)
            {
                flg = false;
                foreach (var line in GouikiList)
                {
                    var g = line.Value;
                    if (g.iDistance != 0 || g.jDistance != 0)
                    {
                        // 剛域で部材を分割します
                        flg = this.SepalateMember(line, InputData);
                        break;
                    }
                }
            }

        }

        /// <summary>
        /// 剛域で部材を分割します
        /// </summary>
        /// <param name="inputData"></param>
        private Boolean SepalateMember(KeyValuePair<string, Gouiki> g,
                                    Dictionary<string, object> inputData)
        {
            bool result = false;

            node _node = (node)inputData["node"];
            member _member = (member)inputData["member"];

            // ターゲットの部材番号
            string mNo = g.Key;

            Member m = _member.getMember(mNo);
            double mLength = m.Length(_node);

            double[] Distance = new double[] { g.Value.iDistance, g.Value.jDistance };
            for (var i = 0; i < Distance.Length; i++)
            {
                if (Distance[i] <= 0)
                    continue;

                string eNo = _member.GetElementNo(m.e, g.Value.A, g.Value.I);
                if (mLength <= Distance[i])
                { // もし、部材長さより剛域長さの方が長かったら諸元の変更だけにする
                    m.e = eNo;
                    if (i == 0) 
                        g.Value.iDistance = 0; 
                    else 
                        g.Value.jDistance = 0;
                    result = true;
                    break;
                }

                if(i == 0)
                {   // i端から
                    g.Value.iDistance = 0;
                    this.Split(mNo, Distance[i], eNo, m.e, inputData, i);
                }
                else
                {   // j端から
                    g.Value.jDistance = 0;
                    this.Split(mNo, mLength - Distance[i], m.e, eNo, inputData, i);
                }
                // Re-fetch member IDs and geometry after each split before handling the other end.
                return true;
            }
            return result;
        }

        /// <summary>
        /// メンバーをi端から Distanceの位置で分割する
        /// </summary>
        /// <param name="old_mNo">分割する対象要素番号</param>
        /// <param name="Distance">i端からの距離</param>
        /// <param name="i_eNo">i端側の材料番号</param>
        /// <param name="j_eNo">j端側の材料番号</param>
        private void Split( string old_mNo, double Distance,
                            string i_eNo, string j_eNo,
                            Dictionary<string, object> inputData,
                            int side)
        {
            node _node = (node)inputData["node"];
            member _member = (member)inputData["member"];
            fix_node _fix_node = (fix_node)inputData["fix_node"];
            fix_member _fix_member = (fix_member)inputData["fix_member"];
            joint _joint = (joint)inputData["joint"];
            notice_points _notice_point = (notice_points)inputData["notice_points"];
            load _load = (load)inputData["load"];

            // ターゲットの部材情報
            Member old_m = _member.getMember(old_mNo);
            string old_i = old_m.ni;
            string old_j = old_m.nj;   // 分割前の j端 の節点番号

            // 新しい節点番号を追加する
            var newNode = _node.addNewNode(old_i, old_j, Distance, _fix_member.HasIntervals(old_mNo));

            // 支点を新しい節点番号に置き換える
            _fix_node.addNewNode(newNode.Key);

            // 節点荷重を新しい節点番号に置き換える
            _load.addNewNode(newNode.Key);



            // 部材を二分割する
            var newMember = _member.addNewMember(old_mNo, newNode.Key, i_eNo, j_eNo);

            // 剛域の部材番号を二分割された部材に置き換える
            int old_iNo = Convert.ToInt32(old_mNo);
            var temp = new Dictionary<string, Gouiki>();
            foreach (var g in this.GouikiList)
            {
                var k = Convert.ToInt32(g.Key);
                if (old_iNo <= k)
                {
                    if (g.Key == old_mNo)
                    {   // 二分割された部材
                        temp.Add(g.Key, new Gouiki()
                        {
                            iDistance = g.Value.iDistance,
                            jDistance = 0,
                            A = g.Value.A,
                            I = g.Value.I
                        });
                        if (side == 0)
                        {

                        }
                        else
                        {

                        }
                    }
                    k += 1;
                }
                temp.Add(k.ToString(), g.Value);
            }
            this.GouikiList = temp;

            // バネを二分割された部材に置き換える
            _fix_member.addNewMember(old_mNo, Distance);

            // 着目点を二分割された部材に置き換える
            _notice_point.addNewMember(newMember, _node, _member);

            // 結合条件を二分割された部材に置き換える
            _joint.addNewMember(old_mNo);

            // 部材荷重を二分割された部材に置き換える
            _load.addNewMember(newMember, _node, _member);

        }

    }
}

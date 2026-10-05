using System;
using System.Collections.Generic;
using System.Text;

namespace Convert_Manager.FrameWebForJS
{
    public class Member
    {
        public string ni; // 節点番号
        public string nj;
        public string e;  // 材料番号
        //public double cg; // コードアングル

        // 部材の長さを計算する
        public double Length(node _node)
        {
            var vi = _node.GetNode(this.ni);
            var vj = _node.GetNode(this.nj);

            if (vi == null || vj == null)
                return 0;

            return vi.Distance(vj);

        }
    }


    public class member : element
    {
        public const string mKEY = "member";
        private const string wFile = "Buzai.tmp";

        private Dictionary<string, Member> MemberList = new Dictionary<string, Member>();
        internal HashSet<string> PileMembers { get; } = new HashSet<string>();

        public string message = "";
        public member(Dictionary<string, string> wdata)
        {
            if (!wdata.ContainsKey(member.wFile))
                return;

            var str = wdata[member.wFile];

            // 1行の抽出
            var lst1 = new List<string>();
            while (str.Length > 0)
            {
                string line = comon.byteSubstr(ref str, 244);
                lst1.Add(line);
            }

            var ee = new List<object[]>();

            foreach (var line in lst1)
            {
                str = line;

                /// 部材
                var m = new Member();

                string tmp = comon.byteSubstr(ref str, 3);
                string No = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 10).Trim();
                m.ni = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 10).Trim();
                m.nj = tmp.Trim();

                m.e = No;

                // 部材集計
                if (!MemberList.ContainsKey(No))
                    if(0 < m.ni.Length + m.nj.Length)
                        this.MemberList.Add(No, m);

                /// 材料
                tmp = comon.byteSubstr(ref str, 32);
                var name = tmp.Trim();

                tmp = comon.byteSubstr(ref str, 10).Trim();
                var n = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 1;

                tmp = comon.byteSubstr(ref str, 10).Trim();
                var E = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 10).Trim();
                var Xp = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;

                tmp = comon.byteSubstr(ref str, 5);
                string mark = tmp.Trim();

                if (name.Length == 0)
                    name = mark;

                double[] A = new double[6];
                for (var j = 0; j < 6; j++)
                {
                    tmp = comon.byteSubstr(ref str, 11).Trim();
                    A[j] = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;
                }

                double[] Iz = new double[6];
                for (var j = 0; j < 6; j++)
                {
                    tmp = comon.byteSubstr(ref str, 13).Trim();
                    Iz[j] = (0 < tmp.Length) ? Convert.ToDouble(tmp) : 0;
                }

                ee.Add(new object[8] { No, mark, name, n, E, Xp, A, Iz });

                if (name.Contains('杭'))
                    PileMembers.Add(No);
            }

            // 材料集計
            base.setElementList(ee);


        }

        public Dictionary<string, Member> GetMember()
        {
            return MemberList;
        }

        public Member getMember(string No)
        {
            if (!MemberList.ContainsKey(No))
                return null;
            return MemberList[No];
        }

        /// <summary>
        /// old_mNo部材を 節点 new_nNo で２分割して i端側の材料を i_eNo とし j端側の材料を j_eNo とする
        /// 以降の部材番号をずらす
        /// </summary>
        /// <param name="old_mNo"></param>
        /// <param name="key"></param>
        /// <param name="i_eNo"></param>
        /// <param name="j_eNo"></param>
        /// <returns></returns>
        internal Dictionary<string, Member> addNewMember(string old_mNo, string new_nNo, string i_eNo, string j_eNo)
        {
            var result = new Dictionary<string, Member>();

            int old_iNo = Convert.ToInt32(old_mNo);
            int new_iNo = Convert.ToInt32(new_nNo);
            var flg = false;

            var temp = new Dictionary<string, Member>();
            foreach (var m in this.MemberList)
            {
                var me = m.Value;
                int ini = Convert.ToInt32(me.ni);
                int inj = Convert.ToInt32(me.nj);
                if (new_iNo <= ini)
                    me.ni = (ini + 1).ToString();
                if (new_iNo <= inj)
                    me.nj = (inj + 1).ToString();

                var k = Convert.ToInt32(m.Key);
                if (old_iNo <= k)
                {
                    k += 1;
                    if (flg == false)
                    {   // ２分割
                        var iM = new Member() { ni = me.ni, nj = new_nNo, e = i_eNo };
                        var jM = new Member() { ni = new_nNo, nj = me.nj, e = j_eNo };
                        temp.Add(old_mNo, iM);
                        result.Add(old_mNo, iM);
                        result.Add(k.ToString(), jM);
                        me = jM;
                        flg = true;
                    }
                }
                temp.Add(k.ToString(), me);
            }

            this.MemberList = temp;

            return result;
        }
    }

}

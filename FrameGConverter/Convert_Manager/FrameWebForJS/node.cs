using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Convert_Manager.FrameWebForJS
{
    public class Vector3
    {
        public double x;
        public double y;
        //public double z;

        /// <summary>
        /// 2点間の距離を計算する
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public double Distance(Vector3 target)
        {
            return Math.Sqrt(Math.Pow(this.x - target.x, 2) 
                + Math.Pow(this.y - target.y, 2));
        }
    }

    public class node
    {
        public const string KEY = "node";
        private const string wFile = "$1.txt";

        private Dictionary<string, Vector3>  NodeList = new Dictionary<string, Vector3>();

        public node(Dictionary<string, string> wdata)
        {
            if (!wdata.ContainsKey(node.wFile))
                return;

            var str = wdata[node.wFile];
            string[] del = { "\r\n" };
            string[] arr = str.Split(del, StringSplitOptions.None);
            for (int i = 1; i < arr.Length; i++)
            {
                var columns = arr[i].Split("\t");

                if (columns.Length < 3)
                    continue;

                var pos = new Vector3();
                pos.x = Convert.ToDouble(columns[1]);
                pos.y = Convert.ToDouble(columns[2]);

                NodeList.Add(columns[0], pos);
            }
        }

        public  Dictionary<string, Vector3> GetNode()
        {
            return NodeList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="No"></param>
        /// <returns></returns>
        public Vector3 GetNode(string No)
        {
            if (!NodeList.ContainsKey(No))
                return null;

            return NodeList[No];
        }

        /// <summary>
        /// ni と nj の中間点（niからの距離 distance）を追加して全ての点の番号をずらす
        /// </summary>
        /// <param name="ni"></param>
        /// <param name="nj"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        internal KeyValuePair<string, Vector3> addNewNode(string ni, string nj, double distance, bool preservePrecision = false)
        {
            var a = this.GetNode(ni);
            var b = this.GetNode(nj);
            var length = a.Distance(b);
            var m = distance;
            var n = length - distance;
            // 新しい節点座標
            var newNode = new Vector3() { 
                x = (n * a.x + m * b.x) / length,
                y = (n * a.y + m * b.y) / length
            };
            if (!preservePrecision)
            {
                newNode.x = Math.Round(newNode.x, 3);
                newNode.y = Math.Round(newNode.y, 3);
            }
            int i_Key = Convert.ToInt32(ni) + 1; // 新しい節点番号
            string n_Key = i_Key.ToString(); 


            // 全ての点の番号をずらす
            var temp = new Dictionary<string, Vector3>();
            var flg = false;    // もう登録したよフラグ
            foreach (var nd in this.NodeList)
            {
                var k = Convert.ToInt32(nd.Key);
                if (i_Key <= k)
                {
                    if (flg == false)
                    {
                        temp.Add(n_Key, newNode);
                        flg = true;
                    }
                    k += 1;
                }
                temp.Add(k.ToString(), nd.Value);
            }

            this.NodeList = temp;

            return new KeyValuePair<string, Vector3>(
                n_Key,
                newNode
            );
        }
    }
}

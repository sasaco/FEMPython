using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Convert_Manager.FrameWebForJS
{
    public class Element
    {
        public double E;
        // public double G;
        public double Xp;
        public double A;
        // public double J;
        //public double Iy;
        public double Iz;
        public string name;

        // FrameWebには無い
        [NonSerialized]
        public string mark;
        [NonSerialized]
        public double n; // 部材数
    }

    public class element
    {
        public const string eKEY = "element";

        protected Dictionary<string, Dictionary<string, Element>> ElementList = new Dictionary<string, Dictionary<string, Element>>();

        public Dictionary<string, Dictionary<string, Element>> GetElement()
        {
            return ElementList;
        }


        public void setElementList(List<object[]> ee)
        {
            // 材料集計
            for (var j = 0; j < 6; j++)
            {
                var eee = new Dictionary<string, Element>();
                foreach (var val in ee)
                {
                    var e = new Element();

                    var No = (string)val[0];
                    e.mark = (string)val[1];
                    e.name = (string)val[2];
                    var n = (double)val[3];
                    e.E = (double)val[4];
                    e.Xp = (double)val[5];
                    var A = (double[])val[6];
                    var Iz = (double[])val[7];

                    if (A[j] != 0)
                    {
                        e.A = A[j] * n;
                    }
                    if (Iz[j] != 0)
                    {
                        e.Iz = Iz[j] * n;
                    }
                    // 有効判定
                    if (e.E != 0 && e.A != 0 && e.Iz != 0)
                    {
                        eee.Add(No, e);
                    }

                }

                if (0 < eee.Count)
                    this.ElementList.Add((j + 1).ToString(), eee);
            }

        }

        /// <summary>
        /// 同じ数値の諸元を探して 材料番号を返す.
        /// 無ければ新しい諸元を生成して 新No を返す.
        /// </summary>
        /// <returns></returns>
        internal string GetElementNo(string eNo, double A, double Iz)
        {
            if (ElementList.Count == 0)
                throw new InvalidDataException($"Rigid-zone material {eNo} does not exist.");
            foreach (var sheet in ElementList)
                if (!sheet.Value.ContainsKey(eNo))
                    throw new InvalidDataException($"Rigid-zone source material {eNo} does not exist in TYPE {sheet.Key}.");

            // A shared material number must match in every TYPE.
            foreach (var candidate in ElementList.First().Value)
                if (ElementList.Values.All(sheet => sheet.TryGetValue(candidate.Key, out var value) &&
                    value.E == sheet[eNo].E && value.Xp == sheet[eNo].Xp && value.A == A && value.Iz == Iz))
                    return candidate.Key;

            int maxNo = ElementList.Values.SelectMany(sheet => sheet.Keys)
                .Max(key => int.Parse(key, CultureInfo.InvariantCulture));
            string newNo = (maxNo + 1).ToString(CultureInfo.InvariantCulture);
            foreach (var sheet in ElementList.Values)
            {
                var source = sheet[eNo];
                sheet.Add(newNo, new Element
                {
                    E = source.E,
                    A = A,
                    Iz = Iz,
                    Xp = source.Xp,
                    name = "剛域"
                });
            }
            return newNo;
        }
    }
}

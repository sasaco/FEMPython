using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Convert_Manager.FrameWebForJS
{
    public class Gouiki
    {
        public double iDistance;
        public double jDistance;
        public double A;
        public double I;
    }

    public class Rigid
    {
        public string m;
        public double Ilength;
        public double Jlength;
        public int e;
    }

    public class gouiki
    {
        public const string KEY = "rigid";
        private const string wFile = "Buzai_G.tmp";
        private const double RelativeLengthTolerance = 1e-12;
        private readonly Dictionary<string, Gouiki> GouikiList = new Dictionary<string, Gouiki>();

        public string message = "";

        public gouiki(Dictionary<string, string> wdata)
        {
            if (!wdata.TryGetValue(wFile, out string str))
                return;

            int memberId = 1;
            while (str.Length > 0)
            {
                string line = comon.byteSubstr(ref str, 56);
                string id = memberId.ToString(CultureInfo.InvariantCulture);
                // FRD stores the J-end distance before the I-end distance.
                GouikiList.Add(id, new Gouiki
                {
                    jDistance = ReadNumber(ref line, id),
                    iDistance = ReadNumber(ref line, id),
                    A = ReadNumber(ref line, id),
                    I = ReadNumber(ref line, id)
                });
                memberId++;
            }
        }

        private static double ReadNumber(ref string line, string memberId)
        {
            string value = comon.byteSubstr(ref line, 14).Trim();
            if (value.Length == 0)
                return 0;
            if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out double number) || !double.IsFinite(number))
                throw new InvalidDataException($"{wFile}: invalid rigid-zone value for member {memberId}.");
            return number;
        }

        /// <summary>元の節点・部材を保持して剛域データと剛域用諸元を生成する。</summary>
        public List<Rigid> GetRigid(node nodes, member members)
        {
            var result = new List<Rigid>();
            foreach (var entry in GouikiList)
            {
                var zone = entry.Value;
                if (zone.iDistance < 0 || zone.jDistance < 0)
                    throw new InvalidDataException($"{wFile}: negative rigid-zone length for member {entry.Key}.");
                if (zone.iDistance == 0 && zone.jDistance == 0)
                    continue;

                var target = members.getMember(entry.Key);
                if (target == null)
                    throw new InvalidDataException($"{wFile}: rigid-zone member {entry.Key} does not exist.");
                double length = target.Length(nodes);
                // Preserve input lengths while allowing roundoff at a shared end boundary.
                if (!double.IsFinite(length) || length <= 0 ||
                    zone.iDistance + zone.jDistance - length > RelativeLengthTolerance * length)
                    throw new InvalidDataException($"{wFile}: rigid-zone lengths exceed the length of member {entry.Key}.");

                result.Add(new Rigid
                {
                    m = entry.Key,
                    Ilength = zone.iDistance,
                    Jlength = zone.jDistance,
                    e = int.Parse(members.GetElementNo(target.e, zone.A, zone.I), CultureInfo.InvariantCulture)
                });
            }
            return result;
        }
    }
}

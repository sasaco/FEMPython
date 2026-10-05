using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Convert_Manager.FrameWebForJS
{
    public static class pile_spring
    {
        // Apply before rigid-zone splitting, while the original member IDs still exist.
        public static void Apply(Dictionary<string, string> wdata, node nodes, member members,
                                 fix_node supports, fix_member springs)
        {
            if (members.PileMembers.Count == 0)
                return;

            bool hasLengths = wdata.ContainsKey("$4.txt");
            bool hasValues = wdata.ContainsKey("$5.txt");
            if (hasLengths != hasValues)
                throw new InvalidDataException("杭バネの区間長 ($4.txt) とバネ値 ($5.txt) が揃っていません。");

            if (hasLengths)
                AddIntervals(wdata, nodes, members, springs);
            if (wdata.ContainsKey("$6.txt"))
                AddSupports(wdata, members, supports);
        }

        private static void AddIntervals(Dictionary<string, string> wdata, node nodes,
                                         member members, fix_member springs)
        {
            var lengths = ReadTable(wdata, "$4.txt", 2);
            var values = ReadTable(wdata, "$5.txt", 1);
            if (!lengths.Keys.SequenceEqual(values.Keys))
                throw new InvalidDataException("杭バネの区間番号が $4.txt と $5.txt で一致しません。");

            var intervals = new Dictionary<int, double>();
            var types = new SortedSet<int>();
            foreach (var row in lengths)
            {
                double length = Number(row.Value[1], "$4.txt");
                if (length <= 0)
                    throw new InvalidDataException("杭バネの区間長は正の数で指定してください ($4.txt)。");
                intervals.Add(row.Key, length);

                var columns = values[row.Key];
                for (int column = 1; column < columns.Length; column += 2)
                {
                    if (string.IsNullOrWhiteSpace(columns[column]) &&
                        (column + 1 == columns.Length || string.IsNullOrWhiteSpace(columns[column + 1])))
                        continue;
                    if (column + 1 == columns.Length)
                        throw new InvalidDataException("杭バネの直角方向と軸方向の列が揃っていません ($5.txt)。");
                    types.Add((column + 1) / 2);
                }
            }

            double totalLength = intervals.Values.Sum();
            foreach (string memberId in members.PileMembers)
            {
                Member pile = members.getMember(memberId);
                if (pile == null)
                    continue;
                double memberLength = pile.Length(nodes);
                if (totalLength > memberLength + 1e-6 * Math.Max(1, memberLength))
                    throw new InvalidDataException($"杭部材 {memberId} のバネ区間長が部材長を超えています ($4.txt)。");

                foreach (int type in types)
                {
                    string key = type.ToString(CultureInfo.InvariantCulture);
                    var sheets = springs.GetFixMember();
                    if (!sheets.TryGetValue(key, out var rows))
                        sheets.Add(key, rows = new List<FixMember>());

                    // B_Bane.tmp defines full-member springs; superpose them on each interval.
                    var existing = rows.Where(row => row.m == memberId).ToArray();
                    double baseTx = existing.Sum(row => row.tx);
                    double baseTy = existing.Sum(row => row.ty);
                    rows.RemoveAll(row => row.m == memberId);
                    foreach (var interval in intervals)
                    {
                        var columns = values[interval.Key];
                        rows.Add(new FixMember
                        {
                            m = memberId,
                            length = interval.Value,
                            ty = Cell(columns, type * 2 - 1, "$5.txt") + baseTy,
                            tx = Cell(columns, type * 2, "$5.txt") + baseTx
                        });
                    }
                    // Zero spring intervals are retained because they carry position information.
                    if ((baseTx != 0 || baseTy != 0) && memberLength - totalLength > 1e-6 * Math.Max(1, memberLength))
                        rows.Add(new FixMember { m = memberId, length = memberLength - totalLength, tx = baseTx, ty = baseTy });
                    for (int i = 0; i < rows.Count; i++)
                        rows[i].row = i + 1;
                }
            }
        }

        private static void AddSupports(Dictionary<string, string> wdata, member members, fix_node supports)
        {
            foreach (var type in ReadTable(wdata, "$6.txt", 4))
            {
                double tx = Number(type.Value[1], "$6.txt");
                double ty = Number(type.Value[2], "$6.txt");
                double rz = Number(type.Value[3], "$6.txt");
                if (tx == 0 && ty == 0 && rz == 0)
                    continue;
                string key = type.Key.ToString(CultureInfo.InvariantCulture);
                var sheets = supports.GetFixNode();
                if (!sheets.TryGetValue(key, out var rows))
                    sheets.Add(key, rows = new List<FixNode>());
                foreach (string memberId in members.PileMembers)
                {
                    Member pile = members.getMember(memberId);
                    if (pile == null)
                        continue;
                    var support = rows.FirstOrDefault(row => row.n == pile.nj);
                    if (support == null)
                    {
                        support = new FixNode { row = rows.Count == 0 ? 1 : rows.Max(row => row.row) + 1, n = pile.nj, count = 1 };
                        rows.Add(support);
                    }
                    support.tx = AddSupportValue(support.tx, tx);
                    support.ty = AddSupportValue(support.ty, ty);
                    support.rz = AddSupportValue(support.rz, rz);
                }
            }
        }

        private static double AddSupportValue(double existing, double value)
        {
            // 1 denotes a fixed DOF in the existing support format.
            return existing == 1 || value == 1 ? 1 : existing + value;
        }

        private static SortedDictionary<int, string[]> ReadTable(Dictionary<string, string> wdata, string file, int minimumColumns)
        {
            var lines = wdata[file].Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0 || !int.TryParse(lines[0].Trim(), out int count) || count < 0 || lines.Length != count + 1)
                throw new InvalidDataException($"杭バネの行数が不正です ({file})。");
            var result = new SortedDictionary<int, string[]>();
            foreach (string line in lines.Skip(1))
            {
                var columns = line.Split('\t');
                if (columns.Length < minimumColumns || !int.TryParse(columns[0].Trim(), out int id) || id <= 0 || result.ContainsKey(id))
                    throw new InvalidDataException($"杭バネの行番号または列数が不正です ({file})。");
                result.Add(id, columns);
            }
            return result;
        }

        private static double Cell(string[] columns, int index, string file)
        {
            return index < columns.Length ? Number(columns[index], file) : 0;
        }

        private static double Number(string text, string file)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            if (!double.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands,
                                 CultureInfo.InvariantCulture, out double value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException($"杭バネの数値が不正です ({file})。");
            return value;
        }
    }
}

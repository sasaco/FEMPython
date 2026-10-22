using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Convert_Manager.FrameG
{
    /// <summary>
    /// Reads a 2D FrameWeb input JSON (the format FrameWebforCS saves) into the FrameG model and
    /// reports what FrameG cannot represent. Errors are collected rather than thrown.
    /// </summary>
    internal sealed class FrdModelReader
    {
        private const double LengthTolerance = 1e-12;

        private static readonly HashSet<string> KnownSections = new HashSet<string>
        {
            "dimension", "node", "member", "element", "rigid", "fix_node", "fix_member", "joint",
            "notice_points", "load", "define", "combine", "pickup", "shell", "bridge_loads",
            "spatial_loads", "load_inf", "load_intensity_layout", "three", "result", "resultDimension", "ver"
        };

        // Sections that do not change the analysis and are not reported.
        private static readonly HashSet<string> SilentSections = new HashSet<string> { "three", "ver" };

        private static readonly string[] WarnedSections = { "result", "resultDimension", "load_intensity_layout" };

        private readonly JObject root;
        private readonly FrdDiagnosticList diagnostics;
        private readonly FrdModel model = new FrdModel();
        private readonly Dictionary<int, int> nodeNumbers = new Dictionary<int, int>();
        private readonly Dictionary<int, FrdMember> members = new Dictionary<int, FrdMember>();
        private readonly Dictionary<int, FrdNode> nodes = new Dictionary<int, FrdNode>();
        private readonly Dictionary<string, JObject>[] materials = new Dictionary<string, JObject>[FrdModel.MaterialSheets + 1];
        private readonly Dictionary<string, int> loadCaseNumbers = new Dictionary<string, int>();
        private readonly Dictionary<string, int> defineNumbers = new Dictionary<string, int>();
        private readonly Dictionary<string, int> combineNumbers = new Dictionary<string, int>();

        private FrdModelReader(JObject root, FrdDiagnosticList diagnostics)
        {
            this.root = root;
            this.diagnostics = diagnostics;
        }

        public static FrdModel Read(JObject root, string title, FrdDiagnosticList diagnostics)
        {
            var reader = new FrdModelReader(root, diagnostics);
            reader.ReadTitle(title);
            reader.ReadSections();
            return reader.model;
        }

        private void ReadTitle(string title)
        {
            title = (title ?? "").Trim();
            if (!IsPlainText(title))
            {
                diagnostics.Error(FrdDiagnosticCodes.InvalidText, "title",
                    "タイトルに Shift-JIS で表せない文字、タブまたは改行が含まれています。");
                return;
            }
            model.Title = Truncated(title, FrdLimits.TitleBytes, "title");
        }

        private void ReadSections()
        {
            foreach (var property in root.Properties())
                if (!KnownSections.Contains(property.Name))
                    diagnostics.Error(FrdDiagnosticCodes.UnknownSection, property.Name,
                        $"FrameG へ変換できない項目 {property.Name} があります。");
            var ignored = WarnedSections.Where(name => !IsEmpty(root[name])).ToArray();
            if (ignored.Length > 0)
                diagnostics.Warning(FrdDiagnosticCodes.SectionIgnored, "",
                    $"{string.Join("・", ignored)} は FrameG のファイルに書き出しません。");

            double? dimension = Number(root["dimension"], "dimension");
            if (dimension != 2)
            {
                diagnostics.Error(FrdDiagnosticCodes.Dimension, "dimension",
                    "FrameG へ変換できるのは 2D のデータ (dimension: 2) だけです。");
                return;
            }
            if (!IsEmpty(root["shell"]))
                diagnostics.Error(FrdDiagnosticCodes.Shell, "shell", "FrameG にはパネル (shell) がありません。");
            ReadSpatialLoads();

            ReadNodes();
            ReadMaterials();
            ReadMembers();
            ReadRigidZones();
            ReadSupports();
            ReadSprings();
            ReadJoints();
            ReadNoticePoints();
            ReadLoadCases();
            ReadDefines();
            ReadCombines();
            ReadPickups();
        }

        private void ReadSpatialLoads()
        {
            if (root["bridge_loads"] is JObject bridge)
            {
                if (!IsEmpty(bridge["panels"]) || !IsEmpty(bridge["paths"]) || !IsEmpty(bridge["cases"]))
                    diagnostics.Error(FrdDiagnosticCodes.SpatialLoad, "bridge_loads", "FrameG には橋面荷重がありません。");
            }
            else if (!IsEmpty(root["bridge_loads"]))
                diagnostics.Error(FrdDiagnosticCodes.SpatialLoad, "bridge_loads", "FrameG には橋面荷重がありません。");
            foreach (string name in new[] { "spatial_loads", "load_inf" })
                if (!IsEmpty(root[name]))
                    diagnostics.Error(FrdDiagnosticCodes.SpatialLoad, name, "FrameG には空間荷重がありません。");
        }

        private void ReadNodes()
        {
            var source = new SortedDictionary<int, FrdNode>();
            foreach (var property in Object("node"))
            {
                string path = "node." + property.Name;
                if (!PositiveId(property.Name, path, out int id))
                    continue;
                if (source.ContainsKey(id))
                {
                    diagnostics.Error(FrdDiagnosticCodes.Duplicate, path, $"節点 {id} が重複しています。");
                    continue;
                }
                var value = property.Value as JObject ?? new JObject();
                var node = new FrdNode
                {
                    X = Number(value["x"], path + ".x") ?? 0,
                    Y = Number(value["y"], path + ".y") ?? 0
                };
                if ((Number(value["z"], path + ".z") ?? 0) != 0)
                    Dropped3D("node", "節点の z 座標");
                source.Add(id, node);
            }
            if (!WithinLimit(source.Count, "node", "節点"))
                return;
            int number = 0;
            foreach (var pair in source)
            {
                pair.Value.Number = ++number;
                nodeNumbers.Add(pair.Key, number);
                nodes.Add(pair.Key, pair.Value);
                model.Nodes.Add(pair.Value);
                model.NodeNumbers.Add(Text(pair.Key), number);
            }
            ReportRenumbering("node", "節点", nodeNumbers);
        }

        private void ReadMaterials()
        {
            foreach (var sheet in Object("element"))
            {
                string path = "element." + sheet.Name;
                if (!int.TryParse(sheet.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
                    index < 1 || index > FrdModel.MaterialSheets)
                {
                    diagnostics.Error(FrdDiagnosticCodes.Limit, path, "FrameG の材料のシートは 1〜6 です。");
                    continue;
                }
                var sheetMaterials = new Dictionary<string, JObject>();
                foreach (var material in sheet.Value as JObject ?? new JObject())
                    if (material.Value is JObject value)
                        sheetMaterials[NormalizeId(material.Key)] = value;
                materials[index] = sheetMaterials;
            }
        }

        private void ReadMembers()
        {
            var source = new SortedDictionary<int, JObject>();
            foreach (var property in Object("member"))
            {
                string path = "member." + property.Name;
                if (!PositiveId(property.Name, path, out int id))
                    continue;
                if (source.ContainsKey(id))
                    diagnostics.Error(FrdDiagnosticCodes.Duplicate, path, $"部材 {id} が重複しています。");
                else
                    source.Add(id, property.Value as JObject ?? new JObject());
            }
            if (!WithinLimit(source.Count, "member", "部材"))
                return;

            int number = 0;
            foreach (var pair in source)
            {
                string path = "member." + Text(pair.Key);
                var member = new FrdMember { Number = ++number };
                members.Add(pair.Key, member);
                model.Members.Add(member);
                model.MemberNumbers.Add(Text(pair.Key), number);
                if ((Number(pair.Value["cg"], path + ".cg") ?? 0) != 0)
                    Dropped3D("member", "部材のコードアングル (cg)");

                FrdNode i = NodeReference(pair.Value["ni"], path + ".ni");
                FrdNode j = NodeReference(pair.Value["nj"], path + ".nj");
                if (i != null && j != null)
                {
                    member.I = i.Number;
                    member.J = j.Number;
                    member.Length = Math.Sqrt(Math.Pow(j.X - i.X, 2) + Math.Pow(j.Y - i.Y, 2));
                    if (!(member.Length > 0))
                        diagnostics.Error(FrdDiagnosticCodes.MemberGeometry, path, $"部材 {pair.Key} の長さが 0 です。");
                }
                ReadMemberMaterial(member, pair.Value["e"], path + ".e");
            }
            ReportRenumbering("member", "部材", members.ToDictionary(pair => pair.Key, pair => pair.Value.Number));
        }

        private void ReadMemberMaterial(FrdMember member, JToken reference, string path)
        {
            string id = NormalizeId(Text(reference));
            double? e = null, xp = null;
            bool found = false;
            for (int sheet = 1; sheet <= FrdModel.MaterialSheets; sheet++)
            {
                if (materials[sheet] == null || !materials[sheet].TryGetValue(id, out JObject material))
                    continue;
                found = true;
                string materialPath = $"element.{sheet}.{id}";
                double sheetE = Number(material["E"], materialPath + ".E") ?? 0;
                double sheetXp = Number(material["Xp"], materialPath + ".Xp") ?? 0;
                if (e == null)
                {
                    e = sheetE;
                    xp = sheetXp;
                }
                else if (e != sheetE || xp != sheetXp)
                    diagnostics.Error(FrdDiagnosticCodes.MaterialConflict, materialPath,
                        $"材料 {id} の E または Xp がシートによって違います。FrameG は部材ごとに E と Xp を 1 つしか持てません。");
                member.A[sheet - 1] = Number(material["A"], materialPath + ".A");
                member.Iz[sheet - 1] = Number(material["Iz"], materialPath + ".Iz");
                foreach (string key in new[] { "G", "J", "Iy" })
                    if ((Number(material[key], materialPath + "." + key) ?? 0) != 0)
                        Dropped3D("element", "材料の G・J・Iy");
                // FrameWebforCS saves the name as "n"; ConvertManager's own output uses "name".
                string nameKey = material["n"] != null ? "n" : "name";
                string name = Text(material[nameKey]);
                if (member.Name.Length == 0 && name.Length > 0)
                    member.Name = MemberName(name, materialPath + "." + nameKey);
            }
            if (!found)
            {
                diagnostics.Error(FrdDiagnosticCodes.Reference, path, $"材料 {id} がありません。");
                return;
            }
            member.E = e.Value;
            member.Xp = xp.Value;
        }

        private string MemberName(string name, string path)
        {
            if (!IsPlainText(name))
            {
                diagnostics.Error(FrdDiagnosticCodes.InvalidText, path, "材料名に Shift-JIS で表せない文字、タブまたは改行が含まれています。");
                return "";
            }
            if (name.Contains("杭"))
                diagnostics.WarningOnce(FrdDiagnosticCodes.PileName, path,
                    "名前に「杭」を含む部材を FrameG は杭として扱うことがあります。杭のデータは書き出しません。");
            return Truncated(name, FrdLimits.MemberNameBytes, path);
        }

        private void ReadRigidZones()
        {
            var rows = root["rigid"] as JArray;
            if (rows == null)
            {
                if (!IsEmpty(root["rigid"]))
                    diagnostics.Error(FrdDiagnosticCodes.InvalidValue, "rigid", "rigid は配列で指定してください。");
                return;
            }
            for (int index = 0; index < rows.Count; index++)
            {
                string path = $"rigid[{index}]";
                if (!(rows[index] is JObject row))
                    continue;
                double iLength = Number(row["Ilength"], path + ".Ilength") ?? 0;
                double jLength = Number(row["Jlength"], path + ".Jlength") ?? 0;
                if (iLength == 0 && jLength == 0)
                    continue;
                FrdMember member = MemberReference(row["m"], path + ".m");
                if (member == null)
                    continue;
                if (member.Rigid != null)
                {
                    diagnostics.Error(FrdDiagnosticCodes.Duplicate, path, $"部材 {Text(row["m"])} の剛域が重複しています。");
                    continue;
                }
                if (iLength < 0 || jLength < 0 ||
                    iLength + jLength - member.Length > LengthTolerance * member.Length)
                {
                    diagnostics.Error(FrdDiagnosticCodes.MemberGeometry, path,
                        "剛域の長さは 0 以上で、合計が部材の長さを超えないように指定してください。");
                    continue;
                }
                member.Rigid = RigidMaterial(member, row["e"], path + ".e");
                if (member.Rigid == null)
                    continue;
                member.Rigid.ILength = iLength;
                member.Rigid.JLength = jLength;
            }
        }

        // FrameG keeps one A and I per rigid zone and takes E and Xp from the member.
        private FrdRigid RigidMaterial(FrdMember member, JToken reference, string path)
        {
            string id = NormalizeId(Text(reference));
            FrdRigid rigid = null;
            for (int sheet = 1; sheet <= FrdModel.MaterialSheets; sheet++)
            {
                if (id.Length == 0 || materials[sheet] == null || !materials[sheet].TryGetValue(id, out JObject material))
                    continue;
                string materialPath = $"element.{sheet}.{id}";
                double a = Number(material["A"], materialPath + ".A") ?? 0;
                double iz = Number(material["Iz"], materialPath + ".Iz") ?? 0;
                double e = Number(material["E"], materialPath + ".E") ?? 0;
                double xp = Number(material["Xp"], materialPath + ".Xp") ?? 0;
                if (e != member.E || xp != member.Xp)
                {
                    diagnostics.Error(FrdDiagnosticCodes.MaterialConflict, materialPath,
                        $"剛域の材料 {id} の E または Xp が部材と違います。FrameG の剛域は部材の E と Xp を使います。");
                    return null;
                }
                if (rigid == null)
                    rigid = new FrdRigid { A = a, I = iz };
                else if (rigid.A != a || rigid.I != iz)
                {
                    diagnostics.Error(FrdDiagnosticCodes.MaterialConflict, materialPath,
                        $"剛域の材料 {id} の A または Iz がシートによって違います。FrameG の剛域は A と I を 1 つしか持てません。");
                    return null;
                }
            }
            if (rigid == null)
                diagnostics.Error(FrdDiagnosticCodes.Reference, path, $"剛域の材料 {id} がありません。");
            return rigid;
        }

        private void ReadSupports()
        {
            var supports = new SortedDictionary<int, FrdSupport>();
            foreach (var (sheet, rows, sheetPath) in ConditionSheets("fix_node"))
            {
                var seen = new HashSet<int>();
                for (int index = 0; index < rows.Count; index++)
                {
                    string path = $"{sheetPath}[{index}]";
                    if (!(rows[index] is JObject row) || Text(row["n"]).Length == 0)
                        continue;
                    FrdNode node = NodeReference(row["n"], path + ".n");
                    if (node == null)
                        continue;
                    if (!seen.Add(node.Number))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.Duplicate, path, $"支点の節点 {Text(row["n"])} がシートの中で重複しています。");
                        continue;
                    }
                    var values = new[]
                    {
                        Number(row["tx"], path + ".tx") ?? 0,
                        Number(row["ty"], path + ".ty") ?? 0,
                        Number(row["rz"], path + ".rz") ?? 0
                    };
                    foreach (string key in new[] { "tz", "rx", "ry" })
                        if ((Number(row[key], path + "." + key) ?? 0) != 0)
                            Dropped3D("fix_node", "支点の tz・rx・ry");
                    if (!supports.TryGetValue(node.Number, out FrdSupport support))
                        supports.Add(node.Number, support = new FrdSupport { Node = node.Number });
                    support.Values[sheet - 1] = values;
                    model.SupportSheetsUsed[sheet] = true;
                }
            }
            model.Supports.AddRange(supports.Values);
        }

        private void ReadSprings()
        {
            var springs = new SortedDictionary<int, FrdSpring>();
            foreach (var (sheet, rows, sheetPath) in ConditionSheets("fix_member"))
            {
                var seen = new HashSet<int>();
                for (int index = 0; index < rows.Count; index++)
                {
                    string path = $"{sheetPath}[{index}]";
                    if (!(rows[index] is JObject row) || Text(row["m"]).Length == 0)
                        continue;
                    if (!IsEmpty(row["length"]))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.PileSpring, path + ".length",
                            "区間を指定した部材ばね (杭ばね) は FrameG へ書き出せません。");
                        continue;
                    }
                    FrdMember member = MemberReference(row["m"], path + ".m");
                    if (member == null)
                        continue;
                    if (!seen.Add(member.Number))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.Duplicate, path, $"部材ばねの部材 {Text(row["m"])} がシートの中で重複しています。");
                        continue;
                    }
                    foreach (string key in new[] { "tz", "tr" })
                        if ((Number(row[key], path + "." + key) ?? 0) != 0)
                            Dropped3D("fix_member", "部材ばねの tz・tr");
                    if (!springs.TryGetValue(member.Number, out FrdSpring spring))
                        springs.Add(member.Number, spring = new FrdSpring { Member = member.Number });
                    spring.Axial[sheet - 1] = Number(row["tx"], path + ".tx") ?? 0;
                    spring.Transverse[sheet - 1] = Number(row["ty"], path + ".ty") ?? 0;
                }
            }
            model.Springs.AddRange(springs.Values);
        }

        private void ReadJoints()
        {
            var joints = new SortedDictionary<int, FrdJoint>();
            foreach (var (sheet, rows, sheetPath) in ConditionSheets("joint"))
            {
                var seen = new HashSet<int>();
                for (int index = 0; index < rows.Count; index++)
                {
                    string path = $"{sheetPath}[{index}]";
                    if (!(rows[index] is JObject row) || Text(row["m"]).Length == 0)
                        continue;
                    FrdMember member = MemberReference(row["m"], path + ".m");
                    if (member == null)
                        continue;
                    if (!seen.Add(member.Number))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.Duplicate, path, $"結合の部材 {Text(row["m"])} がシートの中で重複しています。");
                        continue;
                    }
                    foreach (string key in new[] { "xi", "yi", "xj", "yj" })
                    {
                        double? flag = Number(row[key], path + "." + key);
                        if (flag != null && flag != 1)
                            Dropped3D("joint", "結合の xi・yi・xj・yj");
                    }
                    int? zi = JointFlag(row["zi"], path + ".zi");
                    int? zj = JointFlag(row["zj"], path + ".zj");
                    if (zi == null || zj == null)
                        continue;
                    if (!joints.TryGetValue(member.Number, out FrdJoint joint))
                        joints.Add(member.Number, joint = new FrdJoint { Member = member.Number });
                    joint.Flags[sheet - 1] = new[] { zi.Value, zj.Value };
                }
            }
            model.Joints.AddRange(joints.Values);
        }

        private int? JointFlag(JToken token, string path)
        {
            double value = Number(token, path) ?? 1;
            if (value == 0 || value == 1)
                return (int)value;
            diagnostics.Error(FrdDiagnosticCodes.InvalidValue, path, "結合は 0 (ピン) か 1 (剛) で指定してください。");
            return null;
        }

        private void ReadNoticePoints()
        {
            var rows = root["notice_points"] as JArray ?? new JArray();
            var byMember = new Dictionary<int, FrdNoticePoints>();
            var ordered = rows.Select((row, index) => (row: row as JObject, index))
                .Where(item => item.row != null)
                .OrderBy(item => Number(item.row["row"], "") ?? item.index)
                .ThenBy(item => item.index);
            foreach (var (row, index) in ordered)
            {
                string path = $"notice_points[{index}]";
                if (Text(row["m"]).Length == 0)
                    continue;
                FrdMember member = MemberReference(row["m"], path + ".m");
                if (member == null)
                    continue;
                if (!byMember.TryGetValue(member.Number, out FrdNoticePoints points))
                {
                    byMember.Add(member.Number, points = new FrdNoticePoints { Member = member.Number });
                    model.NoticePoints.Add(points);
                }
                var values = row["Points"] as JArray ?? new JArray();
                for (int i = 0; i < values.Count; i++)
                {
                    double? point = Number(values[i], $"{path}.Points[{i}]");
                    if (point != null)
                        points.Points.Add(point.Value);
                }
                if (points.Points.Count > FrdLimits.NoticePoints)
                    diagnostics.Error(FrdDiagnosticCodes.Limit, path,
                        $"FrameG の着目点は 1 部材に {FrdLimits.NoticePoints} 点までです。");
            }
            model.NoticePoints.RemoveAll(points => points.Points.Count == 0);
        }

        private void ReadLoadCases()
        {
            var cases = Object("load").ToList();
            var ordered = cases.Select((property, index) => (property, index))
                .OrderBy(item => double.TryParse(item.property.Name, NumberStyles.Float, CultureInfo.InvariantCulture, out double id) ? id : double.MaxValue)
                .ThenBy(item => item.index)
                .Select(item => item.property)
                .ToList();
            if (!WithinLimit(ordered.Count, "load", "荷重ケース"))
                return;
            foreach (var property in ordered)
            {
                var loadCase = new FrdLoadCase { Number = model.LoadCases.Count + 1 };
                model.LoadCases.Add(loadCase);
                loadCaseNumbers[NormalizeId(property.Name)] = loadCase.Number;
                model.LoadCaseNumbers[property.Name] = loadCase.Number;
                ReadLoadCase(loadCase, property.Value as JObject ?? new JObject(), "load." + property.Name);
            }
        }

        private void ReadLoadCase(FrdLoadCase loadCase, JObject source, string path)
        {
            string symbol = Text(source["symbol"]);
            if (symbol == "LL")
                diagnostics.Error(FrdDiagnosticCodes.MovingLoad, path + ".symbol", "移動荷重 (記号 LL) は FrameG へ書き出せません。");
            loadCase.Symbol = CaseText(symbol, FrdLimits.SymbolBytes, path + ".symbol");
            loadCase.Name = CaseText(Text(source["name"]), FrdLimits.LoadNameBytes, path + ".name");
            loadCase.FixNode = Selector(source["fix_node"], path + ".fix_node", FrdModel.ConditionSheets);
            loadCase.Element = Selector(source["element"], path + ".element", FrdModel.MaterialSheets);
            loadCase.FixMember = Selector(source["fix_member"], path + ".fix_member", FrdModel.ConditionSheets);
            loadCase.Joint = Selector(source["joint"], path + ".joint", FrdModel.ConditionSheets);
            foreach (string name in new[] { "spatial_loads", "load_inf" })
                if (!IsEmpty(source[name]))
                    diagnostics.Error(FrdDiagnosticCodes.SpatialLoad, path + "." + name, "FrameG には空間荷重がありません。");

            var records = new SortedDictionary<int, FrdLoadRecord>();
            foreach (var (row, rowNumber, rowPath) in LoadRows(source["load_node"] as JArray, path + ".load_node"))
            {
                FrdNodeLoad load = NodeLoad(row, rowPath);
                if (load == null)
                    continue;
                if (!records.TryGetValue(rowNumber, out FrdLoadRecord record))
                    records.Add(rowNumber, record = new FrdLoadRecord());
                if (record.Node != null)
                    diagnostics.Error(FrdDiagnosticCodes.Duplicate, rowPath, $"節点荷重の行 {rowNumber} が重複しています。");
                record.Node = load;
            }
            foreach (var (row, rowNumber, rowPath) in LoadRows(source["load_member"] as JArray, path + ".load_member"))
            {
                FrdMemberLoad load = MemberLoad(row, rowPath);
                if (load == null)
                    continue;
                if (!records.TryGetValue(rowNumber, out FrdLoadRecord record))
                    records.Add(rowNumber, record = new FrdLoadRecord());
                if (record.Member != null)
                    diagnostics.Error(FrdDiagnosticCodes.Duplicate, rowPath, $"要素荷重の行 {rowNumber} が重複しています。");
                record.Member = load;
            }
            // Keep each input row at its position: FrameWeb continues a relative L1 only on the next row.
            int last = records.Count == 0 ? 0 : records.Keys.Max();
            for (int rowNumber = 1; rowNumber <= last; rowNumber++)
                loadCase.Records.Add(records.TryGetValue(rowNumber, out FrdLoadRecord record) ? record : new FrdLoadRecord());
        }

        // Rows with a positive row number keep it; otherwise every row of the list is numbered by position.
        private IEnumerable<(JObject row, int rowNumber, string path)> LoadRows(JArray rows, string path)
        {
            if (rows == null)
                yield break;
            bool numbered = rows.All(row => row is JObject value && Number(value["row"], "") is double number &&
                number >= 1 && number == Math.Truncate(number));
            for (int index = 0; index < rows.Count; index++)
            {
                if (!(rows[index] is JObject row))
                    continue;
                int rowNumber = numbered ? (int)Number(row["row"], "").Value : index + 1;
                yield return (row, rowNumber, $"{path}[{index}]");
            }
        }

        private FrdNodeLoad NodeLoad(JObject row, string path)
        {
            double? signed = Number(row["n"], path + ".n");
            if (signed == null || signed == 0 || signed != Math.Truncate(signed.Value))
                return null;
            if (new[] { "tx", "ty", "tz", "rx", "ry", "rz" }.All(key => IsEmpty(row[key])))
                return null;
            if (signed < 0)
            {
                diagnostics.Error(FrdDiagnosticCodes.ImposedDisplacement, path + ".n",
                    "強制変位 (負の節点番号) は FrameG へ書き出せません。");
                return null;
            }
            FrdNode node = NodeReference(row["n"], path + ".n");
            if (node == null)
                return null;
            foreach (string key in new[] { "tz", "rx", "ry" })
                if ((Number(row[key], path + "." + key) ?? 0) != 0)
                    Dropped3D("load_node", "節点荷重の tz・rx・ry");
            // FrameG takes vertical force and moment with the opposite sign (load.cs exchangeData).
            return new FrdNodeLoad
            {
                Node = node.Number,
                Tx = Number(row["tx"], path + ".tx") ?? 0,
                Ty = -(Number(row["ty"], path + ".ty") ?? 0),
                Rz = -(Number(row["rz"], path + ".rz") ?? 0)
            };
        }

        private FrdMemberLoad MemberLoad(JObject row, string path)
        {
            double? m1 = Number(row["m1"] ?? row["m"], path + ".m1");
            double? m2 = Number(row["m2"], path + ".m2");
            if (m1 == null && m2 == null)
                return null;
            if (new[] { "L1", "L2", "P1", "P2" }.All(key => IsEmpty(row[key])))
                return null;

            double? markValue = Number(row["mark"], path + ".mark");
            string direction = Text(row["direction"]).ToLowerInvariant();
            int mark = markValue != null && markValue == Math.Truncate(markValue.Value) ? (int)markValue.Value : -1;
            var load = new FrdMemberLoad();
            bool negate;
            if ((mark == 1 || mark == 2) && direction == "y")
            {
                load.Mark = mark;
                negate = true;
            }
            else if ((mark == 1 || mark == 2) && direction == "x")
            {
                load.Mark = mark + 4;
                negate = false;
            }
            else if ((mark == 1 || mark == 2) && (direction == "gy" || direction == "gx"))
            {
                // Written like y (mark 1/2, sign reversed) with FrameG's global direction V/H.
                load.Mark = mark;
                load.Direction = direction == "gy" ? "V" : "H";
                negate = true;
            }
            else if (mark == 11 && direction == "z")
            {
                load.Mark = 11;
                negate = true;
            }
            else if (mark == 9)
            {
                load.Mark = 9;
                negate = false;
            }
            else
            {
                diagnostics.Error(FrdDiagnosticCodes.LoadType, path,
                    $"要素荷重 (mark {Text(row["mark"])}, direction {Text(row["direction"])}) は FrameG へ書き出せません。");
                return null;
            }

            load.M1 = MemberRange(m1, path + ".m1", letterCode: false);
            load.M2 = MemberRange(m2, path + ".m2", letterCode: true);
            load.L1 = Distance(row["L1"], path + ".L1");
            load.L2 = Distance(row["L2"], path + ".L2");
            double sign = negate ? -1 : 1;
            load.P1 = sign * (Number(row["P1"], path + ".P1") ?? 0);
            load.P2 = sign * (Number(row["P2"], path + ".P2") ?? 0);
            return load;
        }

        // A member reference keeps its sign; FrameG writes a negative m2 as a letter code (load.cs gDecodeMinusIntegerStr).
        private string MemberRange(double? value, string path, bool letterCode)
        {
            if (value == null || value == 0)
                return "";
            if (value != Math.Truncate(value.Value))
            {
                diagnostics.Error(FrdDiagnosticCodes.InvalidValue, path, "部材番号は整数で指定してください。");
                return "";
            }
            int id = (int)Math.Abs(value.Value);
            if (!members.TryGetValue(id, out FrdMember member))
            {
                diagnostics.Error(FrdDiagnosticCodes.Reference, path, $"部材 {id} がありません。");
                return "";
            }
            string digits = FrdText.Integer(member.Number);
            if (value > 0)
                return digits;
            if (!letterCode)
                return "-" + digits;
            return (char)('A' + digits[0] - '1') + digits.Substring(1);
        }

        // Distances keep their input text, so the lexical sign of "-0" (relative to the previous row) survives.
        private string Distance(JToken token, string path)
        {
            if (IsEmpty(token))
                return "";
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return FrdText.Number(Number(token, path) ?? 0);
            string text = Text(token);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
            {
                diagnostics.Error(FrdDiagnosticCodes.InvalidValue, path, $"距離 {text} は数値ではありません。");
                return "";
            }
            return text;
        }

        private int Selector(JToken token, string path, int max)
        {
            double? value = Number(token, path);
            if (value == null || value == 0)
                return 1;
            if (value < 1 || value > max || value != Math.Truncate(value.Value))
            {
                diagnostics.Error(FrdDiagnosticCodes.Limit, path, $"FrameG のシート番号は 1〜{max} です。");
                return 1;
            }
            return (int)value.Value;
        }

        private string CaseText(string text, int maxBytes, string path)
        {
            if (!IsPlainText(text))
            {
                diagnostics.Error(FrdDiagnosticCodes.InvalidText, path, "Shift-JIS で表せない文字、タブまたは改行が含まれています。");
                return "";
            }
            return Truncated(text, maxBytes, path);
        }

        private static bool IsPlainText(string text) =>
            text.IndexOfAny(new[] { '\t', '\r', '\n', '\0' }) < 0 && FrdText.CanEncode(text);

        private void ReadDefines()
        {
            var defines = OrderedRows("define");
            // COMBINE refers to a DEFINE number in a 4-byte field.
            if (defines.Count > 9999)
            {
                diagnostics.Error(FrdDiagnosticCodes.Limit, "define", "FrameG の DEFINE は 9999 個までです。");
                return;
            }
            foreach (var (property, value) in defines)
            {
                string path = "define." + property;
                var define = new FrdDefine { Number = model.Defines.Count + 1 };
                model.Defines.Add(define);
                defineNumbers[NormalizeId(property)] = define.Number;
                if (Text(value["name"]).Length > 0)
                    diagnostics.WarningOnce(FrdDiagnosticCodes.DefineNameDropped, "define", "FrameG の DEFINE には名前がありません。");
                foreach (var (column, token, columnPath) in Columns(value, path, FrdLimits.DefineColumns))
                {
                    double? number = Number(token, columnPath);
                    if (number == null)
                        continue;
                    if (number != Math.Truncate(number.Value))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.InvalidValue, columnPath, "荷重ケース番号は整数で指定してください。");
                        continue;
                    }
                    if (number == 0)
                    {
                        define.Cases[column - 1] = 0;
                        continue;
                    }
                    string caseId = Text(Math.Abs(number.Value));
                    if (!loadCaseNumbers.TryGetValue(caseId, out int caseNumber))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.Reference, columnPath, $"荷重ケース {caseId} がありません。");
                        continue;
                    }
                    define.Cases[column - 1] = number < 0 ? -caseNumber : caseNumber;
                }
            }
        }

        private void ReadCombines()
        {
            var combines = OrderedRows("combine");
            if (!WithinLimit(combines.Count, "combine", "COMBINE"))
                return;
            // Without DEFINE, COMBINE refers to load cases; FrameG always goes through DEFINE, so add one per case.
            bool direct = model.Defines.Count == 0 && combines.Count > 0;
            if (direct)
                foreach (var pair in loadCaseNumbers.OrderBy(pair => pair.Value))
                {
                    var define = new FrdDefine { Number = pair.Value };
                    define.Cases[0] = pair.Value;
                    model.Defines.Add(define);
                    defineNumbers[pair.Key] = pair.Value;
                }
            foreach (var (property, value) in combines)
            {
                string path = "combine." + property;
                var combine = new FrdCombine();
                model.Combines.Add(combine);
                combineNumbers[NormalizeId(property)] = model.Combines.Count;
                combine.Name = CaseText(Text(value["name"]), FrdLimits.CombineNameBytes, path + ".name");
                foreach (var term in value.Properties().Where(item => IsColumnKey(item.Name)))
                {
                    string termPath = path + "." + term.Name;
                    double? coefficient = Number(term.Value, termPath);
                    if (coefficient == null)
                        continue;
                    string target = NormalizeId(term.Name.Substring(1));
                    if (!defineNumbers.TryGetValue(target, out int defineNumber))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.Reference, termPath,
                            direct ? $"荷重ケース {target} がありません。" : $"DEFINE {target} がありません。");
                        continue;
                    }
                    combine.Terms.Add(new KeyValuePair<int, double>(defineNumber, coefficient.Value));
                }
                if (combine.Terms.Count > FrdLimits.CombineTerms)
                    diagnostics.Error(FrdDiagnosticCodes.Limit, path, $"FrameG の COMBINE は {FrdLimits.CombineTerms} 項までです。");
            }
        }

        private void ReadPickups()
        {
            foreach (var (property, value) in OrderedRows("pickup"))
            {
                string path = "pickup." + property;
                var pickup = new FrdPickup();
                model.Pickups.Add(pickup);
                pickup.Name = CaseText(Text(value["name"]), FrdLimits.CombineNameBytes, path + ".name");
                foreach (var (column, token, columnPath) in Columns(value, path, FrdLimits.PickupColumns))
                {
                    double? number = Number(token, columnPath);
                    if (number == null)
                        continue;
                    string target = Text(number.Value);
                    if (!combineNumbers.TryGetValue(target, out int combineNumber))
                    {
                        diagnostics.Error(FrdDiagnosticCodes.Reference, columnPath, $"COMBINE {target} がありません。");
                        continue;
                    }
                    pickup.Combines[column - 1] = combineNumber;
                }
            }
        }

        // Rows of define/combine/pickup in their "row" order, then their key order.
        private List<(string property, JObject value)> OrderedRows(string section) =>
            Object(section).Select((property, index) => (property, index))
                .Where(item => item.property.Value is JObject)
                .OrderBy(item => Number(item.property.Value["row"], "") ?? double.MaxValue)
                .ThenBy(item => double.TryParse(item.property.Name, NumberStyles.Float, CultureInfo.InvariantCulture, out double id) ? id : double.MaxValue)
                .ThenBy(item => item.index)
                .Select(item => (item.property.Name, (JObject)item.property.Value))
                .ToList();

        private static bool IsColumnKey(string name) =>
            name.Length > 1 && name[0] == 'C' && name.Skip(1).All(char.IsDigit);

        private IEnumerable<(int column, JToken token, string path)> Columns(JObject value, string path, int maxColumns)
        {
            foreach (var property in value.Properties().Where(item => IsColumnKey(item.Name)))
            {
                string columnPath = path + "." + property.Name;
                if (!int.TryParse(property.Name.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out int column) ||
                    column < 1 || column > maxColumns)
                {
                    if (!IsEmpty(property.Value))
                        diagnostics.Error(FrdDiagnosticCodes.Limit, columnPath, $"FrameG の列は C1〜C{maxColumns} です。");
                    continue;
                }
                yield return (column, property.Value, columnPath);
            }
        }

        private IEnumerable<(int sheet, JArray rows, string path)> ConditionSheets(string section)
        {
            foreach (var property in Object(section))
            {
                string path = section + "." + property.Name;
                if (!int.TryParse(property.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int sheet) ||
                    sheet < 1 || sheet > FrdModel.ConditionSheets)
                {
                    diagnostics.Error(FrdDiagnosticCodes.Limit, path, $"FrameG のシートは 1〜{FrdModel.ConditionSheets} です。");
                    continue;
                }
                if (property.Value is JArray rows)
                    yield return (sheet, rows, path);
                else if (!IsEmpty(property.Value))
                    diagnostics.Error(FrdDiagnosticCodes.InvalidValue, path, "シートは配列で指定してください。");
            }
        }

        private IEnumerable<JProperty> Object(string section)
        {
            if (root[section] is JObject value)
                return value.Properties().ToList();
            if (!IsEmpty(root[section]))
                diagnostics.Error(FrdDiagnosticCodes.InvalidValue, section, $"{section} はオブジェクトで指定してください。");
            return Enumerable.Empty<JProperty>();
        }

        private FrdNode NodeReference(JToken token, string path)
        {
            double? id = Number(token, path);
            if (id != null && id == Math.Truncate(id.Value) && nodes.TryGetValue((int)id.Value, out FrdNode node))
                return node;
            diagnostics.Error(FrdDiagnosticCodes.Reference, path, $"節点 {Text(token)} がありません。");
            return null;
        }

        private FrdMember MemberReference(JToken token, string path)
        {
            double? id = Number(token, path);
            if (id != null && id == Math.Truncate(id.Value) && members.TryGetValue((int)id.Value, out FrdMember member))
                return member;
            diagnostics.Error(FrdDiagnosticCodes.Reference, path, $"部材 {Text(token)} がありません。");
            return null;
        }

        private bool PositiveId(string key, string path, out int id)
        {
            if (int.TryParse(key.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out id) && id >= 1)
                return true;
            diagnostics.Error(FrdDiagnosticCodes.InvalidId, path, $"番号 {key} は正の整数で指定してください。");
            return false;
        }

        private bool WithinLimit(int count, string path, string label)
        {
            if (count <= FrdLimits.MaxNumber)
                return true;
            diagnostics.Error(FrdDiagnosticCodes.Limit, path, $"FrameG の{label}は {FrdLimits.MaxNumber} 個までです ({count} 個あります)。");
            return false;
        }

        private void ReportRenumbering(string path, string label, Dictionary<int, int> numbers)
        {
            var changed = numbers.Where(pair => pair.Key != pair.Value).OrderBy(pair => pair.Key).ToList();
            if (changed.Count == 0)
                return;
            string sample = string.Join(", ", changed.Take(10).Select(pair => $"{pair.Key}→{pair.Value}"));
            diagnostics.Warning(FrdDiagnosticCodes.Renumbered, path,
                $"FrameG は 1 からの連番を使うため、{label}番号を振り直しました ({changed.Count} 個: {sample}{(changed.Count > 10 ? ", …" : "")})。");
        }

        private void Dropped3D(string path, string label) =>
            diagnostics.WarningOnce(FrdDiagnosticCodes.ThreeDimensionalFieldDropped, path,
                $"{label}は 2D の解析に使わないため書き出しません。");

        private string Truncated(string text, int maxBytes, string path)
        {
            string result = FrdText.Truncate(text, maxBytes);
            if (result == text)
                return result;
            result = result.TrimEnd();
            diagnostics.Warning(FrdDiagnosticCodes.TextTruncated, path,
                    $"FrameG の欄 ({maxBytes} バイト) に収まらないため「{result}」に切り詰めました。");
            return result;
        }

        // A number, or null when the token is missing, null or blank. Anything else is an error.
        private double? Number(JToken token, string path)
        {
            if (IsEmpty(token))
                return null;
            double value;
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                value = token.Value<double>();
            else if (token.Type != JTokenType.String ||
                !double.TryParse(((string)token).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                value = double.NaN;
            if (double.IsFinite(value))
                return value;
            if (path.Length > 0)
                diagnostics.Error(FrdDiagnosticCodes.InvalidValue, path, $"{token} は有限の数値ではありません。");
            return null;
        }

        private static bool IsEmpty(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return true;
            if (token.Type == JTokenType.String)
                return ((string)token).Trim().Length == 0;
            if (token is JContainer container)
                return !container.HasValues;
            return false;
        }

        private static string Text(JToken token)
        {
            if (IsEmpty(token))
                return "";
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return Text(token.Value<double>());
            return token.Type == JTokenType.String ? ((string)token).Trim() : token.ToString();
        }

        private static string Text(double value) => FrdText.Number(value);

        // Integer IDs compare by value ("01" and 1 are the same); other IDs by their trimmed text.
        private static string NormalizeId(string id)
        {
            id = (id ?? "").Trim();
            return double.TryParse(id, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) &&
                value == Math.Truncate(value) && double.IsFinite(value) ? FrdText.Number(value) : id;
        }
    }
}

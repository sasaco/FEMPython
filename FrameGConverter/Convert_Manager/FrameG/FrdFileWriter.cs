using System.Collections.Generic;
using System.Linq;
using System.Text;
using Convert_Manager.Lzh;

namespace Convert_Manager.FrameG
{
    /// <summary>
    /// Writes the files inside a .frd. Layouts follow the forward readers in FrameWebForJS and the
    /// sample archives: fixed-width .tmp records with no separators, and $N.txt tables with a count
    /// line and CRLF after every line.
    /// </summary>
    internal sealed class FrdFileWriter
    {
        // Archive order of 4_2-FrameG.frd (FrameG writes its file list in reverse).
        internal static readonly string[] EntryOrder =
        {
            "PickUpName.tmp", "PickUp.tmp", "K_Ressha_S.tmp", "K_JituBuzai.tmp", "Ku_MixName.tmp", "Ku_Mix.tmp",
            "Ku_DefNo.tmp", "Ku_Define2.tmp", "Ku_ComNa.tmp", "Ku_Combine.tmp", "Kajyu.tmp", "Chakumoku.tmp",
            "B_Ketugou.tmp", "B_Bane.tmp", "Buzai_G.tmp", "Buzai.tmp", "$DatVer.TXT", "$9.txt", "$8.txt",
            "$7.txt", "$6.txt", "$5.txt", "$4.txt", "$3.txt", "$22.txt", "$21.txt", "$20.txt", "$2.txt",
            "$19.txt", "$18.txt", "$17.txt", "$16.txt", "$15.txt", "$14.txt", "$13.txt", "$12.txt", "$11.txt",
            "$10.txt", "$1.txt", "frvlist.txt", "KFrame.ini"
        };

        // Tables that only hold pile data, print settings FrameWeb lacks, or nothing: written empty.
        private static readonly string[] EmptyTables =
        {
            "$4.txt", "$5.txt", "$6.txt", "$7.txt", "$8.txt", "$12.txt", "$13.txt", "$14.txt", "$15.txt",
            "$19.txt", "$20.txt", "$21.txt", "$22.txt"
        };

        internal const int MemberRecord = 244;
        internal const int RigidRecord = 56;
        internal const int SpringRecord = 323;
        internal const int JointRecord = 99;
        internal const int NoticeRecord = 1053;
        internal const int LoadNameRecord = 88;
        internal const int LoadRecord = 124;
        internal const int MixNameRecord = 63;
        internal const int MixRecord = 370;
        internal const int DefineNumberRecord = 10;
        internal const int DefineRecord = 180;
        internal const int CombineRecord = 370;
        internal const int CombineNameRecord = 45;
        internal const int PickupRecord = 400;
        internal const int PickupNameRecord = 45;
        internal const int TrainRecord = 168;

        private const string Rate = " 1.0000";
        private const string Percent = "100 %";

        private readonly FrdModel model;
        private readonly FrdDiagnosticList diagnostics;

        private FrdFileWriter(FrdModel model, FrdDiagnosticList diagnostics)
        {
            this.model = model;
            this.diagnostics = diagnostics;
        }

        public static List<LzhEntry> Write(FrdModel model, FrdDiagnosticList diagnostics) =>
            new FrdFileWriter(model, diagnostics).Write();

        private List<LzhEntry> Write()
        {
            var files = new Dictionary<string, byte[]>
            {
                ["$1.txt"] = Nodes(),
                ["$2.txt"] = SupportNodes(),
                ["$3.txt"] = SupportValues(),
                ["$9.txt"] = Encode(FrdTemplates.Settings9),
                ["$10.txt"] = MemberList(row => $"{row}\t{Percent}\t0"),
                ["$11.txt"] = Encode(FrdTemplates.Settings11(model.Title)),
                ["$16.txt"] = Encode(FrdTemplates.Settings16),
                ["$17.txt"] = MemberList(row => $"{row}\t{Percent}  \t{Percent}  \t"),
                ["$18.txt"] = MemberList(row => $"{row}\t\t0"),
                ["$DatVer.TXT"] = Encode(FrdTemplates.DataVersion),
                ["frvlist.txt"] = Encode(FrdTemplates.FrameSettings),
                ["KFrame.ini"] = Encode(FrdTemplates.FrameIni),
                ["Buzai.tmp"] = Members(),
                ["Buzai_G.tmp"] = RigidZones(),
                ["B_Bane.tmp"] = Springs(),
                ["B_Ketugou.tmp"] = Joints(),
                ["Chakumoku.tmp"] = NoticePoints(),
                ["Kajyu.tmp"] = LoadNames(),
                ["K_JituBuzai.tmp"] = Loads(),
                ["Ku_MixName.tmp"] = Blank(MixNameRecord),
                ["Ku_Mix.tmp"] = Blank(MixRecord),
                ["Ku_DefNo.tmp"] = DefineNumbers(),
                ["Ku_Define2.tmp"] = Defines(),
                ["Ku_Combine.tmp"] = Combines(),
                ["Ku_ComNa.tmp"] = CombineNames(),
                ["PickUp.tmp"] = Pickups(),
                ["PickUpName.tmp"] = PickupNames(),
                ["K_Ressha_S.tmp"] = Blank(TrainRecord),
            };
            foreach (string name in EmptyTables)
                files[name] = Encode("0\r\n");
            return EntryOrder.Select(name => new LzhEntry(name, files[name])).ToList();
        }

        private static byte[] Encode(string text) => FrdText.ShiftJis.GetBytes(text);

        private static byte[] Table(IReadOnlyCollection<string> rows)
        {
            var text = new StringBuilder();
            text.Append(rows.Count).Append("\r\n");
            foreach (string row in rows)
                text.Append(row).Append("\r\n");
            return Encode(text.ToString());
        }

        private static byte[] Blank(int recordLength) => Encode(new string(' ', recordLength));

        private static byte[] Records(IEnumerable<FrdRecord> records, int recordLength)
        {
            var output = new List<byte>();
            foreach (FrdRecord record in records)
                record.AppendTo(output, recordLength);
            // FrameG keeps one blank record in a table with no rows.
            return output.Count == 0 ? Blank(recordLength) : output.ToArray();
        }

        // A number fitting maxChars; null (with an error) when even one significant digit does not fit.
        private string Fit(double value, int maxChars, bool signSpace, string label)
        {
            string text = FrdText.FitNumber(value, maxChars, signSpace, out bool reduced);
            if (text == null)
                diagnostics.Error(FrdDiagnosticCodes.FieldOverflow, label, $"{FrdText.Number(value)} は FrameG の欄 ({maxChars} 文字) に収まりません。");
            else if (reduced)
                diagnostics.WarningOnce(FrdDiagnosticCodes.PrecisionReduced, label,
                    $"FrameG の欄 ({maxChars} 文字) に収めるため {FrdText.Number(value)} を {text.Trim()} に丸めました。");
            return text ?? "";
        }

        private string Optional(double? value, int maxChars, bool signSpace, string label) =>
            value == null || value == 0 ? "" : Fit(value.Value, maxChars, signSpace, label);

        private static string FitText(string text, int width) =>
            FrdText.ByteCount(text) <= width ? text : FrdText.Truncate(text, width);

        private byte[] Nodes() =>
            Table(model.Nodes.Select(node => $"{node.Number}\t{FrdText.Number(node.X)} \t{FrdText.Number(node.Y)} ").ToList());

        // FrameG writes "11 " here; without the space, ConvertManager (which does not trim it) reads a clean ID.
        private byte[] SupportNodes() =>
            Table(model.Supports.Select((support, index) => $"{index + 1}\t{support.Node}\t").ToList());

        private byte[] SupportValues() =>
            Table(model.Supports.Select((support, index) =>
            {
                var cells = new List<string> { FrdText.Integer(index + 1) };
                for (int sheet = 0; sheet < FrdModel.ConditionSheets; sheet++)
                {
                    double[] values = support.Values[sheet] ?? new double[3];
                    foreach (double value in values)
                        cells.Add(model.SupportSheetsUsed[sheet + 1] ? FrdText.Number(value) + " " : "");
                }
                return string.Join("\t", cells);
            }).ToList());

        private byte[] MemberList(System.Func<string, string> columns) =>
            Table(model.Members.Select((member, index) => columns($"{index + 1}\t{member.Number}\t{member.Name}")).ToList());

        private byte[] Members() =>
            Records(model.Members.Select(member =>
            {
                string label = "member." + member.Number;
                var record = new FrdRecord()
                    .Field(FrdText.Integer(member.Number), 3)
                    .Field(FrdText.Integer(member.I), 10)
                    .Field(FrdText.Integer(member.J), 10)
                    .Field(member.Name, 32)
                    .Blank(10)                                      // count n: blank reads as 1
                    .Field(Optional(member.E, 10, false, label + ".E"), 10)
                    .Field(Optional(member.Xp, 10, false, label + ".Xp"), 10)
                    .Blank(5);                                      // FrameG's member mark
                for (int sheet = 0; sheet < FrdModel.MaterialSheets; sheet++)
                    record.Field(Optional(member.A[sheet], 11, false, $"{label}.A"), 11);
                for (int sheet = 0; sheet < FrdModel.MaterialSheets; sheet++)
                    record.Field(Optional(member.Iz[sheet], 13, false, $"{label}.Iz"), 13);
                return record.Field(Length(member.Length), 10);
            }), MemberRecord);

        // FrameG prints the member length like VB's Format("#.###"): ".1", "2.29", "23.95".
        private static string Length(double length)
        {
            string text = System.Math.Round(length, 3).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            return text.StartsWith("0.") ? text.Substring(1) : text;
        }

        // One record per member, by position: record k belongs to member k.
        private byte[] RigidZones() =>
            Records(model.Members.Select(member =>
            {
                var record = new FrdRecord();
                if (member.Rigid == null)
                    return record.Blank(RigidRecord);
                string label = "rigid." + member.Number;
                return record
                    .Field(Fit(member.Rigid.JLength, 14, false, label + ".Jlength"), 14)
                    .Field(Fit(member.Rigid.ILength, 14, false, label + ".Ilength"), 14)
                    .Field(Fit(member.Rigid.A, 14, false, label + ".A"), 14)
                    .Field(Fit(member.Rigid.I, 14, false, label + ".I"), 14);
            }), RigidRecord);

        private byte[] Springs() =>
            Records(model.Springs.Select(spring =>
            {
                string label = "fix_member." + spring.Member;
                var record = new FrdRecord().Field(FrdText.Integer(spring.Member), 3);
                for (int sheet = 0; sheet < FrdModel.ConditionSheets; sheet++)
                    record.Field(Optional(spring.Transverse[sheet], 10, false, label + ".ty"), 10);
                for (int sheet = 0; sheet < FrdModel.ConditionSheets; sheet++)
                    record.Field(Optional(spring.Axial[sheet], 10, false, label + ".tx"), 10);
                return record;
            }), SpringRecord);

        private byte[] Joints() =>
            Records(model.Joints.Select(joint =>
            {
                var record = new FrdRecord().Field(FrdText.Integer(joint.Member), 3);
                foreach (int[] flags in joint.Flags)
                    // x and y are always connected in 2D: "11" + zi + "11" + zj.
                    record.Field(flags == null ? "" : $"11{flags[0]}11{flags[1]}", 6);
                return record;
            }), JointRecord);

        private byte[] NoticePoints() =>
            Records(model.NoticePoints.Select(points =>
            {
                string label = "notice_points." + points.Member;
                var record = new FrdRecord().Field(FrdText.Integer(points.Member), 4);
                for (int i = 0; i < FrdLimits.NoticePoints; i++)
                    record.Field(i < points.Points.Count ? Fit(points.Points[i], 9, false, label) : "", 9);
                return record.Blank(NoticeRecord - 4 - 9 * FrdLimits.NoticePoints); // point labels
            }), NoticeRecord);

        private byte[] LoadNames() =>
            Records(model.LoadCases.Select(loadCase => new FrdRecord()
                .Field(Rate, 10)
                .Field(FrdText.Integer(loadCase.Number), 4)
                .Field(loadCase.Symbol, 5)
                .Field(loadCase.Name, 41)
                .Field(FrdText.Integer(loadCase.FixNode), 3)
                .Field(FrdText.Integer(loadCase.Element), 3)
                .Field(FrdText.Integer(loadCase.FixMember), 3)
                .Field(FrdText.Integer(loadCase.Joint), 3)
                .Raw(0)                                                 // the 0x00 FrameG writes after the selectors
                .Blank(LoadNameRecord - 73)), LoadNameRecord);

        // Each case: its rows in input order, then one blank record. Even cases end with "1" (as FrameG writes).
        private byte[] Loads() =>
            Records(model.LoadCases.SelectMany(loadCase =>
                loadCase.Records.Append(new FrdLoadRecord()).Select(item => LoadRecordOf(loadCase, item))), LoadRecord);

        private FrdRecord LoadRecordOf(FrdLoadCase loadCase, FrdLoadRecord item)
        {
            string label = "load." + loadCase.Number;
            var record = new FrdRecord().Field(FrdText.Integer(loadCase.Number), 3);
            FrdMemberLoad member = item.Member;
            if (member == null)
                record.Blank(3 + 3 + 12 * 6);
            else
                record
                    .Field(FitText(member.M1, 3) == member.M1 ? member.M1 : Overflow(member.M1, 3, label + ".m1"), 3)
                    .Field(FitText(member.M2, 3) == member.M2 ? member.M2 : Overflow(member.M2, 3, label + ".m2"), 3)
                    .Field(FrdText.Integer(member.Mark), 12)
                    .Field(DistanceText(member.L1, label + ".L1"), 12)
                    .Field(DistanceText(member.L2, label + ".L2"), 12)
                    .Field(Fit(member.P1, 12, true, label + ".P1"), 12)
                    .Field(Fit(member.P2, 12, true, label + ".P2"), 12)
                    .Field(member.Direction, 12);
            FrdNodeLoad node = item.Node;
            if (node == null)
                record.Blank(3 + 12 * 3);
            else
                record
                    .Field(FrdText.Integer(node.Node), 3)
                    .Field(Fit(node.Tx, 12, true, label + ".tx"), 12)
                    // The forward reader takes 11 bytes for ty, so ty must leave the 12th byte blank.
                    .Field(Fit(node.Ty, 11, true, label + ".ty"), 12)
                    .Field(Fit(node.Rz, 12, true, label + ".rz"), 12);
            return record.Field(loadCase.Number % 2 == 0 ? "1" : "", 4);
        }

        private string DistanceText(string text, string label)
        {
            if (text.Length == 0)
                return "";
            string value = text.StartsWith("-") ? text : " " + text;
            return value.Length <= 12 ? value : Overflow(text, 12, label);
        }

        private string Overflow(string text, int width, string label)
        {
            diagnostics.Error(FrdDiagnosticCodes.FieldOverflow, label, $"{text} は FrameG の欄 ({width} 文字) に収まりません。");
            return "";
        }

        private byte[] DefineNumbers() =>
            Records(model.Defines.Select(define => new FrdRecord().Field(FrdText.Integer(define.Number), DefineNumberRecord)),
                DefineNumberRecord);

        private byte[] Defines() =>
            Records(model.Defines.Select(define =>
            {
                var record = new FrdRecord();
                foreach (int? value in define.Cases)
                    record.Field(value == null ? "" : FrdText.Integer(value.Value), 6);
                return record;
            }), DefineRecord);

        private byte[] Combines() =>
            Records(model.Combines.Select((combine, index) =>
            {
                string label = "combine." + (index + 1);
                var record = new FrdRecord().Field(Rate, 10);
                for (int i = 0; i < FrdLimits.CombineTerms; i++)
                    record.Field(i < combine.Terms.Count ? Fit(combine.Terms[i].Value, 8, true, label) : "", 8);
                for (int i = 0; i < FrdLimits.CombineTerms; i++)
                    record.Field(i < combine.Terms.Count ? FrdText.Integer(combine.Terms[i].Key) : "", 4);
                return record;
            }), CombineRecord);

        private byte[] CombineNames() =>
            Records(model.Combines.Select(combine => new FrdRecord().Blank(5).Field(combine.Name, 40)), CombineNameRecord);

        private byte[] Pickups() =>
            Records(model.Pickups.Select(pickup =>
            {
                var record = new FrdRecord();
                foreach (int? combine in pickup.Combines)
                    record.Field(combine == null ? "" : " " + FrdText.Integer(combine.Value), 4);
                return record;
            }), PickupRecord);

        private byte[] PickupNames() =>
            Records(model.Pickups.Select(pickup => new FrdRecord().Blank(5).Field(pickup.Name, 40)), PickupNameRecord);
    }
}

using System.Collections.Generic;

namespace Convert_Manager.FrameG
{
    // The model as FrameG stores it: contiguous numbers from 1, one material per member, 2D only.

    internal sealed class FrdModel
    {
        public const int MaterialSheets = 6;
        public const int ConditionSheets = 16;

        public string Title = "";
        public List<FrdNode> Nodes = new List<FrdNode>();
        public List<FrdMember> Members = new List<FrdMember>();
        public List<FrdSupport> Supports = new List<FrdSupport>();
        /// <summary>Support sheets (1-based) that have at least one row; only these are written to $3.txt.</summary>
        public bool[] SupportSheetsUsed = new bool[ConditionSheets + 1];
        public List<FrdSpring> Springs = new List<FrdSpring>();
        public List<FrdJoint> Joints = new List<FrdJoint>();
        public List<FrdNoticePoints> NoticePoints = new List<FrdNoticePoints>();
        public List<FrdLoadCase> LoadCases = new List<FrdLoadCase>();
        public List<FrdDefine> Defines = new List<FrdDefine>();
        public List<FrdCombine> Combines = new List<FrdCombine>();
        public List<FrdPickup> Pickups = new List<FrdPickup>();

        public Dictionary<string, int> NodeNumbers = new Dictionary<string, int>();
        public Dictionary<string, int> MemberNumbers = new Dictionary<string, int>();
        public Dictionary<string, int> LoadCaseNumbers = new Dictionary<string, int>();
    }

    internal sealed class FrdNode
    {
        public int Number;
        public double X;
        public double Y;
    }

    internal sealed class FrdMember
    {
        public int Number;
        public int I;
        public int J;
        public string Name = "";
        public double E;
        public double Xp;
        /// <summary>Area per material sheet (index 0 is sheet 1); null where the sheet lacks the material.</summary>
        public double?[] A = new double?[FrdModel.MaterialSheets];
        public double?[] Iz = new double?[FrdModel.MaterialSheets];
        public double Length;
        public FrdRigid Rigid;
    }

    internal sealed class FrdRigid
    {
        public double ILength;
        public double JLength;
        public double A;
        public double I;
    }

    internal sealed class FrdSupport
    {
        public int Node;
        /// <summary>(tx, ty, rz) per sheet (index 0 is sheet 1); null where the sheet has no row for the node.</summary>
        public double[][] Values = new double[FrdModel.ConditionSheets][];
    }

    internal sealed class FrdSpring
    {
        public int Member;
        /// <summary>Transverse (ty) and axial (tx) stiffness per sheet; null where the sheet has no row.</summary>
        public double?[] Transverse = new double?[FrdModel.ConditionSheets];
        public double?[] Axial = new double?[FrdModel.ConditionSheets];
    }

    internal sealed class FrdJoint
    {
        public int Member;
        /// <summary>(zi, zj) per sheet, 1 connected and 0 released; null where the sheet has no row.</summary>
        public int[][] Flags = new int[FrdModel.ConditionSheets][];
    }

    internal sealed class FrdNoticePoints
    {
        public int Member;
        public List<double> Points = new List<double>();
    }

    internal sealed class FrdLoadCase
    {
        public int Number;
        public string Symbol = "";
        public string Name = "";
        public int FixNode = 1;
        public int Element = 1;
        public int FixMember = 1;
        public int Joint = 1;
        /// <summary>One record per FrameWeb input row: Records[r - 1] holds row r.</summary>
        public List<FrdLoadRecord> Records = new List<FrdLoadRecord>();
    }

    internal sealed class FrdLoadRecord
    {
        public FrdMemberLoad Member;
        public FrdNodeLoad Node;
    }

    internal sealed class FrdMemberLoad
    {
        public string M1 = "";
        public string M2 = "";
        public int Mark;
        public string L1 = "";
        public string L2 = "";
        public double P1;
        public double P2;
        public string Direction = "";
    }

    internal sealed class FrdNodeLoad
    {
        public int Node;
        public double Tx;
        public double Ty;
        public double Rz;
    }

    internal sealed class FrdDefine
    {
        public int Number;
        /// <summary>Signed FrameG load case numbers by column (index 0 is C1); null where empty.</summary>
        public int?[] Cases = new int?[FrdLimits.DefineColumns];
    }

    internal sealed class FrdCombine
    {
        public string Name = "";
        public List<KeyValuePair<int, double>> Terms = new List<KeyValuePair<int, double>>();
    }

    internal sealed class FrdPickup
    {
        public string Name = "";
        /// <summary>Combine numbers by column (index 0 is C1); null where empty.</summary>
        public int?[] Combines = new int?[FrdLimits.PickupColumns];
    }

    internal static class FrdLimits
    {
        public const int MaxNumber = 999;          // 3-byte number fields
        public const int DefineColumns = 30;
        public const int CombineTerms = 30;
        public const int PickupColumns = 100;
        public const int NoticePoints = 50;
        public const int TitleBytes = 64;
        public const int MemberNameBytes = 30;
        public const int SymbolBytes = 5;
        public const int LoadNameBytes = 41;
        public const int CombineNameBytes = 40;
    }
}

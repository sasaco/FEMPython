namespace Convert_Manager.FrameG
{
    /// <summary>
    /// Files the forward converter does not read, copied from 4_2-FrameG.frd (FrameG 4.6.2, the only
    /// sample without piles). They hold FrameG's version, program and print settings; only the title
    /// line of $11.txt depends on the model.
    /// </summary>
    internal static class FrdTemplates
    {
        public const string DataVersion = "4.0602\r\n";

        public const string FrameIni =
            "[Frame]\r\n" +
            "CombineTypeIsNew=1\r\n";

        public const string FrameSettings =
            "[FRAME]\r\n" +
            "UNIT=1\r\n" +
            "KHEN0=         \r\n" +
            "KHEN1=         \r\n" +
            "KHEN2=         \r\n" +
            "KHEN3= 3\r\n" +
            "PRIMODE= 0\r\n" +
            "SHUKEI0= 0\r\n" +
            "SHUKEI1= 0\r\n" +
            "DANSEIuz= 0\r\n" +
            "着目FLG= 0\r\n" +
            "英語FLG= 0\r\n" +
            "momentMax= 1\r\n" +
            "反曲点FLG= 0\r\n" +
            "Pick軸力FLG= 0\r\n" +
            "GGG_FLG= 1\r\n" +
            "CALCMODE=1\r\n" +
            "PrgVer=4.6.2\r\n" +
            "DatVer=4.6.2\r\n";

        public const string Settings9 =
            "17\r\n" +
            "1\t1\t0\t1\t1\t0\r\n" +
            "2\t-1\t0\t0\t0\r\n" +
            "3\t-1\t0\r\n" +
            "4\t-1\t0\t0\r\n" +
            "5\t\t   1.0\r\n" +
            "6\t\t   1.0\r\n" +
            "7\t\t   1.0\r\n" +
            "8\t\t   1.0\r\n" +
            "9\t  自動\t  自動\r\n" +
            "10\t\r\n" +
            "11\t0\t0\r\n" +
            "12\t0\r\n" +
            "13\tC:\\WinFrameG\\data\\Dxf\\\r\n" +
            "14\t1\r\n" +
            "15\t1\r\n" +
            "16\t0\r\n" +
            "17\t1\r\n";

        public const string Settings16 =
            "17\r\n" +
            "1\t111\r\n" +
            "2\t0\r\n" +
            "3\t0\r\n" +
            "4\t1.2\r\n" +
            "5\t0\t0\t\r\n" +
            "6\t\t\t\r\n" +
            "7\t0\t0\t\r\n" +
            "8\t\t   -20 \t\r\n" +
            "9\t\t\t\t\t\t\r\n" +
            "10\t\r\n" +
            "11\t0\r\n" +
            "12\t0\r\n" +
            "13\tC:\\WinFrameG\\data\\Dxf\\\r\n" +
            "14\t1\r\n" +
            "15\t1\r\n" +
            "16\t0\r\n" +
            "17\t1\r\n" +
            "18\t2\r\n";

        /// <summary>$11.txt; its fourth line (record 3) is the bare title.</summary>
        public static string Settings11(string title) =>
            "10\r\n" +
            "1\t1\t0\t0\t1\r\n" +
            "2\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0\r\n" +
            title + "\r\n" +
            "4\t0\t0\t0\t0\t0\t0\t0\t0\t1\r\n" +
            "5\t0\t-1\t0\t0\t0\t0\t0\t0\r\n" +
            "6\t2\r\n" +
            "7\t0\t0\t-1\r\n" +
            "8\t-1\t0\r\n" +
            "9\t0\t0\t1\r\n" +
            "10\t1\t999\t1\t1\r\n";
    }
}

using System.Globalization;
using System.Text;
using Convert_Manager.FrameG;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Convert_Manager.Tests;

public class FrdExporterTests
{
    internal static readonly DateTimeOffset Time = new(2026, 10, 22, 9, 0, 0, TimeSpan.FromHours(9));

    // A 2D model in the format FrameWebforCS saves, using every section the exporter writes.
    internal const string ModelJson = """
    {
      "dimension": 2,
      "three": { "camera": { "x": 0, "y": 0, "z": 50 } },
      "node": {
        "1": { "x": 0, "y": 0, "z": null },
        "2": { "x": 0, "y": 3, "z": null },
        "3": { "x": 4, "y": 3, "z": null },
        "4": { "x": 4, "y": 0, "z": null }
      },
      "member": {
        "1": { "ni": "1", "nj": "2", "e": "1", "cg": null },
        "2": { "ni": "2", "nj": "3", "e": "2", "cg": null },
        "3": { "ni": "3", "nj": "4", "e": "1", "cg": null }
      },
      "shell": {},
      "rigid": [ { "m": "2", "Ilength": 0.3, "Jlength": 0.25, "e": 3 } ],
      "element": {
        "1": {
          "1": { "E": 25000000, "G": null, "Xp": 1E-05, "A": 0.5, "J": null, "Iy": null, "Iz": 0.0417, "n": "柱" },
          "2": { "E": 25000000, "G": null, "Xp": 1E-05, "A": 0.6, "J": null, "Iy": null, "Iz": 0.05, "n": "梁" },
          "3": { "E": 25000000, "G": null, "Xp": 1E-05, "A": 1000, "J": null, "Iy": null, "Iz": 1000, "n": "剛域" }
        },
        "2": {
          "1": { "E": 25000000, "G": null, "Xp": 1E-05, "A": 0.45, "J": null, "Iy": null, "Iz": 0.04, "n": "柱" },
          "2": { "E": 25000000, "G": null, "Xp": 1E-05, "A": 0.55, "J": null, "Iy": null, "Iz": 0.045, "n": "梁" }
        }
      },
      "fix_node": {
        "1": [ { "row": 1, "n": "1", "tx": 1, "ty": 1, "tz": null, "rx": null, "ry": null, "rz": 1 },
               { "row": 2, "n": "4", "tx": 0, "ty": 1, "tz": null, "rx": null, "ry": null, "rz": 0 } ],
        "2": [ { "row": 1, "n": "1", "tx": 1, "ty": 1, "rz": 0 },
               { "row": 2, "n": "4", "tx": 5000, "ty": 1, "rz": 0 } ],
        "3": []
      },
      "fix_member": { "1": [ { "row": 1, "m": "2", "tx": 100, "ty": 2000, "tz": null, "tr": null } ] },
      "joint": { "1": [ { "row": 1, "m": "2", "xi": 1, "yi": 1, "zi": 0, "xj": 1, "yj": 1, "zj": 1 } ] },
      "notice_points": [ { "row": 1, "m": "1", "Points": [ 1.5, 3 ] }, { "row": 2, "m": "2", "Points": [ 2 ] } ],
      "load": {
        "1": { "fix_node": 1, "fix_member": 1, "element": 1, "joint": 1, "symbol": "D", "LL_pitch": null, "name": "死荷重",
          "load_node": [ { "row": 2, "n": "2", "tx": 10, "ty": -20, "tz": null, "rx": null, "ry": null, "rz": 5 } ],
          "load_member": [
            { "row": 1, "m1": "2", "m2": "2", "direction": "gy", "mark": "2", "L1": "0", "L2": "0", "P1": -10, "P2": -10 },
            { "row": 3, "m1": "1", "m2": "3", "direction": "y", "mark": "1", "L1": "1.5", "L2": "0", "P1": 30, "P2": null } ] },
        "2": { "fix_node": 2, "element": 2, "symbol": "T", "name": "温度",
          "load_member": [
            { "row": 1, "m1": "1", "m2": "3", "direction": "x", "mark": "9", "L1": null, "L2": null, "P1": 10, "P2": null },
            { "row": 2, "m1": "2", "m2": null, "direction": "x", "mark": "2", "L1": "-0", "L2": "-1.5", "P1": 3, "P2": 3 },
            { "row": 3, "m1": "1", "m2": null, "direction": "z", "mark": "11", "L1": "1", "L2": null, "P1": 50, "P2": null } ] },
        "3": { "symbol": "H", "name": "水平",
          "load_member": [ { "row": 1, "m1": "1", "m2": "-3", "direction": "gx", "mark": "2", "L1": "0", "L2": "0", "P1": 7, "P2": 7 } ] }
      },
      "bridge_loads": { "version": 1, "panels": [], "paths": [], "cases": {} },
      "define": { "1": { "row": 1, "C1": 1, "C2": 2 }, "2": { "row": 2, "C1": 3, "C2": 0 } },
      "combine": { "1": { "row": 1, "name": "基本", "C1": 1.1, "C2": 1 }, "2": { "row": 2, "name": "地震", "C2": 1 } },
      "pickup": { "1": { "row": 1, "name": "最大", "C1": 1, "C2": 2 } }
    }
    """;

    internal static JObject Model() => JObject.Parse(ModelJson);

    internal static FrdExportResult Export(JObject model, string title = "test") =>
        FrdExporter.Export(model.ToString(), new FrdExportOptions { Title = title, Time = Time });

    internal static byte[] Succeed(JObject model, string title = "test")
    {
        FrdExportResult result = Export(model, title);
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result.Frd;
    }

    internal static JObject Forward(byte[] frd)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return JObject.Parse(new ConvertManager(new MemoryStream(frd)).getJsonString());
    }

    // Extracts with SevenZipExtractor and checks every entry against the CRC in its header.
    internal static Dictionary<string, byte[]> Files(byte[] frd)
    {
        var files = LzhArchiveWriterTests.Extract(frd);
        foreach (var header in LzhTestReader.Read(frd))
            Assert.Equal(header.Crc, LzhTestReader.Crc16(files[header.Name]));
        return files;
    }

    private static string Sjis(byte[] bytes) => Encoding.GetEncoding(932).GetString(bytes);

    [Fact]
    public void TheForwardReaderReadsBackWhatWasExported()
    {
        JObject json = Forward(Succeed(Model()));

        Assert.Equal(new[] { "1", "2", "3", "4" }, ((JObject)json["node"]!).Properties().Select(p => p.Name));
        Assert.Equal(4.0, (double)json["node"]!["3"]!["x"]!);
        Assert.Equal(3.0, (double)json["node"]!["3"]!["y"]!);
        Assert.Equal("3", (string)json["member"]!["3"]!["ni"]!);
        Assert.Equal("4", (string)json["member"]!["3"]!["nj"]!);
        Assert.Equal("3", (string)json["member"]!["3"]!["e"]!);

        // The shared material 1 becomes one material per member; A and Iz stay per sheet.
        var sheet1 = json["element"]!["1"]!;
        var sheet2 = json["element"]!["2"]!;
        Assert.Equal(25000000.0, (double)sheet1["3"]!["E"]!);
        Assert.Equal(1e-5, (double)sheet1["3"]!["Xp"]!);
        Assert.Equal(0.5, (double)sheet1["3"]!["A"]!);
        Assert.Equal(0.0417, (double)sheet1["3"]!["Iz"]!);
        Assert.Equal(0.45, (double)sheet2["3"]!["A"]!);
        Assert.Equal(0.045, (double)sheet2["2"]!["Iz"]!);
        Assert.Equal("柱", (string)sheet1["1"]!["name"]!);
        Assert.Equal("梁", (string)sheet1["2"]!["name"]!);

        var rigid = Assert.Single((JArray)json["rigid"]!);
        Assert.Equal("2", (string)rigid["m"]!);
        Assert.Equal(0.3, (double)rigid["Ilength"]!);
        Assert.Equal(0.25, (double)rigid["Jlength"]!);
        string rigidMaterial = (string)rigid["e"]!;
        Assert.Equal(1000.0, (double)sheet1[rigidMaterial]!["A"]!);
        Assert.Equal(1000.0, (double)sheet2[rigidMaterial]!["Iz"]!);

        var supports = (JArray)json["fix_node"]!["2"]!;
        Assert.Equal(new[] { "1", "4" }, supports.Select(row => ((string)row["n"]!).Trim()));
        Assert.Equal(5000.0, (double)supports[1]["tx"]!);
        Assert.Equal(new[] { 1.0, 1.0, 1.0 }, new[] { "tx", "ty", "rz" }.Select(key => (double)json["fix_node"]!["1"]![0]![key]!));
        Assert.Empty((JArray)json["fix_node"]!["3"]!);

        var spring = Assert.Single((JArray)json["fix_member"]!["1"]!);
        Assert.Equal(("2", 100.0, 2000.0), ((string)spring["m"]!, (double)spring["tx"]!, (double)spring["ty"]!));
        var joint = Assert.Single((JArray)json["joint"]!["1"]!);
        Assert.Equal(("2", 0, 1), ((string)joint["m"]!, (int)joint["zi"]!, (int)joint["zj"]!));

        var points = (JArray)json["notice_points"]!;
        Assert.Equal(new[] { "1", "2" }, points.Select(row => (string)row["m"]!));
        Assert.Equal(new[] { 1.5, 3 }, points[0]["Points"]!.Select(value => (double)value));

        var dead = json["load"]!["1"]!;
        Assert.Equal(("D", "死荷重", 1, 1, 1, 1), ((string)dead["symbol"]!, (string)dead["name"]!, (int)dead["fix_node"]!,
            (int)dead["element"]!, (int)dead["fix_member"]!, (int)dead["joint"]!));
        var deadMembers = (JArray)dead["load_member"]!;
        Assert.Equal(("gy", "2", -10.0, -10.0), ((string)deadMembers[0]["direction"]!, (string)deadMembers[0]["mark"]!,
            (double)deadMembers[0]["P1"]!, (double)deadMembers[0]["P2"]!));
        Assert.Equal(("y", "1", "1", "3", "1.5", 30.0), ((string)deadMembers[1]["direction"]!, (string)deadMembers[1]["mark"]!,
            (string)deadMembers[1]["m1"]!, (string)deadMembers[1]["m2"]!, (string)deadMembers[1]["L1"]!, (double)deadMembers[1]["P1"]!));
        var node = Assert.Single((JArray)dead["load_node"]!);
        Assert.Equal(("2", 10.0, -20.0, 5.0), ((string)node["n"]!, (double)node["tx"]!, (double)node["ty"]!, (double)node["rz"]!));

        var temperature = json["load"]!["2"]!;
        Assert.Equal((2, 2), ((int)temperature["fix_node"]!, (int)temperature["element"]!));
        var rows = (JArray)temperature["load_member"]!;
        Assert.Equal(("9", "", 10.0), ((string)rows[0]["mark"]!, (string)rows[0]["direction"]!, (double)rows[0]["P1"]!));
        Assert.Equal(("2", "x", "-0", -1.5, 3.0), ((string)rows[1]["mark"]!, (string)rows[1]["direction"]!,
            (string)rows[1]["L1"]!, (double)rows[1]["L2"]!, (double)rows[1]["P2"]!));
        Assert.Equal(("11", "z", 50.0), ((string)rows[2]["mark"]!, (string)rows[2]["direction"]!, (double)rows[2]["P1"]!));

        var horizontal = Assert.Single((JArray)json["load"]!["3"]!["load_member"]!);
        Assert.Equal(("gx", "-3", 7.0), ((string)horizontal["direction"]!, (string)horizontal["m2"]!, (double)horizontal["P1"]!));

        Assert.Equal(new JObject { ["row"] = 1, ["C1"] = 1, ["C2"] = 2 }, json["define"]!["1"]!, JToken.EqualityComparer);
        Assert.Equal(new JObject { ["row"] = 2, ["C1"] = 3, ["C2"] = 0 }, json["define"]!["2"]!, JToken.EqualityComparer);
        Assert.Equal(new JObject { ["row"] = 1, ["name"] = "基本", ["C1"] = 1.1, ["C2"] = 1.0 }, json["combine"]!["1"]!, JToken.EqualityComparer);
        Assert.Equal(new JObject { ["row"] = 1, ["name"] = "最大", ["C1"] = 1, ["C2"] = 2 }, json["pickup"]!["1"]!, JToken.EqualityComparer);
    }

    [Fact]
    public void EveryArchiveEntryMatchesItsCrcAndTheSampleOrder()
    {
        var files = Files(Succeed(Model()));
        var sample = LzhArchiveWriterTests.Extract(SampleBytes("4_2-FrameG.frd"));

        Assert.Equal(sample.Keys, files.Keys);
    }

    [Fact]
    public void TheSameInputGivesTheSameBytes()
    {
        Assert.Equal(Succeed(Model()), Succeed(Model()));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void OutputDoesNotDependOnTheCurrentCulture(string culture)
    {
        byte[] expected = Succeed(Model());
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            Assert.Equal(expected, Succeed(Model()));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void FilesNotReadByTheConverterMatchTheSample()
    {
        var files = Files(Succeed(Model(), "4_2-FrameG"));
        var sample = LzhArchiveWriterTests.Extract(SampleBytes("4_2-FrameG.frd"));

        foreach (string name in new[] { "$DatVer.TXT", "frvlist.txt", "KFrame.ini", "$9.txt", "$11.txt", "$16.txt",
                     "$4.txt", "$5.txt", "$6.txt", "$7.txt", "$8.txt", "$12.txt", "$13.txt", "$14.txt", "$15.txt",
                     "$19.txt", "$20.txt", "$21.txt", "$22.txt", "Ku_Mix.tmp", "Ku_MixName.tmp", "K_Ressha_S.tmp" })
            Assert.True(sample[name].SequenceEqual(files[name]), name);
    }

    [Fact]
    public void TheTitleReplacesOnlyTheFourthLineOfFile11()
    {
        var files = Files(Succeed(Model(), "門型ラーメン"));
        var sample = LzhArchiveWriterTests.Extract(SampleBytes("4_2-FrameG.frd"));

        string[] lines = Sjis(files["$11.txt"]).Split("\r\n");
        string[] sampleLines = Sjis(sample["$11.txt"]).Split("\r\n");
        Assert.Equal("門型ラーメン", lines[3]);
        Assert.Equal(sampleLines.Where((_, i) => i != 3), lines.Where((_, i) => i != 3));
    }

    [Fact]
    public void FixedWidthRecordsHaveTheLayoutFrameGWrites()
    {
        var files = Files(Succeed(Model()));

        string Pad(string text, int width) => text + new string(' ', width - Encoding.GetEncoding(932).GetByteCount(text));
        string Blank(int width) => new(' ', width);

        Assert.Equal(
            Pad("1", 3) + Pad("1", 10) + Pad("2", 10) + Pad("柱", 32) + Blank(10) + Pad("25000000", 10) + Pad("1E-05", 10) + Blank(5) +
            Pad("0.5", 11) + Pad("0.45", 11) + Blank(11 * 4) + Pad("0.0417", 13) + Pad("0.04", 13) + Blank(13 * 4) + Pad("3", 10),
            Sjis(files["Buzai.tmp"][..244]));
        Assert.Equal(244 * 3, files["Buzai.tmp"].Length);

        Assert.Equal(Blank(56) + Pad("0.25", 14) + Pad("0.3", 14) + Pad("1000", 14) + Pad("1000", 14) + Blank(56),
            Sjis(files["Buzai_G.tmp"]));
        Assert.Equal(Pad("2", 3) + Pad("2000", 10) + Blank(150) + Pad("100", 10) + Blank(150), Sjis(files["B_Bane.tmp"]));
        Assert.Equal(Pad("2", 3) + "110111" + Blank(90), Sjis(files["B_Ketugou.tmp"]));
        Assert.Equal(Pad("1", 4) + Pad("1.5", 9) + Pad("3", 9) + Blank(9 * 48 + 599) +
            Pad("2", 4) + Pad("2", 9) + Blank(9 * 49 + 599), Sjis(files["Chakumoku.tmp"]));

        Assert.Equal("4\r\n1\t0 \t0 \r\n2\t0 \t3 \r\n3\t4 \t3 \r\n4\t4 \t0 \r\n", Sjis(files["$1.txt"]));
        Assert.Equal("2\r\n1\t1\t\r\n2\t4\t\r\n", Sjis(files["$2.txt"]));
        string Supports(string values) => values + string.Concat(Enumerable.Repeat("\t", 48 - 6));
        Assert.Equal("2\r\n" + Supports("1\t1 \t1 \t1 \t1 \t1 \t0 ") + "\r\n" + Supports("2\t0 \t1 \t0 \t5000 \t1 \t0 ") + "\r\n",
            Sjis(files["$3.txt"]));
        Assert.StartsWith("3\r\n1\t1\t柱\t100 %\t0\r\n", Sjis(files["$10.txt"]));
        Assert.StartsWith("3\r\n1\t1\t柱\t100 %  \t100 %  \t\r\n", Sjis(files["$17.txt"]));
        Assert.StartsWith("3\r\n1\t1\t柱\t\t0\r\n", Sjis(files["$18.txt"]));

        byte[] names = files["Kajyu.tmp"];
        Assert.Equal(88 * 3, names.Length);
        Assert.Equal(Pad(" 1.0000", 10) + Pad("1", 4) + Pad("D", 5) + Pad("死荷重", 41) + "1  1  1  1  ",
            Sjis(names[..72]));
        Assert.Equal(0, names[72]);
        Assert.Equal(Blank(15), Sjis(names[73..88]));
        Assert.Equal(Pad(" 1.0000", 10) + Pad("2", 4) + Pad("T", 5) + Pad("温度", 41) + "2  2  1  1  ",
            Sjis(names[88..160]));

        string loads = Sjis(files["K_JituBuzai.tmp"]);
        string NoNode = Blank(3 + 36);
        string NoMember = Blank(3 + 3 + 72);
        string[] records = Enumerable.Range(0, loads.Length / 124).Select(i => loads.Substring(i * 124, 124)).ToArray();
        Assert.Equal(new[]
        {
            // Case 1: member load on row 1, node load on row 2, member load on row 3, then a blank record.
            Pad("1", 3) + Pad("2", 3) + Pad("2", 3) + Pad("2", 12) + Pad(" 0", 12) + Pad(" 0", 12) + Pad(" 10", 12) + Pad(" 10", 12) + Pad("V", 12) + NoNode + Blank(4),
            Pad("1", 3) + NoMember + Pad("2", 3) + Pad(" 10", 12) + Pad(" 20", 12) + Pad("-5", 12) + Blank(4),
            Pad("1", 3) + Pad("1", 3) + Pad("3", 3) + Pad("1", 12) + Pad(" 1.5", 12) + Pad(" 0", 12) + Pad("-30", 12) + Pad(" 0", 12) + Blank(12) + NoNode + Blank(4),
            Pad("1", 3) + NoMember + NoNode + Blank(4),
            // Case 2: even cases end with "1".
            Pad("2", 3) + Pad("1", 3) + Pad("3", 3) + Pad("9", 12) + Blank(24) + Pad(" 10", 12) + Pad(" 0", 12) + Blank(12) + NoNode + Pad("1", 4),
            Pad("2", 3) + Pad("2", 3) + Blank(3) + Pad("6", 12) + Pad("-0", 12) + Pad("-1.5", 12) + Pad(" 3", 12) + Pad(" 3", 12) + Blank(12) + NoNode + Pad("1", 4),
            Pad("2", 3) + Pad("1", 3) + Blank(3) + Pad("11", 12) + Pad(" 1", 12) + Blank(12) + Pad("-50", 12) + Pad(" 0", 12) + Blank(12) + NoNode + Pad("1", 4),
            Pad("2", 3) + NoMember + NoNode + Pad("1", 4),
            // Case 3: a negative m2 is written as a letter code (-3 -> "C").
            Pad("3", 3) + Pad("1", 3) + Pad("C", 3) + Pad("2", 12) + Pad(" 0", 12) + Pad(" 0", 12) + Pad("-7", 12) + Pad("-7", 12) + Pad("H", 12) + NoNode + Blank(4),
            Pad("3", 3) + NoMember + NoNode + Blank(4),
        }, records);

        Assert.Equal(Pad("1", 10) + Pad("2", 10), Sjis(files["Ku_DefNo.tmp"]));
        Assert.Equal(Pad("1", 6) + Pad("2", 6) + Blank(6 * 28) + Pad("3", 6) + Pad("0", 6) + Blank(6 * 28), Sjis(files["Ku_Define2.tmp"]));
        Assert.Equal(Pad(" 1.0000", 10) + Pad(" 1.1", 8) + Pad(" 1", 8) + Blank(8 * 28) + Pad("1", 4) + Pad("2", 4) + Blank(4 * 28) +
            Pad(" 1.0000", 10) + Pad(" 1", 8) + Blank(8 * 29) + Pad("2", 4) + Blank(4 * 29), Sjis(files["Ku_Combine.tmp"]));
        Assert.Equal(Blank(5) + Pad("基本", 40) + Blank(5) + Pad("地震", 40), Sjis(files["Ku_ComNa.tmp"]));
        Assert.Equal(Pad(" 1", 4) + Pad(" 2", 4) + Blank(4 * 98), Sjis(files["PickUp.tmp"]));
        Assert.Equal(Blank(5) + Pad("最大", 40), Sjis(files["PickUpName.tmp"]));
    }

    [Fact]
    public void GappedIdsAreRenumberedTogetherWithEveryReference()
    {
        var model = JObject.Parse("""
        {
          "dimension": 2,
          "node": { "10": { "x": 0, "y": 0 }, "20": { "x": 1, "y": 0 }, "30": { "x": 2, "y": 0 }, "40": { "x": 3, "y": 0 } },
          "member": { "5": { "ni": "10", "nj": "20", "e": "1" }, "7": { "ni": "20", "nj": "30", "e": "1" }, "9": { "ni": "30", "nj": "40", "e": "1" } },
          "element": { "1": { "1": { "E": 1, "Xp": 0, "A": 1, "Iz": 1 } } },
          "fix_node": { "1": [ { "row": 1, "n": "10", "tx": 1, "ty": 1, "rz": 1 } ] },
          "load": { "1": { "load_node": [ { "row": 1, "n": "40", "ty": -1 } ],
            "load_member": [ { "row": 2, "m1": "7", "m2": "-9", "direction": "y", "mark": "2", "L1": "0", "L2": "0", "P1": 1, "P2": 1 } ] } }
        }
        """);

        FrdExportResult result = Export(model);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Equal(new[] { "node", "member" }, result.Warnings.Where(w => w.Code == FrdDiagnosticCodes.Renumbered).Select(w => w.Path));
        Assert.Equal(new Dictionary<string, int> { ["10"] = 1, ["20"] = 2, ["30"] = 3, ["40"] = 4 }, result.NodeNumbers);
        Assert.Equal(new Dictionary<string, int> { ["5"] = 1, ["7"] = 2, ["9"] = 3 }, result.MemberNumbers);
        JObject json = Forward(result.Frd);
        Assert.Equal(("2", "3"), ((string)json["member"]!["2"]!["ni"]!, (string)json["member"]!["2"]!["nj"]!));
        Assert.Equal("1", ((string)json["fix_node"]!["1"]![0]!["n"]!).Trim());
        Assert.Equal("4", (string)json["load"]!["1"]!["load_node"]![0]!["n"]!);
        var row = json["load"]!["1"]!["load_member"]![0]!;
        Assert.Equal(("2", "-3"), ((string)row["m1"]!, (string)row["m2"]!));
    }

    [Theory]
    [InlineData(-5, "E")]
    [InlineData(-10, "A0")]
    [InlineData(-14, "A4")]
    [InlineData(-999, "I99")]
    public void NegativeM2UsesFrameGsLetterCode(int m2, string code)
    {
        var model = new JObject
        {
            ["dimension"] = 2,
            // 999 parallel members between two nodes: the most a 3-byte member number allows.
            ["node"] = JObject.Parse("""{ "1": { "x": 0, "y": 0 }, "2": { "x": 1, "y": 0 } }"""),
            ["member"] = new JObject(Enumerable.Range(1, 999).Select(i => new JProperty(i.ToString(),
                new JObject { ["ni"] = "1", ["nj"] = "2", ["e"] = "1" }))),
            ["element"] = JObject.Parse("""{ "1": { "1": { "E": 1, "Xp": 0, "A": 1, "Iz": 1 } } }"""),
            ["load"] = new JObject
            {
                ["1"] = new JObject
                {
                    ["load_member"] = new JArray(new JObject
                    {
                        ["row"] = 1, ["m1"] = "1", ["m2"] = m2.ToString(), ["direction"] = "y", ["mark"] = "2",
                        ["L1"] = "0", ["L2"] = "0", ["P1"] = 1, ["P2"] = 1
                    })
                }
            }
        };

        byte[] frd = Succeed(model);

        Assert.Equal(code, Sjis(Files(frd)["K_JituBuzai.tmp"][6..9]).Trim());
        Assert.Equal(m2.ToString(), (string)Forward(frd)["load"]!["1"]!["load_member"]![0]!["m2"]!);
    }

    [Fact]
    public void CombineWithoutDefineGetsOneDefinePerCase()
    {
        var model = Model();
        model["define"] = new JObject();
        model["combine"] = JObject.Parse("""{ "1": { "row": 1, "name": "case 1+3", "C1": 1.2, "C3": 0.5 } }""");
        model["pickup"] = new JObject();

        JObject json = Forward(Succeed(model));

        Assert.Equal(new[] { 1, 2, 3 }, ((JObject)json["define"]!).Properties().Select(p => (int)p.Value["C1"]!));
        Assert.Equal(new JObject { ["row"] = 1, ["name"] = "case 1+3", ["C1"] = 1.2, ["C3"] = 0.5 }, json["combine"]!["1"]!, JToken.EqualityComparer);
    }

    [Fact]
    public void TheEmptyBridgeLoadsAFrameWebforCSSaveWritesIsAccepted()
    {
        var model = Model();
        model["bridge_loads"] = JObject.Parse("""{ "version": 1, "panels": [], "paths": [], "cases": {} }""");
        Assert.True(Export(model).Succeeded);
    }

    public static TheoryData<string, string, string> Errors() => new()
    {
        { """{ "dimension": 3 }""", FrdDiagnosticCodes.Dimension, "dimension" },
        { """{ "dimension": null }""", FrdDiagnosticCodes.Dimension, "dimension" },
        { """{ "unknown": {} }""", FrdDiagnosticCodes.UnknownSection, "unknown" },
        { """{ "shell": { "1": { "nodes": [1, 2, 3] } } }""", FrdDiagnosticCodes.Shell, "shell" },
        { """{ "bridge_loads": { "version": 1, "panels": [ { "id": 1 } ], "paths": [], "cases": {} } }""", FrdDiagnosticCodes.SpatialLoad, "bridge_loads" },
        { """{ "spatial_loads": { "loads": [ {} ] } }""", FrdDiagnosticCodes.SpatialLoad, "spatial_loads" },
        { """{ "load": { "1": { "load_inf": [ {} ] } } }""", FrdDiagnosticCodes.SpatialLoad, "load.1.load_inf" },
        { """{ "node": { "a": { "x": 0, "y": 0 } } }""", FrdDiagnosticCodes.InvalidId, "node.a" },
        { """{ "node": { "1": { "x": "abc", "y": 0 } } }""", FrdDiagnosticCodes.InvalidValue, "node.1.x" },
        { """{ "member": { "1": { "ni": "1", "nj": "99", "e": "1" } } }""", FrdDiagnosticCodes.Reference, "member.1.nj" },
        { """{ "member": { "1": { "ni": "1", "nj": "2", "e": "9" } } }""", FrdDiagnosticCodes.Reference, "member.1.e" },
        { """{ "member": { "1": { "ni": "1", "nj": "1", "e": "1" } } }""", FrdDiagnosticCodes.MemberGeometry, "member.1" },
        { """{ "element": { "7": {} } }""", FrdDiagnosticCodes.Limit, "element.7" },
        { """{ "element": { "2": { "1": { "E": 30000000, "Xp": 1E-05, "A": 1, "Iz": 1 } } } }""", FrdDiagnosticCodes.MaterialConflict, "element.2.1" },
        { """{ "element": { "2": { "3": { "E": 25000000, "Xp": 1E-05, "A": 999, "Iz": 1000 } } } }""", FrdDiagnosticCodes.MaterialConflict, "element.2.3" },
        { """{ "rigid": [ { "m": "2", "Ilength": 3, "Jlength": 2, "e": 3 } ] }""", FrdDiagnosticCodes.MemberGeometry, "rigid[0]" },
        { """{ "fix_node": { "1": [ { "row": 1, "n": "1", "tx": 1 }, { "row": 2, "n": "1", "ty": 1 } ] } }""", FrdDiagnosticCodes.Duplicate, "fix_node.1[1]" },
        { """{ "fix_node": { "17": [] } }""", FrdDiagnosticCodes.Limit, "fix_node.17" },
        { """{ "fix_member": { "1": [ { "row": 1, "m": "1", "length": 1, "tx": 1, "ty": 1 } ] } }""", FrdDiagnosticCodes.PileSpring, "fix_member.1[0].length" },
        { """{ "joint": { "1": [ { "row": 1, "m": "1", "zi": 2, "zj": 1 } ] } }""", FrdDiagnosticCodes.InvalidValue, "joint.1[0].zi" },
        { """{ "notice_points": [ { "row": 1, "m": "1", "Points": [ 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20 ] }, { "row": 2, "m": "1", "Points": [ 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20 ] }, { "row": 3, "m": "1", "Points": [ 1,2,3,4,5,6,7,8,9,10,11 ] } ] }""", FrdDiagnosticCodes.Limit, "notice_points[2]" },
        { """{ "load": { "1": { "load_node": [ { "row": 1, "n": "-2", "tx": 1 } ] } } }""", FrdDiagnosticCodes.ImposedDisplacement, "load.1.load_node[0].n" },
        { """{ "load": { "1": { "load_node": [ { "row": 1, "n": "9", "tx": 1 } ] } } }""", FrdDiagnosticCodes.Reference, "load.1.load_node[0].n" },
        { """{ "load": { "1": { "symbol": "LL" } } }""", FrdDiagnosticCodes.MovingLoad, "load.1.symbol" },
        { """{ "load": { "1": { "load_member": [ { "row": 1, "m1": "1", "direction": "z", "mark": "1", "L1": "0", "P1": 1 } ] } } }""", FrdDiagnosticCodes.LoadType, "load.1.load_member[0]" },
        { """{ "load": { "1": { "load_member": [ { "row": 1, "m1": "1", "direction": "y", "mark": "14", "L1": "0", "P1": 1 } ] } } }""", FrdDiagnosticCodes.LoadType, "load.1.load_member[0]" },
        { """{ "load": { "1": { "load_member": [ { "row": 1, "m1": "8", "direction": "y", "mark": "1", "L1": "0", "P1": 1 } ] } } }""", FrdDiagnosticCodes.Reference, "load.1.load_member[0].m1" },
        { """{ "load": { "1": { "load_member": [ { "row": 1, "m1": "1", "direction": "y", "mark": "1", "L1": "123456789012.5", "P1": 1 } ] } } }""", FrdDiagnosticCodes.FieldOverflow, "load.1.L1" },
        { """{ "load": { "1": { "element": 7 } } }""", FrdDiagnosticCodes.Limit, "load.1.element" },
        { """{ "load": { "1": { "name": "😀" } } }""", FrdDiagnosticCodes.InvalidText, "load.1.name" },
        { """{ "define": { "1": { "row": 1, "C31": 1 } } }""", FrdDiagnosticCodes.Limit, "define.1.C31" },
        { """{ "define": { "1": { "row": 1, "C1": 9 } } }""", FrdDiagnosticCodes.Reference, "define.1.C1" },
        { """{ "combine": { "1": { "row": 1, "C5": 1 } } }""", FrdDiagnosticCodes.Reference, "combine.1.C5" },
        { """{ "pickup": { "1": { "row": 1, "C101": 1 } } }""", FrdDiagnosticCodes.Limit, "pickup.1.C101" },
        { """{ "pickup": { "1": { "row": 1, "C1": 5 } } }""", FrdDiagnosticCodes.Reference, "pickup.1.C1" },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void UnrepresentableInputIsAnErrorAndWritesNothing(string change, string code, string path)
    {
        var model = Model();
        model.Merge(JObject.Parse(change), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge });

        FrdExportResult result = Export(model);

        Assert.False(result.Succeeded);
        Assert.Null(result.Frd);
        Assert.Contains(result.Errors, error => error.Code == code && error.Path == path);
    }

    [Fact]
    public void MalformedJsonIsAnError()
    {
        FrdExportResult result = FrdExporter.Export("{ \"dimension\": 2, \"dimension\": 2 }");
        Assert.Equal(FrdDiagnosticCodes.InvalidJson, Assert.Single(result.Errors).Code);
        Assert.Null(result.Frd);
    }

    public static TheoryData<string, string, string> Warnings() => new()
    {
        { """{ "node": { "1": { "z": 1 } } }""", FrdDiagnosticCodes.ThreeDimensionalFieldDropped, "node" },
        { """{ "element": { "1": { "1": { "G": 1 } } } }""", FrdDiagnosticCodes.ThreeDimensionalFieldDropped, "element" },
        { """{ "joint": { "1": [ { "row": 1, "m": "2", "xi": 0, "yi": 1, "zi": 0, "xj": 1, "yj": 1, "zj": 1 } ] } }""", FrdDiagnosticCodes.ThreeDimensionalFieldDropped, "joint" },
        { """{ "load": { "1": { "load_node": [ { "row": 2, "n": "2", "tx": 10, "tz": 5 } ] } } }""", FrdDiagnosticCodes.ThreeDimensionalFieldDropped, "load_node" },
        { """{ "result": { "1": {} }, "resultDimension": 2 }""", FrdDiagnosticCodes.SectionIgnored, "" },
        { """{ "load": { "1": { "name": "とても長い荷重の名前とても長い荷重の名前とても長い" } } }""", FrdDiagnosticCodes.TextTruncated, "load.1.name" },
        { """{ "define": { "1": { "name": "D" } } }""", FrdDiagnosticCodes.DefineNameDropped, "define" },
        { """{ "element": { "1": { "1": { "n": "杭" } } } }""", FrdDiagnosticCodes.PileName, "element.1.1.n" },
        { """{ "element": { "1": { "1": { "A": 0.123456789012345 } } } }""", FrdDiagnosticCodes.PrecisionReduced, "member.1.A" },
    };

    [Theory]
    [MemberData(nameof(Warnings))]
    public void LostInformationIsAWarning(string change, string code, string path)
    {
        var model = Model();
        model.Merge(JObject.Parse(change), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

        FrdExportResult result = Export(model);

        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        Assert.Contains(result.Warnings, warning => warning.Code == code && warning.Path == path);
    }

    [Fact]
    public void TheBaseModelHasNoWarnings()
    {
        Assert.Empty(Export(Model()).Diagnostics);
    }

    internal static byte[] SampleBytes(string fileName) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
}

using System.Globalization;
using System.Text;
using Convert_Manager.FrameWebForJS;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Convert_Manager.Tests;

public class RigidZoneConversionTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    [InlineData(2, 3)]
    public void NativeEndsPreserveTopologyAndSourceMaterial(double iLength, double jLength)
    {
        var (nodes, members) = Model();
        var originalNodes = JObject.FromObject(nodes.GetNode());
        var originalMembers = JObject.FromObject(members.GetMember());
        var converter = Zones(iLength, jLength);

        var rigid = Assert.Single(converter.GetRigid(nodes, members));

        Assert.Equal("1", rigid.m);
        Assert.Equal(iLength, rigid.Ilength);
        Assert.Equal(jLength, rigid.Jlength);
        Assert.True(JToken.DeepEquals(originalNodes, JObject.FromObject(nodes.GetNode())));
        Assert.True(JToken.DeepEquals(originalMembers, JObject.FromObject(members.GetMember())));
        Assert.Equal(JTokenType.Integer, JObject.FromObject(rigid)["e"]!.Type);
        foreach (var sheet in members.GetElement().Values)
        {
            var source = sheet["1"];
            var material = sheet[rigid.e.ToString(CultureInfo.InvariantCulture)];
            Assert.Equal(source.E, material.E);
            Assert.Equal(source.Xp, material.Xp);
            Assert.Equal(30, material.A);
            Assert.Equal(40, material.Iz);
            Assert.Equal(10, source.A);
            Assert.Equal(20, source.Iz);
        }
        Assert.Equal(rigid.e, Assert.Single(converter.GetRigid(nodes, members)).e);
        Assert.All(members.GetElement().Values, sheet => Assert.Equal(2, sheet.Count));
    }

    [Fact]
    public void FractionalFullSpanAllowsOnlyFloatingPointRoundoff()
    {
        var (nodes, members) = Model();
        nodes.GetNode("2").x = 0.3;
        nodes.GetNode("2").y = 0;

        var rigid = Assert.Single(Zones(0.1, 0.2).GetRigid(nodes, members));

        Assert.Equal(0.1, rigid.Ilength);
        Assert.Equal(0.2, rigid.Jlength);
        Assert.Throws<InvalidDataException>(() => Zones(0.1, 0.200001).GetRigid(nodes, members));
    }

    [Fact]
    public void MissingAndZeroZonesDoNotAddMaterials()
    {
        var (nodes, members) = Model();
        Assert.Empty(new gouiki(new()).GetRigid(nodes, members));
        Assert.Empty(Zones(0, 0).GetRigid(nodes, members));
        Assert.All(members.GetElement().Values, sheet => Assert.Single(sheet));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(6, 0)]
    [InlineData(3, 3)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void InvalidLengthsReportMember(double iLength, double jLength)
    {
        var (nodes, members) = Model();
        var error = Assert.Throws<InvalidDataException>(() => Zones(iLength, jLength).GetRigid(nodes, members));
        Assert.Contains("member 1", error.Message);
    }

    [Fact]
    public void MissingMemberAndMaterialReportInvalidInput()
    {
        var (nodes, members) = Model();
        members.GetMember().Clear();
        Assert.Contains("member 1", Assert.Throws<InvalidDataException>(
            () => Zones(1, 0).GetRigid(nodes, members)).Message);

        (nodes, members) = Model();
        members.GetElement()["2"].Clear();
        Assert.Contains("TYPE 2", Assert.Throws<InvalidDataException>(
            () => Zones(1, 0).GetRigid(nodes, members)).Message);
        Assert.Single(members.GetElement()["1"]);
        Assert.Empty(members.GetElement()["2"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MaterialReuseRequiresMatchingEveryType(bool allTypesMatch)
    {
        var (nodes, members) = Model();
        foreach (var sheet in members.GetElement())
        {
            var source = sheet.Value["1"];
            sheet.Value.Add("2", new Element
            {
                E = source.E, Xp = source.Xp,
                A = allTypesMatch || sheet.Key == "2" ? 30 : 99, Iz = 40
            });
        }

        var rigid = Assert.Single(Zones(1, 2).GetRigid(nodes, members));

        Assert.Equal(allTypesMatch ? 2 : 3, rigid.e);
        Assert.All(members.GetElement().Values, sheet =>
            Assert.Equal(30, sheet[rigid.e.ToString(CultureInfo.InvariantCulture)].A));
    }

    [Fact]
    public void RigidNumbersUseArchiveDecimalSeparator()
    {
        var (nodes, members) = Model();
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var rigid = Assert.Single(Zones(1.25, 0.5).GetRigid(nodes, members));
            Assert.Equal(1.25, rigid.Ilength);
            Assert.Equal(0.5, rigid.Jlength);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("4_2-FrameG.frd")]
    [InlineData("バネ連衡あり.frd")]
    [InlineData("バネ連衡なし.frd")]
    public void RealArchivesPreserveOriginalTopologyAndReferences(string filename)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", filename));
        var converter = new ConvertManager(stream);
        var json = JObject.Parse(converter.getJsonString());
        var originalNodes = new node(converter.wdata);
        var originalMembers = new member(converter.wdata);
        var supports = new fix_node(converter.wdata);
        var springs = new fix_member(converter.wdata);
        pile_spring.Apply(converter.wdata, originalNodes, originalMembers, supports, springs);

        Assert.True(JToken.DeepEquals(JObject.FromObject(originalNodes.GetNode()), json["node"]));
        Assert.True(JToken.DeepEquals(JObject.FromObject(originalMembers.GetMember()), json["member"]));
        Assert.True(JToken.DeepEquals(JObject.FromObject(supports.GetFixNode()), json["fix_node"]));
        Assert.True(JToken.DeepEquals(JObject.FromObject(springs.GetFixMember()), json["fix_member"]));
        Assert.True(JToken.DeepEquals(JObject.FromObject(new load(converter.wdata).GetLoad()), json["load"]));
        Assert.True(JToken.DeepEquals(JObject.FromObject(new joint(converter.wdata).GetJoint()), json["joint"]));
        Assert.True(JToken.DeepEquals(JArray.FromObject(new notice_points(converter.wdata).GetNoticePoint()), json["notice_points"]));

        var rigid = Assert.IsType<JArray>(json["rigid"]);
        if (filename == "4_2-FrameG.frd") Assert.Empty(rigid);
        else Assert.NotEmpty(rigid);
        string sourceRows = converter.wdata["Buzai_G.tmp"];
        int expectedRows = 0;
        for (int offset = 0; offset < sourceRows.Length; offset += 56)
        {
            double Field(int index)
            {
                string value = sourceRows.Substring(offset + index * 14, 14).Trim();
                return value.Length == 0 ? 0 : double.Parse(value, CultureInfo.InvariantCulture);
            }
            double jLength = Field(0), iLength = Field(1);
            if (iLength == 0 && jLength == 0) continue;
            expectedRows++;
            string id = (offset / 56 + 1).ToString(CultureInfo.InvariantCulture);
            var row = Assert.Single(rigid, value => value.Value<string>("m") == id);
            Assert.Equal(iLength, row.Value<double>("Ilength"));
            Assert.Equal(jLength, row.Value<double>("Jlength"));
            Assert.Equal(JTokenType.Integer, row["e"]!.Type);
            foreach (var sheet in originalMembers.GetElement())
            {
                var source = sheet.Value[originalMembers.getMember(id).e];
                var material = json["element"]![sheet.Key]![row.Value<int>("e").ToString(CultureInfo.InvariantCulture)]!;
                Assert.Equal(source.E, material.Value<double>("E"));
                Assert.Equal(source.Xp, material.Value<double>("Xp"));
                Assert.Equal(Field(2), material.Value<double>("A"));
                Assert.Equal(Field(3), material.Value<double>("Iz"));
            }
        }
        Assert.Equal(expectedRows, rigid.Count);
    }

    private static gouiki Zones(double iLength, double jLength)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string row = string.Concat(new[] { jLength, iLength, 30, 40 }
            .Select(value => value.ToString(CultureInfo.InvariantCulture).PadRight(14)));
        return new gouiki(new() { ["Buzai_G.tmp"] = row });
    }

    private static (node Nodes, member Members) Model()
    {
        var nodes = new node(new());
        nodes.GetNode().Add("1", new Vector3());
        nodes.GetNode().Add("2", new Vector3 { x = 3, y = 4 });
        var members = new member(new());
        members.GetMember().Add("1", new Member { ni = "1", nj = "2", e = "1" });
        for (int type = 1; type <= 2; type++)
            members.GetElement().Add(type.ToString(), new()
            {
                ["1"] = new Element { E = type * 100, Xp = type, A = 10, Iz = 20 }
            });
        return (nodes, members);
    }
}

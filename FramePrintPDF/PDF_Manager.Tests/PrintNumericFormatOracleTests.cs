using System.Globalization;
using Newtonsoft.Json.Linq;
using PDF_Manager;
using PDF_Manager.Printing;
using PDF_Manager.Printing.Comon;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class PrintNumericFormatOracleTests
{
    [Theory]
    [InlineData("ja-JP", 7.625, 2, "F", "7.62")]
    [InlineData("ja-JP", -7.625, 2, "F", "-7.62")]
    [InlineData("ja-JP", 1.2345, 3, "F", "1.234")]
    [InlineData("ja-JP", 1.23445, 4, "F", "1.2345")]
    [InlineData("ja-JP", 0.000001234567, 6, "F", "0.000001")]
    [InlineData("ja-JP", 0.00001, 2, "E", "1.00E-005")]
    [InlineData("ja-JP", -0.00001, 2, "E", "-1.00E-005")]
    [InlineData("de-DE", 7.625, 2, "F", "7,62")]
    [InlineData("de-DE", 0.00001, 2, "E", "1,00E-005")]
    public void PrinterUsesCurrentCultureAndBinary64Formatting(
        string cultureName, double value, int digits, string style, string expected)
    {
        WithCulture(cultureName, () => Assert.Equal(expected,
            printManager.toString(value, digits, style)));
    }

    [Fact]
    public void PrinterTreatsMissingAndNaNAsBlankButFormatsNumericZero()
    {
        WithCulture("ja-JP", () =>
        {
            Assert.Equal("", printManager.toString(null, 4));
            Assert.Equal("", printManager.toString(double.NaN, 4));
            Assert.Equal("0.0000", printManager.toString(0d, 4));
            Assert.Equal("-0.0000", printManager.toString(-0.00001d, 4));
        });
    }

    [Theory]
    [InlineData(2, "999.0000", "999.000000")]
    [InlineData(3, "999", "999")]
    public void MaterialTablePinsTwoAndThreeDimensionalBoundary(
        int dimension, string area, string inertia)
    {
        WithCulture("ja-JP", () =>
        {
            var root = new Dictionary<string, object>
            {
                ["dimension"] = dimension,
                ["element"] = JObject.Parse("""
                    {"1":{"1":{"E":25000000,"G":0.00001,"Xp":0.00001,
                               "A":999,"J":999,"Iy":999,"Iz":999}}}
                    """)
            };
            Table table = new InspectElement(root).FirstTable(new PrintData(root));
            int row = dimension == 3 ? 4 : 3;
            int areaColumn = dimension == 3 ? 1 : 1;
            int inertiaColumn = dimension == 3 ? 5 : 4;
            Assert.Equal(area, table[row, areaColumn]);
            Assert.Equal(inertia, table[row, inertiaColumn]);
            Assert.Equal("2.50E+007", table[row, dimension == 3 ? 2 : 2]);
            Assert.Equal("1.00E-005", table[row, dimension == 3 ? 3 : 3]);
        });
    }

    [Fact]
    public void ThreeDimensionalMaterialUsesFixedDigitsStrictlyBelow999()
    {
        WithCulture("ja-JP", () =>
        {
            var root = new Dictionary<string, object>
            {
                ["dimension"] = 3,
                ["element"] = JObject.Parse("""
                    {"1":{"1":{"A":998.999,"J":998.999,"Iy":998.999,"Iz":998.999}}}
                    """)
            };
            Table table = new InspectElement(root).FirstTable(new PrintData(root));
            Assert.Equal("998.9990", table[4, 1]);
            Assert.Equal("998.999000", table[4, 5]);
            Assert.Equal("998.999000", table[4, 6]);
            Assert.Equal("998.999000", table[4, 7]);
        });
    }

    [Theory]
    [InlineData("ja-JP", "1.25", "1.250", "1.250")]
    [InlineData("ja-JP", "", "", "")]
    [InlineData("ja-JP", "bad", "bad", "")]
    [InlineData("de-DE", "1.25", "125,000", "125,000")]
    public void MemberLoadTableReparsesStringLengthsInPrinterCulture(
        string cultureName, string input, string expectedL1, string expectedL2)
    {
        WithCulture(cultureName, () =>
        {
            var root = new Dictionary<string, object>
            {
                ["dimension"] = 3,
                ["load"] = JObject.Parse("""
                    {"1":{"name":[{"name":"case"}],"load_member":[
                        {"m1":"1","direction":"x","L1":"VALUE","L2":"VALUE","P1":7.625,"P2":-7.625}
                    ]}}
                    """.Replace("VALUE", input, StringComparison.Ordinal))
            };
            var data = new PrintData(root);
            Table table = new InspectLoad(root, [1]).FirstTable(data);
            int row = table.Rows - 1;
            Assert.Equal(expectedL1, table[row, 4]);
            Assert.Equal(expectedL2, table[row, 5]);
            Assert.Equal(cultureName == "de-DE" ? "7,62" : "7.62", table[row, 6]);
            Assert.Equal(cultureName == "de-DE" ? "-7,62" : "-7.62", table[row, 7]);
        });
    }

    [Theory]
    [InlineData("ja-JP", "7.62", "-7.62")]
    [InlineData("de-DE", "7,62", "-7,62")]
    public void ReactionResultTableUsesPdfF2ForBothDimensions(
        string cultureName, string positive, string negative)
    {
        WithCulture(cultureName, () =>
        {
            foreach (int dimension in new[] { 2, 3 })
            {
                var root = new Dictionary<string, object>
                {
                    ["dimension"] = dimension,
                    ["reac"] = JObject.Parse("""
                        {"1":[{"id":"11","tx":7.625,"ty":-7.625,"mz":-0.00001}]}
                        """),
                    ["reacName"] = JArray.Parse("""[["1","Case"]]""")
                };
                Table table = new InspectReac(root).FirstTable(new PrintData(root));
                Assert.Equal(positive, table[4, 1]);
                Assert.Equal(negative, table[4, 2]);
                Assert.Equal(cultureName == "de-DE" ? "-0,00" : "-0.00",
                    table[4, dimension == 3 ? 6 : 3]);
            }
        });
    }

    private static void WithCulture(string name, Action action)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    private sealed class InspectElement(Dictionary<string, object> root) : InputElement(root)
    {
        internal Table FirstTable(PrintData data)
        {
            base.PrintInit(null!, data, out _, out _, out _);
            return Assert.Single(base.GetTables(null!, data, 1));
        }
    }

    private sealed class InspectLoad(Dictionary<string, object> root, List<int> names) : InputLoad(root, names)
    {
        internal Table FirstTable(PrintData data)
        {
            base.PrintInit(null!, data, out _, out _, out _);
            return Assert.Single(base.GetTables(null!, data, 1));
        }
    }

    private sealed class InspectReac(Dictionary<string, object> root) : ResultReac(root)
    {
        internal Table FirstTable(PrintData data)
        {
            base.PrintInit(null!, data, out _, out _, out _);
            return Assert.Single(base.GetTables(null!, data, 1));
        }
    }
}

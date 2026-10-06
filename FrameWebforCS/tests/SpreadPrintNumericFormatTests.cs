using System.Globalization;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using FarPoint.Win.Spread;
using FarPoint.Win.Spread.CellType;
using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.components.result;
using FrameWebforCS.providers;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class SpreadPrintNumericFormatTests
{
    [Fact]
    public void NodeCoordinateUsesSavedDecimalPrinterRounding()
    {
        RunSta("ja-JP", () =>
        {
            var service = InputNodesService.Instance;
            try
            {
                using var json = JsonDocument.Parse("""{"node":{"1":{"x":1.2345,"y":-0.00001}}}""");
                service.setNodeJson(json.RootElement);
                using var form = new Form();
                using var view = new InputNodesComponent();
                form.Controls.Add(view);
                form.Show();
                Assert.Equal(PrinterFloat(service.Nodes[0].X!.Value, "F3"),
                    Spread(view).Sheets[0].Cells[0, 0].Text);
                Assert.Equal("-0.000", Spread(view).Sheets[0].Cells[0, 1].Text);
                FpSpread spread = Spread(view);
                spread.ActiveSheet.SetActiveCell(0, 0);
                spread.StartCellEditing(EventArgs.Empty, false);
                Assert.True(spread.EditMode);
                spread.StopCellEditing();
                Assert.Equal(1.2345f, service.Nodes[0].X);
            }
            finally { service.clear(); }
        });
    }

    [Fact]
    public void MissingInputCoordinateStaysBlankAndMissingLegacyResultStaysZero()
    {
        RunSta("ja-JP", () =>
        {
            var input = InputDataService.Instance;
            int previousDimension = input.dimension;
            try
            {
                using var json = JsonDocument.Parse("""
                    {"dimension":2,"node":{"1":{"x":1.25}},
                     "result":{"1":{"disg":{"1":{"dx":0.001}},
                        "reac":{"1":{"tx":2}},"fsec":{}}}}
                    """);
                input.JsonDataOpen(json.RootElement);
                using var form = new Form();
                using var nodes = new InputNodesComponent();
                using var displacement = new ResultDisgComponent();
                form.Controls.Add(nodes);
                form.Controls.Add(displacement);
                form.Show();
                Assert.Null(InputNodesService.Instance.Nodes[0].Y);
                Assert.Equal("", Spread(nodes).Sheets[0].Cells[0, 1].Text);
                Assert.Null(ResultDisgService.Instance.getDisg()["1"]["1"].dy);
                Assert.Equal("0.0000", Spread(displacement).Sheets[0].Cells[0, 2].Text);
            }
            finally
            {
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
                input.SetDimension(previousDimension);
            }
        });
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ElementCellsUsePrinterFormatsWithoutChangingBoundFloats(int dimension)
    {
        RunSta("ja-JP", () =>
        {
            var input = InputDataService.Instance;
            int previousDimension = input.dimension;
            try
            {
                input.SetDimension(dimension);
                using var json = JsonDocument.Parse("""
                    {"element":{"1":{"1":{"E":25000000,"G":0.00001,"Xp":0.00001,
                        "A":998.999,"J":998.999,"Iy":998.999,"Iz":998.999},
                        "2":{"E":25000000,"G":0.00001,"Xp":0.00001,
                        "A":999,"J":999,"Iy":999,"Iz":999},
                        "3":{"Xp":0.00001005}}}}
                    """);
                InputElementsService.Instance.setElementJson(json.RootElement);
                using var form = new Form();
                using var view = new InputElementsComponent();
                form.Controls.Add(view);
                form.Show();
                SheetView sheet = Spread(view).Sheets[0];
                Assert.Equal("2.50E+007", sheet.Cells[0, 0].Text);
                Assert.Equal("1.00E-005", sheet.Cells[0, 1].Text);
                int area = dimension == 3 ? 3 : 2;
                int inertia = dimension == 3 ? 5 : 3;
                Assert.Equal("998.9990", sheet.Cells[0, area].Text);
                Assert.Equal("998.999000", sheet.Cells[0, inertia].Text);
                Assert.Equal(dimension == 3 ? "999" : "999.0000", sheet.Cells[1, area].Text);
                Assert.Equal(dimension == 3 ? "999" : "999.000000", sheet.Cells[1, inertia].Text);
                float? original = InputElementsService.Instance.GetRows(1)[0].Area;
                Assert.Equal(original, sheet.Cells[0, area].Value);
                using var saved = JsonDocument.Parse(JsonSerializer.Serialize(input.GetSaveJson()));
                Assert.Equal(original, saved.RootElement.GetProperty("element").GetProperty("1")
                    .GetProperty("1").GetProperty("A").GetSingle());
                JsonElement nearMidpoint = saved.RootElement.GetProperty("element")
                    .GetProperty("1").GetProperty("3").GetProperty("Xp");
                string printerText = double.Parse(nearMidpoint.GetRawText(),
                    CultureInfo.InvariantCulture).ToString("E2", CultureInfo.CurrentCulture);
                Assert.Equal(printerText, sheet.Cells[2, dimension == 3 ? 2 : 1].Text);
                FpSpread materialSpread = Spread(view);
                sheet.SetActiveCell(0, area);
                materialSpread.StartCellEditing(EventArgs.Empty, false);
                Assert.True(materialSpread.EditMode);
                materialSpread.StopCellEditing();
                Assert.Equal(original, InputElementsService.Instance.GetRows(1)[0].Area);
                using var afterEdit = JsonDocument.Parse(JsonSerializer.Serialize(input.GetSaveJson()));
                Assert.Equal(original, afterEdit.RootElement.GetProperty("element")
                    .GetProperty("1").GetProperty("1").GetProperty("A").GetSingle());
            }
            finally
            {
                InputElementsService.Instance.clear();
                input.SetDimension(previousDimension);
            }
        });
    }

    [Fact]
    public void LoadStringLengthsKeepRawValuesAfterCultureSensitiveDisplayAndEdit()
    {
        RunSta("de-DE", () =>
        {
            var service = InputLoadService.Instance;
            var input = InputDataService.Instance;
            int previousDimension = input.dimension;
            try
            {
                service.clear();
                input.SetDimension(3);
                using var json = JsonDocument.Parse("""
                    {"load":{"1":{"load_member":[{"row":1,"m1":"1","direction":"x","mark":"1",
                        "L1":"1.23456","L2":"1.23456","P1":7.625,"P2":-7.625}]}}}
                    """);
                service.setLoadJson(json.RootElement);
                using var form = new Form();
                using var view = new InputLoadComponent();
                form.Controls.Add(view);
                form.Show();
                FpSpread spread = Spread(view);
                spread.ActiveSheetIndex = 1;
                SheetView sheet = spread.ActiveSheet;
                int row = service.FindIntensityRowIndex("1", 1);
                Assert.Equal("1,235", sheet.Cells[row, 5].Text);
                Assert.Equal("1,235", sheet.Cells[row, 6].Text);
                Assert.Equal("7,62", sheet.Cells[row, 7].Text);
                Assert.Equal("-7,62", sheet.Cells[row, 8].Text);
                Assert.Equal("1.23456", service.GetIntensityRowAt(row)!.L1);
                Assert.Equal("1.23456", service.GetIntensityRowAt(row)!.L2);

                sheet.SetActiveCell(row, 5);
                spread.StartCellEditing(EventArgs.Empty, false);
                Assert.True(spread.EditMode);
                spread.StopCellEditing();
                Assert.Equal("1.23456", service.GetIntensityRowAt(row)!.L1);
                using var saved = JsonDocument.Parse(JsonSerializer.Serialize(input.GetSaveJson()));
                JsonElement member = saved.RootElement.GetProperty("load").GetProperty("1")
                    .GetProperty("load_member")[0];
                Assert.Equal("1.23456", member.GetProperty("L1").GetString());
                Assert.Equal("1.23456", member.GetProperty("L2").GetString());

                sheet.Cells[row, 5].Value = "1.25";
                Assert.Equal("1,250", sheet.Cells[row, 5].Text);
                sheet.SetActiveCell(row, 5);
                spread.StartCellEditing(EventArgs.Empty, false);
                spread.StopCellEditing();
                Assert.Equal("1.25", service.GetIntensityRowAt(row)!.L1);
                sheet.SetActiveCell(row, 6);
                spread.StartCellEditing(EventArgs.Empty, false);
                spread.EditingControl!.Text = "2.34567";
                spread.StopCellEditing();
                Assert.Equal("2.34567", service.GetIntensityRowAt(row)!.L2);
                string savedJson = JsonSerializer.Serialize(input.GetSaveJson());
                using var reloaded = JsonDocument.Parse(savedJson);
                service.clear();
                service.setLoadJson(reloaded.RootElement);
                int reloadedRow = service.FindIntensityRowIndex("1", 1);
                Assert.Equal("1.25", service.GetIntensityRowAt(reloadedRow)!.L1);
                Assert.Equal("2.34567", service.GetIntensityRowAt(reloadedRow)!.L2);
                Assert.Equal("1,250", sheet.Cells[reloadedRow, 5].Text);
                Assert.Equal("2,346", sheet.Cells[reloadedRow, 6].Text);

                JsonObject snapshot = JsonNode.Parse(savedJson)!.AsObject();
                snapshot["node"] = JsonNode.Parse("""
                    {"1":{"x":0,"y":0,"z":0},"2":{"x":5,"y":0,"z":0}}
                    """);
                snapshot["member"] = JsonNode.Parse("""{"1":{"ni":"1","nj":"2","e":"1"}}""");
                snapshot["element"] = JsonNode.Parse("""
                    {"1":{"1":{"E":200000,"A":1,"Iz":1}}}
                    """);
                snapshot["fix_node"] = JsonNode.Parse("""
                    {"1":[{"n":"1","tx":1,"ty":1,"tz":1,"rx":1,"ry":1,"rz":1}]}
                    """);
                using var request = JsonDocument.Parse(
                    CalculationRequestBuilder.FromSavedJson(snapshot.ToJsonString()).Json);
                JsonElement projected = request.RootElement.GetProperty("load")
                    .GetProperty("1").GetProperty("load_member")[0];
                Assert.Equal(1.25, projected.GetProperty("L1").GetDouble());
                Assert.Equal(2.346, projected.GetProperty("L2").GetDouble());
            }
            finally
            {
                service.clear();
                input.SetDimension(previousDimension);
            }
        });
    }

    [Fact]
    public void CombineCoefficientRetainsPrecisionAcrossDynamicColumns()
    {
        RunSta("ja-JP", () =>
        {
            var combine = InputCombineService.Instance;
            var load = InputLoadService.Instance;
            try
            {
                combine.clear();
                using (var caseJson = JsonDocument.Parse("""{"load":{"9":{"name":"case"}}}"""))
                    load.setLoadJson(caseJson.RootElement);
                using (var combination = JsonDocument.Parse("""
                    {"combine":{"1":{"row":1,"C9":1.23456}}}
                    """)) combine.setCombineJson(combination.RootElement);
                using var view = new InputCombineComponent();
                SheetView sheet = Spread(view).Sheets[1];
                Assert.Equal("1.23", sheet.Cells[0, 8].Text);
                Assert.Equal(1.23456, combine.CombineRows[1].Coefficients[9]);
                using (var next = JsonDocument.Parse("""{"load":{"12":{"name":"later"}}}"""))
                    load.setLoadJson(next.RootElement);
                Assert.Equal("1.23", sheet.Cells[0, 8].Text);
            }
            finally { combine.clear(); load.clear(); }
        });
    }

    [Fact]
    public void CombineNameColumnSurvivesGrowAndShrinkAfterBeingNumeric()
    {
        RunSta("ja-JP", () =>
        {
            var combine = InputCombineService.Instance;
            var load = InputLoadService.Instance;
            try
            {
                combine.clear();
                load.clear();
                using (var definition = JsonDocument.Parse("""
                    {"combine":{"1":{"row":1,"name":"Original","C5":1.23456}}}
                    """)) combine.setCombineJson(definition.RootElement);
                using var form = new Form();
                using var view = new InputCombineComponent();
                form.Controls.Add(view);
                form.Show();
                FpSpread spread = Spread(view);
                spread.ActiveSheetIndex = 1;
                SheetView sheet = spread.ActiveSheet;
                Assert.Equal(6, sheet.ColumnCount);
                Assert.Equal("Original", sheet.Cells[0, 5].Text);

                using (var next = JsonDocument.Parse("""{"load":{"6":{"name":"sixth"}}}"""))
                    load.setLoadJson(next.RootElement);
                Assert.Equal(7, sheet.ColumnCount);
                Assert.Equal("Original", sheet.Cells[0, 6].Text);
                Assert.IsType<PrintNumberCellType>(sheet.Columns[5].CellType);

                load.clear();
                Assert.Equal(6, sheet.ColumnCount);
                Assert.Equal("Original", sheet.Cells[0, 5].Text);
                Assert.IsType<GeneralCellType>(sheet.Columns[5].CellType);
                sheet.SetActiveCell(0, 5);
                spread.StartCellEditing(EventArgs.Empty, false);
                Assert.True(spread.EditMode);
                spread.EditingControl!.Text = "After shrink";
                spread.StopCellEditing();
                Assert.Equal("After shrink", sheet.Cells[0, 5].Text);
                Assert.Equal("After shrink", combine.CombineRows[1].name);
                using var saved = JsonDocument.Parse(JsonSerializer.Serialize(combine.getCombineJson()));
                Assert.Equal("After shrink", saved.RootElement.GetProperty("1")
                    .GetProperty("name").GetString());
                Assert.Equal(1.23456, saved.RootElement.GetProperty("1")
                    .GetProperty("C5").GetDouble());
            }
            finally { combine.clear(); load.clear(); }
        });
    }

    [Fact]
    public void CommaDecimalFloatEditKeepsBoundAndSavedPrecision()
    {
        RunSta("de-DE", () =>
        {
            var nodes = InputNodesService.Instance;
            try
            {
                using var json = JsonDocument.Parse("""{"node":{"1":{"x":1.2345}}}""");
                nodes.setNodeJson(json.RootElement);
                using var form = new Form();
                using var view = new InputNodesComponent();
                form.Controls.Add(view);
                form.Show();
                FpSpread spread = Spread(view);
                SheetView sheet = spread.ActiveSheet;
                Assert.Equal("1,234", sheet.Cells[0, 0].Text);
                sheet.SetActiveCell(0, 0);
                spread.StartCellEditing(EventArgs.Empty, false);
                spread.StopCellEditing();
                Assert.Equal(1.2345f, nodes.Nodes[0].X);
                spread.StartCellEditing(EventArgs.Empty, false);
                spread.EditingControl!.Text = "2,34567";
                spread.StopCellEditing();
                Assert.Equal(2.34567f, nodes.Nodes[0].X);
                using var saved = JsonDocument.Parse(JsonSerializer.Serialize(
                    InputDataService.Instance.GetSaveJson()));
                Assert.Equal(2.34567f, saved.RootElement.GetProperty("node")
                    .GetProperty("1").GetProperty("x").GetSingle());
            }
            finally { nodes.clear(); }
        });
    }

    [Fact]
    public void BoundMemberSpringAndNoticeFloatsUseSavedDecimalPrinterRounding()
    {
        RunSta("ja-JP", () =>
        {
            var input = InputDataService.Instance;
            int previousDimension = input.dimension;
            try
            {
                using var json = JsonDocument.Parse("""
                    {"dimension":3,
                     "node":{"1":{"x":0},"2":{"x":5}},
                     "member":{"1":{"ni":"1","nj":"2","e":"1","cg":1.2345}},
                     "rigid":[{"m":"1","Ilength":1.005,"Jlength":-1.005}],
                     "fix_member":{"1":[{"row":1,"m":"1","length":1.2345,
                        "tx":1.2345,"ty":0.00001005,"tz":-0.00001005,"tr":1.005},
                        {"row":2,"m":"1","length":1.005,"tx":2}]},
                     "notice_points":[{"row":1,"m":"1","Points":[1.2345]}]}
                    """);
                input.JsonDataOpen(json.RootElement);
                using var members = new InputMembersComponent();
                using var springs = new InputFixMemberComponent();
                using var notices = new InputNoticePointsComponent();
                using var form = new Form();
                form.Controls.Add(members);
                form.Show();
                SheetView member = Spread(members).Sheets[0];
                SheetView rigid = Spread(members).Sheets[1];
                SheetView spring = Spread(springs).Sheets[0];
                SheetView notice = Spread(notices).Sheets[0];
                Assert.Equal(PrinterFloat(InputMembersService.Instance.Members[0].Cg!.Value, "F3"),
                    member.Cells[0, 4].Text);
                Assert.Equal(PrinterFloat(InputRigidZoneService.Instance.Rows[0].Ilength!.Value, "F2"),
                    rigid.Cells[0, 3].Text);
                Assert.Equal(PrinterFloat(InputRigidZoneService.Instance.Rows[0].Jlength!.Value, "F2"),
                    rigid.Cells[0, 4].Text);
                clsFixMember springValue = InputFixMemberService.Instance.GetRows("1")[0];
                Assert.Equal(PrinterFloat(springValue.length!.Value, "F3"), spring.Cells[0, 1].Text);
                Assert.Equal(PrinterFloat(springValue.tx!.Value, "F3"), spring.Cells[0, 2].Text);
                Assert.Equal(PrinterFloat(springValue.ty!.Value, "E2"), spring.Cells[0, 3].Text);
                Assert.Equal(PrinterFloat(springValue.tz!.Value, "E2"), spring.Cells[0, 4].Text);
                Assert.Equal(PrinterFloat(springValue.tr!.Value, "F2"), spring.Cells[0, 5].Text);
                Assert.Equal(PrinterFloat(InputNoticePointsService.Instance.NoticePoints[0].P1!.Value,
                    "F3"), notice.Cells[0, 2].Text);
                RangeGroupInfo group = Assert.Single(spring.GetRangeGroupInfo(1, true));
                spring.ExpandRangeGroup(group, true, false);
                Assert.Equal("***", spring.Cells[0, 2].Text);
                Assert.Equal(1.2345f, springValue.tx);
                FpSpread memberSpread = Spread(members);
                member.SetActiveCell(0, 4);
                memberSpread.StartCellEditing(EventArgs.Empty, false);
                Assert.True(memberSpread.EditMode);
                memberSpread.StopCellEditing();
                Assert.Equal(1.2345f, InputMembersService.Instance.Members[0].Cg);
                memberSpread.StartCellEditing(EventArgs.Empty, false);
                memberSpread.EditingControl!.Text = "1.2345678";
                memberSpread.StopCellEditing();
                Assert.Equal(1.2345678f, InputMembersService.Instance.Members[0].Cg);
            }
            finally
            {
                using var empty = JsonDocument.Parse("{}");
                input.JsonDataOpen(empty.RootElement);
                input.SetDimension(previousDimension);
            }
        });
    }

    [Fact]
    public void BoundLoadFloatUsesSavedDecimalPrinterRounding()
    {
        RunSta("ja-JP", () =>
        {
            var service = InputLoadService.Instance;
            try
            {
                using var json = JsonDocument.Parse("""
                    {"load":{"1":{"load_member":[{"row":1,"m1":"1","P1":1.005,"P2":-1.005}]}}}
                    """);
                service.setLoadJson(json.RootElement);
                using var view = new InputLoadComponent();
                SheetView sheet = Spread(view).Sheets[1];
                int row = service.FindIntensityRowIndex("1", 1);
                clsLoadIntensityRow bound = service.GetIntensityRowAt(row)!;
                Assert.Equal(PrinterFloat(bound.P1!.Value, "F2"), sheet.Cells[row, 7].Text);
                Assert.Equal(PrinterFloat(bound.P2!.Value, "F2"), sheet.Cells[row, 8].Text);
            }
            finally { service.clear(); }
        });
    }

    [Fact]
    public void CombineAndPickupReactionCellsKeepPdfMidpointAndCulture()
    {
        RunSta("de-DE", () =>
        {
            var coordinator = ResultCombineReacCoordinator.Instance;
            try
            {
                CalculationResultStore.Instance.Clear();
                coordinator.BeginLoad();
                using (var definitions = JsonDocument.Parse("""
                    {"define":{"5":{"row":1,"C1":1}},
                     "combine":{"7":{"row":1,"name":"Main","C5":1}},
                     "pickup":{"9":{"row":1,"name":"Envelope","C1":7}}}
                    """)) InputCombineService.Instance.setCombineJson(definitions.RootElement);
                using (var reactions = JsonDocument.Parse("""
                    {"result":{"1":{"reac":{"11":{"tx":7.625,"ty":-7.625,"mz":0}}}}}
                    """)) ResultReacService.Instance.setReacJson(reactions.RootElement);
                coordinator.CompleteLoad(2);

                using var form = new Form();
                using var combine = new ResultCombineReacComponent();
                using var pickup = new ResultPickupReacComponent();
                form.Controls.Add(combine);
                form.Controls.Add(pickup);
                form.Show();
                FpSpread combineSpread = Spread(combine);
                FpSpread pickupSpread = Spread(pickup);
                PumpUntil(() => combineSpread.Sheets.Count == 1 &&
                    combineSpread.Sheets[0].RowCount > 1 && pickupSpread.Sheets.Count == 1 &&
                    pickupSpread.Sheets[0].RowCount > 1);
                SheetView combined = combineSpread.Sheets[0];
                SheetView picked = pickupSpread.Sheets[0];
                var combinedModes = Assert.IsType<ResultModeGroupDataModel>(combined.Models.Data);
                var pickupModes = Assert.IsType<ResultModeGroupDataModel>(picked.Models.Data);
                int combineRow = Enumerable.Range(0, combinedModes.RowCount)
                    .Single(row => !combinedModes.IsGroup(row) &&
                        combinedModes.GetModeKey(row) == "tx_max");
                int pickupRow = Enumerable.Range(0, pickupModes.RowCount)
                    .Single(row => !pickupModes.IsGroup(row) &&
                        pickupModes.GetModeKey(row) == "tx_max");
                Assert.Equal("7,62", combined.Cells[combineRow, 1].Text);
                Assert.Equal("-7,62", combined.Cells[combineRow, 2].Text);
                Assert.Equal("7,62", picked.Cells[pickupRow, 1].Text);
                Assert.Equal("-7,62", picked.Cells[pickupRow, 2].Text);
                Assert.Equal(7.625, coordinator.Snapshot!.Reactions[0].Nodes[0].Value.Tx);
            }
            finally
            {
                coordinator.FailLoad();
                ResultReacService.Instance.clear();
                InputCombineService.Instance.clear();
            }
        });
    }

    [Theory]
    [InlineData("ja-JP", 2, "1.2345", "7.62", "1.235", "-7.62")]
    [InlineData("ja-JP", 3, "1.2345", "7.62", "1.235", "-7.62")]
    [InlineData("de-DE", 2, "1,2345", "7,62", "1,235", "-7,62")]
    [InlineData("de-DE", 3, "1,2345", "7,62", "1,235", "-7,62")]
    public void CanonicalBaseTablesUsePdfDigitsAndCultureWhileRetainingDisplayScaling(
        string culture, int dimension, string displacement, string force, string station, string reaction)
    {
        RunSta(culture, () =>
        {
            JsonObject root = JsonNode.Parse(File.ReadAllText(CanonicalFixture()))!.AsObject();
            root["results"]![0]!["node_displacements"]![1]!["components"]!["dx"] = 0.0012345;
            root["results"]![0]!["node_displacements"]![1]!["components"]!["rz"] = -0.00001;
            root["results"]![0]!["support_reactions"]![0]!["components"]!["fx"] = -7.625;
            root["results"]![0]!["support_reactions"]![0]!["components"]!["fy"] = -0.00001;
            root["results"]![0]!["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["fx"] = 7.625;
            root["results"]![0]!["member_section_forces"]![0]!["segments"]![0]!["i_end"]!["fy"] = -0.00001;
            root["topology"]!["members"]![0]!["stations"]![1]!["position"] = 1.235;
            root["topology"]!["nodes"]![1]!["coordinates"]!["x"] = 1.235;
            root["results"]![0]!["member_section_forces"]![0]!["segments"]![0]!["length"] = 1.235;
            var presentation = new CalculationResultPresentation(
                AnalysisResultSetJson.Deserialize(root.ToJsonString()));
            using var d = new SheetView();
            using var r = new SheetView();
            using var f = new SheetView();
            CalculationResultTableWriter.FillDisplacements(d, presentation, presentation.Pages[0], dimension);
            CalculationResultTableWriter.FillReactions(r, presentation, presentation.Pages[0], dimension);
            CalculationResultTableWriter.FillSectionForces(f, presentation, presentation.Pages[0], dimension);
            Assert.Equal(displacement, d.Cells[1, 1].Text);
            Assert.Equal(reaction, r.Cells[0, 1].Text);
            Assert.Equal(force, f.Cells[0, 3].Text);
            Assert.Equal(station, f.Cells[1, 2].Text);
            string separator = culture == "de-DE" ? "," : ".";
            Assert.Equal("-0" + separator + "0000", d.Cells[1, dimension == 3 ? 6 : 3].Text);
            Assert.Equal("-0" + separator + "00", r.Cells[0, 2].Text);
            Assert.Equal("-0" + separator + "00", f.Cells[0, 4].Text);
            Assert.Equal(0.0012345, presentation.ResultSet.Results.OfType<StaticAnalysisResult>()
                .Single().NodeDisplacements[1].Components.Dx);
        });
    }

    private static string CanonicalFixture()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null;
             directory = directory.Parent)
        {
            string path = Path.Combine(directory.FullName, "FrameWeb", "tests", "data",
                "contracts", "positive", "single-static.json");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Canonical result fixture was not found.");
    }

    private static FpSpread Spread(Control control) =>
        (FpSpread)control.Controls.Find("fpSpread1", true).Single();

    private static string PrinterFloat(float value, string format) =>
        double.Parse(value.ToString("R", CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture).ToString(format, CultureInfo.CurrentCulture);

    private static void PumpUntil(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        Assert.True(condition(), "Timed out waiting for numeric result sheets.");
    }

    private static void RunSta(string cultureName, System.Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            CultureInfo previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                action();
            }
            catch (Exception exception) { failure = exception; }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "Numeric display STA test timed out.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}

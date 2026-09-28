using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using System.Text.Json;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class InputLoadLayoutPersistenceTests
{
    [Fact]
    public void SparseLayoutRoundTripPreservesMixedCasesBothFamiliesAndAssignedEmptyLastSlot()
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
            service.IntensityRows[99999].LoadId = "3";
            string[] before = new[] { 0, 1, 2, 4, 99999 }.Select(i => LoadSheetTest.Payload(service.IntensityRows[i])).ToArray();
            string saved = LoadSheetTest.Save(service);
            using (var document = JsonDocument.Parse(saved))
            {
                var layout = document.RootElement.GetProperty("load_intensity_layout");
                Assert.Equal(1, layout.GetProperty("version").GetInt32());
                Assert.Equal(new[] { 1, 3, 5, 100000 }, layout.GetProperty("rows").EnumerateArray()
                    .Select(r => r.GetProperty("slot").GetInt32()).Order().ToArray());
                Assert.Equal(4, layout.GetProperty("rows").GetArrayLength());
                Assert.Equal(new[] { 1 }, document.RootElement.GetProperty("load").GetProperty("3")
                    .GetProperty("input_rows").EnumerateArray().Select(r => r.GetInt32()).ToArray());
            }
            service.clear();
            LoadSheetTest.Load(service, saved);
            Assert.Equal(before, new[] { 0, 1, 2, 4, 99999 }.Select(i => LoadSheetTest.Payload(service.IntensityRows[i])).ToArray());
            Assert.Equal(100000, service.IntensityRows.Count);
            Assert.False(service.IntensityRows[99998].IsAssigned);
            Assert.True(service.IntensityRows[99999].IsAssigned);
            Assert.Null(service.IntensityRows[99999].n);
        });
    }

    [Fact]
    public void NormalDocumentSavePreservesLayoutButCalculationSnapshotExcludesIt()
    {
        var input = InputDataService.Instance;
        try
        {
            OpenDocument(LoadSheetTest.MixedLayout);
            string saved = JsonSerializer.Serialize(input.GetSaveJson());
            using (var document = JsonDocument.Parse(saved))
                Assert.Equal(3, document.RootElement.GetProperty("load_intensity_layout").GetProperty("rows").GetArrayLength());
            using (var calculation = JsonDocument.Parse(input.CaptureCalculationSnapshotJson()))
            {
                Assert.False(calculation.RootElement.TryGetProperty("load_intensity_layout", out _));
                var node = calculation.RootElement.GetProperty("load").GetProperty("2").GetProperty("load_node")[0];
                Assert.Equal(1, node.GetProperty("row").GetInt32());
                Assert.Equal(1, node.GetProperty("tx").GetSingle());
            }
            OpenDocument("{}");
            OpenDocument(saved);
            Assert.Equal("2", InputLoadService.Instance.IntensityRows[0].LoadId);
            Assert.False(InputLoadService.Instance.IntensityRows[1].IsAssigned);
            Assert.Equal("1", InputLoadService.Instance.IntensityRows[2].LoadId);
        }
        finally { OpenDocument("{}"); }
    }

    [Fact]
    public void EmptyDocumentSaveContainsNoLayoutOrDummyLoads()
    {
        var input = InputDataService.Instance;
        try
        {
            OpenDocument("{}");
            using var saved = JsonDocument.Parse(JsonSerializer.Serialize(input.GetSaveJson()));
            Assert.False(saved.RootElement.TryGetProperty("load_intensity_layout", out _));
            Assert.Empty(saved.RootElement.GetProperty("load").EnumerateObject());
            Assert.Equal(100000, InputLoadService.Instance.IntensityRows.Count);
        }
        finally { OpenDocument("{}"); }
    }

    public static IEnumerable<object[]> InvalidLayouts()
    {
        foreach (string layout in new[]
        {
            "null", "[]", "{}", """{"version":2,"rows":[]}""",
            """{"version":"1","rows":[]}""", """{"version":1,"rows":{}}""",
            """{"version":1,"rows":[{"slot":0,"case_id":"2","row":1}]}""",
            """{"version":1,"rows":[{"slot":100001,"case_id":"2","row":1}]}""",
            """{"version":1,"rows":[{"slot":1.5,"case_id":"2","row":1}]}""",
            """{"version":1,"rows":[{"slot":1,"case_id":2,"row":1}]}""",
            """{"version":1,"rows":[{"slot":1,"case_id":"9","row":1}]}""",
            """{"version":1,"rows":[{"slot":1,"case_id":"2","row":3}]}""",
            """{"version":1,"rows":[]}""",
            """{"version":1,"rows":[{"slot":1,"case_id":"2","row":1},{"slot":1,"case_id":"2","row":2}]}""",
            """{"version":1,"rows":[{"slot":1,"case_id":"2","row":1},{"slot":2,"case_id":"2","row":1}]}""",
            """{"version":1,"rows":[{"slot":1,"case_id":"2","row":1},{"slot":2,"case_id":"2","row":2},{"slot":3,"case_id":"9","row":1}]}"""
        }) yield return [layout];
    }

    [Theory]
    [MemberData(nameof(InvalidLayouts))]
    public void MalformedLayoutRejectsWithoutChangingLoadsSlotsSelectionEventsOrRevision(string layout)
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
            service.SelectCase("2");
            string before = LoadSheetTest.Save(service);
            var rows = service.IntensityRows;
            long revision = InputDataService.Instance.CalculationInputRevision;
            int events = 0;
            void Edited() => events++;
            service.LoadsEdited += Edited;
            try
            {
                using var malformed = JsonDocument.Parse(
                    "{\"load\":{\"2\":{\"input_rows\":[1,2]}},\"load_intensity_layout\":" + layout + "}");
                Assert.Throws<JsonException>(() => service.setLoadJson(malformed.RootElement));
                Assert.Equal(before, LoadSheetTest.Save(service));
                Assert.Same(rows, service.IntensityRows);
                Assert.Equal(100000, rows.Count);
                Assert.Equal("2", service.SelectedCaseId);
                Assert.Equal(revision, InputDataService.Instance.CalculationInputRevision);
                Assert.Equal(0, events);
            }
            finally { service.LoadsEdited -= Edited; }
        });
    }

    [Fact]
    public void LaterInvalidDocumentSectionPreservesOldLayoutAndWholeDocumentAtomically()
    {
        var input = InputDataService.Instance;
        int publications = 0;
        void Replaced(long _) => publications++;
        try
        {
            OpenDocument(LoadSheetTest.MixedLayout);
            InputLoadService.Instance.SelectCase("2");
            string before = JsonSerializer.Serialize(input.GetSaveJson());
            long documentRevision = input.DocumentRevision;
            long inputRevision = input.CalculationInputRevision;
            input.FileReplaced += Replaced;
            using var invalid = JsonDocument.Parse("""
                {"dimension":2,"node":{"1":{"x":8}},"load":{"3":{"input_rows":[1]}},
                 "load_intensity_layout":{"version":1,"rows":[{"slot":8,"case_id":"3","row":1}]},
                 "result":{"Case1":{"fsec":{"1":{"0":{"fxi":"bad"}}}}}}
                """);
            Assert.Throws<JsonException>(() => input.JsonDataOpen(invalid.RootElement));
            Assert.Equal(before, JsonSerializer.Serialize(input.GetSaveJson()));
            Assert.Equal(documentRevision, input.DocumentRevision);
            Assert.Equal(inputRevision, input.CalculationInputRevision);
            Assert.Equal("2", InputLoadService.Instance.SelectedCaseId);
            Assert.Equal(0, publications);
        }
        finally { input.FileReplaced -= Replaced; OpenDocument("{}"); }
    }

    [Fact]
    public void AggregateCapacityAcceptsExactlyMaximumAndRejectsOneMoreWithoutTruncation()
    {
        LoadSheetTest.WithLoads(service =>
        {
            int[] allRows = Enumerable.Range(1, 100000).ToArray();
            string atLimit = JsonSerializer.Serialize(new
            {
                load = new Dictionary<string, object>
                {
                    ["1"] = new { input_rows = allRows.Take(50000).ToArray() },
                    ["2"] = new { input_rows = allRows.Skip(50000).ToArray() }
                }
            });
            LoadSheetTest.Load(service, atLimit);
            Assert.Equal(100000, service.AssignedIntensityRowIndices.Count());
            Assert.True(service.IntensityRows[^1].IsAssigned);
            string before = LoadSheetTest.Save(service);
            using var tooMany = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                load = new Dictionary<string, object>
                {
                    ["1"] = new { input_rows = allRows },
                    ["2"] = new { input_rows = new[] { 1 } }
                }
            }));
            Assert.Throws<JsonException>(() => service.setLoadJson(tooMany.RootElement));
            Assert.Equal(before, LoadSheetTest.Save(service));
            Assert.Equal(100000, service.IntensityRows.Count);
        });
    }

    [Fact]
    public void ClearedPayloadReeditedAfterRenumberKeepsUpdatedCaseLocalRow()
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, """
                {"load":{"1":{"load_node":[{"row":1,"n":"11"},{"row":3,"n":"13"}],
                  "load_member":[{"row":3,"m1":"23"}]}}}
                """);
            var row = service.IntensityRows[1];
            row.n = null;
            row.m1 = null;
            Assert.True(service.DeleteIntensityRowsAt([0]));
            var shifted = service.IntensityRows[0];
            Assert.Equal(2, shifted.Row);
            shifted.n = "14";
            shifted.m1 = "24";
            using var saved = JsonDocument.Parse(LoadSheetTest.Save(service));
            var load = saved.RootElement.GetProperty("load").GetProperty("1");
            Assert.Equal(2, Assert.Single(load.GetProperty("load_node").EnumerateArray()).GetProperty("row").GetInt32());
            Assert.Equal(2, Assert.Single(load.GetProperty("load_member").EnumerateArray()).GetProperty("row").GetInt32());
            LoadSheetTest.Load(service, saved.RootElement.GetRawText());
            Assert.Equal("14", service.IntensityRows[0].n);
            Assert.Equal("24", service.IntensityRows[0].m1);
        });
    }

    [Fact]
    public void EditingNewRowUsesMaximumPlusOneAndEmptyAssignmentPersistsWithoutDummyPayload()
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, """{"load":{"2":{"input_rows":[7]}}}""");
            service.IntensityRows[9].LoadId = "2";
            Assert.Equal(8, service.IntensityRows[9].Row);
            using var saved = JsonDocument.Parse(LoadSheetTest.Save(service));
            var load = saved.RootElement.GetProperty("load").GetProperty("2");
            Assert.Equal(new[] { 7, 8 }, load.GetProperty("input_rows").EnumerateArray().Select(r => r.GetInt32()).ToArray());
            if (load.TryGetProperty("load_node", out var nodes)) Assert.Empty(nodes.EnumerateArray());
            if (load.TryGetProperty("load_member", out var members)) Assert.Empty(members.EnumerateArray());
            Assert.Equal(2, saved.RootElement.GetProperty("load_intensity_layout").GetProperty("rows").GetArrayLength());
        });
    }

    private static void OpenDocument(string json)
    {
        using var document = JsonDocument.Parse(json);
        InputDataService.Instance.JsonDataOpen(document.RootElement);
    }

    [Fact]
    public void BothResetObserversSeeFullyInstalledLoadLayoutAndReverseMappingOnDirectReplacementAndClear()
    {
        LoadSheetTest.WithLoads(service =>
        {
            LoadSheetTest.Load(service, LoadSheetTest.MixedLayout);
            var snapshots = new List<string>();
            var names = service.LoadNames;
            var rows = service.IntensityRows;
            void Observe(object? sender, System.ComponentModel.ListChangedEventArgs change)
            {
                if (change.ListChangedType != System.ComponentModel.ListChangedType.Reset) return;
                Assert.Same(rows, service.IntensityRows);
                Assert.Equal(100000, rows.Count);
                Assert.Equal(100000, names.Count);
                string snapshot = LoadSheetTest.Save(service);
                using var parsed = JsonDocument.Parse(snapshot);
                var candidate = InputLoadService.ParseLoadData(parsed.RootElement);
                Assert.Equal(service.AssignedIntensityRowIndices.Count(), candidate.Layout.Count);
                foreach (var (index, identity) in candidate.Layout)
                {
                    Assert.Equal(index, service.FindIntensityRowIndex(identity.CaseId, identity.Row));
                    Assert.Equal(identity.CaseId, service.IntensityRows[index].LoadId);
                }
                snapshots.Add(snapshot);
            }
            names.ListChanged += Observe;
            rows.ListChanged += Observe;
            try
            {
                LoadSheetTest.Load(service, """
                    {"load":{"3":{"input_rows":[5]}},"load_intensity_layout":{"version":1,
                      "rows":[{"slot":100000,"case_id":"3","row":5}]}}
                    """);
                Assert.Equal(2, snapshots.Count);
                Assert.All(snapshots, snapshot => Assert.Equal(LoadSheetTest.Save(service), snapshot));
                snapshots.Clear();
                service.clear();
                Assert.Equal(2, snapshots.Count);
                Assert.All(snapshots, snapshot => Assert.Equal(LoadSheetTest.Save(service), snapshot));
                Assert.Empty(service.AssignedIntensityRowIndices);
            }
            finally { names.ListChanged -= Observe; rows.ListChanged -= Observe; }
        });
    }

    [Theory]
    [InlineData("duplicate-slot")]
    [InlineData("missing-mapping")]
    [InlineData("capacity-overflow")]
    public void InvalidLoadCandidateRejectsWholeDocumentBeforeEarlierValidSectionsPublish(string kind)
    {
        var input = InputDataService.Instance;
        int publications = 0;
        void Replaced(long _) => publications++;
        try
        {
            OpenDocument(LoadSheetTest.MixedLayout);
            InputLoadService.Instance.SelectCase("2");
            string before = JsonSerializer.Serialize(input.GetSaveJson());
            long documentRevision = input.DocumentRevision;
            long inputRevision = input.CalculationInputRevision;
            input.FileReplaced += Replaced;
            string invalid = kind == "capacity-overflow"
                ? JsonSerializer.Serialize(new
                {
                    node = new Dictionary<string, object> { ["8"] = new { x = 8 } },
                    load = new Dictionary<string, object>
                    {
                        ["1"] = new { input_rows = Enumerable.Range(1, 100000).ToArray() },
                        ["2"] = new { input_rows = new[] { 1 } }
                    }
                })
                : "{\"node\":{\"8\":{\"x\":8}},\"load\":{\"3\":{\"input_rows\":[1,2]}}," +
                    "\"load_intensity_layout\":{\"version\":1,\"rows\":" +
                    (kind == "duplicate-slot"
                        ? "[{\"slot\":1,\"case_id\":\"3\",\"row\":1},{\"slot\":1,\"case_id\":\"3\",\"row\":2}]"
                        : "[{\"slot\":1,\"case_id\":\"3\",\"row\":1}]") + "}}";
            using var document = JsonDocument.Parse(invalid);
            Assert.Throws<JsonException>(() => input.JsonDataOpen(document.RootElement));
            Assert.Equal(before, JsonSerializer.Serialize(input.GetSaveJson()));
            Assert.Equal(documentRevision, input.DocumentRevision);
            Assert.Equal(inputRevision, input.CalculationInputRevision);
            Assert.Equal("2", InputLoadService.Instance.SelectedCaseId);
            Assert.Equal(0, publications);
        }
        finally { input.FileReplaced -= Replaced; OpenDocument("{}"); }
    }
}

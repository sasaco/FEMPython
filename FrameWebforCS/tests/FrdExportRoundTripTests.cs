using Convert_Manager.FrameG;
using FrameWebforCS.calculation;
using FrameWebforCS.providers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

// A model saved by the desktop, exported to .frd and opened again must give the same calculation request.
[Collection("DisplacementSingletons")]
public sealed class FrdExportRoundTripTests
{
    private static readonly DateTimeOffset Time = new(2026, 10, 22, 9, 0, 0, TimeSpan.FromHours(9));

    // Gapped IDs, materials shared by several members, a rigid zone, gy loads and a relative L1 row.
    private const string ModelJson = """
    {
      "dimension": 2,
      "node": { "1": { "x": 0, "y": 0 }, "3": { "x": 0, "y": 3 }, "5": { "x": 4, "y": 3 }, "7": { "x": 4, "y": 0 } },
      "member": { "2": { "ni": "1", "nj": "3", "e": "1" }, "4": { "ni": "3", "nj": "5", "e": "2" }, "6": { "ni": "5", "nj": "7", "e": "1" } },
      "rigid": [ { "m": "4", "Ilength": 0.3, "Jlength": 0.25, "e": 3 } ],
      "element": {
        "1": { "1": { "E": 25000000, "Xp": 1E-05, "A": 0.5, "Iz": 0.0417, "n": "柱" },
               "2": { "E": 25000000, "Xp": 1E-05, "A": 0.6, "Iz": 0.05, "n": "梁" },
               "3": { "E": 25000000, "Xp": 1E-05, "A": 1000, "Iz": 1000, "n": "剛域" } },
        "2": { "1": { "E": 25000000, "Xp": 1E-05, "A": 0.45, "Iz": 0.04, "n": "柱" },
               "2": { "E": 25000000, "Xp": 1E-05, "A": 0.55, "Iz": 0.045, "n": "梁" },
               "3": { "E": 25000000, "Xp": 1E-05, "A": 1000, "Iz": 1000, "n": "剛域" } }
      },
      "fix_node": {
        "1": [ { "row": 1, "n": "1", "tx": 1, "ty": 1, "rz": 1 }, { "row": 2, "n": "7", "tx": 0, "ty": 1, "rz": 0 } ],
        "2": [ { "row": 1, "n": "1", "tx": 1, "ty": 1, "rz": 0 }, { "row": 2, "n": "7", "tx": 5000, "ty": 1, "rz": 0 } ]
      },
      "fix_member": { "1": [ { "row": 1, "m": "4", "tx": 100, "ty": 2000 } ] },
      "joint": { "1": [ { "row": 1, "m": "4", "xi": 1, "yi": 1, "zi": 0, "xj": 1, "yj": 1, "zj": 1 } ] },
      "notice_points": [ { "row": 1, "m": "2", "Points": [ 1.5, 3 ] } ],
      "load": {
        "1": { "fix_node": 1, "fix_member": 1, "element": 1, "joint": 1, "symbol": "D", "name": "死荷重",
          "load_node": [ { "row": 2, "n": "3", "tx": 10, "ty": -20, "rz": 5 } ],
          "load_member": [
            { "row": 1, "m1": "4", "m2": "4", "direction": "gy", "mark": "2", "L1": "0", "L2": "0", "P1": -10, "P2": -10 },
            { "row": 3, "m1": "6", "m2": "6", "direction": "y", "mark": "1", "L1": "1.5", "L2": "0", "P1": 30 } ] },
        "2": { "fix_node": 2, "element": 2, "symbol": "T", "name": "温度・水平",
          "load_member": [
            { "row": 1, "m1": "2", "m2": "2", "direction": "x", "mark": "9", "P1": 10 },
            { "row": 2, "m1": "4", "direction": "x", "mark": "2", "L1": "1", "L2": "-1.5", "P1": 3, "P2": 3 },
            { "row": 3, "m1": "4", "direction": "gx", "mark": "2", "L1": "-0.5", "L2": "-1", "P1": 2, "P2": 2 },
            { "row": 4, "m1": "2", "direction": "z", "mark": "11", "L1": "1", "P1": 50 } ] }
      },
      "define": { "1": { "row": 1, "C1": 1, "C2": 2 } },
      "combine": { "1": { "row": 1, "name": "基本", "C1": 1.2 } },
      "pickup": { "1": { "row": 1, "name": "最大", "C1": 1 } }
    }
    """;

    [Fact]
    public void A4_2FrameGModelGivesTheSameRequestAfterExport()
    {
        using JsonDocument opened = InputFileLoader.Open(Sample("4_2-FrameG.frd"));
        string saved = Save(opened.RootElement);

        FrdExportResult result = Export(saved);
        string reopened = Reopen(result.Frd);

        AssertSameModel(saved, reopened, result);
    }

    [Fact]
    public void ADesktopModelWithFeaturesFrameGLacksGivesTheSameRequestAfterExport()
    {
        using JsonDocument model = JsonDocument.Parse(ModelJson);
        string saved = Save(model.RootElement);

        FrdExportResult result = Export(saved);
        Assert.Contains(result.Warnings, warning => warning.Code == FrdDiagnosticCodes.Renumbered && warning.Path == "member");
        string reopened = Reopen(result.Frd);

        AssertSameModel(saved, reopened, result);
        var derived = JsonNode.Parse(reopened)!;
        Assert.Equal(1, (int)derived["define"]!["1"]!["C1"]!);
        Assert.Equal(2, (int)derived["define"]!["1"]!["C2"]!);
        Assert.Equal(1.2, (double)derived["combine"]!["1"]!["C1"]!, 6);
        Assert.Equal("最大", (string)derived["pickup"]!["1"]!["name"]!);
        Assert.Equal(1, (int)derived["pickup"]!["1"]!["C1"]!);
    }

    [Theory]
    [InlineData("バネ連衡あり.frd")]
    [InlineData("杭2こ.lzh")]
    public void PileSpringSamplesAreRefusedWithThePileSpringCode(string fileName)
    {
        string path = Sample(fileName, asFrd: true);
        try
        {
            using JsonDocument opened = InputFileLoader.Open(path);
            FrdExportResult result = FrdExporter.Export(opened.RootElement.GetRawText(), new FrdExportOptions { Time = Time });

            Assert.Null(result.Frd);
            Assert.Contains(result.Errors, error => error.Code == FrdDiagnosticCodes.PileSpring);
        }
        finally
        {
            if (path != Sample(fileName))
                File.Delete(path);
        }
    }

    private static FrdExportResult Export(string saved)
    {
        FrdExportResult result = FrdExporter.Export(saved, new FrdExportOptions { Title = "round trip", Time = Time });
        Assert.True(result.Succeeded, string.Join("\n", result.Errors));
        return result;
    }

    private static string Save(JsonElement input)
    {
        try
        {
            InputDataService.Instance.JsonDataOpen(input);
            return JsonSerializer.Serialize(InputDataService.Instance.GetSaveJson());
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            InputDataService.Instance.JsonDataOpen(empty.RootElement);
        }
    }

    private static string Reopen(byte[] frd)
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".frd");
        try
        {
            File.WriteAllBytes(fileName, frd);
            using JsonDocument reopened = InputFileLoader.Open(fileName);
            return Save(reopened.RootElement);
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    private static void AssertSameModel(string saved, string reopened, FrdExportResult result)
    {
        var expected = Normalize(CalculationRequestBuilder.FromSavedJson(TrimSupportNodes(saved)).Json, result.NodeNumbers, result.MemberNumbers);
        var actual = Normalize(CalculationRequestBuilder.FromSavedJson(TrimSupportNodes(reopened)).Json, null, null);
        foreach (var (section, value) in expected)
            Assert.True(JsonNode.DeepEquals(value, actual[section]),
                $"{section}\nexpected: {value?.ToJsonString()}\nactual: {actual[section]?.ToJsonString()}");
        Assert.Equal(expected.Select(pair => pair.Key).Order(), actual.Select(pair => pair.Key).Order());
    }

    // Two ConvertManager behaviours the calculation request rejects, both outside the export, are
    // evened out on both sides: it keeps the trailing space FrameG writes after a support's node
    // number (fix_node.cs), and it reads a temperature load (mark 9) with no direction (load.cs),
    // which FrameWeb does not use for mark 9.
    private static string TrimSupportNodes(string saved)
    {
        var root = JsonNode.Parse(saved)!.AsObject();
        if (root["fix_node"] is JsonObject sheets)
            foreach (var (_, rows) in sheets)
                foreach (var row in rows?.AsArray() ?? new JsonArray())
                    if (row?["n"] is JsonValue id && id.TryGetValue(out string? text))
                        row["n"] = text.Trim();
        if (root["load"] is JsonObject loads)
            foreach (var (_, load) in loads)
                foreach (var row in load?["load_member"]?.AsArray() ?? new JsonArray())
                    if (row?["mark"]?.ToString() == "9")
                        row["direction"] = "x";
        return root.ToJsonString();
    }

    // Maps IDs through the export's renumbering and replaces material IDs by their values, since
    // FrameG gives every member its own material. Row numbers and names are not compared.
    private static JsonObject Normalize(string requestJson, IReadOnlyDictionary<string, int>? nodeNumbers,
        IReadOnlyDictionary<string, int>? memberNumbers)
    {
        var request = JsonNode.Parse(requestJson)!.AsObject();
        string NodeId(JsonNode? id) => Map(id, nodeNumbers);
        string MemberId(JsonNode? id) => Map(id, memberNumbers);
        var elements = request["element"]!.AsObject();
        JsonArray Material(JsonNode? id, params string[] keys) => new(elements
            .OrderBy(sheet => int.Parse(sheet.Key, CultureInfo.InvariantCulture))
            .Select(sheet => sheet.Value![Map(id, null)] is JsonObject material
                ? (JsonNode)new JsonArray(keys.Select(key => (JsonNode?)Round(material[key])).ToArray())
                : null)
            .ToArray());

        var result = new JsonObject
        {
            ["node"] = new JsonObject(request["node"]!.AsObject()
                .Select(pair => KeyValuePair.Create(NodeId(JsonValue.Create(pair.Key)), (JsonNode?)new JsonArray(Round(pair.Value!["x"]), Round(pair.Value["y"]))))
                .OrderBy(pair => int.Parse(pair.Key, CultureInfo.InvariantCulture))),
            ["member"] = new JsonObject(request["member"]!.AsObject()
                .Select(pair => KeyValuePair.Create(MemberId(JsonValue.Create(pair.Key)), (JsonNode?)new JsonObject
                {
                    ["ni"] = NodeId(pair.Value!["ni"]),
                    ["nj"] = NodeId(pair.Value["nj"]),
                    ["material"] = Material(pair.Value["e"], "E", "Xp", "A", "Iz")
                }))
                .OrderBy(pair => int.Parse(pair.Key, CultureInfo.InvariantCulture))),
            ["rigid"] = new JsonArray(request["rigid"]!.AsArray()
                .Select(row => (JsonNode?)new JsonObject
                {
                    ["m"] = MemberId(row!["m"]),
                    ["Ilength"] = Round(row["Ilength"]),
                    ["Jlength"] = Round(row["Jlength"]),
                    ["material"] = Material(row["e"], "A", "Iz")
                })
                .OrderBy(row => row!["m"]!.GetValue<string>(), StringComparer.Ordinal).ToArray()),
            ["notice_points"] = new JsonArray((request["notice_points"]?.AsArray() ?? new JsonArray())
                .Select(row => (JsonNode?)new JsonObject { ["m"] = MemberId(row!["m"]), ["Points"] = row["Points"]!.DeepClone() })
                .ToArray()),
        };
        foreach (string section in new[] { "fix_node", "fix_member", "joint" })
            if (request[section] is JsonObject sheets)
                result[section] = new JsonObject(sheets
                    .Where(sheet => sheet.Value is JsonArray rows && rows.Count > 0)
                    .Select(sheet => KeyValuePair.Create(sheet.Key, (JsonNode?)new JsonArray(sheet.Value!.AsArray()
                        .Select(row => Row(row!.AsObject(), section == "fix_node" ? "n" : "m", section == "fix_node" ? NodeId : MemberId))
                        .OrderBy(row => row!.ToJsonString(), StringComparer.Ordinal).ToArray())))
                    .OrderBy(pair => int.Parse(pair.Key, CultureInfo.InvariantCulture)));
        result["load"] = new JsonObject(request["load"]!.AsObject().Select(pair =>
        {
            var load = pair.Value!.AsObject();
            var copy = new JsonObject();
            foreach (string key in new[] { "fix_node", "fix_member", "element", "joint", "symbol" })
                copy[key] = load[key]?.DeepClone();
            copy["load_node"] = new JsonArray((load["load_node"]?.AsArray() ?? new JsonArray())
                .Select(row => Row(row!.AsObject(), "n", NodeId)).ToArray());
            copy["load_member"] = new JsonArray((load["load_member"]?.AsArray() ?? new JsonArray())
                .Select(row => Row(row!.AsObject(), "m", MemberId)).ToArray());
            return KeyValuePair.Create(pair.Key, (JsonNode?)copy);
        }));
        return result;
    }

    private static JsonNode Row(JsonObject row, string idKey, Func<JsonNode?, string> map)
    {
        var copy = new JsonObject();
        foreach (var (key, value) in row.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            // xi/yi/xj/yj release the out-of-plane rotations. ConvertManager does not read them, and
            // Complete2D does not reset them because joint sheets are arrays, so they are left out.
            if (key is "row" or "_originalP1" or "xi" or "yi" or "xj" or "yj")
                continue;
            copy[key] = key == idKey ? map(value) : value is JsonValue ? Round(value) : value?.DeepClone();
        }
        return copy;
    }

    private static string Map(JsonNode? id, IReadOnlyDictionary<string, int>? numbers)
    {
        string text = id is JsonValue value && value.TryGetValue(out double number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : (id?.GetValue<string>() ?? "").Trim();
        return numbers != null && numbers.TryGetValue(text, out int mapped) ? mapped.ToString(CultureInfo.InvariantCulture) : text;
    }

    // Values pass through float32 in the desktop, so compare them at float precision.
    private static JsonNode? Round(JsonNode? value) =>
        value is JsonValue number && number.TryGetValue(out double d) ? JsonValue.Create((double)(float)d) : value?.DeepClone();

    private static string Sample(string fileName, bool asFrd = false)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameGConverter")))
            directory = directory.Parent;
        string path = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException(),
            "FrameGConverter", "Convert_Test", "TestData", fileName);
        if (!asFrd || path.EndsWith(".frd", StringComparison.OrdinalIgnoreCase))
            return path;
        // InputFileLoader decides by extension; the .lzh sample is the same archive format.
        string copy = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".frd");
        File.Copy(path, copy);
        return copy;
    }
}

using FrameWebforCS.components.input;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrameWebforCS.providers;

internal static class InputFileLoader
{
    internal static JsonDocument Open(string fileName)
    {
        using FileStream file = File.OpenRead(fileName);
        if (!Path.GetExtension(fileName).Equals(".frd", StringComparison.OrdinalIgnoreCase))
            return JsonDocument.Parse(file);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string json = new ConvertManager(file).getJsonString();
        JsonObject root = JsonNode.Parse(json)?.AsObject() ??
            throw new JsonException("The converted FRD data is empty.");
        // The converter can emit 16 legacy sheets; the desktop exposes six of each type.
        RemoveEmptyUnsupportedSheets(root, "fix_node", InputFixNodeService.TypeCount);
        RemoveEmptyUnsupportedSheets(root, "fix_member", InputFixMemberService.TypeCount);
        RemoveEmptyUnsupportedSheets(root, "joint", InputJointService.TypeCount);
        // Converter notice-point rows are zero-based; saved desktop rows are one-based.
        if (root["notice_points"] is JsonArray noticePoints)
            foreach (JsonNode? point in noticePoints)
                if (point is JsonObject row && row["row"] is JsonValue index &&
                    index.TryGetValue(out int zeroBasedRow))
                    row["row"] = checked(zeroBasedRow + 1);
        root["dimension"] = 2;
        return JsonDocument.Parse(root.ToJsonString());
    }

    private static void RemoveEmptyUnsupportedSheets(JsonObject root, string section, int supportedCount)
    {
        if (root[section] is not JsonObject sheets) return;
        foreach (string name in sheets.Select(sheet => sheet.Key).ToArray())
        {
            if (!int.TryParse(name, out int number) || number <= supportedCount) continue;
            if (sheets[name] is not JsonArray rows || rows.Count != 0)
                throw new InvalidDataException($"{section} のシート {name} は読み込み可能な範囲を超えています。");
            sheets.Remove(name);
        }
    }
}

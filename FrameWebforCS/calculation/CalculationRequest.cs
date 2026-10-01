using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FrameWebforCS.components.input;

namespace FrameWebforCS.calculation;

internal sealed record CalculationRequest(string Json);

internal sealed class CalculationRequestException(string message) : Exception(message);

/// <summary>Projects a detached save snapshot into the Angular getInputJson(0) legacy solver map.</summary>
internal static class CalculationRequestBuilder
{
    private static readonly string[] OptionalSections =
        ["fix_node", "fix_member", "joint", "shell", "notice_points"];
    private static readonly string[] LoadComponents = ["tx", "ty", "tz", "rx", "ry", "rz"];

    internal static CalculationRequest FromSavedJson(string savedJson)
    {
        JsonObject saved;
        try { saved = JsonNode.Parse(savedJson) as JsonObject ?? throw new JsonException(); }
        catch (JsonException error) { throw new CalculationRequestException($"Invalid input snapshot: {error.Message}"); }

        int dimension = (int?)saved["dimension"] ?? 3;
        if (dimension is not (2 or 3)) throw new CalculationRequestException("dimension must be 2 or 3.");
        var result = new JsonObject { ["ver"] = "2.5.12" };
        foreach (string section in new[] { "node", "member", "element", "rigid", "load" })
            result[section] = saved[section]?.DeepClone() ?? (section == "rigid" ? new JsonArray() : new JsonObject());
        foreach (string section in OptionalSections)
            if (saved[section] is JsonObject value && value.Count > 0)
                result[section] = value.DeepClone();
        NormalizeConstraintRows(result, "fix_node", "n", LoadComponents);

        var nodes = Object(result, "node");
        var members = Object(result, "member");
        var shells = result["shell"] as JsonObject ?? new JsonObject();
        var elements = Object(result, "element");
        if (nodes.Count == 0) throw new CalculationRequestException("At least one node is required.");
        if (members.Count + shells.Count == 0)
            throw new CalculationRequestException("At least one member or shell is required.");

        var usedNodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, value) in members)
        {
            var member = value as JsonObject ?? throw new CalculationRequestException($"member.{id} must be an object.");
            foreach (string end in new[] { "ni", "nj" })
            {
                string nodeId = Id(member[end]);
                if (!nodes.ContainsKey(nodeId)) throw new CalculationRequestException($"member.{id}.{end}: node {nodeId} does not exist.");
                usedNodes.Add(nodeId);
                member[end] = int.Parse(nodeId, CultureInfo.InvariantCulture);
            }
            if (Number(member["e"]) is double elementId) member["e"] = elementId;
            DefaultNumber(member, "cg");
        }
        NormalizeJointRows(result, members);
        foreach (var (id, value) in shells)
        {
            var shell = value as JsonObject ?? throw new CalculationRequestException($"shell.{id} must be an object.");
            var corners = shell["nodes"] as JsonArray ?? throw new CalculationRequestException($"shell.{id}.nodes is required.");
            if (corners.Count is not (3 or 4)) throw new CalculationRequestException($"shell.{id}.nodes needs three or four corners.");
            foreach (var corner in corners)
            {
                string nodeId = Id(corner);
                if (!nodes.ContainsKey(nodeId)) throw new CalculationRequestException($"shell.{id}: node {nodeId} does not exist.");
                usedNodes.Add(nodeId);
            }
            if (corners.Select(Id).Distinct().Count() != corners.Count)
                throw new CalculationRequestException($"shell.{id} contains duplicate nodes.");
            if (corners.Count == 4) ValidatePlanarity(id, corners, nodes);
        }
        foreach (string id in nodes.Select(pair => pair.Key).Where(id => !usedNodes.Contains(id)).ToArray())
            nodes.Remove(id);
        foreach (var (_, value) in nodes)
        {
            var node = value as JsonObject ?? throw new CalculationRequestException("node must be an object.");
            foreach (string axis in new[] { "x", "y", "z" }) DefaultNumber(node, axis);
        }
        NormalizeMemberSpringRows(result, members, nodes);
        foreach (var (_, sheetValue) in elements)
        {
            if (sheetValue is not JsonObject sheet) throw new CalculationRequestException("element sheet must be an object.");
            foreach (var (_, value) in sheet)
                if (value is JsonObject element)
                {
                    foreach (string key in new[] { "E", "G", "Xp", "A", "J", "Iy", "Iz" })
                        DefaultNumber(element, key);
                    element["n"] = String(element["n"]);
                    if (dimension == 2)
                        foreach (string key in new[] { "G", "J", "Iy" }) element[key] = 1;
                }
        }

        result["load"] = ProjectLoads(Object(result, "load"), nodes, members, elements,
            result["fix_node"] as JsonObject, result["fix_member"] as JsonObject, dimension);
        var loads = Object(result, "load");
        if (loads.Count == 0) throw new CalculationRequestException("At least one load case is required.");
        if (dimension == 2) Complete2D(result);
        result["dimension"] = dimension;
        return new CalculationRequest(result.ToJsonString());
    }

    private static JsonObject ProjectLoads(JsonObject savedLoads, JsonObject nodes, JsonObject members,
        JsonObject elements, JsonObject? supports, JsonObject? springs, int dimension)
    {
        var projected = new JsonObject();
        foreach (var (caseId, caseValue) in savedLoads.OrderBy(pair =>
            double.TryParse(pair.Key, NumberStyles.Float, CultureInfo.InvariantCulture, out double id) ? id : double.MaxValue))
        {
            var source = caseValue as JsonObject ?? throw new CalculationRequestException($"load.{caseId} must be an object.");
            var selector = new JsonObject();
            foreach (string key in new[] { "fix_node", "fix_member", "element", "joint" })
                selector[key] = Number(source[key]) is 0 or null ? 1 : (int)Number(source[key])!.Value;
            bool hasNameRow = !string.IsNullOrWhiteSpace(String(source["symbol"])) ||
                !string.IsNullOrWhiteSpace(String(source["name"])) ||
                new[] { "fix_node", "fix_member", "element", "joint" }.Any(key => Number(source[key]) != null);
            if (hasNameRow)
            {
                selector["symbol"] = String(source["symbol"]);
                selector["LL_pitch"] = Number(source["LL_pitch"]) ?? 0.1;
            }
            var nodeRows = new JsonArray();
            if (source["load_node"] is JsonArray rawNodeRows)
                foreach (var rowValue in rawNodeRows)
                {
                    if (rowValue is not JsonObject row || Number(row["n"]) is not double signed || signed == 0 || signed != Math.Truncate(signed)) continue;
                    if (!LoadComponents.Any(key => Number(row[key]) != null)) continue;
                    string nodeId = Math.Abs(signed).ToString(CultureInfo.InvariantCulture);
                    if (!nodes.ContainsKey(nodeId)) throw new CalculationRequestException($"load.{caseId}: node {nodeId} does not exist.");
                    var item = new JsonObject { ["n"] = nodeId, ["row"] = (int?)row["row"] ?? 0 };
                    for (int i = 0; i < LoadComponents.Length; i++)
                    {
                        string target = signed > 0 ? LoadComponents[i] : new[] { "dx", "dy", "dz", "ax", "ay", "az" }[i];
                        item[target] = (Number(row[LoadComponents[i]]) ?? 0) / (signed > 0 ? 1 : 1000);
                    }
                    nodeRows.Add(item);
                }
            var memberRows = new List<JsonObject>();
            if (source["load_member"] is JsonArray rawMemberRows)
                foreach (var row in NormalizeRelativeMemberRows(rawMemberRows, members, nodes))
                    memberRows.AddRange(ExpandMemberRow(row, members, caseId));
            if (nodeRows.Count == 0 && memberRows.Count == 0) continue;
            int elementId = (int)Number(selector["element"])!.Value;
            if (!elements.ContainsKey(elementId.ToString(CultureInfo.InvariantCulture)))
                throw new CalculationRequestException($"load.{caseId}: element {elementId} does not exist.");
            string supportId = Id(selector["fix_node"]);
            string springId = Id(selector["fix_member"]);
            var supportRows = supports?[supportId] as JsonArray;
            var springRows = springs?[springId] as JsonArray;
            if ((supportRows?.Count ?? 0) + (springRows?.Count ?? 0) == 0)
                throw new CalculationRequestException($"load.{caseId}: support or spring is required.");
            if (supportRows != null)
                foreach (var value in supportRows)
                    if (value is JsonObject row && !nodes.ContainsKey(Id(row["n"])))
                        throw new CalculationRequestException($"fix_node.{supportId}: node {Id(row["n"])} does not exist.");
            if (springRows != null)
                foreach (var value in springRows)
                    if (value is JsonObject row && !members.ContainsKey(Id(row["m"])))
                        throw new CalculationRequestException($"fix_member.{springId}: member {Id(row["m"])} does not exist.");
            var positions = String(selector["symbol"]) == "LL"
                ? MovingPositions(source, memberRows, members, nodes, caseId) : new List<double> { 0 };
            int digit = (int)Math.Pow(10, (positions.Count - 1).ToString(CultureInfo.InvariantCulture).Length);
            string? firstEffectiveId = null;
            for (int index = 0; index < positions.Count; index++)
            {
                string id = index == 0 ? caseId : (double.Parse(caseId, CultureInfo.InvariantCulture) +
                    index / (double)digit).ToString(CultureInfo.InvariantCulture);
                var item = (JsonObject)selector.DeepClone();
                if (nodeRows.Count > 0 && index == 0) item["load_node"] = nodeRows.DeepClone();
                var positioned = PositionMemberRows(memberRows, positions[index], members, nodes);
                if (positioned.Count > 0) item["load_member"] = positioned;
                if (item.ContainsKey("load_node") || item.ContainsKey("load_member"))
                {
                    if (projected.ContainsKey(id)) throw new CalculationRequestException($"Duplicate projected load case {id}.");
                    projected[id] = item;
                    firstEffectiveId ??= id;
                }
            }
            // A moving load may not reach the structure at position zero. Keep the
            // first actual load under its integral case ID so result/print grouping
            // has a parent, independent of other load cases in the request.
            if (positions.Count > 1 && !projected.ContainsKey(caseId) && firstEffectiveId != null)
            {
                JsonNode firstEffective = projected[firstEffectiveId]!.DeepClone();
                projected.Remove(firstEffectiveId);
                projected[caseId] = firstEffective;
            }
        }
        // JavaScript object enumeration puts integer case IDs before decimal child IDs.
        var ordered = new JsonObject();
        foreach (var pair in projected.Where(pair => !pair.Key.Contains('.'))
            .OrderBy(pair => double.Parse(pair.Key, CultureInfo.InvariantCulture))
            .Concat(projected.Where(pair => pair.Key.Contains('.'))))
            ordered[pair.Key] = pair.Value?.DeepClone();
        return ordered;
    }

    private static IEnumerable<JsonObject> ExpandMemberRow(JsonObject row, JsonObject members, string caseId)
    {
        double? first = Number(row["m1"]), last = Number(row["m2"]), mark = Number(row["mark"]);
        string direction = String(row["direction"]).Trim().ToLowerInvariant();
        if (first == null && last == null || mark == null || direction.Length == 0) yield break;
        first ??= 0;
        last ??= 0;
        if (first == 0) first = last;
        if (last == 0) last = first;
        if (first == null || last == null || first == 0 || last == 0 ||
            first != Math.Truncate(first.Value) || last != Math.Truncate(last.Value)) yield break;
        if (Math.Abs(first.Value) > Math.Abs(last.Value)) (first, last) = (-last, -first);
        int begin = (int)Math.Abs(first.Value), end = (int)Math.Abs(last.Value);
        if (last < 0 && mark is 1 or 11) end = begin;
        for (int member = begin; member <= end; member++)
        {
            string id = member.ToString(CultureInfo.InvariantCulture);
            if (!members.ContainsKey(id)) throw new CalculationRequestException($"load.{caseId}: member {id} does not exist.");
            var projected = new JsonObject
            {
                ["m"] = member, ["direction"] = mark == 9 ? "x" : direction,
                ["mark"] = mark, ["L1"] = Number(row["L1"]) ?? 0,
                ["L2"] = Number(row["L2"]) ?? 0,
                ["P1"] = member == begin ? Number(row["P1"]) ?? 0 :
                    Number(row["_originalP1"]) ?? Number(row["P1"]) ?? 0,
                ["P2"] = mark == 9 ? Number(row["P1"]) ?? 0 : Number(row["P2"]) ?? 0,
                ["row"] = (int?)row["row"] ?? 0
            };
            yield return projected;
        }
    }

    private static IEnumerable<JsonObject> NormalizeRelativeMemberRows(JsonArray source,
        JsonObject members, JsonObject nodes)
    {
        double previousEnd = 0;
        int previousRow = -1;
        foreach (var value in source.OfType<JsonObject>().OrderBy(row => (int?)row["row"] ?? 0))
        {
            var row = (JsonObject)value.DeepClone();
            int rowNumber = (int?)row["row"] ?? 0;
            double rawStart = Number(row["L1"]) ?? 0;
            double rawEnd = Number(row["L2"]) ?? 0;
            bool continuation = previousRow + 1 == rowNumber;
            double start = !continuation ? rawStart :
                rawStart < 0 ? previousEnd + Math.Abs(rawStart) : rawStart;
            double end = rawEnd < 0 ? start + Math.Abs(rawEnd) : rawEnd;
            previousRow = rowNumber;
            int mark = (int)(Number(row["mark"]) ?? 0);
            int member = (int)Math.Abs(Number(row["m2"]) ?? Number(row["m1"]) ?? 0);
            // Angular's next relative L1 starts at the previous load's actual end.
            // For a distributed load, L2 is the right margin from the member end.
            previousEnd = mark is 1 or 11 || member == 0 || rawEnd < 0
                ? end : MemberLength(member.ToString(CultureInfo.InvariantCulture), members, nodes) - end;
            if (mark is 1 or 11)
            {
                if (start < 0) { row["P1"] = 0; start = 0; }
                row["L1"] = start;
                row["L2"] = end;
            }
            else if (mark == 2 && start < 0)
            {
                double distance = Math.Abs(start) + Math.Abs(end);
                double p1 = Number(row["P1"]) ?? 0;
                double p2 = Number(row["P2"]) ?? 0;
                row["_originalP1"] = p1;
                if (distance > 0) row["P1"] = p1 + Math.Abs(start) * (p2 - p1) / distance;
                row["L1"] = 0;
                row["L2"] = -end;
            }
            else if (rawStart < 0 && continuation)
                row["L1"] = start;
            yield return row;
        }
    }

    private static List<double> MovingPositions(JsonObject source, List<JsonObject> rows,
        JsonObject members, JsonObject nodes, string caseId)
    {
        if (rows.Count == 0) return [0];
        double pitch = Number(source["LL_pitch"]) ?? 0.1;
        if (!double.IsFinite(pitch) || pitch <= 0)
            throw new CalculationRequestException($"load.{caseId}: LL_pitch must be positive.");
        var rawRows = (source["load_member"] as JsonArray)?.OfType<JsonObject>()
            .OrderBy(row => (int?)row["row"] ?? 0).ToArray() ?? [];
        if (rawRows.Length == 0) return [0];
        var first = rawRows[0];
        int begin = (int)Math.Abs(Number(first["m1"]) ?? 0);
        int end = (int)Math.Abs(Number(first["m2"]) ?? 0);
        if (begin == 0) begin = end;
        if (end == 0) end = begin;
        if (begin > end) (begin, end) = (end, begin);
        double length = 0;
        for (int member = begin; member <= end; member++)
            length += JsRound(MemberLength(member.ToString(CultureInfo.InvariantCulture), members, nodes) * 1000) / 1000;
        double beforeStart = rawRows.Sum(row => Math.Max(0, -(Number(row["L1"]) ?? 0)) +
            Math.Max(0, -(Number(row["L2"]) ?? 0)));
        double start = -beforeStart;
        int firstRow = (int?)first["row"] ?? 0;
        foreach (var row in rows.Where(row => (int?)row["row"] == firstRow)) row["L1"] = start;
        var result = new List<double> { 0 };
        double count = Math.Round((length + Math.Abs(start)) / pitch, 1,
            MidpointRounding.AwayFromZero);
        for (int index = 1; index <= count; index++)
            result.Add(Math.Round(index * pitch, 3, MidpointRounding.AwayFromZero));
        return result;
    }

    private static JsonArray PositionMemberRows(List<JsonObject> rows, double offset, JsonObject members, JsonObject nodes)
    {
        var output = new JsonArray();
        foreach (var source in rows)
        {
            var row = (JsonObject)source.DeepClone();
            double length = MemberLength(Id(row["m"]), members, nodes);
            double start = (Number(row["L1"]) ?? 0) + offset;
            double end = Number(row["L2"]) ?? 0;
            int mark = (int)(Number(row["mark"]) ?? 0);
            if (mark is 1 or 11)
            {
                if (start <= 0 && end <= 0) continue;
                if (start < 0 || start > length) { start = 0; row["P1"] = 0; }
                if (end < 0 || end > length) { end = 0; row["P2"] = 0; }
            }
            else if (end < 0)
                end = Math.Max(0, length - start - Math.Abs(end));
            else if (start + end > length)
            {
                start = end = 0; row["P1"] = row["P2"] = 0;
            }
            row["L1"] = JsRound(start * 1000) / 1000;
            row["L2"] = JsRound(end * 1000) / 1000;
            row["P1"] = JsRound((Number(row["P1"]) ?? 0) * 100) / 100;
            row["P2"] = JsRound((Number(row["P2"]) ?? 0) * 100) / 100;
            if ((Number(row["P1"]) ?? 0) != 0 || (Number(row["P2"]) ?? 0) != 0)
                output.Add(row);
        }
        return output;
    }

    private static double MemberLength(string id, JsonObject members, JsonObject nodes)
    {
        var member = members[id] as JsonObject ?? throw new CalculationRequestException($"member {id} does not exist.");
        var ni = nodes[Id(member["ni"])] as JsonObject ?? throw new CalculationRequestException($"member {id}: ni does not exist.");
        var nj = nodes[Id(member["nj"])] as JsonObject ?? throw new CalculationRequestException($"member {id}: nj does not exist.");
        double dx = (Number(nj["x"]) ?? 0) - (Number(ni["x"]) ?? 0);
        double dy = (Number(nj["y"]) ?? 0) - (Number(ni["y"]) ?? 0);
        double dz = (Number(nj["z"]) ?? 0) - (Number(ni["z"]) ?? 0);
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static void Complete2D(JsonObject result)
    {
        foreach (var (_, value) in Object(result, "node")) if (value is JsonObject row) row["z"] = 0;
        if (result["joint"] is JsonObject joints)
            foreach (var (_, sheetValue) in joints)
                if (sheetValue is JsonObject sheet)
                    foreach (var (_, value) in sheet)
                        if (value is JsonObject row)
                            foreach (string key in new[] { "xi", "xj", "yi", "yj" }) row[key] = 1;
        var supports = result["fix_node"] as JsonObject ?? new JsonObject();
        foreach (var (_, caseValue) in Object(result, "load"))
        {
            if (caseValue is not JsonObject load) continue;
            if (load["load_node"] is JsonArray nodeLoads)
                foreach (var value in nodeLoads)
                    if (value is JsonObject row) { row["tz"] = 0; row["rx"] = 0; row["ry"] = 0; }
            string supportId = Id(load["fix_node"]);
            if (!supports.ContainsKey(supportId)) supports[supportId] = new JsonArray();
        }
        foreach (var (id, _) in Object(result, "node"))
            foreach (var (_, values) in supports)
                if (values is JsonArray rows)
                {
                    var match = rows.OfType<JsonObject>().FirstOrDefault(row => Id(row["n"]) == id);
                    if (match == null) rows.Add(new JsonObject { ["n"] = id, ["tz"] = 1, ["rx"] = 1, ["ry"] = 1 });
                    else { match["tz"] = 1; match["rx"] = 1; match["ry"] = 1; }
                }
        result["fix_node"] = supports;
        if (result["fix_member"] is JsonObject springs)
            foreach (var (_, values) in springs)
                if (values is JsonArray rows)
                    foreach (var value in rows)
                        if (value is JsonObject row) { row["tz"] = 0; row["tr"] = 0; }
    }

    private static void NormalizeConstraintRows(JsonObject result, string section, string idKey,
        string[] components)
    {
        if (result[section] is not JsonObject groups) return;
        foreach (string groupId in groups.Select(item => item.Key).ToArray())
        {
            if (groups[groupId] is not JsonArray source)
                throw new CalculationRequestException($"{section}.{groupId} must be an array.");
            var rows = new JsonArray();
            foreach (var value in source)
            {
                if (value is not JsonObject row) continue;
                if (Number(row[idKey]) == null || !components.Any(key => (Number(row[key]) ?? 0) != 0))
                    continue;
                var projected = new JsonObject { ["row"] = (int?)row["row"] ?? 0,
                    [idKey] = String(row[idKey]) };
                foreach (string key in components) projected[key] = Number(row[key]) ?? 0;
                rows.Add(projected);
            }
            if (rows.Count == 0) groups.Remove(groupId);
            else groups[groupId] = rows;
        }
        if (groups.Count == 0) result.Remove(section);
    }

    private static void NormalizeMemberSpringRows(JsonObject result, JsonObject members, JsonObject nodes)
    {
        if (result["fix_member"] is not JsonObject groups) return;
        foreach (string caseId in groups.Select(item => item.Key).ToArray())
        {
            if (groups[caseId] is not JsonArray source)
                throw new CalculationRequestException($"fix_member.{caseId} must be an array.");
            var typed = new List<clsFixMember>();
            foreach (JsonNode? value in source)
            {
                if (value is not JsonObject row)
                    throw new CalculationRequestException($"fix_member.{caseId} contains an invalid row.");
                string memberId = Id(row["m"]);
                if (!members.ContainsKey(memberId))
                    throw new CalculationRequestException($"fix_member.{caseId}: member {memberId} does not exist.");
                double? length = row["length"] == null ? null : Number(row["length"]);
                if (row["length"] != null && (length == null || length <= 0 || length > float.MaxValue))
                    throw new CalculationRequestException($"fix_member.{caseId}: invalid length for member {memberId}.");
                typed.Add(new clsFixMember
                {
                    row = (int?)row["row"] ?? 0, m = memberId,
                    length = length is double metres ? (float)metres : null,
                    tx = (float)(Number(row["tx"]) ?? 0),
                    ty = (float)(Number(row["ty"]) ?? 0),
                    tz = (float)(Number(row["tz"]) ?? 0),
                    tr = (float)(Number(row["tr"]) ?? 0)
                });
            }
            foreach (var byMember in typed.GroupBy(row => row.m))
            {
                try { MemberSpringIntervals.Resolve(byMember, MemberLength(byMember.Key!, members, nodes)); }
                catch (ArgumentException error)
                {
                    throw new CalculationRequestException($"fix_member.{caseId}, member {byMember.Key}: {error.Message}");
                }
            }
            var explicitMembers = typed.Where(row => row.length != null).Select(row => row.m).ToHashSet();
            var projected = new JsonArray();
            foreach (var row in typed.OrderBy(row => row.row))
            {
                if (row.length == null && !explicitMembers.Contains(row.m) &&
                    row.tx == 0 && row.ty == 0 && row.tz == 0 && row.tr == 0)
                    continue;
                var item = new JsonObject { ["row"] = row.row, ["m"] = row.m,
                    ["tx"] = row.tx ?? 0, ["ty"] = row.ty ?? 0,
                    ["tz"] = row.tz ?? 0, ["tr"] = row.tr ?? 0 };
                if (row.length is float metres) item["length"] = metres;
                projected.Add(item);
            }
            if (projected.Count == 0) groups.Remove(caseId);
            else groups[caseId] = projected;
        }
        if (groups.Count == 0) result.Remove("fix_member");
    }

    private static void NormalizeJointRows(JsonObject result, JsonObject members)
    {
        if (result["joint"] is not JsonObject groups) return;
        foreach (string groupId in groups.Select(item => item.Key).ToArray())
        {
            if (groups[groupId] is not JsonArray source)
                throw new CalculationRequestException($"joint.{groupId} must be an array.");
            var rows = new JsonArray();
            foreach (var value in source)
            {
                if (value is not JsonObject row) continue;
                string memberId = Id(row["m"]);
                if (!members.ContainsKey(memberId))
                    throw new CalculationRequestException($"joint.{groupId}: member {memberId} does not exist.");
                var item = new JsonObject { ["row"] = (int?)row["row"] ?? 0, ["m"] = memberId };
                foreach (string key in new[] { "xi", "yi", "zi", "xj", "yj", "zj" })
                    item[key] = Number(row[key]) ?? 0;
                rows.Add(item);
            }
            if (rows.Count == 0) groups.Remove(groupId);
            else groups[groupId] = rows;
        }
        if (groups.Count == 0) result.Remove("joint");
    }

    private static void ValidatePlanarity(string id, JsonArray corners, JsonObject nodes)
    {
        double[][] points = corners.Select(corner =>
        {
            var row = (JsonObject)nodes[Id(corner)]!;
            return new[] { Number(row["x"]) ?? 0, Number(row["y"]) ?? 0, Number(row["z"]) ?? 0 };
        }).ToArray();
        double[] a = Sub(points[1], points[0]), b = Sub(points[2], points[0]), c = Sub(points[3], points[0]);
        double determinant = a[0] * (b[1] * c[2] - b[2] * c[1]) -
            a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0]);
        if (Math.Abs(determinant) > 1e-7) throw new CalculationRequestException($"shell.{id} is not planar.");
    }

    private static double[] Sub(double[] a, double[] b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]];
    private static double JsRound(double value) => Math.Floor(value + 0.5);
    private static JsonObject Object(JsonObject parent, string key) => parent[key] as JsonObject ??
        throw new CalculationRequestException($"{key} must be an object.");
    private static string Id(JsonNode? node) => node == null ? "" : node.ToString();
    private static string String(JsonNode? node) => node == null ? "" : node.ToString();
    private static double? Number(JsonNode? node) => double.TryParse(node?.ToString(), NumberStyles.Float,
        CultureInfo.InvariantCulture, out double value) && double.IsFinite(value) ? value : null;
    private static void DefaultNumber(JsonObject row, string key) { if (Number(row[key]) == null) row[key] = 0; }
}

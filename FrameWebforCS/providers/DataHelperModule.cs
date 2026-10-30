using FarPoint.Win.Spread;
using FrameWebforCS.components.input;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FrameWebforCS.providers
{
    internal static class DataHelperModule
    {

        public static Dictionary<string, T>? JsonToDict<T>(JsonElement jsonData, string key) where T : class, new()
        {
            if (!jsonData.TryGetProperty(key, out JsonElement targetJson) ||
                targetJson.ValueKind != JsonValueKind.Object)
            {
                // throw new JsonException("key がJSONオブジェクトとして定義されていません。");
                return null;
            }

            return JsonToDict<T>(targetJson);
        }

        public static Dictionary<string, T>? JsonToDict<T>(JsonElement jsonData) where T : class, new()
        {
            return JsonToDict(jsonData, JsonToClass<T>);
        }

        public static Dictionary<string, T>? JsonToDict<T>(
            JsonElement jsonData,
            string key,
            Func<JsonElement, T?> valueConverter) where T : class
        {
            if (!jsonData.TryGetProperty(key, out JsonElement targetJson) ||
                targetJson.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return JsonToDict(targetJson, valueConverter);
        }

        private static Dictionary<string, T>? JsonToDict<T>(
            JsonElement jsonData,
            Func<JsonElement, T?> valueConverter) where T : class
        {
            if (jsonData.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var targets = new Dictionary<string, T>();
            foreach (JsonProperty property in jsonData.EnumerateObject())
            {
                T? tmp = valueConverter(property.Value);

                if (tmp == null)
                {
                    continue;
                }
                if (!targets.TryAdd(property.Name, tmp))
                {
                    continue;
                }
            }
            return targets;
        }

        /// <summary>
        /// JsonElement → クラスへ自動変換
        /// JSONのキー名とフィールド名が一致しているものを設定する
        /// </summary>
        public static T? JsonToClass<T>(JsonElement json) where T : class, new()
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new T();

            foreach (FieldInfo field in typeof(T).GetFields())
            {
                if (!json.TryGetProperty(field.Name, out JsonElement value))
                    continue;

                object? convertedValue = ConvertJsonValue(value, field.FieldType);

                if (convertedValue != null)
                {
                    field.SetValue(result, convertedValue);
                }
            }

            // 全フィールドが初期値のままなら無効
            var defaultValue = new T();

            bool allDefault = typeof(T)
                .GetFields()
                .All(field =>
                    Equals(
                        field.GetValue(result),
                        field.GetValue(defaultValue)
                    )
                );

            if (allDefault)
                return null;

            return result;
        }

        public static List<T>? JsonToList<T>(JsonElement json) where T : class, new()
        {
            if (json.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var result = new List<T>();
            foreach (JsonElement itemJson in json.EnumerateArray())
            {
                T? item = JsonToClass<T>(itemJson);
                if (item != null)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// JsonElement を指定された型へ変換
        /// </summary>
        private static object? ConvertJsonValue(JsonElement value, Type targetType)
        {
            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            // Nullable<T> の場合は中身の型を取得
            Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (type == typeof(string))
                return value.ToString();

            if (type == typeof(float))
                return value.TryGetSingle(out float f) ? f : null;

            if (type == typeof(double))
                return value.TryGetDouble(out double d) ? d : null;

            if (type == typeof(int))
                return value.TryGetInt32(out int i) ? i : null;

            if (type == typeof(long))
                return value.TryGetInt64(out long l) ? l : null;

            if (type == typeof(bool))
            {
                if (value.ValueKind == JsonValueKind.True)
                    return true;

                if (value.ValueKind == JsonValueKind.False)
                    return false;

                return null;
            }

            if (type == typeof(JsonObject))
                return value.ValueKind == JsonValueKind.Object ? JsonNode.Parse(value.GetRawText())!.AsObject() : null;

            if (type == typeof(List<float>))
            {
                if (value.ValueKind != JsonValueKind.Array)
                    return null;

                var result = new List<float>();
                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (!item.TryGetSingle(out float point))
                        return null;

                    result.Add(point);
                }

                return result;
            }

            return null;
        }

        public static Dictionary<string, object?> ClassToDictionary<T>(T obj)
        {
            var result = new Dictionary<string, object?>();

            foreach (var field in typeof(T).GetFields())
            {
                result[field.Name] = field.GetValue(obj);
            }

            return result;
        }

    }
}

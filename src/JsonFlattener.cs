using System.Text;
using System.Text.Json;

namespace Philiprehberger.FlattenJson;

/// <summary>
/// Provides methods to flatten nested JSON objects into dot-separated key-value pairs
/// and to unflatten them back into nested JSON.
/// </summary>
public static class JsonFlattener
{
    /// <summary>
    /// Flattens a nested JSON string into a dictionary of dot-separated keys and their string values.
    /// </summary>
    /// <param name="json">The JSON string to flatten.</param>
    /// <param name="separator">The separator to use between nested keys. Defaults to <c>"."</c>.</param>
    /// <returns>A dictionary mapping flattened keys to their string values, with <c>null</c> for JSON null values.</returns>
    public static Dictionary<string, string?> Flatten(string json, string separator = ".")
    {
        var result = new Dictionary<string, string?>();
        using var document = JsonDocument.Parse(json);
        FlattenElement(document.RootElement, string.Empty, separator, result);
        return result;
    }

    private static void FlattenElement(JsonElement element, string prefix, string separator, Dictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : prefix + separator + property.Name;
                    FlattenElement(property.Value, key, separator, result);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? index.ToString()
                        : prefix + separator + index;
                    FlattenElement(item, key, separator, result);
                    index++;
                }
                break;

            case JsonValueKind.Null:
                result[prefix] = null;
                break;

            default:
                result[prefix] = element.ToString();
                break;
        }
    }

    /// <summary>
    /// Reconstructs a nested JSON string from a dictionary of flattened key-value pairs.
    /// </summary>
    /// <param name="flat">The dictionary of flattened keys and their string values.</param>
    /// <param name="separator">The separator used between nested keys. Defaults to <c>"."</c>.</param>
    /// <returns>A JSON string representing the reconstructed nested structure.</returns>
    public static string Unflatten(Dictionary<string, string?> flat, string separator = ".")
    {
        var root = new Dictionary<string, object?>();

        foreach (var (key, value) in flat)
        {
            var parts = key.Split(separator);
            var current = root;

            for (var i = 0; i < parts.Length - 1; i++)
            {
                if (!current.TryGetValue(parts[i], out var next) || next is not Dictionary<string, object?>)
                {
                    next = new Dictionary<string, object?>();
                    current[parts[i]] = next;
                }
                current = (Dictionary<string, object?>)next!;
            }

            current[parts[^1]] = value;
        }

        return SerializeNode(root);
    }

    private static string SerializeNode(object? node)
    {
        if (node is null)
            return "null";

        if (node is string s)
        {
            // Try to preserve numeric/bool/null values
            return $"\"{EscapeJsonString(s)}\"";
        }

        if (node is Dictionary<string, object?> dict)
        {
            // Check if all keys are numeric — if so, serialize as array
            var keys = dict.Keys.ToList();
            var isArray = keys.Count > 0 && keys.All(k => int.TryParse(k, out _));

            if (isArray)
            {
                var ordered = keys
                    .Select(k => (index: int.Parse(k), value: dict[k]))
                    .OrderBy(x => x.index)
                    .Select(x => SerializeNode(x.value));
                return "[" + string.Join(",", ordered) + "]";
            }

            var props = dict.Select(kvp => $"\"{EscapeJsonString(kvp.Key)}\":{SerializeNode(kvp.Value)}");
            return "{" + string.Join(",", props) + "}";
        }

        return $"\"{node}\"";
    }

    private static string EscapeJsonString(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}

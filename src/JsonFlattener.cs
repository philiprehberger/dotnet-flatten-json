using System.Text;
using System.Text.Json;

namespace Philiprehberger.FlattenJson;

/// <summary>
/// Provides methods to flatten nested JSON objects into dot-separated key-value pairs
/// and to unflatten them back into nested JSON.
/// </summary>
public static class JsonFlattener
{
    private const string EmptyObject = "{}";
    private const string EmptyArray = "[]";

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
        FlattenElement(document.RootElement, string.Empty, 0, separator, ArrayIndexFormat.Dot, null, false, result);
        return result;
    }

    /// <summary>
    /// Flattens a nested JSON string into a dictionary using the specified options.
    /// </summary>
    /// <param name="json">The JSON string to flatten.</param>
    /// <param name="options">The options that control the separator, depth limit, array index format, and empty container handling.</param>
    /// <returns>A dictionary mapping flattened keys to their string values, with <c>null</c> for JSON null values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> or <paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="JsonException">Thrown when <paramref name="json"/> is not valid JSON.</exception>
    public static Dictionary<string, string?> Flatten(string json, FlattenOptions options)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(options);

        using var document = JsonDocument.Parse(json);
        return Flatten(document.RootElement, options);
    }

    /// <summary>
    /// Flattens an already parsed JSON element into a dictionary of key-value pairs.
    /// </summary>
    /// <param name="element">The JSON element to flatten. The returned dictionary does not reference the element after the call.</param>
    /// <param name="options">The options to apply, or <c>null</c> to use the defaults.</param>
    /// <returns>A dictionary mapping flattened keys to their string values, with <c>null</c> for JSON null values.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="element"/> is an undefined (default) element.</exception>
    public static Dictionary<string, string?> Flatten(JsonElement element, FlattenOptions? options = null)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("The JSON element is undefined.", nameof(element));

        options ??= new FlattenOptions();
        var result = new Dictionary<string, string?>();
        FlattenElement(
            element,
            string.Empty,
            0,
            options.Separator,
            options.ArrayIndexFormat,
            options.MaxDepth,
            options.IncludeEmptyContainers,
            result);
        return result;
    }

    private static void FlattenElement(
        JsonElement element,
        string prefix,
        int depth,
        string separator,
        ArrayIndexFormat indexFormat,
        int? maxDepth,
        bool includeEmptyContainers,
        Dictionary<string, string?> result)
    {
        var isContainer = element.ValueKind is JsonValueKind.Object or JsonValueKind.Array;

        if (isContainer && depth > 0)
        {
            if (maxDepth.HasValue && depth >= maxDepth.Value)
            {
                result[prefix] = element.GetRawText();
                return;
            }

            if (includeEmptyContainers && IsEmptyContainer(element))
            {
                result[prefix] = element.ValueKind == JsonValueKind.Object ? EmptyObject : EmptyArray;
                return;
            }
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : prefix + separator + property.Name;
                    FlattenElement(property.Value, key, depth + 1, separator, indexFormat, maxDepth, includeEmptyContainers, result);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    string key;
                    if (indexFormat == ArrayIndexFormat.Brackets)
                        key = prefix + "[" + index + "]";
                    else
                        key = string.IsNullOrEmpty(prefix)
                            ? index.ToString()
                            : prefix + separator + index;
                    FlattenElement(item, key, depth + 1, separator, indexFormat, maxDepth, includeEmptyContainers, result);
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

    private static bool IsEmptyContainer(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            using var properties = element.EnumerateObject();
            return !properties.MoveNext();
        }

        return element.GetArrayLength() == 0;
    }

    /// <summary>
    /// Reconstructs a nested JSON string from a dictionary of flattened key-value pairs.
    /// </summary>
    /// <param name="flat">The dictionary of flattened keys and their string values.</param>
    /// <param name="separator">The separator used between nested keys. Defaults to <c>"."</c>.</param>
    /// <returns>A JSON string representing the reconstructed nested structure.</returns>
    public static string Unflatten(Dictionary<string, string?> flat, string separator = ".")
    {
        return UnflattenCore(flat, separator, ArrayIndexFormat.Dot, null, false);
    }

    /// <summary>
    /// Reconstructs a nested JSON string from a dictionary of flattened key-value pairs using the specified options.
    /// </summary>
    /// <param name="flat">The dictionary of flattened keys and their string values.</param>
    /// <param name="options">
    /// The options used when the dictionary was flattened. With <see cref="ArrayIndexFormat.Brackets"/>, keys such as
    /// <c>a[0].b</c> are parsed; with <see cref="FlattenOptions.IncludeEmptyContainers"/>, <c>"{}"</c> and <c>"[]"</c>
    /// values become empty containers; with <see cref="FlattenOptions.MaxDepth"/>, values at that depth that hold a JSON
    /// object or array are embedded as JSON rather than as strings.
    /// </param>
    /// <returns>A JSON string representing the reconstructed nested structure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="flat"/> or <paramref name="options"/> is <c>null</c>.</exception>
    public static string Unflatten(Dictionary<string, string?> flat, FlattenOptions options)
    {
        ArgumentNullException.ThrowIfNull(flat);
        ArgumentNullException.ThrowIfNull(options);

        return UnflattenCore(flat, options.Separator, options.ArrayIndexFormat, options.MaxDepth, options.IncludeEmptyContainers);
    }

    private static string UnflattenCore(
        Dictionary<string, string?> flat,
        string separator,
        ArrayIndexFormat indexFormat,
        int? maxDepth,
        bool includeEmptyContainers)
    {
        var root = new Dictionary<string, object?>();

        foreach (var (key, value) in flat)
        {
            var parts = indexFormat == ArrayIndexFormat.Brackets
                ? SplitBracketKey(key, separator)
                : key.Split(separator);
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

            current[parts[^1]] = ConvertLeaf(value, parts.Length, maxDepth, includeEmptyContainers);
        }

        return SerializeNode(root);
    }

    private static object? ConvertLeaf(string? value, int depth, int? maxDepth, bool includeEmptyContainers)
    {
        if (value is null)
            return null;

        if (includeEmptyContainers)
        {
            if (value == EmptyObject)
                return new Dictionary<string, object?>();
            if (value == EmptyArray)
                return new RawJson(EmptyArray);
        }

        if (maxDepth.HasValue && depth == maxDepth.Value && LooksLikeContainer(value))
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                return new RawJson(document.RootElement.GetRawText());
            }
            catch (JsonException)
            {
                // Not valid JSON: keep it as a plain string value.
            }
        }

        return value;
    }

    private static bool LooksLikeContainer(string value)
    {
        var trimmed = value.AsSpan().Trim();
        return trimmed.Length >= 2
            && ((trimmed[0] == '{' && trimmed[^1] == '}') || (trimmed[0] == '[' && trimmed[^1] == ']'));
    }

    private static string[] SplitBracketKey(string key, string separator)
    {
        var segments = new List<string>();

        foreach (var part in key.Split(separator))
        {
            var open = part.IndexOf('[');
            if (open < 0 || !TryParseIndices(part.AsSpan(open), out var indices))
            {
                segments.Add(part);
                continue;
            }

            if (open > 0)
                segments.Add(part[..open]);
            segments.AddRange(indices);
        }

        return segments.ToArray();
    }

    private static bool TryParseIndices(ReadOnlySpan<char> text, out List<string> indices)
    {
        indices = new List<string>();

        while (!text.IsEmpty)
        {
            if (text[0] != '[')
                return false;

            var close = text.IndexOf(']');
            if (close < 2)
                return false;

            var digits = text[1..close];
            foreach (var c in digits)
            {
                if (c is < '0' or > '9')
                    return false;
            }

            indices.Add(digits.ToString());
            text = text[(close + 1)..];
        }

        return indices.Count > 0;
    }

    private static string SerializeNode(object? node)
    {
        if (node is null)
            return "null";

        if (node is RawJson raw)
            return raw.Text;

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

    private sealed record RawJson(string Text);
}

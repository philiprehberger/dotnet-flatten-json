using Xunit;
using System.Text.Json;

namespace Philiprehberger.FlattenJson.Tests;

public class JsonFlattenerTests
{
    [Fact]
    public void Flatten_SimpleObject_ReturnsFlatDictionary()
    {
        var json = """{"name":"Alice","age":"30"}""";

        var result = JsonFlattener.Flatten(json);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result["name"]);
        Assert.Equal("30", result["age"]);
    }

    [Fact]
    public void Flatten_NestedObject_UsesDotSeparator()
    {
        var json = """{"user":{"name":"Alice","address":{"city":"Vienna"}}}""";

        var result = JsonFlattener.Flatten(json);

        Assert.Equal("Alice", result["user.name"]);
        Assert.Equal("Vienna", result["user.address.city"]);
    }

    [Fact]
    public void Flatten_ArrayElements_UsesNumericIndices()
    {
        var json = """{"items":["a","b","c"]}""";

        var result = JsonFlattener.Flatten(json);

        Assert.Equal("a", result["items.0"]);
        Assert.Equal("b", result["items.1"]);
        Assert.Equal("c", result["items.2"]);
    }

    [Fact]
    public void Flatten_NullValue_StoresNull()
    {
        var json = """{"key":null}""";

        var result = JsonFlattener.Flatten(json);

        Assert.Null(result["key"]);
    }

    [Theory]
    [InlineData("/", "user/name")]
    [InlineData("_", "user_name")]
    [InlineData("::", "user::name")]
    public void Flatten_CustomSeparator_UsesSpecifiedSeparator(string separator, string expectedKey)
    {
        var json = """{"user":{"name":"Alice"}}""";

        var result = JsonFlattener.Flatten(json, separator);

        Assert.Equal("Alice", result[expectedKey]);
    }

    [Fact]
    public void Flatten_EmptyObject_ReturnsEmptyDictionary()
    {
        var json = "{}";

        var result = JsonFlattener.Flatten(json);

        Assert.Empty(result);
    }

    [Fact]
    public void Unflatten_FlatDictionary_ReconstructsNestedJson()
    {
        var flat = new Dictionary<string, string?>
        {
            ["user.name"] = "Alice",
            ["user.age"] = "30"
        };

        var json = JsonFlattener.Unflatten(flat);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("Alice", doc.RootElement.GetProperty("user").GetProperty("name").GetString());
        Assert.Equal("30", doc.RootElement.GetProperty("user").GetProperty("age").GetString());
    }

    [Fact]
    public void Unflatten_NumericKeys_ReconstructsArray()
    {
        var flat = new Dictionary<string, string?>
        {
            ["items.0"] = "a",
            ["items.1"] = "b"
        };

        var json = JsonFlattener.Unflatten(flat);
        using var doc = JsonDocument.Parse(json);

        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal("a", items[0].GetString());
        Assert.Equal("b", items[1].GetString());
    }

    [Fact]
    public void Unflatten_NullValue_ReconstructsNull()
    {
        var flat = new Dictionary<string, string?>
        {
            ["key"] = null
        };

        var json = JsonFlattener.Unflatten(flat);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("key").ValueKind);
    }

    [Fact]
    public void Flatten_ThenUnflatten_Roundtrips()
    {
        var original = """{"user":{"name":"Alice","tags":["a","b"]}}""";

        var flat = JsonFlattener.Flatten(original);
        var reconstructed = JsonFlattener.Unflatten(flat);
        using var doc = JsonDocument.Parse(reconstructed);

        Assert.Equal("Alice", doc.RootElement.GetProperty("user").GetProperty("name").GetString());
    }

    [Fact]
    public void Flatten_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => JsonFlattener.Flatten("not json"));
    }
}

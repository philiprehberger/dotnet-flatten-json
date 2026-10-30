using Xunit;
using System.Text.Json;

namespace Philiprehberger.FlattenJson.Tests;

public class FlattenOptionsTests
{
    private const string Nested = """{"user":{"name":"Alice","tags":["a","b"],"address":{"city":"Vienna","geo":{"lat":"48.2"}}}}""";

    private static string Normalize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement);
    }

    [Fact]
    public void Defaults_MatchDocumentedValues()
    {
        var options = new FlattenOptions();

        Assert.Equal(".", options.Separator);
        Assert.Null(options.MaxDepth);
        Assert.Equal(ArrayIndexFormat.Dot, options.ArrayIndexFormat);
        Assert.False(options.IncludeEmptyContainers);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Separator_EmptyOrNull_ThrowsArgumentException(string? separator)
    {
        Assert.ThrowsAny<ArgumentException>(() => new FlattenOptions { Separator = separator! });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxDepth_LessThanOne_ThrowsArgumentOutOfRangeException(int maxDepth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlattenOptions { MaxDepth = maxDepth });
    }

    [Fact]
    public void Flatten_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JsonFlattener.Flatten("{}", (FlattenOptions)null!));
    }

    [Fact]
    public void Unflatten_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => JsonFlattener.Unflatten(new Dictionary<string, string?>(), (FlattenOptions)null!));
    }

    [Fact]
    public void Flatten_DefaultOptions_MatchesLegacyOverload()
    {
        var legacy = JsonFlattener.Flatten(Nested);
        var withOptions = JsonFlattener.Flatten(Nested, new FlattenOptions());

        Assert.Equal(legacy, withOptions);
    }

    [Fact]
    public void Unflatten_DefaultOptions_MatchesLegacyOverload()
    {
        var flat = JsonFlattener.Flatten(Nested);

        Assert.Equal(JsonFlattener.Unflatten(flat), JsonFlattener.Unflatten(flat, new FlattenOptions()));
    }

    [Fact]
    public void Flatten_CustomSeparatorOption_UsesSeparator()
    {
        var result = JsonFlattener.Flatten(Nested, new FlattenOptions { Separator = "/" });

        Assert.Equal("Alice", result["user/name"]);
        Assert.Equal("b", result["user/tags/1"]);
    }

    [Fact]
    public void Separator_RoundTrips()
    {
        var options = new FlattenOptions { Separator = "::" };

        var flat = JsonFlattener.Flatten(Nested, options);
        var json = JsonFlattener.Unflatten(flat, options);

        Assert.Equal(Normalize(Nested), Normalize(json));
    }

    [Fact]
    public void Flatten_MaxDepth_StoresSubtreeAsRawJson()
    {
        var result = JsonFlattener.Flatten(Nested, new FlattenOptions { MaxDepth = 2 });

        Assert.Equal("Alice", result["user.name"]);
        Assert.Equal("""["a","b"]""", result["user.tags"]);
        Assert.Equal("""{"city":"Vienna","geo":{"lat":"48.2"}}""", result["user.address"]);
        Assert.False(result.ContainsKey("user.address.city"));
    }

    [Fact]
    public void Flatten_MaxDepthOne_KeepsOnlyTopLevelKeys()
    {
        var result = JsonFlattener.Flatten("""{"a":{"b":1},"c":2}""", new FlattenOptions { MaxDepth = 1 });

        Assert.Equal(2, result.Count);
        Assert.Equal("""{"b":1}""", result["a"]);
        Assert.Equal("2", result["c"]);
    }

    [Fact]
    public void MaxDepth_RoundTrips()
    {
        var options = new FlattenOptions { MaxDepth = 2 };

        var flat = JsonFlattener.Flatten(Nested, options);
        var json = JsonFlattener.Unflatten(flat, options);

        Assert.Equal(Normalize(Nested), Normalize(json));
    }

    [Fact]
    public void Unflatten_MaxDepth_InvalidJsonLookalike_StaysString()
    {
        var flat = new Dictionary<string, string?> { ["a"] = "{not json}" };

        var json = JsonFlattener.Unflatten(flat, new FlattenOptions { MaxDepth = 1 });
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("{not json}", doc.RootElement.GetProperty("a").GetString());
    }

    [Fact]
    public void Flatten_Brackets_UsesBracketIndices()
    {
        var json = """{"users":[{"name":"Alice","roles":["admin","dev"]}]}""";

        var result = JsonFlattener.Flatten(json, new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets });

        Assert.Equal("Alice", result["users[0].name"]);
        Assert.Equal("dev", result["users[0].roles[1]"]);
    }

    [Fact]
    public void Flatten_BracketsNestedArrays_ChainsBrackets()
    {
        var result = JsonFlattener.Flatten("""{"m":[[1,2],[3]]}""", new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets });

        Assert.Equal("2", result["m[0][1]"]);
        Assert.Equal("3", result["m[1][0]"]);
    }

    [Fact]
    public void Flatten_BracketsRootArray_UsesBracketKeys()
    {
        var result = JsonFlattener.Flatten("""["x","y"]""", new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets });

        Assert.Equal("x", result["[0]"]);
        Assert.Equal("y", result["[1]"]);
    }

    [Fact]
    public void Unflatten_Brackets_ParsesBracketKeys()
    {
        var flat = new Dictionary<string, string?>
        {
            ["a[0].b"] = "1",
            ["a[1].b"] = "2"
        };

        var json = JsonFlattener.Unflatten(flat, new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets });

        Assert.Equal("""{"a":[{"b":"1"},{"b":"2"}]}""", json);
    }

    [Fact]
    public void Unflatten_BracketsMalformed_KeepsSegmentLiteral()
    {
        var flat = new Dictionary<string, string?> { ["a[x]"] = "1" };

        var json = JsonFlattener.Unflatten(flat, new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets });

        Assert.Equal("""{"a[x]":"1"}""", json);
    }

    [Fact]
    public void Brackets_RoundTrips()
    {
        var original = """{"users":[{"name":"Alice","roles":["admin","dev"]}],"m":[["1","2"]]}""";
        var options = new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets };

        var flat = JsonFlattener.Flatten(original, options);
        var json = JsonFlattener.Unflatten(flat, options);

        Assert.Equal(Normalize(original), Normalize(json));
    }

    [Fact]
    public void Flatten_IncludeEmptyContainers_EmitsPlaceholders()
    {
        var result = JsonFlattener.Flatten("""{"a":{},"b":[],"c":{"d":[]}}""", new FlattenOptions { IncludeEmptyContainers = true });

        Assert.Equal("{}", result["a"]);
        Assert.Equal("[]", result["b"]);
        Assert.Equal("[]", result["c.d"]);
    }

    [Fact]
    public void Flatten_EmptyContainersByDefault_AreOmitted()
    {
        var result = JsonFlattener.Flatten("""{"a":{},"b":[],"c":"1"}""", new FlattenOptions());

        Assert.Single(result);
        Assert.Equal("1", result["c"]);
    }

    [Fact]
    public void IncludeEmptyContainers_RoundTrips()
    {
        var original = """{"a":{},"b":[],"c":{"d":[],"e":"1"}}""";
        var options = new FlattenOptions { IncludeEmptyContainers = true };

        var flat = JsonFlattener.Flatten(original, options);
        var json = JsonFlattener.Unflatten(flat, options);

        Assert.Equal(Normalize(original), Normalize(json));
    }

    [Fact]
    public void Unflatten_WithoutIncludeEmptyContainers_TreatsPlaceholdersAsStrings()
    {
        var flat = new Dictionary<string, string?> { ["a"] = "{}" };

        var json = JsonFlattener.Unflatten(flat, new FlattenOptions());

        Assert.Equal("""{"a":"{}"}""", json);
    }

    [Fact]
    public void AllOptions_RoundTrip()
    {
        var original = """{"a":{"list":[{"x":"1"},{}],"deep":{"k":{"v":"2"}}},"e":[]}""";
        var options = new FlattenOptions
        {
            Separator = "/",
            MaxDepth = 3,
            ArrayIndexFormat = ArrayIndexFormat.Brackets,
            IncludeEmptyContainers = true
        };

        var flat = JsonFlattener.Flatten(original, options);
        var json = JsonFlattener.Unflatten(flat, options);

        Assert.Equal("""{"v":"2"}""", flat["a/deep/k"]);
        Assert.Equal("{}", flat["a/list[1]"]);
        Assert.Equal(Normalize(original), Normalize(json));
    }

    [Fact]
    public void Flatten_JsonElement_MatchesStringOverload()
    {
        using var doc = JsonDocument.Parse(Nested);

        var result = JsonFlattener.Flatten(doc.RootElement);

        Assert.Equal(JsonFlattener.Flatten(Nested), result);
    }

    [Fact]
    public void Flatten_JsonElementWithOptions_AppliesOptions()
    {
        using var doc = JsonDocument.Parse(Nested);

        var result = JsonFlattener.Flatten(doc.RootElement.GetProperty("user"), new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets });

        Assert.Equal("a", result["tags[0]"]);
        Assert.Equal("48.2", result["address.geo.lat"]);
    }

    [Fact]
    public void Flatten_JsonElement_ResultOutlivesDocument()
    {
        Dictionary<string, string?> result;
        using (var doc = JsonDocument.Parse("""{"a":{"b":"1"}}"""))
        {
            result = JsonFlattener.Flatten(doc.RootElement);
        }

        Assert.Equal("1", result["a.b"]);
    }

    [Fact]
    public void Flatten_UndefinedJsonElement_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => JsonFlattener.Flatten(default(JsonElement)));
    }
}

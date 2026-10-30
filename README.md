# Philiprehberger.FlattenJson

[![CI](https://github.com/philiprehberger/dotnet-flatten-json/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-flatten-json/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.FlattenJson.svg)](https://www.nuget.org/packages/Philiprehberger.FlattenJson)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-flatten-json)](https://github.com/philiprehberger/dotnet-flatten-json/commits/main)

![Philiprehberger.FlattenJson](https://raw.githubusercontent.com/philiprehberger/dotnet-flatten-json/main/package-card.webp)

Flatten nested JSON objects into dot-notation key-value pairs, and unflatten them back.

## Installation

```bash
dotnet add package Philiprehberger.FlattenJson
```

## Usage

```csharp
using Philiprehberger.FlattenJson;

// Flatten nested JSON
var flat = JsonFlattener.Flatten("""{"a":{"b":1,"c":[2,3]}}""");
// flat["a.b"] == "1"
// flat["a.c.0"] == "2"
// flat["a.c.1"] == "3"

// Null values are preserved
var flat2 = JsonFlattener.Flatten("""{"x":null,"y":{"z":42}}""");
// flat2["x"] == null
// flat2["y.z"] == "42"

// Custom separator
var flat3 = JsonFlattener.Flatten("""{"a":{"b":1}}""", separator: "/");
// flat3["a/b"] == "1"

// Unflatten back to JSON
var json = JsonFlattener.Unflatten(flat);
// => {"a":{"b":"1","c":["2","3"]}}
```

### Arrays

```csharp
using Philiprehberger.FlattenJson;

var json = """{"users": [{"name": "Alice"}, {"name": "Bob"}]}""";
var flat = JsonFlattener.Flatten(json);
// { "users.0.name": "Alice", "users.1.name": "Bob" }
```

### Custom Separator

```csharp
using Philiprehberger.FlattenJson;

var json = """{"db": {"host": "localhost", "port": "5432"}}""";
var flat = JsonFlattener.Flatten(json, "/");
// { "db/host": "localhost", "db/port": "5432" }
```

### Unflatten

```csharp
using Philiprehberger.FlattenJson;

var flat = new Dictionary<string, string?>
{
    ["db.host"] = "localhost",
    ["db.port"] = "5432"
};
var json = JsonFlattener.Unflatten(flat);
// {"db":{"host":"localhost","port":"5432"}}
```

All leaf values are reconstructed as JSON strings, and a dictionary level whose keys are all numeric becomes an array.

### Options

```csharp
using Philiprehberger.FlattenJson;

var options = new FlattenOptions
{
    Separator = "/",
    MaxDepth = 2,
    IncludeEmptyContainers = true
};

var json = """{"a":{"b":{"c":1}},"tags":[],"meta":{}}""";
var flat = JsonFlattener.Flatten(json, options);
// { "a/b": "{\"c\":1}", "tags": "[]", "meta": "{}" }

var restored = JsonFlattener.Unflatten(flat, options);
// {"a":{"b":{"c":1}},"tags":[],"meta":{}}
```

### Bracket Notation

```csharp
using Philiprehberger.FlattenJson;

var options = new FlattenOptions { ArrayIndexFormat = ArrayIndexFormat.Brackets };

var json = """{"users": [{"name": "Alice", "roles": ["admin"]}]}""";
var flat = JsonFlattener.Flatten(json, options);
// { "users[0].name": "Alice", "users[0].roles[0]": "admin" }

var restored = JsonFlattener.Unflatten(flat, options);
// {"users":[{"name":"Alice","roles":["admin"]}]}
```

### JsonElement

```csharp
using System.Text.Json;
using Philiprehberger.FlattenJson;

using var document = JsonDocument.Parse("""{"db": {"host": "localhost"}}""");
var flat = JsonFlattener.Flatten(document.RootElement.GetProperty("db"));
// { "host": "localhost" }
```

## API

### `JsonFlattener`

| Method | Description |
|--------|-------------|
| `Flatten(string json, string separator = ".")` | Parse `json` and return a flat `Dictionary<string, string?>`; leaf values are strings (`null` for JSON nulls) and array elements are keyed by zero-based index (`"a.0"`) |
| `Flatten(string json, FlattenOptions options)` | Parse `json` and flatten it using `options` |
| `Flatten(JsonElement element, FlattenOptions? options = null)` | Flatten an already parsed element; `null` options use the defaults |
| `Unflatten(Dictionary<string, string?> flat, string separator = ".")` | Reconstruct a JSON string from a flat dictionary; values become JSON strings and all-numeric key levels become arrays |
| `Unflatten(Dictionary<string, string?> flat, FlattenOptions options)` | Reconstruct a JSON string using the options the dictionary was flattened with |

### `FlattenOptions`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Separator` | `string` | `"."` | Separator between key segments; must be non-empty |
| `MaxDepth` | `int?` | `null` | Number of key segments to expand; deeper objects and arrays are stored as raw JSON text and restored by `Unflatten`; must be at least 1 |
| `ArrayIndexFormat` | `ArrayIndexFormat` | `Dot` | `Dot` writes `a.0.b`; `Brackets` writes and parses `a[0].b` |
| `IncludeEmptyContainers` | `bool` | `false` | Emit empty objects and arrays as `"{}"` / `"[]"` values and restore them as empty containers |

Uses `System.Text.Json`, with no external dependencies.

## Development

```bash
dotnet build src/Philiprehberger.FlattenJson.csproj --configuration Release
dotnet test tests/Philiprehberger.FlattenJson.Tests/Philiprehberger.FlattenJson.Tests.csproj --configuration Release
```

## Support

If you find this project useful:

⭐ [Star the repo](https://github.com/philiprehberger/dotnet-flatten-json)

🐛 [Report issues](https://github.com/philiprehberger/dotnet-flatten-json/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

💡 [Suggest features](https://github.com/philiprehberger/dotnet-flatten-json/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)

❤️ [Sponsor development](https://github.com/sponsors/philiprehberger)

🌐 [All Open Source Projects](https://philiprehberger.com/open-source-packages)

💻 [GitHub Profile](https://github.com/philiprehberger)

🔗 [LinkedIn Profile](https://www.linkedin.com/in/philiprehberger)

## License

[MIT](LICENSE)

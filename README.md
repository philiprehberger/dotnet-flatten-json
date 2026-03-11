# Philiprehberger.FlattenJson

Flatten nested JSON objects into dot-notation key-value pairs, and unflatten them back.

## Install

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

## API

### `JsonFlattener`

| Method | Description |
|--------|-------------|
| `Flatten(string json, string separator = ".")` | Parse `json` and return a flat `Dictionary<string, string?>` using dot-notation keys |
| `Unflatten(Dictionary<string, string?> flat, string separator = ".")` | Reconstruct a JSON string from a flat dictionary |

**Notes:**
- All leaf values are stored as strings (or `null` for JSON nulls).
- Array elements are keyed by their zero-based index: `"a.0"`, `"a.1"`, etc.
- Uses `System.Text.Json` — no external dependencies.

## License

MIT

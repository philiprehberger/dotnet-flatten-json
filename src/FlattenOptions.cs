namespace Philiprehberger.FlattenJson;

/// <summary>
/// Specifies how array indices are written into flattened keys.
/// </summary>
public enum ArrayIndexFormat
{
    /// <summary>
    /// Array indices are joined with the separator like any other key segment, e.g. <c>a.0.b</c>.
    /// </summary>
    Dot,

    /// <summary>
    /// Array indices are written in square brackets, e.g. <c>a[0].b</c>.
    /// </summary>
    Brackets
}

/// <summary>
/// Options that control how <see cref="JsonFlattener"/> flattens and unflattens JSON.
/// </summary>
public sealed class FlattenOptions
{
    private readonly string _separator = ".";
    private readonly int? _maxDepth;

    /// <summary>
    /// Gets the separator placed between nested key segments. Defaults to <c>"."</c>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when set to <c>null</c> or an empty string.</exception>
    public string Separator
    {
        get => _separator;
        init
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Separator must be a non-empty string.", nameof(Separator));
            _separator = value;
        }
    }

    /// <summary>
    /// Gets the maximum number of key segments to expand. Objects and arrays found at this depth are
    /// stored as their raw JSON text instead of being flattened further. Defaults to <c>null</c> (no limit).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value less than 1.</exception>
    public int? MaxDepth
    {
        get => _maxDepth;
        init
        {
            if (value is < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxDepth), value, "MaxDepth must be at least 1.");
            _maxDepth = value;
        }
    }

    /// <summary>
    /// Gets how array indices are written into keys. Defaults to <see cref="FlattenJson.ArrayIndexFormat.Dot"/>.
    /// </summary>
    public ArrayIndexFormat ArrayIndexFormat { get; init; } = ArrayIndexFormat.Dot;

    /// <summary>
    /// Gets a value indicating whether empty objects and arrays are emitted as <c>"{}"</c> and <c>"[]"</c>
    /// leaf values (and restored as empty containers on unflatten). Defaults to <c>false</c>, which omits them.
    /// </summary>
    public bool IncludeEmptyContainers { get; init; }
}

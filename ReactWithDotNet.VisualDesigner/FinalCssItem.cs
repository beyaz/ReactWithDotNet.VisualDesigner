global using static ReactWithDotNet.VisualDesigner.FinalCssItemFactory;

namespace ReactWithDotNet.VisualDesigner;

static class FinalCssItemFactory
{
    public static Result<FinalCssItem> CreateFinalCssItem(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new ArgumentException("Style name cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return new ArgumentException("Style value cannot be whitespace.", nameof(value));
        }

        return new FinalCssItem { Name = name, Value = value };
    }

    public static Result<FinalCssItem> CreateFinalCssItem(KeyValuePair<string, string> keyValuePair)
    {
        return CreateFinalCssItem(keyValuePair.Key, keyValuePair.Value);
    }
}

public sealed class FinalCssItem
{
    internal FinalCssItem()
    {
    }

    public required string Name { get; init; }

    public required string Value { get; init; }

    public override string ToString()
    {
        return $"{Name}: {Value};";
    }
}
global using static ReactWithDotNet.VisualDesigner.PropertyDomain.ParsedPropertyFactory;
global using static ReactWithDotNet.VisualDesigner.PropertyDomain.ParsedPropertyExtensions;

namespace ReactWithDotNet.VisualDesigner.PropertyDomain;

public interface ParsedProperty
{
    public string Name { get; }

    public string Value { get; }
}

static class ParsedPropertyExtensions
{
    public static bool Is(this Result<ParsedProperty> result, string name, string value)
    {
        if (result.HasError)
        {
            return false;
        }

        return result.Value.Name == name && result.Value.Value == value;
    }

    public static bool Is(this Result<ParsedProperty> result, Func<ParsedProperty, bool> nextFunc)
    {
        if (result.HasError)
        {
            return false;
        }

        return nextFunc(result.Value);
    }
}

public static class ParsedPropertyFactory
{
    // todo:remove me
    public static Maybe<ParsedProperty> TryParseProperty(string nameValueCombined)
    {
        var result = ParseProperty(nameValueCombined);
        if (!result.HasError)
        {
            return Maybe<ParsedProperty>.Some(result.Value);
        }

        return None;
    }
    public static Result<ParsedProperty> ParseProperty(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return new ArgumentNullException(nameof(nameValueCombined));
        }

        if (nameValueCombined.StartsWith("..."))
        {
            return new ParsedPropertyImpl
            {
                Name  = Design.SpreadOperator,
                Value = nameValueCombined
            };
        }

        var colonIndex = nameValueCombined.IndexOf(':');
        if (colonIndex < 0)
        {
            return new ArgumentNullException($"{nameValueCombined} should contains :");
        }

        var name = nameValueCombined[..colonIndex];

        var value = nameValueCombined[(colonIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ArgumentNullException($"{nameValueCombined} should contains value");
        }

        return new ParsedPropertyImpl
        {
            Name  = name.Trim(),
            Value = value
        };
    }

    sealed class ParsedPropertyImpl : ParsedProperty
    {
        public string Name { get; init; }

        public string Value { get; init; }
    }
}
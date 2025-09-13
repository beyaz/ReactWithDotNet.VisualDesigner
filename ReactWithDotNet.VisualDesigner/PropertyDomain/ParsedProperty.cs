
global using static ReactWithDotNet.VisualDesigner.PropertyDomain.ParsedPropertyFactory;

namespace ReactWithDotNet.VisualDesigner.PropertyDomain;

public interface IParsedProperty
{
    public string Name { get; }

    public string Value { get; }
}

static class ParsedPropertyFactory
{
    
    public static bool Is(this Result<IParsedProperty> result, string name, string value)
    {
        if (result.HasError)
        {
            return false;
        }

        return result.Value.Name == name && result.Value.Value == value;
    }
    
    public static bool Is(this Result<IParsedProperty> result, Func<IParsedProperty, bool> nextFunc)
    {
        if (result.HasError)
        {
            return false;
        }

        return nextFunc(result.Value);
    }
    
    public static Result<IParsedProperty> ParseProperty(string nameValueCombined)
    {
        if (string.IsNullOrWhiteSpace(nameValueCombined))
        {
            return new ArgumentNullException(nameof(nameValueCombined));
        }

        if (nameValueCombined.StartsWith("..."))
        {
            return new ParsedProperty
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

        return new ParsedProperty
        {
            Name  = name.Trim(),
            Value = value
        };
    }

    sealed class ParsedProperty : IParsedProperty
    {
        public string Name { get; init; }

        public string Value { get; init; }
    }
}
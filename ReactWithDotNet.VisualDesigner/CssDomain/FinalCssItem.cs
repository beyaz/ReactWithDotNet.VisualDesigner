global using static ReactWithDotNet.VisualDesigner.CssDomain.FinalCssItemFactory;

namespace ReactWithDotNet.VisualDesigner.CssDomain;

static class FinalCssItemFactory
{
    public static Result<FinalCssItem> CreateFinalCssItem(string name, string value)
    {
        return CreateFinalCssItem(new(){Name = name, Value = value});
    }
    
    public static Result<FinalCssItem> CreateFinalCssItem(CreateFinalCssItemInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return new ArgumentException("Style name cannot be null or whitespace.", nameof(input.Name));
        }

        if (string.IsNullOrWhiteSpace(input.Value))
        {
            return new ArgumentException("Style value cannot be whitespace.", nameof(input.Value));
        }

        return new FinalCssItemImp { Name = input.Name, Value = input.Value };
    }
    
    sealed class FinalCssItemImp : FinalCssItem
    {
        public required string Name { get; init; }

        public required string Value { get; init; }

        public override string ToString()
        {
            return $"{Name}: {Value};";
        }
    }
}

public sealed record CreateFinalCssItemInput
{
    public required string Name { get; init; }

    public required string Value { get; init; }
    
    public static implicit operator CreateFinalCssItemInput(KeyValuePair<string, string> keyValuePair)
    {
        return new() { Name = keyValuePair.Key, Value = keyValuePair.Value };
    }
}

public interface FinalCssItem
{
    public string Name { get;  }

    public string Value { get;  }
}
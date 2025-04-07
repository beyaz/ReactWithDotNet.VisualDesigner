namespace ReactWithDotNet.VisualDesigner.Models;

public sealed record PropertyInfo
{
    public string Name { get; init; }

    public IReadOnlyList<string> Suggestions { get; init; }
}

public sealed record PropertyGroupModel
{
    // @formatter:off
    
    public string Condition { get; set; }

    public List<string> Items { get; init; } = [];
    
    // @formatter:on
}

public sealed record VisualElementModel
{
    // @formatter:off
    
    public string Tag { get; set; }
    
    public string Text { get; set; }
    
    public List<PropertyGroupModel> StyleGroups { get; init; } = [];
    
    public List<string> Properties { get; init; } = [];
    
    public List<VisualElementModel> Children { get; init; } = [];
    
    // @formatter:on
}
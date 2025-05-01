namespace ReactWithDotNet.VisualDesigner.Models;

public sealed record PropertyInfo
{
    public string Name { get; init; }

    public IReadOnlyList<string> Suggestions { get; init; }
}

public sealed record VisualElementModel
{
    // @formatter:off
    
    public string Tag { get; set; }
    
    public string Text { get; set; }
    
    public List<string> Properties { get; init; } = [];
    
    public List<string> Styles { get; init; } = [];
    
    public List<VisualElementModel> Children { get; init; } = [];

    public bool HideInDesigner { get; set; }
    
    // @formatter:on
}
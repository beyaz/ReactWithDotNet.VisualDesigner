namespace ReactWithDotNet.VisualDesigner.Models;

public sealed record VisualElementModel
{
    // @formatter:off
    
    public string Tag { get; init; }
    
    public List<string> Properties { get; init; } = [];
    
    public List<string> Styles { get; init; } = [];
    
    public List<VisualElementModel> Children { get; init; } = [];

    public bool HideInDesigner { get; init; }
    
    // @formatter:on
}
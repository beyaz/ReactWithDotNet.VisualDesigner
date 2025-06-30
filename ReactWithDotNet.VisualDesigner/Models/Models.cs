namespace ReactWithDotNet.VisualDesigner.Models;

public sealed record VisualElementModel
{
    // @formatter:off
    
    public string Tag { get; init; }
    
    public IReadOnlyList<string> Properties { get; init; } = [];
    
    public IReadOnlyList<string> Styles { get; init; } = [];
    
    public List<VisualElementModel> Children { get; init; } = [];

    public bool HideInDesigner { get; init; }
    
    // @formatter:on
}
namespace ReactWithDotNet.VisualDesigner;

public sealed record StyleAttribute
{
    public string Name { get; init; }
    public string Pseudo { get; init; }
    public string Value { get; init; }
}
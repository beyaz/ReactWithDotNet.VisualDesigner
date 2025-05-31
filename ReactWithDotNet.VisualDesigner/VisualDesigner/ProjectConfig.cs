namespace ReactWithDotNet.VisualDesigner;

public sealed record ProjectConfig
{
    public string Name { get; init; }
    
    public IReadOnlyDictionary<string, string> Colors { get; init; } = new Dictionary<string, string>();

    public string GlobalCss { get; init; }

    public IReadOnlyDictionary<string, string> Styles { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Suggestions { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}
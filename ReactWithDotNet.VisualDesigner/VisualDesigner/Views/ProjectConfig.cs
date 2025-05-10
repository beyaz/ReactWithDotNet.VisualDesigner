namespace ReactWithDotNet.VisualDesigner.Views;

sealed record ProjectConfig
{
    public IReadOnlyDictionary<string, string> Colors { get; init; }

    public IReadOnlyDictionary<string, string> Styles { get; init; }

    public string GlobalCss { get; init; }
    
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Suggestions { get; init; }
}
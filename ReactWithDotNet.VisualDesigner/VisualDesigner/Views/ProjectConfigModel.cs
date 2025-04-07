namespace ReactWithDotNet.VisualDesigner.Views;

sealed record ProjectConfigModel
{
    public IReadOnlyDictionary<string, string> Colors { get; init; }

    public IReadOnlyDictionary<string, string> Styles { get; init; }

    public string GlobalCss { get; init; }
}
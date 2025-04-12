namespace ReactWithDotNet.VisualDesigner.Views;

sealed record ProjectConfigModel
{
    public IReadOnlyDictionary<string, string> Colors { get; init; }

    public IReadOnlyDictionary<string, string> Styles { get; init; }

    public string GlobalCss { get; init; }

    public IReadOnlyList<ExternalComponent> ExternalComponents { get; init; }
}

class ExternalComponent
{
    public string Name { get; init; }
    
    public IReadOnlyList<ExternalComponentEvent> Events { get; init; }
}

class ExternalComponentEvent
{
    public string Name { get; init; }
    public IReadOnlyList<ExternalComponentParameter> Parameters { get; init; }
}

class ExternalComponentParameter
{
    public string Name { get; init; }
    public string Type { get; init; }
}
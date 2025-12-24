namespace ReactWithDotNet.VisualDesigner;

public sealed record ProjectConfig
{
    public string Name { get; init; }
    
    public IReadOnlyDictionary<string, string> Colors { get; init; } = new Dictionary<string, string>();

    public string GlobalCss { get; init; }

    public IReadOnlyDictionary<string, string> Styles { get; init; } = new Dictionary<string, string>();

    public bool ExportStylesAsTailwind { get; init; }
    
    public bool ExportStylesAsInline { get; init; }
    
    public string TranslationFunctionName { get; init; }
    
    public bool ExportAsCSharp { get; init; }
    
    public bool ExportAsCSharpString { get; init; }

    public string PrettierOptions { get; init; }
    
    public string ProjectDirectory { get; init; }
}

public sealed record ComponentConfig
{
    // @formatter:off
    
    public string Name { get; init; }
    
    public string DesignLocation { get => field?.Trim(); init; }
    
    public string OutputFilePath { get => field?.Trim(); init; }
    
    public string Translate { get => field?.Trim(); init; }

    public bool SkipExport { get; init; }
    
    public bool Inline  { get; init; }

    // @formatter:on
   
}


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

    public int TabWidth { get; init; } = 2;
    
    public string ProjectDirectory { get; init; }
}

public sealed record ComponentConfig
{
    public string Name { get; init; }
    
    public string ExportFilePath { get; init; }

    public IReadOnlyList<VariableConfig> DotNetVariables { get; init; } = [];

    public bool SkipExport { get; init; }
    
    public string SolutionDirectory { get => field?.Trim(); init; }
    
    public string Translate { get => field?.Trim(); init; }

    public string DesignLocation { get => field?.Trim(); init; }
    
    public string OutputFilePath { get => field?.Trim(); init; }
   
}

public sealed record VariableConfig
{
    public string DotNetAssemblyFilePath { get => field?.Trim(); init; }

    public string DotnetTypeFullName { get => field?.Trim(); init; }

    public string VariableName { get => field?.Trim(); init; }
}


namespace BDigitalFrameworkApiToTsExporter;

sealed class Config
{
    public string? AssemblyFilePath { get; set; }
    
    public string? OutputDirectoryPath { get; set; }
    
    public string[]? ListOfTypes { get; set; }
}
namespace b_digital_framework_type_exporter;

sealed class Config
{
    public string? AssemblyFilePath { get; set; }
    
    public string? OutputDirectoryPath { get; set; }
    
    public string[]? ListOfTypes { get; set; }
}
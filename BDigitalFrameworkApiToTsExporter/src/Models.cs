namespace BDigitalFrameworkApiToTsExporter;

sealed record TsFieldDefinition
{
    public required string Name { get; init; }
    
    public required bool IsNullable { get; init; }
    
    public required string TypeName { get; init; }

    public override string ToString()
    {
        return $"{Name}{(IsNullable ? '?' : string.Empty)} : {TypeName};";
    }
}
namespace BDigitalFrameworkApiToTsExporter;

sealed record TsFieldDefinition
{
    // @formatter:off
    
    public required string Name { get; init; }
    
    public required bool IsNullable { get; init; }
    
    public required string TypeName { get; init; }
    
    public required string ConstantValue { get; init; }

    public override string ToString()
    {
        return TsOutput.GetTsCode(this);
    }
    
    // @formatter:on
}

sealed record TsTypeDefinition
{
    // @formatter:off
    
    public required string Name { get; init; }
    
    public required bool IsEnum { get; init; }
    
    public required string BaseTypeName { get; init; }

    public required IReadOnlyList<TsFieldDefinition> Fields { get; init; }
    
    // @formatter:on
}
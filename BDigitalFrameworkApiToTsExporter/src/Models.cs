namespace BDigitalFrameworkApiToTsExporter;

sealed record TsFieldDefinition
{
    // @format: off
    public required string Name { get; init; }
    
    public required bool IsNullable { get; init; }
    
    public required string TypeName { get; init; }
    
    public required string ConstantValue { get; init; }

    // @format: on

    public override string ToString()
    {
        return TsOutput.GetTsCode(this);
    }
}

sealed record TsTypeDefinition
{
    // @format: off
    
    public required string Name { get; init; }
    
    public required bool IsEnum { get; init; }
    
    public required string BaseTypeName { get; init; }

    // @format: on

    public required IReadOnlyList<TsFieldDefinition> Fields { get; init; }
}
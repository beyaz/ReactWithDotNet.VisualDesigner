namespace BDigitalFrameworkApiToTsExporter;

sealed record TsImportInfo
{
    // @formatter:off
    
    public required string LocalName { get; init; }
    
    public required string Source { get; init; }
    
    // @formatter:on
}

sealed record TsTypeReference
{
    // @formatter:off
    
    public required string Name { get; init; }
    
    public required IReadOnlyList<TsImportInfo> Imports { get; init; }
    
    // @formatter:on
}


sealed record TsFieldDefinition
{
    // @formatter:off
    
    public required string Name { get; init; }
    
    public required bool IsNullable { get; init; }
    
    public required TsTypeReference Type { get; init; }
    
    public required string ConstantValue { get; init; }
    
    // @formatter:on
}

sealed record TsTypeDefinition
{
    // @formatter:off
    
    public required string Name { get; init; }
    
    public required bool IsEnum { get; init; }
    
    public required TsTypeReference BaseType { get; init; }

    public required IReadOnlyList<TsFieldDefinition> Fields { get; init; }
    
    // @formatter:on
}

class LineCollection : List<string>
{
    public void Add(IEnumerable<LineCollection> lineCollections)
    {
        foreach (var collection in lineCollections)
        {
            AddRange(collection);
        }
    }
}
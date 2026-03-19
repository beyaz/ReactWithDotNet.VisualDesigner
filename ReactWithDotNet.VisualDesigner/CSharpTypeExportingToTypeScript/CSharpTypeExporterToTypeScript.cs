using Mono.Cecil;
using static ReactWithDotNet.VisualDesigner.CSharpTypeExportingToTypeScript.Extensions;

namespace ReactWithDotNet.VisualDesigner.CSharpTypeExportingToTypeScript;

sealed record TsFieldDefinition
{
    // @formatter:off
    
    public required string Name { get; init; }
    
    public required bool IsNullable { get; init; }
    
    public required string Type { get; init; }
    
    public required string ConstantValue { get; init; }
    
    // @formatter:on
}

sealed record TsTypeDefinition
{
    // @formatter:off
    
    public required string Name { get; init; }
    
    public required bool IsEnum { get; init; }
    
    public required string BaseType { get; init; }

    public required IReadOnlyList<TsFieldDefinition> Fields { get; init; }
    
    // @formatter:on
}


/*
 * ExportTypeScriptFromCSharp
    variables:
      - ApiDll: 'c:\dd.c.dll'
      - out: d:\boa\tools\
      - ns: BOA.Common.X.Api
     
   
    types:
      - type: {ns}.Models.XModel1
        output: {out}{TypeName}.ts
   
      - type: {ns}.Types.User*
        output: {out}{TypeName}.ts
   	 
      - type: {ns}.Types.User
        output: {out}{TypeName}.ts
   	 includeOnlyProperties:[ 'abc', 'yx']
   	 
      - type: {ns}.Types.User
        output: {out}{TypeName}.ts
   	 excludeProperties:[ 'abc', 'yx']
 */
static class CSharpTypeExporterToTypeScript
{

    public static TsTypeDefinition CreateFrom(TypeDefinition typeDefinition)
    {
        IEnumerable<TsFieldDefinition> fields;
        if (typeDefinition.IsEnum)
        {
            fields =
                from fieldDefinition in typeDefinition.Fields
                where fieldDefinition.Name != "value__"
                select new TsFieldDefinition
                {
                    Name          = fieldDefinition.Name,
                    ConstantValue = fieldDefinition.Constant + string.Empty,
                    IsNullable    = false,
                    Type = string.Empty
                };
        }
        else
        {
            fields =
                from propertyDefinition in typeDefinition.Properties
                where propertyDefinition.SetMethod is not null
                where propertyDefinition.IsExportable
                select new TsFieldDefinition
                {
                    Name          = propertyDefinition.Name,
                    IsNullable    = propertyDefinition.IsNullable,
                    Type          = GetTSType(propertyDefinition.PropertyType),
                    ConstantValue = string.Empty
                };
        }

        return new()
        {
            Name = typeDefinition.Name,

            IsEnum = typeDefinition.IsEnum,

            BaseType = typeDefinition.BaseType switch
            {
                var baseType when baseType is null ||
                                  baseType.FullName == typeof(object).FullName ||
                                  baseType.FullName == typeof(Enum).FullName
                    => string.Empty,
               
                _ => GetTSType(typeDefinition.BaseType)
            },

            Fields = fields.ToList()
        };
    }
    
    static string GetTSType(TypeReference typeReference)
    {
        return Exec
        (
            typeReference,
            
            UnwrapNullable,
            x => ExecUntilNotNull(x,
            [
                TryGetPrimitive,
                TryGetArray,
                CreateComplex
            ])
        );

        static TypeReference UnwrapNullable(TypeReference t)
        {
            return t.IsNullable
                ? UnwrapNullable(((GenericInstanceType)t).GenericArguments[0])
                : t;
        }

        static string TryGetPrimitive(TypeReference t)
        {
            return t switch
            {
                _ when t.IsString   => "string",
                _ when t.IsNumber   => "number",
                _ when t.IsDateTime => "Date",
                _ when t.IsBoolean  => "boolean",
                _ when t.IsObject   => "any",
                _                   => null
            };

            
        }

        static string TryGetArray(TypeReference t)
        {
            if (t is not GenericInstanceType g)
            {
                return null;
            }

            var isArray =
                g.GenericArguments.Count == 1 &&
                g.ElementType.IsCollectionType;

            if (!isArray)
            {
                return null;
            }

            var element = GetTSType(g.GenericArguments[0]);

            return element + "[]";
        }

        static string CreateComplex(TypeReference t)
        {
            return t.Name;
        }
    }
}
using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class TsModelCreator
{
    public static TsTypeDefinition CreateFrom(IReadOnlyList<ExternalTypeInfo> externalTypes, TypeDefinition typeDefinition, string apiName)
    {
        var isExportingForModelFile = typeDefinition.Name.EndsWith("Model");

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
                    Type = new()
                    {
                        Name    = string.Empty,
                        Imports = []
                    }
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
                    Name          = GetTsVariableName(propertyDefinition.Name),
                    IsNullable    = CecilHelper.IsNullableProperty(propertyDefinition),
                    Type          = GetTSType(externalTypes, propertyDefinition.PropertyType, isExportingForModelFile, apiName),
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
                    => new()
                    {
                        Name    = string.Empty,
                        Imports = []
                    },
                var baseType when baseType.FullName == typeof(Enum).FullName
                    => new()
                    {
                        Name    = string.Empty,
                        Imports = []
                    },
                _ => GetTSType(externalTypes, typeDefinition.BaseType, isExportingForModelFile, apiName)
            },

            Fields = fields.ToList()
        };
    }

    public static string GetExtraClassFileName(TypeReference typeReference, string apiName)
    {
        return typeReference.Name.RemoveFromStart(apiName, StringComparison.OrdinalIgnoreCase).RemoveFromEnd("Contract");
    }

    static TsTypeReference GetTSType(IReadOnlyList<ExternalTypeInfo> externalTypes, TypeReference typeReference, bool isExportingForModelFile, string apiName)
    {
        if (CecilHelper.IsNullableType(typeReference))
        {
            return GetTSType(externalTypes, ((GenericInstanceType)typeReference).GenericArguments[0], isExportingForModelFile, apiName);
        }

        return ExecUntilNotNull(typeReference, [
            TryGetPrimitive,
            TryGetArray,
            CreateComplex
        ]);

        static TsTypeReference TryGetPrimitive(TypeReference t)
        {
            return t switch
            {
                _ when t.IsString   => Create("string"),
                _ when t.IsNumber   => Create("number"),
                _ when t.IsDateTime => Create("Date"),
                _ when t.IsBoolean  => Create("boolean"),
                _ when t.IsObject   => Create("any"),
                _                   => null
            };

            static TsTypeReference Create(string name)
            {
                return new()
                {
                    Name    = name,
                    Imports = []
                };
            }
        }

        TsTypeReference TryGetArray(TypeReference t)
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

            var element = GetTSType(externalTypes, g.GenericArguments[0], isExportingForModelFile, apiName);

            return element with
            {
                Name = element.Name + "[]"
            };
        }

        TsTypeReference CreateComplex(TypeReference t)
        {
            return new()
            {
                Name    = t.Name,
                Imports = GetImports(externalTypes, t, isExportingForModelFile, apiName).ToList()
            };
        }

        static IEnumerable<TsImportInfo> GetImports(IReadOnlyList<ExternalTypeInfo> externalTypes, TypeReference typeReference, bool isExportingForModelFile, string apiName)
        {
            var list = new List<TsImportInfo>
            {
                from externalType in externalTypes
                where externalType.DotNetFullTypeName == typeReference.FullName
                select new TsImportInfo
                {
                    LocalName = externalType.LocalName,
                    Source    = externalType.Source
                }
            };

            if (list.Count > 0)
            {
                return list;
            }

            if (isExportingForModelFile)
            {
                return
                [
                    new()
                    {
                        LocalName = typeReference.Name,
                        Source    = "../types"
                    }
                ];
            }

            return
            [
                new()
                {
                    LocalName = typeReference.Name,
                    Source    = $"./{GetExtraClassFileName(typeReference, apiName)}"
                }
            ];
        }
    }
}
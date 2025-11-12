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
                where !IsImplicitDefinition(propertyDefinition)
                where !IsJsonIgnored(propertyDefinition)
                select new TsFieldDefinition
                {
                    Name          = GetTsVariableName(propertyDefinition.Name),
                    IsNullable    = CecilHelper.IsNullableProperty(propertyDefinition),
                    Type          = GetTSType(externalTypes, propertyDefinition.PropertyType,isExportingForModelFile,apiName),
                    ConstantValue = string.Empty
                };
        }

        return new()
        {
            Name = typeDefinition.Name,

            IsEnum = typeDefinition.IsEnum,

            BaseType = (typeDefinition.BaseType.FullName == typeof(object).FullName) switch
            {
                true => new()
                {
                    Name    = string.Empty,
                    Imports = []
                },
                false => GetTSType(externalTypes, typeDefinition.BaseType,isExportingForModelFile, apiName)
            },

            Fields = fields.ToList()
        };
    }

    static TsTypeReference GetTSType(IReadOnlyList<ExternalTypeInfo> externalTypes, TypeReference typeReference, bool isExportingForModelFile, string apiName)
    {
        if (CecilHelper.IsNullableType(typeReference))
        {
            return GetTSType(externalTypes, ((GenericInstanceType)typeReference).GenericArguments[0], isExportingForModelFile,apiName);
        }

        if (typeReference.FullName == "System.String")
        {
            return new()
            {
                Name    = "string",
                Imports = []
            };
        }

        if (typeReference.FullName == typeof(short).FullName ||
            typeReference.FullName == typeof(int).FullName ||
            typeReference.FullName == typeof(byte).FullName ||
            typeReference.FullName == typeof(sbyte).FullName ||
            typeReference.FullName == typeof(short).FullName ||
            typeReference.FullName == typeof(ushort).FullName ||
            typeReference.FullName == typeof(double).FullName ||
            typeReference.FullName == typeof(float).FullName ||
            typeReference.FullName == typeof(decimal).FullName ||
            typeReference.FullName == typeof(long).FullName)

        {
            return new()
            {
                Name    = "number",
                Imports = []
            };
        }

        if (typeReference.FullName == "System.DateTime")
        {
            return new()
            {
                Name    = "Date",
                Imports = []
            };
        }

        if (typeReference.FullName == "System.Boolean")
        {
            return new()
            {
                Name    = "boolean",
                Imports = []
            };
        }

        if (typeReference.FullName == "System.Object")
        {
            return new()
            {
                Name    = "any",
                Imports = []
            };
        }

        if (typeReference.IsGenericInstance)
        {
            var genericInstanceType = (GenericInstanceType)typeReference;

            var isArrayType =
                genericInstanceType.GenericArguments.Count == 1 &&
                (
                    typeReference.Name == "Collection`1" ||
                    typeReference.Name == "List`1" ||
                    typeReference.Name == "IReadOnlyCollection`1" ||
                    typeReference.Name == "IReadOnlyList`1"
                );

            if (isArrayType)
            {
                var arrayType = genericInstanceType.GenericArguments[0];

                var tsTypeReference = GetTSType(externalTypes, arrayType, isExportingForModelFile, apiName);

                return tsTypeReference with
                {
                    Name = tsTypeReference.Name + "[]"
                };
            }
        }

        return new()
        {
            Name    = typeReference.Name,
            Imports = GetImports(externalTypes, typeReference, isExportingForModelFile,apiName).ToList()
        };

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
                    new TsImportInfo
                    {
                        LocalName = typeReference.Name,
                        Source    = "../types"
                    }
                ];
            }

            return
            [
                new TsImportInfo
                {
                    LocalName = typeReference.Name,
                    Source    = $"./{GetExtraClassFileName(typeReference, apiName)}"
                }
            ];

        }
    }

    static bool IsImplicitDefinition(PropertyDefinition propertyDefinition)
    {
        if (propertyDefinition.PropertyType.FullName == "System.Runtime.Serialization.ExtensionDataObject")
        {
            return true;
        }

        if (propertyDefinition.Name == "EqualityContract")
        {
            return true;
        }

        return propertyDefinition.Name.Contains(".");
    }
    
    static bool IsJsonIgnored(PropertyDefinition propertyDefinition)
    {
        return propertyDefinition.CustomAttributes.Any(x => 
            x.AttributeType.Name.Contains("JsonIgnore", StringComparison.OrdinalIgnoreCase)||
            x.AttributeType.Name.Contains("TsIgnore", StringComparison.OrdinalIgnoreCase));
    }


    public static string GetExtraClassFileName(TypeReference typeReference, string apiName)
    {
        return typeReference.Name.RemoveFromStart(apiName, StringComparison.OrdinalIgnoreCase).RemoveFromEnd("Contract");
    }
    
}


using Mono.Cecil;
using Newtonsoft.Json.Serialization;

namespace BDigitalFrameworkApiToTsExporter;

static class TsModelCreator
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
                    Type      = new TsTypeReference
                    {
                        Name = string.Empty,
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
                select new TsFieldDefinition
                {
                    Name          = TypescriptNaming.GetResolvedPropertyName(propertyDefinition.Name),
                    IsNullable    = CecilHelper.IsNullableProperty(propertyDefinition),
                    Type      = GetTSType(propertyDefinition.PropertyType),
                    ConstantValue = string.Empty
                };
        }
        
        return new()
        {
            Name = typeDefinition.Name,

            IsEnum = typeDefinition.IsEnum,

            BaseTypeName = (typeDefinition.BaseType.FullName == typeof(object).FullName) switch
            {
                true  => new()
                {
                    Name    = string.Empty,
                    Imports = []
                },
                false => GetTSType(typeDefinition.BaseType)
            },

            Fields = fields.ToList()
        };
    }


    
    
    static TsTypeReference GetTSType(TypeReference typeReference)
    {
        if (CecilHelper.IsNullableType(typeReference))
        {
            return GetTSType(((GenericInstanceType)typeReference).GenericArguments[0]);
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

                var tsTypeReference = GetTSType(arrayType);

                return tsTypeReference with
                {
                    Name = tsTypeReference.Name + "[]"
                };
            }
        }
        
        return new()
        {
            Name    = typeReference.Name,
            Imports = [] // todo: autofind file
        };

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

    static class TypescriptNaming
    {
        static readonly CamelCasePropertyNamesContractResolver CamelCasePropertyNamesContractResolver = new();

        public static string GetResolvedPropertyName(string propertyNameInCSharp)
        {
            return CamelCasePropertyNamesContractResolver.GetResolvedPropertyName(propertyNameInCSharp);
        }
    }
}
using Mono.Cecil;
using Newtonsoft.Json.Serialization;

namespace BDigitalFrameworkApiToTsExporter;

static class TsModelCreator
{
    public static TsTypeDefinition CreatFrom(TypeDefinition typeDefinition)
    {
        IEnumerable<TsFieldDefinition> fields;
        if (typeDefinition.IsEnum)
        {
            fields = from fieldDefinition in typeDefinition.Fields.Where(f => f.Name != "value__")
                select new TsFieldDefinition
                {
                    Name          = fieldDefinition.Name,
                    ConstantValue = fieldDefinition.Constant + string.Empty,
                    IsNullable    = false,
                    TypeName      = string.Empty
                };
        }
        else
        {
            fields =
                from propertyDefinition in typeDefinition.Properties.Where(p => !IsImplicitDefinition(p))
                select new TsFieldDefinition
                {
                    Name          = TypescriptNaming.GetResolvedPropertyName(propertyDefinition.Name),
                    IsNullable    = CecilHelper.isNullableProperty(propertyDefinition),
                    TypeName      = GetTSTypeName(propertyDefinition.PropertyType),
                    ConstantValue = string.Empty
                };
        }

        return new()
        {
            Name = typeDefinition.Name,

            IsEnum = typeDefinition.IsEnum,

            BaseTypeName = typeDefinition.BaseType.FullName == typeof(object).FullName ? string.Empty : typeDefinition.BaseType.Name,

            Fields = fields.ToList()
        };
    }

    static string GetTSTypeName(TypeReference typeReference)
    {
        if (CecilHelper.IsNullableType(typeReference))
        {
            return GetTSTypeName(((GenericInstanceType)typeReference).GenericArguments[0]);
        }

        if (typeReference.FullName == "System.String")
        {
            return "string";
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
            return "number";
        }

        if (typeReference.FullName == "System.DateTime")
        {
            return "Date";
        }

        if (typeReference.FullName == "System.Boolean")
        {
            return "boolean";
        }

        if (typeReference.FullName == "System.Object")
        {
            return "any";
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
                return GetTSTypeName(arrayType) + "[]";
            }
        }

        return typeReference.Name;
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
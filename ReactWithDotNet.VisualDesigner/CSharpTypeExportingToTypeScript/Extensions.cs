global using static ReactWithDotNet.VisualDesigner.CSharpTypeExportingToTypeScript.Extensions;

using Mono.Cecil;

namespace ReactWithDotNet.VisualDesigner.CSharpTypeExportingToTypeScript;

static class Extensions
{
    extension(TypeReference typeReference)
    {
        public bool IsString
            => typeReference.FullName == typeof(string).FullName;

        public bool IsNumber
            => typeReference.FullName == typeof(short).FullName ||
               typeReference.FullName == typeof(int).FullName ||
               typeReference.FullName == typeof(byte).FullName ||
               typeReference.FullName == typeof(sbyte).FullName ||
               typeReference.FullName == typeof(short).FullName ||
               typeReference.FullName == typeof(ushort).FullName ||
               typeReference.FullName == typeof(double).FullName ||
               typeReference.FullName == typeof(float).FullName ||
               typeReference.FullName == typeof(decimal).FullName ||
               typeReference.FullName == typeof(long).FullName;

        public bool IsDateTime
            => typeReference.FullName == typeof(DateTime).FullName;

        public bool IsBoolean
            => typeReference.FullName == typeof(bool).FullName;

        public bool IsObject
            => typeReference.FullName == typeof(object).FullName;
    }

    extension(PropertyDefinition propertyDefinition)
    {
        internal bool IsExportable
        {
            get
            {
                if (propertyDefinition.IsImplicitDefinition)
                {
                    return false;
                }

                if (propertyDefinition.IsJsonIgnored)
                {
                    return false;
                }

                return true;
            }
        }

        bool IsImplicitDefinition
        {
            get
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
        }

        bool IsJsonIgnored
        {
            get
            {
                if (HasCustomAttributeNameLike("TsInclude"))
                {
                    return false;
                }

                return HasCustomAttributeNameLike("JsonIgnore") || HasCustomAttributeNameLike("TsIgnore");

                bool HasCustomAttributeNameLike(string attributeName)
                {
                    return propertyDefinition.CustomAttributes.Any(x => x.AttributeType.Name.Contains(attributeName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}
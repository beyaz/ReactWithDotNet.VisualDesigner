using Mono.Cecil;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReactWithDotNet.VisualDesigner.CSharpTypeExportingToTypeScript;

static class Extensions
{
    internal static B ExecUntilNotNull<A, B>(A a, Func<A, B>[] methods)
    {
        foreach (var func in methods)
        {
            var result = func(a);
            if (result is not null)
            {
                return result;
            }
        }

        return default;
    }
    
    internal static C Exec<A, B, C>(A a, Func<A, B> method_a_b, Func<B, C> method_b_c)
    {
        var b = method_a_b(a);

        return method_b_c(b);
    }
    
    internal static C ExecUntilNotNull<A, B, C>(A a, B b, Func<A, B, C>[] methods)
    {
        foreach (var func in methods)
        {
            var result = func(a, b);
            if (result is not null)
            {
                return result;
            }
        }

        return default;
    }
    
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

        public bool IsNullable => typeReference.Name == "Nullable`1" && typeReference.IsGenericInstance;

        public bool IsCollectionType
        {
            get
            {
                var names = new List<string>
                {
                    typeof(Collection<>).FullName,
                    typeof(IReadOnlyCollection<>).FullName,
                    typeof(List<>).FullName,
                    typeof(IReadOnlyList<>).FullName,
                    typeof(ImmutableList<>).FullName
                };

                return names.Any(name => typeReference.FullName.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            }
        }
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

        public bool IsNullable
        {
            get
            {
                // is value type
                if (isSystemNullable(propertyDefinition.PropertyType))
                {
                    return true;
                }

                if (hasNullableAttribute(propertyDefinition, NullabilityState.Nullable))
                {
                    return true;
                }

                if (hasNullableContextAttribute(propertyDefinition, NullabilityState.Nullable))
                {
                    if (propertyDefinition.PropertyType.IsValueType)
                    {
                        return false;
                    }

                    if (hasNullableAttribute(propertyDefinition, NullabilityState.NotNull))
                    {
                        return false;
                    }

                    return true;
                }

                return false;

                static bool isSystemNullable(TypeReference typeReference)
                {
                    return typeReference is GenericInstanceType git && git.ElementType.FullName == "System.Nullable`1";
                }

                static bool hasNullableAttribute(PropertyDefinition propertyDefinition, NullabilityState state)
                {
                    // Reference type nullability (C# 8)
                    var nullableAttribute = propertyDefinition.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(NullableAttribute).FullName);
                    if (nullableAttribute is null)
                    {
                        return false;
                    }

                    // 2:nullable 1:nonnullable 0: oblivious
                    var argument = nullableAttribute.ConstructorArguments[0];
                    if (argument.Type.FullName == "System.Byte")
                    {
                        return (byte)argument.Value == (byte)state;
                    }

                    if (argument.Type.FullName == "System.Byte[]")
                    {
                        return ((CustomAttributeArgument[])argument.Value)[0].Value is byte b && b == (byte)state;
                    }

                    return false;
                }

                static bool hasNullableContextAttribute(PropertyDefinition propertyDefinition, NullabilityState state)
                {
                    // NullableContextAttribute class / module seviyesinde olabilir
                    var nullableContext = propertyDefinition.DeclaringType.CustomAttributes
                                              .FirstOrDefault(a => a.AttributeType.FullName == typeof(NullableContextAttribute).FullName)
                                          ?? propertyDefinition.Module.CustomAttributes
                                              .FirstOrDefault(a => a.AttributeType.FullName == typeof(NullableContextAttribute).FullName);

                    if (nullableContext == null)
                    {
                        return false;
                    }

                    var argument = (byte)nullableContext.ConstructorArguments[0].Value;

                    return argument == (byte)state;
                }
            }
        }
    }
}
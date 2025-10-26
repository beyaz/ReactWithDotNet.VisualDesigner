using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace BDigitalFrameworkApiToTsExporter;

static class CecilHelper
{
    public static Result<TypeDefinition> GetType(AssemblyDefinition assemblyDefinition, string fullTypeName)
    {
        var query = from module in assemblyDefinition.Modules
                    from type in module.Types
                    where type.FullName == fullTypeName
                    select type;
        
        foreach (var typeDefinition in query)
        {
            return typeDefinition;
        }
        
        return new MissingMemberException(fullTypeName);
    }
    
    public static IReadOnlyList<TypeDefinition> GetTypes(AssemblyDefinition assemblyDefinition, IReadOnlyList<string> listOfTypes)
    {
        var typeDefinitions = new List<TypeDefinition>();

        foreach (var item in listOfTypes)
        {
            var typeNamePrefix = item;

            if (typeNamePrefix.EndsWith("*", StringComparison.OrdinalIgnoreCase))
            {
                typeNamePrefix = typeNamePrefix.Remove(typeNamePrefix.Length - 1);

                foreach (var typeDefinition in assemblyDefinition.MainModule.Types)
                {
                    if (typeDefinition.FullName.StartsWith(typeNamePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        typeDefinitions.Add(typeDefinition);
                    }
                }

                continue;
            }

            typeDefinitions.Add(assemblyDefinition.MainModule.GetType(typeNamePrefix));
        }

        return typeDefinitions;
    }

    public static bool IsNullableProperty(PropertyDefinition propertyDefinition)
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

    public static bool IsNullableType(TypeReference typeReference)
    {
        return typeReference.Name == "Nullable`1" && typeReference.IsGenericInstance;
    }

    public static Result<AssemblyDefinition> ReadAssemblyDefinition(string assemblyFilePath)
    {
        const string secondarySearchDirectoryPath = @"d:\boa\server\bin";

        return CecilAssemblyReader.ReadAssembly(assemblyFilePath, secondarySearchDirectoryPath);
    }
}
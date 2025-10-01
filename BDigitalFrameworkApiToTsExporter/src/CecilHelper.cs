using Mono.Cecil;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace BDigitalFrameworkApiToTsExporter;

static class CecilHelper
{
    public static bool isNullableProperty(PropertyDefinition propertyDefinition)
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
    
    public static Result<AssemblyDefinition> ReadAssemblyDefinition(string? assemblyFilePath)
    {
        var primarySearchDirectoryPath = Path.GetDirectoryName(assemblyFilePath) ?? Directory.GetCurrentDirectory();

        const string secondarySearchDirectoryPath = @"d:\boa\server\bin";

        var resolver = new CustomAssemblyResolver(primarySearchDirectoryPath, secondarySearchDirectoryPath);

        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver
        };

        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFilePath, readerParameters);

        if (assemblyDefinition is null)
        {
            return new Exception("AssemblyNotFound:" + assemblyFilePath);
        }

        return assemblyDefinition;
    }

    class CustomAssemblyResolver : BaseAssemblyResolver
    {
        readonly string[] _searchDirectories;

        public CustomAssemblyResolver(params string[] searchDirectories)
        {
            _searchDirectories = searchDirectories;
            foreach (var directory in _searchDirectories)
            {
                AddSearchDirectory(directory);
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            foreach (var directory in _searchDirectories)
            {
                var filePath = Path.Combine(directory, name.Name + ".dll");
                if (File.Exists(filePath))
                {
                    return AssemblyDefinition.ReadAssembly(filePath, parameters);
                }
            }

            // Eğer burada bulamazsa, varsayılan çözümleyiciye dön
            return base.Resolve(name, parameters);
        }
    }
}
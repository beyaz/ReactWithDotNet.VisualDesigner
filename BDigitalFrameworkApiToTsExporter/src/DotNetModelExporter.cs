using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    public static Result<int, Exception> TryExport()
    {
        return 
            from files in CalculateFiles()
            from count in writeFiles(files)
            select count;

        static Result<int> writeFiles(IEnumerable<TsFileModel> files)
        {
            var count = 0;
            foreach (var fileModel in files)
            {
                var exception = writeFile(fileModel);
                if (exception is not null)
                {
                    return exception;
                }

                count++;
            }

            return count;
        }

        static Exception? writeFile(TsFileModel file)
        {
            var fileContent = file.Content;

            if (File.Exists(file.Path))
            {
                return FileSystem.ReadAllText(file.Path).Then(fileContentInDirectory =>
                {
                    fileContentInDirectory ??= string.Empty;

                    var exportIndex = fileContentInDirectory.IndexOf("export ", StringComparison.OrdinalIgnoreCase);
                    if (exportIndex > 0)
                    {
                        fileContent = fileContentInDirectory[..exportIndex] + fileContent;
                    }

                    return FileSystem.WriteAllText(file.Path, fileContent);
                });
            }

            return FileSystem.WriteAllText(file.Path, fileContent);
        }
    }

    internal static IReadOnlyList<string> GetTsCodes(TypeDefinition typeDefinition)
    {
        List<string> lines = [];

        if (typeDefinition.IsEnum)
        {
            lines.Add($"export enum {typeDefinition.Name}");
        }
        else
        {
            var extends = " extends ";
            if (typeDefinition.BaseType.FullName == typeof(object).FullName)
            {
                extends = "";
            }
            else
            {
                extends += typeDefinition.BaseType.Name;
            }

            lines.Add($"export interface {typeDefinition.Name}" + extends);
        }

        lines.Add("{");

        if (typeDefinition.IsEnum)
        {
            var fieldDeclarations = new List<string>();
            foreach (var field in typeDefinition.Fields.Where(f => f.Name != "value__"))
            {
                fieldDeclarations.Add($"{field.Name} = {field.Constant}");
            }

            for (var i = 0; i < fieldDeclarations.Count; i++)
            {
                var declaration = fieldDeclarations[i];

                if (i < fieldDeclarations.Count - 1)
                {
                    lines.Add(declaration + ",");
                }
                else
                {
                    lines.Add(declaration);
                }
            }
        }
        else
        {
            foreach (var propertyDefinition in typeDefinition.Properties.Where(p => !IsImplicitDefinition(p)))
            {
                var typeName = GetTSTypeName(propertyDefinition.PropertyType);

                var name = TypescriptNaming.GetResolvedPropertyName(propertyDefinition.Name);

                if (isNullableProperty(propertyDefinition))
                {
                    name += "?";
                }

                lines.Add($"{name} : {typeName};");
            }
        }

        lines.Add("}");

        return lines;

        static bool isNullableProperty(PropertyDefinition propertyDefinition)
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

        static string GetTSTypeName(TypeReference typeReference)
        {
            if (IsNullableType(typeReference))
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

        static bool IsNullableType(TypeReference typeReference)
        {
            return typeReference.Name == "Nullable`1" && typeReference.IsGenericInstance;
        }
    }

    static Result<IReadOnlyList<TsFileModel>> CalculateFiles()
    {
        var config = ReadConfig();
        if (config is null)
        {
            return new Exception("Config is null");
        }

        AssemblyDefinition assemblyDefinition;
        {
            var result = CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath);
            if (result.HasError)
            {
                return result.Error;
            }

            assemblyDefinition = result.Value;
        }

        var typeDefinitions = pickTypes(assemblyDefinition, config);

        return typeDefinitions.ConvertAll(typeDefinition =>
        {
            var tsCode = LinesToString(GetTsCodes(typeDefinition));

            var filePath = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts");

            return new TsFileModel(filePath, tsCode);
        });

        static List<TypeDefinition> pickTypes(AssemblyDefinition assemblyDefinition, Config config)
        {
            var typeDefinitions = new List<TypeDefinition>();
            {
                foreach (var item in config.ListOfTypes ?? [])
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
            }

            return typeDefinitions;
        }

        static Config? ReadConfig()
        {
            var configFilePath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty, "Config.json");
            if (File.Exists(configFilePath))
            {
                var fileContent = File.ReadAllText(configFilePath);

                return JsonConvert.DeserializeObject<Config>(fileContent);
            }

            return null;
        }
    }

    static string LinesToString(IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();

        var indentCount = 0;

        foreach (var line in lines)
        {
            var padding = string.Empty.PadRight(indentCount * 4, ' ');

            if (line == "{")
            {
                sb.AppendLine(padding + line);
                indentCount++;
                continue;
            }

            if (line == "}")
            {
                indentCount--;

                padding = string.Empty.PadRight(indentCount * 4, ' ');
            }

            sb.AppendLine(padding + line);
        }

        return sb.ToString();
    }

    static class TypescriptNaming
    {
        static readonly CamelCasePropertyNamesContractResolver CamelCasePropertyNamesContractResolver = new();

        public static string GetResolvedPropertyName(string propertyNameInCSharp)
        {
            return CamelCasePropertyNamesContractResolver.GetResolvedPropertyName(propertyNameInCSharp);
        }
    }

    record TsFileModel(string Path, string Content);
}
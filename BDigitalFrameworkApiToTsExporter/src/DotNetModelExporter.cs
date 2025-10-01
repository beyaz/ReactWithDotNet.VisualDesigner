using System.Text;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BDigitalFrameworkApiToTsExporter;

static class DotNetModelExporter
{
    public static Result<Unit> TryExport()
    {
        return
            from files in CalculateFiles()
            from fileModel in files.Select(trySyncWithLocalFileSystem)
            select FileSystem.Save(fileModel);

        static Result<FileModel> trySyncWithLocalFileSystem(FileModel file)
        {
            if (File.Exists(file.Path))
            {
                var result = FileSystem.ReadAllText(file.Path);
                if (result.HasError)
                {
                    return result.Error;
                }

                var fileContentInDirectory = result.Value;

                var exportIndex = fileContentInDirectory.IndexOf("export ", StringComparison.OrdinalIgnoreCase);
                if (exportIndex > 0)
                {
                    return file with
                    {
                        Content = fileContentInDirectory[..exportIndex] + file.Content
                    };
                }
            }

            return file;
        }
    }

    static IReadOnlyList<string> GetTsCodes(TypeDefinition typeDefinition)
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
            var enumFields = from fieldDefinition in typeDefinition.Fields.Where(f => f.Name != "value__")
                select new TsFieldDefinition
                {
                    Name          = fieldDefinition.Name,
                    ConstantValue = fieldDefinition.Constant + string.Empty,
                    IsNullable    = false,
                    TypeName      = string.Empty
                };
            
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
            var properties =
                from propertyDefinition in typeDefinition.Properties.Where(p => !IsImplicitDefinition(p))
                    select new TsFieldDefinition
                    {
                        Name       = TypescriptNaming.GetResolvedPropertyName(propertyDefinition.Name),
                        IsNullable = CecilHelper.isNullableProperty(propertyDefinition),
                        TypeName   = GetTSTypeName(propertyDefinition.PropertyType),
                        ConstantValue = string.Empty
                    };

            foreach (var propertyDefinition in typeDefinition.Properties.Where(p => !IsImplicitDefinition(p)))
            {
                var typeName = GetTSTypeName(propertyDefinition.PropertyType);

                var name = TypescriptNaming.GetResolvedPropertyName(propertyDefinition.Name);

                if (CecilHelper.isNullableProperty(propertyDefinition))
                {
                    name += "?";
                }

                lines.Add($"{name} : {typeName};");
            }
        }

        lines.Add("}");

        return lines;

        

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

        
    }

    static Result<IEnumerable<FileModel>> CalculateFiles()
    {
        return
            from config in ReadConfig()
            from assemblyDefinition in CecilHelper.ReadAssemblyDefinition(config.AssemblyFilePath)
            from typeDefinition in pickTypes(assemblyDefinition, config)
            select new FileModel
            {
                Path    = Path.Combine(config.OutputDirectoryPath ?? string.Empty, $"{typeDefinition.Name}.ts"),
                Content = LinesToString(GetTsCodes(typeDefinition))
            };

        static List<TypeDefinition> pickTypes(AssemblyDefinition assemblyDefinition, Config config)
        {
            var typeDefinitions = new List<TypeDefinition>();

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

            return typeDefinitions;
        }

        static Result<Config> ReadConfig()
        {
            var configFilePath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? string.Empty, "Config.json");
            if (File.Exists(configFilePath))
            {
                var fileContent = File.ReadAllText(configFilePath);

                var config = JsonConvert.DeserializeObject<Config>(fileContent);
                if (config is not null)
                {
                    return config;
                }
            }

            return new IOException("ConfigFileNotRead");
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
}
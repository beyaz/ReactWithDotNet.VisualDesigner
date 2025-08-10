using Mono.Cecil;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class DotNetModelExporter
{
    public static TypeDefinition FindType( this AssemblyDefinition definition, string classFullName)
    {
        foreach (var module in definition.Modules)
        {
            var typeDefinition = module.GetType(classFullName);

            if (typeDefinition != null)
            {
                return typeDefinition;
            }
        }

        return null;
    }
    
    public static Result<IReadOnlyList<string>> Export(IReadOnlyList<ExportInfo> exportInfoList)
    {
        List<string> lines = [];

        var typeDefinitions = new List<TypeDefinition>();

        foreach (var info in exportInfoList)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(@"d:\boa\server\bin\" + info.Assembly);
            if (assemblyDefinition == null)
            {
                return new Exception("AssemblyNotFound:" + info.Assembly);
            }

            foreach (var className in info.ExportClassNames)
            {
                var typeDefinition = assemblyDefinition.FindType(className);

                if (typeDefinition == null)
                {
                    return new Exception("typeDefinitionNotFound:" + className);
                }

                typeDefinitions.Add(typeDefinition);
            }
        }

        var groupBy = typeDefinitions.GroupBy(x => x.Namespace, p => p);
        foreach (var ns in groupBy)
        {
            lines.Add($"namespace {ns.Key}");
            lines.Add("{");

            var types = typeDefinitions.Where(t => t.Namespace == ns.Key);
            foreach (var typeDefinition in types)
            {
                lines.AddRange(GenerateType(typeDefinition));
            }

            lines.Add("}");
        }

        return lines;
    }

    public static string GetTypeNameInContainerNamespace(string typeFullName, string containerNamespace)
    {
        while (true)
        {
            var prefix = containerNamespace + ".";
            if (typeFullName.StartsWith(prefix))
            {
                return typeFullName.RemoveFromStart(prefix);
            }

            var packages = containerNamespace.Split('.');
            if (packages.Length <= 1)
            {
                return typeFullName;
            }

            var items = new List<string>();
            for (var i = 0; i < packages.Length - 1; i++)
            {
                items.Add(packages[i]);
            }

            containerNamespace = string.Join(".", items);
        }
    }

    static IReadOnlyList<string> GenerateType(TypeDefinition typeDefinition)
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
                extends += GetTypeNameInContainerNamespace(typeDefinition.BaseType.FullName, typeDefinition.Namespace);
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
                var typeName = GetTSTypeName(propertyDefinition.PropertyType, typeDefinition.Namespace);

                var name = TypescriptNaming.GetResolvedPropertyName(propertyDefinition.Name);

                // if (IsNullableType(propertyDefinition.DeclaringType))
                {
                    name += "?";
                }

                lines.Add($"{name} : {typeName};");
            }
        }

        lines.Add("}");

        return lines;
    }

    static string GetTSTypeName(TypeReference typeReference, string containerNamespace)
    {
        if (IsNullableType(typeReference))
        {
            return GetTSTypeName(((GenericInstanceType)typeReference).GenericArguments[0], containerNamespace);
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
                return GetTSTypeName(arrayType, containerNamespace) + "[]";
            }
        }

        return GetTypeNameInContainerNamespace(typeReference.FullName, containerNamespace);
    }

    static bool IsImplicitDefinition(PropertyDefinition propertyDefinition)
    {
        if (propertyDefinition.PropertyType.FullName == "System.Runtime.Serialization.ExtensionDataObject")
        {
            return true;
        }

        return propertyDefinition.Name.Contains(".");
    }

    static bool IsNullableType(TypeReference typeReference)
    {
        return typeReference.Name == "Nullable`1" && typeReference.IsGenericInstance;
    }

    public class ExportInfo
    {
        #region Public Properties
        /// <summary>
        ///     Gets or sets the assembly.
        /// </summary>
        public string Assembly { get; set; }

        /// <summary>
        ///     Gets or sets the export class names.
        /// </summary>
        public ICollection<string> ExportClassNames { get; set; }
        #endregion
    }
}
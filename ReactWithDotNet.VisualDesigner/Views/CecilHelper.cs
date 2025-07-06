using Mono.Cecil;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace ReactWithDotNet.VisualDesigner.Views;

static class CecilHelper
{
    public static IReadOnlyList<string> GetPropertyPathList(string assemblyPath, string typeFullName, Func<PropertyDefinition, bool> matchFunc)
    {
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        if (assembly is null)
        {
            return [];
        }

        var type = assembly.MainModule.GetType(typeFullName);
        if (type is null)
        {
            return [];
        }

        return FindStringProperties(type, string.Empty, matchFunc);

        static IReadOnlyList<string> FindStringProperties(TypeDefinition type, string prefix, Func<PropertyDefinition, bool> matchFunc)
        {
            var properties = from p in type.Properties where matchFunc(p) select prefix + p.Name;

            var nestedProperties = from nestedType in type.NestedTypes
                                   from nestedProperty in FindStringProperties(nestedType, prefix + nestedType.Name + ".", matchFunc)
                                   select nestedProperty;

            return properties.Concat(nestedProperties).ToList();
        }
    }

    public static bool IsBooleanProperty(PropertyDefinition propertyDefinition)
    {
        var propertyTypeFullName = propertyDefinition.PropertyType.FullName;

        string[] types =
        [
            typeof(bool).FullName
        ];

        return types.Contains(propertyTypeFullName) ||
               types.Contains(propertyTypeFullName.RemoveFromStart("System.Nullable`1<").RemoveFromEnd(">"));
    }

    public static bool IsDateTimeProperty(PropertyDefinition propertyDefinition)
    {
        var propertyTypeFullName = propertyDefinition.PropertyType.FullName;

        string[] types =
        [
            typeof(DateTime).FullName
        ];

        return types.Contains(propertyTypeFullName) ||
               types.Contains(propertyTypeFullName.RemoveFromStart("System.Nullable`1<").RemoveFromEnd(">"));
    }

    public static bool IsNumberProperty(PropertyDefinition propertyDefinition)
    {
        var propertyTypeFullName = propertyDefinition.PropertyType.FullName;

        string[] types =
        [
            typeof(sbyte).FullName,
            typeof(byte).FullName,
            typeof(short).FullName,
            typeof(int).FullName,
            typeof(long).FullName,
            typeof(decimal).FullName,
        ];

        return types.Contains(propertyTypeFullName) ||
               types.Contains(propertyTypeFullName.RemoveFromStart("System.Nullable`1<").RemoveFromEnd(">"));
    }

    public static bool IsStringProperty(PropertyDefinition propertyDefinition)
    {
        return propertyDefinition.PropertyType.FullName == "System.String";
    }
}
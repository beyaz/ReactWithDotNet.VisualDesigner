using System.IO;
using Mono.Cecil;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace ReactWithDotNet.VisualDesigner;

public enum JsType
{
    String, Number, Date, Boolean, Array
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsTypeInfoAttribute : Attribute
{
    public JsTypeInfoAttribute(JsType jsType)
    {
        JsType = jsType;
    }

    public JsType JsType { get; init; }
}

static class CecilHelper
{
    public static IReadOnlyList<string> GetPropertyPathList(string assemblyPath, string typeFullName, string prefix, Func<PropertyDefinition, bool> matchFunc)
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

        var isInSameAssembly = (TypeReference typeReference) =>
        {
            if (typeReference.Scope.Name == Path.GetFileName(assemblyPath))
            {
                return true;
            }

            return false;
        };

        return findProperties(isInSameAssembly, type, prefix, matchFunc);

        static IReadOnlyList<string> findProperties(Func<TypeReference, bool> isInSameAssembly, TypeDefinition type, string prefix, Func<PropertyDefinition, bool> matchFunc)
        {
            var properties = from p in type.Properties
                             where matchFunc(p)
                             select prefix + p.Name;

            var nestedProperties = from nestedType in type.NestedTypes
                                   from nestedProperty in findProperties(isInSameAssembly, nestedType, prefix + nestedType.Name + ".", matchFunc)
                                   select nestedProperty;

            var contractProperties = from p in type.Properties
                                     where !IsSystemType(p.PropertyType) &&
                                           !IsCollection(p.PropertyType) &&
                                           isInSameAssembly(p.PropertyType)
                                     from nestedProperty in findProperties(isInSameAssembly, p.PropertyType.Resolve(), prefix + p.Name + ".", matchFunc)
                                     select nestedProperty;

            return properties.Concat(nestedProperties).Concat(contractProperties).ToList();
        }
    }

    public static bool IsBoolean(TypeReference typeReference)
    {
        var typeFullName = typeReference.FullName;

        string[] types =
        [
            typeof(bool).FullName
        ];

        return types.Contains(typeFullName) ||
               types.Contains(typeFullName.RemoveFromStart("System.Nullable`1<").RemoveFromEnd(">"));
    }

    public static bool IsBoolean(PropertyDefinition propertyDefinition)
    {
        return IsBoolean(propertyDefinition.PropertyType);
    }

    public static bool IsDateTime(TypeReference typeReference)
    {
        var typeFullName = typeReference.FullName;

        string[] types =
        [
            typeof(DateTime).FullName
        ];

        return types.Contains(typeFullName) ||
               types.Contains(typeFullName.RemoveFromStart("System.Nullable`1<").RemoveFromEnd(">"));
    }

    public static bool IsDateTime(PropertyDefinition propertyDefinition)
    {
        return IsDateTime(propertyDefinition.PropertyType);
    }

    public static bool IsNumber(TypeReference typeReference)
    {
        var typeFullName = typeReference.FullName;

        string[] types =
        [
            typeof(sbyte).FullName,
            typeof(byte).FullName,
            typeof(short).FullName,
            typeof(int).FullName,
            typeof(long).FullName,
            typeof(decimal).FullName,
        ];

        return types.Contains(typeFullName) ||
               types.Contains(typeFullName.RemoveFromStart("System.Nullable`1<").RemoveFromEnd(">"));
    }

    public static bool IsNumber(PropertyDefinition propertyDefinition)
    {
        return IsNumber(propertyDefinition.PropertyType);
    }

    public static bool IsString(TypeReference typeReference)
    {
        return typeReference.FullName == "System.String";
    }

    public static bool IsString(PropertyDefinition propertyDefinition)
    {
        return IsString(propertyDefinition.PropertyType);
    }

    public static bool IsSystemType(TypeReference typeReference)
    {
        var typeFullName = typeReference.FullName;

        string[] types =
        [
            typeof(Type).FullName
        ];

        return types.Contains(typeFullName) ||
               types.Contains(typeFullName.RemoveFromStart("System.Nullable`1<").RemoveFromEnd(">"));
    }

    static bool IsCollection(TypeReference typeReference)
    {
        return typeReference.FullName.StartsWith("System.Collections.Generic.List`1", StringComparison.OrdinalIgnoreCase);
    }
    
    public static bool IsCollection(PropertyDefinition propertyDefinition)
    {
        return IsCollection(propertyDefinition.PropertyType);
    }
}
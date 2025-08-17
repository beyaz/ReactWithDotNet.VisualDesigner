using System.IO;
using Mono.Cecil;
using TypeDefinition = Mono.Cecil.TypeDefinition;

namespace ReactWithDotNet.VisualDesigner;

public enum JsType
{
    String,
    Number,
    Date,
    Boolean,
    Array,
    Function
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
    public static PropertyDefinition FindPropertyPath(TypeDefinition typeDefinition, string propertyPath)
    {
        if (typeDefinition == null)
        {
            throw new ArgumentNullException(nameof(typeDefinition));
        }

        if (string.IsNullOrEmpty(propertyPath))
        {
            throw new ArgumentException("Property path cannot be null or empty.", nameof(propertyPath));
        }

        var properties = propertyPath.Split('.');
        var currentType = typeDefinition;

        for (var i = 0; i < properties.Length; i++)
        {
            var propertyName = properties[i];
            var property = currentType.Properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (property == null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' not found in type '{currentType.FullName}'.");
            }

            if (i < properties.Length - 1)
            {
                // Update the currentType to the property type for the next iteration
                var propertyTypeReference = property.PropertyType;

                if (!(propertyTypeReference is TypeDefinition nextType))
                {
                    nextType = propertyTypeReference.Resolve();
                }

                currentType = nextType;
            }
            else
            {
                return property;
            }
        }

        throw new InvalidOperationException("Invalid property path.");
    }

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

        return findProperties(isInSameAssembly, type, prefix, matchFunc);

        bool isInSameAssembly(TypeReference typeReference)
        {
            if (typeReference.Scope.Name == Path.GetFileName(assemblyPath))
            {
                return true;
            }

            return false;
        }

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

    public static IReadOnlyList<string> GetRemoteApiMethodNames(string assemblyPath, string requestFullName)
    {
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        if (assembly is null)
        {
            return [];
        }

        var controllerFullName = requestFullName.Replace(".Types.", ".Controllers.").Replace("ClientRequest", "Controller");

        var type = assembly.MainModule.GetType(controllerFullName);
        if (type is null)
        {
            return [];
        }

        return type.Methods.Where(isReadyToCallFromDesignerGeneratedCodes).Select(m => m.Name).ToList();

        bool isReadyToCallFromDesignerGeneratedCodes(MethodDefinition m) => m.IsPublic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == requestFullName;
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

    public static bool IsCollection(PropertyDefinition propertyDefinition)
    {
        return IsCollection(propertyDefinition.PropertyType);
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
            typeof(double).FullName,
            typeof(float).FullName,
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

    public static bool IsPropertyPathProvidedByCollection(string dotNetAssemblyFilePath, string dotnetTypeFullName, string propertyPath)
    {
        var assembly = AssemblyDefinition.ReadAssembly(dotNetAssemblyFilePath);
        if (assembly is null)
        {
            return false;
        }

        var type = assembly.MainModule.GetType(dotnetTypeFullName);
        if (type is null)
        {
            return false;
        }

        var propertyDefinition = FindPropertyPath(type, propertyPath);
        if (propertyDefinition is null)
        {
            return false;
        }

        return IsCollection(propertyDefinition.PropertyType);
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
        return typeReference.FullName.StartsWith("System.Collections.Generic.List`1", StringComparison.OrdinalIgnoreCase)||
               typeReference.FullName.StartsWith("System.Collections.Generic.IReadOnlyList`1", StringComparison.OrdinalIgnoreCase);;
    }
}
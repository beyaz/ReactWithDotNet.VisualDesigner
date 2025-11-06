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

public static class CecilHelper
{
    public static Result<PropertyDefinition> FindPropertyPath(TypeDefinition typeDefinition, string propertyPath)
    {
        if (typeDefinition == null)
        {
            return new ArgumentNullException(nameof(typeDefinition));
        }

        if (string.IsNullOrEmpty(propertyPath))
        {
            return new ArgumentException("Property path cannot be null or empty.", nameof(propertyPath));
        }

        var properties = propertyPath.Split('.');
        var currentType = typeDefinition;

        for (var i = 0; i < properties.Length; i++)
        {
            var propertyName = properties[i];
            var property = currentType.Properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (property == null)
            {
                return new InvalidOperationException($"Property '{propertyName}' not found in type '{currentType.FullName}'.");
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

        return new InvalidOperationException("Invalid property path." + propertyPath);
    }

    static Result<AssemblyDefinition> ReadAssemblyDefinition(string assemblyFilePath)
    {
        var cacheKey = $"{nameof(ReadAssemblyDefinition)}-{assemblyFilePath}";
        
        return Cache.AccessValue(cacheKey, () => CecilAssemblyReader.ReadAssembly(assemblyFilePath));
    }
    
    public static Result<IReadOnlyList<string>> GetPropertyPathList(string assemblyPath, string typeFullName, string prefix, Func<PropertyDefinition, bool> matchFunc)
    {
        
        return from assembly in ReadAssemblyDefinition(assemblyPath)
               let maybeType = assembly.MainModule.FindTypeByClrName(typeFullName)
               select maybeType.HasNoValue switch
               {
                   true => new Exception($"TypeNotFound.{typeFullName}"),
                   false => new Result<IReadOnlyList<string>>
                   {
                       Value = findProperties(isInSameAssembly, maybeType.Value, prefix, matchFunc)
                   }
               };
        

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

            return (from path in properties.Concat(nestedProperties).Concat(contractProperties) select TypescriptNaming.NormalizeBindingPath(path)).ToList();
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

    public static Result<bool> IsPropertyPathProvidedByCollection(string dotNetAssemblyFilePath, string dotnetTypeFullName, string propertyPath)
    {
        
        
        //var assembly = ReadAssemblyDefinition(dotNetAssemblyFilePath);
        //if (assembly is null)
        //{
        //    return false;
        //}

        //var type = assembly.MainModule.FindTypeByClrName(dotnetTypeFullName);
        //if (type.HasNoValue)
        //{
        //    return false;
        //}

        return
            from assembly in ReadAssemblyDefinition(dotNetAssemblyFilePath)
            from type in assembly.MainModule.FindTypeByClrName(dotnetTypeFullName)
            from propertyDefinition in FindPropertyPath(type, propertyPath)
            select IsCollection(propertyDefinition.PropertyType);
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
               typeReference.FullName.StartsWith("System.Collections.Generic.IReadOnlyList`1", StringComparison.OrdinalIgnoreCase);
    }
    
    
        public static Maybe<TypeDefinition> FindTypeByClrName(this ModuleDefinition module, string clrFullName)
        {
            // CLR format: Namespace.Outer+Inner+Inner2
            var parts = clrFullName.Split('+');
            var outerFullName = parts[0];
            var outerType = module.GetType(outerFullName);

            if (outerType == null)
                return None;

            // İç içe sınıfları sırayla ara
            TypeDefinition current = outerType;
            for (int i = 1; i < parts.Length; i++)
            {
                var nestedName = parts[i];
                current = current.NestedTypes.FirstOrDefault(t => t.Name == nestedName);
                if (current == null)
                    return None;
            }

            return current;
        }
    

}
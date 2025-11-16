using Mono.Cecil;
using Newtonsoft.Json.Serialization;


namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
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

    }
    
   

    public static void Add<T>(this List<T> collection, IEnumerable<T> items) => collection.AddRange(items);
    
    static readonly CamelCasePropertyNamesContractResolver CamelCasePropertyNamesContractResolver = new();

    public static IEnumerable<string> AppendBetween(this IEnumerable<string> enumerable, string suffix)
    {
        var list = enumerable.ToList();

        for (var i = 0; i < list.Count - 1; i++)
        {
            list[i] += suffix;
        }

        return list;
    }

    public static string GetTsVariableName(string propertyNameInCSharp)
    {
        return CamelCasePropertyNamesContractResolver.GetResolvedPropertyName(propertyNameInCSharp);
    }

    public static string RemoveFromStart(this string source, string value)
    {
        if (source.StartsWith(value))
        {
            return source.Substring(value.Length);
        }

        return source;
    }
}
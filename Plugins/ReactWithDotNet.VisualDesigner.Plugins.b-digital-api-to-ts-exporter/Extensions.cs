using Newtonsoft.Json.Serialization;

namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
    static readonly CamelCasePropertyNamesContractResolver CamelCasePropertyNamesContractResolver = new();

    public static void Add<T>(this List<T> collection, IEnumerable<T> items)
    {
        collection.AddRange(items);
    }

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

    public static string ToLowerFirstCharInvariant(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }

   

}
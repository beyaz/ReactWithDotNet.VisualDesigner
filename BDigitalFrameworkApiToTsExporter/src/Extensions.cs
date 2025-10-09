using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;

namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
    public static bool IsEqualsIgnoreWhitespace(string? a, string? b)
    {
        return ignore_whitespace_characters(a) == ignore_whitespace_characters(b);
        
        static string? ignore_whitespace_characters(string? value)
        {
            if (value == null)
            {
                return null;
            }

            return Regex.Replace(value, @"\s+", string.Empty);
        }
    }
    
    
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
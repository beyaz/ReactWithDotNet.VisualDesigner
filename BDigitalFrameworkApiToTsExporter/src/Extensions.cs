
using Newtonsoft.Json.Serialization;

namespace BDigitalFrameworkApiToTsExporter;

static class Extensions
{
    
    public static string RemoveFromStart(this string source, string value)
    {
        if (source.StartsWith(value))
        {
            return source.Substring(value.Length);
        }
        return source;
    }
    
   
        static readonly CamelCasePropertyNamesContractResolver CamelCasePropertyNamesContractResolver = new();

        public static string GetTsVariableName(string propertyNameInCSharp)
        {
            return CamelCasePropertyNamesContractResolver.GetResolvedPropertyName(propertyNameInCSharp);
        }
    

}
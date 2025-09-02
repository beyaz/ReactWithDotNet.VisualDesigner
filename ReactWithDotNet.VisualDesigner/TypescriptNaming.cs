using Newtonsoft.Json.Serialization;

namespace ReactWithDotNet.VisualDesigner;

static class TypescriptNaming
{
    static readonly CamelCasePropertyNamesContractResolver CamelCasePropertyNamesContractResolver = new();

    public static string NormalizeBindingPath(string propertyNameInCSharp)
    {
        var names =
            from name in propertyNameInCSharp.Split('.', StringSplitOptions.RemoveEmptyEntries)
            select CamelCasePropertyNamesContractResolver.GetResolvedPropertyName(name);

        return string.Join(".", names);
    }
}
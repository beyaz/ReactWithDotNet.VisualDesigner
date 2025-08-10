using Newtonsoft.Json.Serialization;

namespace ReactWithDotNet.VisualDesigner;

static class TypescriptNaming
{
    #region Static Fields
    static readonly CamelCasePropertyNamesContractResolver CamelCasePropertyNamesContractResolver = new CamelCasePropertyNamesContractResolver();
    #endregion

    #region Public Methods
    public static string GetResolvedPropertyName(string propertyNameInCSharp)
    {
        return CamelCasePropertyNamesContractResolver.GetResolvedPropertyName(propertyNameInCSharp);
    }

    public static string NormalizeBindingPath(string propertyNameInCSharp)
    {
        return string.Join(".", propertyNameInCSharp.Split('.',StringSplitOptions.RemoveEmptyEntries).ToList().ConvertAll(GetResolvedPropertyName));
    }
    #endregion
}
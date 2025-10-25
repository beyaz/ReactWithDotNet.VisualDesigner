using System.Reflection;

namespace ReactWithDotNet.VisualDesigner;

static class PluginWrapper
{
    static readonly IEnumerable<Assembly> PluginAssemblies = [typeof(Plugin).Assembly];

    static readonly IReadOnlyList<IPlugin> Plugins =
    (
        from assembly in PluginAssemblies
        from type in assembly.GetTypes()
        where type.GetInterfaces().Any(i => i == typeof(IPlugin)) && !type.IsAbstract
        select (IPlugin)Activator.CreateInstance(type)
    ).ToList();
    

    public static string AnalyzeExportFilePath(string exportFilePathForComponent)
    {
        foreach (var plugin in Plugins)
        {
            exportFilePathForComponent = plugin.AnalyzeExportFilePath(exportFilePathForComponent);
        }
        
        return exportFilePathForComponent;
    }

    public static Element TryCreateElementForPreview(TryCreateElementForPreviewInput input)
    {
        foreach (var plugin in Plugins)
        {
            var element = plugin.TryCreateElementForPreview(input);
            if (element is not null)
            {
                return element;
            }
        }
        
        return null;
    }
}
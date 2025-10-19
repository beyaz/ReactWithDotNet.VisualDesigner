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

    public static Element TryCreateElementForPreview(string tag, string id, MouseEventHandler onMouseClick)
    {
        foreach (var plugin in Plugins)
        {
            var element = plugin.TryCreateElementForPreview(tag, id, onMouseClick);
            if (element is not null)
            {
                return element;
            }
        }
        
        return null;
    }
}
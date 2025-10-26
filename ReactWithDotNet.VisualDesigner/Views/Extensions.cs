global using static  ReactWithDotNet.VisualDesigner.Views.Extensions;
using System.IO;

namespace ReactWithDotNet.VisualDesigner.Views;

static class Extensions
{
    
    public static Maybe<ComponentEntity> TryFindComponentByComponentNameWithExportFilePath(int projectId, string componentNameWithExportFilePath)
    {
        if (componentNameWithExportFilePath.HasNoValue())
        {
            return None;
        }
        
        if (componentNameWithExportFilePath.Contains(" > ",StringComparison.OrdinalIgnoreCase))
        {
            var index = componentNameWithExportFilePath.IndexOf(" > ", StringComparison.OrdinalIgnoreCase);

            var exportFilePath = componentNameWithExportFilePath.Substring(0, index);
            var componentName = componentNameWithExportFilePath.Substring(index + 3, componentNameWithExportFilePath.Length -index -3);

            var record = GetAllComponentsInProjectFromCache(projectId).FirstOrDefault(x => x.GetExportFilePath() == exportFilePath && x.GetName() == componentName);
            if (record is not null)
            {
                return record;
            }
        }
        
        if (componentNameWithExportFilePath.Contains("/",StringComparison.OrdinalIgnoreCase))
        {
            var exportFilePath = componentNameWithExportFilePath;
            var componentName = Path.GetFileNameWithoutExtension(componentNameWithExportFilePath);

            var record = GetAllComponentsInProjectFromCache(projectId).FirstOrDefault(x => x.GetExportFilePath() == exportFilePath && x.GetName() == componentName);
            if (record is not null)
            {
                return record;
            }
        }

        return None;
    }

    public static VisualElementModel AsVisualElementModel(this string rootElementAsYaml)
    {
        if (rootElementAsYaml is null)
        {
            return null;
        }

        var value = DeserializeFromYaml<VisualElementModel>(rootElementAsYaml);

        return value;
    }
    
    public static VisualElementModel FindTreeNodeByTreePath(VisualElementModel node, string path)
    {
        if (path.HasNoValue())
        {
            return null;
        }

        foreach (var index in path.Split(',').Select(int.Parse).Skip(1))
        {
            if (node.Children.Count <= index)
            {
                return null;
            }

            node = node.Children[index];
        }

        return node;
    }
    
    public static string GetDesignText(this VisualElementModel model)
    {
        var query =
            from p in model.Properties
            from v in TryParseProperty(p)
            where v.Name == Design.TextPreview
            select v.Value;

        return query.FirstOrDefault();
    }
}
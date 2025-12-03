global using static  ReactWithDotNet.VisualDesigner.Views.Extensions;

namespace ReactWithDotNet.VisualDesigner.Views;

static class Extensions
{
    public static Maybe<string> TryGetPropertyValue(this IReadOnlyList<string> properties, params string[] propertyNameWithAlias)
    {
        foreach (var property in properties)
        {
            foreach (var parsedProperty in TryParseProperty(property))
            {
                foreach (var propertyName in propertyNameWithAlias)
                {
                    if (parsedProperty.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return parsedProperty.Value;
                    }
                }
            }
        }

        return None;
    }
    
    public static Maybe<ComponentEntity> TryFindComponentByComponentNameWithExportFilePath(int projectId, string designLocation)
    {
        if (designLocation.HasNoValue)
        {
            return None;
        }
        
        var allComponentsInProject = GetAllComponentsInProjectFromCache(projectId);
            
        var record = allComponentsInProject.FirstOrDefault(x=>x.Config.ResolvedDesignLocation == designLocation);
        if (record is not null)
        {
            return record;
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
        if (node is null)
        {
            return null;
        }
        if (path.HasNoValue)
        {
            return null;
        }

        foreach (var index in path.Split(',').Select(int.Parse).Skip(1))
        {
            if (node is null)
            {
                return null;
            }
            
            if (node.Children.Count <= index)
            {
                return null;
            }

            node = node.Children[index];
        }

        return node;
    }
}
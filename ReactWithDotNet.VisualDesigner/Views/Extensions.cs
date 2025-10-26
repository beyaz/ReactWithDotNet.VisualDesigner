global using static  ReactWithDotNet.VisualDesigner.Views.Extensions;

namespace ReactWithDotNet.VisualDesigner.Views;

static class Extensions
{
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
global using static ReactWithDotNet.VisualDesigner.Models.Extensions;

namespace ReactWithDotNet.VisualDesigner.Models;

static class Extensions
{
    public static string GetText(this VisualElementModel model)
    {
        var query =
            from p in model.Properties
            from v in TryParseProperty(p)
            where v.Name == Design.Text
            select v.Value;

        return query.FirstOrDefault();
    }

    public static bool HasNoChild(this VisualElementModel model)
    {
        return model.Children is null || model.Children.Count == 0;
    }

    public static bool HasNoText(this VisualElementModel model)
    {
        return GetText(model).HasNoValue();
    }

    

    public static bool HasText(this VisualElementModel model)
    {
        return GetText(model).HasValue();
    }
}
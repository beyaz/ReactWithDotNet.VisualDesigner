global using static ReactWithDotNet.VisualDesigner.CssDomain.DesignerStyleItemFactory;

namespace ReactWithDotNet.VisualDesigner.CssDomain;

static class DesignerStyleItemFactory
{
    public static Result<DesignerStyleItem> CreateDesignerStyleItem(CreateDesignerStyleItemInput input)
    {
        var finalCssItems = input.FinalCssItems.ToList();

        if (finalCssItems.Count == 0)
        {
            return new ArgumentException("finalCssItems.Length cannot be zero");
        }

        foreach (var result in finalCssItems)
        {
            if (result.HasError)
            {
                return result.Error;
            }
        }

        return new DesignerStyleItemImp
        {
            Pseudo = input.Pseudo,

            FinalCssItems = ListFrom(from x in finalCssItems select x.Value)
        };
    }

    public sealed class CreateDesignerStyleItemInput
    {
        public IEnumerable<Result<FinalCssItem>> FinalCssItems { get; init; }

        public string Pseudo { get; init; }
    }

    class DesignerStyleItemImp : DesignerStyleItem
    {
        public IReadOnlyList<FinalCssItem> FinalCssItems { get; init; }

        public string Pseudo { get; init; }
    }
}

public interface DesignerStyleItem
{
    public IReadOnlyList<FinalCssItem> FinalCssItems { get; }

    public string Pseudo { get; }
}
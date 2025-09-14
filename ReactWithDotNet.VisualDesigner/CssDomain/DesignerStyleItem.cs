global using static ReactWithDotNet.VisualDesigner.CssDomain.DesignerStyleItemFactory;

namespace ReactWithDotNet.VisualDesigner.CssDomain;

public interface DesignerStyleItem
{
    public FinalCssItem FinalCssItem { get; }
    public IReadOnlyList<FinalCssItem> FinalCssItems { get; }

    public string Pseudo { get; }
}

static class DesignerStyleItemFactory
{
    public static Result<DesignerStyleItem> CreateDesignerStyleItem(CreateDesignerStyleItemInput input)
    {
        var finalCssItem = input.FinalCssItem;
        if (finalCssItem is not null)
        {
            return finalCssItem switch
            {
                { HasError: true } => finalCssItem.Error,

                _ => new DesignerStyleItemImp
                {
                    Pseudo = input.Pseudo,

                    FinalCssItems = [finalCssItem.Value],

                    FinalCssItem = finalCssItem.Value
                }
            };
        }

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

    class DesignerStyleItemImp : DesignerStyleItem
    {
        public FinalCssItem FinalCssItem { get; init; }

        public IReadOnlyList<FinalCssItem> FinalCssItems { get; init; }

        public string Pseudo { get; init; }
    }

    public sealed record CreateDesignerStyleItemInput
    {
        public Result<FinalCssItem> FinalCssItem { get; init; }

        public IEnumerable<Result<FinalCssItem>> FinalCssItems { get; init; }

        public string Pseudo { get; init; }
    }
}
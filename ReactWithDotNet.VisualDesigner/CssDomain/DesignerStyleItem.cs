global using static ReactWithDotNet.VisualDesigner.DesignerStyleItemFactory;

namespace ReactWithDotNet.VisualDesigner;

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

        return new DesignerStyleItem
        {
            Pseudo = input.Pseudo,

            FinalCssItems = ListFrom(from x in finalCssItems select x.Value)
        };
    }
}

public sealed class CreateDesignerStyleItemInput
{
    public IEnumerable<Result<FinalCssItem>> FinalCssItems { get; init; }

    public string Pseudo { get; init; }
}

public sealed class DesignerStyleItem
{
    internal DesignerStyleItem()
    {
    }

    public IReadOnlyList<FinalCssItem> FinalCssItems { get; init; }

    public string Pseudo { get; init; }
}
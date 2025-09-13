global using static ReactWithDotNet.VisualDesigner.DesignerStyleItemFactory;

namespace ReactWithDotNet.VisualDesigner;

static class DesignerStyleItemFactory
{
    public static DesignerStyleItem CreateDesignerStyleItem2(CreateDesignerStyleItemInput input)
    {
        var finalCssItems = input.FinalCssItems.ToList();

        if (finalCssItems.Count == 0)
        {
            throw new ArgumentException("finalCssItems.Length cannot be zero");
        }

        return new()
        {
            Pseudo = input.Pseudo,

            FinalCssItems = finalCssItems
        };
    }
}

public sealed class CreateDesignerStyleItemInput
{
    public IEnumerable<FinalCssItem> FinalCssItems { get; init; }

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
global using static ReactWithDotNet.VisualDesigner.CssDomain.DesignerStyleItemFactory;

namespace ReactWithDotNet.VisualDesigner.CssDomain;

public interface DesignerStyleItem
{
    public IReadOnlyList<FinalCssItem> FinalCssItems { get; }

    public string OriginalText { get; }

    public string Pseudo { get; }
}

static class DesignerStyleItemFactory
{
    public static Result<DesignerStyleItem> CreateDesignerStyleItem(CreateDesignerStyleItemInput input)
    {
        var finalCssItems = input.FinalCssItems.ToList();

        if (finalCssItems.Count == 0)
        {
            return new ArgumentException("finalCssItems.Length cannot be zero");
        }

        if (input.OriginalText.HasNoValue)
        {
            return new ArgumentException(nameof(input.OriginalText));
        }

        return
            from list in finalCssItems.AsResult()
            select (DesignerStyleItem)new DesignerStyleItemImp
            {
                Pseudo = input.Pseudo,

                FinalCssItems = list.AsReadOnlyList()
            };
    }

    class DesignerStyleItemImp : DesignerStyleItem
    {
        public IReadOnlyList<FinalCssItem> FinalCssItems { get; init; }

        public string OriginalText { get; init; }

        public string Pseudo { get; init; }
    }

    public sealed record CreateDesignerStyleItemInput
    {
        public IEnumerable<Result<FinalCssItem>> FinalCssItems { get; init; }

        public string OriginalText { get; init; }

        public string Pseudo { get; init; }
    }
}
global using static ReactWithDotNet.VisualDesigner.CssDomain.DesignerStyleItemFactory;

namespace ReactWithDotNet.VisualDesigner.CssDomain;

public interface DesignerStyleItem
{
    public IReadOnlyList<FinalCssItem> FinalCssItems { get; }

    public string Pseudo { get; }
    
    public string OriginalText { get; }
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

        if (input.OriginalText.HasNoValue())
        {
            return new ArgumentException(nameof(input.OriginalText));
        }

        return finalCssItems switch
        {
            _ when finalCssItems.HasError() => finalCssItems.GetError(),

            _ => new DesignerStyleItemImp
            {
                Pseudo = input.Pseudo,

                FinalCssItems = ListFrom(from x in finalCssItems select x.Value)
            }
        };
    }

    class DesignerStyleItemImp : DesignerStyleItem
    {
        public IReadOnlyList<FinalCssItem> FinalCssItems { get; init; }

        public string Pseudo { get; init; }
        
        public string OriginalText { get; init; }
    }

    public sealed record CreateDesignerStyleItemInput
    {
        public IEnumerable<Result<FinalCssItem>> FinalCssItems { get; init; }

        public string Pseudo { get; init; }
        
        public string OriginalText { get; init; }
    }
}
global using static ReactWithDotNet.VisualDesigner.DesignerStyleItemFactory;

namespace ReactWithDotNet.VisualDesigner;

static class DesignerStyleItemFactory
{
    public static DesignerStyleItem CreateDesignerStyleItem(string maybePseudo, params IReadOnlyList<FinalCssItem> finalCssItems)
    {
        if (finalCssItems.Count == 0)
        {
            throw new ArgumentException("finalCssItems.Length cannot be zero");
        }
     
        return new DesignerStyleItem
        {
            Pseudo        = maybePseudo,
            FinalCssItems = finalCssItems
        };
    }
    
    public static DesignerStyleItem CreateDesignerStyleItem2(DesignerStyleItemParameter parameter)
    {

        var finalCssItems = parameter.FinalCssItems.ToList();
        
        if (finalCssItems.Count == 0)
        {
            throw new ArgumentException("finalCssItems.Length cannot be zero");
        }
     
        return new DesignerStyleItem
        {
            Pseudo        = parameter.Pseudo,
            
            FinalCssItems = finalCssItems
        };
    }
}

public sealed class DesignerStyleItemParameter
{
    public string Pseudo { get; init; }

    public IEnumerable<FinalCssItem> FinalCssItems { get; init; }

}

public sealed class DesignerStyleItem
{
    public string Pseudo { get; init; }

    public IReadOnlyList<FinalCssItem> FinalCssItems { get; init; }

    public DesignerStyleItem()
    {
        
    }

}
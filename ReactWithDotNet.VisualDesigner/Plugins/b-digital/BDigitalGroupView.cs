namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalGroupView", Package = "b-digital-group-view")]
public sealed class BDigitalGroupView : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string title { get; set; }

    protected override Element render()
    {
        return new FlexColumn(MarginBottom(24),MarginTop(8))
        {
            title is null ? null : new div(FontSize18, FontWeight600, LineHeight32, Color(rgba(0, 0, 0, 0.87))) { title },
                    
            new FlexColumn( Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(24), Id(id), OnClick(onMouseClick))
            {
                children
            }
        };
    }

    
    

    [TryGetIconForElementTreeNode]
    public static Scope TryGetIconForElementTreeNode(Scope scope)
    {
        var model = Plugin.VisualElementModel[scope];
        
        if (model.Tag == nameof(BDigitalGroupView))
        {
            return Scope.Create(new()
            {
                { Plugin.IconForElementTreeNode, new Plugin.Icons.Panel() }
            });
        }

        return Scope.Empty;
    }
}



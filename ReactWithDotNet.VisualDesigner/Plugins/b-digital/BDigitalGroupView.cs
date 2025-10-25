using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

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
                { Plugin.IconForElementTreeNode, new IconPanel() }
            });
        }

        return Scope.Empty;
    }
}


[CustomComponent]
[Import(Name = "BIconExtended as BIcon", Package = "../utils/FormAssistant")]
public sealed class BIcon : PluginComponentBase
{
    [Suggestions("TimerRounded , content_copy")]
    [JsTypeInfo(JsType.String)]
    public string name { get; set; }

    [JsTypeInfo(JsType.String)]
    public string size { get; set; }

    protected override Element render()
    {
        return new FlexRowCentered(Size(GetSize()), Id(id), OnClick(onMouseClick))
        {
            createSvg
        };
    }

    Element createSvg()
    {
        return new DynamicMuiIcon
        {
            name     = name,
            fontSize = "medium"
        };
    }

    double GetSize()
    {
        if (size.HasValue())
        {
            if (double.TryParse(size, out var d))
            {
                return d;
            }
        }

        return 24;
    }
    
    [TryGetIconForElementTreeNode]
    public static Scope TryGetIconForElementTreeNode(Scope scope)
    {
        var model = Plugin.VisualElementModel[scope];
        
        if (model.Tag == nameof(BDigitalGroupView))
        {
            return Scope.Create(new()
            {
                { Plugin.IconForElementTreeNode, new IconImage() }
            });
        }

        return Scope.Empty;
    }
}

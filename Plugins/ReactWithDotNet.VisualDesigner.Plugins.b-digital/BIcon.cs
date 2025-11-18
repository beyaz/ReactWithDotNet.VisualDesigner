using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[TsImport(Name = "BIconExtended as BIcon", Package = "../utils/FormAssistant")]
sealed class BIcon : PluginComponentBase
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
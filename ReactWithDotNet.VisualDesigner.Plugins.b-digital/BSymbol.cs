using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BSymbol), Package = "b-icon")]
sealed class BSymbol : PluginComponentBase
{
    [Suggestions("outlined , sharp , rounded")]
    [JsTypeInfo(JsType.String)]
    public string color { get; set; }

    [Suggestions("16 , 24 , 32 , 36")]
    [JsTypeInfo(JsType.Number)]
    public string size { get; set; }

    [JsTypeInfo(JsType.String)]
    public string symbol { get; set; }

    [Suggestions("outlined , sharp , rounded")]
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }

    [Suggestions("300 , 400 , 500 , 600 , 700")]
    [JsTypeInfo(JsType.Number)]
    public string weight { get; set; }

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

    protected override Element render()
    {
        return new FlexRowCentered(Size(GetSize()), Id(id), OnClick(onMouseClick))
        {
            new span
            {
                className = "material-icons",
                style = { fontSize = "64px", color="#1976d2"},
                children = { symbol }
            }
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
}
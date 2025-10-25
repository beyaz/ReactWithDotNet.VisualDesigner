using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BDigitalGrid", Package = "b-digital-grid")]
sealed class BDigitalGrid : PluginComponentBase
{
    [Suggestions("flex-start , flex-end , stretch , center , baseline")]
    [JsTypeInfo(JsType.String)]
    public string alignItems { get; set; }

    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string container { get; set; }

    [Suggestions("column , row")]
    [JsTypeInfo(JsType.String)]
    public string direction { get; set; }

    [Suggestions("true")]
    [JsTypeInfo(JsType.Boolean)]
    public string item { get; set; }

    [Suggestions("flex-start , center , flex-end , space-between , space-around , space-evenly")]
    [JsTypeInfo(JsType.String)]
    public string justifyContent { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string lg { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string md { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string sm { get; set; }

    [Suggestions("1 , 2 , 3 , 4 , 5 , 6")]
    [JsTypeInfo(JsType.Number)]
    public string spacing { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string xl { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string xs { get; set; }

    protected override Element render()
    {
        return new Grid
        {
            children = { children },

            container      = container == null ? null : Convert.ToBoolean(container),
            item           = item == null ? null : Convert.ToBoolean(item),
            direction      = direction,
            justifyContent = justifyContent,
            alignItems     = alignItems,
            spacing        = spacing == null ? null : Convert.ToDouble(spacing),
            xs             = xs == null ? null : Convert.ToInt32(xs),
            sm             = sm == null ? null : Convert.ToInt32(sm),
            md             = md == null ? null : Convert.ToInt32(md),
            lg             = lg == null ? null : Convert.ToInt32(lg),
            xl             = xl == null ? null : Convert.ToInt32(xl),

            id      = id,
            onClick = onMouseClick
        };
    }
            
            
    [TryGetIconForElementTreeNode]
    public static Scope TryGetIconForElementTreeNode(Scope scope)
    {
        var node = Plugin.VisualElementModel[scope];
        
        if (node.Tag is nameof(BDigitalGrid))
        {
            foreach (var p in node.Properties)
            {
                foreach (var property in TryParseProperty(p))
                {
                    if (property.Name == "direction")
                    {
                        if (TryClearStringValue(property.Value).Contains("column", StringComparison.OrdinalIgnoreCase))
                        {
                            return Scope.Create(new()
                            {
                                { Plugin.IconForElementTreeNode, new IconFlexColumn() }
                            });
                                    
                        }

                        if (TryClearStringValue(property.Value).Contains("row", StringComparison.OrdinalIgnoreCase))
                        {
                            return Scope.Create(new()
                            {
                                { Plugin.IconForElementTreeNode, new IconFlexRow() }
                            });
                                    
                        }
                    }
                }
            }

            return Scope.Create(new()
            {
                { Plugin.IconForElementTreeNode, new IconFlexRow() }
            });
                    
        }

        return Scope.Empty;
    }
}
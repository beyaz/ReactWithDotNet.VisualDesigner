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

  [CustomComponent]
        [Import(Name = "BDigitalGrid", Package = "b-digital-grid")]
        public sealed class BDigitalGrid : PluginComponentBase
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

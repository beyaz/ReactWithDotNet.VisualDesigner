

using ReactWithDotNet.ThirdPartyLibraries.GoogleMaterialSymbols;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.


[CustomComponent]
sealed class leftListData : PluginComponentBase
{
    protected override Element render()
    {
        return new div
        {
            children
        };
    }
}

[CustomComponent]
sealed class rightListData : PluginComponentBase
{
    protected override Element render()
    {
        return new div
        {
            children
        };
    }
}

[CustomComponent]
sealed class BDigitalListAction : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string leftListData { get; set; }

    [JsTypeInfo(JsType.String)]
    public string lineIcon { get; set; }
    
    
    [JsTypeInfo(JsType.String)]
    public string lineIconColor { get; set; }
    

    [JsTypeInfo(JsType.Function)]
    public string onLineClick { get; set; }

    [Suggestions("0")]
    [JsTypeInfo(JsType.Number)]
    public string selectedCount { get; set; }
    
    [JsTypeInfo(JsType.Array)]
    public string action { get; set; }

    [NodeAnalyzer]
    public static async NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalListAction))
        {
            return await AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;
        
        TsImportCollection tsImports = new();

        foreach (var name in new[] { "leftListData", "rightListData" })
        {
            var listData = node.FindNodeByTag(name);
            if (listData is null)
            {
                continue;
            }

            
            IReadOnlyList<string> linesCollection;
            {
                var response = await
                (
                    from child in listData.Children
                    from nodeAnalyzeOutput in input.AnalyzeNode(child)
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(nodeAnalyzeOutput.Node)
                    let childAsTsxLines = string.Join(Environment.NewLine, lines)
                    select (tsx: childAsTsxLines, tsImports: nodeAnalyzeOutput.TsImportCollection)
                ).AsResult();

                if (response.HasError)
                {
                    return response.Error;
                }

                linesCollection = new List<string>{ from x in response.Value select x.tsx};
                
                tsImports.Add(from x in response.Value select x.tsImports);
            }

            var items = string.Join("," + Environment.NewLine, linesCollection);

            var property = new ReactProperty
            {
                Name  = name,
                Value = "[" + items + "]"
            };

            node = node with
            {
                Properties = node.Properties.Add(property)
            };
        }

        node = node with
        {
            Children = []
        };
        
        return (node, new()
        {
            { nameof(BDigitalListAction), "b-digital-list-action" },
            tsImports
        });
        
        
    }

    protected override Element render()
    {
        var iconName = lineIcon switch
        {
            "ChevronRight" => "chevron_right",
            
            null => "more_vert",
            
            _   => lineIcon
        };
        
        
        var moreIcon = action.HasValue && selectedCount == "0" || lineIcon.HasValue
            ? new FlexRowCentered(Width(24))
            {
                new MaterialSymbol
                {
                    name         =  iconName,
                    size         = 24,
                    styleVariant = MaterialSymbolVariant.outlined,
                    color        = "#16A085"
                }
            }
            : null;
        
        

        var leftListDataContainer = children.FindElementByElementType(typeof(leftListData));
        
        var rightListDataContainer = children.FindElementByElementType(typeof(rightListData));
        
        return new Fragment
        {
            new FlexRow(IsNotMobile(DisplayNone), JustifyContentSpaceBetween, AlignItemsCenter)
            {
                Id(id),
                OnClick(onMouseClick),
            
                new FlexColumn(AlignItemsFlexStart)
                {
                    leftListDataContainer?.children,
                    
                    rightListDataContainer?.children
                }
                ,
            
                new FlexRowCentered(Size(64))
                {
                    moreIcon
                }
           
            },

            new FlexRow(IsMobile(DisplayNone), AlignItemsCenter, JustifyContentSpaceBetween)
            {
                Id(id),
                OnClick(onMouseClick),

                new FlexColumn(JustifyContentFlexStart)
                {
                    leftListDataContainer?.children
                },

                new FlexRow(Gap(8))
                {
                    new FlexColumn(AlignItemsFlexEnd)
                    {
                        rightListDataContainer?.children,
                    },

                    moreIcon
                }

            }
        };
    }
}
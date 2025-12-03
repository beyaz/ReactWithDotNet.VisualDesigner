namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.


[CustomComponent]
sealed class BTab : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string text { get; set; }
    
    [JsTypeInfo(JsType.Number)]
    public string value { get; set; }
    
    protected override Element render()
    {
        return new div
        {
            children
        };
    }
}


[CustomComponent]
sealed class BTabBar : PluginComponentBase
{
    [Suggestions("secondary , primary")]
    [JsTypeInfo(JsType.String)]
    public string mode { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }

    [JsTypeInfo(JsType.Array)]
    public string tabItems { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string value { get; set; }

    [NodeAnalyzer]
    public static async NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BTabBar))
        {
            return await AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;
        
        TsImportCollection tsImportCollection = new();

        if (node.Properties.All(p => p.Name != nameof(tabItems)))
        {
            var tabsResult = await
            (
                from x in node.Children
                from tabItem in ConvertToTabItem(x)
                select tabItem
            ).AsResult();

            if (tabsResult.HasError)
            {
                return tabsResult.Error;
            }

            var tabs = tabsResult.Value;

            var property = new ReactProperty
            {
                Name  = nameof(tabItems),
                Value = "[" + string.Join("," + Environment.NewLine, from x in tabs select x.tsx) + "]"
            };

            node = node with
            {
                Properties = node.Properties.Add(property)
            };
            
            tsImportCollection.Add(from x in tabs select x.tsImports);
        }

        node = node with
        {
            Children = []
        };

        return Result.From((node, new TsImportCollection
        {
            {nameof(BTabBar),"b-tab-bar"},
            tsImportCollection
        }));
        
        async Task<Result<(string tsx, TsImportCollection tsImports )>> ConvertToTabItem(ReactNode childNode)
        {
            List<string> lineList = [];
            
            TsImportCollection tsImports = new();

            var textProperty = childNode.Properties.FirstOrDefault(p => p.Name == nameof(BTab.text));

            var valueProperty = childNode.Properties.FirstOrDefault(p => p.Name == nameof(BTab.value));

            string content;

            if (childNode.Children.Count == 0)
            {
                content = "<></>";
            }
            else if (childNode.Children.Count == 1)
            {
                var response = await
                (
                    from nodeAnalyzeOutput in input.AnalyzeNode(childNode.Children[0])
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(nodeAnalyzeOutput.Node)
                    let childAsTsxLines = string.Join(Environment.NewLine, lines)
                    select (tsx: childAsTsxLines, tsImports: nodeAnalyzeOutput.TsImportCollection)
                );

                if (response.HasError)
                {
                    return response.Error;
                }

                content = response.Value.tsx;
                
                tsImports.Add(response.Value.tsImports);
            }
            else
            {
                var response = await
                (
                    from x in childNode.Children
                    from nodeAnalyzeOutput in input.AnalyzeNode(x)
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(nodeAnalyzeOutput.Node)
                    let childAsTsxLines = string.Join(Environment.NewLine, lines)
                    select (tsx: childAsTsxLines, tsImports: nodeAnalyzeOutput.TsImportCollection)
                ).AsResult();

                if (response.HasError)
                {
                    return response.Error;
                }
                
              
                
                

                content = string.Join(Environment.NewLine, from x in response.Value select x.tsx);

                content = $"<>{content}</>";
                
                tsImports.Add(from x in response.Value select x.tsImports);
            }

            lineList.Add("{");
            lineList.Add("text:" + (textProperty?.Value ?? "''") + ",");
            lineList.Add("value:" + (valueProperty?.Value ?? "''") + ",");

            lineList.Add("content:" + content);
            lineList.Add("}");

            return (tsx: string.Join(Environment.NewLine, lineList) , tsImports);
        }
    }

    protected override Element render()
    {
        return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexRow(BorderBottom("1px solid rgb(189, 189, 189)"))
            {
                from tab in children.Select((el, index) => new { el, index })
                where ((BTab)tab.el).text.HasValue
                select new FlexRowCentered(MarginX(24), Height(30))
                {
                    TryClearStringValue(((BTab)tab.el).text),

                    Color("#16A085"),

                    BorderBottom(2, solid, "#16A085"),

                    FontWeight500, FontSize13
                }
            },

            children
        };
    }
}
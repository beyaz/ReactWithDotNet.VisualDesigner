using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalListAction : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string leftListData { get; set; }

    [JsTypeInfo(JsType.String)]
    public string lineIcon { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onLineClick { get; set; }

    [Suggestions("0")]
    [JsTypeInfo(JsType.Number)]
    public string selectedCount { get; set; }

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
            var listData = node.TryFindDesignNamedNode(name);
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
        return new FlexRow(JustifyContentSpaceBetween)
        {
            Id(id),
            OnClick(onMouseClick),
            
            // leftListData
            new FlexColumn
            {
                children.Count > 0 ? children[0].children : null
            },

            // rightListData
            new FlexColumn
            {
                children.Count > 1 ? children[1].children : null
            }
        };
    }
}
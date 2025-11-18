using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[TsImport(Name = nameof(BDigitalListAction), Package = "b-digital-list-action")]
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
                    from analyzedChild in input.AnalyzeNode(child)
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(analyzedChild)
                    let childAsTsxLines = string.Join(Environment.NewLine, lines)
                    select childAsTsxLines
                ).AsResult();

                if (response.HasError)
                {
                    return response.Error;
                }

                linesCollection = response.Value;
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

        return node with
        {
            Children = []
        };
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
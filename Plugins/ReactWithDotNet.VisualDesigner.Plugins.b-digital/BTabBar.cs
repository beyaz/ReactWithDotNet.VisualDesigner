using Azure;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[TsImport(Name = nameof(BTabBar), Package = "b-tab-bar")]
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
        
        if (node.Properties.All(p => p.Name != nameof(tabItems)))
        {
            var tabItemTexts = await
            (
                from x in node.Children
                from tabItem in ConvertToTabItem(x)
                select tabItem
            ).AsResult();

            if (tabItemTexts.HasError)
            {
                return tabItemTexts.Error;
            }

            var property = new ReactProperty
            {
                Name  = nameof(tabItems),
                Value = "[" + string.Join("," + Environment.NewLine, tabItemTexts.Value) + "]"
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

        return Result.From((node, new TsImportCollection
        {
            {nameof(BTabBar),"b-tab-bar"}
        }));
        
        async Task<Result<string>> ConvertToTabItem(ReactNode childNode)
        {
            List<string> lineList = [];

            var textProperty = childNode.Properties.FirstOrDefault(p => p.Name == "data-text");

            var valueProperty = childNode.Properties.FirstOrDefault(p => p.Name == "data-value");

            string content;

            if (childNode.Children.Count == 0)
            {
                content = "<></>";
            }
            else if (childNode.Children.Count == 1)
            {
                var response = await
                (
                    from analyzedChild in input.AnalyzeNode(childNode.Children[0])
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(analyzedChild)
                    let childAsTsxLines = string.Join(Environment.NewLine, lines)
                    select childAsTsxLines
                );

                if (response.HasError)
                {
                    return response.Error;
                }

                content = response.Value;
            }
            else
            {
                var response = await
                (
                    from x in childNode.Children
                    from analyzedChild in input.AnalyzeNode(x)
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(analyzedChild)
                    let childAsTsxLines = string.Join(Environment.NewLine, lines)
                    select childAsTsxLines
                ).AsResult();

                if (response.HasError)
                {
                    return response.Error;
                }

                content = string.Join(Environment.NewLine, response.Value);

                content = $"<>{content}</>";
            }

            lineList.Add("{");
            lineList.Add("text:" + (textProperty?.Value ?? "''") + ",");
            lineList.Add("value:" + (valueProperty?.Value ?? "''") + ",");

            lineList.Add("content:" + content);
            lineList.Add("}");

            return string.Join(Environment.NewLine, lineList);
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
                where ((HtmlElement)tab.el).data.ContainsKey("text")
                select new FlexRowCentered(MarginX(24), Height(30))
                {
                    TryClearStringValue(((HtmlElement)tab.el).data["text"]),

                    Color("#16A085"),

                    BorderBottom(2, solid, "#16A085"),

                    FontWeight500, FontSize13
                }
            },

            children
        };
    }
}
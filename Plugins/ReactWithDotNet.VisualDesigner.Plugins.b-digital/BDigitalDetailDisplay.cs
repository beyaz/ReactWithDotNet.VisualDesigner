namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
class BDigitalDetailDisplay : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string items { get; set; }

    record DetailDisplayItem
    {
        public string text { get; init; }
        
        public string value { get; init; }
    }

    
    
    [NodeAnalyzer]
    public static async NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalDetailDisplay))
        {
            return await AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;

        if (node.Children.Count > 0)
        {
            List<string> lines = [];

            for (var i = 0; i < node.Children.Count; i += 2)
            {
                var textContent = TryGetPropFinalText(node.TryGetNodeItemAt([i]), Design.Content, input.ComponentConfig);
                var valueContent = TryGetPropFinalText(node.TryGetNodeItemAt([i + 1]), Design.Content,input.ComponentConfig);

                if (textContent.HasValue && valueContent.HasValue)
                {
                    lines.Add($"{{ text: {textContent}, value: {valueContent} }}");
                }
            }

            var items = string.Join("," + Environment.NewLine, lines);

            var property = new ReactProperty
            {
                Name  = "items",
                Value = "[" + items + "]"
            };

            node = node with
            {
                Properties = node.Properties.Add(property),
                Children = []
            };
        }

        var import = (nameof(BDigitalDetailDisplay), "b-digital-detail-display");

        return await AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);

    }

    protected override Element render()
    {
        if (children.Count % 2 != 0)
        {
            return null;
        }

        var newChildren = new List<Element>();

        for (var i = 0; i < children.Count; i += 2)
        {
            newChildren.Add(new FlexRow(AlignItemsCenter)
            {
                children[i],
                new BTypography
                {
                    variant = "body2",

                    color = "textSecondary",

                    children =
                    {
                        ":"
                    }
                } + PaddingRight(4),
                children[i + 1]
            });
        }

        return new FlexColumn(Gap(8))
        {
            Id(id), OnClick(onMouseClick),

            newChildren
        };
    }

}
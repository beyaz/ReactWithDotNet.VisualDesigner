namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalDialog : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string displayCloseIcon { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string displayOkButton { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string fullScreen { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onClose { get; set; }

    [JsTypeInfo(JsType.Boolean)]
    public string open { get; set; }

    [JsTypeInfo(JsType.String)]
    public string title { get; set; }

    [Suggestions("error , warning , info , success")]
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalDialog))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        
        var (node, componentConfig) = input;

        var content = node.TryFindDesignNamedNode("content");

        var actions = node.TryFindDesignNamedNode("actions");

        if (content is not null && actions is not null)
        {
            var actionsProperty = new ReactProperty
            {
                Name = "actions",
                Value = "[" +
                        string.Join(", ",
                            from actionButton in actions.Children
                            let label = actionButton.Properties.First(x => x.Name == nameof(BButton.text)).Value
                            let onClick = actionButton.Properties.First(x => x.Name == nameof(BButton.onClick)).Value
                            select $"{{ label: {label}, onClick: {onClick} }}") +
                        "]"
            };

            node = node with
            {
                Children = [content],
                Properties = node.Properties.Add(actionsProperty)
            };
        }

        var import = (nameof(BDigitalDialog), "b-digital-dialog");
   
        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        return new div(Background(rgba(0, 0, 0, 0.5)), Padding(24), BorderRadius(8))
        {
            Id(id), OnClick(onMouseClick),
            new div(Background("white"), BorderRadius(8), Padding(16))
            {
                // T o p   B a r
                title.HasNoValue()
                    ? null
                    : new FlexRow(JustifyContentSpaceBetween, AlignItemsCenter, PaddingY(16))
                    {
                        // t i t l e
                        new div(FontSize20, FontWeight400, LineHeight("160%"), LetterSpacing("0.15px")) { title },

                        // c l o s e   i c o n
                        displayCloseIcon == "false" || displayOkButton == "false"
                            ? null
                            : new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24))
                            {
                                new path
                                {
                                    d = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"
                                }
                            }
                    },

                title.HasNoValue() ? null : SpaceY(12),

                children
            }
        };
    }
}
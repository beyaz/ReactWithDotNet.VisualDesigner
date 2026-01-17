namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.

[CustomComponent]
sealed class actions : PluginComponentBase
{
    internal bool isBorderTopNone;

    protected override Element render()
    {
        return new FlexRow(Padding(8), Gap(8), JustifyContentFlexEnd, BorderTop(1, solid, rgba(0, 0, 0, 0.12)), isBorderTopNone ? BorderTop(none) : null)
        {
            children
        };
    }
}

[CustomComponent]
sealed class BDigitalDialog : PluginComponentBase
{
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string displayCloseIcon { get; set; }

    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string displayOkButton { get; set; }

    [Suggestions("true , false")]
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
    public static async NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalDialog))
        {
            return await AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        input = ApplyTranslateOperationOnProps(input, nameof(title));

        var node = input.Node;

        TsImportCollection tsImports = new();

        var actionsNode = node.FindNodeByTag(nameof(actions));

        if (actionsNode is not null)
        {
            node = node with
            {
                Children = node.Children.Remove(actionsNode)
            };

            IReadOnlyList<string> linesCollection;
            {
                var response = await
                (
                    from child in actionsNode.Children
                    from nodeAnalyzeOutput in input.AnalyzeNode(child)
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(nodeAnalyzeOutput.Node)
                    let childAsTsxLines = string.Join(Environment.NewLine, lines)
                    select (tsx: childAsTsxLines, tsImports: nodeAnalyzeOutput.TsImportCollection)
                ).AsResult();

                if (response.HasError)
                {
                    return response.Error;
                }

                linesCollection = new List<string> { from x in response.Value select "{ element: " + x.tsx + ", label: null, onClick: null}" };

                tsImports.Add(from x in response.Value select x.tsImports);
            }

            var items = string.Join("," + Environment.NewLine, linesCollection);

            var property = new ReactProperty
            {
                Name  = "actions",
                Value = "[" + items + "]"
            };

            node = node with
            {
                Properties = node.Properties.Add(property)
            };
        }

        var import = (nameof(BDigitalDialog), "b-digital-dialog");

        return await AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import).With(tsImports);
    }

    protected override Element render()
    {
        Element bottomActionBar = null;

        // arrange actions
        {
            var actionsElement = (actions)children.FindElementByElementType(typeof(actions));
            if (actionsElement is not null)
            {
                children.Remove(actionsElement);

                bottomActionBar = actionsElement;

                if (displayOkButton?.Equals("false") is true)
                {
                    actionsElement.isBorderTopNone = true;
                }
            }
        }

        Element partTop = new div();

        if (title.HasValue)
        {
            partTop.Add(new FlexRow(AlignItemsCenter, PaddingY(12), PaddingX(16))
            {
                // c l o s e   i c o n
                displayCloseIcon == "false" || displayOkButton == "false"
                    ? null
                    : new FlexRowCentered(Padding(12))
                    {
                        new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Color(rgba(0, 0, 0, 0.54)))
                        {
                            new path
                            {
                                d = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"
                            }
                        }
                    },

                // t i t l e
                new div(FontSize("1.5rem"), FontWeight400, LineHeight(1.334), LetterSpacing("0.15px")) { title }
            });

            partTop.Add(new BDivider { Margin(0) });
        }

        return new div(Background(rgba(0, 0, 0, 0.4)), Margin(4), Padding(24), BorderRadius(8))
        {
            Id(id), OnClick(onMouseClick),

            new div(Background("white"), BorderRadius(8))
            {
                partTop,

                new div(Padding(12))
                {
                    children
                },

                bottomActionBar
            }
        };
    }
}
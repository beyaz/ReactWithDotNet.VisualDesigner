using Newtonsoft.Json;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
class BDigitalDetailDisplay : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string items { get; set; }

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
                var textNode = node.TryGetNodeItemAt([i]);

                var valueNode = node.TryGetNodeItemAt([i + 1]);

                var props = new List<string>();
                {
                    var textContent = TryGetPropFinalText(textNode, Design.Content, input.ComponentConfig);
                    if (textContent.HasValue)
                    {
                        props.Add("text: " + textContent);
                    }

                    var textVariant = TryGetPropValueByPropName(textNode, nameof(BTypography.variant));
                    if (textVariant.HasValue)
                    {
                        props.Add("textVariant: " + textVariant);
                    }

                    var valueContent = TryGetPropFinalText(valueNode, Design.Content, input.ComponentConfig);
                    if (valueContent.HasValue)
                    {
                        props.Add("value: " + valueContent);
                    }

                    var valueVariant = TryGetPropValueByPropName(valueNode, nameof(BTypography.variant));
                    if (valueVariant.HasValue)
                    {
                        props.Add("valueVariant: " + valueVariant);
                    }

                    string color = null;
                    {
                        // try get from typography
                        {
                            var valueColor = TryGetPropValueByPropName(valueNode, nameof(BTypography.color));
                            if (valueColor.HasValue)
                            {
                                Result<string> cssColor = valueColor switch
                                {
                                    "\"primary\""       => "theme.palette.primary.main",
                                    "\"secondary\""     => "theme.palette.secondary.main",
                                    "\"textPrimary\""   => "theme.palette.text.primary",
                                    "\"textSecondary\"" => "theme.palette.text.secondary",
                                    _                   => new Exception(valueColor)
                                };

                                if (cssColor.HasError)
                                {
                                    return cssColor.Error;
                                }

                                color = cssColor.Value;
                            }
                        }

                        // try get from style
                        {
                            var styleAsJson = TryGetPropValueByPropName(valueNode, "style");
                            if (styleAsJson.HasValue)
                            {
                                var map = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, string>>(styleAsJson);
                                if (map.TryGetValue(nameof(Style.color), out var colorValue))
                                {
                                    color = colorValue;
                                }
                            }
                        }
                    }
                    
                    if (color.HasValue)
                    {
                        props.Add("color: " + color);
                    }
                    
                }

                if (props.Count > 0)
                {
                    lines.Add("{ " + string.Join(", ", props) + " }");
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
            var label = children[i];
            {
                if (label is BTypography bTypography)
                {
                    if (bTypography.variant.HasNoValue)
                    {
                        bTypography.variant = "body2";
                    }

                    if (bTypography.color.HasNoValue)
                    {
                        bTypography.color = "textSecondary";
                    }
                }
            }

            var value = children[i + 1];
            {
                if (value is BTypography bTypography)
                {
                    if (bTypography.variant.HasNoValue)
                    {
                        bTypography.variant = "body1";
                    }

                    if (bTypography.color.HasNoValue)
                    {
                        bTypography.color = "textPrimary";
                    }
                }
            }

            newChildren.Add(new FlexRow(AlignItemsCenter)
            {
                label,
                new BTypography
                {
                    variant = "body2",

                    color = "textSecondary",

                    children =
                    {
                        ":"
                    }
                } + PaddingRight(4),
                value
            });
        }

        return new FlexColumn(Gap(8))
        {
            Id(id), OnClick(onMouseClick),

            newChildren
        };
    }
}
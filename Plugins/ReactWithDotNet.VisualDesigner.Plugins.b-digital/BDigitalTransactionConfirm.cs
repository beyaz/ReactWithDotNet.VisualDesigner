using System.Collections.Immutable;

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

class PropertyTag_FlexColumn : PluginComponentBase
{
    protected override Element render()
    {
        return new FlexColumn
        {
            children
        };
    }
}

class PropertyTag_FlexRow : PluginComponentBase
{
    public virtual int Gap => 0;

    protected override Element render()
    {
        return new FlexRow(Gap(Gap), AlignItemsCenter)
        {
            children
        };
    }
}

[CustomComponent]
sealed class sender : PropertyTag_FlexColumn;

[CustomComponent]
sealed class title : PropertyTag_FlexColumn;

[CustomComponent]
sealed class amount : PropertyTag_FlexColumn;

[CustomComponent]
sealed class transactionDetailList : PropertyTag_FlexColumn;

[CustomComponent]
sealed class receiver : PropertyTag_FlexColumn;

[CustomComponent]
sealed class item1 : PropertyTag_FlexRow
{
    public override int Gap => 4;
}

[CustomComponent]
sealed class item2 : PropertyTag_FlexRow
{
    public override int Gap => 4;
}

[CustomComponent]
sealed class item3 : PropertyTag_FlexRow
{
    public override int Gap => 4;
}

[CustomComponent]
sealed class item4 : PropertyTag_FlexRow
{
    public override int Gap => 4;
}
[CustomComponent]
sealed class item5 : PropertyTag_FlexRow
{
    public override int Gap => 4;
}


[CustomComponent]
sealed class item : PropertyTag_FlexRow;

[CustomComponent]
sealed class BDigitalTransactionConfirm : PluginComponentBase
{
    // @formatter:on

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalTransactionConfirm))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;

        List<ReactProperty> finalProps = [];

        string TryGetPropFinalText(ReactNode reactNode, string propName)
        {
            var tempNode = ApplyTranslateOperationOnProps(reactNode, input.ComponentConfig, propName);

            return TryGetPropValueByPropName(tempNode, propName);
        }

        // sender
        {
            List<string> lines = [];

            var senderNode = node.FindNodeByTag(nameof(b_digital.sender));

            var sender = new
            {
                title = senderNode.FindNodeByTag(nameof(title)).TryGetNodeItemAt([0]),

                item1Text  = senderNode.FindNodeByTag(nameof(b_digital.item1)).TryGetNodeItemAt([0]),
                item1Value = senderNode.FindNodeByTag(nameof(b_digital.item1)).TryGetNodeItemAt([1]),

                item2Text  = senderNode.FindNodeByTag(nameof(b_digital.item2)).TryGetNodeItemAt([0]),
                item2Value = senderNode.FindNodeByTag(nameof(b_digital.item2)).TryGetNodeItemAt([1]),

                item3Text  = senderNode.FindNodeByTag(nameof(b_digital.item3)).TryGetNodeItemAt([0]),
                item3Value = senderNode.FindNodeByTag(nameof(b_digital.item3)).TryGetNodeItemAt([1]),
                
                item4Text  = senderNode.FindNodeByTag(nameof(b_digital.item4)).TryGetNodeItemAt([0]),
                item4Value = senderNode.FindNodeByTag(nameof(b_digital.item4)).TryGetNodeItemAt([1]),
                
                item5Text  = senderNode.FindNodeByTag(nameof(b_digital.item5)).TryGetNodeItemAt([0]),
                item5Value = senderNode.FindNodeByTag(nameof(b_digital.item5)).TryGetNodeItemAt([1]),
            };

            var titleNode = sender.title;
            if (titleNode is not null)
            {
                var titleText = TryGetPropFinalText(titleNode, Design.Content);
                if (titleText is not null)
                {
                    lines.Add($"titleText: {titleText}");
                }
                
                var variant = TryGetPropValueByPropName(titleNode, nameof(BTypography.variant));
                if (variant.HasValue)
                {
                    lines.Add($"titleTextVariant: {variant}");
                }

                var color = TryGetPropValueByPropName(titleNode, nameof(BTypography.color));
                if (color.HasValue)
                {
                    lines.Add($"titleColorVariant: {color}");
                }
            }

            var item1 = getItem(sender.item1Text, sender.item1Value);
            if (item1 is not null)
            {
                lines.Add("item1: " + item1);
            }

            var item2 = getItem(sender.item2Text, sender.item2Value);
            if (item2 is not null)
            {
                lines.Add("item2: " + item2);
            }

            var item3 = getItem(sender.item3Text, sender.item3Value);
            if (item3 is not null)
            {
                lines.Add("item3: " + item3);
            }
            
            var item4 = getItem(sender.item4Text, sender.item4Value);
            if (item4 is not null)
            {
                lines.Add("item4: " + item4);
            }
            
            var item5 = getItem(sender.item5Text, sender.item5Value);
            if (item5 is not null)
            {
                lines.Add("item5: " + item5);
            }

            if (lines.Count > 0)
            {
                var prop = new ReactProperty
                {
                    Name  = "senderData",
                    Value = '{' + string.Join(",", lines) + '}'
                };

                finalProps.Add(prop);
            }
        }

        // amount
        {
            var amountNode = node.FindNodeByTag(nameof(amount));

            var transferAmountNode = amountNode.TryGetNodeItemAt([0]);
            if (transferAmountNode is not null)
            {
                var transferAmount = TryGetPropValueByPropName(transferAmountNode, Design.Content);
                if (transferAmount.HasValue)
                {
                    finalProps.Add(new ReactProperty
                    {
                        Name  = "transferAmount",
                        Value = transferAmount
                    });
                }
                
                var variant = TryGetPropValueByPropName(transferAmountNode, nameof(BTypography.variant));
                if (variant.HasValue)
                {
                    finalProps.Add(new ReactProperty
                    {
                        Name  = "transferAmountVariant",
                        Value = variant
                    });
                }

                var color = TryGetPropValueByPropName(transferAmountNode, nameof(BTypography.color));
                if (color.HasValue)
                {
                    finalProps.Add(new ReactProperty
                    {
                        Name  = "transferAmountColorVariant",
                        Value = color
                    });
                }
            }
        }

        // receiver
        {
            var receiverNode = node.FindNodeByTag(nameof(b_digital.receiver));

            List<string> lines = [];

            var receiver = new
            {
                title = receiverNode.FindNodeByTag(nameof(title)).TryGetNodeItemAt([0]),

                item1Text  = receiverNode.FindNodeByTag(nameof(b_digital.item1)).TryGetNodeItemAt([0]),
                item1Value = receiverNode.FindNodeByTag(nameof(b_digital.item1)).TryGetNodeItemAt([1]),

                item2Text  = receiverNode.FindNodeByTag(nameof(b_digital.item2)).TryGetNodeItemAt([0]),
                item2Value = receiverNode.FindNodeByTag(nameof(b_digital.item2)).TryGetNodeItemAt([1]),

                item3Text  = receiverNode.FindNodeByTag(nameof(b_digital.item3)).TryGetNodeItemAt([0]),
                item3Value = receiverNode.FindNodeByTag(nameof(b_digital.item3)).TryGetNodeItemAt([1]),
                
                item4Text  = receiverNode.FindNodeByTag(nameof(b_digital.item4)).TryGetNodeItemAt([0]),
                item4Value = receiverNode.FindNodeByTag(nameof(b_digital.item4)).TryGetNodeItemAt([1]),
                
                item5Text  = receiverNode.FindNodeByTag(nameof(b_digital.item5)).TryGetNodeItemAt([0]),
                item5Value = receiverNode.FindNodeByTag(nameof(b_digital.item5)).TryGetNodeItemAt([1]),
            };

            var titleNode = receiver.title;
            if (titleNode is not null)
            {
                var titleText = TryGetPropFinalText(titleNode, Design.Content);
                if (titleText is not null)
                {
                    lines.Add($"titleText: {titleText}");
                }
                
                
                var variant = TryGetPropValueByPropName(titleNode, nameof(BTypography.variant));
                if (variant.HasValue)
                {
                    lines.Add($"titleTextVariant: {variant}");
                }

                var color = TryGetPropValueByPropName(titleNode, nameof(BTypography.color));
                if (color.HasValue)
                {
                    lines.Add($"titleColorVariant: {color}");
                }
            }
            
            var item1 = getItem(receiver.item1Text, receiver.item1Value);
            if (item1 is not null)
            {
                lines.Add("item1: " + item1);
            }

            var item2 = getItem(receiver.item2Text, receiver.item2Value);
            if (item2 is not null)
            {
                lines.Add("item2: " + item2);
            }

            var item3 = getItem(receiver.item3Text, receiver.item3Value);
            if (item3 is not null)
            {
                lines.Add("item3: " + item3);
            }
            
            var item4 = getItem(receiver.item4Text, receiver.item4Value);
            if (item4 is not null)
            {
                lines.Add("item4: " + item4);
            }
            
            var item5 = getItem(receiver.item5Text, receiver.item5Value);
            if (item5 is not null)
            {
                lines.Add("item5: " + item5);
            }

            if (lines.Count > 0)
            {
                var prop = new ReactProperty
                {
                    Name  = "receiverData",
                    Value = '{' + string.Join(",", lines) + '}'
                };

                finalProps.Add(prop);
            }
        }

        // transactionDetailLines
        {
            List<string> lines = [];

            var transactionDetailListNode = node.FindNodeByTag(nameof(transactionDetailList));

            var items = new
            {
                item1Text  = transactionDetailListNode.TryGetNodeItemAt([0]),
                item1Value = transactionDetailListNode.TryGetNodeItemAt([1]),

                item2Text  = transactionDetailListNode.TryGetNodeItemAt([0]),
                item2Value = transactionDetailListNode.TryGetNodeItemAt([1]),

                item3Text  = transactionDetailListNode.TryGetNodeItemAt([0]),
                item3Value = transactionDetailListNode.TryGetNodeItemAt([1]),
                
                item4Text  = transactionDetailListNode.TryGetNodeItemAt([0]),
                item4Value = transactionDetailListNode.TryGetNodeItemAt([1]),
                
                item5Text  = transactionDetailListNode.TryGetNodeItemAt([0]),
                item5Value = transactionDetailListNode.TryGetNodeItemAt([1]),
                
           
            };

            var item1 = getItem(items.item1Text, items.item1Value);
            if (item1 is not null)
            {
                lines.Add(item1);
            }

            var item2 = getItem(items.item2Text, items.item2Value);
            if (item2 is not null)
            {
                lines.Add(item2);
            }

            var item3 = getItem(items.item3Text, items.item3Value);
            if (item3 is not null)
            {
                lines.Add(item3);
            }
            
            var item4 = getItem(items.item4Text, items.item4Value);
            if (item4 is not null)
            {
                lines.Add(item4);
            }
            
            var item5 = getItem(items.item5Text, items.item5Value);
            if (item5 is not null)
            {
                lines.Add(item5);
            }

            finalProps.Add(new ReactProperty
            {
                Name  = "transactionDetailList",
                Value = '[' + string.Join(",", lines) + ']'
            });
        }

        node = node with
        {
            Children = [],
            Properties = finalProps.ToImmutableList()
        };

        return Result.From((node, new TsImportCollection
        {
            { nameof(BDigitalTransactionConfirm), "b-digital-transaction-confirm" }
        }));

        string getItem(ReactNode textNode, ReactNode valueNode)
        {
            List<string> returnList = [];

            if (valueNode is null)
            {
                if (textNode is null)
                {
                    return null;
                }

                var value = TryGetPropFinalText(textNode, Design.Content);
                if (value.HasValue)
                {
                    returnList.Add($"value: {value}");
                }

                var valueVariant = TryGetPropValueByPropName(textNode, nameof(BTypography.variant));
                if (valueVariant.HasValue)
                {
                    returnList.Add($"valueVariant: {valueVariant}");
                }

                var valueColor = TryGetPropValueByPropName(textNode, nameof(BTypography.color));
                if (valueColor.HasValue)
                {
                    returnList.Add($"valueColor: {valueColor}");
                }

                return '{' + string.Join(",", returnList) + '}';
            }

            {
                var text = TryGetPropFinalText(textNode, Design.Content);
                if (text.HasValue)
                {
                    returnList.Add($"text: {text}");
                }

                var textVariant = TryGetPropValueByPropName(textNode, nameof(BTypography.variant));
                if (textVariant.HasValue)
                {
                    returnList.Add($"textVariant: {textVariant}");
                }

                var textColor = TryGetPropValueByPropName(textNode, nameof(BTypography.color));
                if (textColor.HasValue)
                {
                    returnList.Add($"textColor: {textColor}");
                }

                var value = TryGetPropFinalText(valueNode, Design.Content);
                if (value.HasValue)
                {
                    returnList.Add($"value: {value}");
                }

                var valueVariant = TryGetPropValueByPropName(valueNode, nameof(BTypography.variant));
                if (valueVariant.HasValue)
                {
                    returnList.Add($"valueVariant: {valueVariant}");
                }

                var valueColor = TryGetPropValueByPropName(valueNode, nameof(BTypography.color));
                if (valueColor.HasValue)
                {
                    returnList.Add($"valueColor: {valueColor}");
                }

                return '{' + string.Join(",", returnList) + '}';
            }
        }

        static string TryGetPropValueByPropName(ReactNode node, string propName)
        {
            return FirstOrDefaultOf(from p in node.Properties where p.Name == propName select p.Value);
        }
    }

    protected override Element render()
    {
        var senderElement = children.FindElementByElementType(typeof(sender));
        var amountElement = children.FindElementByElementType(typeof(amount));
        var receiverElement = children.FindElementByElementType(typeof(receiver));
        var transactionDetailListElement = children.FindElementByElementType(typeof(transactionDetailList));

        return new Fragment
        {
            new div(Id(id), OnClick(onMouseClick))
            {
                new FlexRow
                {
                    new FlexColumn(AlignItemsCenter, PaddingTop("10%"))
                    {
                        new FlexRowCentered
                        {
                            Background(rgb(22, 160, 133)),
                            BorderRadius("50%"),
                            Padding(6),
                            Margin(6)
                        },
                        new div
                        {
                            Height(80),
                            Width(2),
                            Border(1, dashed, rgba(0, 0, 0, 0.12))
                        }
                    },

                    new FlexColumn
                    {
                        Width("100%"),
                        JustifyContentCenter,
                        new FlexColumn
                        {
                            JustifyContentCenter,
                            Background(rgb(248, 249, 250)),
                            Padding(16),
                            MarginLeft(16),
                            HeightFitContent,
                            BorderRadius(10),

                            senderElement?.children
                        }
                    }
                },

                new FlexRow(AlignItemsCenter, Gap(32))
                {
                    new svg(svg.ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Fill(rgb(22, 160, 133)))
                    {
                        new path { d = "m20 12-1.41-1.41L13 16.17V4h-2v12.17l-5.58-5.59L4 12l8 8z" }
                    },
                    amountElement?.children
                },

                new FlexRow
                {
                    new FlexColumn(AlignItemsCenter, MarginBottom("10%"))
                    {
                        new div
                        {
                            Height(80),
                            Width(2),
                            Border(1, dashed, rgba(0, 0, 0, 0.12))
                        },
                        new FlexRowCentered
                        {
                            Background(rgb(22, 160, 133)),
                            BorderRadius("50%"),
                            Padding(6),
                            Margin(6)
                        },
                    },

                    new FlexColumn
                    {
                        Width("100%"),
                        JustifyContentCenter,
                        new FlexColumn
                        {
                            JustifyContentCenter,
                            Background(rgb(248, 249, 250)),
                            Padding(16),
                            MarginLeft(16),
                            HeightFitContent,
                            BorderRadius(10),

                            receiverElement?.children
                        }
                    }
                },

                new div
                {
                    PaddingY(24), PositionRelative,
                    new div
                    {
                        PositionAbsolute, Left(-24), Right(-24), Height(1),
                        Background(rgba(0, 0, 0, 0.12))
                    }
                },

                new FlexColumn(PaddingLeft(36))
                {
                    transactionDetailListElement?.children
                }
            }
        };
    }
}
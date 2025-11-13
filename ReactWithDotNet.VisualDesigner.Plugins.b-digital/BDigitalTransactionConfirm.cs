using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDigitalTransactionConfirm), Package = "b-digital-transaction-confirm")]
sealed class BDigitalTransactionConfirm : PluginComponentBase
{
    // @formatter:on

    protected override Element render()
    {
        return new Fragment
        {
            new div(Id(id), OnClick(onMouseClick))
            {
                children
            }
        };

        //var leftFlow = new FlexColumnCentered(Width(24), Gap(12))
        //{
        //    new div { Background(rgb(22, 160, 133)), Padding(6), BorderRadius(8) },
        //    new div { Width(0), Border(1, dashed, rgba(0, 0, 0, 0.12)), Height(80) },
        //    new DynamicMuiIcon { name = "ArrowDownwardOutlined", fontSize = "small" },
        //    new div { Width(0), Border(1, dashed, rgba(0, 0, 0, 0.12)), Height(80) },
        //    new div { Background(rgb(22, 160, 133)), Padding(6), BorderRadius(8) },
        //};

        //var partContent = new FlexColumn(JustifyContentSpaceBetween)
        //{
        //    // TOP
        //    new FlexColumn
        //    {
        //        children.Count > 0 ? children[0] : null,

        //        new FlexRow(AlignItemsCenter, Gap(4))
        //        {
        //            children.Count > 0 ? children[1] : null,

        //            new BTypography
        //            {
        //                children ={sender_item1_text + (sender_item1_value.HasValue() ? ":" : null)},
        //                variant  = sender_item1_text_variant,
        //                color    = sender_item1_text_color
        //            },
        //            new BTypography
        //            {
        //                children ={sender_item1_value},
        //                variant  = sender_item1_value_variant,
        //                color    = sender_item1_value_color
        //            }
        //        }
        //    },

        //    // CENTER
        //    new div{ "Amount"},

        //    // BOTTOM
        //    new FlexColumn
        //    {
        //        new BTypography
        //        {
        //            children ={receiver_title},
        //            variant  = receiver_title_variant,
        //            color    = receiver_title_color
        //        },

        //        new FlexRow
        //        {
        //            new BTypography
        //            {
        //                children ={sender_item1_text},
        //                variant  = sender_item1_text_variant,
        //                color    = sender_item1_text_color
        //            },
        //            new BTypography
        //            {
        //                children ={sender_item1_value},
        //                variant  = sender_item1_value_variant,
        //                color    = sender_item1_value_color
        //            }
        //        }
        //    }
        //};

        //return new FlexColumn(Background(White), BorderRadius(10), Border(1, solid, rgba(0, 0, 0, 0.12)), Padding(24), Id(id), OnClick(onMouseClick))
        //{
        //    new FlexRow(Gap(32), Height(368))
        //    {
        //        leftFlow,  partContent
        //    },

        //    new FlexRow(BorderTop(1, solid, rgba(0, 0, 0, 0.12)))
        //    {

        //    }
        //};
        /* <BDigitalTransactionConfirm
                                              senderData={{
                                                  titleText: getMessage("MoneyTransferSenderLabel"),
                                                  item1: { value: getMessage("GeneralKuveytTurkBankNameLabel"), valueVariant: "body1" },
                                                  item2: { text: getMessage("GeneralAccountNo2Label"), textVariant: "body2", value: model.selectedAccount.fullAccountNumber, valueVariant: "body1" },
                                                  item3: { text: `${getMessage("GeneralBalanceLabel")}:`, textVariant: "body2", value: `${BLocalization.formatMoney(model.selectedAccount.balance)} ${model.selectedAccount.fecCode}`, valueVariant: "body1" }
                                              }}
                                              receiverData={{
                                                  titleText: getMessage("MoneyTransferReceiverLabel"),
                                                  item1: { value: getMessage("FindeksLabel"), valueVariant: "body1" },
                                                  item2: { text: `${getMessage("GeneralTransactionLabel")}:`, textVariant: "body2", value: getMessage("FindeksRiskReport"), valueVariant: "body1" }
                                              }}
                                              transferAmount={`${BLocalization.formatMoney(model.amount)} ${model.fecCode}`}
                                              transferAmountVariant="h6"
                                              transactionDetailList={[
                                                  {
                                                      text: getMessage("GeneralTransactionDate2Label"),
                                                      textVariant: "body2",
                                                      value: BLocalization.formatDateTime(model.transactionDate, Constant.Format.DateCommon),
                                                      valueVariant: "body1",
                                                  }
                                              ]}
                                          />
                                      </BDigitalBox>*/
    }
     

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalTransactionConfirm))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;






        List<ReactProperty> finalProps = [];

        // sender
        {
            List<string> lines = [];
                        
            var sender = new
            {
                title = node.TryGetNodeItemAt( [0, 1, 0, 0]),

                item1Text  = node.TryGetNodeItemAt( [0, 1, 0, 1, 0]),
                item1Value = node.TryGetNodeItemAt( [0, 1, 0, 1, 1]),
                        
                item2Text  = node.TryGetNodeItemAt( [0, 1, 0, 2, 0]),
                item2Value = node.TryGetNodeItemAt( [0, 1, 0, 2, 1]),
                        
                item3Text  = node.TryGetNodeItemAt( [0, 1, 0, 3, 0]),
                item3Value = node.TryGetNodeItemAt( [0, 1, 0, 3, 1])
            };
                        
            if (sender.title is not null)
            {
                lines.Add($"titleText: {sender.title.Children[0].Properties[0].Value}");
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
            var transferAmount = node.TryGetNodeItemAt( [1, 1]);
            if (transferAmount is not null)
            {
                finalProps.Add(new ReactProperty
                {
                    Name  = "transferAmount",
                    Value =  string.Join(",", transferAmount.Children[0].Children[0].Text)
                });

                finalProps.Add(new ReactProperty
                {
                    Name  = "transferAmountVariant",
                    Value = string.Join(",", transferAmount.Properties[0].Value)
                });
            }
        }
                    
                    
        // receiver
        {
            List<string> lines = [];
                        
            var receiver = new
            {
                title = node.TryGetNodeItemAt( [2, 1, 0, 0]),

                item1Text  = node.TryGetNodeItemAt( [2, 1, 0, 1, 0]),
                item1Value = node.TryGetNodeItemAt( [2, 1, 0, 1, 1]),
                        
                item2Text  = node.TryGetNodeItemAt( [2, 1, 0, 2, 0]),
                item2Value = node.TryGetNodeItemAt( [2, 1, 0, 2, 1]),
                        
                item3Text  = node.TryGetNodeItemAt( [2, 1, 0, 3, 0]),
                item3Value = node.TryGetNodeItemAt( [2, 1, 0, 3, 1])
            };
                        
            if (receiver.title is not null)
            {
                lines.Add($"titleText: {receiver.title.Children[0].Properties[0].Value}");
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
                        
            var items = new
            {
                item1Text  = node.TryGetNodeItemAt( [4, 0, 0]),
                item1Value = node.TryGetNodeItemAt( [4, 0, 1]),
                        
                item2Text  = node.TryGetNodeItemAt( [4, 1, 0]),
                item2Value = node.TryGetNodeItemAt( [4, 1, 1]),
                        
                item3Text  = node.TryGetNodeItemAt( [4, 2, 0]),
                item3Value = node.TryGetNodeItemAt( [4, 2, 1])
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

            finalProps.Add(new ReactProperty
            {
                Name  = "transactionDetailList",
                Value = '[' + string.Join(",", lines) + ']'
            });
        }

                 
                    
        node =  node with
        {
            Children = [],
            Properties = finalProps.ToImmutableList()
        };

        return Result.From(node);

        static string getItem(ReactNode textNode, ReactNode valueNode)
        {
            if (valueNode is null)
            {
                if (textNode is null)
                {
                    return null;
                }

                var value = textNode.Children[0].Properties[0].Value;

                var valueVariant = textNode.Properties[0].Value;

                return '{' + "value:" + value + ", valueVariant: " + valueVariant + '}';
            }

            {
                var text = textNode.Children[0].Properties[0].Value;

                 var textVariant = textNode.Properties[0].Value;
                            
                var value = valueNode.Children[0].Properties[0].Value;

                var valueVariant = valueNode.Properties[0].Value;

                return '{' + "text: " + text + ", textVariant: "+ textVariant +  ", value:" + value + ", valueVariant: " + valueVariant + '}';
            }
        }
                
       
    }

}
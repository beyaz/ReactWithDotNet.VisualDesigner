using System.Collections.Immutable;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
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

        
        return Result.From((node, new TsImportCollection
        {
            {nameof(BDigitalTransactionConfirm),"b-digital-transaction-confirm"}
        }));

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
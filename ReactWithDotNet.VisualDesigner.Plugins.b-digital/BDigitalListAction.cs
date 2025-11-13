using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDigitalListAction), Package = "b-digital-list-action")]
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
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalListAction))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;
        
        
        
       
        var leftListData = node.TryFindDesignNamedNode("leftListData");
        
        if (leftListData is not null)
        {
            var leftListDataProperty = new ReactProperty
            {
                Name = "leftListData",
                Value = "[" +
                        
                       
                        
                        "]"
            };

            node = node with
            {
                Children = [],
                Properties = node.Properties.Add(leftListDataProperty)
            };
        }

        return AnalyzeChildren(input with{Node = node}, AnalyzeReactNode);
        
    }

    protected override Element render()
    {
        return new FlexRow(JustifyContentSpaceBetween, AlignItemsCenter, Padding(16))
        {
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
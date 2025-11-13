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
    public static ReactNode AnalyzeReactNode(ReactNode node, ComponentConfig componentConfig)
    {
        if (node.Tag != nameof(BDigitalListAction))
        {
            return node with
            {
                Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList()
            };
        }
        
       
        var leftListData = node.TryFindDesignNamedNode("leftListData");
        
        if (leftListData is not null)
        {
            var leftListDataProperty = new ReactProperty
            {
                Name = "leftListData",
                Value = "[" +
                        
                        Exporters.
                        
                        +
                        
                        "]"
            };

            node = node with
            {
                Children = [],
                Properties = node.Properties.Add(leftListDataProperty)
            };
        }

        return node;
        
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
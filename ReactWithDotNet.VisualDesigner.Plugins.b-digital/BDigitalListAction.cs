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

        var actions = node.TryFindDesignNamedNode("action");

        if (leftListData is not null && actions is not null)
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
                Children = [leftListData],
                Properties = node.Properties.Add(actionsProperty)
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
using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BDigitalSearchInput), Package = "b-digital-search-input")]
sealed class BDigitalSearchInput : PluginComponentBase
{
    [JsTypeInfo(JsType.Function)]
    public string handleChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string hintText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string searchTerm { get; set; }

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, ComponentConfig componentConfig)
    {
        if (node.Tag != nameof(BDigitalSearchInput))
        {
            return node with
            {
                Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList()
            };
        }

        node = ApplyTranslateOperationOnProps(node, componentConfig, nameof(hintText));

        return node;
    }

    protected override Element render()
    {
        return null;
    }
}
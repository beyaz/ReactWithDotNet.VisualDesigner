using System.Collections.Immutable;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BChip", Package = "b-chip")]
sealed class BChip : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string color { get; set; }

    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onClick { get; set; }

    [Suggestions("default, filled , outlined")]
    [JsTypeInfo(JsType.String)]
    public string variant { get; set; }

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
    {
        if (node.Tag == nameof(BChip))
        {
            var onClickProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onClick));
            if (onClickProp is not null)
            {
                var properties = node.Properties;

                List<string> lines =
                [
                    "() =>",
                    "{"
                ];

                if (IsAlphaNumeric(onClickProp.Value))
                {
                    lines.Add(onClickProp.Value + "();");
                }
                else
                {
                    lines.Add(onClickProp.Value);
                }

                lines.Add("}");

                onClickProp = onClickProp with
                {
                    Value = string.Join(Environment.NewLine, lines)
                };

                properties = properties.SetItem(properties.FindIndex(x => x.Name == onClickProp.Name), onClickProp);

                node = node with { Properties = properties };
            }

            node = AddContextProp(node);
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        return new div(BorderRadius(16))
        {
            new Chip
            {
                color   = color,
                label   = label,
                variant = variant
            },
            Id(id), OnClick(onMouseClick)
        };
    }
}
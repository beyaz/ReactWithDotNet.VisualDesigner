using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BButton", Package = "b-button")]
sealed class BButton : PluginComponentBase
{
    [JsTypeInfo(JsType.Function)]
    public string onClick { get; set; }

    [JsTypeInfo(JsType.String)]
    public string text { get; set; }

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, ComponentConfig componentConfig)
    {
        if (node.Tag == nameof(BButton))
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
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        return new FlexRowCentered(Background(rgba(22, 160, 133, 1)), BorderRadius(10), PaddingY(8), PaddingX(64), MinWidth(160))
        {
            FontSize(15),
            LineHeight26,
            LetterSpacing("0.46px"),
            Color(rgba(248, 249, 250, 1)),

            new div { text ?? "?" },

            Id(id),
            OnClick(onMouseClick),
            
            
        };
    }
}
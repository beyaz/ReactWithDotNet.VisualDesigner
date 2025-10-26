using System.Collections.Immutable;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = "BCheckBox", Package = "b-check-box")]
sealed class BCheckBox : PluginComponentBase
{
    [JsTypeInfo(JsType.Boolean)]
    public string @checked { get; set; }

    [JsTypeInfo(JsType.String)]
    public new string id { get; set; }

    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onCheck { get; set; }

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
    {
        if (node.Tag == nameof(BCheckBox))
        {
            var checkedProp = node.Properties.FirstOrDefault(x => x.Name == nameof(@checked));
            var onCheckProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onCheck));

            if (checkedProp is not null)
            {
                var properties = node.Properties;

                var requestAssignmentLine = string.Empty;
                if (checkedProp.Value.StartsWith("request.", StringComparison.OrdinalIgnoreCase))
                {
                    requestAssignmentLine = $"  updateRequest(r => {{ r.{checkedProp.Value.RemoveFromStart("request.")} = checked; }});";
                }

                List<string> lines =
                [
                    "(e: any, checked: boolean) =>",
                    "{",
                    requestAssignmentLine
                ];

                if (onCheckProp is not null)
                {
                    if (IsAlphaNumeric(onCheckProp.Value))
                    {
                        lines.Add(onCheckProp.Value + "(e, checked);");
                    }
                    else
                    {
                        lines.Add(onCheckProp.Value);
                    }
                }

                lines.Add("}");

                if (onCheckProp is not null)
                {
                    onCheckProp = onCheckProp with
                    {
                        Value = string.Join(Environment.NewLine, lines)
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == onCheckProp.Name), onCheckProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = "onCheck",
                        Value = string.Join(Environment.NewLine, lines)
                    });
                }

                node = node with { Properties = properties };
            }

            node = Plugin.AddContextProp(node);
        }

        return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
    }

    protected override Element render()
    {
        var svgForIsCheckedFalse = new svg(ViewBox(0, 0, 24, 24), Fill(rgb(22, 160, 133)))
        {
            new path { d = "M19 5v14H5V5h14m0-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z" }
        };

        var svgForIsCheckedTrue = new svg(ViewBox(0, 0, 24, 24), Fill(rgb(22, 160, 133)))
        {
            new path { d = "M19 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.11 0 2-.9 2-2V5c0-1.1-.89-2-2-2zm-9 14l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" }
        };

        return new FlexRowCentered(Gap(12), WidthFitContent)
        {
            new FlexRowCentered(Size(24))
            {
                @checked == "true" ? svgForIsCheckedTrue : svgForIsCheckedFalse
            },
            new div { label ?? "?" },

            Id(id),
            OnClick(onMouseClick)
        };
    }
}
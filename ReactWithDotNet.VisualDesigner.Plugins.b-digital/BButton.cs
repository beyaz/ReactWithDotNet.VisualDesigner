using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
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
    
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }

    [NodeAnalyzer]
    public static ReactNode AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BButton))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        var node = input.Node;

        var onClickProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onClick));

        if (onClickProp is not null && !IsAlphaNumeric(onClickProp.Value))
        {
            var properties = node.Properties;

            List<string> lines =
            [
                "() =>",

                "{",
                onClickProp.Value,
                "}"
            ];

            onClickProp = onClickProp with
            {
                Value = string.Join(Environment.NewLine, lines)
            };

            properties = properties.SetItem(properties.FindIndex(x => x.Name == onClickProp.Name), onClickProp);

            node = node with { Properties = properties };
        }


        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode);
    }

    protected override Element render()
    {

        return new Button
        {
            variant = type ?? "text",
            
            id = id,
            
            onClick = onMouseClick,
            
            color = "success",
            
            sx={ textTransform= "none", fontWeight= "bold", color = "#16A085"},
            children=
            {
                new div{ text}
            }
        };

    }
}
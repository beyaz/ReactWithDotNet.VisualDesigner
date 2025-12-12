using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BChip : PluginComponentBase
{
    [Suggestions("default , primary , secondary")]
    [JsTypeInfo(JsType.String)]
    public string color { get; set; }

    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string onClick { get; set; }

    [Suggestions("default, outlined")]
    [JsTypeInfo(JsType.String)]
    public string variant { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BChip))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var node = input.Node;
        
        
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

        var import = (nameof(BChip), "b-chip");
        
        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        Style extraStyle = [];
        switch (color)
        {
            case "default":
                extraStyle = [ variant =="default" ?  Background("#e0e0e0") : null, Color(rgba(0, 0, 0, 0.87))];
                break;
            case "primary":
                extraStyle = [variant =="default" ? Background("#16A085"): null, Color("#fff")];
                break;
            case "secondary":
                extraStyle = [variant =="default" ? Background("#FF9500"): null, Color(rgba(0, 0, 0, 0.87))];
                break;
        }

        return  new Chip
            {
                
                label   = label,
                variant = variant,
                style =
                {
                    extraStyle
                },
                id = id,
                onClick = onMouseClick
            };
    }
}
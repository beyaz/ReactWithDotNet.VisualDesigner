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
    
    
    [Suggestions("raised , flat")]
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }
    
    [Suggestions("primary")]
    [JsTypeInfo(JsType.String)]
    public string colorType { get; set; }
    
    
    [Suggestions("true , false")]
    [JsTypeInfo(JsType.Boolean)]
    public string fullWidth { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
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

    internal Style style { get; set; }
    
    protected override Element render()
    {
        var variant = type switch
        {
            "raised"=>"contained",
            
            _=> "text"
        };

        Style defaultStyle = [];
        
        if (colorType == "primary")
        {
            defaultStyle = [FontWeightBold, TextTransform(none), BackgroundColor("#16A085"), Color("white"), BorderRadius(10)];
        }

        return new Button
        {
            fullWidth = fullWidth == "true",

            style = { defaultStyle, style },

            variant = variant,

            id = id,

            onClick = onMouseClick,

            children =
            {
                new div { text }
            }
        };

    }
}
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BButton : PluginComponentBase
{
    [JsTypeInfo(JsType.Function)]
    public string onClick { get; set; }

    [JsTypeInfo(JsType.String)]
    public string text { get; set; }
    
    [JsTypeInfo(JsType.Boolean)]
    public string allowLabelCase { get; set; }
    
    
    
    [Suggestions("raised , flat , contained")]
    [JsTypeInfo(JsType.String)]
    public string type { get; set; }
    
    [Suggestions("primary , default")]
    [JsTypeInfo(JsType.String)]
    public string colorType { get; set; }
    
    [Suggestions("contained , outlined , text")]
    [JsTypeInfo(JsType.String)]
    public string variant { get; set; }
    
    [Suggestions("primary , default")]
    [JsTypeInfo(JsType.String)]
    public string color { get; set; }
    
    
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


        var import = (nameof(BButton), "b-button");
        
        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    internal Style style { get; set; }
    
    protected override Element render()
    {
        if (variant is null)
        {
            if (type.HasValue())
            {
                variant = type switch
                {
                    "raised"=>"contained",
            
                    not null =>type,
            
                    null => "text"
                };
            }
        }
       

        Style defaultStyle = [BorderRadius(10),  TextTransform(none)];
        
        if (color == "primary" || colorType == "primary")
        {
            defaultStyle =
            [
                DisplayFlexRowCentered,
                TextTransform(none),
                FontWeightBold,
                BackgroundColor("#16A085"),
                Color("white"),
                BorderRadius(10)
            ];
        }
        
        if (variant == "contained" || color == "default" || (type is null && color is null && variant.HasNoValue) )
        {
            defaultStyle =
            [
                DisplayFlexRowCentered,
                TextTransform(none),
                FontWeightBold,
                BackgroundColor("#d5d5d5"),
                Color("black"),
                BorderRadius(10)
            ];
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
                new div { text } , children
            }
        };

    }
}
namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalPhone : PluginComponentBase
{
    [JsTypeInfo(JsType.String)]
    public string label { get; set; }

    [JsTypeInfo(JsType.String)]
    public string hintText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string phoneNumber { get; set; }

    [JsTypeInfo(JsType.Function)]
    public string handlePhoneChange { get; set; }


    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalPhone))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }

        input = ApplyTranslateOperationOnProps(input, nameof(label), nameof(hintText));
        
        
        var node = input.Node;

        node = Run(node, [
            Transforms.OnChange
        ]);


        var import = (nameof(BDigitalPhone), "b-digital-phone");

        return AnalyzeChildren(input with { Node = node }, AnalyzeReactNode).With(import);
    }

    protected override Element render()
    {
        var textContent = string.Empty;
        if (hintText.HasValue)
        {
            textContent = hintText;
        }

        if (phoneNumber.HasValue)
        {
            textContent += " | " + phoneNumber;
        }

        return new div(PaddingTop(16), PaddingBottom(8))
        {
            new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
            {
                new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif")) { textContent },

                Id(id), OnClick(onMouseClick)
            }
        };
    }
    
    
    static class Transforms
    {
      

        internal static ReactNode OnChange(ReactNode node)
        {
           
            var phoneNumberProp = node.Properties.FirstOrDefault(x => x.Name == nameof(phoneNumber));
            var handlePhoneChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(handlePhoneChange));

            if (phoneNumberProp is not null)
            {
                var properties = node.Properties;

                var lines = new TsLineCollection
                {
                    "(value: string, formattedValue: string, areaCode: string) =>",
                    "{",
                    GetUpdateStateLines(phoneNumberProp.Value, "value")
                };

                if (handlePhoneChangeProp is not null)
                {
                    if (IsAlphaNumeric(handlePhoneChangeProp.Value))
                    {
                        lines.Add(handlePhoneChangeProp.Value + "(value, formattedValue, areaCode);");
                    }
                    else
                    {
                        lines.Add(handlePhoneChangeProp.Value);
                    }
                }

                lines.Add("}");

                if (handlePhoneChangeProp is not null)
                {
                    handlePhoneChangeProp = handlePhoneChangeProp with
                    {
                        Value = lines.ToTsCode()
                    };

                    properties = properties.SetItem(properties.FindIndex(x => x.Name == handlePhoneChangeProp.Name), handlePhoneChangeProp);
                }
                else
                {
                    properties = properties.Add(new()
                    {
                        Name  = "handlePhoneChange",
                        Value = lines.ToTsCode()
                    });
                }

                node = node with { Properties = properties };
            }
            
            return node;
        }
    }
}
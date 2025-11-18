namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[TsImport(Name = nameof(BDigitalSearchInput), Package = "b-digital-search-input")]
sealed class BDigitalSearchInput : PluginComponentBase
{
    [JsTypeInfo(JsType.Function)]
    public string handleChange { get; set; }

    [JsTypeInfo(JsType.String)]
    public string hintText { get; set; }

    [JsTypeInfo(JsType.String)]
    public string searchTerm { get; set; }

    

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalSearchInput))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;




        



        node = ApplyTranslateOperationOnProps(node, componentConfig, nameof(hintText));

        
        
        
        
        return Result.From((node, new TsImportCollection
        {
            {nameof(BDigitalSearchInput),"b-digital-search-input"}
        }));
    }

    protected override Element render()
    {
        return new div(Border(1, solid, "#c0c0c0"), Height(56), BorderRadius(10), DisplayFlex, AlignItemsCenter, Color("#000000DE"), Gap(8), PaddingLeft(16))
        {
            new svg(svg.Width(24), svg.Height(24), ViewBox(0, 0, 24, 24), Fill(none), svg.Xmlns("http://www.w3.org/2000/svg"))
            {
                new path(path.D("M9.5193 15.6153C7.81164 15.6153 6.36547 15.023 5.1808 13.8385C3.9963 12.6538 3.40405 11.2077 3.40405 9.50002C3.40405 7.79235 3.9963 6.34618 5.1808 5.16152C6.36547 3.97702 7.81164 3.38477 9.5193 3.38477C11.227 3.38477 12.6731 3.97702 13.8578 5.16152C15.0423 6.34618 15.6346 7.79235 15.6346 9.50002C15.6346 10.2142 15.5147 10.8963 15.2751 11.5463C15.0352 12.1963 14.7153 12.7616 14.3153 13.2423L20.0693 18.9963C20.2078 19.1346 20.2786 19.3086 20.2818 19.5183C20.285 19.7279 20.2141 19.9052 20.0693 20.05C19.9245 20.1948 19.7488 20.2673 19.5423 20.2673C19.336 20.2673 19.1604 20.1948 19.0156 20.05L13.2616 14.296C12.7616 14.7088 12.1866 15.0319 11.5366 15.2653C10.8866 15.4986 10.2141 15.6153 9.5193 15.6153ZM9.5193 14.1155C10.8078 14.1155 11.8991 13.6683 12.7933 12.774C13.6876 11.8798 14.1348 10.7885 14.1348 9.50002C14.1348 8.21152 13.6876 7.12018 12.7933 6.22601C11.8991 5.33168 10.8078 4.88452 9.5193 4.88452C8.2308 4.88452 7.13947 5.33168 6.2453 6.22601C5.35097 7.12018 4.9038 8.21152 4.9038 9.50002C4.9038 10.7885 5.35097 11.8798 6.2453 12.774C7.13947 13.6683 8.2308 14.1155 9.5193 14.1155Z"), Fill(Black))
            },
            new div(Opacity(0.5))
            {
                hintText
            }
        };
    }
}
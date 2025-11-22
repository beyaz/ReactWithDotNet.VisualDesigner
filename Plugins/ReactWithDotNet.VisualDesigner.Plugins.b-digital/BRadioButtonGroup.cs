using Newtonsoft.Json;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BRadioButtonGroup : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string items { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BRadioButtonGroup))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BRadioButtonGroup),"b-radio-button-group");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    protected override Element render()
    {
        if (items.HasNoValue)
        {
            return null;
        }

        var itemList = JsonConvert.DeserializeObject<ItemModel[]>(items);

        return new FlexRow(Gap(24))
        {
            itemList.Select(x => new FlexRowCentered(Gap(12), WidthFitContent)
            {
                new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Fill(rgb(22, 160, 133)))
                {
                    new path { d = "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zm0-5C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8z" }
                },

                new label
                {
                    FontSize16, FontWeight400, LineHeight(1.5), FontFamily("Roboto, sans-serif"),
                    x.label
                }
            })
        } + Id(id) + OnClick(onMouseClick);
    }

    class ItemModel
    {
        public string label { get; set; }
    }
}
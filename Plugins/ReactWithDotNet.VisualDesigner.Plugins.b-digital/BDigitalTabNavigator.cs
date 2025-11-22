using Newtonsoft.Json;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
sealed class BDigitalTabNavigator : PluginComponentBase
{
    [JsTypeInfo(JsType.Array)]
    public string items { get; set; }

    [JsTypeInfo(JsType.String)]
    public string mainResource { get; set; }

    [JsTypeInfo(JsType.Number)]
    public string selectedTab { get; set; }

    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BDigitalTabNavigator))
        {
            return  AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var import = (nameof(BDigitalTabNavigator),"b-digital-tab-navigator");
        
        return  AnalyzeChildren(input, AnalyzeReactNode).With(import);
    }
    
    protected override Element render()
    {
        if (items.HasNoValue)
        {
            return null;
        }

        var itemList = JsonConvert.DeserializeObject<ItemModel[]>(items);

        return new FlexRow(BorderBottom(1, solid, rgba(0, 0, 0, 0.12)), Color(rgb(22, 160, 133)))
        {
            new FlexRow(Gap(24))
            {
                itemList.Select(x => new FlexRowCentered(Padding(24), WidthFitContent, AlignItemsCenter)
                {
                    BorderBottom(2, solid, rgb(22, 160, 133)),

                    new label
                    {
                        FontSize16, FontWeight400, LineHeight(1.5), FontFamily("Roboto, sans-serif"),
                        x.label
                    }
                })
            }
        } + Id(id) + OnClick(onMouseClick);
    }

    class ItemModel
    {
        public string label { get; set; }
    }
}
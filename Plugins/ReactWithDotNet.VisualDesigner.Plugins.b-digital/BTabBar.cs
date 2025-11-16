using System.Collections.Immutable;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

[CustomComponent]
[Import(Name = nameof(BTabBar), Package = "b-tab-bar")]
sealed class BTabBar : PluginComponentBase
{
 

    [JsTypeInfo(JsType.Array)]
    public string tabItems { get; set; }

   

    [JsTypeInfo(JsType.Number)]
    public string value { get; set; }

    
    [Suggestions("secondary , primary")]
    [JsTypeInfo(JsType.String)]
    public string mode { get; set; }
    
    [JsTypeInfo(JsType.Function)]
    public string onChange { get; set; }

    
    [NodeAnalyzer]
    public static NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BTabBar))
        {
            return AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var (node, componentConfig) = input;

        return Result.From(node);
    }

    public string SelectedTabIndex { get; set; }
    
    Task OnClickHandler(MouseEvent e)
    {
        SelectedTabIndex = e.target.id;

        return Task.CompletedTask;
    }
    
    protected override Element render()
    {
        

        return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexRow(BorderBottom("1px solid rgb(189, 189, 189)"))
            {

                from tab in children.Select((el,index)=>new{el,index})
                select new FlexRowCentered
                {
                    ((HtmlElement)tab.el).data["text"],
                    
                     Color("#16A085"),
                     
                     SelectedTabIndex == tab.index ? Opacity(1) : Opacity(0.7) ,  
                    
                    OnClick(OnClickHandler)
                }
            },
            
            new FlexRow
            {
                new FlexRowCentered
                {
                    textContent
                }
            },
        }; 
    }

    
}
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
    public static async NodeAnalyzeOutput AnalyzeReactNode(NodeAnalyzeInput input)
    {
        if (input.Node.Tag != nameof(BTabBar))
        {
            return await AnalyzeChildren(input, AnalyzeReactNode);
        }
        
        var node = input.Node;

        foreach (var name in new[] { "leftListData", "rightListData" })
        {
            var listData = node.TryFindDesignNamedNode(name);
            if (listData is null)
            {
                continue;
            }

            IReadOnlyList<string> linesCollection;
            {
                var response = await
                (
                    from child in listData.Children
                    from analyzedChild in input.AnalyzeNode(child)
                    from lines in input.ReactNodeModelToElementTreeSourceLinesConverter(analyzedChild)
                    select string.Join(Environment.NewLine, lines)
                ).AsResult();

                if (response.HasError)
                {
                    return response.Error;
                }

                linesCollection = response.Value;
            }

            var items = string.Join("," + Environment.NewLine, linesCollection);

            var property = new ReactProperty
            {
                Name  = name,
                Value = "[" + items + "]"
            };

            node = node with
            {
                Properties = node.Properties.Add(property)
            };
        }

        return node with
        {
            Children = []
        };
    }

  
    
    
    protected override Element render()
    {
        

        return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
        {
            Id(id), OnClick(onMouseClick),

            new FlexRow(BorderBottom("1px solid rgb(189, 189, 189)"))
            {
                from tab in children.Select((el,index)=>new{el,index})
                where ((HtmlElement)tab.el).data.ContainsKey("text")
                select new FlexRowCentered(MarginX(24), Height(30))
                {
                    TryClearStringValue(((HtmlElement)tab.el).data["text"]),
                    
                     Color("#16A085"),
                     
                     BorderBottom(2, solid, "#16A085"),
                     
                     FontWeight500, FontSize13
                }
            },
            
            children
        }; 
    }

    
}
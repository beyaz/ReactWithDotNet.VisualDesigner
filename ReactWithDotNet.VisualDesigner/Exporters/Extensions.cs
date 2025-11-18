global using static ReactWithDotNet.VisualDesigner.Exporters.Extensions;
global using NodeAnalyzeOutput = System.Threading.Tasks.Task<Toolbox.Result<(ReactWithDotNet.VisualDesigner.Exporters.ReactNode Node, ReactWithDotNet.VisualDesigner.TsImportCollection TsImportCollection)>>;
    
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.Exporters;

public delegate NodeAnalyzeOutput AnalyzeNodeDelegate(NodeAnalyzeInput input);


public sealed record NodeAnalyzeInput
{
    public ReactNode Node { get; init; }
    
    public ComponentConfig ComponentConfig{ get; init; }
    
    public Func<ReactNode,Task<Result<IReadOnlyList<string>>>> 
        ReactNodeModelToElementTreeSourceLinesConverter{ get; init; }
    
    public Func<ReactNode, NodeAnalyzeOutput> AnalyzeNode { get; init; }

    public void Deconstruct(out ReactNode node, out ComponentConfig componentConfig)
    {
        node            = Node;
        componentConfig = ComponentConfig;
    }

}

static class Extensions
{
    static IReadOnlyList<AnalyzeNodeDelegate> AnalyzeNodeList
    {
        get
        {
            return
                field ??=
                    (
                        from type in Plugin.AllCustomComponents
                        from methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                        where methodInfo.GetCustomAttribute<NodeAnalyzerAttribute>() is not null
                        select (AnalyzeNodeDelegate)Delegate
                            .CreateDelegate(typeof(AnalyzeNodeDelegate), methodInfo)
                    )
                    .ToList();
        }
    }

    public static async NodeAnalyzeOutput AnalyzeNode(NodeAnalyzeInput input)
    {
        TsImportCollection tsImportCollection = new();
        
        foreach (var analyze in AnalyzeNodeList)
        {
            var response = await analyze(input);
            if (response.HasError)
            {
                return response.Error;
            }
                
            input = input with
            {
                Node =  response.Value.Node
            };
            
            tsImportCollection.Add(response.Value.TsImportCollection);
        }

        return (input.Node, tsImportCollection);
    }


}
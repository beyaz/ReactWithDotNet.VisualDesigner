global using static ReactWithDotNet.VisualDesigner.Exporters.Extensions;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.Exporters;

public sealed record NodeAnalyzeInput
{
    public ReactNode Node { get; init; }
    public ComponentConfig ComponentConfig{ get; init; }
    
    
    public void Deconstruct(out ReactNode node, out ComponentConfig componentConfig)
    {
        node            = Node;
        componentConfig = ComponentConfig;
    }

}

static class Extensions
{
    static IReadOnlyList<Func<ReactNode, ComponentConfig, ReactNode>> AnalyzeNodeList
    {
        get
        {
            return
                field ??=
                    (
                        from type in Plugin.AllCustomComponents
                        from methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                        where methodInfo.GetCustomAttribute<NodeAnalyzerAttribute>() is not null
                        select (Func<ReactNode, ComponentConfig, ReactNode>)Delegate
                            .CreateDelegate(typeof(Func<ReactNode, ComponentConfig, ReactNode>), methodInfo)
                    )
                    .ToList();
        }
    }

    public static ReactNode AnalyzeNode(ReactNode node, ComponentConfig componentConfig)
    {
        foreach (var method in AnalyzeNodeList)
        {
            node = method(node, componentConfig);
        }

        return node;
    }

    public static IEnumerable<string> CalculateImportLines(ReactNode node)
    {
        var lines = new List<string>();

        foreach (var type in Plugin.AllCustomComponents)
        {
            lines.AddRange(tryGetImportLines(type, node));
        }

        foreach (var child in node.Children)
        {
            lines.AddRange(CalculateImportLines(child));
        }

        return lines.Distinct();

        static IEnumerable<string> tryGetImportLines(Type type, ReactNode node)
        {
            if (type.Name == node.Tag)
            {
                return
                    from a in type.GetCustomAttributes<ImportAttribute>()
                    select $"import {{ {a.Name} }} from \"{a.Package}\";";
            }

            return [];
        }
    }
}
global using static ReactWithDotNet.VisualDesigner.Exporters.Extensions;
using System.Reflection;

namespace ReactWithDotNet.VisualDesigner.Exporters;

static class Extensions
{
    static IReadOnlyList<Func<ReactNode, IReadOnlyDictionary<string, string>, ReactNode>> AnalyzeNodeList
    {
        get
        {
            return
                field ??=
                    (
                        from type in Plugin.AllCustomComponents
                        from methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                        where methodInfo.GetCustomAttribute<NodeAnalyzerAttribute>() is not null
                        select (Func<ReactNode, IReadOnlyDictionary<string, string>, ReactNode>)Delegate
                            .CreateDelegate(typeof(Func<ReactNode, IReadOnlyDictionary<string, string>, ReactNode>), methodInfo)
                    )
                    .ToList();
        }
    }

    public static ReactNode AnalyzeNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
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
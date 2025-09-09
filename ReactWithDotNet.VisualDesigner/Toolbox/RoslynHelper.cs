using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReactWithDotNet.VisualDesigner.Toolbox;

static class RoslynHelper
{
    public static Maybe<int> FindMethodStartLineIndexInCSharpCode(string code, string className, string methodName)
    {
        var tree = CSharpSyntaxTree.ParseText(code);

        var root = tree.GetRoot();

        var method = root
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m =>
                                m.Identifier.Text == methodName &&
                                (m.Parent as ClassDeclarationSyntax)?.Identifier.Text == className);

        if (method != null)
        {
            var lineSpan = method.GetLocation().GetLineSpan();
            return lineSpan.StartLinePosition.Line;
        }

        return None;
    }
}
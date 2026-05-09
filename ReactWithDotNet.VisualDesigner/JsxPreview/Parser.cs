using System.Text.Json;

namespace ReactWithDotNet.VisualDesigner.JsxPreview;

public class JsxElementDto
{
    public string Tag { get; set; }
    public List<string> Props { get; set; } = new();
    public List<JsxElementDto> Children { get; set; } = new();
    public string Condition { get; set; }
}

public class MethodResult
{
    public string MethodName { get; set; }
    public List<JsxElementDto> Elements { get; set; } = new();
}

class Parser
{
    static string GetText(TsNode node)
    {
        return node?.Text ?? node?.Name?.Text;
    }
    
    public static List<MethodResult> Extract(string json)
    {
        var root = JsonSerializer.Deserialize<TsNode>(json, JsonSerializerOptions.Web);
        var results = new List<MethodResult>();

        Traverse(root, results, null);

        return results;
    }

    private static void Traverse(TsNode node, List<MethodResult> results, string currentMethod)
    {
        if (node == null) return;

        // METHOD YAKALA
        if (node.Kind == SyntaxKind.FunctionDeclaration || node.Kind == SyntaxKind.MethodDeclaration)
        {
            currentMethod = node.Name?.Text;
        }

        // RETURN JSX
        if (node.Kind == SyntaxKind.ReturnStatement && node.Expression != null)
        {
            var jsx = ParseJsx(node.Expression.Expression);

            if (jsx != null && currentMethod != null)
            {
                results.Add(new MethodResult
                {
                    MethodName = currentMethod,
                    Elements   = new List<JsxElementDto> { jsx }
                });
            }
        }

        // CONDITIONAL RENDER (if)
        if (node.Kind == SyntaxKind.IfStatement)
        {
            var conditionText = GetText(node.Condition);

            if (node.ThenStatement != null)
            {
                var jsx = FindReturnJsxStatement(node.ThenStatement);
                if (jsx != null && currentMethod != null)
                {
                    jsx.Condition = conditionText;
                    results.Add(new MethodResult
                    {
                        MethodName = currentMethod,
                        Elements   = new List<JsxElementDto> { jsx }
                    });
                }
            }
        }

        // RECURSION
        foreach (var child in GetAllChildren(node))
        {
            Traverse(child, results, currentMethod);
        }
    }


    static  IEnumerable<TsNode> GetAllChildren(TsNode node)
    {
        if (node.Children != null)
            foreach (var c in node.Children)
                yield return c;

        if (node.Statements != null)
            foreach (var s in node.Statements)
                yield return s;

        if (node.Body != null)
            yield return node.Body;
    }

    static JsxElementDto ParseJsx(TsNode node)
    {
        if (node == null) return null;

        // JSX ELEMENT
        if (node.Kind == SyntaxKind.JsxElement || node.Kind == SyntaxKind.JsxSelfClosingElement)
        {
            var element = new JsxElementDto
            {
                Tag = node.TagName?.Text ?? node.OpeningElement?.TagName?.EscapedText
            };

            // Props
            if (node.Attributes != null)
            {
                foreach (var attr in node.Attributes)
                {
                    element.Props.Add(attr.Name?.Text);
                }
            }

            // Children
            foreach (var child in GetAllChildren(node))
            {
                var childJsx = ParseJsx(child);
                if (childJsx != null)
                    element.Children.Add(childJsx);
            }

            return element;
        }

        // CONDITIONAL JSX (ternary)
        if (node.Kind == SyntaxKind.ConditionalExpression)
        {
            var jsx = ParseJsx(node.ThenStatement);
            if (jsx != null)
            {
                jsx.Condition = GetText(node.Condition);
                return jsx;
            }
        }

        return null;
    }
    
    static  JsxElementDto FindReturnJsxStatement(TsNode node)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Kind == SyntaxKind.ReturnStatement)
            return ParseJsx(node.Expression);

        foreach (var child in GetAllChildren(node))
        {
            var res = FindReturnJsxStatement(child);
            if (res != null)
                return res;
        }

        return null;
    }

}
using System.Text.Json;

namespace ReactWithDotNet.VisualDesigner.JsxPreview;

static class Parser
{
    public static Result<IReadOnlyList<MethodResult>> Extract(string json)
    {
        var root = JsonSerializer.Deserialize<TsNode>(json, JsonSerializerOptions.Web);

        return Traverse(root, currentMethod: null);
    }

    static string AsDesignerPropText(TsNode jsxAttribute)
    {
        var name = jsxAttribute.Name.EscapedText;

        string value = null;

        if (jsxAttribute.Initializer.Kind == SyntaxKind.StringLiteral)
        {
            value = jsxAttribute.Initializer.Text;
        }

        return $"{name}: {value}";
    }

    static JsxElementDto FindReturnJsxStatement(TsNode node)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Kind == SyntaxKind.ReturnStatement)
        {
            return ParseJsx(node.Expression);
        }

        foreach (var child in GetAllChildren(node))
        {
            var res = FindReturnJsxStatement(child);
            if (res != null)
            {
                return res;
            }
        }

        return null;
    }

    static IEnumerable<TsNode> GetAllChildren(TsNode node)
    {
        if (node.Children != null)
        {
            foreach (var c in node.Children)
            {
                yield return c;
            }
        }

        if (node.Statements != null)
        {
            foreach (var s in node.Statements)
            {
                yield return s;
            }
        }

        if (node.Body != null)
        {
            yield return node.Body;
        }
    }

    static string GetText(TsNode node)
    {
        return node?.Text ?? node?.Name?.Text;
    }

    static JsxElementDto ParseJsx(TsNode node)
    {
        if (node == null || node.ContainsOnlyTriviaWhiteSpaces)
        {
            return null;
        }

        // JSX ELEMENT
        if (node.Kind == SyntaxKind.JsxElement || node.Kind == SyntaxKind.JsxSelfClosingElement)
        {
            var openingElement = node.OpeningElement;

            var element = new JsxElementDto
            {
                Tag = node.TagName?.Text
            };

            if (openingElement is not null)
            {
                element = new JsxElementDto
                {
                    Tag = openingElement.TagName.EscapedText
                };

                foreach (var attr in openingElement.Attributes ?? [])
                {
                    foreach (var prop in attr.Properties ?? [])
                    {
                        element.Props.Add(AsDesignerPropText(prop));
                    }
                }
            }

            // Children
            foreach (var child in GetAllChildren(node))
            {
                var childJsx = ParseJsx(child);
                if (childJsx != null)
                {
                    element.Children.Add(childJsx);
                }
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

    static Result<IReadOnlyList<MethodResult>> Traverse(TsNode node, string currentMethod)
    {
        if (node is null)
        {
            return Result.From<IReadOnlyList<MethodResult>>([]);
        }

        // Bu node bir method/function ise scope'u güncelle (recursive scope)
        var methodInThisScope = currentMethod;

        if (node.Kind == SyntaxKind.FunctionDeclaration || node.Kind == SyntaxKind.MethodDeclaration)
        {
            methodInThisScope = node.Name?.EscapedText;
        }

        List<MethodResult> results = [];

        // RETURN JSX
        if (node.Kind == SyntaxKind.ReturnStatement && node.Expression is not null)
        {
            // Mevcut kod: node.Expression.Expression
            // Null güvenliği için fallback:
            var jsxRoot = node.Expression.Expression ?? node.Expression;
            var jsx = ParseJsx(jsxRoot);

            if (jsx is not null && methodInThisScope is not null)
            {
                results.Add(new MethodResult
                {
                    MethodName = methodInThisScope,
                    Elements   = [jsx]
                });
            }
        }

        // CONDITIONAL RENDER (if)
        if (node.Kind == SyntaxKind.IfStatement)
        {
            var conditionText = GetText(node.Condition);

            if (node.ThenStatement is not null)
            {
                var jsx = FindReturnJsxStatement(node.ThenStatement);
                if (jsx is not null && methodInThisScope is not null)
                {
                    jsx.Condition = conditionText;
                    results.Add(new MethodResult
                    {
                        MethodName = methodInThisScope,
                        Elements   = [jsx]
                    });
                }
            }
        }

        // RECURSION: alt sonuçları birleştir
        foreach (var child in GetAllChildren(node))
        {
            var result = Traverse(child, methodInThisScope);
            if (result.HasError)
            {
                return result.Error;
            }

            results.AddRange(result.Value);
        }

        return results.AsReadOnly();
    }
}
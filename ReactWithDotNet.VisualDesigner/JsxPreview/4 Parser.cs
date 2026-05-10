using System.Text.Json;

namespace ReactWithDotNet.VisualDesigner.JsxPreview;

static class Parser
{
    public static Result<IReadOnlyList<MethodResult>> Extract(string json)
    {
        var root = JsonSerializer.Deserialize<TsNode>(json, JsonSerializerOptions.Web);

        return Traverse(root, currentMethod: null);
    }

    static Result<string> AsDesignerPropText(TsNode jsxAttribute)
    {
        var name = jsxAttribute.Name.EscapedText;

        string value = null;

        if (jsxAttribute.Initializer.Kind == SyntaxKind.StringLiteral)
        {
            value = '"'+jsxAttribute.Initializer.Text+'"';
        }
        else if (jsxAttribute.Initializer.Kind == SyntaxKind.NumericLiteral)
        {
            value = jsxAttribute.Initializer.Text;
        }
        else
        {
            return new ArgumentException($"Unsupported initializer kind: {jsxAttribute.Initializer.Kind}");
        }

        return $"{name}: {value}";
    }

    static Result<JsxElementDto> FindReturnJsxStatement(TsNode node)
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

    static Result<JsxElementDto> ParseJsx(TsNode node)
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
                        var result = AsDesignerPropText(prop);
                        if (result.HasError)
                        {
                            return result.Error;
                        }
                        element.Props.Add(result.Value);
                    }
                }
            }

            
            foreach (var child in GetAllChildren(node))
            {
                var childJsx = ParseJsx(child);
                if (childJsx.HasError)
                {
                    return childJsx.Error;
                }
                
                if (childJsx.Value is not null)
                {
                    element.Children.Add(childJsx.Value);
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
                jsx.Value.Condition = GetText(node.Condition);
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

       

        if (node.Kind == SyntaxKind.FunctionDeclaration || node.Kind == SyntaxKind.MethodDeclaration)
        {
            currentMethod = node.Name?.EscapedText;
        }

        List<MethodResult> results = [];

        // RETURN JSX
        if (node.Kind == SyntaxKind.ReturnStatement && node.Expression is not null)
        {
            var jsx = ParseJsx(node.Expression.Expression ?? node.Expression);

            if (jsx is not null && currentMethod is not null)
            {
                results.Add(new MethodResult
                {
                    MethodName = currentMethod,
                    Elements   = [jsx.Value]
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
                if (jsx is not null && currentMethod is not null)
                {
                    jsx.Value.Condition = conditionText;
                    results.Add(new MethodResult
                    {
                        MethodName = currentMethod,
                        Elements   = [jsx.Value]
                    });
                }
            }
        }

        // RECURSION: alt sonuçları birleştir
        foreach (var child in GetAllChildren(node))
        {
            var result = Traverse(child, currentMethod);
            if (result.HasError)
            {
                return result.Error;
            }

            results.AddRange(result.Value);
        }

        return results.AsReadOnly();
    }
}
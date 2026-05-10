using System.Text.Json;

namespace ReactWithDotNet.VisualDesigner.JsxPreview;

static class Parser
{
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
    
    public static Result<List<MethodResult>> Extract(string json)
    {
        var root = JsonSerializer.Deserialize<TsNode>(json, JsonSerializerOptions.Web);

        var results = new List<MethodResult>();

        Traverse(root, results, null);
        
        return results;
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
            
            JsxElementDto element = new JsxElementDto
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

    static void Traverse(TsNode node, List<MethodResult> results, string currentMethod)
    {
        if (node == null)
        {
            return;
        }

        // METHOD YAKALA
        if (node.Kind == SyntaxKind.FunctionDeclaration || node.Kind == SyntaxKind.MethodDeclaration)
        {
            currentMethod = node.Name?.EscapedText;
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
                    Elements   = [jsx]
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
                        Elements   = [jsx]
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
}
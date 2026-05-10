using System.Text.Json;

namespace ReactWithDotNet.VisualDesigner.JsxPreview;

static class Parser
{
    record Scope
    {
        public string TsCode { get; init; }
        
        public string MethodName { get; init; }
    }
    
    public static async Task<Result<IReadOnlyList<MethodResult>>> Extract(string tsCode)
    {
        var ast = await NodeJsBridge.Ast(tsCode);

        var root = JsonSerializer.Deserialize<TsNode>(ast.Value, JsonSerializerOptions.Web);

        return Traverse(root, new Scope{ TsCode = tsCode });
    }

    static Result<string> AsDesignerPropText(TsNode jsxAttribute, Scope scope)
    {
        var name = jsxAttribute.Name.EscapedText;

        var initializer = jsxAttribute.Initializer;
        
        if (initializer.Kind == SyntaxKind.StringLiteral)
        {
            return $"{name}: {'"' + initializer.Text + '"'}";
        }
        
        if (initializer.Kind == SyntaxKind.NumericLiteral)
        {
            return $"{name}: {initializer.Text}";
        }
        
        if (initializer.Kind == SyntaxKind.JsxExpression)
        {
            return $"{name}: {ClearConnectedValue(scope.TsCode.Substring(initializer.Pos, initializer.End-initializer.Pos))}";
        }
        
        return new ArgumentException($"Unsupported initializer kind: {initializer.Kind}");
    }

    static Result<JsxElementDto> FindReturnJsxStatement(TsNode node, Scope scope)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Kind == SyntaxKind.ReturnStatement)
        {
            return ParseJsx(node.Expression, scope);
        }

        foreach (var child in GetAllChildren(node))
        {
            var res = FindReturnJsxStatement(child, scope);
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

    static Result<JsxElementDto> ParseJsx(TsNode node, Scope scope)
    {
        if (node == null || node.ContainsOnlyTriviaWhiteSpaces)
        {
            return Result.From<JsxElementDto>(null);
        }

        if (node.Kind == SyntaxKind.JsxText)
        {
            return new JsxElementDto
            {
                Tag = "#text",
                Properties = [$"{Design.Content}: {node.Text}"]
            };
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
                        var result = AsDesignerPropText(prop, scope);
                        if (result.HasError)
                        {
                            return result.Error;
                        }
                        element.Properties = element.Properties.Add(result.Value);
                    }
                }
            }

            
            foreach (var child in GetAllChildren(node))
            {
                var childJsx = ParseJsx(child, scope);
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
            var jsx = ParseJsx(node.ThenStatement, scope);
            if (jsx != null)
            {
                jsx.Value.Properties = jsx.Value.Properties.Add( Design.ShowIf + ":" + GetText(node.Condition));
                return jsx;
            }
        }

        return Result.From<JsxElementDto>(null);
    }

    static Result<IReadOnlyList<MethodResult>> Traverse(TsNode node, Scope scope)
    {
        if (node is null)
        {
            return Result.From<IReadOnlyList<MethodResult>>([]);
        }

       

        if (node.Kind == SyntaxKind.FunctionDeclaration || node.Kind == SyntaxKind.MethodDeclaration)
        {
            scope = scope with
            {
                MethodName = node.Name?.EscapedText
            };
        }

        List<MethodResult> results = [];

        // RETURN JSX
        if (node.Kind == SyntaxKind.ReturnStatement && node.Expression is not null)
        {
            var jsx = ParseJsx(node.Expression.Expression ?? node.Expression, scope);

            if (jsx is not null && scope.MethodName is not null)
            {
                results.Add(new MethodResult
                {
                    MethodName = scope.MethodName,
                    RootElement   = jsx.Value
                });
            }
        }

        // CONDITIONAL RENDER (if)
        if (node.Kind == SyntaxKind.IfStatement)
        {
            var conditionText = GetText(node.Condition);

            if (node.ThenStatement is not null)
            {
                var jsx = FindReturnJsxStatement(node.ThenStatement, scope);
                if (jsx is not null && scope.MethodName is not null)
                {
                    jsx.Value.Properties = jsx.Value.Properties.Add( Design.ShowIf + ":" + conditionText);
                    results.Add(new MethodResult
                    {
                        MethodName = scope.MethodName,
                        RootElement   = jsx.Value
                    });
                }
            }
        }

        // RECURSION: alt sonuçları birleştir
        foreach (var child in GetAllChildren(node))
        {
            var result = Traverse(child, scope);
            if (result.HasError)
            {
                return result.Error;
            }

            results.AddRange(result.Value);
        }

        return results.AsReadOnly();
    }
}
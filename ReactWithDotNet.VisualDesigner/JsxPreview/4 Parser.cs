using ReactWithDotNet.VisualDesigner.PropertyDomain;
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

    static Result<ParsedProperty> AsDesignerPropText(TsNode jsxAttribute, Scope scope)
    {
        var name = jsxAttribute.Name.EscapedText;

        var initializer = jsxAttribute.Initializer;
        
        if (initializer.Kind == SyntaxKind.StringLiteral)
        {
            return TryCreateProperty(name, '"' + initializer.Text + '"');
        }
        
        if (initializer.Kind == SyntaxKind.NumericLiteral)
        {
            return TryCreateProperty(name, initializer.Text);
        }
        
        if (initializer.Kind == SyntaxKind.JsxExpression)
        {
            return TryCreateProperty(name, ClearConnectedValue(scope.TsCode.Substring(initializer.Pos, initializer.End-initializer.Pos)));
        }
        
        return new ArgumentException($"Unsupported initializer kind: {initializer.Kind}");
    }

    static Result<VisualElementModel> FindReturnJsxStatement(TsNode node, Scope scope)
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

    static Result<VisualElementModel> ParseJsx(TsNode node, Scope scope)
    {
        if (node == null || node.ContainsOnlyTriviaWhiteSpaces || node.Kind == SyntaxKind.FalseKeyword)
        {
            return Result.Success<VisualElementModel>(null);
        }

        if (node.Kind == SyntaxKind.JsxText)
        {
            return new VisualElementModel
            {
                Tag = "#text",
                Properties = [$"{Design.Content}: {node.Text}"]
            };
        }

        if (node.Kind == SyntaxKind.JsxFragment)
        {
            var element = new VisualElementModel
            {
                Tag = "Fragment"
            };

            foreach (var child in GetAllChildren(node))
            {
                var childJsx = ParseJsx(child, scope);
                if (childJsx.HasError)
                {
                    return childJsx.Error;
                }
                
                if (childJsx.Value is not null)
                {
                    element = element with
                    {
                        Children = element.Children.Add(childJsx.Value)
                    };
                }
                
            }
            
            return element;
        }
        
        // JSX ELEMENT
        if (node.Kind == SyntaxKind.JsxElement || node.Kind == SyntaxKind.JsxSelfClosingElement)
        {
            var openingElement = node.OpeningElement;

            VisualElementModel element;

            if (openingElement is not null)
            {
                element = new VisualElementModel
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

                        if (result.Value.Name == "style")
                        {
                            foreach (var styleAttribute in ClearConnectedValue(result.Value.Value).Split(";"))
                            {
                                var s = ParseStyleAttribute(styleAttribute);
                                if (s is not null)
                                {
                                    element = element with
                                    {
                                        Styles = element.Styles.Add(styleAttribute)
                                    };
                                }
                            }

                            continue;
                            
                        }
                        element = element with
                        {
                            Properties = element.Properties.Add(result.Value.ToNameValueCombined())
                        };
                    }
                }
            }
            else
            {
                return Result.Success<VisualElementModel>(null);
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
                    element = element with
                    {
                        Children = element.Children.Add(childJsx.Value)
                    };
                }
                
            }

            return element;
        }

        // CONDITIONAL JSX (ternary)
        if (node.Kind == SyntaxKind.ConditionalExpression)
        {
            var result = ParseJsx(node.ThenStatement, scope);
            if (result.HasError)
            {
                return result.Error;
            }

            var jsx = result.Value;
            
            if (jsx != null)
            {
                return jsx with
                {
                    Properties = jsx.Properties.Add(Design.ShowIf + ":" + GetText(node.Condition))
                };
            }
        }

        return Result.Success<VisualElementModel>(null);
    }

    static Result<IReadOnlyList<MethodResult>> Traverse(TsNode node, Scope scope)
    {
        if (node is null)
        {
            return Result.Success<IReadOnlyList<MethodResult>>([]);
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
                var result = FindReturnJsxStatement(node.ThenStatement, scope);
                if (result.HasError)
                {
                    return result.Error;
                }

                var jsx = result.Value;
                if (jsx is not null && scope.MethodName is not null)
                {
                    jsx = jsx with
                    {
                        Properties = jsx.Properties.Add(Design.ShowIf + ":" + conditionText)
                    };
                    
                    results.Add(new MethodResult
                    {
                        MethodName = scope.MethodName,
                        RootElement   = jsx
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
using System.Text.Json;
using static ReactWithDotNet.VisualDesigner.Test.SyntaxKind;

namespace ReactWithDotNet.VisualDesigner.Test;



sealed record TsNode
{
    public int Pos { get; init; }
    
    public int End { get; init; }
    
    public int Kind { get; init; }

    public TsNode Name { get; init; }

    public string EscapedText { get; init; }
    
    public string Text { get; init; }

    public TsNode Expression { get; init; }

    public List<TsNode> Arguments { get; init; }

    public List<TsNode> Children { get; init; }

    public List<TsNode> Statements { get; init; }

    public TsNode Body { get; init; }

    public TsNode ThenStatement { get; init; }

    public TsNode ElseStatement { get; init; }
   
    public TsNode Condition { get; init; }

    public TsNode TagName { get; init; }

    public List<TsNode> Attributes { get; init; }
}

static class SyntaxKind
{
    public const int ReturnStatement = 254;
    public const int FunctionDeclaration = 263;
    public const int MethodDeclaration = 264;
    public const int IfStatement = 265;
    public const int JsxElement = 266;
    public const int JsxSelfClosingElement = 267;
    public const int ConditionalExpression = 3;
}



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



[TestClass]
public class NodeJsBridgeTest
{
    
    
    static string GetText(TsNode node)
    {
        return node?.Text ?? node?.Name?.Text;
    }


 
    public List<MethodResult> Extract(string json)
    {
        var root = JsonSerializer.Deserialize<TsNode>(json);
        var results = new List<MethodResult>();

        Traverse(root, results, null);

        return results;
    }

    private void Traverse(TsNode node, List<MethodResult> results, string currentMethod)
    {
        if (node == null) return;

        // METHOD YAKALA
        if (node.Kind == FunctionDeclaration || node.Kind == MethodDeclaration)
        {
            currentMethod = node.Name?.Text;
        }

        // RETURN JSX
        if (node.Kind == ReturnStatement && node.Expression != null)
        {
            var jsx = ParseJsx(node.Expression);

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
        if (node.Kind == IfStatement)
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
        if (node.Kind == JsxElement || node.Kind == JsxSelfClosingElement)
        {
            var element = new JsxElementDto
            {
                Tag = node.TagName?.Text
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
        if (node.Kind == ConditionalExpression)
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

    
    [TestMethod]
    public async Task KebabToCamelCaseTest()
    {
        const string tsCode =
            """
            function greet()
            {
                const x = 5;");
                
                return  (
                  <div>
                    <h1>Hello, world!</h1>
                  </div>
                );
            }
            """;
        
        var ast = await NodeJsBridge.Ast(tsCode);

        ast.HasError.ShouldBeFalse();

        var tsNode = JsonSerializer.Deserialize<TsNode>(ast.Value, JsonSerializerOptions.Web);

        tsNode.Statements[0].Name.EscapedText.ShouldBe("greet");
    }
}
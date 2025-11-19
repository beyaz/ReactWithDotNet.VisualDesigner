using Newtonsoft.Json.Linq;

namespace ReactWithDotNet.VisualDesigner.TsxAnalyze;

public static class TsxWalker
{
    public static TsxAnalysisResult AnalyzeAst(JObject ast)
    {
        var result = new TsxAnalysisResult();
        Walk(ast, result);
        return result;
    }

    private static void Walk(JToken node, TsxAnalysisResult result)
    {
        if (node == null) return;

        if (node.Type == JTokenType.Object)
        {
            var obj = (JObject)node;

            // FunctionDeclaration
            if (obj.TryGetValue("kind", out var kind))
            {
                int kindValue = kind.Value<int>();

                if (kindValue == (int)SyntaxKind.FunctionDeclaration)
                    ExtractFunctionName(obj, result);

                if (kindValue == (int)SyntaxKind.VariableDeclaration)
                    ExtractUseState(obj, result);
            }

            foreach (var prop in obj.Properties())
                Walk(prop.Value, result);
        }
        else if (node.Type == JTokenType.Array)
        {
            foreach (var item in (JArray)node)
                Walk(item, result);
        }
    }

    private static void ExtractFunctionName(JObject node, TsxAnalysisResult result)
    {
        var name = node["name"]?["escapedText"]?.Value<string>();
        if (!string.IsNullOrEmpty(name))
            result.Functions.Add(name);
    }

    private static void ExtractUseState(JObject node, TsxAnalysisResult result)
    {
        var initializer = node["initializer"];
        if (initializer == null) return;

        var expr = initializer["expression"]?["escapedText"]?.Value<string>();
        if (expr != "useState") return;

        // Left-hand destructuring
        var elements = node["name"]?["elements"] as JArray;
        if (elements == null || elements.Count < 2) return;

        var stateName = elements[0]?["name"]?["escapedText"]?.Value<string>();
        var setterName = elements[1]?["name"]?["escapedText"]?.Value<string>();

        string type = "any";
        var typeArgs = initializer["typeArguments"] as JArray;
        if (typeArgs != null && typeArgs.Count > 0)
        {
            type = ReadTypeNode(typeArgs[0]);
        }

        result.States.Add(new StateInfo
        {
            State  = stateName,
            Setter = setterName,
            Type   = type
        });
    }

    private static string ReadTypeNode(JToken node)
    {
        if (node == null) return "any";

        int kind = node["kind"]?.Value<int>() ?? 0;

        // Basit tipler
        switch (kind)
        {
            case (int)SyntaxKind.NumberKeyword:
                return "number";
            case (int)SyntaxKind.StringKeyword:
                return "string";
            case (int)SyntaxKind.BooleanKeyword:
                return "boolean";
            case (int)SyntaxKind.TypeLiteral:
                return "dictionary";
            default:
                return "unknown";
        }
    }

    // TS SyntaxKind sabitleri (TS 5.0+ uyumlu)
    private enum SyntaxKind
    {
        Unknown = 0,
        FunctionDeclaration = 241,
        VariableDeclaration = 224,
        NumberKeyword = 70,
        StringKeyword = 71,
        BooleanKeyword = 72,
        TypeLiteral = 217
    }
}
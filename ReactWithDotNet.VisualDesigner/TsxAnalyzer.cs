namespace ReactWithDotNet.VisualDesigner;


using Jint;
using Jint.Native;
using Jint.Native.Object;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Net.WebRequestMethods;

public class TsxAnalysisResult
{
    public List<string> Functions { get; set; } = new();
    public List<StateInfo> States { get; set; } = new();
}

public class StateInfo
{
    public string State { get; set; }
    public string Setter { get; set; }
    public string Type { get; set; }
}

public class TsxAnalyzer
{
    private readonly Engine _engine;
    private readonly ObjectInstance _ts;
    private readonly ObjectInstance _syntaxKind;

    public TsxAnalyzer(string typeScriptCompilerPath)
    {
        _engine = new Engine(cfg => cfg.LimitRecursion(5000));

        // TS Compiler API yükle
        var tsCode = System.IO.File.ReadAllText(typeScriptCompilerPath);
        _engine.Execute(tsCode);

        // TS namespace alınır
        _ts = _engine.GetValue("ts").AsObject();
        _syntaxKind = _ts.Get("SyntaxKind").AsObject();
    }

    private double KindOf(string name)
        => _syntaxKind.Get(name).AsNumber();

    public TsxAnalysisResult Analyze(string tsxText)
    {
        var ast = ParseToAst(tsxText);
        var result = new TsxAnalysisResult();

        Walk(ast, result);

        return result;
    }

    private JsValue ParseToAst(string code)
    {
        // Backtick kaçışını düzgün yap
        string safeCode = code.Replace("`", "\\`");

        string js = $@"
        ts.createSourceFile(
            'file.tsx',
            `{safeCode}`,
            ts.ScriptTarget.Latest,
            true,
            ts.ScriptKind.TSX
        )
    ";

        // AST doğrudan evaluate ile döner
        var result = _engine.Evaluate(js);
        return result;
    }



    // ------------------------
    // AST Walking
    // ------------------------

    private void Walk(JsValue node, TsxAnalysisResult result)
    {
        if (!node.IsObject()) return;

        var obj = node.AsObject();

        var kindProp = obj.Get("kind");
        if (kindProp.IsNumber())
        {
            double kind = kindProp.AsNumber();

            if (kind == KindOf("FunctionDeclaration"))
                ExtractFunctionName(obj, result);

            if (kind == KindOf("VariableDeclaration"))
                ExtractUseState(obj, result);
        }

        foreach (var prop in obj.GetOwnProperties())
        {
            var val = obj.Get(prop.Key);

            if (val.IsObject())
                Walk(val, result);

            else if (val.IsArray())
            {
                var arr = val.AsArray();
                var length = (int)arr.Length;

                for (int i = 0; i < length; i++)
                {
                    var element = arr.Get(i);   // JsValue
                    Walk(element, result);
                }
            }
        }
    }

    // ------------------------
    // Function name extraction
    // ------------------------

    private void ExtractFunctionName(ObjectInstance node, TsxAnalysisResult result)
    {
        var name = node.Get("name");
        if (name.IsObject())
        {
            var n = name.AsObject().Get("escapedText");
            if (n.IsString())
                result.Functions.Add(n.AsString());
        }
    }

    // ------------------------
    // useState extraction
    // ------------------------

    private void ExtractUseState(ObjectInstance node, TsxAnalysisResult result)
    {
        var initializer = node.Get("initializer");
        if (!initializer.IsObject()) return;

        var initObj = initializer.AsObject();
        var expr = initObj.Get("expression");

        // MUST be:   useState(...)
        if (!expr.IsObject()) return;
        if (expr.AsObject().Get("escapedText")?.AsString() != "useState")
            return;

        // Left-hand destructuring: const [x, setX] = useState(...)
        var nameNode = node.Get("name");
        if (!nameNode.IsObject()) return;

        var arr = nameNode.AsObject().Get("elements");
        if (!arr.IsArray()) return;

        // GetOwnProperties → PropertyDescriptor dictionary
        var elements = arr.AsArray()
            .GetOwnProperties()
            .OrderBy(x => int.Parse(x.Key.ToString()))
            .ToList();

        if (elements.Count < 2) return;

        // Extract state name
        var stateName = elements[0].Value.Value   // PropertyDescriptor → JsValue
            .AsObject()
            .Get("name").AsObject()
            .Get("escapedText").AsString();

        // Extract setter name
        var setterName = elements[1].Value.Value
            .AsObject()
            .Get("name").AsObject()
            .Get("escapedText").AsString();

        // Type: useState<number>() → typeArguments[0]
        string type = "any";

        var typeArgs = initObj.Get("typeArguments");
        if (typeArgs.IsArray() && typeArgs.AsArray().Length > 0)
        {
            var typeNode = typeArgs.AsArray().Get("0");
            type = ReadTypeNode(typeNode);
        }

        result.States.Add(new StateInfo
        {
            State  = stateName,
            Setter = setterName,
            Type   = type
        });
    }


    // ------------------------
    // Type Node Reader
    // ------------------------

    private string ReadTypeNode(JsValue node)
    {
        if (!node.IsObject()) return "any";

        var kind = node.AsObject().Get("kind").AsNumber();

        if (kind == KindOf("NumberKeyword"))
            return "number";

        if (kind == KindOf("StringKeyword"))
            return "string";

        if (kind == KindOf("BooleanKeyword"))
            return "boolean";

        // Object literal → dictionary
        if (kind == KindOf("TypeLiteral"))
            return "dictionary";

        return "unknown";
    }
}




//var analyzer = new TsxAnalyzer("typescript.js");

//string code = @"
//function Test() {
//  const [count, setCount] = useState<number>(0);
//  const [user, setUser] = useState({
//    name: 'Ali',
//    age: 25
//  });

//  function increase() {}
//  const hello = () => {};
//}
//";

//var result = analyzer.Analyze(code);

//Console.WriteLine("Functions:");
//foreach (var fn in result.Functions)
//    Console.WriteLine("  " + fn);

//Console.WriteLine("\nStates:");
//foreach (var s in result.States)
//    Console.WriteLine($"  {s.State} : {s.Type}");


//using var http = new HttpClient();
//var tsCode = await http.GetStringAsync("https://unpkg.com/typescript@5.1.6/lib/typescript.js");
//_engine.Execute(tsCode);
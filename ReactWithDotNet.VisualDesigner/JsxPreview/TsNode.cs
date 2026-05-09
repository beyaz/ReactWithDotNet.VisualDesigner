using Mysqlx.Expr;
using System.Text.Json;
using System.Text.Json.Serialization;
using static ReactWithDotNet.VisualDesigner.JsxPreview.SyntaxKind;

namespace ReactWithDotNet.VisualDesigner.JsxPreview;

public class SingleOrArrayConverter<T> : JsonConverter<List<T>>
{
    public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<T>();

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // normal array
            list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            // single object → wrap
            var item = JsonSerializer.Deserialize<T>(ref reader, options);
            list.Add(item);
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

sealed record TsNode
{
    public int Pos { get; init; }
    
    public int End { get; init; }
    
    public int Kind { get; init; }

    public TsNode Name { get; init; }

    public string EscapedText { get; init; }
    
    public TsNode OpeningElement { get; init; }
    
    public TsNode ClosingElement { get; init; }

    
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

    [JsonConverter(typeof(SingleOrArrayConverter<TsNode>))]
    public List<TsNode> Attributes { get; init; }

}
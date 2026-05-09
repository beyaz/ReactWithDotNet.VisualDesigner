using System.Text.Json;
using ReactWithDotNet.VisualDesigner.JsxPreview;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class JsxPreviewTests
{
    [TestMethod]
    public async Task TestSimpleReturnStatement()
    {
        const string tsCode =
            """
            function greet()
            {
                const x = 5;
                
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

        Parser.Extract(ast.Value);
    }
}
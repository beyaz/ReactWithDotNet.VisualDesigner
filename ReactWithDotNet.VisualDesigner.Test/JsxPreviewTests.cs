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
                  <div id="container" name='abc' p1={4} p2={yy ? 'a' : 'b'}>
                    <h1>Hello, world!</h1>
                  </div>
                );
            }
            """;

        

        var result = await Parser.Extract(tsCode);
        
        result.Value.Count.ShouldBe(1);
    }
}
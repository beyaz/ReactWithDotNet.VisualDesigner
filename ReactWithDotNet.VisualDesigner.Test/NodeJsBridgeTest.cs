namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class NodeJsBridgeTest
{
    [TestMethod]
    public async Task KebabToCamelCaseTest()
    {
        var ast = await NodeJsBridge.Ast("""
                                         const x = 5;");

                                         return  (
                                           <div>
                                             <h1>Hello, world!</h1>
                                             </div>
                                         );
                                         """);

        ast.Value.ToString();
    }
}
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
                    <h1>Hello, world! <span>xyz</span></h1>
                  </div>
                );
            }

            """;

        var result = await Parser.Extract(tsCode);

        result.Value.Count.ShouldBe(1);

        var actualRootElement = result.Value[0].RootElement;


        var actual = SerializeToYaml(actualRootElement);


        var expected = """
                       tag: div
                       properties:
                       - 'id: "container"'
                       - 'name: "abc"'
                       - 'p1: 4'
                       - "p2: yy ? 'a' : 'b'"
                       children:
                       - tag: h1
                         children:
                         - tag: '#text'
                           properties:
                           - 'd-content: Hello, world! '
                         - tag: span
                           children:
                           - tag: '#text'
                             properties:
                             - 'd-content: xyz'

                       """;

        actual.Trim().ShouldBeEquivalentTo(expected.Trim());
    }
}
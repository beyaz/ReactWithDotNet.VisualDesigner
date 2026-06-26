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

        VisualElementModel expectedRoot = new()
        {
            Tag        = "div",
            Properties = ["id=\"container\"", "name='abc'", "p1={4}", "p2={yy ? 'a' : 'b'}"],
            Children =
            [
                new()
                {
                    Tag = "h1",
                    Children =
                    [
                        new()
                        {
                            Tag        = "#text",
                            Properties = ["d-content: Hello, world! "]
                        },
                        new()
                        {
                            Tag = "span",
                            Children =
                            [
                                new()
                                {
                                    Tag        = "#text",
                                    Properties = ["d-content: xyz"]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        actualRootElement.ShouldBeEquivalentTo(expectedRoot);
    }
}
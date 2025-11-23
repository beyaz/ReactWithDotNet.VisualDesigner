
namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class ExporterTest
{
    [TestMethod]
    public async Task Export_as_tailwind()
    {
        var input = new VisualElementModel
        {
            Tag        = "div",
            Styles     = ["color: red", "background: blue",  "hover:background: yellow"],
            Properties = [$"{Design.Content}: | Abc"]
        };

        string[] expected =
        [
            "    <div className=\"text-[red] bg-[blue] hover:bg-[yellow]\">",
            "      Abc",
            "    </div>"
        ];

        await act(input, expected);

        return;

        static async Task act(VisualElementModel visualElementModel, IReadOnlyList<string> expected)
        {
            
            var componentScope = new ComponentScope
            {
                ProjectConfig = new ProjectConfig
                {
                    ExportStylesAsTailwind = true
                },
                ComponentId       = 0,
                ProjectId         = 0,
                ComponentConfig   = new ComponentConfig(),
                OutFile           = (null,null),
                RootVisualElement = visualElementModel
            };
            
            

            var result = await TsxExporter.CalculateElementTreeSourceCodes(componentScope, visualElementModel);

            result.HasError.ShouldBeFalse();

            result.Value.elementTreeSourceLines.ShouldBe(expected);
        }
    }
}
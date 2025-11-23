
namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class CSharpExporterTest
{
   
    [TestMethod]
    public async Task Export_as_csharp()
    {
        var input = new VisualElementModel
        {
            Tag        = "div",
            Styles     = ["color: red", "background: rgb(2,3,6)", "hover:color:blue"],
            Properties = [$"{Design.Content}: | Abc"]
        };

        string[] expected =
        [
            "new div(Color(Red), Background(rgb(2,3,6)), Hover(Color(Blue)))",
            "{",
            "    Abc",
            "}"
        ];

        await act(input, expected);
        
        return;

        static async Task act(VisualElementModel visualElementModel, IReadOnlyList<string> expected)
        {
            var componentScope = new ComponentScope
            {
                ProjectConfig = new ProjectConfig
                {
                    ExportAsCSharp = true
                },
                ComponentId = 0,
                ProjectId = 0,
                ComponentConfig = new ComponentConfig(),
                OutFile = (null,null),
                RootVisualElement = visualElementModel
            };
            

            var result = await CSharpExporter.CalculateElementTreeSourceCodes(componentScope, visualElementModel);

            result.HasError.ShouldBeFalse();

            result.Value.elementTreeSourceLines.Select(x=>x.Trim()).ShouldBe(expected.Select(x=>x.Trim()));
        }
    }
}
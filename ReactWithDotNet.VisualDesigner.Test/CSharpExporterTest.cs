using ReactWithDotNet.VisualDesigner.Exporters;

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
            Properties = ["d-text: | Abc"]
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
            var project = new ProjectConfig
            {
                ExportAsCSharp = true
            };

            var result = await CSharpExporter.CalculateElementTreeSourceCodes(project,new Dictionary<string, string>(), visualElementModel);

            result.Success.ShouldBeTrue();

            result.Value.elementJsxTree.Select(x=>x.Trim()).ShouldBe(expected.Select(x=>x.Trim()));
        }
    }
}
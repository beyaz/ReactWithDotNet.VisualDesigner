using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class ExporterTest
{
    [TestMethod]
    public async Task Export()
    {
        var input = new VisualElementModel
        {
            Tag        = "div",
            Styles     = ["color: red", "bg: blue"],
            Properties = ["d-text: | Abc"]
        };

        string[] expected =
        [
            "    <div className=\"text-[red] bg-[blue]\">",
            "      Abc",
            "    </div>"
        ];

        await act(input, expected);

        return;

        static async Task act(VisualElementModel visualElementModel, IReadOnlyList<string> expected)
        {
            var project = new ProjectConfig
            {
                ExportStylesAsTailwind = true
            };

            var result = await TsxExporter.CalculateElementTreeTsxCodes(project,new Dictionary<string, string>(), visualElementModel);

            result.Success.ShouldBeTrue();

            result.Value.elementJsxTree.ShouldBe(expected);
        }
    }
}
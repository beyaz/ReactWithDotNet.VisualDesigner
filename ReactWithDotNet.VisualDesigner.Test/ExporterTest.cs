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
                Styles = new Dictionary<string, string>
                {
                    { "ABC", "font-size: 16px; font-family: \"Host Grotesk\"; font-weight: 400; line-height: 24px;" }
                }
            };

            var result = await NextJs_with_Tailwind.CalculateElementTreeTsxCodes(project, visualElementModel);

            result.Success.ShouldBeTrue();

            result.Value.ShouldBe(expected);
        }
    }
}
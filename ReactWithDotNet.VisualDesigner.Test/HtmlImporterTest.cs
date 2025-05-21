using FluentAssertions;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class HtmlImporterTest
{
    [TestMethod]
    public void ShouldImportWithCssClasses()
    {

        var html = """
                   <div style="color: #222530; font-size: 16px; font-family: Host Grotesk; font-weight: 400; line-height: 24px; word-wrap: break-word">Phuket, Thailand</div>
                   """;

        var project = new ProjectConfig
        {
            Styles = new Dictionary<string, string>
            {
                { "ABC", "font-size: 16px; font-family: Host Grotesk; font-weight: 400; line-height: 24px;" }
            }
        };

        var model = HtmlImporter.ConvertToVisualElementModel(project, html);
        model.Styles.Should().AllBeEquivalentTo(new string[]
        {
            "color: #222530",
            "word-wrap: break-word",
            "ABC"
        });



    }
}
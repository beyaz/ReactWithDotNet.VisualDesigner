using FluentAssertions;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class NextJsExportTest
{
    [TestMethod]
    public async Task ExportAll()
    {
        (await NextJs_with_Tailwind.ExportAll(1)).Success.Should().BeTrue();
    }

    [TestMethod]
    public async Task FixAll()
    {
        foreach (var record in await Store.GetAllComponentsInProject(1))
        {
            var map = DeserializeFromYaml<Dictionary<string, string>>(record.ConfigAsYaml);

            var name = map["name"];
            
            var exportFilePath = map["exportFilePath"];

            await Store.Update(record with
            {
                ConfigAsYaml = SerializeToYaml(new Dictionary<string, string>()
                {
                    { "Name", name },
                    { "ExportFilePath", exportFilePath }
                })
            });

        }
    }
}
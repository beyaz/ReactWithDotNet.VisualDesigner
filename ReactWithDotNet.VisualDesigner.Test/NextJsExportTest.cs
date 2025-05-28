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
        await DbOperation(async db =>
        {
            foreach (var record in db.GetAll<ComponentEntity>())
            {

                string outputFilePath = string.Empty;
                string name = string.Empty;
                {
                    var path = record.Name + ".txt";

                    var fileName = Path.GetFileNameWithoutExtension(path);
                
                    if (fileName.Contains("."))
                    {
                        outputFilePath = Path.GetDirectoryName(path).Replace('\\','/') + "/" + fileName.Split('.', StringSplitOptions.RemoveEmptyEntries).First() + ".tsx";
                        name           = fileName.Split('.', StringSplitOptions.RemoveEmptyEntries).Last();
                    }
                    else
                    {
                        name = fileName;
                        outputFilePath = Path.GetDirectoryName(path).Replace('\\','/') + "/" + fileName + ".tsx";
                    }
                }
                
                
               
                

                await db.UpdateAsync(record with
                {
                    ConfigAsYaml = SerializeToYaml(new Dictionary<string,string>
                    {
                        {"name", name},
                        {"exportFilePath", outputFilePath}
                    })
                });
            }
        });
        return;

        static async Task fix(VisualElementModel model)
        {
            var index = model.Styles.IndexOf("border: 1px solid border-sub");
            if (index >= 0)
            {
                model.Styles.Insert(index, "border-sub");
                model.Styles.Insert(index, "border");

                model.Styles.Remove("border: 1px solid border-sub");
            }

            foreach (var child in model.Children)
            {
                await fix(child);
            }
        }
    }
}
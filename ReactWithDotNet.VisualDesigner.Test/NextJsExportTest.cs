using Dapper.Contrib.Extensions;
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
    public void FixAll()
    {
        DbOperation(db =>
        {
            foreach (var record in db.GetAll<ComponentEntity>())
            {
                db.Update(record with
                {
                    RootElementAsYaml = SerializeToYaml(DeserializeFromJson<VisualElementModel>(record.RootElementAsJson)),
                    RootElementAsJson = null
                });
            }
        });

        DbOperation(db =>
        {
            foreach (var record in db.GetAll<ComponentHistoryEntity>())
            {
                db.Update(record with
                {
                    RootElementAsYaml = SerializeToYaml(DeserializeFromJson<VisualElementModel>(record.RootElementAsJson)),
                    RootElementAsJson = null
                });
            }
        });
    }
}
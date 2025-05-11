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
                var root = DeserializeFromYaml<VisualElementModel>(record.RootElementAsYaml);

                await fix(root);

                await db.UpdateAsync(record with
                {
                    RootElementAsYaml = SerializeToYaml(root)
                });
            }
        });

        static async Task fix(VisualElementModel model)
        {
            var cmp = await TryFindComponentByComponentName(1, model.Tag);
            if (cmp.HasError)
            {
                throw cmp.Error;
            }

            if (cmp.Value is not null)
            {
                model.Tag = cmp.Value.Id.ToString();
            }

            foreach (var child in model.Children)
            {
                await fix(child);
            }
        }
    }
}
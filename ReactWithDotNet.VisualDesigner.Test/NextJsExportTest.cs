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
        return;

        static async Task fix(VisualElementModel model)
        {
            for (var i = 0; i < model.Styles.Count; i++)
            {
                model.Styles[i] = fixStyle(model.Styles[i]);
            }

            foreach (var child in model.Children)
            {
                await fix(child);
            }

            return;

            static string fixStyle(string style)
            {
                var map = new Dictionary<string, string>
                {
                    { "-L", "-l" },
                    { "-XL", "-xl" },
                    { "-XXL", "-xxl" },
                    { "-M", "-m" },
                    { "-S", "-s" },
                    { "-XS", "-xs" },
                };

                foreach (var (key, value) in map)
                {
                    if (style.EndsWith(key))
                    {
                        return style.RemoveFromEnd(key) + value;
                    }
                }

                return style;
            }
        }
    }
}
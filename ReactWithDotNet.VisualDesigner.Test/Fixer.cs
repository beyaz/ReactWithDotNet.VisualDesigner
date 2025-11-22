namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class Fixer
{
    [TestMethod]
    public static async Task FixAll()
    {
        Configuration.Extensions.Config = Configuration.Extensions.ReadConfig().Value;

        const int projectId = 1;

        foreach (var component in await Store.GetAllComponentsInProject(projectId))
        {
            var visualElementModel = DeserializeFromYaml<VisualElementModel>(component.RootElementAsYaml);

            await Store.Update(component with
            {
                RootElementAsYaml = SerializeToYaml(Fix(visualElementModel))
            });
        }
    }

    static VisualElementModel Fix(VisualElementModel model)
    {
        model = model with
        {
            Properties = ChangePropName(model.Properties, "d-text", Design.Text)
        };

        var children = model.Children;

        for (var i = 0; i < children.Count; i++)
        {
            children = children.SetItem(i, Fix(children[i]));
        }

        return model with { Children = children };

        static IReadOnlyList<string> ChangePropName(IReadOnlyList<string> props, string propNameOld, string propNameNew)
        {
            var list = new List<string>();

            foreach (var prop in props)
            {
                var maybe = TryParseProperty(prop);
                if (maybe.HasValue && maybe.Value.Name == propNameOld)
                {
                    list.Add(propNameNew + ": " + maybe.Value.Value);

                    continue;
                }

                list.Add(prop);
            }

            return list;
        }
    }
}
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
    public async Task FixAll()
    {
        //var components = await GetAllComponentsInProject(1);

        //foreach (var component in components)
        //{
        //    component.RootElementAsJson = SerializeToJson(component.RootElementAsJson.AsVisualElementModel().Fix());

        //    DbOperation(db => db.Update(component));
        //}
    }
    
   
}
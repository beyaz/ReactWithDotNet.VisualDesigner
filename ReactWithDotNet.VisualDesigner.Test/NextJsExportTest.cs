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

    //[TestMethod]
    //public async Task FixAll()
    //{
    //    await DbOperation(async db =>
    //    {
    //        foreach (var record in db.GetAll<ComponentEntity>())
    //        {

               
    //        }
    //    });
    //}
}
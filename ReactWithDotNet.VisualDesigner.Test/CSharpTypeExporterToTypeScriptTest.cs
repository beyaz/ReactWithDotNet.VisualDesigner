using ReactWithDotNet.VisualDesigner.CSharpTypeExportingToTypeScript;

namespace ReactWithDotNet.VisualDesigner.Test;

class Abc
{
    public string str { get; set; }
}
[TestClass]
public sealed class CSharpTypeExporterToTypeScriptTest
{
    [TestMethod]
    public void Export_as_tailwind()
    {
        var path = typeof(CSharpTypeExporterToTypeScriptTest).Assembly.Location;
        
        var assembly = CecilAssemblyReader.ReadAssembly(path);

        var tsTypeDefinition = CSharpTypeExporterToTypeScript.CreateFrom(assembly.Value.MainModule.GetType(typeof(Abc).FullName));
        
        tsTypeDefinition.Fields.Count.ShouldBe(1);
    }
}
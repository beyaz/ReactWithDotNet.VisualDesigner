using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class DotNetModelExporterTest
{
    [TestMethod]
    public void ExportModels()
    {
        var assemblyFilePath = @"D:\work\BOA.BusinessModules\Dev\BOA.MobilePos\API\BOA.POSPortal.MobilePos.API\bin\Debug\net8.0\BOA.POSPortal.MobilePos.API.dll";

        var result = DotNetModelExporter.ExportModelsInAssembly(assemblyFilePath);

        result.Success.ShouldBeTrue();
    }
}
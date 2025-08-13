using Mono.Cecil;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner.Test;

class Model1
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public double DoubleValue { get; set; }
    
    public int? IdNullable { get; set; }
    public double? DoubleValueNullable { get; set; }
}

[TestClass]
public sealed class DotNetModelExporterTest
{
    static TypeDefinition GetTypeDefinition<T>()
    {
        var assemblyFilePath = typeof(DotNetModelExporterTest).Assembly.Location;
        
        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyFilePath);
        if (assemblyDefinition is null)
        {
            throw new ("AssemblyNotFound:" + assemblyFilePath);
        }
        return assemblyDefinition.MainModule.GetType(typeof(T).FullName) ?? throw new ();
    }

    [TestMethod]
    public void Export_simple_declerated_type()
    {
        var typeDefinition = GetTypeDefinition<Model1>();

        var result = DotNetModelExporter.GetTsCodes(typeDefinition);

        var tsCode = DotNetModelExporter.LinesToString(result);
        
        tsCode.ToString();
    }
    
    [TestMethod]
    public void ExportModels()
    {
        var assemblyFilePath = @"D:\work\BOA.BusinessModules\Dev\BOA.MobilePos\API\BOA.POSPortal.MobilePos.API\bin\Debug\net8.0\BOA.POSPortal.MobilePos.API.dll";

        var result = DotNetModelExporter.ExportModelsInAssembly(assemblyFilePath);

        result.Success.ShouldBeTrue();
    }
    
    [TestMethod]
    public void ExportModels2()
    {
        var assemblyFilePath = @"D:\work\BOA.BusinessModules\Dev\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\BOA.InternetBanking.Payments.API.dll";

        var result = DotNetModelExporter.ExportModelsInAssembly(assemblyFilePath);

        result.Success.ShouldBeTrue();
    }
}
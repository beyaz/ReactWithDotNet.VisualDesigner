namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class CecilHelperTest
{
    [TestMethod]
    public void GetPropertyPathList()
    {
        var assemblyFilePath = @"D:\work\BOA.BusinessModules\Dev\BOA.MobilePos\API\BOA.POSPortal.MobilePos.API\bin\Debug\net8.0\BOA.POSPortal.MobilePos.API.dll";

        var dotnetTypeFullName = "BOA.POSPortal.MobilePos.API.Types.TestFormClientRequest";

        var variableName = "request";

        CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsString);
    }
}
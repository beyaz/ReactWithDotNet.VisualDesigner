namespace ReactWithDotNet.VisualDesigner.Test;

public sealed  class SampleUserInfo
{
    public string Name { get; set; }
    public int Age { get; set; }
    public SampleAddressInfo Address { get; set; }
    public List<string> Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class SampleAddressInfo
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}


[TestClass]
public class CecilHelperTest
{
    [TestMethod]
    public void GetPropertyPathList()
    {
        var assemblyFilePath = typeof(CecilHelperTest).Assembly.Location;

        var dotnetTypeFullName = typeof(SampleUserInfo).FullName;

        const string variableName = "request";

        var propertyPathList = CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsString);
        
        propertyPathList.Count.ShouldBeGreaterThan(1);
    }
}
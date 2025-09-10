namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class HelperTest
{
    [TestMethod]
    public void KebabToCamelCaseTest()
    {
        KebabToCamelCase("abc-xyzT").ShouldBe("abcXyzT");
    }
}
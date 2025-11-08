namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public class Fixer
{

    [TestMethod]
    public void FixAll()
    {
        Configuration.Extensions.Config = Configuration.Extensions.ReadConfig().Value;

    }

}
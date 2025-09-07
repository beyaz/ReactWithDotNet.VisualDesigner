
namespace ReactWithDotNet.VisualDesigner.Test;

[TestClass]
public sealed class CssHelperTest
{
    [TestClass]
    public class CreateDesignerStyleItemFromText_Test
    {
        static DesignerStyleItem Call(string designerStyleItem)
        {
            var result = CreateDesignerStyleItemFromText(GetProjectConfig(1),designerStyleItem);

            result.Success.ShouldBeTrue();

            return result.Value;
        }
        
        [TestMethod]
        public void ShouldBeParseTailwindClass()
        {
            var value = Call("w-full");

            value.Pseudo.ShouldBeNull();
        
            value.RawHtmlStyles["width"].ShouldBe("100%");
        }
        
        [TestMethod]
        public void ShouldBeParseSimpleStyle()
        {
            var value = Call("width: 400px");
            
            value.Pseudo.ShouldBeNull();
        
            value.RawHtmlStyles["width"].ShouldBe("400px");
        }
        
        [TestMethod]
        public void ShouldBeParseSimpleStyle_with_pseudo()
        {
            var value = Call("hover: width: 400px");

            value.Pseudo.ShouldBe("hover");
        
            value.RawHtmlStyles["width"].ShouldBe("400px");
        }
        
        [TestMethod]
        public void ShouldBeParseTailwindColors()
        {
            var value = Call("bg: Gray300");
            
            value.RawHtmlStyles["background"].ShouldBe(Gray300);
        }
    }
 
   
}
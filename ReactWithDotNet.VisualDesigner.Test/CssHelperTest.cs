﻿using FluentAssertions;

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

            result.Success.Should().BeTrue();

            return result.Value;
        }
        
        [TestMethod]
        public void ShouldBeParseTailwindClass()
        {
            var value = Call("w-full");

            value.Pseudo.Should().BeNull();
        
            value.RawHtmlStyles["width"].Should().Be("100%");
        }
        
        [TestMethod]
        public void ShouldBeParseSimpleStyle()
        {
            var value = Call("width: 400px");
            
            value.Pseudo.Should().BeNull();
        
            value.RawHtmlStyles["width"].Should().Be("400px");
        }
        
        [TestMethod]
        public void ShouldBeParseSimpleStyle_with_pseudo()
        {
            var value = Call("hover: width: 400px");

            value.Pseudo.Should().Be("hover");
        
            value.RawHtmlStyles["width"].Should().Be("400px");
        }
    }
 
   
}
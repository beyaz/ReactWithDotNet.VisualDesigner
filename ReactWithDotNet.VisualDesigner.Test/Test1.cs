using FluentAssertions;
using static ReactWithDotNet.VisualDesigner.CssHelper;

namespace ReactWithDotNet.VisualDesigner.Test
{
    [TestClass]
    public sealed class CssHelperTest
    {
        [TestMethod]
        public void TailwindClassShouldBeParse()
        {
            TryConvertCssUtilityClassToHtmlStyle("w-full").HasValue.Should().BeTrue();
        }
    }
}

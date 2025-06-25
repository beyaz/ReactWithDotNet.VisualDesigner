namespace ReactWithDotNet.VisualDesigner;

class Plugin
{
    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }
}
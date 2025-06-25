namespace ReactWithDotNet.VisualDesigner;

class Plugin
{
    public static Element BeforePreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }
}
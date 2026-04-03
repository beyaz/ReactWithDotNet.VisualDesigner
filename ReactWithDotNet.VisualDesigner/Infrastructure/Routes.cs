global using  ReactWithDotNet.VisualDesigner.Infrastructure;

namespace ReactWithDotNet.VisualDesigner.Infrastructure;

static class Routes
{
    public const string Home = "/";
    
    public const string VisualDesigner = "/"+nameof(VisualDesigner);

    public const string VisualDesignerPreview = "/" + nameof(VisualDesignerPreview);
}
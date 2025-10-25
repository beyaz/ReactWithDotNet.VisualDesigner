global using  ReactWithDotNet.VisualDesigner.Infrastructure;

namespace ReactWithDotNet.VisualDesigner.Infrastructure;

static class Routes
{
    public const string VisualDesigner = "/";

    public const string VisualDesignerPreview = "/" + nameof(VisualDesignerPreview);

    public const string MainWindow = "/" + nameof(BDigitalFrameworkApiToTsExporter);
}
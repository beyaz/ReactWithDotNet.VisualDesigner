namespace ReactWithDotNet.VisualDesigner.Infrastructure;

sealed record PageRouteInfo(string Url, Type page);

static class Page
{
    public static readonly PageRouteInfo VisualDesigner = new("/", typeof(ApplicationView));
    public static readonly PageRouteInfo VisualDesignerPreview = new($"/{nameof(VisualDesignerPreview)}", typeof(ApplicationPreview));
}
namespace ReactWithDotNet.WebSite;

sealed record PageRouteInfo(string Url, Type page);

static class Page
{
    public static readonly PageRouteInfo VisualDesigner = new($"/{nameof(VisualDesigner)}", typeof(ApplicationView));
    public static readonly PageRouteInfo VisualDesignerPreview = new($"/{nameof(VisualDesignerPreview)}", typeof(ApplicationPreview));
}
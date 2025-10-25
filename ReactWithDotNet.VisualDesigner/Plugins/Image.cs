namespace ReactWithDotNet.VisualDesigner.Plugins;

[CustomComponent]
public sealed class Image : PluginComponentBase
{
    public string alt { get; set; }

    public string className { get; set; }

    public bool? fill { get; set; }

    public string height { get; set; }
    public string src { get; set; }

    public string width { get; set; }

    protected override Element render()
    {
        return new img
        {
            src       = src,
            alt       = alt,
            width     = width,
            height    = height,
            className = className
        } + When(fill is true, SizeFull);
    }

    [IsImage]
    public static Scope IsImage(Scope scope)
    {
        var element = Plugin.CurrentElementInstanceInPreview[scope];
        
        if (element is Image)
        {
            return Scope.Create(new()
            {
                { Plugin.IsImageKey, true }
            });
        }

        return Scope.Empty;
    }
    
    [TryGetIconForElementTreeNode]
    public static Scope TryGetIconForElementTreeNode(Scope scope)
    {
        var model = Plugin.VisualElementModel[scope];
        
        if (model.Tag == nameof(Link))
        {
            return Scope.Create(new()
            {
                { Plugin.IconForElementTreeNode, new IconImage() }
            });
        }

        return Scope.Empty;
    }
}
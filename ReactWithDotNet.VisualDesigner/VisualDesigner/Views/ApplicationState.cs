using System.Collections.Concurrent;

namespace ReactWithDotNet.VisualDesigner.Views;

public sealed class ApplicationPreviewInfo
{
    // @formatter:off
    
    public int Width { get; set; }
    
    public int Height { get; init; }
    
    public double Scale { get; set; }
    
    // @formatter:on
}

public sealed record ApplicationSelectionState
{
    // @formatter:off
      
    public string VisualElementTreeItemPath { get; init; }
    
    public string VisualElementTreeItemPathHover { get; set; }
    
    public int? StyleGroupIndex { get; init; }
    
    public int? PropertyIndexInStyleGroup { get; set; }
    
    public int? PropertyIndexInProps { get; set; }
    
    // @formatter:on
}

public sealed class ApplicationState
{
    // @formatter:off

    // APPLICATION STATE
    public required ApplicationPreviewInfo Preview { get; init; }
    
    public int ProjectId { get; set; }
    
    public string ComponentName { get; set; }
    
    public VisualElementModel ComponentRootElement { get; set; }
    
    public bool IsProjectSettingsPopupVisible { get; set; }
    
    public required ApplicationSelectionState Selection { get; set; }
    
    public string UserName { get; init; }
    
    public bool HtmlCodeVisible { get; set; }
    
    public string HtmlCode { get; init; }

    // @formatter:on
}

static class ApplicationStateMemoryCache
{
    internal static readonly ConcurrentDictionary<string, ApplicationState> ApplicationStateCache = new();

    public static ApplicationState GetUserLastState(string userName)
    {
        ApplicationStateCache.TryGetValue(userName, out var state);

        return state;
    }

    public static void SetUserLastState(ApplicationState state)
    {
        ApplicationStateCache.AddOrUpdate(state.UserName, state, (_, _) => state);
    }
}
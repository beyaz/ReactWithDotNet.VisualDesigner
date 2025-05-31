using System.Collections.Concurrent;

namespace ReactWithDotNet.VisualDesigner.Views;

public sealed record ApplicationPreviewInfo
{
    // @formatter:off
    
    public int Width { get; init; }
    
    public int Height { get; init; }
    
    public double Scale { get; init; }
    
    // @formatter:on
}

public sealed record ApplicationSelectionState
{
    // @formatter:off
      
    public string VisualElementTreeItemPath { get; init; }
    
    public string VisualElementTreeItemPathHover { get; init; }
    
    public int? SelectedStyleIndex { get; init; }
    
    public int? SelectedPropertyIndex { get; init; }
    
    // @formatter:on
}

public sealed record ApplicationState
{
    // @formatter:off

    // APPLICATION STATE
    public required ApplicationPreviewInfo Preview { get; init; }
    
    public int ProjectId { get; init; }
    
    public int ComponentId { get; init; }
    
    public VisualElementModel ComponentRootElement { get; init; }
    
    public bool IsProjectSettingsPopupVisible { get; init; }
    
    public required ApplicationSelectionState Selection { get; init; }
    
    public string UserName { get; init; }
    
    public string MainContentText { get; init; }
    
    public LeftTabs LeftTab  { get; init; }
    
    public MainContentTabs MainContentTab  { get; init; }
    
    public required AttibuteDragDropData StyleItemDragDrop { get; init; }
    
    public required AttibuteDragDropData PropertyItemDragDrop { get; init; }

    // @formatter:on
}

public sealed record AttibuteDragDropData
{
    public int? StartItemIndex { get; init; }
    public int? EndItemIndex { get; init; }
    public AttibuteDragPosition? Position { get; init; }
}

public enum AttibuteDragPosition
{
    Before,
    After
}

public enum LeftTabs
{
    Components, ElementTree
}

public enum MainContentTabs
{
    Design, Code, ProjectConfig, ImportHtml, ComponentConfig
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
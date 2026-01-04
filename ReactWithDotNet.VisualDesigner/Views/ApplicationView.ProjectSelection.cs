namespace ReactWithDotNet.VisualDesigner.Views;

sealed class ProjectSelectionView : Component<ProjectSelectionView.State>
{
    [CustomEvent]
    public required Func<Task> OnAddNewComponent { get; set; }

    [CustomEvent]
    public required Func<string, Task> OnChange { get; init; }

    public required string ProjectName { get; init; }

    public required IReadOnlyList<string> Suggestions { get; init; } 

    protected override Element render()
    {
        return new div(DisplayFlex, AlignItemsCenter, JustifyContentCenter, PositionRelative, Border(1, solid, "#d5d5d8"), BorderRadius(4), Height(36), WidthFitContent)
        {
            new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background("#eff3f8"), PaddingLeft(4), PaddingRight(4))
            {
                "Project"
            },
            new div(DisplayFlex, WidthFull, PaddingLeft(4), PaddingRight(4), AlignItemsCenter, Gap(4))
            {
                new div(OnClick(OnAddNewComponentClicked), DisplayFlex, JustifyContentCenter, AlignItemsCenter, Padding(3))
                {
                    new svg(ViewBox(0, 0, 16, 16), Width(16), Height(16), Fill("currentcolor"), Color(Gray300), Hover(Color(Gray600)), Width(20), Height(20))
                    {
                        new path(Fill("currentColor"), path.D("M12 8.667H8.667V12c0 .367-.3.667-.667.667A.669.669 0 0 1 7.333 12V8.667H4A.669.669 0 0 1 3.333 8c0-.367.3-.667.667-.667h3.333V4c0-.366.3-.667.667-.667.367 0 .667.3.667.667v3.333H12c.367 0 .667.3.667.667 0 .367-.3.667-.667.667Z"))
                    }
                },
                new div(WhiteSpace("nowrap"))
                {
                    ProjectName
                },
                new div(OnClick(ToggleSuggestions), DisplayFlex, JustifyContentCenter, AlignItemsCenter, Padding(3))
                {
                    new svg(svg.Height(20), svg.Width(20), ViewBox(0, 0, 20, 20), Fill("currentcolor"), Color(Gray300), Hover(Color(Gray600)))
                    {
                        new path(path.D("M4.516 7.548c0.436-0.446 1.043-0.481 1.576 0l3.908 3.747 3.908-3.747c0.533-0.481 1.141-0.446 1.574 0 0.436 0.445 0.408 1.197 0 1.615-0.406 0.418-4.695 4.502-4.695 4.502-0.217 0.223-0.502 0.335-0.787 0.335s-0.57-0.112-0.789-0.335c0 0-4.287-4.084-4.695-4.502s-0.436-1.17 0-1.615z"))
                    }
                }
            },
            !state.IsSuggestionsVisible ? null :
                new div(OnMouseLeave(ToggleSuggestions), DisplayFlex, JustifyContentCenter, AlignItemsCenter, PositionFixed, Background(White), Border(1, solid, Gray300), BorderRadius(4), PaddingTop(4), PaddingBottom(4), Left(state.SuggestionPopupLocationX), Top(state.SuggestionPopupLocationY), ZIndex(3))
                {
                    new div
                    {
                        from item in Suggestions
                        select new div(Id(item), OnClick(OnSuggestionItemClicked), DisplayFlex, JustifyContentCenter, AlignItemsCenter, Padding(6, 12), BorderRadius(4), Hover(Background(Blue50)), item == ProjectName ? Background(Gray200) : Background(White))
                        {
                            item
                        }
                    } 
                }
            
        };
    }

    Task OnAddNewComponentClicked(MouseEvent e)
    {
        DispatchEvent(OnAddNewComponent, []);

        return Task.CompletedTask;
    }

    Task OnSuggestionItemClicked(MouseEvent e)
    {
        var projectName = e.target.id;

        state = state with
        {
            IsSuggestionsVisible = false
        };

        DispatchEvent(OnChange, [projectName]);

        return Task.CompletedTask;
    }

    Task ToggleSuggestions(MouseEvent e)
    {
        var rect = e.target.boundingClientRect;

        state = state with
        {
            IsSuggestionsVisible = !state.IsSuggestionsVisible,
            SuggestionPopupLocationX = rect.left-100,
            SuggestionPopupLocationY = rect.top + rect.height + 8
        };

        return Task.CompletedTask;
    }

    internal record State
    {
        public bool IsSuggestionsVisible { get; set; }

        public double SuggestionPopupLocationX { get; init; }

        public double SuggestionPopupLocationY { get; init; }
    }
}
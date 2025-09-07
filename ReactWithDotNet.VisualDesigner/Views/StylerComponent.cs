namespace ReactWithDotNet.VisualDesigner.Views;


sealed class StylerComponent : Component<StylerComponent.State>
{
    protected override Task constructor()
    {
        state = new()
        {
            GroupNames =
            [
                "Layout",
                "Spacing",
                "Border",
                "Corner",
                "Typeography",
            ],
            Options = []
        };
        
        return Task.CompletedTask;
    }

    protected override Element render()
    {
        return new div(Padding(4), DisplayFlex, FlexDirectionColumn, Gap(8), CursorDefault)
        {
            !state.IsPopupVisible ? null :
                new div(OnMouseLeave(TogglePopup), DisplayFlex, FlexDirectionColumn, Gap(8), PositionFixed, Right(32), Width(500), Height(400), Bottom(32), Bottom(32), Border(1, solid, Gray300), BorderRadius(4), Background(White))
                {
                    new div(Display("grid"), GridTemplateRows("1fr 1fr 1fr 1fr 1fr 1fr"), GridTemplateColumns("1fr 1fr 1fr 1fr 1fr 1fr"), Gap(4), BorderRadius(4), Flex(1, 1, 0))
                    {
                        new div(GridArea("2 / 2 / 6 / 6"), Background(White), DisplayFlex, Gap(8), Padding(16))
                        {
                            from item in state.Options
                            select new div(Background(Stone100), Padding(4), MinWidth(50), WidthFitContent, HeightFitContent, MinHeight(30), BorderRadius(4), Hover(Background(Stone200)))
                        },
                        new div(GridRow(2), GridColumn(1), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(3), GridColumn(1), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(4), GridColumn(1), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(5), GridColumn(1), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(6), GridColumn(2), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(6), GridColumn(3), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(6), GridColumn(4), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(6), GridColumn(5), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(2), GridColumn(6), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(3), GridColumn(6), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(4), GridColumn(6), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(5), GridColumn(6), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(1), GridColumn(2), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(1), GridColumn(3), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(1), GridColumn(4), Background(Gray100), BorderRadius(4)),
                        new div(GridRow(1), GridColumn(5), Background(Gray100), BorderRadius(4))
                    },
                    new div(Background(Gray50), DisplayFlex, JustifyContentSpaceBetween, Padding(8), BorderRadius(4), Gap(8))
                    {
                        from item in state.GroupNames
                        select new div(Id(item), OnMouseEnter(OnGroupItemMouseEnter), Background(Gray100), WidthFull, TextAlignCenter, Padding(8), BorderRadius(4))
                        {
                            item
                        }
                    }
                }
            ,
            new div(OnMouseEnter(TogglePopup), TextAlignCenter, WidthFitContent, PositionFixed, Right(16), Bottom(16))
            {
                new svg(svg.Xmlns("http://www.w3.org/2000/svg"), svg.Width(23), svg.Height(23), ViewBox(0, 0, 23, 23), Fill(none))
                {
                    new line(line.X1(11.5), line.Y1(4), line.X2(11.5), line.Y2(19), Stroke("currentColor")),
                    new line(line.X1(4), line.Y1(11.5), line.X2(19), line.Y2(11.5), Stroke("currentColor"))
                }
            }
        };
    }

    Task TogglePopup(MouseEvent e)
    {
        var rect = e.target.boundingClientRect;

        state = state with
        {
            IsPopupVisible = !state.IsPopupVisible,
            PopupLocationX = rect.left + rect.width / 2 - 24,
            PopupLocationY = rect.top + rect.height + 8
        };

        return Task.CompletedTask;
    }
    
    Task OnGroupItemMouseEnter(MouseEvent e)
    {
      

        state = state with
        {
            SelectedGroup = e.target.id
        };

        return Task.CompletedTask;
    }
    
    
    internal record State
    {
        public IReadOnlyList<string> GroupNames { get; init; }
        
        public IReadOnlyList<Option> Options { get; init; }
        
        public double PopupLocationX { get; init; }

        public double PopupLocationY { get; init; }

        public bool IsPopupVisible { get; init; }
        
        public string SelectedGroup { get; init; }
    }

    internal sealed record Option
    {
        public string Label { get; init; }
        public string Value { get; init; }
        
       
    }
}
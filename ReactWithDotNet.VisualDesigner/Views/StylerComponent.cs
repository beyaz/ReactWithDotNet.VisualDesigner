namespace ReactWithDotNet.VisualDesigner.Views;

sealed class StylerComponent : Component<StylerComponent.State>
{
    static readonly Dictionary<string, Dictionary<string, IReadOnlyList<Option>>> AllData = new()
    {
        ["Layout"] = new()
        {
            ["display"] =
            [
                new()
                {
                    Label = "flex",
                    Value = "display: flex"
                },
                new()
                {
                    Label = "grid",
                    Value = "display: grid"
                }
            ],

            ["position"] =
            [
                new()
                {
                    Label = "absolute",
                    Value = "position: absolute"
                },
                new()
                {
                    Label = "relative",
                    Value = "position: relative"
                }
            ]
        },

        ["Spacing"] = new()
        {
            ["margin"] =
            [
                new()
                {
                    Label = "4px",
                    Value = "margin: 4px"
                },
                new()
                {
                    Label = "8px",
                    Value = "margin: 8px"
                }
            ],

            ["padding"] =
            [
                new()
                {
                    Label = "4px",
                    Value = "padding: 4px"
                },
                new()
                {
                    Label = "8px",
                    Value = "padding: 8px"
                }
            ]
        },

        ["Typeograpth"] = new()
        {
            ["font-size"] =
            [
                new()
                { 
                    Label = "10",
                    Value = "font-size: 10px"
                },
                new()
                {
                    Label = "11",
                    Value = "font-size: 10px"
                }
            ],

            ["font-weight"] =
            [
                new()
                {
                    Label = "thin",
                    Value = "font-weight: absolute"
                },
                new()
                {
                    Label = "bold",
                    Value = "font-weight: bold"
                }
            ]
        }
    };

    IReadOnlyList<string> GroupNames => AllData.Keys.ToList();

    protected override Element render()
    {
        return new div(Padding(4), DisplayFlex, FlexDirectionColumn, Gap(8), CursorDefault)
        {
            !state.IsPopupVisible ? null :
                new div(OnMouseLeave(TogglePopup), DisplayFlex, FlexDirectionColumn, Gap(8), PositionFixed, Right(32), Width(500), Height(400), Bottom(32), Bottom(32), Border(1, solid, Gray300), BorderRadius(4), Background(White))
                {
                    new div(Display("grid"), GridTemplateRows("1fr 1fr 1fr 1fr 1fr 1fr"), GridTemplateColumns("1fr 1fr 1fr 1fr 1fr 1fr"), Gap(4), BorderRadius(4), Flex(1, 1, 0))
                    {
                        new div(GridArea("2 / 2 / 6 / 6"), Background(White), DisplayFlex, Gap(8), Padding(16), AlignItemsFlexEnd)
                        {
                            from item in GetOptions()
                            select new div(Background(Stone100), Padding(4), MinWidth(50), WidthFitContent, HeightFitContent, MinHeight(30), BorderRadius(4), Hover(Background(Stone200)), DisplayFlex, JustifyContentCenter, AlignItemsCenter)
                            {
                                item.Label
                            }
                        },
                        new div(GridRow(2), GridColumn(1))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(7),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(7)
                            }
                        },
                        new div(GridRow(3), GridColumn(1))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(6),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(6)
                            }
                        },
                        new div(GridRow(4), GridColumn(1))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(5),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(5)
                            }
                        },
                        new div(GridRow(5), GridColumn(1))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(4),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(4)
                            }
                        },
                        new div(GridRow(6), GridColumn(2))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(0),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(0)
                            }
                        },
                        new div(GridRow(6), GridColumn(3))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(1),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(1)
                            }
                        },
                        new div(GridRow(6), GridColumn(4))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(2),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(2)
                            }
                        },
                        new div(GridRow(6), GridColumn(5))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(3),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(3)
                            }
                        },
                        new div(GridRow(2), GridColumn(6))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(12),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(12)
                            }
                        },
                        new div(GridRow(3), GridColumn(6))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(13),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(13)
                            }
                        },
                        new div(GridRow(4), GridColumn(6))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(14),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(14)
                            }
                        },
                        new div(GridRow(5), GridColumn(6))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(15),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(15)
                            }
                        },
                        new div(GridRow(1), GridColumn(2))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(8),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(8)
                            }
                        },
                        new div(GridRow(1), GridColumn(3))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(9),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(9)
                            }
                        },
                        new div(GridRow(1), GridColumn(4))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(10),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(10)
                            }
                        },
                        new div(GridRow(1), GridColumn(5))
                        {
                            new SubGroupItem
                            {
                                Label=TryGetSubGroupLabelAt(11),
                                SelectionChange=OnSubGroupItemChanged,
                                IsSelected=IsSelectedSubGroup(11)
                            }
                        }
                    },
                    new div(Background(Gray50), DisplayFlex, JustifyContentSpaceBetween, Padding(8), BorderRadius(4), Gap(8))
                    {
                        from item in GroupNames
                        select new div(Id(item), OnMouseEnter(OnGroupItemMouseEnter), Background(item == state.SelectedGroupName ? Gray300 : Gray100), WidthFull, TextAlignCenter, Padding(8), BorderRadius(4))
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

    IReadOnlyList<Option> GetOptions()
    {
        var groupName = state.SelectedGroupName;
        if (groupName is null)
        {
            return [];
        }

        var subGroupName = state.SelectedSubGroupName;
        if (subGroupName is null)
        {
            return [];
        }

        return AllData[groupName][subGroupName];
    }

    Task OnGroupItemMouseEnter(MouseEvent e)
    {
        var selectedGroupName = e.target.id;

        state = state with
        {
            SelectedGroupName = selectedGroupName,

            SelectedSubGroupName = null
        };

        return Task.CompletedTask;
    }

    
    Task OnSubGroupItemChanged(string subGroupName)
    {
        state = state with
        {
            SelectedSubGroupName = subGroupName
        };

        return Task.CompletedTask;
    }
    

    Task TogglePopup(MouseEvent e)
    {
        state = state with
        {
            IsPopupVisible = !state.IsPopupVisible
        };

        return Task.CompletedTask;
    }

    string TryGetSubGroupLabelAt(int index)
    {
        var groupName = state.SelectedGroupName;
        if (groupName is null)
        {
            return null;
        }

        var subGroupNames = AllData[groupName].Keys.ToList();
        if (subGroupNames.Count > index)
        {
            return subGroupNames[index];
        }
         
        return null;
    }
    
    bool IsSelectedSubGroup(int index) 
    {
        return state.SelectedSubGroupName == TryGetSubGroupLabelAt(index);
    }
    

    class SubGroupItem : Component
    {
        [CustomEvent]
        public Func<string, Task> SelectionChange { get; init; }
        
        public required string Label { get; init; }
        
        public required bool IsSelected { get; init; }
        
        protected override Element render()
        {
            if (Label is null)
            {
                return null;
            }
            
            return new div(OnMouseEnter(OnSubGroupItemMouseEnter), Id(Label), Background(IsSelected ? Gray200 : Gray100), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFull, HeightFull)
            {
                Label
            };
        }
        
        Task OnSubGroupItemMouseEnter(MouseEvent e)
        {
            DispatchEvent(SelectionChange,[Label]);

            return Task.CompletedTask;
        }
    }

    internal record State
    {
        public bool IsPopupVisible { get; init; }

        public string SelectedGroupName { get; init; }

        public string SelectedSubGroupName { get; init; }
    }

    internal sealed record Option
    {
        public string Label { get; init; }

        public string Value { get; init; }
    }
}
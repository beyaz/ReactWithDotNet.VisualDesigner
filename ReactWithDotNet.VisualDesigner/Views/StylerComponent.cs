namespace ReactWithDotNet.VisualDesigner.Views;

sealed class StylerComponent : Component<StylerComponent.State>
{
    static readonly Dictionary<string, Dictionary<string, IReadOnlyList<Option>>> AllData = new()
    {
        ["Layout"] = new()
        {
            ["display"] =
            [
                new() { Label = "flex", Value         = "display: flex" },
                new() { Label = "grid", Value         = "display: grid" },
                new() { Label = "block", Value        = "display: block" },
                new() { Label = "inline", Value       = "display: inline" },
                new() { Label = "inline-block", Value = "display: inline-block" },
                new() { Label = "none", Value         = "display: none" }
            ],
            ["position"] =
            [
                new() { Label = "static", Value   = "position: static" },
                new() { Label = "relative", Value = "position: relative" },
                new() { Label = "absolute", Value = "position: absolute" },
                new() { Label = "fixed", Value    = "position: fixed" },
                new() { Label = "sticky", Value   = "position: sticky" }
            ],
            ["overflow"] =
            [
                new() { Label = "visible", Value = "overflow: visible" },
                new() { Label = "hidden", Value  = "overflow: hidden" },
                new() { Label = "scroll", Value  = "overflow: scroll" },
                new() { Label = "auto", Value    = "overflow: auto" }
            ],
            ["z-index"] =
            [
                new() { Label = "0", Value    = "z-index: 0" },
                new() { Label = "1", Value    = "z-index: 1" },
                new() { Label = "2", Value    = "z-index: 2" },
                new() { Label = "3", Value    = "z-index: 3" },
                new() { Label = "4", Value    = "z-index: 4" },
                new() { Label = "5", Value    = "z-index: 5" },
                new() { Label = "6", Value    = "z-index: 6" },
                new() { Label = "7", Value    = "z-index: 7" },
                new() { Label = "8", Value    = "z-index: 8" },
                new() { Label = "9", Value    = "z-index: 9" },
                new() { Label = "10", Value   = "z-index: 10" },
                new() { Label = "100", Value  = "z-index: 100" },
                new() { Label = "9999", Value = "z-index: 9999" }
            ]
        },

        ["Size"] = new()
        {
            ["width"] =
            [
                new() { Label = "auto", Value = "width: auto" },
                new() { Label = "25%", Value  = "width: 25%" },
                new() { Label = "50%", Value  = "width: 50%" },
                new() { Label = "75%", Value  = "width: 75%" },
                new() { Label = "100%", Value = "width: 100%" }
            ],
            ["height"] =
            [
                new() { Label = "auto", Value = "height: auto" },
                new() { Label = "25%", Value  = "height: 25%" },
                new() { Label = "50%", Value  = "height: 50%" },
                new() { Label = "75%", Value  = "height: 75%" },
                new() { Label = "100%", Value = "height: 100%" }
            ]
        },

        ["Spacing"] = new()
        {
            ["margin"] =
            [
                new() { Label = "0", Value    = "margin: 0" },
                new() { Label = "4px", Value  = "margin: 4px" },
                new() { Label = "8px", Value  = "margin: 8px" },
                new() { Label = "16px", Value = "margin: 16px" },
                new() { Label = "auto", Value = "margin: auto" }
            ],
            ["padding"] =
            [
                new() { Label = "0", Value    = "padding: 0" },
                new() { Label = "4px", Value  = "padding: 4px" },
                new() { Label = "8px", Value  = "padding: 8px" },
                new() { Label = "16px", Value = "padding: 16px" }
            ],
            ["gap"] =
            [
                new() { Label = "4px", Value  = "gap: 4px" },
                new() { Label = "8px", Value  = "gap: 8px" },
                new() { Label = "16px", Value = "gap: 16px" }
            ]
        },

        ["Border"] = new()
        {
            ["border-style"] =
            [
                new() { Label = "none", Value   = "border-style: none" },
                new() { Label = "solid", Value  = "border-style: solid" },
                new() { Label = "dashed", Value = "border-style: dashed" },
                new() { Label = "dotted", Value = "border-style: dotted" }
            ],
            ["border-width"] =
            [
                new() { Label = "1px", Value = "border-width: 1px" },
                new() { Label = "2px", Value = "border-width: 2px" },
                new() { Label = "4px", Value = "border-width: 4px" }
            ],
            ["border-radius"] =
            [
                new() { Label = "0", Value   = "border-radius: 0" },
                new() { Label = "4px", Value = "border-radius: 4px" },
                new() { Label = "8px", Value = "border-radius: 8px" },
                new() { Label = "50%", Value = "border-radius: 50%" }
            ],
            ["box-shadow"] =
            [
                new() { Label = "none", Value   = "box-shadow: none" },
                new() { Label = "small", Value  = "box-shadow: 0 1px 3px rgba(0,0,0,0.2)" },
                new() { Label = "medium", Value = "box-shadow: 0 4px 6px rgba(0,0,0,0.2)" },
                new() { Label = "large", Value  = "box-shadow: 0 10px 15px rgba(0,0,0,0.3)" }
            ]
        },

        ["Typography"] = new()
        {
            ["font-family"] =
            [
                new() { Label = "Arial", Value           = "font-family: Arial, sans-serif" },
                new() { Label = "Times New Roman", Value = "font-family: 'Times New Roman', serif" },
                new() { Label = "Courier New", Value     = "font-family: 'Courier New', monospace" }
            ],
            ["font-size"] =
            [
                new() { Label = "12px", Value = "font-size: 12px" },
                new() { Label = "14px", Value = "font-size: 14px" },
                new() { Label = "16px", Value = "font-size: 16px" },
                new() { Label = "20px", Value = "font-size: 20px" }
            ],
            ["font-weight"] =
            [
                new() { Label = "normal", Value  = "font-weight: normal" },
                new() { Label = "bold", Value    = "font-weight: bold" },
                new() { Label = "lighter", Value = "font-weight: lighter" },
                new() { Label = "100", Value     = "font-weight: 100" },
                new() { Label = "900", Value     = "font-weight: 900" }
            ],
            ["line-height"] =
            [
                new() { Label = "1", Value   = "line-height: 1" },
                new() { Label = "1.5", Value = "line-height: 1.5" },
                new() { Label = "2", Value   = "line-height: 2" }
            ],
            ["letter-spacing"] =
            [
                new() { Label = "normal", Value = "letter-spacing: normal" },
                new() { Label = "1px", Value    = "letter-spacing: 1px" },
                new() { Label = "2px", Value    = "letter-spacing: 2px" }
            ],
            ["text-align"] =
            [
                new() { Label = "left", Value    = "text-align: left" },
                new() { Label = "center", Value  = "text-align: center" },
                new() { Label = "right", Value   = "text-align: right" },
                new() { Label = "justify", Value = "text-align: justify" }
            ],
            ["text-decoration"] =
            [
                new() { Label = "none", Value         = "text-decoration: none" },
                new() { Label = "underline", Value    = "text-decoration: underline" },
                new() { Label = "line-through", Value = "text-decoration: line-through" }
            ],
            ["color"] =
            [
                new() { Label = "Black", Value = "color: #000000" },
                new() { Label = "White", Value = "color: #ffffff" },
                new() { Label = "Red", Value   = "color: red" },
                new() { Label = "Blue", Value  = "color: blue" }
            ]
        },

        ["Background"] = new()
        {
            ["background-color"] =
            [
                new() { Label = "White", Value       = "background-color: #ffffff" },
                new() { Label = "Black", Value       = "background-color: #000000" },
                new() { Label = "Red", Value         = "background-color: red" },
                new() { Label = "Blue", Value        = "background-color: blue" },
                new() { Label = "Transparent", Value = "background-color: transparent" }
            ],
            ["background-size"] =
            [
                new() { Label = "auto", Value    = "background-size: auto" },
                new() { Label = "cover", Value   = "background-size: cover" },
                new() { Label = "contain", Value = "background-size: contain" }
            ],
            ["background-repeat"] =
            [
                new() { Label = "no-repeat", Value = "background-repeat: no-repeat" },
                new() { Label = "repeat", Value    = "background-repeat: repeat" },
                new() { Label = "repeat-x", Value  = "background-repeat: repeat-x" },
                new() { Label = "repeat-y", Value  = "background-repeat: repeat-y" }
            ]
        },

        ["Effects"] = new()
        {
            ["opacity"] =
            [
                new() { Label = "0", Value   = "opacity: 0" },
                new() { Label = "0.5", Value = "opacity: 0.5" },
                new() { Label = "1", Value   = "opacity: 1" }
            ],
            ["transition"] =
            [
                new() { Label = "all 0.3s", Value     = "transition: all 0.3s ease" },
                new() { Label = "opacity 0.5s", Value = "transition: opacity 0.5s ease-in-out" }
            ],
            ["animation"] =
            [
                new() { Label = "none", Value = "animation: none" },
                new() { Label = "spin", Value = "animation: spin 1s linear infinite" }
            ],
            ["filter"] =
            [
                new() { Label = "none", Value      = "filter: none" },
                new() { Label = "blur", Value      = "filter: blur(5px)" },
                new() { Label = "grayscale", Value = "filter: grayscale(100%)" }
            ]
        },

        ["Other"] = new()
        {
            ["cursor"] =
            [
                new() { Label = "default", Value = "cursor: default" },
                new() { Label = "pointer", Value = "cursor: pointer" },
                new() { Label = "text", Value    = "cursor: text" },
                new() { Label = "move", Value    = "cursor: move" }
            ],
            ["visibility"] =
            [
                new() { Label = "visible", Value  = "visibility: visible" },
                new() { Label = "hidden", Value   = "visibility: hidden" },
                new() { Label = "collapse", Value = "visibility: collapse" }
            ],
            ["pointer-events"] =
            [
                new() { Label = "auto", Value = "pointer-events: auto" },
                new() { Label = "none", Value = "pointer-events: none" }
            ],
            ["clip-path"] =
            [
                new() { Label = "circle", Value  = "clip-path: circle(50%)" },
                new() { Label = "inset", Value   = "clip-path: inset(10px)" },
                new() { Label = "polygon", Value = "clip-path: polygon(50% 0, 100% 100%, 0 100%)" }
            ]
        }
    };

    static IReadOnlyList<string> GroupNames => AllData.Keys.ToList();

    
    [CustomEvent]
    public required Func<string, Task> OptionSelected { get; init; }


    protected override Element render()
    {
        return new div(Padding(4), DisplayFlex, FlexDirectionColumn, Gap(8), CursorDefault)
        {
            !state.IsPopupVisible ? null :
                new div(OnMouseLeave(TogglePopup), DisplayFlex, FlexDirectionColumn, Gap(8), PositionFixed, Right(32), Width(600), Height(400), Bottom(32), Bottom(32), Border(1, solid, Gray300), BorderRadius(4), Background(White))
                {
                    new div(Display("grid"), GridTemplateRows("1fr 1fr 1fr 1fr 1fr 1fr"), GridTemplateColumns("1fr 1fr 1fr 1fr 1fr 1fr"), Gap(4), BorderRadius(4), Flex(1, 1, 0))
                    {
                        new div(GridArea("2 / 2 / 6 / 6"), Background(White), DisplayFlex, Gap(8), Padding(16), AlignItemsFlexEnd)
                        {
                            from item in GetOptions()
                            select new div(Id(item.Label), OnClick(OnOptionItemClicked), Background(Stone100), Padding(4), MinWidth(50), WidthFitContent, HeightFitContent, MinHeight(30), BorderRadius(4), Hover(Background(Stone200)), DisplayFlex, JustifyContentCenter, AlignItemsCenter)
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

    bool IsSelectedSubGroup(int index)
    {
        return state.SelectedSubGroupName == TryGetSubGroupLabelAt(index);
    }

    Task OnOptionItemClicked(MouseEvent e)
    { 
        var optionLabel = e.target.id;

        var option = GetOptions().First(x => x.Label == optionLabel);

        state = state with
        {
            IsPopupVisible = false
        };
        
        DispatchEvent(OptionSelected,[option.Value]);

        return Task.CompletedTask;
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

    class SubGroupItem : Component
    {
        public required bool IsSelected { get; init; }

        public required string Label { get; init; }

        [CustomEvent]
        public Func<string, Task> SelectionChange { get; init; }

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
            DispatchEvent(SelectionChange, [Label]);

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
namespace ReactWithDotNet.VisualDesigner.Views;

sealed class StylerComponent : Component<StylerComponent.State>
{
    record SubGroupItemModel
    {
        public required string Label { get; init; }
        
        public required IReadOnlyList<Option> Suggestions { get; init; }
        
        public required string TargetCssName { get; init; }
        
        public bool IsCssUnitEnabled { get; init; }
    }
    
    record GroupModel
    {
        public required string Label { get; init; }
        
        public required IReadOnlyList<SubGroupItemModel> SubGroups { get; init; }
    }

    static IReadOnlyList<GroupModel> AllGroups =>
    [
        new()
        {
            Label = "Layout",

            SubGroups =
            [
                new()
                {
                    Label = "display",

                    TargetCssName = "display",

                    Suggestions =
                    [
                        new("flex"),
                        new("grid"),
                        new("block"),
                        new("inline"),
                        new("inline-block")
                    ]
                },
                new()
                {
                    Label = "position",

                    TargetCssName = "position",

                    Suggestions =
                    [
                        new("static"),
                        new("relative"),
                        new("absolute"),
                        new("fixed"),
                        new("sticky")
                    ]
                },

                new()
                {
                    Label = "overflow",

                    TargetCssName = "overflow",

                    Suggestions =
                    [
                        new("visible"),
                        new("hidden"),
                        new("scroll"),
                        new("auto")
                    ]
                },

                new()
                {
                    Label = "transform",

                    TargetCssName = "transform",

                    Suggestions =
                    [
                        new("translateY(-50%)"),
                        new("translateX(-50%)")
                    ]
                },

                new()
                {
                    Label = "z-index",

                    TargetCssName = "z-index",

                    Suggestions =
                    [
                        new("1"),
                        new("2"),
                        new("3"),
                        new("4"),
                        new("5"),
                        new("6"),
                        new("7"),
                        new("8"),
                        new("9"),
                        new("10"),
                        new("100"),
                        new("1000"),
                    ]
                }
            ]
        },

        new()
        {
            Label = "Flex",

            SubGroups =
            [
                new()
                {
                    Label = "direction",

                    TargetCssName = "flex-direction",

                    Suggestions =
                    [
                        new("row"),
                        new("column"),
                        new("row-reverse"),
                        new("column-reverse")
                    ]
                },
                
                new()
                {
                    Label = "gap",
                    
                    TargetCssName = "gap",
                    
                    IsCssUnitEnabled = true,
                    
                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                    ]
                },

                new()
                {
                    Label = "justify",

                    TargetCssName = "justify-content",

                    Suggestions =
                    [
                        new("flex-start"),
                        new("flex-end"),
                        new("center"),
                        new("space-between"),
                        new("space-around"),
                        new("space-evenly"),
                    ]
                },

                new()
                {
                    Label = "align",

                    TargetCssName = "align-items",

                    Suggestions =
                    [
                        new("flex-start"),
                        new("flex-end"),
                        new("center"),
                        new("stretch")
                    ]
                },

                new()
                {
                    Label = "wrap",

                    TargetCssName = "flex-wrap",

                    Suggestions =
                    [
                        new("wrap"),
                        new("wrap-reverse"),
                        new("nowrap")
                    ]
                },
            ]
        },

        new()
        {
            Label = "Size",

            SubGroups =
            [
                new()
                {
                    Label = "min-width",

                    TargetCssName = "min-width",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("50px"),
                        new("100px")
                    ]
                },
                new()
                {
                    Label = "height",

                    TargetCssName = "height",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("100%"),
                        new("75%"),
                        new("50%"),
                        new("25%"),
                        new("fit-content"),
                        new("max-content"),
                        new("min-content")
                    ]
                },
                new()
                {
                    Label = "width",

                    TargetCssName = "width",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("100%"),
                        new("75%"),
                        new("50%"),
                        new("25%"),
                        new("fit-content"),
                        new("max-content"),
                        new("min-content")
                    ]
                },
                new()
                {
                    Label = "min-height",

                    TargetCssName = "min-height",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("50px"),
                        new("100px")
                    ]
                }
            ]
        },

        new()
        {
            Label = "Font",
            
            SubGroups =
            [
                new()
                {
                    Label = "size",

                    TargetCssName = "font-size",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("small"),
                        new("medium"),
                        new("large")
                    ]
                },

                new()
                {
                    Label = "weight",

                    TargetCssName = "font-weight",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("lighter"),
                        new("light"),
                        new("normal"),
                        new("bold"),
                        new("bolder")
                    ]
                },

                new()
                {
                    Label = "family",

                    TargetCssName = "font-family",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("Arial, sans-serif"),
                        new("'Times New Roman', serif"),
                        new("'Courier New', monospace")
                    ]
                },

                new()
                {
                    Label = "decoration",

                    TargetCssName = "text-decoration",

                    Suggestions =
                    [
                        new("underline"),
                        new("overline"),
                        new("line-through"),
                    ]
                },

                new()
                {
                    Label = "height",

                    TargetCssName = "line-height",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                    ]
                },

                new()
                {
                    Label = "spacing",

                    TargetCssName = "letter-spacing",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                    ]
                },

                new()
                {
                    Label = "align",

                    TargetCssName = "text-align",

                    Suggestions =
                    [
                        new("left"),
                        new("center"),
                        new("right"),
                        new("justify")
                    ]
                },

                new()
                {
                    Label = "wrap",

                    TargetCssName = "white-space",

                    Suggestions =
                    [
                        new("normal"),
                        new("nowrap"),
                        new("pre"),
                        new("pre-wrap"),
                        new("pre-line")
                    ]
                },

                new()
                {
                    Label = "color",

                    TargetCssName = "color",

                    Suggestions =
                    [
                        new("Black"),
                        new("White"),
                        new("Gray")
                    ]
                }
            ]
        },

        new()
        {
            Label = "Border",

            SubGroups =
            [
                new()
                {
                    Label = "style",
                    TargetCssName = "border-style",
                    Suggestions =
                    [
                        new(solid),
                        new(dashed),
                        new(dotted)
                    ]
                },
                
                new()
                {
                    Label         = "width",
                    
                    TargetCssName = "border-width",
                    
                    IsCssUnitEnabled = true,
                    
                    Suggestions =
                    [
                        new("1px"),
                        new("2px"),
                        new("3px")
                    ]
                },
                
                new()
                {
                    Label = "radius",
                    
                    TargetCssName = "border-radius",
                    
                    IsCssUnitEnabled = true,
                    
                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px")
                    ]
                },
                
                new()
                {
                    Label = "shadow",
                    
                    TargetCssName = "box-shadow",
                    
                    Suggestions =
                    [
                        new(){ Label = "small", Value = "0 1px 3px rgba(0,0,0,0.2)"},
                        new(){Label = "medium", Value = "0 4px 6px rgba(0,0,0,0.2)"},
                        new(){Label = "large", Value = "0 10px 15px rgba(0,0,0,0.3)"}
                    ]
                }
            ]
        },
        
        new()
        {
            Label = "Space",

            SubGroups =
            [
                new()
                {
                    Label = "margin",
                    
                    TargetCssName = "margin",
                    
                    IsCssUnitEnabled = true,
                    
                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("auto")
                    ]
                },
                
                new()
                {
                    Label = "padding",
                    
                    TargetCssName = "padding",
                    
                    IsCssUnitEnabled = true,
                    
                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("auto")
                    ]
                },
                
            ]
        },
        
        new()
        {
            Label = "Bg",

            SubGroups =
            [
                new()
                {
                    Label = "color",
                    
                    TargetCssName = "background-color",
                    
                    Suggestions =
                    [
                        new("White"),
                        new("Black"),
                        new("Gray"),
                        new("transparent")
                    ]
                },
                
                new()
                {
                    Label = "size",
                    
                    TargetCssName = "background-size",
                    
                    Suggestions =
                    [
                        new("auto"),
                        new("cover"),
                        new("contain")
                    ]
                },
                
                new()
                {
                    Label = "repeat",
                    
                    TargetCssName = "background-repeat",
                    
                    Suggestions =
                    [
                        new("no-repeat"),
                        new("repeat"),
                        new("repeat-x"),
                        new("repeat-y")
                    ]
                },
                
              
                
            ]
        },
        
        new()
        {
            Label = "Effects",

            SubGroups =
            [
                new()
                {
                    Label = "opacity",
                    
                    TargetCssName = "opacity",
                    
                    Suggestions =
                    [
                        new("0.1"),
                        new("0.2"),
                        new("0.3"),
                        new("0.4"),
                        new("0.5"),
                        new("0.6"),
                        new("0.7"),
                        new("0.8"),
                        new("0.9")
                    ]
                },
                
                new()
                {
                    Label = "transition",
                    
                    TargetCssName = "transition",
                    
                    Suggestions =
                    [
                        new("all 0.3s ease"),
                        new("opacity 0.5s ease-in-out")
                    ]
                },
                
                new()
                {
                    Label = "animation",
                    
                    TargetCssName = "animation",
                    
                    Suggestions =
                    [
                        new("spin 1s linear infinite")
                    ]
                },
                
                new()
                {
                    Label = "filter",
                    
                    TargetCssName = "filter",
                    
                    Suggestions =
                    [
                        new("blur(5px)"),
                        new("grayscale(100%)")
                    ]
                },

                
                
              
                
            ]
        },
        
        new()
        {
            Label = "Other",

            SubGroups =
            [
                new()
                {
                    Label = "object fit",
                    
                    TargetCssName = "object-fit",
                    
                    Suggestions =
                    [
                        new("cover"),
                        new("contain"),
                        new("fill"),
                        new("scale-down")
                    ]
                },
                
                new()
                {
                    Label = "cursor",
                    
                    TargetCssName = "cursor",
                    
                    Suggestions =
                    [
                        new("default"),
                        new("pointer"),
                        new("wait"),
                        new("text"),
                        new("move"),
                        new("not-allowed"),
                        new("crosshair")
                    ]
                },

              

               
                
              
                
            ]
        },
        
        new ()
        {
            Label = "",
            SubGroups = []
        }
    ];
    
    [CustomEvent]
    public required Func<string, Task> OptionSelected { get; init; }

    
    protected override Element render()
    {  
        return new div(OnMouseEnter(OnMouseEntered), OnMouseLeave(OnMouseLeaved), WidthFull, Height(300), Padding(16), DisplayFlex, FlexDirectionColumn, FontSize14, Background(White), Opacity(state.Opacity), CursorDefault, UserSelect(none), MinHeight(400))
        {
            new div(WidthFull, HeightFull, Border(1, solid, Gray200), BorderRadius(4), PositionRelative, Background(White), Padding(24))
            {
                new div(PositionAbsolute, Left(0), Top(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                {
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(0),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(0)
                    },
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(1),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(1)
                    },
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(2),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(2)
                    }
                },
                new div(PositionAbsolute, Left(0), Bottom(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                {
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(7),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(7)
                    },
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(8),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(8)
                    },
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(9),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(9)
                    }
                },
                new div(PositionAbsolute, Left(-8), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                {
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(3),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(3)
                    },
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(4),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(4)
                    }
                },
                new div(PositionAbsolute, Right(0), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                {
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(5),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(5)
                    },
                    new GroupItem
                    {
                        Label=TryGetGroupLabelAt(6),
                        SelectionChange=OnGroupItemChanged,
                        IsSelected=IsSelectedGroup(6)
                    }
                },
                new div(WidthFull, HeightFull, Border(1, solid, Gray200), BorderRadius(4), PositionRelative, Background(White))
                {
                    new div(PositionAbsolute, Left(0), Top(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                    {
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(0),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(0)
                        },
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(1),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(1)
                        },
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(2),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(2)
                        }
                    },
                    new div(PositionAbsolute, Left(0), Bottom(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                    {
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(7),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(7)
                        },
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(8),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(8)
                        },
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(9),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(9)
                        }
                    },
                    new div(PositionAbsolute, Left(-8), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                    {
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(3),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(3)
                        },
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(4),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(4)
                        }
                    },
                    new div(PositionAbsolute, Right(0), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                    {
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(5),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(5)
                        },
                        new SubGroupItem
                        {
                            Label=TryGetSubGroupLabelAt(6),
                            SelectionChange=OnSubGroupItemChanged,
                            IsSelected=IsSelectedSubGroup(6)
                        }
                    },
                    !HasAnyActiveSubGroup ? null :
                        new div(DisplayFlex, Padding(16), HeightFull, WidthFull, AlignItemsCenter, JustifyContentCenter, Gap(8))
                        {
                            !ActiveSubGroup.Suggestions.Any() ? null :
                                new div(DisplayFlex, AlignContentCenter, JustifyContentCenter, Gap(4), FlexWrap)
                                {
                                    from item in ActiveSubGroup.Suggestions
                                    select new CssValueItem
                                    {
                                        Label=item.Label,
                                        Value=item.Value,
                                        Click=OnCssItemClicked,
                                        TargetCssName=ActiveSubGroup.TargetCssName
                                    }
                                }
                            ,
                            !ActiveSubGroup.IsCssUnitEnabled ? null :
                                new div(DisplayFlex, AlignItemsCenter, JustifyContentCenter)
                                {
                                    new CssUnitEditor
                                    {
                                        Change=OnCssItemClicked,
                                        CssName=ActiveSubGroup.TargetCssName
                                    }
                                }
                            
                        }
                    
                }
            }
        };
    }

    Task OnMouseEntered(MouseEvent e)
    {
        state.Opacity = 1;
        
        return Task.CompletedTask;
    }
    Task OnMouseLeaved(MouseEvent e)
    {
        state.Opacity = 0.3;
        
        return Task.CompletedTask;
    }

    
    bool HasAnyActiveSubGroup=>ActiveSubGroup != null;
    
    GroupModel ActiveGroup =>
        FirstOrDefaultOf(from g in AllGroups
                where g.Label == state.SelectedGroupName
                select g);
    
    SubGroupItemModel ActiveSubGroup =>
        FirstOrDefaultOf(from sg in ActiveGroup?.SubGroups ?? []
                         where sg.Label == state.SelectedSubGroupName
                         select sg);
    
    bool IsSelectedGroup(int index)
    {
        return state.SelectedGroupName == TryGetGroupLabelAt(index);
    }
    
    bool IsSelectedSubGroup(int index)
    {
        return state.SelectedSubGroupName == TryGetSubGroupLabelAt(index);
    }

    Task OnGroupItemChanged(string groupName)
    {
        state = state with
        {
            SelectedGroupName = groupName,
            SelectedSubGroupName = null
        };

        return Task.CompletedTask;
    }

    Task OnCssItemClicked(string cssValue)
    {
        DispatchEvent(OptionSelected, [cssValue]);

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


    static string TryGetGroupLabelAt(int index)
    {
        if (AllGroups.Count > index)
        {
            return AllGroups[index].Label;
        }

        return null;
    }

    string TryGetSubGroupLabelAt(int index)
    {
        var groupName = state.SelectedGroupName;
        if (groupName is null)
        {
            return null;
        }

        var groupModel = AllGroups.First(x => x.Label == groupName);
        if (groupModel.SubGroups.Count > index)
        {
            return groupModel.SubGroups[index].Label;
        }

        return null;
    }

    class GroupItem : Component
    {
        public required bool IsSelected { get; init; }

        public required string Label { get; init; }

        IReadOnlyList<string> GetChars()
        {
            if (Label is null)
            {
                return [];
            }
            return Label.ToCharArray().Select(c => c.ToString()).ToList();
        }

        [CustomEvent]
        public Func<string, Task> SelectionChange { get; init; }

        protected override Element render()
        {
            if (Label.HasNoValue())
            {
                return new div();
            }

            return new div(OnMouseEnter(OnGroupItemMouseEnter), Id(Label), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFitContent, HeightFitContent, TextAlignCenter, Padding(4), LineHeight16, Background(White), DisplayFlex, Gap(8), IsSelected ? Background(Gray200) : Background(White), Border(1, solid, Gray300), FlexWrap)
            {
                from item in GetChars()
                select new div(WidthFitContent, HeightFitContent, LineHeight7)
                {
                    item
                }
            };
        } 
         
        Task OnGroupItemMouseEnter(MouseEvent e)
        {
            DispatchEvent(SelectionChange, [Label]);

            return Task.CompletedTask;
        }
    }

    class SubGroupItem : Component
    {
        public required bool IsSelected { get; init; }

        public required string Label { get; init; }

        IReadOnlyList<string> GetChars()
        {
            if (Label is null)
            {
                return [];
            }
            return Label.ToCharArray().Select(c => c.ToString()).ToList();
        }
        
        
        [CustomEvent]
        public Func<string, Task> SelectionChange { get; init; }

        protected override Element render()
        {
            if (Label is null)
            {
                return string.Empty;
            }

            return new div(OnMouseEnter(OnSubGroupItemMouseEnter), Id(Label), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFitContent, HeightFitContent, TextAlignCenter, Padding(4), LineHeight16, Background(White), DisplayFlex, Gap(2), IsSelected ? Background(Gray200) : Background(White), Border(1, solid, Gray300), FlexWrap, FontSize13)
            {
                from item in GetChars()
                select new div(WidthFitContent, HeightFitContent, LineHeight7, FontSize14)
                {
                    item
                }
            };
        }

        Task OnSubGroupItemMouseEnter(MouseEvent e)
        {
            DispatchEvent(SelectionChange, [Label]);

            return Task.CompletedTask;
        }
    }

    class CssValueItem : Component
    {
        public required string Label { get; init; }
        
        public required string Value { get; init; }
        
        public required string TargetCssName { get; init; }
        
        
        [CustomEvent]
        public required Func<string, Task> Click { get; init; }
        


        protected override Element render()
        {
            if (Label is null)
            {
                return new div(Opacity(0.2), BorderColor(Gray200), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFull, HeightFull, TextAlignCenter, Border(1, solid, Gray200));
            }

            return new div(OnClick(OnClicked), Border(1, solid, Gray200), Padding(2, 4), MinWidth(50), WidthFitContent, HeightFitContent, MinHeight(30), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, Hover(Border(1, solid, Gray400)))
            {
                Label
            };
        } 
         
        Task OnClicked(MouseEvent e)
        {
            DispatchEvent(Click, [$"{TargetCssName}: {Value}"]);

            return Task.CompletedTask;
        }
    }
    
    internal record State
    {
        public bool IsPopupVisible { get; init; }

        public string SelectedGroupName { get; init; }

        public string SelectedSubGroupName { get; init; }
        
        public double Opacity { get; set; }
    } 

    internal sealed record Option
    {
        public Option()
        {
            
        }

        public Option(string value)
        {
            Label = Value = value;
        }
        
        public string Label { get; init; }

        public string Value { get; init; }
    } 
}

class CssUnitEditor : Component<CssUnitEditor.State>
{
    public string CssName { get; init; }
    
    [CustomEvent]
    public Func<string, Task> Change { get; init; }
    
    protected override Element render()
    {
        return new div(WidthFitContent, Border(1, solid, Gray200), DisplayFlex, FlexDirectionColumn)
        {
            new div(DisplayFlex, BackgroundColor("#ffffff"), Height(36), Gap(4), JustifyContentSpaceEvenly, BorderRadius(4), AlignItemsCenter)
            {
                new div(LetterSpacing(2))
                {
                    state.Value
                },
                new div(HeightFull, Width(1), Background(Gray200)),
                new div
                {
                    "px"
                }
            },
            new div(WidthFull, Height(1), Background(Gray200)),
            new div(DisplayFlex, Width(120), FlexWrap, Padding(4), Gap(8), CursorDefault, JustifyContentSpaceAround)
            {
                from item in new[]{ "1", "2", "3","4","5","6","7","8","9","-","0","."}
                select new div(OnClick(OnButtonClicked), Id(item), Width(30), Height(30), BorderRadius(50), AlignItemsCenter, JustifyContentCenter, DisplayFlex, Border(1, solid, Gray200), Hover(BorderColor(Gray300)), Hover(Background(Gray50)), BorderWidth(string.IsNullOrWhiteSpace(item ) ? "0px" : "1px"))
                {
                    item
                }
            }
        };
        
        
    }

    Task OnButtonClicked(MouseEvent e)
    {
        var charachter = e.target.id;
        if (charachter.HasNoValue())
        {
            return Task.CompletedTask;    
        }

        state = state with
        {
            Value = state.Value + charachter
        };
        
        DispatchEvent(Change,[CssName +":"+state.Value + state.Unit]);
        
        return Task.CompletedTask;
    }

    internal record State
    {
        public string Unit { get; init; } = "px";
        public string Value { get; init; }
    }
} 
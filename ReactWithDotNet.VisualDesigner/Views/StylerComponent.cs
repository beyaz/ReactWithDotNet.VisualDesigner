namespace ReactWithDotNet.VisualDesigner.Views;

sealed class StylerComponent : Component<StylerComponent.State>
{
    [CustomEvent]
    public required Func<string, Task> OptionSelected { get; init; }

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
                        new("1000")
                    ]
                },

                new()
                {
                    Label = "left",

                    TargetCssName = "left",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "right",

                    TargetCssName = "right",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "top",

                    TargetCssName = "top",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "bottom",

                    TargetCssName = "bottom",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
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
                        new("24px")
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
                        new("space-evenly")
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

                new()
                {
                    Label = "flex",

                    TargetCssName = "flex",

                    Suggestions =
                    [
                        new("1"),
                        new("0 1 auto"),
                        new("none")
                    ]
                },

                new()
                {
                    Label = "align self",

                    TargetCssName = "align-self",

                    Suggestions =
                    [
                        new("auto"),
                        new("flex-start"),
                        new("center"),
                        new("flex-end"),
                        new("stretch")
                    ]
                },

                new()
                {
                    Label = "order",

                    TargetCssName = "order",

                    Suggestions =
                    [
                        new("0"),
                        new("1"),
                        new("2"),
                        new("3"),
                        new("-1"),
                        new("-2"),
                        new("-3")
                    ]
                }
            ]
        },

        new()
        {
            Label = "Grid",
            SubGroups =
            [
                new()
                {
                    Label         = "t-rows",
                    TargetCssName = "grid-template-rows",
                    Suggestions =
                    [
                        new("1fr", "1fr"),
                        new("2fr", "1fr 1fr"),
                        new("3fr", "1fr 1fr 1fr"),
                        new("4fr", "1fr 1fr 1fr 1fr"),
                        new("5fr", "1fr 1fr 1fr 1fr 1fr"),
                        new("6fr", "1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("7fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("8fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("9fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("10fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("11fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("12fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr")
                    ]
                },
                new()
                {
                    Label         = "t-columns",
                    TargetCssName = "grid-template-columns",
                    Suggestions =
                    [
                        new("1fr", "1fr"),
                        new("2fr", "1fr 1fr"),
                        new("3fr", "1fr 1fr 1fr"),
                        new("4fr", "1fr 1fr 1fr 1fr"),
                        new("5fr", "1fr 1fr 1fr 1fr 1fr"),
                        new("6fr", "1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("7fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("8fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("9fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("10fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("11fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr"),
                        new("12fr", "1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr 1fr")
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
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "row",

                    TargetCssName = "grid-row",

                    IsGridItemEditorEnabled = true,

                    Suggestions = []
                },

                new()
                {
                    Label = "column",

                    IsGridItemEditorEnabled = true,

                    TargetCssName = "grid-column",

                    Suggestions = []
                },

                new()
                {
                    Label = "align",

                    TargetCssName = "align-items",

                    Suggestions =
                    [
                        new("start"),
                        new("center"),
                        new("end"),
                        new("stretch")
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

                    Suggestions =
                    [
                        new("200"),
                        new("300"),
                        new("400"),
                        new("500"),
                        new("600"),
                        new("700"),
                        new("800"),
                        new("900")
                    ]
                },

                new()
                {
                    Label = "family",

                    TargetCssName = "font-family",

                    Suggestions =
                    [
                        new("Arial, sans-serif"),
                        new("'Times New Roman', serif"),
                        new("'Courier New', monospace")
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
                }
            ]
        },

        new()
        {
            Label = "Text",

            SubGroups =
            [
                new()
                {
                    Label = "decoration",

                    TargetCssName = "text-decoration",

                    Suggestions =
                    [
                        new("underline"),
                        new("overline"),
                        new("line-through")
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
                },

                new()
                {
                    Label         = "transform",
                    TargetCssName = "text-transform",
                    Suggestions =
                    [
                        new("none"),
                        new("uppercase"),
                        new("lowercase"),
                        new("capitalize")
                    ]
                },

                new()
                {
                    Label         = "user select",
                    TargetCssName = "user-select",
                    Suggestions =
                    [
                        new("auto"),
                        new("none"),
                        new("text")
                    ]
                }
            ]
        },

        new()
        {
            Label = "Size",

            SubGroups =
            [
                new()
                {
                    Label = "width",

                    TargetCssName = "width",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("100%"),
                        new("50%"),
                        new("fit-content"),
                        new("max-content"),
                        new("min-content")
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
                        new("50%"),
                        new("fit-content"),
                        new("max-content"),
                        new("min-content")
                    ]
                },

                new()
                {
                    Label = "min-w",

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
                    Label = "min-h",

                    TargetCssName = "min-height",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("50px"),
                        new("100px")
                    ]
                },

                new()
                {
                    Label = "box-sizing",

                    TargetCssName = "box-sizing",

                    Suggestions =
                    [
                        new("border-box"),
                        new("content-box")
                    ]
                }
            ]
        },

        new()
        {
            Label = "P",

            SubGroups =
            [
                new()
                {
                    Label = "l",

                    TargetCssName = "padding-left",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "r",

                    TargetCssName = "padding-right",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },
                null,
                new()
                {
                    Label = "t",

                    TargetCssName = "padding-top",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "b",

                    TargetCssName = "padding-bottom",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                null,

                null,

                null,

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
                        new("24px")
                    ]
                }
            ]
        },

        new()
        {
            Label = "M",

            SubGroups =
            [
                new()
                {
                    Label = "l",

                    TargetCssName = "margin-left",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "r",

                    TargetCssName = "margin-right",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },
                null,
                new()
                {
                    Label = "t",

                    TargetCssName = "margin-top",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                new()
                {
                    Label = "b",

                    TargetCssName = "margin-bottom",

                    IsCssUnitEnabled = true,

                    Suggestions =
                    [
                        new("4px"),
                        new("8px"),
                        new("16px"),
                        new("24px")
                    ]
                },

                null,

                null,

                null,

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
                        new("24px")
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
                    Label         = "style",
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
                    Label = "width",

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
                        new() { Label = "small", Value  = "0 1px 3px rgba(0,0,0,0.2)" },
                        new() { Label = "medium", Value = "0 4px 6px rgba(0,0,0,0.2)" },
                        new() { Label = "large", Value  = "0 10px 15px rgba(0,0,0,0.3)" }
                    ]
                }
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
                }
            ]
        },

        new()
        {
            Label = "Others",

            SubGroups =
            [
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
                }
            ]
        }
    ];

    GroupModel ActiveGroup =>
        FirstOrDefaultOf(
            from g in AllGroups
            where g.Label == state.SelectedGroupName
            select g);

    SubGroupItemModel ActiveSubGroup =>
        FirstOrDefaultOf(
            from sg in ActiveGroup?.SubGroups ?? []
            where sg is not null && sg.Label == state.SelectedSubGroupName
            select sg);

    bool HasAnyActiveSubGroup => ActiveSubGroup != null;

    protected override Element render()
    {
        return new div(OnMouseEnter(OnMouseEntered), OnMouseLeave(OnMouseLeaved), WidthFull, Height(300), Padding(12), DisplayFlex, FlexDirectionColumn, FontSize14, Background(White), Opacity(state.Opacity), CursorDefault, UserSelect(none), MinHeight(300))
        {
            new div(WidthFull, HeightFull, Border(1, solid, Gray200), BorderRadius(4), PositionRelative, Background(White), Padding(24))
            {
                new div(PositionAbsolute, Left(0), Top(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                {
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(0),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(0)
                    },
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(1),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(1)
                    },
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(2),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(2)
                    }
                },
                new div(PositionAbsolute, Left(0), Bottom(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                {
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(8),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(8)
                    },
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(9),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(9)
                    },
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(10),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(10)
                    }
                },
                new div(PositionAbsolute, Left(-9), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                {
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(3),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(3)
                    },
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(4),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(4)
                    }
                },
                new div(PositionAbsolute, Right(1), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                {
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(5),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(5)
                    },
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(6),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(6)
                    },
                    new GroupItem
                    {
                        Label           = TryGetGroupLabelAt(7),
                        SelectionChange = OnGroupItemChanged,
                        IsSelected      = IsSelectedGroup(7)
                    }
                },
                new div(WidthFull, HeightFull, Border(1, solid, Gray200), BorderRadius(4), PositionRelative, Background(White))
                {
                    new div(PositionAbsolute, Left(0), Top(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                    {
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(0),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(0)
                        },
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(1),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(1)
                        },
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(2),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(2)
                        }
                    },
                    new div(PositionAbsolute, Left(0), Bottom(-8), DisplayFlex, Right(0), JustifyContentSpaceAround)
                    {
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(7),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(7)
                        },
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(8),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(8)
                        },
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(9),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(9)
                        }
                    },
                    new div(PositionAbsolute, Left(-9), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                    {
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(3),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(3)
                        },
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(4),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(4)
                        }
                    },
                    new div(PositionAbsolute, Right(1), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
                    {
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(5),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(5)
                        },
                        new SubGroupItem
                        {
                            Label           = TryGetSubGroupLabelAt(6),
                            SelectionChange = OnSubGroupItemChanged,
                            IsSelected      = IsSelectedSubGroup(6)
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
                                        Label         = item.Label,
                                        Value         = item.Value,
                                        Click         = OnCssItemClicked,
                                        TargetCssName = ActiveSubGroup.TargetCssName
                                    }
                                },
                            !ActiveSubGroup.IsCssUnitEnabled ? null :
                                new div(DisplayFlex, AlignItemsCenter, JustifyContentCenter)
                                {
                                    new CssUnitEditor
                                    {
                                        Change  = OnCssItemClicked,
                                        CssName = ActiveSubGroup.TargetCssName
                                    }
                                },
                            !ActiveSubGroup.IsGridItemEditorEnabled ? null :
                                new div(DisplayFlex, AlignItemsCenter, JustifyContentCenter)
                                {
                                    new GridItemEditor
                                    {
                                        Change  = OnCssItemClicked,
                                        CssName = ActiveSubGroup.TargetCssName
                                    }
                                }
                        }
                }
            }
        };
    }

    static string TryGetGroupLabelAt(int index)
    {
        if (AllGroups.Count > index)
        {
            return AllGroups[index].Label;
        }

        return null;
    }

    bool IsSelectedGroup(int index)
    {
        return state.SelectedGroupName == TryGetGroupLabelAt(index);
    }

    bool IsSelectedSubGroup(int index)
    {
        return state.SelectedSubGroupName == TryGetSubGroupLabelAt(index);
    }

    Task OnCssItemClicked(string cssValue)
    {
        DispatchEvent(OptionSelected, [cssValue]);

        return Task.CompletedTask;
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

    Task OnMouseEntered(MouseEvent e)
    {
        state.Opacity = 1;

        return Task.CompletedTask;
    }

    Task OnMouseLeaved(MouseEvent e)
    {
        state = new()
        {
            Opacity = 0.2
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
            var subGroup = groupModel.SubGroups[index];
            if (subGroup is null)
            {
                return null;
            }

            return subGroup.Label;
        }

        return null;
    }

    class CssValueItem : Component
    {
        [CustomEvent]
        public required Func<string, Task> Click { get; init; }

        public required string Label { get; init; }

        public required string TargetCssName { get; init; }

        public required string Value { get; init; }

        protected override Element render()
        {
            if (Label is null)
            {
                return new div(Opacity(0.2), BorderColor(Gray200), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFull, HeightFull, TextAlignCenter, Border(1, solid, Gray200));
            }

            return new div(OnClick(OnClicked), Border(1, solid, Gray200), Padding(2, 4), MinWidth(50), WidthFitContent, HeightFitContent, MinHeight(30), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, Hover(Border(1, solid, Gray300)), Hover(BackgroundColor(Gray100)))
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

    class GroupItem : Component
    {
        public required bool IsSelected { get; init; }

        public required string Label { get; init; }

        [CustomEvent]
        public Func<string, Task> SelectionChange { get; init; }

        StyleModifier ArrangeSizeForTopBottomLeftRight
        {
            get
            {
                if (Label == "P" || Label == "M")
                {
                    return PaddingY(8 * 2);
                }

                return null;
            }
        }

        protected override Element render()
        {
            if (Label.HasNoValue)
            {
                return new div();
            }

            return new div(OnMouseEnter(OnGroupItemMouseEnter), Id(Label), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFitContent, HeightFitContent, TextAlignCenter, Padding(4), LineHeight16, Background(White), DisplayFlex, Gap(8), IsSelected ? Background(Gray200) : Background(White), Border(1, solid, Gray300), FlexWrap)
            {
                from item in GetChars()
                select new div(WidthFitContent, HeightFitContent, LineHeight7)
                {
                    item , ArrangeSizeForTopBottomLeftRight
                }
            };
        }

        IReadOnlyList<string> GetChars()
        {
            if (Label is null)
            {
                return [];
            }

            return Label.ToCharArray().Select(c => c.ToString()).ToList();
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

        [CustomEvent]
        public Func<string, Task> SelectionChange { get; init; }

        StyleModifier ArrangeSizeForTopBottomLeftRight
        {
            get
            {
                if (Label == "t" || Label == "b")
                {
                    return PaddingY(6 * 2);
                }

                if (Label == "l" || Label == "r")
                {
                    return PaddingX(6 * 2);
                }

                return null;
            }
        }

        protected override Element render()
        { 
            if (Label is null)
            {
                return string.Empty;
            }

            return new div(OnMouseEnter(OnSubGroupItemMouseEnter), Id(Label), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFitContent, HeightFitContent, TextAlignCenter, Padding(4), LineHeight16, Background(White), DisplayFlex, Gap(4), IsSelected ? Background(Gray200) : Background(White), Border(1, solid, Gray300), FlexWrap, FontSize13)
            {
                from item in GetChars()
                select new div(WidthFitContent, HeightFitContent, LineHeight7, FontSize14)
                {
                    item, ArrangeSizeForTopBottomLeftRight
                }
            };
        }

        IReadOnlyList<string> GetChars()
        {
            if (Label is null)
            {
                return [];
            }

            return Label.ToCharArray().Select(c => c.ToString()).ToList();
        }

        Task OnSubGroupItemMouseEnter(MouseEvent e)
        {
            DispatchEvent(SelectionChange, [Label]);

            return Task.CompletedTask;
        }
    }

    record SubGroupItemModel
    {
        public bool IsCssUnitEnabled { get; init; }

        public bool IsGridItemEditorEnabled { get; init; }

        public required string Label { get; init; }

        public required IReadOnlyList<Option> Suggestions { get; init; }

        public required string TargetCssName { get; init; }
    }

    record GroupModel
    {
        public required string Label { get; init; }

        public required IReadOnlyList<SubGroupItemModel> SubGroups { get; init; }
    }

    internal record State
    {
        public bool IsPopupVisible { get; init; }

        public double Opacity { get; set; }

        public string SelectedGroupName { get; init; }

        public string SelectedSubGroupName { get; init; }
    }

    internal sealed record Option
    {
        public Option()
        {
        }

        public Option(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public Option(string value)
        {
            Label = Value = value;
        }

        public string Label { get; init; }

        public string Value { get; init; }

        public static implicit operator Option(string value)
        {
            return new(value);
        }
    }
}

class CssUnitEditor : Component<CssUnitEditor.State>
{
    [CustomEvent]
    public Func<string, Task> Change { get; init; }

    public string CssName { get; init; }

    protected override Element render()
    {
        return new div(WidthFitContent, Border(1, solid, Gray200), DisplayFlex, FlexDirectionColumn)
        {
            new div(DisplayFlex, BackgroundColor("#ffffff"), Height(36), Gap(4), BorderRadius(4), AlignItemsCenter, JustifyContentSpaceEvenly)
            {
                new div(TextAlignCenter, Width("50%"))
                {
                    new div(LetterSpacing(2))
                    {
                        state.Value
                    }
                },
                new div(OnClick(ToggleUnitTypeSuggestions), Width("50%"), BorderLeft(1, solid, "#e5e7eb"), HeightFull, JustifyContentCenter, AlignItemsCenter, DisplayFlex)
                {
                    new div(HeightFull, LineHeight36)
                    {
                        state.Unit
                    }
                }
            },
            new div(WidthFull, Height(1), Background(Gray200)),
            new div(DisplayFlex, Width(120), FlexWrap, Padding(4), Gap(8), CursorDefault, JustifyContentSpaceAround)
            {
                from item in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "-", "0", "." }
                select new div(OnClick(OnButtonClicked), Id(item), Width(30), Height(30), BorderRadius(50), AlignItemsCenter, JustifyContentCenter, DisplayFlex, Border(1, solid, Gray200), Hover(BorderColor(Gray300)), Hover(Background(Gray50)), string.IsNullOrWhiteSpace(item) ? BorderWidth(0) : BorderWidth(1))
                {
                    item
                }
            },
            !state.IsUnitSuggestionsVisible ? null :
                new div(OnMouseLeave(ToggleUnitTypeSuggestions), DisplayFlex, JustifyContentCenter, AlignItemsCenter, PositionFixed, Background(White), Border(1, solid, Gray300), BorderRadius(4), PaddingTop(4), PaddingBottom(4), Left(state.SuggestionPopupLocationX), Top(state.SuggestionPopupLocationY), ZIndex(3), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"))
                {
                    new div
                    {
                        from item in new[] { "px", "%", "rem", "vw", "vh", "cm", "mm", "pt" }
                        select new div(Id(item), OnClick(OnUnitTypeSuggestionItemClicked), DisplayFlex, JustifyContentCenter, AlignItemsCenter, Padding(6, 12), BorderRadius(4), Hover(Background(Gray100)))
                        {
                            item
                        }
                    }
                }
        };
    }

    Task OnButtonClicked(MouseEvent e)
    {
        var charachter = e.target.id;
        if (charachter.HasNoValue)
        {
            return Task.CompletedTask;
        }

        state = state with
        {
            Value = state.Value + charachter
        };

        DispatchEvent(Change, [CssName + ":" + state.Value + state.Unit]);

        return Task.CompletedTask;
    }

    Task OnUnitTypeSuggestionItemClicked(MouseEvent e)
    {
        state = state with
        {
            Unit = e.target.id,
            IsUnitSuggestionsVisible = false
        };

        if (state.Value.HasValue)
        {
            DispatchEvent(Change, [CssName + ":" + state.Value + state.Unit]);
        }

        return Task.CompletedTask;
    }

    Task ToggleUnitTypeSuggestions(MouseEvent e)
    {
        var rect = e.target.boundingClientRect;

        state = state with
        {
            IsUnitSuggestionsVisible = !state.IsUnitSuggestionsVisible,
            SuggestionPopupLocationX = rect.left + rect.width / 2 - 24,
            SuggestionPopupLocationY = rect.top - 64
        };

        return Task.CompletedTask;
    }

    internal record State
    {
        public bool IsUnitSuggestionsVisible { get; init; }

        public double SuggestionPopupLocationX { get; init; }

        public double SuggestionPopupLocationY { get; init; }
        public string Unit { get; init; } = "px";
        public string Value { get; init; }
    }
}

class GridItemEditor : Component<GridItemEditor.State>
{
    [CustomEvent]
    public Func<string, Task> Change { get; init; }

    public string CssName { get; init; }

    protected override Task OverrideStateFromPropsBeforeRender()
    {
        if (CssName != state.CssName)
        {
            state = new()
            {
                CssName = CssName
            };
        }

        return Task.CompletedTask;
    }

    protected override Element render()
    {
        return new div(DisplayFlex, AlignItemsCenter, JustifyContentCenter, WidthFull, HeightFull, PaddingTop(16), PaddingBottom(16), PaddingLeft(8), PaddingRight(8))
        {
            new div(Border(1, solid, Gray200), BorderRadius(8), WidthFull, HeightFull, PositionRelative)
            {
                new div(PositionAbsolute, Left(0), Right(0), Top(-12), DisplayFlex, JustifyContentSpaceAround)
                {
                    new div(OnMouseEnter(OnStartModeEnter), PaddingLeft(8), PaddingRight(8), BorderRadius(4), Border(1, solid, Gray200), Hover(Border(1, solid, Gray300)), Hover(BackgroundColor(Gray100)), state.IsInSpanSelection ? Background(White) : Background(Gray200))
                    {
                        "Start"
                    },
                    new div(OnMouseEnter(OnSpanModeEnter), PaddingLeft(8), PaddingRight(8), BorderRadius(4), Border(1, solid, Gray200), Hover(Border(1, solid, Gray300)), Hover(BackgroundColor(Gray100)), state.IsInSpanSelection ? Background(Gray200) : Background(White))
                    {
                        "Span"
                    }
                },
                new div(DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFull, HeightFull, Gap(16), FlexWrap, Padding(24))
                {
                    from item in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }
                    select new div(OnClick(OnItemClicked), Id(item + ""), Padding(2, 4), Border(1, solid, Gray200), BorderRadius(4), MinWidth(30), TextAlignCenter, Hover(Border(1, solid, Gray300)), Hover(BackgroundColor(Gray100)))
                    {
                        item
                    }
                }
            }
        };
    }

    Task OnItemClicked(MouseEvent e)
    {
        var value = int.Parse(e.target.id);

        if (state.IsInSpanSelection)
        {
            state = state with
            {
                ValueForSpan = value
            };
        }
        else
        {
            state = state with
            {
                ValueForStart = value
            };
        }

        if (state.ValueForStart > 0 && state.ValueForSpan > 0)
        {
            DispatchEvent(Change, [$"{CssName}: {state.ValueForStart} / span {state.ValueForSpan}"]);
        }

        return Task.CompletedTask;
    }

    Task OnSpanModeEnter(MouseEvent e)
    {
        state = state with
        {
            IsInSpanSelection = true
        };

        return Task.CompletedTask;
    }

    Task OnStartModeEnter(MouseEvent e)
    {
        state = state with
        {
            IsInSpanSelection = false
        };

        return Task.CompletedTask;
    }

    internal record State
    {
        public string CssName { get; init; }
        
        public bool IsInSpanSelection { get; init; }

        public int? ValueForSpan { get; init; }

        public int? ValueForStart { get; init; }
    }
}
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
      new ()
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
      
      new ()
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
      
      
      new ()
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
              }
          ]
      }
    ];
    
    [CustomEvent]
    public required Func<string, Task> OptionSelected { get; init; }

    static Dictionary<string, Dictionary<string, IReadOnlyList<Option>>> AllData => new()
    {
       

        

        ["Size"] = new()
        {
            ["min width"] =
            [
                new() { Label = "50px", Value  = "min-width: 50px" },
                new() { Label = "100px", Value = "min-width: 100px" },
                new() { Label = "150px", Value = "min-width: 150px" },
                new() { Label = "200px", Value = "min-width: 200px" },
                new() { Label = "300px", Value = "min-width: 300px" },
                new() { Label = "400px", Value = "min-width: 400px" },
                new() { Label = "500px", Value = "min-width: 500px" }
            ],

            ["height"] =
            [
                new() { Label = "auto", Value        = "height: auto" },
                new() { Label = "fit-content", Value = "height: fit-content" },
                new() { Label = "max-content", Value = "height: max-content" },
                new() { Label = "min-content", Value = "height: min-content" },
                new() { Label = "100%", Value        = "height: 100%" },
                new() { Label = "75%", Value         = "height: 75%" },
                new() { Label = "50%", Value         = "height: 50%" },
                new() { Label = "25%", Value         = "height: 25%" }
            ],

            ["width"] =
            [
                new() { Label = "100%", Value        = "width: 100%" },
                new() { Label = "75%", Value         = "width: 75%" },
                new() { Label = "50%", Value         = "width: 50%" },
                new() { Label = "25%", Value         = "width: 25%" },
                new() { Label = "auto", Value        = "width: auto" },
                new() { Label = "fit-content", Value = "width: fit-content" },
                new() { Label = "max-content", Value = "width: max-content" },
                new() { Label = "min-content", Value = "width: min-content" },

                new() { Label = "8px", Value   = "width: 8px" },
                new() { Label = "12px", Value  = "width: 12px" },
                new() { Label = "16px", Value  = "width: 16px" },
                new() { Label = "20px", Value  = "width: 20px" },
                new() { Label = "24px", Value  = "width: 24px" },
                new() { Label = "32px", Value  = "width: 32px" },
                new() { Label = "36px", Value  = "width: 36px" },
                new() { Label = "40px", Value  = "width: 40px" },
                new() { Label = "50px", Value  = "width: 50px" },
                new() { Label = "80px", Value  = "width: 80px" },
                new() { Label = "100px", Value = "width: 100px" },
                new() { Label = "200px", Value = "width: 200px" }
            ],

            ["min | height"] =
            [
                new() { Label = "50px", Value  = "min-height: 50px" },
                new() { Label = "100px", Value = "min-height: 100px" },
                new() { Label = "150px", Value = "min-height: 150px" },
                new() { Label = "200px", Value = "min-height: 200px" },
                new() { Label = "300px", Value = "min-height: 300px" },
                new() { Label = "400px", Value = "min-height: 400px" },
                new() { Label = "500px", Value = "min-height: 500px" }
            ]
        }, 

        ["Font"] = new()
        {
            ["size"] =
            [
                new() { Label = "small", Value  = "font-size: small" },
                new() { Label = "medium", Value = "font-size: medium" },
                new() { Label = "large", Value  = "font-size: large" },
            ],
            ["weight"] =
            [
                new() { Label = "normal", Value  = "font-weight: normal" },
                new() { Label = "bold", Value    = "font-weight: bold" },
                new() { Label = "bolder", Value  = "font-weight: bolder" },
                new() { Label = "lighter", Value = "font-weight: lighter" },
                new() { Label = "100", Value     = "font-weight: 100" },
                new() { Label = "900", Value     = "font-weight: 900" }
            ],
            ["family"] =
            [
                new() { Label = "Arial", Value           = "font-family: Arial, sans-serif" },
                new() { Label = "Times New Roman", Value = "font-family: 'Times New Roman', serif" },
                new() { Label = "Courier New", Value     = "font-family: 'Courier New', monospace" }
            ],
            ["decoration"] =
            [
                new() { Label = "underline", Value    = "text-decoration: underline" },
                new() { Label = "overline", Value     = "text-decoration: overline" },
                new() { Label = "line-through", Value = "text-decoration: line-through" }
            ],
            ["height"] =
            [
                new() { Label = "1", Value   = "line-height: 1" },
                new() { Label = "1.5", Value = "line-height: 1.5" },
                new() { Label = "2", Value   = "line-height: 2" }
            ],
            ["spacing"] =
            [
                new() { Label = "normal", Value = "letter-spacing: normal" },
                new() { Label = "1px", Value    = "letter-spacing: 1px" },
                new() { Label = "2px", Value    = "letter-spacing: 2px" }
            ], 
            ["align"] =
            [
                new() { Label = "left", Value    = "text-align: left" },
                new() { Label = "center", Value  = "text-align: center" },
                new() { Label = "right", Value   = "text-align: right" },
                new() { Label = "justify", Value = "text-align: justify" }
            ],
           
            ["white space"] =
            [
                new() { Label = "normal", Value   = "white-space: normal" },
                new() { Label = "nowrap", Value   = "white-space: nowrap" },
                new() { Label = "pre", Value      = "white-space: pre" },
                new() { Label = "pre-wrap", Value = "white-space: pre-wrap" },
                new() { Label = "pre-line", Value = "white-space: pre-line" }
            ],
            ["color"] =
            [
                new() { Label = "Black", Value = "color: #000000" },
                new() { Label = "White", Value = "color: #ffffff" },
                new() { Label = "Red", Value   = "color: red" },
                new() { Label = "Blue", Value  = "color: blue" }
            ]
        },

        ["Border"] = new()
        {
            ["border style"] =
            [
                new() { Label = "none", Value   = "border-style: none" },
                new() { Label = "solid", Value  = "border-style: solid" },
                new() { Label = "dashed", Value = "border-style: dashed" },
                new() { Label = "dotted", Value = "border-style: dotted" }
            ],
            ["border width"] =
            [
                new() { Label = "1px", Value = "border-width: 1px" },
                new() { Label = "2px", Value = "border-width: 2px" },
                new() { Label = "4px", Value = "border-width: 4px" }
            ],
            ["border radius"] =
            [
                new() { Label = "0", Value   = "border-radius: 0" },
                new() { Label = "4px", Value = "border-radius: 4px" },
                new() { Label = "8px", Value = "border-radius: 8px" },
                new() { Label = "50%", Value = "border-radius: 50%" }
            ],
            ["box shadow"] =
            [
                new() { Label = "none", Value   = "box-shadow: none" },
                new() { Label = "small", Value  = "box-shadow: 0 1px 3px rgba(0,0,0,0.2)" },
                new() { Label = "medium", Value = "box-shadow: 0 4px 6px rgba(0,0,0,0.2)" },
                new() { Label = "large", Value  = "box-shadow: 0 10px 15px rgba(0,0,0,0.3)" }
            ]
        },

        ["Space"] = new()
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
                new() { Label = "2px", Value  = "gap: 2px" },
                new() { Label = "4px", Value  = "gap: 4px" },
                new() { Label = "8px", Value  = "gap: 8px" },
                new() { Label = "12px", Value = "gap: 12px" },
                new() { Label = "16px", Value = "gap: 16px" }
            ]
        },

        ["bg"] = new()
        {
            ["background color"] =
            [
                new() { Label = "White", Value       = "background-color: #ffffff" },
                new() { Label = "Black", Value       = "background-color: #000000" },
                new() { Label = "Red", Value         = "background-color: red" },
                new() { Label = "Blue", Value        = "background-color: blue" },
                new() { Label = "Transparent", Value = "background-color: transparent" }
            ],
            ["background size"] =
            [
                new() { Label = "auto", Value    = "background-size: auto" },
                new() { Label = "cover", Value   = "background-size: cover" },
                new() { Label = "contain", Value = "background-size: contain" }
            ],
            ["background repeat"] =
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
                new() { Label = "0.1", Value = "opacity: 0.1" },
                new() { Label = "0.2", Value = "opacity: 0.2" },
                new() { Label = "0.3", Value = "opacity: 0.3" },
                new() { Label = "0.4", Value = "opacity: 0.4" },
                new() { Label = "0.5", Value = "opacity: 0.5" },
                new() { Label = "0.6", Value = "opacity: 0.6" },
                new() { Label = "0.7", Value = "opacity: 0.7" },
                new() { Label = "0.8", Value = "opacity: 0.8" },
                new() { Label = "0.9", Value = "opacity: 0.9" }
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
            ["object fit"] =
            [
                new() { Label = "cover", Value      = "object-fit: cover" },
                new() { Label = "contain", Value    = "object-fit: contain" },
                new() { Label = "fill", Value       = "object-fit: fill" },
                new() { Label = "none", Value       = "object-fit: none" },
                new() { Label = "scale-down", Value = "object-fit: scale-down" }
            ],
            ["cursor"] =
            [
                new() { Label = "auto", Value        = "cursor: auto" },
                new() { Label = "default", Value     = "cursor: default" },
                new() { Label = "pointer", Value     = "cursor: pointer" },
                new() { Label = "wait", Value        = "cursor: wait" },
                new() { Label = "text", Value        = "cursor: text" },
                new() { Label = "move", Value        = "cursor: move" },
                new() { Label = "not-allowed", Value = "cursor: not-allowed" },
                new() { Label = "crosshair", Value   = "cursor: crosshair" }
            ],
            ["visibility"] =
            [
                new() { Label = "visible", Value  = "visibility: visible" },
                new() { Label = "hidden", Value   = "visibility: hidden" },
                new() { Label = "collapse", Value = "visibility: collapse" }
            ],
            ["pointer events"] =
            [
                new() { Label = "auto", Value = "pointer-events: auto" },
                new() { Label = "none", Value = "pointer-events: none" }
            ],
            ["clip path"] =
            [
                new() { Label = "circle", Value  = "clip-path: circle(50%)" },
                new() { Label = "inset", Value   = "clip-path: inset(10px)" },
                new() { Label = "polygon", Value = "clip-path: polygon(50% 0, 100% 100%, 0 100%)" }
            ]
        },
        
        ["Other2"] = new()
        {
            ["object fit"] =
            [
                new() { Label = "cover", Value      = "object-fit: cover" },
                new() { Label = "contain", Value    = "object-fit: contain" },
                new() { Label = "fill", Value       = "object-fit: fill" },
                new() { Label = "none", Value       = "object-fit: none" },
                new() { Label = "scale-down", Value = "object-fit: scale-down" }
            ],
            ["cursor"] =
            [
                new() { Label = "auto", Value        = "cursor: auto" },
                new() { Label = "default", Value     = "cursor: default" },
                new() { Label = "pointer", Value     = "cursor: pointer" },
                new() { Label = "wait", Value        = "cursor: wait" },
                new() { Label = "text", Value        = "cursor: text" },
                new() { Label = "move", Value        = "cursor: move" },
                new() { Label = "not-allowed", Value = "cursor: not-allowed" },
                new() { Label = "crosshair", Value   = "cursor: crosshair" }
            ],
            ["visibility"] =
            [
                new() { Label = "visible", Value  = "visibility: visible" },
                new() { Label = "hidden", Value   = "visibility: hidden" },
                new() { Label = "collapse", Value = "visibility: collapse" }
            ],
            ["pointer events"] =
            [
                new() { Label = "auto", Value = "pointer-events: auto" },
                new() { Label = "none", Value = "pointer-events: none" }
            ],
            ["clip path"] =
            [
                new() { Label = "circle", Value  = "clip-path: circle(50%)" },
                new() { Label = "inset", Value   = "clip-path: inset(10px)" },
                new() { Label = "polygon", Value = "clip-path: polygon(50% 0, 100% 100%, 0 100%)" }
            ]
        }
    };

    bool IsFontSizeEditor => TryGetSubGroupLabelAt(0) == "size";

    
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
                new div(PositionAbsolute, Left(0), Bottom(-4), DisplayFlex, Right(0), JustifyContentSpaceAround)
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
                new div(PositionAbsolute, Left(-6), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
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
                new div(PositionAbsolute, Right(-2), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
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
                    new div(PositionAbsolute, Left(0), Top(-12), DisplayFlex, Right(0), JustifyContentSpaceAround)
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
                    new div(PositionAbsolute, Left(0), Bottom(-4), DisplayFlex, Right(0), JustifyContentSpaceAround)
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
                    new div(PositionAbsolute, Left(-6), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
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
                    new div(PositionAbsolute, Right(-2), Top(0), DisplayFlex, Bottom(0), JustifyContentSpaceAround, FlexDirectionColumn, Width(7))
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
                    new div(DisplayFlex, Padding(16), HeightFull, WidthFull, AlignItemsCenter, JustifyContentCenter)
                    {
                        !HasAnyActiveSubGroup ? null :
                            new div(WidthFull, HeightFull, DisplayFlex, Gap(8))
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
                                            TargetCssName= ActiveSubGroup.TargetCssName
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

    bool HasAnyActiveGroup=>ActiveGroup != null;
    
    bool HasAnyActiveSubGroup=>ActiveSubGroup != null;
    
    GroupModel ActiveGroup =>
        FirstOrDefaultOf(from g in AllGroups
                where g.Label == state.SelectedGroupName
                select g);
    
    SubGroupItemModel ActiveSubGroup =>
        FirstOrDefaultOf(from g in AllGroups
                         where g.Label == state.SelectedGroupName
                         from sg in g.SubGroups
                         where sg.Label == state.SelectedSubGroupName
                         select sg);
    
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

        FirstOf(from g in AllGroups
                where g.Label == groupName
                from sg in g.SubGroups
                where sg.Label == subGroupName
                select sg);
        
        return AllGroups.First(x => x.Label == groupName).SubGroups.First(x => x.Label == subGroupName).Suggestions;
    }

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

        public bool IsVerticle { get; init; }
        
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
                return new div(Opacity(0.2), BorderColor(Gray200), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFull, HeightFull, TextAlignCenter, Border(1, solid, Gray200));
            }

            return new div(OnMouseEnter(OnGroupItemMouseEnter), Id(Label), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFitContent, HeightFitContent, TextAlignCenter, Padding(4), LineHeight16, Background(White), DisplayFlex, IsVerticle ? FlexWrap : FlexNoWrap, Gap(5), IsSelected ? BorderColor(Gray400) : BorderColor(Gray100), Border(1, solid, transparent))
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

            return new div(OnMouseEnter(OnSubGroupItemMouseEnter), Id(Label), BorderColor(IsSelected ?  Gray400 : Gray100), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, WidthFitContent, HeightFitContent, TextAlignCenter, Padding(3), LineHeight16, Background(White), DisplayFlex, FlexWrap, Gap(4), Border(1, solid, transparent))
            {
                from item in GetChars()
                select new div(WidthFitContent, HeightFitContent, LineHeight7)
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
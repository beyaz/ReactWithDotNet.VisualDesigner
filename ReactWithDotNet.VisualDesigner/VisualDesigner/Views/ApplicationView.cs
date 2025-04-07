using System.Text;
using Dapper.Contrib.Extensions;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;
using Page = ReactWithDotNet.WebSite.Page;

namespace ReactWithDotNet.VisualDesigner.Views;

sealed class ApplicationView : Component<ApplicationState>
{
    enum Icon
    {
        add,
        remove
    }

    PropertyGroupModel CurrentStyleGroup => CurrentVisualElement.StyleGroups[state.Selection.StyleGroupIndex!.Value];

    VisualElementModel CurrentVisualElement => FindTreeNodeByTreePath(state.ComponentRootElement, state.Selection.VisualElementTreeItemPath);

    protected override async Task constructor()
    {
        var userName = Environment.UserName; // future: get userName from cookie or url

        Client.ListenEvent("Change_VisualElementTreeItemPath", treeItemPath =>
        {
            state.Selection = new ()
            {
                VisualElementTreeItemPath = treeItemPath
            };

            return Task.CompletedTask;
        });
        
        
        // try take from memory cache
        {
            var userLastState = GetUserLastState(userName);
            if (userLastState is not null)
            {
                state = userLastState;

                state.IsActionButtonsVisible = false;

                return;
            }
        }

        // try take from db cache
        {
            var lastUsage = (await GetLastUsageInfoByUserName(userName)).FirstOrDefault();
            if (lastUsage is not null)
            {
                state = DeserializeFromJson<ApplicationState>(lastUsage.StateAsJson);

                state.IsActionButtonsVisible = false;

                return;
            }
        }

        // create new state

        state = new()
        {
            UserName = userName,

            Preview = new()
            {
                Width  = 600,
                Height = 100,
                Scale  = 100
            },

            Selection = new()
        };

        var projectId = await GetFirstProjectId();
        if (projectId.HasValue)
        {
            await ChangeSelectedProject(projectId.Value);
        }
    }

    protected override Task OverrideStateFromPropsBeforeRender()
    {
        SetUserLastState(state);

        Client.RefreshComponentPreview();

        return Task.CompletedTask;
    }

    protected override Element render()
    {
        return new FlexRow(Padding(10), SizeFull, Background(Theme.BackgroundColor))
        {
            EditorFontLinks,
            EditorFont(),
            new FlexColumn
            {
                PartApplicationTopPanel,

                new FlexRow(Flex(1, 1, 0), OverflowYAuto)
                {
                    MainContent
                },

                new Style
                {
                    Border(Solid(1, Theme.BorderColor)),
                    SizeFull,
                    BorderRadius(10),
                    BoxShadow(0, 30, 30, 0, rgba(69, 42, 124, 0.15))
                },
                NotificationHost
            }
        };
    }

    static FlexRowCentered CreateIcon(Icon name, int size, Modifier[] modifiers = null)
    {
        Style style = [Size(size), BorderRadius(16), BorderWidth(1), BorderStyle(solid), BorderColor(Gray200), Color(Gray200)];

        if (name == Icon.add)
        {
            return new(style)
            {
                new IconPlus(),

                Hover(BorderColor(Blue300), Color(Blue300)),
                modifiers
            };
        }

        if (name == Icon.remove)
        {
            return new(style)
            {
                new IconMinus(), modifiers
            };
        }

        return null;
    }

    Task AddNewLayerClicked(MouseEvent e)
    {
        // add as root
        if (state.ComponentRootElement is null)
        {
            state.ComponentRootElement = new()
            {
                Tag = "div"
            };

            state.Selection = new()
            {
                VisualElementTreeItemPath = "0"
            };

            return Task.CompletedTask;
        }

        var selection = state.Selection;

        var node = FindTreeNodeByTreePath(state.ComponentRootElement, selection.VisualElementTreeItemPath);

        node.Children.Add(new()
        {
            Tag = "div",
            StyleGroups = [new (){ Condition = "*"}]
        });
        
        return Task.CompletedTask;
    }

    async Task ChangeSelectedComponent(string componentName)
    {
        ComponentEntity component;
        {
            var componentResult = await GetComponenUserOrMainVersion(state.ProjectId, componentName, state.UserName);
            if (componentResult.HasError)
            {
                this.FailNotification(componentResult.Error.Message);
                return;
            }

            component = componentResult.Value;
            if (component is null)
            {
                this.FailNotification($"Component not found. @{componentName}");
                return;
            }
        }

        var componentRootElement = DeserializeFromJson<VisualElementModel>(component.RootElementAsJson ?? string.Empty);

        state = new()
        {
            UserName = state.UserName,

            ProjectId = state.ProjectId,

            Preview = state.Preview,

            LeftPanelSelectedTab = LeftPanelTab.Layers,

            ComponentName = componentName,

            ComponentRootElement = componentRootElement,

            Selection = new()
        };
    }

    async Task ChangeSelectedProject(int projectId)
    {
        var userName = Environment.UserName; // future: get userName from cookie or url

        // try take from db cache
        {
            var lastUsage = (await GetLastUsageInfoByUserName(userName)).FirstOrDefault();
            if (lastUsage is not null)
            {
                state = DeserializeFromJson<ApplicationState>(lastUsage.StateAsJson);

                return;
            }
        }

        state = new()
        {
            UserName = state.UserName,

            ProjectId = projectId,

            Preview = state.Preview,

            LeftPanelSelectedTab = LeftPanelTab.Layers,

            Selection = new()
        };

        // try select first component
        {
            var component = await GetFirstComponentInProject(projectId);
            if (component is null)
            {
                return;
            }

            await ChangeSelectedComponent(component.Name);
        }
    }

    Element createHorizontalRuler()
    {
        const int step = 50;
        var max = state.Preview.Width / step + 1;

        return new FlexRow(PositionRelative, WidthFull, Height(20))
        {
            Enumerable.Range(0, max).Select(number => new div(PositionAbsolute)
            {
                Bottom(3), Left(number * step),
                new FlexColumn(FontSize8, LineHeight6, FontWeight500, Gap(4))
                {
                    new div(MarginLeft(calculateMarginForCenterizeLabel(number)))
                    {
                        (number * step).ToString()
                    },
                    new div(BorderRadius(3))
                    {
                        Width(0.5),
                        Height(7),

                        Background("green")
                    }
                }
            }),
            createTenPoints()
        };

        IReadOnlyList<Element> createTenPoints()
        {
            var returnList = new List<Element>();

            var miniStep = 10;

            var cursor = 0;
            var distance = miniStep;
            while (distance <= state.Preview.Width)
            {
                cursor++;

                distance = cursor * miniStep;

                if (distance % step == 0 || distance > state.Preview.Width)
                {
                    continue;
                }

                returnList.Add(new div(PositionAbsolute)
                {
                    Bottom(3),
                    Left(distance),

                    Width(0.5),
                    Height(4),
                    Background("green")
                });
            }

            return returnList;
        }

        double calculateMarginForCenterizeLabel(int stepNumber)
        {
            var label = stepNumber * step;

            if (label < 10)
            {
                return -2;
            }

            if (label < 100)
            {
                return -4.5;
            }

            if (label < 1000)
            {
                return -7;
            }

            return -9;
        }
    }

    Task JsonTextInComponentSettingsUpdatedByUser()
    {
        state.JsonText = JsonPrettify(state.JsonText);

        switch (state.LeftPanelSelectedTab)
        {
            case LeftPanelTab.Props:
                return UpdateUserVersion(state, x => x with { PropsAsJson = state.JsonText });

            case LeftPanelTab.State:
                return UpdateUserVersion(state, x => x with { StateAsJson = state.JsonText });

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    Task LayersTabRemoveSelectedItemClicked(MouseEvent e)
    {
        var intArray = state.Selection.VisualElementTreeItemPath.Split(',');
        if (intArray.Length == 1)
        {
            state.ComponentRootElement = null;
        }
        else
        {
            var node = state.ComponentRootElement;

            for (var i = 1; i < intArray.Length - 1; i++)
            {
                node = node.Children[int.Parse(intArray[i])];
            }

            node.Children.RemoveAt(int.Parse(intArray[^1]));
        }

        state.Selection = new();

        return Task.CompletedTask;
    }

    async Task<Element> MainContent()
    {
        var scaleStyle = TransformOrigin("0 0") + Transform($"scale({state.Preview.Scale / (double)100})");

        return new SplitRow
        {
            sizes = [20, 60, 20],
            children =
            {
                await PartLeftPanel() + BorderBottomLeftRadius(8) + OverflowAuto,

                new FlexColumn(state.Preview.Width < 768 ? AlignItemsCenter : AlignItemsFlexStart, FlexGrow(1), Padding(7), MarginLeft(40), scaleStyle, OverflowXAuto)
                {
                    createHorizontalRuler() + Width(state.Preview.Width) + MarginTop(12),
                    PartPreview
                },

                await PartRightPanel() + BorderBottomRightRadius(8)
            }
        };
    }

    Task OnCommonSizeClicked(MouseEvent e)
    {
        state.Preview.Width = e.currentTarget.data["value"] switch
        {
            "M"   => 320,
            "SM"  => 640,
            "MD"  => 768,
            "LG"  => 1024,
            "XL"  => 1280,
            "XXL" => 1536,
            _     => throw new ArgumentOutOfRangeException()
        };

        return Task.CompletedTask;
    }

    Task OnComponentNameChanged(string newValue)
    {
        return ChangeSelectedComponent(newValue);
    }

    Element PartApplicationTopPanel()
    {
        return new FlexRow(UserSelect(none))
        {
            new FlexRowCentered(Gap(16))
            {
                new h3 { "React Visual Designer" },

                PartProject,

                // A C T I O N S
                SpaceX(8),
                new FlexRowCentered
                {
                    OnMouseEnter(ToggleIsActionButtonsVisible), OnMouseLeave(ToggleIsActionButtonsVisible),

                    new FlexRowCentered(Gap(16), Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(8))
                    {
                        PositionRelative,
                        new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "Component" },

                        state.IsActionButtonsVisible is false ? VisibilityHidden : null,

                        new FlexRowCentered(Hover(Color(Blue300)))
                        {
                            "Rollback",
                            OnClick(_ =>
                            {
                                this.SuccessNotification("Rollback ok");

                                return Task.CompletedTask;
                            })
                        },

                        new FlexRowCentered(Hover(Color(Blue300)))
                        {
                            "Commit",
                            OnClick(async _ =>
                            {
                                var result = await CommitComponent(state);
                                if (result.HasError)
                                {
                                    this.SuccessNotification(result.Error.Message);

                                    return;
                                }

                                this.SuccessNotification("OK");
                            })
                        },

                        new FlexRowCentered(Hover(Color(Blue300)))
                        {
                            "Export",
                            OnClick(_ =>
                            {
                                Exporter_For_NextJs_with_Tailwind.Export(state);
                                
                                this.SuccessNotification("OK");

                                return Task.CompletedTask;
                            })
                        }
                    }
                }
            },
            new FlexRowCentered(Gap(32))
            {
                new FlexRowCentered(Gap(4))
                {
                    new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        OnClick(_ =>
                        {
                            state.Preview.Width -= 10;

                            return Task.CompletedTask;
                        }),

                        new IconMinus()
                    },
                    $"{state.Preview.Width}px",
                    new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        OnClick(_ =>
                        {
                            state.Preview.Width += 10;

                            return Task.CompletedTask;
                        }),
                        new IconPlus()
                    }
                },
                PartMediaSizeButtons,

                PartScale
            },

            new LogoutButton(),

            new Style
            {
                JustifyContentSpaceBetween,
                AlignItemsCenter,
                BorderBottom(Solid(1, Theme.BorderColor)),
                Padding(5, 30)
            }
        };
    }

    async Task<Element> PartLeftPanel()
    {
        var componentSelector = new MagicInput
        {
            Name = string.Empty,

            Suggestions       = await GetSuggestionsForComponentSelection(state),
            Value             = state.ComponentName,
            OnChange          = (_, componentName) => OnComponentNameChanged(componentName),
            IsTextAlignCenter = true,
            IsBold            = true
        };

        var removeIconInLayersTab = CreateIcon(Icon.remove, 16);
        if (state.LeftPanelSelectedTab == LeftPanelTab.Layers && state.Selection.VisualElementTreeItemPath.HasValue())
        {
            removeIconInLayersTab.Add(Hover(Color(Blue300), BorderColor(Blue300)), OnClick(LayersTabRemoveSelectedItemClicked));
        }
        else
        {
            removeIconInLayersTab.Add(VisibilityCollapse);
        }

        var addIconInLayersTab = CreateIcon(Icon.add, 16);
        if (state.LeftPanelSelectedTab == LeftPanelTab.Layers && (state.ComponentRootElement is null || state.Selection.VisualElementTreeItemPath.HasValue()))
        {
            addIconInLayersTab.Add(Hover(Color(Blue300), BorderColor(Blue300)), OnClick(AddNewLayerClicked));
        }
        else
        {
            addIconInLayersTab.Add(VisibilityCollapse);
        }

        return new FlexColumn(WidthFull, AlignItemsCenter, BorderRight(1, dotted, "#d9d9d9"), Background(White))
        {
            componentSelector,
            new FlexRow(WidthFull, FontWeightBold, AlignItemsCenter, Padding(8, 4), JustifyContentSpaceAround, BorderBottom(1, dotted, "#d9d9d9"), BorderTop(1, dotted, "#d9d9d9"))
            {
                Color(Gray300), CursorDefault, UserSelect(none),

                new FlexRowCentered(WidthFull)
                {
                    removeIconInLayersTab,

                    new FlexRowCentered(WidthFull)
                    {
                        new IconLayers() + Size(18) + (state.LeftPanelSelectedTab == LeftPanelTab.Layers ? Color(Gray500) : null),

                        OnClick(_ =>
                        {
                            state.LeftPanelSelectedTab = LeftPanelTab.Layers;

                            return Task.CompletedTask;
                        })
                    },

                    addIconInLayersTab
                },

                new FlexRowCentered(WidthFull, When(state.LeftPanelSelectedTab == LeftPanelTab.Props, Color(Gray500)))
                {
                    "Props",
                    PaddingX(8), OnClick(async _ =>
                    {
                        state.LeftPanelSelectedTab = LeftPanelTab.Props;

                        ShowErrorIfExist(await GetComponenUserOrMainVersion(state).Then(x => { state.JsonText = x.PropsAsJson; }));
                    })
                },
                new FlexRowCentered(WidthFull, Opacity(0.7), When(state.LeftPanelSelectedTab == LeftPanelTab.State, Color(Gray500)))
                {
                    "State",
                    PaddingX(8), OnClick(async _ =>
                    {
                        state.LeftPanelSelectedTab = LeftPanelTab.State;

                        ShowErrorIfExist(await GetComponenUserOrMainVersion(state).Then(x => { state.JsonText = x.StateAsJson; }));
                    })
                }
            },

            When(state.LeftPanelSelectedTab == LeftPanelTab.Layers, () => new VisualElementTreeView
            {
                Model = state.ComponentRootElement,

                SelectedPath = state.Selection.VisualElementTreeItemPath,

                TreeItemHover = treeItemPath =>
                {
                    state.Selection.VisualElementTreeItemPathHover = treeItemPath;

                    return Task.CompletedTask;
                },
                MouseLeave = () =>
                {
                    state.Selection.VisualElementTreeItemPathHover = null;
                    return Task.CompletedTask;
                },
                SelectionChanged = treeItemPath =>
                {
                    state.Selection = new()
                    {
                        VisualElementTreeItemPath = treeItemPath
                    };

                    return Task.CompletedTask;
                },

                CopyPaste = (source, target) =>
                {
                    var sourceNode = FindTreeNodeByTreePath(state.ComponentRootElement, source);
                    var targetNode = FindTreeNodeByTreePath(state.ComponentRootElement, target);

                    var sourceNodeClone = DeserializeFromJson<VisualElementModel>(SerializeToJson(sourceNode));

                    targetNode.Children.Add(sourceNodeClone);
                    
                    return Task.CompletedTask;
                },
                TreeItemMove = (source, target, position) =>
                {
                    // root check
                    {
                        if (source == "0")
                        {
                            this.FailNotification("Root node cannot move.");

                            return Task.CompletedTask;
                        }
                    }

                    // parent - child control
                    {
                        if (target.StartsWith(source, StringComparison.OrdinalIgnoreCase))
                        {
                            this.FailNotification("Parent node cannot add to child.");

                            return Task.CompletedTask;
                        }
                    }

                    // same target control
                    {
                        if (source == target)
                        {
                            return Task.CompletedTask;
                        }
                    }

                    VisualElementModel sourceNodeParent;
                    int sourceNodeIndex;
                    {
                        var temp = state.ComponentRootElement;

                        var indexArray = source.Split(',');

                        var length = indexArray.Length - 1;
                        for (var i = 1; i < length; i++)
                        {
                            temp = temp.Children[int.Parse(indexArray[i])];
                        }

                        sourceNodeIndex = int.Parse(indexArray[length]);

                        sourceNodeParent = temp;
                    }

                    VisualElementModel targetNodeParent;
                    int targetNodeIndex;
                    {
                        var temp = state.ComponentRootElement;

                        var indexArray = target.Split(',');

                        var length = indexArray.Length - 1;
                        for (var i = 1; i < length; i++)
                        {
                            temp = temp.Children[int.Parse(indexArray[i])];
                        }

                        targetNodeIndex = int.Parse(indexArray[length]);

                        targetNodeParent = temp;
                    }

                    if (position == DragPosition.Inside)
                    {
                        var sourceNode = sourceNodeParent.Children[sourceNodeIndex];

                        var targetNode = targetNodeParent.Children[targetNodeIndex];

                        if (targetNode.Children.Count > 0)
                        {
                            this.FailNotification("Select valid location");

                            return Task.CompletedTask;
                        }

                        // remove from source
                        sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

                        if (targetNode.HasNoChild())
                        {
                            targetNode.Children.Add(sourceNode);

                            state.Selection = new();

                            return Task.CompletedTask;
                        }
                    }

                    // is same parent
                    if (sourceNodeParent == targetNodeParent)
                    {
                        if (position == DragPosition.After && sourceNodeIndex - targetNodeIndex == 1)
                        {
                            return Task.CompletedTask;
                        }

                        if (position == DragPosition.Before && targetNodeIndex - sourceNodeIndex == 1)
                        {
                            return Task.CompletedTask;
                        }
                    }

                    {
                        var sourceNode = sourceNodeParent.Children[sourceNodeIndex];

                        // remove from source
                        sourceNodeParent.Children.RemoveAt(sourceNodeIndex);

                        if (sourceNodeParent == targetNodeParent)
                        {
                            // is adding end
                            if (position == DragPosition.After && targetNodeIndex == targetNodeParent.Children.Count)
                            {
                                targetNodeParent.Children.Insert(targetNodeIndex, sourceNode);

                                state.Selection = new();

                                return Task.CompletedTask;
                            }

                            if (position == DragPosition.After && targetNodeIndex == 0)
                            {
                                targetNodeIndex++;
                            }

                            if (position == DragPosition.Before && targetNodeIndex == targetNodeParent.Children.Count)
                            {
                                targetNodeIndex--;
                            }
                        }

                        // insert into target
                        targetNodeParent.Children.Insert(targetNodeIndex, sourceNode);

                        state.Selection = new();
                    }

                    return Task.CompletedTask;
                }
            }),

            When(state.LeftPanelSelectedTab == LeftPanelTab.Props || state.LeftPanelSelectedTab == LeftPanelTab.State, () => new FlexColumnCentered(SizeFull)
            {
                new Editor
                {
                    defaultLanguage          = "json",
                    valueBind                = () => state.JsonText,
                    valueBindDebounceTimeout = 700,
                    valueBindDebounceHandler = JsonTextInComponentSettingsUpdatedByUser,
                    options =
                    {
                        renderLineHighlight = "none",
                        fontFamily          = "'IBM Plex Mono Medium', 'Courier New', monospace",
                        fontSize            = 11,
                        minimap             = new { enabled = false },
                        formatOnPaste       = true,
                        formatOnType        = true,
                        automaticLayout     = true,
                        lineNumbers         = false
                    }
                }
            })
        };
    }

    Element PartMediaSizeButtons()
    {
        return new FlexRow(JustifyContentSpaceAround, AlignItemsCenter, Gap(16))
        {
            new[] { "M", "SM", "MD", "LG", "XL", "XXL" }.Select(x => new FlexRowCentered
            {
                x,
                FontSize16,
                FontWeight300,
                CursorDefault,
                PaddingTopBottom(3),
                FlexGrow(1),

                Data("value", x),
                OnClick(OnCommonSizeClicked),
                Hover(Color("#2196f3")),

                (x == "M" && state.Preview.Width == 320) ||
                (x == "SM" && state.Preview.Width == 640) ||
                (x == "MD" && state.Preview.Width == 768) ||
                (x == "LG" && state.Preview.Width == 1024) ||
                (x == "XL" && state.Preview.Width == 1280) ||
                (x == "XXL" && state.Preview.Width == 1536)
                    ? FontWeight500 + Color("#2196f3")
                    : null
            })
        };
    }

    Element PartPreview()
    {
        return new FlexRow(JustifyContentFlexStart, PositionRelative)
        {
            BackgroundImage("radial-gradient(#a5a8ed 0.5px, #f8f8f8 0.5px)"),
            BackgroundSize("10px 10px"),

            createVerticleRuler,
            createElement(),

            Width(state.Preview.Width),
            Height(state.Preview.Height * percent),
            BoxShadow(0, 4, 12, 0, rgba(0, 0, 0, 0.1))
        };

        static Element createVerticleRuler()
        {
            const int maxHeight = 5000;

            const int step = 50;
            const int max = maxHeight / step + 1;

            return new div(SizeFull, Width(30), MarginLeft(-30), OverflowHidden, PositionRelative)
            {
                Enumerable.Range(0, max).Select(number => new div(PositionAbsolute)
                {
                    Right(3), Top(number * step),
                    new FlexRow(FontSize8, LineHeight6, FontWeight500, Gap(4))
                    {
                        new div(MarginTop(number == 0 ? 0 : -3))
                        {
                            (number * step).ToString()
                        },
                        new div
                        {
                            Height(0.5),
                            Width(7),

                            Background("green")
                        }
                    }
                }),

                createTenPoints()
            };

            IReadOnlyList<Element> createTenPoints()
            {
                var returnList = new List<Element>();

                var miniStep = 10;

                var cursor = 0;
                var distance = miniStep;
                while (distance <= maxHeight)
                {
                    cursor++;

                    distance = cursor * miniStep;

                    if (distance % step == 0 || distance > maxHeight)
                    {
                        continue;
                    }

                    returnList.Add(new div(PositionAbsolute)
                    {
                        Right(3),
                        Top(distance),

                        Height(0.5),
                        Width(4),
                        Background("green")
                    });
                }

                return returnList;
            }
        }

        static Element createElement()
        {
            return new iframe
            {
                id    = "ComponentPreview",
                src   = Page.VisualDesignerPreview.Url,
                style = { BorderNone, WidthFull, HeightFull },
                title = "Component Preview"
            };
        }
    }

    Element PartProject()
    {
        return new FlexRowCentered(Gap(4))
        {
            new FlexRowCentered
            {
                new IconPlus() + Size(24) + Color(state.IsProjectSettingsPopupVisible ? Gray600 : Gray300) + Hover(Color(Gray600)),

                OnClick(_ =>
                {
                    state.IsProjectSettingsPopupVisible = !state.IsProjectSettingsPopupVisible;

                    return Task.CompletedTask;
                }),

                state.IsProjectSettingsPopupVisible ? PositionRelative : null,
                state.IsProjectSettingsPopupVisible ? new FlexColumn(PositionAbsolute, Top(24), Left(16), Zindex2)
                {
                    Background(White), Border(Solid(1, Theme.BorderColor)), BorderRadius(4), Padding(8),

                    Width(300),

                    new MagicInput
                    {
                        Placeholder = "New component name",
                        Name        = string.Empty,
                        Value       = string.Empty,
                        AutoFocus   = true,
                        OnChange = async (_, newValue) =>
                        {
                            if (newValue.HasNoValue())
                            {
                                this.FailNotification("Component name is empty.");

                                return;
                            }

                            if ((await GetAllComponentNamesInProject(state.ProjectId)).Any(name => name == newValue))
                            {
                                this.FailNotification("Has already same named component.");

                                return;
                            }

                            var newDbRecord = new ComponentEntity
                            {
                                Name      = newValue,
                                ProjectId = state.ProjectId,
                                UserName  = state.UserName,
                                LastAccessTime = DateTime.Now
                            };

                            await DbOperation(db => db.InsertAsync(newDbRecord));

                            await OnComponentNameChanged(newValue);

                            state.IsProjectSettingsPopupVisible = false;
                        }
                    }
                } : null
            },

            new MagicInput
            {
                Name = string.Empty,

                Suggestions = GetProjectNames(state),
                Value       = GetAllProjects().FirstOrDefault(p => p.Id == state.ProjectId)?.Name,
                OnChange = async (_, projectName) =>
                {
                    var projectEntity = GetAllProjects().FirstOrDefault(x => x.Name == projectName);
                    if (projectEntity is null)
                    {
                        this.FailNotification("Project not found. @" + projectName);

                        return;
                    }

                    await ChangeSelectedProject(projectEntity.Id);
                },
                FitContent = true
            }
        };
    }

    async Task<Element> PartRightPanel()
    {
        VisualElementModel visualElementModel = null;

        if (state.Selection.VisualElementTreeItemPath.HasValue())
        {
            visualElementModel = FindTreeNodeByTreePath(state.ComponentRootElement, state.Selection.VisualElementTreeItemPath);
        }

        if (visualElementModel is null)
        {
            return new div();
        }

        var inputTag = new FlexRow(WidthFull)
        {
            new MagicInput
            {
                Name        = string.Empty,
                Value       = visualElementModel.Tag,
                Suggestions = await GetTagSuggestions(state),
                OnChange = (_, newValue) =>
                {
                    CurrentVisualElement.Tag = newValue;

                    return Task.CompletedTask;
                },
                IsTextAlignCenter = true
            }
        };

        var inputText = new FlexRow(WidthFull, Gap(4))
        {
            new MagicInput
            {
                Placeholder = "text..",
                Name        = string.Empty,
                Value       = visualElementModel.Text,
                OnChange = (_, newValue) =>
                {
                    CurrentVisualElement.Text = newValue;

                    return Task.CompletedTask;
                }
            } + Width(6, 10)
        };

        var stylesHeader = new FlexRow(WidthFull, AlignItemsCenter)
        {
            CreateIcon(Icon.remove, 32, state.Selection.StyleGroupIndex.HasValue ?
                           [
                               OnClick(StyleGroupRemoveClicked),
                               Hover(Color(Blue300))
                           ] :
                           [
                               Color(Gray100),
                               BorderColor(Gray100)
                           ]),

            new div { Height(1), FlexGrow(1), Background(Gray200) },
            new span { "S T Y L E", WhiteSpaceNoWrap, UserSelect(none), PaddingX(4) },
            new div { Height(1), FlexGrow(1), Background(Gray200) },

            CreateIcon(Icon.add, 32) + OnClick(StyleGroupAddClicked)
        };

        var propsHeader = new FlexRow(WidthFull, AlignItemsCenter)
        {
            new div { Height(1), FlexGrow(1), Background(Gray200) },
            new span { "P R O P S", WhiteSpaceNoWrap, UserSelect(none), PaddingX(4) },
            new div { Height(1), FlexGrow(1), Background(Gray200) }
        };

        return new FlexColumn(BorderLeft(1, dotted, "#d9d9d9"), PaddingX(2), Gap(8), OverflowYAuto, Background(White))
        {
            inputTag,

            inputText,

            stylesHeader,

            new FlexColumnCentered(WidthFull)
            {
                visualElementModel.StyleGroups?.Select(viewStyleGroup)
            },

            propsHeader,
            viewProps(visualElementModel.Properties)
        };

        Element viewStyleGroup(PropertyGroupModel styleGroup, int styleGroupIndex)
        {
            return new FlexColumn(WidthFull, Gap(4))
            {
                conditionEditor(),
                new FlexRow(FlexWrap, Gap(4))
                {
                    new FlexRow(WidthFull, BorderRadius(4), PaddingLeft(4), Background(WhiteSmoke))
                    {
                        inputEditor()
                    },
                    new FlexRow(WidthFull, FlexWrap, Gap(4))
                    {
                        OnClick(_ =>
                        {
                            state.Selection = state.Selection with { PropertyIndexInStyleGroup = null };

                            return Task.CompletedTask;
                        }),
                        styleGroup.Items?.Select((value, index) => attributeItem(index, value))
                    }
                }
            };

            Element conditionEditor()
            {
                return new FlexRow(WidthFull, AlignItemsCenter, Gap(4), PaddingX(2))
                {
                    new MagicInput
                    {
                        Name = styleGroupIndex.ToString(),
                        OnFocus = senderNameAsStyleGroupIndex =>
                        {
                            state.Selection = new()
                            {
                                VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                                StyleGroupIndex = int.Parse(senderNameAsStyleGroupIndex)
                            };

                            return Task.CompletedTask;
                        },
                        Value             = styleGroup.Condition,
                        IsTextAlignCenter = true,
                        Suggestions       = GetStyleGroupConditionSuggestions(state)
                    } + FlexGrow(1)
                };
            }

            Element inputEditor()
            {
                string value = null;

                if (state.Selection.StyleGroupIndex == styleGroupIndex && state.Selection.PropertyIndexInStyleGroup >= 0)
                {
                    value = styleGroup.Items[state.Selection.PropertyIndexInStyleGroup.Value];
                }

                return new MagicInput
                {
                    Placeholder = "Add style attribute",
                    Suggestions = GetStyleAttributeNameSuggestions(state),
                    Id = new StyleInputLocation
                    {
                        StyleGroupIndex = styleGroupIndex
                    },
                    Name = new StyleInputLocation
                    {
                        StyleGroupIndex      = styleGroupIndex,
                        PropertyIndexInGroup = state.Selection.PropertyIndexInStyleGroup ?? CurrentVisualElement.StyleGroups[styleGroupIndex].Items.Count
                    },
                    OnChange = (senderName, newValue) =>
                    {
                        StyleInputLocation location = senderName;
                        if (state.Selection.StyleGroupIndex is null)
                        {
                            state.Selection = state.Selection with
                            {
                                StyleGroupIndex = location.StyleGroupIndex
                            };
                        }

                        newValue = TryBeautifyPropertyValue(newValue);

                        if (state.Selection.StyleGroupIndex.HasValue && state.Selection.PropertyIndexInStyleGroup.HasValue)
                        {
                            CurrentStyleGroup.Items[state.Selection.PropertyIndexInStyleGroup.Value] = newValue;
                        }
                        else
                        {
                            CurrentStyleGroup.Items.Add(newValue);
                        }

                        state.Selection.PropertyIndexInStyleGroup = null;

                        return Task.CompletedTask;
                    },
                    Value = value
                };
            }

            FlexRowCentered attributeItem(int index, string value)
            {
                var isSelected = index == state.Selection.PropertyIndexInStyleGroup &&
                                 styleGroupIndex == state.Selection.StyleGroupIndex;

                var closeIcon = new FlexRowCentered(Size(20), PositionAbsolute, Top(-8), Right(-8), Padding(4), Background(White),
                                                    Border(0.5, solid, Theme.BorderColor), BorderRadius(24))
                {
                    Color(Gray500), Hover(Color(Blue300), BorderColor(Blue300)),

                    new IconClose() + Size(16),

                    OnClick([StopPropagation](_) =>
                    {
                        CurrentStyleGroup.Items.RemoveAt(state.Selection.PropertyIndexInStyleGroup!.Value);

                        state.Selection.PropertyIndexInStyleGroup = null;

                        return Task.CompletedTask;
                    })
                };

                Element content = value;
                {
                    var (success, name, realValue) = TryParsePropertyValue(value);
                    if (success)
                    {
                        content = new FlexRowCentered
                        {
                            new span(FontWeight500) { name }, ": ", realValue
                        };
                    }
                }
                
                return new(CursorDefault, Padding(4, 8), BorderRadius(16))
                {
                    Background(isSelected ? Gray200 : Gray50),

                    isSelected ? PositionRelative : null,
                    isSelected ? closeIcon : null,

                    content,
                    Id(new StyleInputLocation
                    {
                        StyleGroupIndex      = styleGroupIndex,
                        PropertyIndexInGroup = index
                    }),
                    OnClick([StopPropagation](e) =>
                    {
                        StyleInputLocation location = e.currentTarget.id;

                        state.Selection = new()
                        {
                            VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                            StyleGroupIndex = location.StyleGroupIndex,

                            PropertyIndexInStyleGroup = location.PropertyIndexInGroup
                        };

                        var id = new StyleInputLocation
                        {
                            StyleGroupIndex = location.StyleGroupIndex
                        }.ToString();

                        // calculate js code for focus to input editor
                        {
                            var jsCode = new StringBuilder();

                            jsCode.AppendLine($"document.getElementById('{id}').focus();");

                            // calculate text selection in edit input
                            {
                                var nameValue = CurrentVisualElement.StyleGroups[location.StyleGroupIndex].Items[state.Selection.PropertyIndexInStyleGroup.Value];
                                var (success, _, parsedValue) = TryParsePropertyValue(nameValue);
                                if (success)
                                {
                                    var startIndex = nameValue.LastIndexOf(parsedValue, StringComparison.OrdinalIgnoreCase);
                                    var endIndex = nameValue.Length;

                                    jsCode.AppendLine($"document.getElementById('{id}').setSelectionRange({startIndex}, {endIndex});");
                                }
                            }

                            Client.RunJavascript(jsCode.ToString());
                        }

                        return Task.CompletedTask;
                    })
                };
            }
        }

        Element viewProps(IReadOnlyList<string> props)
        {
            return new FlexColumn(WidthFull, Gap(4))
            {
                new FlexRow(FlexWrap, Gap(4))
                {
                    new FlexRow(WidthFull, BorderRadius(4), PaddingLeft(4), Background(WhiteSmoke))
                    {
                        inputEditor()
                    },
                    new FlexRow(WidthFull, FlexWrap, Gap(4))
                    {
                        OnClick(_ =>
                        {
                            state.Selection = state.Selection with { PropertyIndexInProps = null };

                            return Task.CompletedTask;
                        }),
                        props.Select((value, index) => attributeItem(index, value))
                    }
                }
            };

            Element inputEditor()
            {
                string value = null;

                if (state.Selection.PropertyIndexInProps >= 0)
                {
                    value = props[state.Selection.PropertyIndexInProps.Value];
                }

                return new MagicInput
                {
                    Placeholder = "Add property",

                    Suggestions = GetPropSuggestions(state),

                    Name = (state.Selection.PropertyIndexInProps ?? (props.Count + 1) * -1).ToString(),

                    Id = "PROPS-INPUT-EDITOR-" + (state.Selection.PropertyIndexInProps ?? -1),

                    OnChange = (senderName, newValue) =>
                    {
                        var index = int.Parse(senderName);

                        newValue = TryBeautifyPropertyValue(newValue);

                        if (index >= 0)
                        {
                            CurrentVisualElement.Properties[index] = newValue;
                        }
                        else
                        {
                            CurrentVisualElement.Properties.Add(newValue);
                        }

                        state.Selection.PropertyIndexInProps = null;

                        return Task.CompletedTask;
                    },
                    Value = value
                };
            }

            FlexRowCentered attributeItem(int index, string value)
            {
                var isSelected = index == state.Selection.PropertyIndexInProps;

                var closeIcon = new FlexRowCentered(Size(20), PositionAbsolute, Top(-8), Right(-8), Padding(4), Background(White),
                                                    Border(0.5, solid, Theme.BorderColor), BorderRadius(24))
                {
                    Color(Gray500), Hover(Color(Blue300), BorderColor(Blue300)),

                    new IconClose() + Size(16),

                    OnClick([StopPropagation](_) =>
                    {
                        CurrentVisualElement.Properties.RemoveAt(state.Selection.PropertyIndexInProps!.Value);

                        state.Selection.PropertyIndexInProps = null;

                        return Task.CompletedTask;
                    })
                };

                return new(CursorDefault, Padding(4, 8), BorderRadius(16))
                {
                    Background(isSelected ? Gray200 : Gray50),

                    isSelected ? PositionRelative : null,
                    isSelected ? closeIcon : null,

                    value,
                    Id("PROPS-" + index),
                    OnClick([StopPropagation](e) =>
                    {
                        var location = int.Parse(e.target.id.RemoveFromStart("PROPS-"));

                        state.Selection = new()
                        {
                            VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                            PropertyIndexInProps = location
                        };

                        var id = "PROPS-INPUT-EDITOR-" + location;

                        // calculate js code for focus to input editor
                        {
                            var jsCode = new StringBuilder();

                            jsCode.AppendLine($"document.getElementById('{id}').focus();");

                            // calculate text selection in edit input
                            {
                                var nameValue = CurrentVisualElement.Properties[location];
                                var (success, _, parsedValue) = TryParsePropertyValue(nameValue);
                                if (success)
                                {
                                    var startIndex = nameValue.LastIndexOf(parsedValue, StringComparison.OrdinalIgnoreCase);
                                    var endIndex = nameValue.Length;

                                    jsCode.AppendLine($"document.getElementById('{id}').setSelectionRange({startIndex}, {endIndex});");
                                }
                            }

                            Client.RunJavascript(jsCode.ToString());
                        }

                        return Task.CompletedTask;
                    })
                };
            }
        }
    }

    Element PartScale()
    {
        return new FlexRow(WidthFull, PaddingLeftRight(3), AlignItemsCenter, Gap(4))
        {
            new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
            {
                OnClick(_ =>
                {
                    if (state.Preview.Scale <= 20)
                    {
                        return Task.CompletedTask;
                    }

                    state.Preview.Scale -= 10;

                    return Task.CompletedTask;
                }),
                new IconMinus()
            },

            $"%{state.Preview.Scale}",
            new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
            {
                OnClick(_ =>
                {
                    if (state.Preview.Scale >= 100)
                    {
                        return Task.CompletedTask;
                    }

                    state.Preview.Scale += 10;

                    return Task.CompletedTask;
                }),
                new IconPlus()
            }
        };
    }

    void ShowErrorIfExist<T>(Result<T> result)
    {
        if (result.Success)
        {
            return;
        }

        this.FailNotification(result.Error.Message);
    }

    Task StyleGroupAddClicked(MouseEvent e)
    {
        var styleGroups = CurrentVisualElement.StyleGroups;

        PropertyGroupModel newStyleGroup;
        if (styleGroups.Count == 0)
        {
            newStyleGroup = new()
            {
                Condition = "*"
            };
        }
        else
        {
            newStyleGroup = new()
            {
                Condition = "? ? ? ?"
            };
        }

        styleGroups.Add(newStyleGroup);

        state.Selection = new()
        {
            VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

            StyleGroupIndex = styleGroups.Count - 1
        };

        return Task.CompletedTask;
    }

    Task StyleGroupRemoveClicked(MouseEvent e)
    {
        CurrentVisualElement.StyleGroups.RemoveAt(state.Selection.StyleGroupIndex!.Value);

        state.Selection = new()
        {
            VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath
        };

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task ToggleIsActionButtonsVisible(MouseEvent _)
    {
        state.IsActionButtonsVisible = !state.IsActionButtonsVisible;

        return Task.CompletedTask;
    }

    class StyleInputLocation
    {
        public int PropertyIndexInGroup { get; init; }

        public required int StyleGroupIndex { get; init; }
        string Prefix { get; init; } = "style-input-location";

        public static implicit operator StyleInputLocation(string input)
        {
            return Parse(input);
        }

        public static implicit operator string(StyleInputLocation location)
        {
            return location.ToString();
        }

        public override string ToString()
        {
            return $"{Prefix},{StyleGroupIndex},{PropertyIndexInGroup}";
        }

        static StyleInputLocation Parse(string input)
        {
            var parts = input.Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException("Invalid input format");
            }

            return new()
            {
                Prefix               = parts[0],
                StyleGroupIndex      = int.Parse(parts[1]),
                PropertyIndexInGroup = int.Parse(parts[2])
            };
        }
    }
}
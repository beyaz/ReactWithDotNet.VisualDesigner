﻿using System.Text;
using Dommel;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;
using ReactWithDotNet.VisualDesigner.Exporters;
using Page = ReactWithDotNet.VisualDesigner.Infrastructure.Page;

namespace ReactWithDotNet.VisualDesigner.Views;

sealed class ApplicationView : Component<ApplicationState>
{
    enum Icon
    {
        add,
        remove
    }

    VisualElementModel CurrentVisualElement => FindTreeNodeByTreePath(state.ComponentRootElement, state.Selection.VisualElementTreeItemPath);

    protected override async Task constructor()
    {
        var userName = Environment.UserName; // future: get userName from cookie or url

        Client.ListenEvent("Change_VisualElementTreeItemPath", treeItemPath =>
        {
            state.Selection = new()
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

                UpdateZoomInClient();

                return;
            }
        }

        // try take from db cache
        {
            var lastUsage = (await GetLastUsageInfoByUserName(userName)).FirstOrDefault();
            if (lastUsage is not null && lastUsage.LastStateAsYaml.HasValue())
            {
                state = DeserializeFromYaml<ApplicationState>(lastUsage.LastStateAsYaml);

                UpdateZoomInClient();

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
            Tag = "div"
        });

        return Task.CompletedTask;
    }

    async Task ChangeSelectedComponent(string componentName)
    {
        ComponentEntity component;
        {
            var componentResult = await GetComponenUserOrMainVersionAsync(state.ProjectId, componentName, state.UserName);
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

        var componentRootElement = component.RootElementAsYaml.AsVisualElementModel();

        state = new()
        {
            UserName = state.UserName,

            ProjectId = state.ProjectId,

            Preview = state.Preview,

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
            var lastUsage = (await GetLastUsageInfoByUserName(userName)).FirstOrDefault(p => p.ProjectId == projectId);
            if (lastUsage?.LastStateAsYaml.HasValue() is true)
            {
                state = DeserializeFromYaml<ApplicationState>(lastUsage.LastStateAsYaml);

                return;
            }
        }

        state = new()
        {
            UserName = state.UserName,

            ProjectId = projectId,

            Preview = state.Preview,

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

    Task DeleteSelectedTreeItem()
    {
        if (state.Selection.VisualElementTreeItemPath.HasNoValue())
        {
            return Task.CompletedTask;
        }

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

    Task LayersTabRemoveSelectedItemClicked(MouseEvent e)
    {
        return DeleteSelectedTreeItem();
    }

    Element MainContent()
    {
        return new SplitRow
        {
            sizes = [25, 50, 25],
            children =
            {
                left(),

                middle,

                right()
            }
        };

        async Task<Element> left()
        {
            return await PartLeftPanel() + BorderBottomLeftRadius(8) + OverflowAuto;
        }

        Element middle()
        {
            return state.MainContentTab.In(MainContentTabs.Code, MainContentTabs.ProjectConfig) ?
                new(FlexGrow(1), Padding(7), OverflowXAuto)
                {
                    YamlEditor
                }
                :
                new FlexColumn(FlexGrow(1), Padding(7), OverflowXAuto)
                {
                    Ruler.HorizontalRuler(state.Preview.Width, state.Preview.Scale) + Width(state.Preview.Width) + MarginTop(12) + PaddingLeft(30),
                    new FlexRow(SizeFull, Width(state.Preview.Width + 30))
                    {
                        Ruler.VerticleRuler(state.Preview.Scale),
                        PartPreview
                    }
                };
        }

        async Task<Element> right()
        {
            return await PartRightPanel() + BorderBottomRightRadius(8);
        }
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

                new FlexRowCentered(Gap(16), Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(8),Height(36))
                {
                    PositionRelative,
                    new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "Project" },

                    PartProject
                },

                // A C T I O N S
                SpaceX(8),
                new FlexRowCentered
                {
                    new FlexRowCentered(Gap(16), Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(8), Height(36))
                    {
                        PositionRelative,
                        new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "Component" },

                        new FlexRowCentered(Hover(Color(Blue300)))
                        {
                            "Rollback",
                            OnClick(async _ =>
                            {
                                if (state.ComponentName.HasNoValue())
                                {
                                    this.FailNotification("Select any component.");

                                    return;
                                }

                                var result = await RollbackComponent(state);
                                if (result.HasError)
                                {
                                    this.FailNotification(result.Error.Message);

                                    return;
                                }

                                this.SuccessNotification("OK");
                            })
                        },

                        new FlexRowCentered(Hover(Color(Blue300)))
                        {
                            "Commit",
                            OnClick(async _ =>
                            {
                                if (state.ComponentName.HasNoValue())
                                {
                                    this.FailNotification("Select any component.");

                                    return;
                                }

                                var result = await CommitComponent(state);
                                if (result.HasError)
                                {
                                    this.FailNotification(result.Error.Message);

                                    return;
                                }

                                this.SuccessNotification("OK");
                            })
                        },

                        new FlexRowCentered(Hover(Color(Blue300)))
                        {
                            "Export",
                            OnClick(async _ =>
                            {
                                if (state.ComponentName.HasNoValue())
                                {
                                    this.FailNotification("Select any component.");

                                    return;
                                }

                                var result = await NextJs_with_Tailwind.Export(state.AsExportInput());
                                if (result.HasError)
                                {
                                    this.FailNotification(result.Error.Message);
                                    return;
                                }

                                this.SuccessNotification("OK");
                            })
                        }
                    }
                },

                SpaceX(8),
                new FlexRowCentered(Border(1, solid, Theme.BorderColor), BorderRadius(4), Height(36))
                {
                    PositionRelative,
                    new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "View" },

                    new FlexRowCentered(Gap(8),Padding(4),LineHeight10)
                    {
                        new FlexRowCentered(Padding(4), Border(1,solid,transparent))
                        {
                            "Design",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.Design),
                            When(state.MainContentTab==MainContentTabs.Design, BorderRadius(4), Border(1,solid,Theme.BorderColor))
                        },
                        new FlexRowCentered(Padding(4), Border(1,solid,transparent))
                        {
                            "Code",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.Code),
                            When(state.MainContentTab==MainContentTabs.Code, BorderRadius(4), Border(1,solid,Theme.BorderColor))
                        },
                        new FlexRowCentered(Padding(4), Border(1,solid,transparent))
                        {
                            "Project",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.ProjectConfig),
                            When(state.MainContentTab==MainContentTabs.ProjectConfig, BorderRadius(4), Border(1,solid,Theme.BorderColor))
                        }
                    },

                    
                }
            },
            new FlexRowCentered(Gap(32))
            {
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

    [StopPropagation]
    async Task OnMainContentTabHeaderClicked(MouseEvent e)
    {
        var tab = Enum.Parse<MainContentTabs>(e.target.id);
        
        // has no change
        if (state.MainContentTab == tab)
        {
            return;
        }

        if (state.MainContentTab == MainContentTabs.Code)
        {
            // check has any edit
            if (state.YamlText != SerializeToYaml(CurrentVisualElement))
            {
                var result = UpdateElementNode(state.Selection.VisualElementTreeItemPath, state.YamlText);
                if (result.HasError)
                {
                    this.FailNotification(result.Error.Message);
                    return;
                }
            }
        }
        
        if (state.MainContentTab == MainContentTabs.ProjectConfig)
        {
            var result = await DbOperation(async db =>
            {
                var project = await db.FirstOrDefaultAsync<ProjectEntity>(x => x.Id == state.ProjectId);
                if (project is null)
                {
                    return Fail("ProjectNotFound");
                }

                await db.UpdateAsync(project with { ConfigAsYaml = state.YamlText });

                return Success;
            });
            
            if (result.HasError)
            {
                this.FailNotification(result.Error.Message);
            }
        }

        state.MainContentTab = tab;

        if (tab == MainContentTabs.Design)
        {
            state.YamlText = null;
        }

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
        if (state.Selection.VisualElementTreeItemPath.HasValue())
        {
            removeIconInLayersTab.Add(Hover(Color(Blue300), BorderColor(Blue300)), OnClick(LayersTabRemoveSelectedItemClicked));
        }
        else
        {
            removeIconInLayersTab.Add(VisibilityCollapse);
        }

        var addIconInLayersTab = CreateIcon(Icon.add, 16);
        if (state.ComponentRootElement is null || state.Selection.VisualElementTreeItemPath.HasValue())
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
                        new IconLayers() + Size(18) + Color(Gray500)
                    },

                    addIconInLayersTab
                }
            },

            new VisualElementTreeView
            {
                Model = state.ComponentRootElement,

                SelectedPath = state.Selection.VisualElementTreeItemPath,

                TreeItemHover = treeItemPath =>
                {
                    state.Selection.VisualElementTreeItemPathHover = treeItemPath;

                    return Task.CompletedTask;
                },
                OnHideInDesignerToggle = () =>
                {
                    CurrentVisualElement.HideInDesigner = !CurrentVisualElement.HideInDesigner;

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

                OnDelete = DeleteSelectedTreeItem,

                CopyPaste = (source, target) =>
                {
                    var sourceNode = FindTreeNodeByTreePath(state.ComponentRootElement, source);
                    var targetNode = FindTreeNodeByTreePath(state.ComponentRootElement, target);

                    var sourceNodeClone = SerializeToYaml(sourceNode).AsVisualElementModel();

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

                    var isTryingToMakeRoot = target == "0" && position == DragPosition.Before;

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

                    if (isTryingToMakeRoot)
                    {
                        state.ComponentRootElement = sourceNodeParent.Children[sourceNodeIndex];

                        state.Selection = new();

                        return Task.CompletedTask;
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
            }
        };
    }

    Element PartMediaSizeButtons()
    {
        return new FlexRowCentered(Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(4), Height(36))
        {
            PositionRelative,
            new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "Width" },

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

                new FlexRow(JustifyContentSpaceAround, AlignItemsCenter, Gap(16))
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
                }
            }
        };
    }

    Element PartPreview()
    {
        return new FlexRow(JustifyContentFlexStart, PositionRelative)
        {
            BackgroundImage("radial-gradient(#a5a8ed 0.5px, #f8f8f8 0.5px)"),
            BackgroundSize("10px 10px"),

            createElement(),

            Width(state.Preview.Width),
            Height(state.Preview.Height * percent),
            BoxShadow(0, 4, 12, 0, rgba(0, 0, 0, 0.1))
        };

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
            },

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
                                Name           = newValue,
                                ProjectId      = state.ProjectId,
                                UserName       = state.UserName,
                                LastAccessTime = DateTime.Now
                            };

                            await DbOperation(db => db.InsertAsync(newDbRecord));

                            await OnComponentNameChanged(newValue);

                            state.IsProjectSettingsPopupVisible = false;
                        }
                    }
                } : null
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

        var stylesHeader = new FlexRow(WidthFull, AlignItemsCenter)
        {
            new div { Height(1), FlexGrow(1), Background(Gray200) },
            new span { "S T Y L E", WhiteSpaceNoWrap, UserSelect(none), PaddingX(4) },
            new div { Height(1), FlexGrow(1), Background(Gray200) }
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

            propsHeader,
            viewProps(visualElementModel.Properties),
            
            stylesHeader,
            viewStyles(CurrentVisualElement.Styles)
        };

        Element viewStyles(IReadOnlyList<string> styles)
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
                        styles.Select((value, index) => attributeItem(index, value)),
                        OnClick([StopPropagation](_) =>
                        {
                            state.Selection.SelectedStyleIndex = null;

                            return Task.CompletedTask;
                        })
                    }
                }
            };

            Element inputEditor()
            {
                string value = null;

                if (state.Selection.SelectedStyleIndex >= 0)
                {
                    value = CurrentVisualElement.Styles[state.Selection.SelectedStyleIndex.Value];
                }

                return new MagicInput
                {
                    Placeholder = "Add style",
                    Suggestions = GetStyleAttributeNameSuggestions(state),
                    Name        = "style_editor" + styles.Count,
                    Id          = "style_editor",
                    OnChange = (_, newValue) =>
                    {
                        newValue = TryBeautifyPropertyValue(newValue);

                        if (state.Selection.SelectedStyleIndex.HasValue)
                        {
                            CurrentVisualElement.Styles[state.Selection.SelectedStyleIndex.Value] = newValue;
                        }
                        else
                        {
                            CurrentVisualElement.Styles.Add(newValue);
                        }

                        state.Selection.SelectedStyleIndex = null;

                        return Task.CompletedTask;
                    },
                    Value = value
                };
            }

            FlexRowCentered attributeItem(int index, string value)
            {
                var isSelected = index == state.Selection.SelectedStyleIndex;

                var closeIcon = new FlexRowCentered(Size(20), PositionAbsolute, Top(-8), Right(-8), Padding(4), Background(White),
                                                    Border(0.5, solid, Theme.BorderColor), BorderRadius(24))
                {
                    Color(Gray500), Hover(Color(Blue300), BorderColor(Blue300)),

                    new IconClose() + Size(16),

                    OnClick([StopPropagation](_) =>
                    {
                        CurrentVisualElement.Styles.RemoveAt(state.Selection.SelectedStyleIndex!.Value);

                        state.Selection.SelectedStyleIndex = null;

                        return Task.CompletedTask;
                    })
                };

                Element content = value;
                {
                    TryParseProperty(value).HasValue(x =>
                    {
                        content = new FlexRowCentered
                        {
                            new span(FontWeight600) { x.Name }, ": ", new span(PaddingLeft(2)) { x.Value }
                        };
                    });
                }

                return new(CursorDefault, Padding(4, 8), BorderRadius(16))
                {
                    Background(isSelected ? Gray200 : Gray50),
                    Border(1, solid, Gray100),

                    isSelected ? PositionRelative : null,
                    isSelected ? closeIcon : null,

                    content,
                    Id(index),
                    OnClick([StopPropagation](e) =>
                    {
                        var styleIndex = int.Parse(e.currentTarget.id);

                        state.Selection = new()
                        {
                            VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                            SelectedStyleIndex = styleIndex
                        };

                        var id = "style_editor";

                        // calculate js code for focus to input editor
                        {
                            var jsCode = new StringBuilder();

                            jsCode.AppendLine($"document.getElementById('{id}').focus();");

                            // calculate text selection in edit input
                            {
                                var nameValue = CurrentVisualElement.Styles[state.Selection.SelectedStyleIndex.Value];
                                TryParseProperty(nameValue).HasValue(parseResult =>
                                {
                                    var startIndex = nameValue.LastIndexOf(parseResult.Value, StringComparison.OrdinalIgnoreCase);
                                    var endIndex = nameValue.Length;

                                    jsCode.AppendLine($"document.getElementById('{id}').setSelectionRange({startIndex}, {endIndex});");
                                });
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
                            state.Selection = state.Selection with { SelectedPropertyIndex = null };

                            return Task.CompletedTask;
                        }),
                        props.Select((value, index) => attributeItem(index, value))
                    }
                }
            };

            Element inputEditor()
            {
                string value = null;

                if (state.Selection.SelectedPropertyIndex >= 0)
                {
                    value = props[state.Selection.SelectedPropertyIndex.Value];
                }

                return new MagicInput
                {
                    Placeholder = "Add property",

                    Suggestions = GetPropSuggestions(state),

                    Name = (state.Selection.SelectedPropertyIndex ?? (props.Count + 1) * -1).ToString(),

                    Id = "PROPS-INPUT-EDITOR-" + (state.Selection.SelectedPropertyIndex ?? -1),

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

                        state.Selection.SelectedPropertyIndex = null;

                        return Task.CompletedTask;
                    },
                    OnPaste = (text) =>
                    {
                        if (!HtmlImporter.CanImportAsHtml(text))
                        {
                            return Task.CompletedTask;
                        }
                        
                        var model = HtmlImporter.ConvertToVisualElementModel(state.ProjectId, text);
                        if (model is null)
                        {
                            return Task.CompletedTask;
                        }
                        
                        CurrentVisualElement.Children.Add(model);

                        return Task.CompletedTask;
                    },
                    Value = value
                };
            }

            FlexRowCentered attributeItem(int index, string value)
            {
                var isSelected = index == state.Selection.SelectedPropertyIndex;

                var closeIcon = new FlexRowCentered(Size(20), PositionAbsolute, Top(-8), Right(-8), Padding(4), Background(White),
                                                    Border(0.5, solid, Theme.BorderColor), BorderRadius(24))
                {
                    Color(Gray500), Hover(Color(Blue300), BorderColor(Blue300)),

                    new IconClose() + Size(16),

                    OnClick([StopPropagation](_) =>
                    {
                        CurrentVisualElement.Properties.RemoveAt(state.Selection.SelectedPropertyIndex!.Value);

                        state.Selection.SelectedPropertyIndex = null;

                        return Task.CompletedTask;
                    })
                };

                Element content = value;
                {
                    TryParseProperty(value).HasValue(x =>
                    {
                        content = new FlexRowCentered
                        {
                            new span(FontWeight600) { x.Name }, ": ", new span(PaddingLeft(2)) { x.Value }
                        };
                    });
                }

                return new(CursorDefault, Padding(4, 8), BorderRadius(16))
                {
                    Background(isSelected ? Gray200 : Gray50),
                    Border(1, solid, Gray100),

                    isSelected ? PositionRelative : null,
                    isSelected ? closeIcon : null,

                    content,
                    Id("PROPS-" + index),
                    OnClick([StopPropagation](e) =>
                    {
                        var location = int.Parse(e.currentTarget.id.RemoveFromStart("PROPS-"));

                        state.Selection = new()
                        {
                            VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                            SelectedPropertyIndex = location
                        };

                        var id = "PROPS-INPUT-EDITOR-" + location;

                        // calculate js code for focus to input editor
                        {
                            var jsCode = new StringBuilder();

                            jsCode.AppendLine($"document.getElementById('{id}').focus();");

                            // calculate text selection in edit input
                            {
                                var nameValue = CurrentVisualElement.Properties[location];
                                
                                TryParseProperty(nameValue).HasValue(parseResult =>
                                {
                                    var startIndex = nameValue.LastIndexOf(parseResult.Value, StringComparison.OrdinalIgnoreCase);
                                    var endIndex = nameValue.Length;

                                    jsCode.AppendLine($"document.getElementById('{id}').setSelectionRange({startIndex}, {endIndex});");
                                });
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
        return new FlexRowCentered(Border(1, solid, Theme.BorderColor), BorderRadius(4), Height(36))
        {
            PositionRelative,
            new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "Zoom" },

            new FlexRow(WidthFull, PaddingLeftRight(4), AlignItemsCenter, Gap(4))
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

                        UpdateZoomInClient();

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

                        UpdateZoomInClient();

                        return Task.CompletedTask;
                    }),
                    new IconPlus()
                }
            }
        };
    }

    Result UpdateElementNode(string path, string yaml)
    {
        VisualElementModel newModel;

        try
        {
            newModel = YamlHelper.DeserializeFromYaml<VisualElementModel>(yaml);
        }
        catch (Exception exception)
        {
            return exception;
        }

        if (path.HasNoValue())
        {
            state.ComponentRootElement = newModel;

            state.Selection = new()
            {
                VisualElementTreeItemPath = "0"
            };

            return Success;
        }
        
        if (path.HasNoValue() || path == "0")
        {
            state.ComponentRootElement = newModel;

            return Success;
        }

        var isSuccessfullyUpdated = false;

        var node = state.ComponentRootElement;

        var paths = path.Split(',').Select(int.Parse).ToList();
        for (var i = 1; i < paths.Count; i++)
        {
            var index = paths[i];
            if (node.Children.Count <= index)
            {
                return new Exception($"IndexIsNotValid: {path}");
            }

            if (i == paths.Count - 1)
            {
                node.Children[index]  = newModel;
                isSuccessfullyUpdated = true;
                break;
            }

            node = node.Children[index];
        }

        if (!isSuccessfullyUpdated)
        {
            return new Exception($"IndexIsNotValid: {path}");
        }

        return Success;
    }

    void UpdateZoomInClient()
    {
        Client.RunJavascript($"window.ComponentIndicatorZoom = {state.Preview.Scale}");
    }

    Element YamlEditor()
    {
        state.YamlText = null;
        
        if (state.MainContentTab == MainContentTabs.Code)
        {
            state.YamlText = SerializeToYaml(CurrentVisualElement);
        }
        
        if (state.MainContentTab == MainContentTabs.ProjectConfig)
        {
            state.YamlText = DbOperation(db => db.FirstOrDefault<ProjectEntity>(x => x.Id == state.ProjectId)?.ConfigAsYaml);
        }

        return new Editor
        {
            valueBind       = () => state.YamlText,
            defaultLanguage = "yaml",
            options =
            {
                renderLineHighlight = "none",
                fontFamily          = "consolas, 'IBM Plex Mono Medium', 'Courier New', monospace",
                fontSize            = 11,
                minimap             = new { enabled = false },
                lineNumbers         = "off",
                unicodeHighlight    = new { showExcludeOptions = false }
            }
        };
    }

    static class Ruler
    {
        public static Element HorizontalRuler(int screenWidth, double scale = 100)
        {
            const int step = 50;
            var max = screenWidth / step + 1;

            return new FlexRow(WidthFull)
            {
                new FlexRow(PositionRelative, WidthFull, Height(20))
                {
                    Enumerable.Range(0, max).Select(number => new div(PositionAbsolute)
                    {
                        Bottom(3), Left(number * step),
                        new FlexColumn(FontSize8, LineHeight6, FontWeight500, Gap(4))
                        {
                            new div(MarginLeft(calculateMarginForCenterizeLabel(number)))
                            {
                                Convert.ToInt32(number * step * (100 / scale))
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
                }
            };

            IReadOnlyList<Element> createTenPoints()
            {
                var returnList = new List<Element>();

                var miniStep = 10;

                var cursor = 0;
                var distance = miniStep;
                while (distance <= screenWidth)
                {
                    cursor++;

                    distance = cursor * miniStep;

                    if (distance % step == 0 || distance > screenWidth)
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

        public static Element VerticleRuler(double scale = 100)
        {
            const int maxHeight = 5000;

            const int step = 50;
            const int max = maxHeight / step + 1;

            return new div(HeightFull)
            {
                new div(HeightFull, Width(30), OverflowHidden, PositionRelative)
                {
                    Enumerable.Range(0, max).Select(number => new div(PositionAbsolute)
                    {
                        Right(3), Top(number * step),
                        new FlexRow(FontSize8, LineHeight6, FontWeight500, Gap(4))
                        {
                            new div(MarginTop(number == 0 ? 0 : -3))
                            {
                                Convert.ToInt32(number * step * (100 / scale))
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
                }
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
    }
}
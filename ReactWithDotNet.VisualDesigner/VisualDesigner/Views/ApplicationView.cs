using System.Text;
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

            state.LeftTab = LeftTabs.ElementTree;

            return Task.CompletedTask;
        });

        // try take from memory cache
        {
            var userLastState = GetUserLastState(userName);
            if (userLastState is not null)
            {
                state = userLastState with
                {
                    StyleItemDragDrop = new(),
                    PropertyItemDragDrop = new(),
                    MainContentTab = MainContentTabs.Design
                };

                UpdateZoomInClient();

                return;
            }
        }

        // try take from db cache
        {
            var lastUsage = (await GetLastUsageInfoByUserName(userName)).FirstOrDefault();
            if (lastUsage is not null && lastUsage.LastStateAsYaml.HasValue())
            {
                state = DeserializeFromYaml<ApplicationState>(lastUsage.LastStateAsYaml) with
                {
                    StyleItemDragDrop = new(),
                    PropertyItemDragDrop = new(),
                    MainContentTab = MainContentTabs.Design
                };

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

            Selection = new(),

            StyleItemDragDrop    = new(),
            PropertyItemDragDrop = new(),
            MainContentTab       = MainContentTabs.Design
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

    static Element CreateDropLocationElement(bool onMouseOver, Modifier[] modifiers)
    {
        return new FlexRowCentered(Size(24))
        {
            new div(Size(16), BorderRadius(32), Background(Blue200))
            {
                When(onMouseOver, Size(24) + Border(1, dotted, Blue600)),

                modifiers
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

    static async Task<Result<string>> UpdateComponentConfig(int componentId, string componentConfigAsYamlNewValue, string userName)
    {
        var component = await DbOperation(db => db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == componentId));
        if (component is null)
        {
            return new Exception("ComponentNotFound");
        }

        if (component.ConfigAsYaml == componentConfigAsYamlNewValue)
        {
            return string.Empty;
        }

        var name = string.Empty;
        var exportFilePath = string.Empty;
        {
            var config = DeserializeFromYaml<Dictionary<string, string>>(componentConfigAsYamlNewValue);
            {
                foreach (var item in config.TryGetComponentName())
                {
                    name = item;
                }

                if (name.HasNoValue())
                {
                    return new Exception("NameMustBeEntered");
                }
            }
            {
                foreach (var item in config.TryGetComponentExportFilePath())
                {
                    exportFilePath = item;
                }

                if (exportFilePath.HasNoValue())
                {
                    return new Exception("ExportFilePathMustBeEnteredCorrectly");
                }
            }
        }

        // check name & export file path
        {
            foreach (var item in (await DbOperation(db => db.GetAllAsync<ComponentEntity>())).Where(x => x.Id != component.Id))
            {
                if (item.GetName() == name && item.GetExportFilePath() == exportFilePath)
                {
                    return new Exception("Has already same named component.");
                }
            }

            await DbOperation(db => db.InsertAsync(new ComponentHistoryEntity
            {
                ComponentId                = component.Id,
                ComponentName              = component.Name,
                ConfigAsYaml               = component.ConfigAsYaml,
                ComponentRootElementAsYaml = component.RootElementAsYaml,
                InsertTime                 = DateTime.Now,
                UserName                   = userName
            }));

            await DbOperation(db => db.UpdateAsync(component with
            {
                ConfigAsYaml = componentConfigAsYamlNewValue
            }));

            Cache.Clear();
        }

        return "Component name updated.";
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
        var componentResult = await DbOperation(db => db.GetComponentByComponentName_NotNull(componentName));
        if (componentResult.HasError)
        {
            this.FailNotification(componentResult.Error.Message);
            return;
        }

        var component = componentResult.Value;

        await ChangeSelectedComponent(component.Id, component);
    }

    async Task ChangeSelectedComponent(int componentId, ComponentEntity component)
    {
        component ??= await DbOperation(db => db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == componentId));

        VisualElementModel componentRootElement;
        {
            var userVersion = await TryGetUserVersion(componentId, state.UserName);
            if (userVersion is not null)
            {
                componentRootElement = userVersion.RootElementAsYaml.AsVisualElementModel();
            }
            else
            {
                componentRootElement = component.RootElementAsYaml.AsVisualElementModel();
            }
        }

        state = new()
        {
            UserName = state.UserName,

            ProjectId = state.ProjectId,

            Preview = state.Preview,

            ComponentName = component.Name,

            ComponentId = componentId,

            ComponentRootElement = componentRootElement,

            Selection = new(),

            StyleItemDragDrop    = new(),
            PropertyItemDragDrop = new()
        };

        if (state.ComponentRootElement is not null)
        {
            state.Selection = state.Selection with { VisualElementTreeItemPath = "0" };
        }
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

        await InitializeStateWithFirstComponentInProject(projectId);
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

    async Task FocusToCurrentComponentInIde(MouseEvent e)
    {
        if (state.UserName.HasNoValue())
        {
            this.FailNotification("UserName has no value");
            return;
        }

        var user = await DbOperation(db => db.FirstOrDefaultAsync<UserEntity>(x => x.UserName == state.UserName));
        if (user is null)
        {
            this.FailNotification($"UserNotFound {state.UserName}");
            return;
        }

        if (user.LocalWorkspacePath.HasNoValue())
        {
            this.FailNotification("UserLocalWorkspacePathShouldBeSet");
            return;
        }

        var location = GetComponentFileLocation(state.ComponentName, user.LocalWorkspacePath);
        if (location.HasError)
        {
            this.FailNotification(location.Error.Message);
            return;
        }

        var fileContent = await IO.TryReadFileAllLines(location.Value.filePath);
        if (fileContent.HasError)
        {
            this.FailNotification(fileContent.Error.Message);
            return;
        }

        var lineIndex = GetComponentDeclerationLineIndex(fileContent.Value, location.Value.targetComponentName);
        if (lineIndex.HasError)
        {
            this.FailNotification(lineIndex.Error.Message);
            return;
        }

        var exception = IdeBridge.OpenEditor(location.Value.filePath, lineIndex.Value + 1);
        if (exception is not null)
        {
            this.FailNotification(exception.Message);
        }
    }

    async Task InitializeStateWithFirstComponentInProject(int projectId)
    {
        state = new()
        {
            UserName = state.UserName,

            ProjectId = projectId,

            Preview = state.Preview,

            Selection = new(),

            StyleItemDragDrop    = new(),
            PropertyItemDragDrop = new()
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

        Element left()
        {
            return PartLeftPanel() + BorderBottomLeftRadius(8);
        }

        Element middle()
        {
            return state.MainContentTab.In(MainContentTabs.Code, MainContentTabs.ProjectConfig, MainContentTabs.ImportHtml, MainContentTabs.ComponentConfig) ?
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

    

    async Task OnDeleteSelectedComponentClicked(MouseEvent e)
    {
        await DbOperation(async db =>
        {
            var component = await db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == state.ComponentId);
            if (component is not null)
            {
                await db.InsertAsync(new ComponentHistoryEntity
                {
                    ComponentId                = component.Id,
                    ComponentName              = component.Name,
                    ComponentRootElementAsYaml = component.RootElementAsYaml,
                    InsertTime                 = DateTime.Now,
                    UserName                   = state.UserName
                });

                await db.DeleteAsync(component);
            }
        });

        Cache.Clear();

        await InitializeStateWithFirstComponentInProject(state.ProjectId);

        this.SuccessNotification("Component deleted.");
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
            if (state.MainContentText != SerializeToYaml(CurrentVisualElement))
            {
                var result = UpdateElementNode(state.Selection.VisualElementTreeItemPath, state.MainContentText);
                if (result.HasError)
                {
                    this.FailNotification(result.Error.Message);
                    return;
                }
            }
        }

        if (state.MainContentTab == MainContentTabs.ImportHtml)
        {
            await TryImportHtml(state.MainContentText);
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

                await db.UpdateAsync(project with { ConfigAsYaml = state.MainContentText });

                Cache.Clear();

                return Success;
            });

            if (result.HasError)
            {
                this.FailNotification(result.Error.Message);
            }
        }

        if (state.MainContentTab == MainContentTabs.ComponentConfig)
        {
            var result = await UpdateComponentConfig(state.ComponentId, state.MainContentText, state.UserName);
            if (result.HasError)
            {
                this.FailNotification(result.Error.Message);
                return;
            }

            if (result.Value.HasValue())
            {
                this.SuccessNotification(result.Value);
            }
        }

        state.MainContentTab = tab;

        if (tab == MainContentTabs.Design)
        {
            state = state with { MainContentText = null };
        }
    }

    Task OnPropertyItemDropLocationDroped(DragEvent _)
    {
        var dd = state.PropertyItemDragDrop;

        if (dd.StartItemIndex.HasValue)
        {
            if (dd.EndItemIndex.HasValue)
            {
                CurrentVisualElement.Properties.MoveItemRelativeTo(dd.StartItemIndex.Value, dd.EndItemIndex.Value, dd.Position == AttibuteDragPosition.Before);
            }
        }

        state = state with { PropertyItemDragDrop = new() };

        return Task.CompletedTask;
    }

    Task OnPropertyItemDropLocationLeaved(DragEvent _)
    {
        state = state with
        {
            PropertyItemDragDrop = state.PropertyItemDragDrop with
            {
                Position = null
            }
        };
        return Task.CompletedTask;
    }

    Task OnStyleItemDropLocationDroped(DragEvent _)
    {
        var dd = state.StyleItemDragDrop;

        if (dd.StartItemIndex.HasValue)
        {
            if (dd.EndItemIndex.HasValue)
            {
                CurrentVisualElement.Styles.MoveItemRelativeTo(dd.StartItemIndex.Value, dd.EndItemIndex.Value, dd.Position == AttibuteDragPosition.Before);
            }
        }

        state = state with { StyleItemDragDrop = new() };

        return Task.CompletedTask;
    }

    Task OnStyleItemDropLocationLeaved(DragEvent _)
    {
        state = state with
        {
            StyleItemDragDrop = state.StyleItemDragDrop with
            {
                Position = null
            }
        };
        return Task.CompletedTask;
    }

    Element PartApplicationTopPanel()
    {
        return new FlexRow(UserSelect(none))
        {
            new FlexRowCentered(Gap(16))
            {
                new h3 { "React Visual Designer" },

                new FlexRowCentered(Gap(16), Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(8), Height(36))
                {
                    PositionRelative,
                    new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "Project" },

                    PartProject
                },

                // A C T I O N S

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
                                if (state.ComponentId <= 0)
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
                                if (state.ComponentId <= 0)
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
                                if (state.ComponentId <= 0)
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

                                if (result.Value.HasChange is false)
                                {
                                    this.SuccessNotification("File already same.");
                                }
                                else
                                {
                                    this.SuccessNotification("File updated.");
                                }
                            })
                        }
                    }
                },

                new IconFocus() + Color(Gray500) + Hover(Color(Blue300)) + OnClick(FocusToCurrentComponentInIde),

                new FlexRowCentered(Border(1, solid, Theme.BorderColor), BorderRadius(4), Height(36))
                {
                    PositionRelative,
                    new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background(Theme.BackgroundColor), PaddingX(4)) { "View" },

                    new FlexRowCentered(Gap(8), Padding(4), LineHeight10, WhiteSpaceNoWrap)
                    {
                        new FlexRowCentered(Padding(4), Border(1, solid, transparent))
                        {
                            "Design",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.Design),
                            When(state.MainContentTab == MainContentTabs.Design, BorderRadius(4), Border(1, solid, Theme.BorderColor))
                        },
                        new FlexRowCentered(Padding(4), Border(1, solid, transparent))
                        {
                            "Code",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.Code),
                            When(state.MainContentTab == MainContentTabs.Code, BorderRadius(4), Border(1, solid, Theme.BorderColor))
                        },
                        new FlexRowCentered(Padding(4), Border(1, solid, transparent))
                        {
                            "Project",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.ProjectConfig),
                            When(state.MainContentTab == MainContentTabs.ProjectConfig, BorderRadius(4), Border(1, solid, Theme.BorderColor))
                        },
                        new FlexRowCentered(Padding(4), Border(1, solid, transparent))
                        {
                            "Import Html",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.ImportHtml),
                            When(state.MainContentTab == MainContentTabs.ImportHtml, BorderRadius(4), Border(1, solid, Theme.BorderColor))
                        },
                        new FlexRowCentered(Padding(4), Border(1, solid, transparent))
                        {
                            "Config",
                            OnClick(OnMainContentTabHeaderClicked),
                            Id((int)MainContentTabs.ComponentConfig),
                            When(state.MainContentTab == MainContentTabs.ComponentConfig, BorderRadius(4), Border(1, solid, Theme.BorderColor))
                        }
                    }
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

    Element PartLeftPanel()
    {
        var componentNameEditor = new input
        {
            type                     = "text",
            value                = state.ComponentName,
            disabled = true,
            style =
            {
                FlexGrow(1),
                Focus(OutlineNone)
            }
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

        var tabButtons = new FlexRow(WidthFull, FontWeightBold, AlignItemsCenter, Padding(8, 4), JustifyContentSpaceAround, BorderBottom(1, dotted, "#d9d9d9"), BorderTop(1, dotted, "#d9d9d9"))
        {
            Color(Gray300), CursorDefault, UserSelect(none),

            new FlexRowCentered(WidthFull)
            {
                removeIconInLayersTab + When(state.LeftTab != LeftTabs.ElementTree, VisibilityCollapse),

                new FlexRow(JustifyContentSpaceEvenly, WidthFull, PaddingX(4))
                {
                    new FlexRowCentered(WidthFull, Hover(Background(Gray50), BorderRadius(36)))
                    {
                        new IconReact() + Size(24) + Color(state.LeftTab == LeftTabs.Components ? Gray500 : Gray200),
                        OnClick([StopPropagation](_) =>
                        {
                            state.LeftTab = LeftTabs.Components;
                            return Task.CompletedTask;
                        })
                    },
                    new FlexRowCentered(WidthFull, Hover(Background(Gray50), BorderRadius(36)))
                    {
                        new IconLayers() + Size(18) + Color(state.LeftTab == LeftTabs.ElementTree ? Gray500 : Gray200),
                        OnClick([StopPropagation](_) =>
                        {
                            state.LeftTab = LeftTabs.ElementTree;
                            return Task.CompletedTask;
                        })
                    }
                },

                addIconInLayersTab + When(state.LeftTab != LeftTabs.ElementTree, VisibilityCollapse)
            }
        };

        var elementTree = new VisualElementTreeView
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
            },

            NavigateToComponent = path =>
            {
                var node = FindTreeNodeByTreePath(state.ComponentRootElement, path);
                foreach (var componentId in TryReadTagAsDesignerComponentId(node))
                {
                    return ChangeSelectedComponent(componentId, null);
                }

                return Task.CompletedTask;
            }
        } + When(state.LeftTab != LeftTabs.ElementTree, DisplayNone);

        var componentTree = new ComponentTreeView
        {
            ProjectId        = state.ProjectId,
            ComponentName    = state.ComponentName,
            SelectionChanged = ChangeSelectedComponent
        } + When(state.LeftTab != LeftTabs.Components, DisplayNone);

        return new FlexColumn(SizeFull, AlignItemsCenter, BorderRight(1, dotted, "#d9d9d9"), Background(White))
        {
            new FlexRow(WidthFull, AlignItemsCenter, Gap(4), PaddingLeft(8))
            {
                new IconDelete() + Size(16) + Color(Theme.text_primary) + Hover(Color(Blue300)) + OnClick(OnDeleteSelectedComponentClicked),

                componentNameEditor
            },

            tabButtons,

            new FlexColumn(WidthFull, Flex(1), OverflowAuto)
            {
                elementTree,

                componentTree
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
                                Name      = newValue,
                                ProjectId = state.ProjectId
                            };

                            await DbOperation(db => db.InsertAsync(newDbRecord));

                            Cache.Clear();

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

        Element inputTag;
        {
            var inputValue = visualElementModel.Tag;

            foreach (var componentId in TryReadTagAsDesignerComponentId(visualElementModel))
            {
                var component = await DbOperation(db => db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == componentId));
                if (component is null)
                {
                    return new div { $"ComponentNotFound.{componentId}" };
                }

                inputValue = component.Name;
            }

            inputTag = new FlexRow(WidthFull)
            {
                new MagicInput
                {
                    Name        = string.Empty,
                    Value       = inputValue,
                    Suggestions = await GetTagSuggestions(state),
                    OnChange = async (_, newValue) =>
                    {
                        foreach (var dbRecord in await TryFindComponentByComponentName(state.ProjectId, newValue))
                        {
                            CurrentVisualElement.Tag = dbRecord.Id.ToString();
                            return;
                        }

                        CurrentVisualElement.Tag = newValue;
                    },
                    IsTextAlignCenter = true
                }
            };
        }

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

            SpaceY(16),

            stylesHeader,
            viewStyles(CurrentVisualElement.Styles)
        };

        static Element CreateAttributeItemCloseIcon(params Modifier[] modifiers)
        {
            return new FlexRowCentered
            {
                Size(20),
                Padding(4),
                PositionAbsolute, Top(-8), Right(-8),

                Background(White),
                Border(0.5, solid, Theme.BorderColor),
                BorderRadius(24),

                Color(Gray500),
                Hover(Color(Blue300), BorderColor(Blue300)),

                new IconClose() + Size(16),

                modifiers
            };
        }

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

            Element attributeItem(int index, string value)
            {
                var closeIcon = CreateAttributeItemCloseIcon(OnClick([StopPropagation](_) =>
                {
                    CurrentVisualElement.Styles.RemoveAt(state.Selection.SelectedStyleIndex!.Value);

                    state.Selection.SelectedStyleIndex = null;

                    return Task.CompletedTask;
                }));

                Element content = value;
                {
                    TryParseProperty(value).HasValue(x =>
                    {
                        content = new FlexRow(AlignItemsCenter, FlexWrap)
                        {
                            new span(FontWeight600) { x.Name }, ": ", new span(PaddingLeft(2)) { x.Value }
                        };
                    });
                }

                var isSelected = index == state.Selection.SelectedStyleIndex;

                if (state.StyleItemDragDrop.StartItemIndex == index)
                {
                    isSelected = false;
                }

                var styleItem = new FlexRowCentered(CursorDefault, Padding(4, 8), BorderRadius(16), UserSelect(none))
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
                        if (styleIndex == state.Selection?.SelectedStyleIndex)
                        {
                            state.Selection = state.Selection with { SelectedStyleIndex = null };
                            return Task.CompletedTask;
                        }

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
                    }),

                    // Drag Drop Operation
                    {
                        DraggableTrue,
                        OnDragStart(e =>
                        {
                            state = state with
                            {
                                StyleItemDragDrop = state.StyleItemDragDrop with
                                {
                                    StartItemIndex = int.Parse(e.currentTarget.id)
                                }
                            };
                            return Task.CompletedTask;
                        }),
                        OnDragEnter(e =>
                        {
                            state = state with
                            {
                                StyleItemDragDrop = state.StyleItemDragDrop with
                                {
                                    EndItemIndex = int.Parse(e.currentTarget.id)
                                }
                            };
                            return Task.CompletedTask;
                        })
                    }
                };

                if (state.StyleItemDragDrop.EndItemIndex == index)
                {
                    var dropLocationBefore = CreateDropLocationElement(state.StyleItemDragDrop.Position == AttibuteDragPosition.Before,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { StyleItemDragDrop = state.StyleItemDragDrop with { Position = AttibuteDragPosition.Before } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnStyleItemDropLocationLeaved),
                        OnDrop(OnStyleItemDropLocationDroped)
                    ]);

                    var dropLocationAfter = CreateDropLocationElement(state.StyleItemDragDrop.Position == AttibuteDragPosition.After,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { StyleItemDragDrop = state.StyleItemDragDrop with { Position = AttibuteDragPosition.After } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnStyleItemDropLocationLeaved),
                        OnDrop(OnStyleItemDropLocationDroped)
                    ]);

                    return new FlexRowCentered(Gap(8))
                    {
                        dropLocationBefore,

                        styleItem,

                        dropLocationAfter
                    };
                }

                return styleItem;
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
                    Value = value
                };
            }

            Element attributeItem(int index, string value)
            {
                var closeIcon = CreateAttributeItemCloseIcon(OnClick([StopPropagation](_) =>
                {
                    CurrentVisualElement.Properties.RemoveAt(state.Selection.SelectedPropertyIndex!.Value);

                    state.Selection.SelectedPropertyIndex = null;

                    return Task.CompletedTask;
                }));

                Element content = value;
                {
                    TryParseProperty(value).HasValue(x =>
                    {
                        content = new FlexRow(AlignItemsCenter, FlexWrap)
                        {
                            new span(FontWeight600) { x.Name }, ": ", new span(PaddingLeft(2)) { x.Value }
                        };
                    });
                }

                var isSelected = index == state.Selection.SelectedPropertyIndex;

                if (state.PropertyItemDragDrop.StartItemIndex == index)
                {
                    isSelected = false;
                }

                var propertyItem = new FlexRowCentered(CursorDefault, Padding(4, 8), BorderRadius(16))
                {
                    Background(isSelected ? Gray200 : Gray50),
                    Border(1, solid, Gray100),

                    isSelected ? PositionRelative : null,
                    isSelected ? closeIcon : null,

                    content,
                    Id("PROPS-" + index),
                    OnClick([StopPropagation](e) =>
                    {
                        var propertyIndex = getPropertyIndex(e.currentTarget);
                        if (propertyIndex == state.Selection?.SelectedPropertyIndex)
                        {
                            state.Selection = state.Selection with { SelectedPropertyIndex = null };
                            return Task.CompletedTask;
                        }

                        state.Selection = new()
                        {
                            VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                            SelectedPropertyIndex = propertyIndex
                        };

                        var id = "PROPS-INPUT-EDITOR-" + propertyIndex;

                        // calculate js code for focus to input editor
                        {
                            var jsCode = new StringBuilder();

                            jsCode.AppendLine($"document.getElementById('{id}').focus();");

                            // calculate text selection in edit input
                            {
                                var nameValue = CurrentVisualElement.Properties[propertyIndex];

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
                    }),

                    // Drag Drop Operation
                    {
                        DraggableTrue,
                        OnDragStart(e =>
                        {
                            state = state with
                            {
                                PropertyItemDragDrop = state.PropertyItemDragDrop with
                                {
                                    StartItemIndex = getPropertyIndex(e.currentTarget)
                                }
                            };
                            return Task.CompletedTask;
                        }),
                        OnDragEnter(e =>
                        {
                            state = state with
                            {
                                PropertyItemDragDrop = state.PropertyItemDragDrop with
                                {
                                    EndItemIndex = getPropertyIndex(e.currentTarget)
                                }
                            };
                            return Task.CompletedTask;
                        })
                    }
                };

                if (state.PropertyItemDragDrop.EndItemIndex == index)
                {
                    var dropLocationBefore = CreateDropLocationElement(state.PropertyItemDragDrop.Position == AttibuteDragPosition.Before,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { PropertyItemDragDrop = state.PropertyItemDragDrop with { Position = AttibuteDragPosition.Before } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnPropertyItemDropLocationLeaved),
                        OnDrop(OnPropertyItemDropLocationDroped)
                    ]);

                    var dropLocationAfter = CreateDropLocationElement(state.PropertyItemDragDrop.Position == AttibuteDragPosition.After,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { PropertyItemDragDrop = state.PropertyItemDragDrop with { Position = AttibuteDragPosition.After } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnPropertyItemDropLocationLeaved),
                        OnDrop(OnPropertyItemDropLocationDroped)
                    ]);

                    return new FlexRowCentered(Gap(8))
                    {
                        dropLocationBefore,

                        propertyItem,

                        dropLocationAfter
                    };
                }

                return propertyItem;

                static int getPropertyIndex(ShadowHtmlElement htmlElement)
                {
                    return int.Parse(htmlElement.id.RemoveFromStart("PROPS-"));
                }
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
                        if (state.Preview.Scale >= 200)
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

    Task TryImportHtml(string htmlText)
    {
        if (htmlText.HasNoValue())
        {
            return Task.CompletedTask;
        }

        htmlText = htmlText.Trim();

        if (!HtmlImporter.CanImportAsHtml(htmlText))
        {
            return Task.CompletedTask;
        }

        var model = HtmlImporter.ConvertToVisualElementModel(GetProjectConfig(state.ProjectId), htmlText);
        if (model is null)
        {
            return Task.CompletedTask;
        }

        if (CurrentVisualElement is null)
        {
            state.ComponentRootElement = model;
        }
        else
        {
            CurrentVisualElement.Children.Add(model);
        }

        return Task.CompletedTask;
    }

    Result UpdateElementNode(string path, string yaml)
    {
        VisualElementModel newModel;

        try
        {
            newModel = DeserializeFromYaml<VisualElementModel>(yaml);
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
        state = state with
        {
            MainContentText = state.MainContentTab switch
            {
                MainContentTabs.Code            => SerializeToYaml(CurrentVisualElement),
                MainContentTabs.ProjectConfig   => DbOperation(db => db.FirstOrDefault<ProjectEntity>(x => x.Id == state.ProjectId)?.ConfigAsYaml),
                MainContentTabs.ComponentConfig => DbOperation(db => db.FirstOrDefault<ComponentEntity>(x => x.Id == state.ComponentId)?.ConfigAsYaml),
                _                               => null
            }
        };

        return new Editor
        {
            valueBind       = () => state.MainContentText,
            defaultLanguage = state.MainContentTab == MainContentTabs.ImportHtml ? "html" : "yaml",
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
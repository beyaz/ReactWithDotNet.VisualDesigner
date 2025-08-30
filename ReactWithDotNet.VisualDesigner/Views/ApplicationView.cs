using System.Reflection;
using System.Text;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;
using ReactWithDotNet.VisualDesigner.Exporters;
using static ReactWithDotNet.VisualDesigner.Views.ComponentEntityExtensions;
using Page = ReactWithDotNet.VisualDesigner.Infrastructure.Page;

namespace ReactWithDotNet.VisualDesigner.Views;

sealed class ApplicationView : Component<ApplicationState>
{
    static readonly IReadOnlyList<string> MediaSizes = ["M", "SM", "MD", "LG", "XL", "XXL"];

    enum Icon
    {
        add,
        remove
    }

    VisualElementModel CurrentVisualElement => FindTreeNodeByTreePath(state.ComponentRootElement, state.Selection.VisualElementTreeItemPath);

    protected override Element componentDidCatch(Exception exceptionOccurredInRender)
    {
        return new div(Background(Gray100))
        {
            exceptionOccurredInRender.ToString()
        };
    }

    protected override async Task constructor()
    {
        var userName = EnvironmentUserName; // future: get userName from cookie or url

        Client.ListenEvent("Change_VisualElementTreeItemPath", treeItemPath =>
        {
            state = state with
            {
                Selection = new()
                {
                    VisualElementTreeItemPath = treeItemPath
                }
            };

            state = state with { LeftTab = LeftTabs.ElementTree };

            // try focus in element tree
            {
                var js = new StringBuilder();

                js.AppendLine($"const element = document.getElementById('{treeItemPath}');");
                js.AppendLine("if(element)");
                js.AppendLine("{");
                js.AppendLine("    element.scrollIntoView({ behavior: 'smooth', block: 'center' });");
                js.AppendLine("}");

                Client.RunJavascript(js.ToString());
            }

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

                if (state.ProjectId <= 0)
                {
                    var projectId = await Store.GetFirstProjectId();
                    if (projectId.HasValue)
                    {
                        await ChangeSelectedProject(projectId.Value);
                    }
                }

                return;
            }
        }

        // try take from db cache
        {
            var lastUsage = (await Store.GetUserByUserName(userName)).OrderByDescending(x => x.LastAccessTime).FirstOrDefault();
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
        {
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

            var projectId = await Store.GetFirstProjectId();
            if (projectId.HasValue)
            {
                await ChangeSelectedProject(projectId.Value);
            }
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
        return new FlexRow(Padding(10), SizeFull, BackgroundTheme)
        {
            EditorFontLinks(Context),
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

    static async Task<Result<string>> UpdateComponentConfig(int projectId, int componentId, string componentConfigAsYamlNewValue, string userName)
    {
        var component = await Store.TryGetComponent(componentId);
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
            foreach (var item in (await Store.GetAllComponentsInProject(projectId)).Where(x => x.Id != component.Id))
            {
                if (item.GetName() == name && item.GetExportFilePath() == exportFilePath)
                {
                    return new Exception("Has already same named component.");
                }
            }

            await Store.Insert(new ComponentHistoryEntity
            {
                ComponentId                = component.Id,
                ConfigAsYaml               = component.ConfigAsYaml,
                ComponentRootElementAsYaml = component.RootElementAsYaml,
                InsertTime                 = DateTime.Now,
                UserName                   = userName
            });

            await Store.Update(component with
            {
                ConfigAsYaml = componentConfigAsYamlNewValue
            });

            Cache.Clear();
        }

        return "Component config updated.";
    }

    Task AddNewLayerClicked(MouseEvent e)
    {
        // add as root
        if (state.ComponentRootElement is null)
        {
            state = state with
            {
                ComponentRootElement = new()
                {
                    Tag = "div"
                },
                Selection = new()
                {
                    VisualElementTreeItemPath = "0"
                }
            };

            return Task.CompletedTask;
        }

        UpdateCurrentVisualElement(x => x with
        {
            Children = x.Children.Add(new()
            {
                Tag = "div"
            })
        });

        return Task.CompletedTask;
    }

    void ArrangePropEditMode(int propertyIndex)
    {
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
                    var startIndex = nameValue.LastIndexOf(parseResult.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                    var endIndex = nameValue.Length;

                    jsCode.AppendLine($"document.getElementById('{id}').setSelectionRange({startIndex}, {endIndex});");
                });
            }

            Client.RunJavascript(jsCode.ToString());
        }
    }

    Task ChangeSelectedComponent(int componentId)
    {
        return ChangeSelectedComponent(componentId, null);
    }

    async Task ChangeSelectedComponent(int componentId, ComponentEntity component)
    {
        component ??= await Store.TryGetComponent(componentId);

        VisualElementModel componentRootElement;
        {
            var userVersion = await Store.TryGetComponentWorkspace(componentId, state.UserName);
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

            ComponentId = componentId,

            ComponentRootElement = componentRootElement,

            LeftTab = state.LeftTab,

            Selection = new(),

            StyleItemDragDrop    = new(),
            PropertyItemDragDrop = new()
        };

        if (state.ComponentRootElement is not null)
        {
            state = state with
            {
                Selection = state.Selection with { VisualElementTreeItemPath = "0" }
            };
        }
    }

    async Task ChangeSelectedProject(int projectId)
    {
        var userName = EnvironmentUserName; // future: get userName from cookie or url

        // try take from db cache
        {
            var lastUsage = await Store.TryGetUser(projectId, userName);
            if (lastUsage?.LastStateAsYaml.HasValue() is true)
            {
                state = DeserializeFromYaml<ApplicationState>(lastUsage.LastStateAsYaml);

                return;
            }
        }

        await InitializeStateWithFirstComponentInProject(projectId);
    }

    async Task<Result<string>> CreateNewComponent(string componentConfigAsYamlNewValue)
    {
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
                    return new Exception($"{ComponentConfigReservedName.Name} should be entered.");
                }
            }
            {
                foreach (var item in config.TryGetComponentExportFilePath())
                {
                    exportFilePath = item;
                }

                if (exportFilePath.HasNoValue())
                {
                    return new Exception($"{ComponentConfigReservedName.ExportFilePath} should be entered.");
                }

                if (!exportFilePath.Contains('/'))
                {
                    return new Exception($"{ComponentConfigReservedName.ExportFilePath} should be entered correctly. Expected directory seperator: '/' ");
                }
            }
        }

        // check name & export file path
        {
            foreach (var item in await Store.GetAllComponentsInProject(state.ProjectId))
            {
                if (item.GetName() == name && item.GetExportFilePath() == exportFilePath)
                {
                    return new Exception("Has already same named component.");
                }
            }

            var newDbRecord = new ComponentEntity
            {
                ProjectId    = state.ProjectId,
                ConfigAsYaml = componentConfigAsYamlNewValue
            };

            newDbRecord = newDbRecord with
            {
                Id = await Store.Insert(newDbRecord)
            };

            Cache.Clear();

            await ChangeSelectedComponent(newDbRecord.Id);
        }

        return "Component created.";
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
            state = state with { ComponentRootElement = null };
        }
        else
        {
            var node = state.ComponentRootElement;

            for (var i = 1; i < intArray.Length - 1; i++)
            {
                node = node.Children[int.Parse(intArray[i])];
            }

            state = state with
            {
                ComponentRootElement = Modify(state.ComponentRootElement, node, x => x with
                {
                    Children = x.Children.RemoveAt(int.Parse(intArray[^1]))
                })
            };
        }

        state = state with { Selection = new() };

        return Task.CompletedTask;
    }

    async Task FocusToCurrentComponentInIde(MouseEvent e)
    {
        if (state.UserName.HasNoValue())
        {
            this.FailNotification("UserName has no value");
            return;
        }

        var user = await Store.TryGetUser(state.ProjectId, state.UserName);
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

        var location = await GetComponentFileLocation(state.ComponentId, user.LocalWorkspacePath);
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

        var lineIndex = GetComponentDeclarationLineIndex(fileContent.Value, location.Value.targetComponentName);
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
            var component = await Store.GetFirstComponentInProject(projectId);
            if (component is null)
            {
                return;
            }

            await ChangeSelectedComponent(component.Id, component);
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
            return state.MainContentTab.In(MainContentTabs.Structure,
                                           MainContentTabs.Output,
                                           MainContentTabs.ProjectConfig,
                                           MainContentTabs.ImportHtml,
                                           MainContentTabs.ComponentConfig,
                                           MainContentTabs.NewComponentConfig) ?
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
                        Ruler.VerticalRuler(state.Preview.Scale),
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
        state = state with
        {
            Preview = state.Preview with
            {
                Width = e.currentTarget.data["value"] switch
                {
                    "M"   => 320,
                    "SM"  => 640,
                    "MD"  => 768,
                    "LG"  => 1024,
                    "XL"  => 1280,
                    "XXL" => 1536,
                    _     => throw new ArgumentOutOfRangeException(e.currentTarget.data["value"])
                }
            }
        };

        return Task.CompletedTask;
    }

    async Task OnDeleteSelectedComponentClicked(MouseEvent e)
    {
        var component = await Store.TryGetComponent(state.ComponentId);
        if (component is null)
        {
            this.FailNotification("ComponentNotFound");
            return;
        }

        // validate other users working on component
        {
            var workingUserNames = (await Store.GetComponentWorkspaces(component.Id)).Select(x => x.UserName).ToList();

            if (workingUserNames.Count > 0)
            {
                this.FailNotification($"Users working on component. They should commit or rollback the component.{string.Join(", ", workingUserNames)}");
                return;
            }
        }

        await Store.Insert(new ComponentHistoryEntity
        {
            ComponentId                = component.Id,
            ComponentRootElementAsYaml = component.RootElementAsYaml,
            ConfigAsYaml               = component.ConfigAsYaml,
            InsertTime                 = DateTime.Now,
            UserName                   = state.UserName
        });

        await Store.Delete(component);

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

        if (state.MainContentTab == MainContentTabs.Structure)
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
            var project = await Store.TryGetProject(state.ProjectId);
            if (project is null)
            {
                this.FailNotification("ProjectNotFound");
                return;
            }

            if (project.ConfigAsYaml != state.MainContentText)
            {
                var parseResult = Try(() => DeserializeFromYaml<ProjectConfig>(state.MainContentText));
                if (parseResult.HasError)
                {
                    this.FailNotification(parseResult.Error.ToString());
                    return;
                }

                await Store.Update(project with { ConfigAsYaml = state.MainContentText });

                Cache.Clear();

                this.SuccessNotification("Project config updated.");
            }
        }

        if (state.MainContentTab == MainContentTabs.ComponentConfig)
        {
            var result = await UpdateComponentConfig(state.ProjectId, state.ComponentId, state.MainContentText, state.UserName);
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

        if (state.MainContentTab == MainContentTabs.NewComponentConfig)
        {
            var result = await CreateNewComponent(state.MainContentText);
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

        state = state with { MainContentTab = tab };

        if (tab == MainContentTabs.Design)
        {
            state = state with { MainContentText = null };
        }
    }

    Task OnPreviewMouseLeave(MouseEvent e)
    {
        return Task.CompletedTask;
    }

    Task OnPropertyItemDropLocationDropped(DragEvent _)
    {
        var dd = state.PropertyItemDragDrop;

        if (dd.StartItemIndex.HasValue)
        {
            if (dd.EndItemIndex.HasValue)
            {
                UpdateCurrentVisualElement(x => x with
                {
                    Properties = x.Properties.MoveItemRelativeTo(dd.StartItemIndex.Value, dd.EndItemIndex.Value, dd.Position == AttributeDragPosition.Before)
                });
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

    Task OnShadowPropClicked(string propName, string value)
    {
        UpdateCurrentVisualElement(x => x with
        {
            Properties = x.Properties.Add($"{propName}: {value ?? "?"}")
        });

        if (value is null)
        {
            var propertyIndex = CurrentVisualElement.Properties.Count - 1;

            state = state with
            {
                Selection = state.Selection with { SelectedPropertyIndex = propertyIndex }
            };

            ArrangePropEditMode(propertyIndex);
        }

        return Task.CompletedTask;
    }

    Task OnStyleItemDropLocationDropped(DragEvent _)
    {
        var dd = state.StyleItemDragDrop;

        if (dd.StartItemIndex.HasValue)
        {
            if (dd.EndItemIndex.HasValue)
            {
                CurrentVisualElement.Styles.MoveItemRelativeTo(dd.StartItemIndex.Value, dd.EndItemIndex.Value, dd.Position == AttributeDragPosition.Before);
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
            // P R O J E C T
            new FlexRowCentered(Gap(16), Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(8), Height(36))
            {
                PositionRelative,
                new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, BackgroundTheme, PaddingX(4)) { "Project" },

                PartProject
            },

            // A C T I O N S

            new FlexRowCentered
            {
                new FlexRowCentered(Gap(16), Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(8), Height(36))
                {
                    PositionRelative,
                    new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, BackgroundTheme, PaddingX(4)) { "Component" },

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

                            state = result.Value;

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

                            var result = await TsxExporter.Export(state.AsExportInput());
                            if (result.HasError)
                            {
                                this.FailNotification(result.Error.Message);
                                return;
                            }

                            if (result.Value.HasChange)
                            {
                                this.SuccessNotification("File updated.");
                            }
                            else
                            {
                                this.SuccessNotification("File already same.");
                            }
                        })
                    }
                }
            },

            // F O C U S
            new IconFocus() + Color(Gray500) + Hover(Color(Blue300)) + OnClick(FocusToCurrentComponentInIde),

            // V I E W
            new FlexRowCentered(Border(1, solid, Theme.BorderColor), BorderRadius(4), Height(36))
            {
                PositionRelative,
                new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, BackgroundTheme, PaddingX(4)) { "View" },

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
                        "Structure",
                        OnClick(OnMainContentTabHeaderClicked),
                        Id((int)MainContentTabs.Structure),
                        When(state.MainContentTab == MainContentTabs.Structure, BorderRadius(4), Border(1, solid, Theme.BorderColor))
                    },
                    new FlexRowCentered(Padding(4), Border(1, solid, transparent))
                    {
                        "Output",
                        OnClick(OnMainContentTabHeaderClicked),
                        Id((int)MainContentTabs.Output),
                        When(state.MainContentTab == MainContentTabs.Output, BorderRadius(4), Border(1, solid, Theme.BorderColor))
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
                        When(state.MainContentTab.In(MainContentTabs.ComponentConfig, MainContentTabs.NewComponentConfig), BorderRadius(4), Border(1, solid, Theme.BorderColor))
                    },
                    new FlexRowCentered(Padding(4), Border(1, solid, transparent))
                    {
                        "Project",
                        OnClick(OnMainContentTabHeaderClicked),
                        Id((int)MainContentTabs.ProjectConfig),
                        When(state.MainContentTab == MainContentTabs.ProjectConfig, BorderRadius(4), Border(1, solid, Theme.BorderColor))
                    }
                }
            },

            PartMediaSizeButtons,

            PartScale,

            new LogoutButton(),

            new Style
            {
                JustifyContentSpaceBetween,
                AlignItemsCenter,
                BorderBottom(Solid(1, Theme.BorderColor)),
                Padding(8, 32)
            }
        };
    }

    Element PartLeftPanel()
    {
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
                            state = state with { LeftTab = LeftTabs.Components };
                            return Task.CompletedTask;
                        })
                    },
                    new FlexRowCentered(WidthFull, Hover(Background(Gray50), BorderRadius(36)))
                    {
                        new IconLayers() + Size(18) + Color(state.LeftTab == LeftTabs.ElementTree ? Gray500 : Gray200),
                        OnClick([StopPropagation](_) =>
                        {
                            state = state with { LeftTab = LeftTabs.ElementTree };
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
                state = state with { Selection = state.Selection with { VisualElementTreeItemPathHover = treeItemPath } };

                return Task.CompletedTask;
            },
            OnHideInDesignerToggle = () =>
            {
                UpdateCurrentVisualElement(x => x with
                {
                    HideInDesigner = !CurrentVisualElement.HideInDesigner
                });

                return Task.CompletedTask;
            },
            MouseLeave = () =>
            {
                state = state with { Selection = state.Selection with { VisualElementTreeItemPathHover = null } };
                return Task.CompletedTask;
            },
            SelectionChanged = treeItemPath =>
            {
                state = state with
                {
                    Selection = new()
                    {
                        VisualElementTreeItemPath = treeItemPath
                    }
                };

                return Task.CompletedTask;
            },

            OnDelete = DeleteSelectedTreeItem,

            CopyPaste = (source, target) =>
            {
                var sourceNode = FindTreeNodeByTreePath(state.ComponentRootElement, source);
                var targetNode = FindTreeNodeByTreePath(state.ComponentRootElement, target);

                var sourceNodeClone = SerializeToYaml(sourceNode).AsVisualElementModel();

                state = state with
                {
                    ComponentRootElement = Modify(state.ComponentRootElement, targetNode, x => x with
                    {
                        Children = x.Children.Add(sourceNodeClone)
                    })
                };

                return Task.CompletedTask;
            },
            TreeItemMove = (source, target, position) =>
            {
                VisualElementTreeOperationMoveResponse response;
                {
                    var result = VisualElementTreeOperation.Move(state.ComponentRootElement, source, target, position);
                    if (result.HasError)
                    {
                        this.FailNotification(result.Error.Message);

                        return Task.CompletedTask;
                    }

                    response = result.Value;
                }

                if (response.NewRoot is not null)
                {
                    state = state with { ComponentRootElement = response.NewRoot };
                }

                if (response.Selection is not null)
                {
                    state = state with { Selection = response.Selection };
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
            ComponentId      = state.ComponentId,
            SelectionChanged = ChangeSelectedComponent
        } + When(state.LeftTab != LeftTabs.Components, DisplayNone);

        return new FlexColumn(SizeFull, AlignItemsCenter, BorderRight(1, dotted, "#d9d9d9"), Background(White))
        {
            new FlexRow(WidthFull, AlignItemsCenter, Gap(4), PaddingLeft(8))
            {
                new IconDelete() + Size(16) + Color(text_primary) + Hover(Color(Blue300)) + OnClick(OnDeleteSelectedComponentClicked),

                GetComponentDisplayText(state.ProjectId, state.ComponentId)
            },

            tabButtons,

            new FlexColumn(WidthFull, Flex(1), OverflowAuto)
            {
                componentTree,
                elementTree
            }
        };
    }

    Element PartMediaSizeButtons()
    {
        return new FlexRowCentered(Border(1, solid, Theme.BorderColor), BorderRadius(4), PaddingX(4), Height(36))
        {
            PositionRelative,
            new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, BackgroundTheme, PaddingX(4)) { "Width" },

            new FlexRowCentered(Gap(32))
            {
                new FlexRowCentered(Gap(4))
                {
                    new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        OnClick(_ =>
                        {
                            state = state with { Preview = state.Preview with { Width = state.Preview.Width - 10 } };

                            return Task.CompletedTask;
                        }),

                        new IconMinus()
                    },
                    $"{state.Preview.Width}px",
                    new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        OnClick(_ =>
                        {
                            state = state with { Preview = state.Preview with { Width = state.Preview.Width + 10 } };

                            return Task.CompletedTask;
                        }),
                        new IconPlus()
                    }
                },

                new FlexRow(JustifyContentSpaceAround, AlignItemsCenter, Gap(16))
                {
                    MediaSizes.Select(x => new FlexRowCentered
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
        return new FlexRow(JustifyContentFlexStart, PositionRelative, OnMouseLeave(OnPreviewMouseLeave))
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
                Value       = GetAllProjectsCached().FirstOrDefault(p => p.Id == state.ProjectId)?.Name,
                OnChange = async (_, projectName) =>
                {
                    var projectEntity = GetAllProjectsCached().FirstOrDefault(x => x.Name == projectName);
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
                new IconPlus() + Size(24) + Color(state.MainContentTab == MainContentTabs.NewComponentConfig ? Gray600 : Gray300) + Hover(Color(Gray600)),

                OnClick(_ =>
                {
                    if (state.MainContentTab == MainContentTabs.NewComponentConfig)
                    {
                        state = state with { MainContentTab = MainContentTabs.Design };

                        return Task.CompletedTask;
                    }

                    state = state with
                    {
                        MainContentTab = MainContentTabs.NewComponentConfig
                    };

                    return Task.CompletedTask;
                })
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
                var component = await Store.TryGetComponent(componentId);
                if (component is null)
                {
                    return new div { $"ComponentNotFound.{componentId}" };
                }

                inputValue = component.GetNameWithExportFilePath();
            }

            inputTag = new FlexRow(WidthFull)
            {
                new MagicInput
                {
                    Name        = string.Empty,
                    Value       = inputValue,
                    Suggestions = GetTagSuggestions(state),
                    OnChange = (_, newValue) =>
                    {
                        foreach (var dbRecord in TryFindComponentByComponentNameWithExportFilePath(state.ProjectId, newValue))
                        {
                            UpdateCurrentVisualElement(x => x with { Tag = dbRecord.Id.ToString() });

                            return Task.CompletedTask;
                        }

                        UpdateCurrentVisualElement(x => x with { Tag = newValue });

                        return Task.CompletedTask;
                    },
                    IsTextAlignCenter = true
                }
            };
        }

        Element shadowProps;
        {
            shadowProps = new FlexRow(WidthFull, FlexWrap, Gap(4))
            {
                calculateShadowProps
            };

            IEnumerable<Element> calculateShadowProps()
            {
                foreach (var type in from type in Plugin.GetAllCustomComponents() where type.Name == CurrentVisualElement.Tag select type)
                {
                    foreach (var propertyInfo in from propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) select propertyInfo)
                    {
                        // has already declered
                        if ((from p in CurrentVisualElement.Properties
                                from prop in TryParseProperty(p)
                                where propertyInfo.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)
                                select prop).Any())
                        {
                            continue;
                        }

                        yield return createShadowProperty(propertyInfo);
                    }
                }
            }

            if (shadowProps.children.Count == 0)
            {
                shadowProps = null;
            }

            Element createShadowProperty(PropertyInfo propertyInfo)
            {
                return new ShadowPropertyView
                {
                    PropertyName = propertyInfo.Name,
                    PropertyType = propertyInfo.GetCustomAttribute<JsTypeInfoAttribute>()?.JsType.ToString().ToLower(),

                    OnChange    = OnShadowPropClicked,
                    Suggestions = propertyInfo.GetCustomAttribute<SuggestionsAttribute>()?.Suggestions
                };
            }
        }

        return new FlexColumn(BorderLeft(1, dotted, "#d9d9d9"), PaddingX(2), Gap(8), OverflowYAuto, Background(White))
        {
            inputTag,

            SpaceY(16),
            new FlexRow(WidthFull, AlignItemsCenter)
            {
                new div { Height(1), FlexGrow(1), Background(Gray200) },
                new span { "P R O P S", WhiteSpaceNoWrap, UserSelect(none), PaddingX(4) },
                new div { Height(1), FlexGrow(1), Background(Gray200) }
            },
            viewProps(visualElementModel.Properties),

            shadowProps,

            SpaceY(16),

            new FlexRow(WidthFull, AlignItemsCenter)
            {
                new div { Height(1), FlexGrow(1), Background(Gray200) },
                new span { "S T Y L E", WhiteSpaceNoWrap, UserSelect(none), PaddingX(4) },
                new div { Height(1), FlexGrow(1), Background(Gray200) }
            },
            viewStyles(CurrentVisualElement.Styles),
            SpaceY(16)
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
                            state = state with { Selection = state.Selection with { SelectedStyleIndex = null } };

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
                            UpdateCurrentVisualElement(x => x with
                            {
                                Styles = x.Styles.SetItem(state.Selection.SelectedStyleIndex.Value, newValue)
                            });
                        }
                        else
                        {
                            UpdateCurrentVisualElement(x => x with
                            {
                                Styles = x.Styles.Add(newValue)
                            });
                        }

                        state = state with { Selection = state.Selection with { SelectedStyleIndex = null } };

                        return Task.CompletedTask;
                    },
                    Value = value
                };
            }

            Element attributeItem(int index, string value)
            {
                var closeIcon = CreateAttributeItemCloseIcon(OnClick([StopPropagation](_) =>
                {
                    UpdateCurrentVisualElement(x => x with
                    {
                        Styles = x.Styles.RemoveAt(state.Selection.SelectedStyleIndex!.Value)
                    });

                    state = state with { Selection = state.Selection with { SelectedStyleIndex = null } };

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
                        if (styleIndex == state.Selection.SelectedStyleIndex)
                        {
                            state = state with { Selection = state.Selection with { SelectedStyleIndex = null } };
                            return Task.CompletedTask;
                        }

                        state = state with
                        {
                            Selection = new()
                            {
                                VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                                SelectedStyleIndex = styleIndex
                            }
                        };

                        const string id = "style_editor";

                        // calculate js code for focus to input editor
                        {
                            var jsCode = new StringBuilder();

                            jsCode.AppendLine($"document.getElementById('{id}').focus();");

                            // calculate text selection in edit input
                            {
                                var nameValue = CurrentVisualElement.Styles[styleIndex];
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
                    var dropLocationBefore = CreateDropLocationElement(state.StyleItemDragDrop.Position == AttributeDragPosition.Before,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { StyleItemDragDrop = state.StyleItemDragDrop with { Position = AttributeDragPosition.Before } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnStyleItemDropLocationLeaved),
                        OnDrop(OnStyleItemDropLocationDropped)
                    ]);

                    var dropLocationAfter = CreateDropLocationElement(state.StyleItemDragDrop.Position == AttributeDragPosition.After,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { StyleItemDragDrop = state.StyleItemDragDrop with { Position = AttributeDragPosition.After } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnStyleItemDropLocationLeaved),
                        OnDrop(OnStyleItemDropLocationDropped)
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
                            state = state with { Selection = state.Selection with { SelectedPropertyIndex = null } };

                            return Task.CompletedTask;
                        }),
                        props.Select((value, index) => attributeItem(index, value))
                    }
                }
            };

            async Task<Element> inputEditor()
            {
                string value = null;

                if (state.Selection.SelectedPropertyIndex >= 0)
                {
                    value = props[state.Selection.SelectedPropertyIndex.Value];
                }

                return new MagicInput
                {
                    Placeholder = "Add property",

                    Suggestions = await GetPropSuggestions(state),

                    Name = (state.Selection.SelectedPropertyIndex ?? (props.Count + 1) * -1).ToString(),

                    Id = "PROPS-INPUT-EDITOR-" + (state.Selection.SelectedPropertyIndex ?? -1),

                    OnChange = (senderName, newValue) =>
                    {
                        var index = int.Parse(senderName);

                        newValue = TryBeautifyPropertyValue(newValue);

                        if (index >= 0)
                        {
                            UpdateCurrentVisualElement(x => x with
                            {
                                Properties = x.Properties.SetItem(index, newValue)
                            });
                        }
                        else
                        {
                            UpdateCurrentVisualElement(x => x with
                            {
                                Properties = x.Properties.Add(newValue)
                            });
                        }

                        state = state with { Selection = state.Selection with { SelectedPropertyIndex = null } };

                        return Task.CompletedTask;
                    },
                    Value = value
                };
            }

            Element attributeItem(int index, string value)
            {
                var closeIcon = CreateAttributeItemCloseIcon(OnClick([StopPropagation](_) =>
                {
                    UpdateCurrentVisualElement(x => x with
                    {
                        Properties = x.Properties.RemoveAt(state.Selection.SelectedPropertyIndex!.Value)
                    });

                    state = state with { Selection = state.Selection with { SelectedPropertyIndex = null } };

                    return Task.CompletedTask;
                }));

                Element content = value;
                {
                    TryParseProperty(value).HasValue(x =>
                    {
                        if (x.Name == Design.SpreadOperator)
                        {
                            content = new FlexRow(AlignItemsCenter, FlexWrap)
                            {
                                new span { x.Value }
                            };
                        }
                        else
                        {
                            content = new FlexRow(AlignItemsCenter, FlexWrap)
                            {
                                new span(FontWeight600) { x.Name }, ": ", new span(PaddingLeft(2)) { x.Value }
                            };
                        }
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
                        if (propertyIndex == state.Selection.SelectedPropertyIndex)
                        {
                            state = state with
                            {
                                Selection = state.Selection with
                                {
                                    SelectedPropertyIndex = null
                                }
                            };
                            return Task.CompletedTask;
                        }

                        state = state with
                        {
                            Selection = new()
                            {
                                VisualElementTreeItemPath = state.Selection.VisualElementTreeItemPath,

                                SelectedPropertyIndex = propertyIndex
                            }
                        };

                        ArrangePropEditMode(propertyIndex);

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
                    var dropLocationBefore = CreateDropLocationElement(state.PropertyItemDragDrop.Position == AttributeDragPosition.Before,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { PropertyItemDragDrop = state.PropertyItemDragDrop with { Position = AttributeDragPosition.Before } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnPropertyItemDropLocationLeaved),
                        OnDrop(OnPropertyItemDropLocationDropped)
                    ]);

                    var dropLocationAfter = CreateDropLocationElement(state.PropertyItemDragDrop.Position == AttributeDragPosition.After,
                    [
                        OnDragEnter(_ =>
                        {
                            state = state with { PropertyItemDragDrop = state.PropertyItemDragDrop with { Position = AttributeDragPosition.After } };
                            return Task.CompletedTask;
                        }),
                        OnDragLeave(OnPropertyItemDropLocationLeaved),
                        OnDrop(OnPropertyItemDropLocationDropped)
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
        return new ZoomComponent
        {
            Scale = state.Preview.Scale,
            OnChange = newValue =>
            {
                state = state with
                {
                    Preview = state.Preview with
                    {
                        Scale = newValue
                    }
                };

                UpdateZoomInClient();

                return Task.CompletedTask;
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
            state = state with { ComponentRootElement = model };
        }
        else
        {
            UpdateCurrentVisualElement(x => x with
            {
                Children = x.Children.Add(model)
            });
        }

        return Task.CompletedTask;
    }

    void UpdateCurrentVisualElement(Func<VisualElementModel, VisualElementModel> modify)
    {
        state = state with
        {
            ComponentRootElement = Modify(state.ComponentRootElement, CurrentVisualElement, modify)
        };
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
            state = state with
            {
                ComponentRootElement = newModel,
                Selection = new()
                {
                    VisualElementTreeItemPath = "0"
                }
            };

            return Success;
        }

        if (path.HasNoValue() || path == "0")
        {
            state = state with { ComponentRootElement = newModel };

            return Success;
        }

        UpdateCurrentVisualElement(x => x with
        {
            Tag = newModel.Tag,
            Properties = newModel.Properties,
            Styles = newModel.Styles,
            Children = newModel.Children,
            HideInDesigner = newModel.HideInDesigner
        });

        return Success;
    }

    void UpdateZoomInClient()
    {
        Client.RunJavascript($"window.ComponentIndicatorZoom = {state.Preview.Scale}");
    }

    async Task<Element> YamlEditor()
    {
        state = state with
        {
            MainContentText = state.MainContentTab switch
            {
                MainContentTabs.Structure => SerializeToYaml(CurrentVisualElement),

                MainContentTabs.ProjectConfig => (await Store.TryGetProject(state.ProjectId))?.ConfigAsYaml,

                MainContentTabs.ComponentConfig => (await Store.TryGetComponent(state.ComponentId))?.ConfigAsYaml,

                MainContentTabs.NewComponentConfig =>
                    $"""
                     {ComponentConfigReservedName.Name}: write_component_name_here
                     {ComponentConfigReservedName.ExportFilePath}: write_export_file_path_here
                     """,

                MainContentTabs.Output => await calculateTsxCodeOfCurrentVisualElement(),

                _ => null
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

        async Task<string> calculateTsxCodeOfCurrentVisualElement()
        {
            if (state.Selection.VisualElementTreeItemPath.HasNoValue())
            {
                return "Select any component.";
            }

            var componentEntity = await Store.TryGetComponent(state.ComponentId);
            if (componentEntity is null)
            {
                return $"ComponentNotFound-id:{state.ComponentId}";
            }

            var result = await TsxExporter.CalculateElementTsxCode(state.ProjectId, componentEntity.GetConfig(), CurrentVisualElement);
            if (result.HasError)
            {
                return result.Error.Message;
            }

            result = await Prettier.FormatCode(result.Value);
            if (result.HasError)
            {
                return result.Error.Message;
            }

            return result.Value;
        }
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
                            new div(MarginLeft(calculateMarginForCenterLabel(number)))
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

            double calculateMarginForCenterLabel(int stepNumber)
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

        public static Element VerticalRuler(double scale = 100)
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

            static IReadOnlyList<Element> createTenPoints()
            {
                var returnList = new List<Element>();

                const int miniStep = 10;

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

    class ShadowPropertyView : Component<ShadowPropertyView.State>
    {
        const string SHADOW_PROP_PREFIX = "SHADOW_PROP-";

        [CustomEvent]
        public Func<string, string, Task> OnChange { get; init; }

        public string PropertyName { get; init; }

        public string PropertyType { get; init; }

        public IReadOnlyList<string> Suggestions { get; init; }

        protected override Task OverrideStateFromPropsBeforeRender()
        {
            if (PropertyName != state.Value)
            {
                state = state with
                {
                    Value = PropertyName,
                    InitialValue = PropertyName
                };
            }

            return Task.CompletedTask;
        }

        protected override Element render()
        {
            if (Suggestions is null || Suggestions.Count == 0)
            {
                return new FlexRowCentered(CursorDefault, Opacity(0.4), Padding(4, 8), BorderRadius(16), UserSelect(none), OnClick(OnShadowPropClicked))
                {
                    Hover(Background(Gray100), Opacity(0.6)),

                    Id(SHADOW_PROP_PREFIX + PropertyName),

                    PropertyName + ": " + PropertyType
                };
            }

            return new FlexRowCentered(CursorDefault, Padding(4, 8), BorderRadius(16), UserSelect(none), OnClick(OnShadowPropClicked))
            {
                Id(SHADOW_PROP_PREFIX + PropertyName),

                state.IsSuggestionsVisible ? null : OnMouseEnter(ToggleZoomSuggestions),

                state.IsSuggestionsVisible ? null : Opacity(0.4),

                !state.IsSuggestionsVisible ? null : Background(Gray50),

                OnMouseLeave(OnPropertyNameMouseLeved),

                new label
                {
                    PropertyName
                },
                new FlexRowCentered(Size(16))
                {
                    new IconArrowRightOrDown { IsArrowDown = true, style = { Width(16) } }
                },

                !state.IsSuggestionsVisible ? null : new FlexColumnCentered(MinWidth(50), PositionFixed, Zindex2, Background(Gray50), Border(1, solid, Gray100), BorderRadius(16), PaddingY(4), Left(state.SuggestionPopupLocationX), Top(state.SuggestionPopupLocationY))
                {
                    Suggestions?.Select(text => new FlexRowCentered(Padding(6, 12), BorderRadius(16), Hover(Background(Gray100)))
                    {
                        text,
                        Id(text),
                        OnClick(OnSuggestionItemClicked)
                    }),

                    OnMouseEnter(OnSuggestionBoxEntered),
                    OnMouseLeave(ToggleZoomSuggestions)
                }
            };

            [StopPropagation]
            Task ToggleZoomSuggestions(MouseEvent e)
            {
                var rect = e.target.boundingClientRect;

                state = state with
                {
                    IsSuggestionsVisible = !state.IsSuggestionsVisible,
                    SuggestionPopupLocationX = rect.left + rect.width / 2 - 24,
                    SuggestionPopupLocationY = rect.top + rect.height
                };

                return Task.CompletedTask;
            }
        }

        [StopPropagation]
        [DebounceTimeout(200)]
        Task OnPropertyNameMouseLeved(MouseEvent e)
        {
            if (state.IsEnteredSuggestionsBox is false)
            {
                state = state with { IsSuggestionsVisible = false };
            }

            return Task.CompletedTask;
        }

        Task OnShadowPropClicked(MouseEvent e)
        {
            DispatchEvent(OnChange, [PropertyName, null]);

            return Task.CompletedTask;
        }

        [StopPropagation]
        Task OnSuggestionBoxEntered(MouseEvent e)
        {
            state = state with
            {
                IsEnteredSuggestionsBox = true
            };

            return Task.CompletedTask;
        }

        [StopPropagation]
        Task OnSuggestionItemClicked(MouseEvent e)
        {
            state = state with
            {
                Value = e.target.id,
                IsSuggestionsVisible = false
            };

            DispatchEvent(OnChange, [PropertyName, state.Value]);

            return Task.CompletedTask;
        }

        internal record State
        {
            public string InitialValue { get; init; }

            public bool IsEnteredSuggestionsBox { get; init; }

            public bool IsSuggestionsVisible { get; init; }

            public double SuggestionPopupLocationX { get; init; }

            public double SuggestionPopupLocationY { get; init; }

            public string Value { get; init; }
        }
    }

    class ZoomComponent : Component<ZoomComponent.State>
    {
        [CustomEvent]
        public Func<double, Task> OnChange { get; init; }

        public double Scale { get; init; }

        protected override Task OverrideStateFromPropsBeforeRender()
        {
            if (Math.Abs(state.ScaleInitialValue - Scale) > 1)
            {
                state = state with
                {
                    Scale = Scale,
                    ScaleInitialValue = Scale
                };
            }

            return Task.CompletedTask;
        }

        protected override Element render()
        {
            return new FlexRowCentered(Border(1, solid, Theme.BorderColor), BorderRadius(4), Height(36))
            {
                PositionRelative,

                new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, BackgroundTheme, PaddingX(4))
                {
                    "Zoom"
                },

                new FlexRow(WidthFull, PaddingLeftRight(4), AlignItemsCenter, Gap(4))
                {
                    new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        OnClick(_ =>
                        {
                            if (state.Scale <= 20)
                            {
                                return Task.CompletedTask;
                            }

                            state = state with
                            {
                                Scale = state.Scale - 10
                            };

                            DispatchEvent(OnChange, [state.Scale]);

                            return Task.CompletedTask;
                        }),
                        new IconMinus()
                    },

                    new div
                    {
                        $"%{state.Scale}",
                        OnClick(ToggleZoomSuggestions)
                    },
                    state.IsSuggestionsVisible ? new FlexColumnCentered(PositionFixed, Background(White), Border(1, solid, Gray300), BorderRadius(4), PaddingY(4), Left(state.SuggestionPopupLocationX), Top(state.SuggestionPopupLocationY))
                    {
                        new[] { "%25", "%50", "%75", "%100" }.Select(text => new FlexRowCentered(Padding(6, 12), BorderRadius(4), Hover(Background(Gray100)))
                        {
                            text,
                            Id(text),
                            OnClick(OnSuggestionItemClicked)
                        })
                    } : null,

                    new FlexRowCentered(BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        OnClick(_ =>
                        {
                            if (state.Scale >= 200)
                            {
                                return Task.CompletedTask;
                            }

                            state = state with
                            {
                                Scale = state.Scale + 10
                            };

                            DispatchEvent(OnChange, [state.Scale]);

                            return Task.CompletedTask;
                        }),
                        new IconPlus()
                    }
                }
            };

            Task ToggleZoomSuggestions(MouseEvent e)
            {
                var rect = e.target.boundingClientRect;

                state = state with
                {
                    IsSuggestionsVisible = !state.IsSuggestionsVisible,
                    SuggestionPopupLocationX = rect.left + rect.width / 2 - 24,
                    SuggestionPopupLocationY = rect.top + rect.height + 8
                };

                return Task.CompletedTask;
            }
        }

        Task OnSuggestionItemClicked(MouseEvent e)
        {
            state = state with
            {
                Scale = int.Parse(e.target.id.RemoveFromStart("%")),
                IsSuggestionsVisible = false
            };

            DispatchEvent(OnChange, [state.Scale]);

            return Task.CompletedTask;
        }

        internal record State
        {
            public bool IsSuggestionsVisible { get; init; }

            public double Scale { get; init; }

            public double ScaleInitialValue { get; init; }

            public double SuggestionPopupLocationX { get; init; }

            public double SuggestionPopupLocationY { get; init; }
        }
    }
}
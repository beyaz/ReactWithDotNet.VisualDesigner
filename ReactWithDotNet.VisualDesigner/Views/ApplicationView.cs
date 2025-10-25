using System.Reflection;
using System.Text;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;
using ReactWithDotNet.VisualDesigner.Exporters;
using static ReactWithDotNet.VisualDesigner.Views.ComponentEntityExtensions;

namespace ReactWithDotNet.VisualDesigner.Views;

[Route(Routes.VisualDesigner)]
sealed class ApplicationView : Component<ApplicationState>
{
    static readonly Dictionary<string, IReadOnlyList<(string Name, IReadOnlyList<string> Suggestions)>> HtmlPropSuggestions = new()
    {
        [nameof(table)] =
        [
            (nameof(table.border), ["0", "1"]),
            (nameof(table.cellSpacing), ["0", "1", "2"]),
            (nameof(table.cellPadding), ["0", "1", "2"]),
            (nameof(table.align), ["left", "center", "right"]),
            (nameof(table.rules), ["all", "cols", "rows", "groups", "none"]),
            (nameof(table.frame), ["void", "above", "below", "hsides", "lhs", "rhs", "vsides", "box", "border"])
        ],

        [nameof(th)] =
        [
            (nameof(th.align), ["left", "center", "right", "justify"]),
            (nameof(th.valign), ["top", "middle", "bottom", "baseline"]),
            (nameof(th.scope), ["row", "col", "rowgroup", "colgroup"]),
            (nameof(th.colSpan), ["2", "3", "4", "5", "6", "7", "8", "9", "10"]),
            (nameof(th.rowSpan), ["2", "3", "4", "5", "6", "7", "8", "9", "10"])
        ],

        [nameof(tr)] =
        [
            (nameof(tr.colSpan), ["2", "3", "4", "5", "6", "7", "8", "9", "10"]),
            (nameof(tr.rowSpan), ["2", "3", "4", "5", "6", "7", "8", "9", "10"]),
            (nameof(tr.align), ["left", "center", "right", "justify"]),
            (nameof(tr.valign), ["top", "middle", "bottom", "baseline"])
        ],

        [nameof(td)] =
        [
            (nameof(td.colSpan), ["2", "3", "4", "5", "6", "7", "8", "9", "10"]),
            (nameof(td.rowSpan), ["2", "3", "4", "5", "6", "7", "8", "9", "10"]),
            (nameof(td.align), ["left", "right", "center", "justify", "char"]),
            (nameof(td.valign), ["top", "middle", "bottom", "baseline"]),
            (nameof(td.nowrap), ["true", "false"]),
            (nameof(td.scope), ["row", "col", "rowgroup", "colgroup"])
        ],

        [nameof(input)] =
        [
            (nameof(input.type), ["text", "password", "email", "number", "checkbox", "radio", "submit", "button"]),
            (nameof(input.disabled), ["true", "false"]),
            (nameof(input.readOnly), ["true", "false"]),
            (nameof(input.required), ["true", "false"])
        ],

        [nameof(a)] =
        [
            (nameof(a.rel), ["nofollow", "noopener", "noreferrer"])
        ],

        [nameof(img)] =
        [
            (nameof(img.loading), ["lazy", "eager"]),
            (nameof(img.decoding), ["sync", "async", "auto"])
        ],

        [nameof(button)] =
        [
            (nameof(button.type), ["button", "submit", "reset"]),
            (nameof(button.disabled), ["true", "false"])
        ],

        [nameof(form)] =
        [
            (nameof(form.method), ["get", "post"]),
            (nameof(form.enctype), ["application/x-www-form-urlencoded", "multipart/form-data", "text/plain"]),
            (nameof(form.novalidate), ["true", "false"])
        ]
    };

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
                    MainContentTab = MainContentTabs.Design,
                    ElementTreeEditPosition = null
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
                    MainContentTab = MainContentTabs.Design,
                    ElementTreeEditPosition = null
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

        UpdateCurrentVisualElement(parent => parent with
        {
            Children = parent.Children.Add(new()
            {
                Tag = parent.Tag switch
                {
                    var x when x == nameof(tr) => nameof(td),

                    var x when x == nameof(tbody) || x == nameof(thead) || x == nameof(tfoot) => nameof(tr),

                    var x when x == nameof(table) && parent.Children.Any(c => c.Tag == nameof(tr))    => nameof(tr),
                    var x when x == nameof(table) && parent.Children.All(c => c.Tag != nameof(thead)) => nameof(thead),
                    var x when x == nameof(table) && parent.Children.All(c => c.Tag != nameof(tbody)) => nameof(tbody),
                    var x when x == nameof(table) && parent.Children.All(c => c.Tag != nameof(tfoot)) => nameof(tfoot),

                    _ => nameof(div)
                }
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

                foreach (var parsedProperty in TryParseProperty(nameValue))
                {
               
                    var startIndex = nameValue.LastIndexOf(parsedProperty.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                    var endIndex = nameValue.Length;

                    jsCode.AppendLine($"document.getElementById('{id}').setSelectionRange({startIndex}, {endIndex});");
                }
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

    async Task<Element> createTagEditor()
    {
        if (state.ElementTreeEditPosition is null)
        {
            return null;
        }

        VisualElementModel visualElementModel = null;

        if (state.Selection.VisualElementTreeItemPath.HasValue())
        {
            visualElementModel = FindTreeNodeByTreePath(state.ComponentRootElement, state.Selection.VisualElementTreeItemPath);
        }

        if (visualElementModel is null)
        {
            return null;
        }

        string inputValue;
        {
            inputValue = visualElementModel.Tag;

            foreach (var componentId in TryReadTagAsDesignerComponentId(visualElementModel))
            {
                var component = await Store.TryGetComponent(componentId);
                if (component is null)
                {
                    return new div { $"ComponentNotFound.{componentId}" };
                }

                inputValue = component.GetNameWithExportFilePath();
            }
        }

        var inputTag = new FlexRow(WidthFull)
        {
            PositionFixed,
            Left(state.ElementTreeEditPosition.left),
            Top(state.ElementTreeEditPosition.top),
            Width(state.ElementTreeEditPosition.width),
            Height(state.ElementTreeEditPosition.height),
            Background(White),
            Border(1, solid, Gray100),
            BorderRadius(4),
            PaddingLeft(8),

            new MagicInput
            {
                Id          = "TagEditor",
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

                    state = state with { ElementTreeEditPosition = null };

                    return Task.CompletedTask;
                }
            }
        };

        Client.RunJavascript("""
                             var el = document.getElementById('TagEditor');

                             el.setSelectionRange(0, el.value.length);

                             el.focus();
                             """);

        return inputTag;
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

        var fileContent = await FileSystem.ReadAllLines(location.Value.filePath);
        if (fileContent.HasError)
        {
            this.FailNotification(fileContent.Error.Message);
            return;
        }

        var lineIndex = ExporterFactory.GetComponentLineIndexPointsInSourceFile(state.ProjectId, fileContent.Value, location.Value.targetComponentName);
        if (lineIndex.HasError)
        {
            this.FailNotification(lineIndex.Error.Message);
            return;
        }

        var exception = IdeBridge.OpenEditor(location.Value.filePath, lineIndex.Value.FirstReturnLineIndex);
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
                    new FlexRow(SizeFull, Width(state.Preview.Width + 33))
                    {
                        Ruler.VerticalRuler(state.Preview.Scale),
                        PartPreview
                    }
                };
        }

        Element right()
        {
            return PartRightPanel() + BorderBottomRightRadius(8);
        }
    }

    Task OnCommonSizeClicked(MouseEvent e)
    {
        Result<int> widthResult = e.currentTarget.data["value"] switch
        {
            "M"   => 320,
            "SM"  => 640,
            "MD"  => 768,
            "LG"  => 1024,
            "XL"  => 1280,
            "XXL" => 1536,
            _     => new ArgumentOutOfRangeException(e.currentTarget.data["value"])
        };

        if (widthResult.HasError)
        {
            this.FailNotification(widthResult.Error.Message);
            
            return Task.CompletedTask;
        }

        widthResult.Match
        (
             width =>
             {
                 state = state with
                 {
                     Preview = state.Preview with
                     {
                         Width = width
                     }
                 };
             },

             err => this.FailNotification(err.Message)
        );
        
        
        state = state with
        {
            Preview = state.Preview with
            {
                Width = widthResult.Value
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

        state = state with
        {
            PropertyItemDragDrop = new(),
            Selection = state.Selection with
            {
                SelectedPropertyIndex = null
            }
        };

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

    Task OnStyleItemDropLocationDropped(int styleIndex)
    {
        var dd = state.StyleItemDragDrop;

        if (dd.StartItemIndex.HasValue)
        {
            if (dd.EndItemIndex.HasValue)
            {
                UpdateCurrentVisualElement(m => m with
                {
                    Styles = m.Styles.MoveItemRelativeTo(dd.StartItemIndex.Value, dd.EndItemIndex.Value, dd.Position == AttributeDragPosition.Before)
                });
            }
        }

        state = state with
        {
            StyleItemDragDrop = new(),
            Selection = state.Selection with
            {
                SelectedStyleIndex = null
            }
        };

        return Task.CompletedTask;
    }

    Task OnStyleItemDropLocationLeaved(int styleIndex)
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

                            var result = await ExporterFactory.ExportToFileSystem(state.AsExportInput());
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
            EnterEditMode = rect =>
            {
                if (state.ElementTreeEditPosition is not null)
                {
                    rect = null;
                }

                state = state with
                {
                    ElementTreeEditPosition = rect
                };

                return Task.CompletedTask;
            },

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
                    ElementTreeEditPosition = null,
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
                src   = Routes.VisualDesignerPreview,
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

    Element PartRightPanel()
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

        Element shadowProps;
        {
            shadowProps = new FlexRow(WidthFull, FlexWrap, Gap(4))
            {
                calculateShadowProps
            };

            IEnumerable<Element> calculateShadowProps()
            {
                if (HtmlPropSuggestions.TryGetValue(CurrentVisualElement.Tag, out var list))
                {
                    return
                        from x in list
                        where !alreadyContainsProp(x.Name)
                        select new ShadowPropertyView
                        {
                            PropertyName = x.Name,
                            PropertyType = "string",
                            OnChange     = OnShadowPropClicked,
                            Suggestions  = x.Suggestions
                        };
                }

                return
                    from type in Plugin.AllCustomComponents
                    where type.Name == CurrentVisualElement.Tag
                    from propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    where !alreadyContainsProp(propertyInfo.Name)
                    let jsTypeInfo = propertyInfo.GetCustomAttribute<JsTypeInfoAttribute>()
                    let isStringProp = jsTypeInfo?.JsType == JsType.String
                    
                    let suggestionItems = propertyInfo.GetCustomAttribute<SuggestionsAttribute>()?.Suggestions ?? []
                    
                    select new ShadowPropertyView
                    {
                        PropertyName = propertyInfo.Name,
                        PropertyType = jsTypeInfo?.JsType.ToString().ToLower(),

                        OnChange    = OnShadowPropClicked,
                        Suggestions = isStringProp switch
                        {
                            true=> suggestionItems.ToList().ConvertAll(x=> '"' + x + '"'),
                            
                            false=>suggestionItems
                        }
                    };

                bool alreadyContainsProp(string propName)
                {
                    return HasAny(from p in CurrentVisualElement.Properties
                                  from prop in TryParseProperty(p)
                                  where propName.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)
                                  select prop);
                }
            }

            if (shadowProps.children.Count == 0)
            {
                shadowProps = null;
            }
        }

        return new FlexColumn(BorderLeft(1, dotted, "#d9d9d9"), PaddingX(2), Gap(8), OverflowYAuto, Background(White))
        {
            createTagEditor,

            SpaceY(16),
            new FlexRow(WidthFull, AlignItemsCenter)
            {
                new div { Height(1), FlexGrow(1), Background(Gray200) },
                new span { "P R O P S", WhiteSpaceNoWrap, UserSelect(none), PaddingX(4) },
                new div { Height(1), FlexGrow(1), Background(Gray200) }
            },
            viewProps(visualElementModel.Properties),

            shadowProps, ShadowPropertyView.CreatePopupHandlerView(),

            SpaceY(16),

            new FlexRow(WidthFull, AlignItemsCenter)
            {
                new div { Height(1), FlexGrow(1), Background(Gray200) },
                new span { "S T Y L E", WhiteSpaceNoWrap, UserSelect(none), PaddingX(4) },
                new div { Height(1), FlexGrow(1), Background(Gray200) }
            },
            viewStyles(CurrentVisualElement.Styles),
            new FlexColumn(Flex(1, 1, 0), JustifyContentFlexEnd, AlignItemsCenter)
            {
                new StylerComponent
                {
                    OptionSelected = newValue =>
                    {
                        var existingItemIndex =
                            (from item in CurrentVisualElement.Styles.Select((text, index) => new { text, index })
                                let styleItem = ParseStyleAttribute(item.text)
                                where styleItem.Name == ParseStyleAttribute(newValue).Name
                                select (int?)item.index).FirstOrDefault();

                        if (existingItemIndex is null)
                        {
                            UpdateCurrentVisualElement(x => x with
                            {
                                Styles = x.Styles.Add(newValue)
                            });
                        }
                        else
                        {
                            UpdateCurrentVisualElement(x => x with
                            {
                                Styles = x.Styles.SetItem(existingItemIndex.Value, newValue)
                            });
                        }

                        return Task.CompletedTask;
                    }
                }
            },
            SpaceY(16)
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
                            state = state with
                            {
                                Selection = state.Selection with
                                {
                                    SelectedStyleIndex = null
                                },
                                PropertyItemDragDrop = new (),
                                StyleItemDragDrop = new()
                            };

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

                        state = state with
                        {
                            Selection = state.Selection with
                            {
                                SelectedStyleIndex = null
                            }
                        };

                        return Task.CompletedTask;
                    },
                    Value = value
                };
            }

            Element attributeItem(int index, string value)
            {
                var isSelected = index == state.Selection.SelectedStyleIndex;
                if (state.StyleItemDragDrop.StartItemIndex == index)
                {
                    isSelected = false;
                }

                return new StyleItemView
                {
                    Value                      = value,
                    StyleIndex                 = index,
                    IsSelected                 = isSelected,
                    DragDropPosition           = state.StyleItemDragDrop.Position,
                    IsDragDropLocationsVisible = state.StyleItemDragDrop.EndItemIndex == index,
                    Close = [StopPropagation](styleIndex) =>
                    {
                        UpdateCurrentVisualElement(x => x with
                        {
                            Styles = x.Styles.RemoveAt(styleIndex)
                        });

                        state = state with
                        {
                            Selection = state.Selection with
                            {
                                SelectedStyleIndex = null
                            }
                        };

                        return Task.CompletedTask;
                    },
                    Select = [StopPropagation](styleIndex) =>
                    {
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

                                foreach (var parsedProperty in TryParseProperty(nameValue))
                                {
                                    var startIndex = nameValue.LastIndexOf(parsedProperty.Value, StringComparison.OrdinalIgnoreCase);
                                    var endIndex = nameValue.Length;

                                    jsCode.AppendLine($"document.getElementById('{id}').setSelectionRange({startIndex}, {endIndex});");
                                }
                            }

                            Client.RunJavascript(jsCode.ToString());
                        }

                        return Task.CompletedTask;
                    },

                    DragStart = styleIndex =>
                    {
                        state = state with
                        {
                            StyleItemDragDrop = state.StyleItemDragDrop with
                            {
                                StartItemIndex = styleIndex
                            }
                        };
                        return Task.CompletedTask;
                    },

                    DragEnter = styleIndex =>
                    {
                        state = state with
                        {
                            StyleItemDragDrop = state.StyleItemDragDrop with
                            {
                                EndItemIndex = styleIndex
                            }
                        };
                        return Task.CompletedTask;
                    },
                    DropLocation_Before_DragEnter = _ =>
                    {
                        state = state with { StyleItemDragDrop = state.StyleItemDragDrop with { Position = AttributeDragPosition.Before } };
                        return Task.CompletedTask;
                    },
                    DropLocation_Before_DragLeave = OnStyleItemDropLocationLeaved,
                    DropLocation_Before_Drop      = OnStyleItemDropLocationDropped,

                    DropLocation_After_DragEnter = _ =>
                    {
                        state = state with { StyleItemDragDrop = state.StyleItemDragDrop with { Position = AttributeDragPosition.After } };
                        return Task.CompletedTask;
                    },
                    DropLocation_After_DragLeave = OnStyleItemDropLocationLeaved,
                    DropLocation_After_Drop      = OnStyleItemDropLocationDropped,
                    IsMouseEnterLeaveEnabled     = state.StyleItemDragDrop.StartItemIndex is null && state.Selection.SelectedStyleIndex is null
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
                            state = state with
                            {
                                Selection = state.Selection with
                                {
                                    SelectedPropertyIndex = null
                                },
                                
                                PropertyItemDragDrop = new (),
                                StyleItemDragDrop = new()
                            };

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

                var suggestions = await GetPropSuggestions(state);
                if (suggestions.HasError)
                {
                    return new div { suggestions.Error.ToString() };

                }

                return new MagicInput
                {
                    Placeholder = "Add property",

                    Suggestions = suggestions.Value,

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
                    foreach (var x in TryParseProperty(value))
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
                    }
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

    Result<Unit> UpdateElementNode(string path, string yaml)
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

            return Unit.Value;
        }

        if (path.HasNoValue() || path == "0")
        {
            state = state with { ComponentRootElement = newModel };

            return Unit.Value;
        }

        UpdateCurrentVisualElement(x => x with
        {
            Tag = newModel.Tag,
            Properties = newModel.Properties,
            Styles = newModel.Styles,
            Children = newModel.Children,
            HideInDesigner = newModel.HideInDesigner
        });

        return Unit.Value;
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

            var result = await ExporterFactory.CalculateElementSourceCode(state.ProjectId, componentEntity.GetConfig(), CurrentVisualElement);
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
                new div(HeightFull, Width(33), OverflowHidden, PositionRelative)
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

        delegate Task PopupItemSelect(PopupItemSelectArgs e);

        delegate Task SenderMouseEnter(SenderMouseEnterArgs e);

        delegate Task SenderMouseLeave(SenderMouseLeaveArgs e);

        [CustomEvent]
        public Func<string, string, Task> OnChange { get; init; }

        public string PropertyName { get; init; }

        public string PropertyType { get; init; }

        public IReadOnlyList<string> Suggestions { get; init; }

        public static Element CreatePopupHandlerView()
        {
            return new PopupView();
        }

        protected override Task constructor()
        {
            var sender = SHADOW_PROP_PREFIX + PropertyName;

            Client.ListenEvent<PopupItemSelect>(e =>
            {
                DispatchEvent(OnChange, [PropertyName, e.Value]);

                return Task.CompletedTask;
            }, sender);

            return base.constructor();
        }

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

                OnMouseEnter([StopPropagation](e) =>
                {
                    var rect = e.target.boundingClientRect;

                    var args = new SenderMouseEnterArgs
                    {
                        Suggestions              = Suggestions,
                        SuggestionPopupLocationX = rect.left + rect.width / 2 - 24,
                        SuggestionPopupLocationY = rect.top + rect.height,
                        Sender                   = SHADOW_PROP_PREFIX + PropertyName
                    };

                    Client.DispatchEvent<SenderMouseEnter>([args]);

                    return Task.CompletedTask;
                }),

                OnMouseLeave([StopPropagation] [DebounceTimeout(200)](_) =>
                {
                    var args = new SenderMouseLeaveArgs
                    {
                        Sender = SHADOW_PROP_PREFIX + PropertyName
                    };

                    Client.DispatchEvent<SenderMouseLeave>([args]);

                    return Task.CompletedTask;
                }),

                Opacity(0.4),

                Background(Gray50),

                new label
                {
                    PropertyName
                },
                new FlexRowCentered(Size(16))
                {
                    new IconArrowRightOrDown { IsArrowDown = true, style = { Width(16) } }
                }
            };
        }

        Task OnShadowPropClicked(MouseEvent e)
        {
            DispatchEvent(OnChange, [PropertyName, null]);

            return Task.CompletedTask;
        }

        class PopupItemSelectArgs
        {
            public string Sender { get; init; }
            public string Value { get; init; }
        }

        class PopupView : Component<PopupView.PopupViewState>
        {
            protected override Task constructor()
            {
                Client.ListenEvent<SenderMouseEnter>(e =>
                {
                    state = state with
                    {
                        IsSuggestionsVisible = true,
                        EventArgs = e
                    };

                    return Task.CompletedTask;
                });

                Client.ListenEvent<SenderMouseLeave>(e =>
                {
                    if (e.Sender == state.SenderWhenMouseEntered)
                    {
                        return Task.CompletedTask;
                    }

                    if (e.Sender == state.EventArgs?.Sender)
                    {
                        state = state with
                        {
                            EventArgs = null,

                            IsSuggestionsVisible = false
                        };
                    }

                    return Task.CompletedTask;
                });

                return Task.CompletedTask;
            }

            protected override Element render()
            {
                var args = state.EventArgs;
                if (args is null)
                {
                    return null;
                }

                var suggestions = args.Suggestions ?? [];

                if (suggestions.Count == 0 || !state.IsSuggestionsVisible)
                {
                    return null;
                }

                return new FlexColumnCentered(MinWidth(50), PositionFixed, Zindex2, Background(Gray50), Border(1, solid, Gray100), BorderRadius(16), PaddingY(4), Left(args.SuggestionPopupLocationX), Top(args.SuggestionPopupLocationY))
                {
                    CursorDefault,

                    OnMouseEnter(_ =>
                    {
                        state = state with
                        {
                            SenderWhenMouseEntered = state.EventArgs.Sender
                        };

                        return Task.CompletedTask;
                    }),

                    suggestions.Select(text => new FlexRowCentered(Padding(6, 12), BorderRadius(16), Hover(Background(Gray100)))
                    {
                        text,
                        Id(text),
                        OnClick(OnSuggestionItemClicked)
                    }),

                    OnMouseLeave(ClosePopup)
                };
            }

            [StopPropagation]
            Task ClosePopup(MouseEvent _)
            {
                state = state with
                {
                    IsSuggestionsVisible = false
                };

                return Task.CompletedTask;
            }

            [StopPropagation]
            Task OnSuggestionItemClicked(MouseEvent e)
            {
                var value = e.target.id;

                state = state with
                {
                    IsSuggestionsVisible = false
                };

                var args = new PopupItemSelectArgs
                {
                    Sender = state.EventArgs.Sender,
                    Value  = value
                };

                Client.DispatchEvent<PopupItemSelect>([args], state.SenderWhenMouseEntered);

                return Task.CompletedTask;
            }

            public record PopupViewState
            {
                public SenderMouseEnterArgs EventArgs { get; init; }
                public bool IsSuggestionsVisible { get; init; }

                public string SenderWhenMouseEntered { get; init; }
            }
        }

        class SenderMouseEnterArgs
        {
            public string Sender { get; init; }

            public double SuggestionPopupLocationX { get; init; }

            public double SuggestionPopupLocationY { get; init; }
            public IReadOnlyList<string> Suggestions { get; init; }
        }

        class SenderMouseLeaveArgs
        {
            public string Sender { get; init; }
        }

        internal record State
        {
            public string InitialValue { get; init; }

            public string Value { get; init; }
        }
    }

    class StyleItemView : Component<StyleItemView.StyleItemViewState>
    {
        [CustomEvent]
        public Func<int, Task> Close { get; init; }

        public required AttributeDragPosition? DragDropPosition { get; init; }

        [CustomEvent]
        public Func<int, Task> DragEnter { get; init; }

        [CustomEvent]
        public Func<int, Task> DragStart { get; init; }

        [CustomEvent]
        public Func<int, Task> DropLocation_After_DragEnter { get; init; }

        [CustomEvent]
        public Func<int, Task> DropLocation_After_DragLeave { get; init; }

        [CustomEvent]
        public Func<int, Task> DropLocation_After_Drop { get; init; }

        [CustomEvent]
        public Func<int, Task> DropLocation_Before_DragEnter { get; init; }

        [CustomEvent]
        public Func<int, Task> DropLocation_Before_DragLeave { get; init; }

        [CustomEvent]
        public Func<int, Task> DropLocation_Before_Drop { get; init; }

        public required bool IsDragDropLocationsVisible { get; init; }

        public bool IsMouseEnterLeaveEnabled { get; init; }

        public required bool IsSelected { get; init; }

        [CustomEvent]
        public Func<int, Task> Select { get; init; }

        public required int StyleIndex { get; init; }

        public required string Value { get; init; }

        protected override Element render()
        {
            var closeIcon = CreateAttributeItemCloseIcon(OnClick(OnDeleteClicked));

            Element content = Value;
            {
                foreach (var x in TryParseProperty(Value))
                {
                    content = new FlexRow(AlignItemsCenter, FlexWrap)
                    {
                        new span(FontWeight600) { x.Name }, ": ", new span(PaddingLeft(2)) { x.Value }
                    };
                }
            }

            var styleItem = new FlexRowCentered(CursorDefault, Padding(4, 8), BorderRadius(16), UserSelect(none))
            {
                Background(IsSelected ? Gray200 : Gray50),
                Border(1, solid, Gray100),

                PositionRelative,
                IsSelected || state.IsCloseIconVisible ? closeIcon : null,

                content,
                Id(StyleIndex),

                IsMouseEnterLeaveEnabled ? OnMouseEnter(OnMouseEntered) : null,

                IsMouseEnterLeaveEnabled ? OnMouseLeave(OnMouseLeaved) : null,

                OnClick(OnClicked),

                // Drag Drop Operation
                {
                    DraggableTrue,
                    OnDragStart(OnDragStarted),
                    OnDragEnter(OnDragEntered)
                }
            };

            if (IsDragDropLocationsVisible)
            {
                var dropLocationBefore = CreateDropLocationElement(DragDropPosition == AttributeDragPosition.Before,
                [
                    OnDragEnter(OnDropLocation_Before_DragEntered),
                    OnDragLeave(OnDropLocation_Before_DragLeaved),
                    OnDrop(OnDropLocation_Before_Dropped)
                ]);

                var dropLocationAfter = CreateDropLocationElement(DragDropPosition == AttributeDragPosition.After,
                [
                    OnDragEnter(OnDropLocation_After_DragEntered),
                    OnDragLeave(OnDropLocation_After_DragLeaved),
                    OnDrop(OnDropLocation_After_Dropped)
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

        [StopPropagation]
        Task OnClicked(MouseEvent e)
        {
            state.IsCloseIconVisible = true;

            DispatchEvent(Select, [StyleIndex]);

            return Task.CompletedTask;
        }

        [StopPropagation]
        Task OnDeleteClicked(MouseEvent _)
        {
            DispatchEvent(Close, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDragEntered(DragEvent _)
        {
            state.IsCloseIconVisible = false;

            DispatchEvent(DragEnter, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDragStarted(DragEvent _)
        {
            state.IsCloseIconVisible = false;

            DispatchEvent(DragStart, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDropLocation_After_DragEntered(DragEvent _)
        {
            DispatchEvent(DropLocation_After_DragEnter, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDropLocation_After_DragLeaved(DragEvent _)
        {
            DispatchEvent(DropLocation_After_DragLeave, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDropLocation_After_Dropped(DragEvent _)
        {
            DispatchEvent(DropLocation_After_Drop, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDropLocation_Before_DragEntered(DragEvent _)
        {
            DispatchEvent(DropLocation_Before_DragEnter, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDropLocation_Before_DragLeaved(DragEvent _)
        {
            DispatchEvent(DropLocation_Before_DragLeave, [StyleIndex]);

            return Task.CompletedTask;
        }

        Task OnDropLocation_Before_Dropped(DragEvent _)
        {
            DispatchEvent(DropLocation_Before_Drop, [StyleIndex]);

            return Task.CompletedTask;
        }

        [StopPropagation]
        Task OnMouseEntered(MouseEvent e)
        {
            state.IsCloseIconVisible = true;

            return Task.CompletedTask;
        }

        [StopPropagation]
        Task OnMouseLeaved(MouseEvent e)
        {
            state.IsCloseIconVisible = false;

            return Task.CompletedTask;
        }

        internal sealed class StyleItemViewState
        {
            public bool IsCloseIconVisible { get; set; }
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
            return View();
        }

        Task OnIconMinusClicked(MouseEvent _)
        {
            if (state.Scale <= 20)
            {
                return Task.CompletedTask;
            }

            state = state with
            {
                Scale = state.Scale - 10,

                IsSuggestionsVisible = false
            };

            DispatchEvent(OnChange, [state.Scale]);

            return Task.CompletedTask;
        }

        Task OnPlusIconClicked(MouseEvent _)
        {
            if (state.Scale >= 200)
            {
                return Task.CompletedTask;
            }

            state = state with
            {
                Scale = state.Scale + 10,

                IsSuggestionsVisible = false
            };

            DispatchEvent(OnChange, [state.Scale]);

            return Task.CompletedTask;
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

        Element View()
        {
            return new div(DisplayFlex, AlignItemsCenter, JustifyContentCenter, PositionRelative, Border(1, solid, "#d5d5d8"), BorderRadius(4), Height(36), WidthFitContent)
            {
                new label(PositionAbsolute, Top(-4), Left(8), FontSize10, LineHeight7, Background("#eff3f8"), PaddingLeft(4), PaddingRight(4))
                {
                    "Zoom"
                },
                new div(DisplayFlex, WidthFull, PaddingLeft(4), PaddingRight(4), AlignItemsCenter, Gap(4))
                {
                    new div(OnClick(OnIconMinusClicked), DisplayFlex, JustifyContentCenter, AlignItemsCenter, BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        new svg(ViewBox(0, 0, 16, 16), Width(16), Height(16), Fill("currentcolor"))
                        {
                            new path(Fill("currentColor"), path.D("M12 8.667H4A.669.669 0 0 1 3.333 8c0-.367.3-.667.667-.667h8c.367 0 .667.3.667.667 0 .367-.3.667-.667.667Z"))
                        }
                    },
                    new div(OnMouseEnter(ToggleZoomSuggestions))
                    {
                        $"%{state.Scale}"
                    },
                    new div(OnClick(OnPlusIconClicked), DisplayFlex, JustifyContentCenter, AlignItemsCenter, BorderRadius(100), Padding(3), Background(Blue200), Hover(Background(Blue300)))
                    {
                        new svg(ViewBox(0, 0, 16, 16), Width(16), Height(16), Fill("currentcolor"))
                        {
                            new path(Fill("currentColor"), path.D("M12 8.667H8.667V12c0 .367-.3.667-.667.667A.669.669 0 0 1 7.333 12V8.667H4A.669.669 0 0 1 3.333 8c0-.367.3-.667.667-.667h3.333V4c0-.366.3-.667.667-.667.367 0 .667.3.667.667v3.333H12c.367 0 .667.3.667.667 0 .367-.3.667-.667.667Z"))
                        }
                    }
                },
                !state.IsSuggestionsVisible ? null :
                    new div(OnMouseLeave(ToggleZoomSuggestions), DisplayFlex, JustifyContentCenter, AlignItemsCenter, PositionFixed, Background(White), Border(1, solid, Gray300), BorderRadius(4), PaddingTop(4), PaddingBottom(4), Left(state.SuggestionPopupLocationX), Top(state.SuggestionPopupLocationY), ZIndex(3))
                    {
                        new div
                        {
                            from item in new[] { "%25", "%50", "%75", "%100", "%125" }
                            select new div(Id(item), OnClick(OnSuggestionItemClicked), DisplayFlex, JustifyContentCenter, AlignItemsCenter, Padding(6, 12), BorderRadius(4), Hover(Background(Gray100)))
                            {
                                item
                            }
                        }
                    }
            };
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
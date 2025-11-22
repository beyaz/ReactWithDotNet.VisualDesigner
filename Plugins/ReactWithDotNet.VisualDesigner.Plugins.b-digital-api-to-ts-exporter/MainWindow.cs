using System.IO;
using System.Text;
using Newtonsoft.Json;
using ReactWithDotNet;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;

namespace BDigitalFrameworkApiToTsExporter;

[Route(Routes.MainWindow)]
sealed class MainWindow : Component<MainWindow.State>
{
    static string StateCacheFilePath => Path.Combine(Path.GetTempPath(), "BDigitalFrameworkApiToTsExporter.state.json");

    static State StateCache
    {
        get
        {
            if (File.Exists(StateCacheFilePath))
            {
                return JsonConvert.DeserializeObject<State>(File.ReadAllText(StateCacheFilePath));
            }

            return null;
        }
        set => File.WriteAllText(StateCacheFilePath, JsonConvert.SerializeObject(value), Encoding.UTF8);
    }

    string ProjectDirectory => new DirectoryInfo(state.AssemblyFilePath).Parent?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;

    protected override Task constructor()
    {
        state = new()
        {
            ApiNames      = [],
            Files         = [],
            StatusMessage = "Loading..."
        };
        
        Client.GotoMethod(InitializeForm,TimeSpan.FromMilliseconds(500));
        
        return Task.CompletedTask;
    }

    async Task InitializeForm()
    {
        var cachedState = StateCache;

        state = new()
        {
            AssemblyFilePath = @"D:\workgit\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\BOA.InternetBanking.Payments.API.dll"
        };

        if (cachedState is not null)
        {
            state = JsonConvert.DeserializeObject<State>(JsonConvert.SerializeObject(cachedState));
        }

        await OnAssemblyFilePathChanged();

        if (state.ApiNames.Count > 0)
        {
            await OnApiSelected(cachedState?.SelectedApiName ?? "Religious");
        }


        if (cachedState?.SelectedFilePath is not null)
        {
            await OnFileSelected(cachedState.SelectedFilePath);
        }

        if (cachedState is not null)
        {
            StateCache = cachedState;
        }

        // clear status

        state.StatusMessage ??= "Ready";

        state.IsExportingAllFiles = false;

        state.IsExportingSelectedFile = false;
    }

    protected override Element render()
    {
        return new div(WidthFull, HeightFull, BackgroundColor(WhiteSmoke), Padding(16), FontSize13, FontFamily("-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol'"), DisplayFlex, FlexDirectionColumn, Gap(4))
        {
            new div(WidthFull, BorderRadius(4), BackgroundColor(White), Border(1, solid, Gray300), DisplayFlex, FlexDirectionColumn, CursorDefault, FlexGrow(1))
            {
                new div(BorderBottom(1, solid, "#d1d5db"), Padding(8))
                {
                    new div(FontWeight600)
                    {
                        "AssemblyFilePath"
                    },
                    new input(input.Type("text"), input.ValueBind(()=> state.AssemblyFilePath), input.ValueBindDebounceTimeout(1000), input.ValueBindDebounceHandler(OnAssemblyFilePathChanged), WidthFull, Border(1, solid, Gray300), BorderRadius(4), PaddingLeft(4), OutlineNone, PaddingTop(4), PaddingBottom(4))
                },
                new div(WidthFull, DisplayFlex, Flex(1, 1, 0))
                {
                    new div(Width("20%"), DisplayFlex, FlexDirectionColumn, BorderRight(1, solid, "#d1d5db"))
                    {
                        new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, BorderBottom(1, solid, "#d1d5db"))
                        {
                            new div
                            {
                                "Api List"
                            }
                        },
                        new div(Flex(1, 1, 0), Padding(8), DisplayFlex, FlexDirectionColumn, Gap(16), OverflowAuto)
                        {
                            from item in state.ApiNames
                            select new div(OnClick(OnApiSelected), Id(item), Border(1, solid, Gray300), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, state.SelectedApiName == item ? BackgroundColor(Gray100) : BackgroundColor(White), Hover(BorderColor(Gray500)), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"))
                            {
                                new div(Padding(4))
                                {
                                    item
                                }
                            }
                        }
                    },
                    new div(Width("30%"), BorderRight(1, solid, "#d1d5db"), DisplayFlex, FlexDirectionColumn)
                    {
                        new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, BorderBottom(1, solid, "#d1d5db"))
                        {
                            new div
                            {
                                "Files"
                            }
                        },
                        new div(Flex(1, 1, 0), Padding(8), DisplayFlex, FlexDirectionColumn, Gap(16), OverflowAuto)
                        {
                            from item in state.Files
                            select new div(Id(item.Path), OnClick(OnFileSelected), Border(1, solid, Gray300), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, state.SelectedFilePath== item.Path ? BackgroundColor(Gray100) : BackgroundColor(White), Hover(BorderColor(Gray500)), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"))
                            {
                                new div(Padding(4))
                                {
                                    Path.GetFileName(item.Path)
                                }
                            }
                        }
                    },
                    new div(Width("50%"), DisplayFlex, FlexDirectionColumn)
                    {
                        new div(Height(40), DisplayFlex, BorderBottom(1, solid, "#d1d5db"))
                        {
                            new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, Width("50%"))
                            {
                                state.Files.Count == 0 ? null :
                                    new div(OnClick(OnExportAllClicked), Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"), DisplayFlex, Gap(8), AlignItemsCenter)
                                    {
                                        new div
                                        {
                                            "Export All"
                                        },
                                        !state.IsExportingAllFiles ? null :
                                            new LoadingIcon(Width(20), Height(20))
                                        
                                    }
                                
                            },
                            new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, Width("50%"), Gap(8))
                            {
                                state.SelectedFilePath is  null ? null :
                                    new div(OnClick(OnExportClicked), Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"), DisplayFlex, Gap(8), AlignItemsCenter)
                                    {
                                        new div
                                        {
                                            "Export Selected File"
                                        },
                                        !state.IsExportingSelectedFile ? null :
                                            new LoadingIcon(Width(20), Height(20))
                                        
                                    }
                                
                            }
                        },
                        new div(DisplayFlex, FlexDirectionColumn, FlexGrow(1))
                        {
                            new div(Height(40), DisplayFlex, AlignItemsCenter, PaddingLeft(8))
                            {
                                new div
                                {
                                    CalculateSelectedFilePathRelativeToProject()
                                }
                            },
                            new div(Padding(4), WidthFull, FlexGrow(1), OverflowHidden, BorderTop(1, solid, "#d1d5db"))
                            {
                                new TsFileViewer
                                {
                                    Value = GetSelectedFileContent()
                                }
                            }
                        }
                    }
                },
                new div(PaddingLeft(8), BorderTop(1, solid, "#d1d5db"))
                {
                    new div(FontSize12)
                    {
                        state.StatusMessage
                    }
                }
            }
        };
    }

    string CalculateSelectedFilePathRelativeToProject()
    {
        if (state.SelectedFilePath.HasNoValue)
        {
            return string.Empty;
        }

        var names = state.SelectedFilePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        return "/" + string.Join("/", names.SkipWhile(x => x != "ClientApp").Skip(1));
    }

    string GetSelectedFileContent()
    {
        return state.Files.FirstOrDefault(x => x.Path == state.SelectedFilePath)?.Content ?? string.Empty;
    }

    Task OnApiSelected(MouseEvent e)
    {
        return OnApiSelected(e.currentTarget.id);
    }

    async Task OnApiSelected(string apiName)
    {
        state.SelectedApiName = apiName;

        await UpdateFiles();
    }

    Task OnAssemblyFilePathChanged()
    {
        if (!File.Exists(state.AssemblyFilePath))
        {
            state.StatusMessage = "Fail > FileNotFound." + state.AssemblyFilePath;

            state.ApiNames = [];

            state.Files = [];

            state.SelectedFilePath = null;

            return Task.CompletedTask;
        }

        var assembly = CecilHelper.ReadAssemblyDefinition(state.AssemblyFilePath);
        if (assembly.HasError)
        {
            state.StatusMessage = "Fail > " + assembly.Error.Message;

            return Task.CompletedTask;
        }

        var apiTypes = from type in assembly.Value.MainModule.Types where type.BaseType?.Name == "CommonControllerBase" select type;

        state.ApiNames = (from type in apiTypes select type.Name.RemoveFromEnd("Controller")).ToList();

        state.StatusMessage = $"Ready > {state.ApiNames.Count} api listed.";

        state.SelectedApiName = null;

        state.SelectedFilePath = null;

        StateCache = state;

        return Task.CompletedTask;
    }

    async Task OnExportAllClicked()
    {
        state.IsExportingAllFiles = false;

        var count = 0;

        foreach (var fileModel in state.Files)
        {
            var result = await FileSystem.Save(fileModel);
            if (result.HasError)
            {
                state.StatusMessage = result.Error.Message;
                return;
            }

            count++;
        }

        state.StatusMessage = $"Success > {count} file exported.";
    }

    Task OnExportAllClicked(MouseEvent e)
    {
        state.IsExportingAllFiles = true;

        state.StatusMessage = "Executing...";

        Client.GotoMethod(OnExportAllClicked, TimeSpan.FromMilliseconds(500));

        return Task.CompletedTask;
    }

    Task OnExportClicked(MouseEvent e)
    {
        state.StatusMessage = "Executing...";

        state.IsExportingSelectedFile = true;

        Client.GotoMethod(OnExportClicked, TimeSpan.FromMilliseconds(500));

        return Task.CompletedTask;
    }

    async Task OnExportClicked()
    {
        state.IsExportingSelectedFile = false;

        var count = 0;

        foreach (var fileModel in state.Files.Where(f => f.Path == state.SelectedFilePath))
        {
            var result = await FileSystem.Save(fileModel);
            if (result.HasError)
            {
                state.StatusMessage = result.Error.Message;
                return;
            }

            count++;
        }

        state.StatusMessage = $"Success > {count} file exported.";
    }

    async Task OnFileSelected(MouseEvent e)
    {
        await OnFileSelected(e.currentTarget.id);

        StateCache = state;
    }

    Task OnFileSelected(string selectedFilePath)
    {
        state.StatusMessage = "Ready";

        state.SelectedFilePath = selectedFilePath;

        return Task.CompletedTask;
    }

    async Task UpdateFiles()
    {
        if (!state.SelectedApiName.HasValue)
        {
            return;
        }

        var files = Exporter.CalculateFiles(ProjectDirectory, state.SelectedApiName);

        List<FileModel> fileModels = [];

        await foreach (var file in files)
        {
            if (file.HasError)
            {
                state.StatusMessage = file.Error.Message;
                return;
            }

            fileModels.Add(file.Value);
        }

        state.Files = fileModels;

        state.StatusMessage = $"Ready > {fileModels.Count} file listed.";
    }

    internal record State
    {
        public string AssemblyFilePath { get; init; }

        public string StatusMessage { get; set; }

        public IReadOnlyList<string> ApiNames { get; set; }

        public string SelectedApiName { get; set; }

        public IReadOnlyList<FileModel> Files { get; set; }

        public string SelectedFilePath { get; set; }

        public bool IsExportingSelectedFile { get; set; }

        public bool IsExportingAllFiles { get; set; }
    }
}

class TsFileViewer : PluginComponentBase
{
    public string Value { get; set; }

    protected override Element render()
    {
        return new Editor
        {
            width           = "100%",
            value           = Value,
            defaultLanguage = "typescript",
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
}

// Taken from https://www.w3schools.com/howto/tryit.asp?filename=tryhow_css_loader
sealed class LoadingIcon : PluginComponentBase
{
    public LoadingIcon(params Modifier[] modifiers)
    {
        Add(modifiers);
    }

    public LoadingIcon()
    {
    }

    protected override Element render()
    {
        return new div
        {
            new style
            {
                """
                @-webkit-keyframes spin
                {
                    0% { -webkit-transform: rotate(0deg); }
                    100% { -webkit-transform: rotate(360deg); }
                }

                @keyframes spin
                {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }
                """,

                new CssClass("loader",
                [
                    Border(1, solid, "#f3f3f3"),
                    BorderRadius("50%"),
                    BorderTop(1, solid, "#3498db"),
                    SizeFull,
                    WebkitAnimation("spin 1s linear infinite"),
                    Animation("spin 1s linear infinite")
                ])
            },

            new div { className = "loader" }
        };
    }
}

static class ExporterPlugin
{
    [TryCreateElementForPreview]
    public static Scope TryCreateElementForPreview(Scope scope)
    {
        var input = Plugin.TryCreateElementForPreviewInputKey[scope];

        Element element = input.Tag switch
        {
            nameof(TsFileViewer) => new TsFileViewer { id = input.Id, onMouseClick = input.OnMouseClick },
            nameof(LoadingIcon)  => new LoadingIcon { id  = input.Id, onMouseClick = input.OnMouseClick },
            _                    => null
        };

        if (element is not null)
        {
            return Scope.Create(new()
            {
                { Plugin.TryCreateElementForPreviewOutputKey, element }
            });
        }

        return Scope.Empty;
    }
}
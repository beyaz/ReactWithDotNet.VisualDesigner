using System.IO;
using System.Text;
using Newtonsoft.Json;
using ReactWithDotNet;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;
using ReactWithDotNet.VisualDesigner;

namespace BDigitalFrameworkApiToTsExporter;

class MainWindow : Component<MainWindow.State>
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

    protected override async Task constructor()
    {
        var cachedState = StateCache;

        state = new()
        {
            AssemblyFilePath = @"D:\work\BOA.BusinessModules\Dev\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\BOA.InternetBanking.Payments.API.dll"
        };

        if (cachedState is not null)
        {
            state = JsonConvert.DeserializeObject<State>(JsonConvert.SerializeObject(cachedState));
        }

        await OnAssemblyFilePathChanged();

        await OnApiSelected(cachedState?.SelectedApiName ?? "Religious");

        if (cachedState?.SelectedFilePath is not null)
        {
            await OnFileSelected(cachedState.SelectedFilePath);
        }

        if (cachedState is not null)
        {
            StateCache = cachedState;
        }

        state.StatusMessage = "Ready";
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
                    new input(input.Type("text"), input.ValueBind(() => state.AssemblyFilePath), input.ValueBindDebounceTimeout(1000), input.ValueBindDebounceHandler(OnAssemblyFilePathChanged), WidthFull, Border(1, solid, Gray300), BorderRadius(4), PaddingLeft(4), OutlineNone, PaddingTop(4), PaddingBottom(4))
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
                            select new div(Id(item.Path), OnClick(OnFileSelected), Border(1, solid, Gray300), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, state.SelectedFilePath == item.Path ? BackgroundColor(Gray100) : BackgroundColor(White), Hover(BorderColor(Gray500)), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"))
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
                                state.Files.Count == 0
                                    ? null
                                    : new div(OnClick(OnExportAllClicked), Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"))
                                    {
                                        "Export All"
                                    }
                            },
                            new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, Width("50%"))
                            {
                                state.SelectedFilePath is null
                                    ? null
                                    : new div(OnClick(OnExportClicked), Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)), BoxShadow("0 1px 3px rgba(0,0,0,0.2)"))
                                    {
                                        "Export"
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
        if (state.SelectedFilePath.HasNoValue())
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
        state.StatusMessage = "Executing...";

        Client.GotoMethod(OnExportAllClicked, TimeSpan.FromMilliseconds(500));

        return Task.CompletedTask;
    }

    Task OnExportClicked(MouseEvent e)
    {
        state.StatusMessage = "Executing...";

        Client.GotoMethod(OnExportClicked, TimeSpan.FromMilliseconds(500));

        return Task.CompletedTask;
    }

    async Task OnExportClicked()
    {
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
        if (!state.SelectedApiName.HasValue())
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

class ExporterPlugin : PluginBase
{
    public override Element TryCreateElementForPreview(string tag, string id, MouseEventHandler onMouseClick)
    {
        if (tag == nameof(TsFileViewer))
        {
            return new TsFileViewer
            {
                id = id,

                onMouseClick = onMouseClick
            };
        }

        return null;
    }
}
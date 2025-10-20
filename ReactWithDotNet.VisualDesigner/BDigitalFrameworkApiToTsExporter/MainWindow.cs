using System.IO;
using ReactWithDotNet;
using ReactWithDotNet.ThirdPartyLibraries.MonacoEditorReact;
using ReactWithDotNet.VisualDesigner;

namespace BDigitalFrameworkApiToTsExporter;

class MainWindow : Component<MainWindow.Model>
{
    string ProjectDirectory => new DirectoryInfo(state.AssemblyFilePath).Parent?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;

    protected override async Task constructor()
    {
        state = new()
        {
            StatusMessage = "Ready",

            SelectedApiName = "Religious",

            AssemblyFilePath = @"D:\work\BOA.BusinessModules\Dev\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\BOA.InternetBanking.Payments.API.dll"
        };

        await TryUpdateApiList();

        await UpdateFiles(null);
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
                    new input(input.Type("text"), input.Value(state.AssemblyFilePath), WidthFull, Border(1, solid, Gray300), BorderRadius(4), PaddingLeft(4), OutlineNone)
                },
                new div(WidthFull, DisplayFlex, Flex(1, 1, 0))
                {
                    new div(Width(200), DisplayFlex, FlexDirectionColumn, BorderRight(1, solid, "#d1d5db"))
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
                            select new div(OnClick(OnApiSelected), Id(item), Border(1, solid, Gray300), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, state.SelectedApiName == item ? BackgroundColor(Gray100) : BackgroundColor(White), Hover(BorderColor(Gray500)))
                            {
                                new div(Padding(4))
                                {
                                    item
                                }
                            }
                        }
                    },
                    new div(Width(200), BorderRight(1, solid, "#d1d5db"), DisplayFlex, FlexDirectionColumn)
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
                            select new div(Id(item.Path), OnClick(OnFileSelected), Border(1, solid, Gray300), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter, state.SelectedFilePath== item.Path ? BackgroundColor(Gray100) : BackgroundColor(White), Hover(BorderColor(Gray500)))
                            {
                                new div(Padding(4))
                                {
                                    Path.GetFileName(item.Path)
                                }
                            }
                        }
                    },
                    new div(WidthFull, DisplayFlex, FlexDirectionColumn)
                    {
                        new div(Height(40), DisplayFlex, BorderBottom(1, solid, "#d1d5db"))
                        {
                            new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, Width("50%"))
                            {
                                new div(OnClick(OnExportAllClicked), Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)))
                                {
                                    "Export All"
                                }
                            },
                            new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, Width("50%"))
                            {
                                new div(OnClick(OnExportClicked), Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)))
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
                            new div(Padding(4), WidthFull, Border(1, solid, Gray300), BorderRadius(4), FlexGrow(1), OverflowHidden)
                            {
                                new TsFileViewer()
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

    async Task OnApiSelected(MouseEvent e)
    {
        state.SelectedApiName = e.currentTarget.id;

        await UpdateFiles(null);
    }

    async Task OnExportAllClicked(MouseEvent e)
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

        state.StatusMessage = $"Success / ( {count} file exported. )";
    }

    async Task OnExportClicked(MouseEvent e)
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

        state.StatusMessage = $"Success / ( {count} file exported. )";
    }

    async Task OnFileSelected(MouseEvent e)
    {
        state.SelectedFilePath = e.currentTarget.id;

        await UpdateFiles(null);
    }

    Task TryUpdateApiList()
    {
        if (!File.Exists(state.AssemblyFilePath))
        {
            return Task.CompletedTask;
        }

        var assembly = CecilHelper.ReadAssemblyDefinition(state.AssemblyFilePath);
        if (assembly.HasError)
        {
            state.StatusMessage = assembly.Error.Message;
            return Task.CompletedTask;
        }

        var apiTypes = from type in assembly.Value.MainModule.Types where type.BaseType?.Name == "CommonControllerBase" select type;

        state.ApiNames = from type in apiTypes select type.Name.RemoveFromEnd("Controller");

        return Task.CompletedTask;
    }

    async Task UpdateFiles(MouseEvent _)
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
    }

    internal record Model
    {
        public string AssemblyFilePath { get; init; }

        public string StatusMessage { get; set; }

        public IEnumerable<string> ApiNames { get; set; }

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
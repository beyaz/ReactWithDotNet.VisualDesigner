using System.IO;
using ReactWithDotNet;

namespace BDigitalFrameworkApiToTsExporter;

class MainWindow : Component<MainWindow.Model>
{
    protected override Element render()
    {
        return new div(WidthFull, HeightFull, BackgroundColor(WhiteSmoke), Padding(16), FontSize13, FontFamily("-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol'"))
        {
            new div(WidthFull, HeightFull, BorderRadius(4), BackgroundColor(White), Border(1, solid, Gray300), DisplayFlex, FlexDirectionColumn, CursorDefault)
            {
                new div(BorderBottom(1, solid, "#d1d5db"), Padding(8))
                {
                    new div(FontWeight600)
                    {
                        "AssemblyFilePath"
                    },
                    new input(input.Type("text"), input.Value(state.AssemblyFilePath), WidthFull, Border(1, solid, Gray300), BorderRadius(4), PaddingLeft(4))
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
                    new div(WidthFull, DisplayFlex)
                    {
                        new div(Width(150), BorderRight(1, solid, "#d1d5db"), DisplayFlex, FlexDirectionColumn)
                        {
                            new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, BorderBottom(1, solid, "#d1d5db")) 
                            {
                                new div
                                {
                                    "Files"
                                }
                            },
                            new div(Flex(1, 1, 0), Padding(8), DisplayFlex, FlexDirectionColumn, Gap(16))
                            {
                                new div(Border(1, solid, Gray300), BorderRadius(4), DisplayFlex, JustifyContentCenter, AlignItemsCenter)
                                {
                                    new div(Padding(4))
                                    {
                                        "Api 1"
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
                                    new div(Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)))
                                    {
                                        "Export All"
                                    }
                                },
                                new div(Height(40), FontWeight600, DisplayFlex, JustifyContentCenter, AlignItemsCenter, Width("50%"))
                                {
                                    new div(Padding(4, 8), Border(1, solid, Gray300), BorderRadius(4), Hover(BackgroundColor(Gray100)))
                                    {
                                        "Export"
                                    }
                                }
                            },
                            new div(HeightFull)
                        }
                    }
                }
            }
        };
    }

    protected override Task constructor()
    {
        state = new()
        {
            StatusMessage = "Ready",
            
            AssemblyFilePath = @"D:\work\BOA.BusinessModules\Dev\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\BOA.InternetBanking.Payments.API.dll"
        };

        return TryUpdateApiList();
    }
       
    static async Task Main2()
    {
        await foreach (var result in Exporter.TryExport())
        {
            if (result.HasError)
            {
                throw result.Error;
            }
        }
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
     
    Task OnApiSelected(MouseEvent e)
    {

        state.SelectedApiName = e.currentTarget.id;
        
        return Task.CompletedTask;
    }
    
    

    internal record Model
    {
        public string AssemblyFilePath { get; init; }
        
        public string StatusMessage { get; set; }
        
        public IEnumerable<string> ApiNames { get; set; }

        public string SelectedApiName { get; set; }
    }
}
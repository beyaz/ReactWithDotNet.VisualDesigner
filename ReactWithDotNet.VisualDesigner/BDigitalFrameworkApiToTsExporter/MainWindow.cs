using ReactWithDotNet;

namespace BDigitalFrameworkApiToTsExporter;

class MainWindow : Component<MainWindow.Model>
{
    protected override Element render()
    {
        return new div(WidthFull, HeightFull, BackgroundColor(WhiteSmoke), Padding(16), FontSize13, FontFamily("-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol'"))
        {
            new div(WidthFull, HeightFull, BorderRadius(4), BackgroundColor(White), Border(1, solid, Gray300), DisplayFlex, FlexDirectionColumn)
            {
                new div(BorderBottom(1, solid, "#d1d5db"), Padding(8))
                {
                    new div(FontWeight600)
                    {
                        "AssemblyFilePath"
                    },
                    new input(input.Type("text"), WidthFull, Border(1, solid, Gray300), BorderRadius(4))
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

    internal record Model
    {
        public string AssemblyFilePath { get; init; }
    }
}
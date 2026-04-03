using ReactWithDotNet;

namespace BDigitalFrameworkApiToTsExporter;

[Route(Routes.Home)]
sealed class HomeWindow : Component
{
    protected override Element render()
    {
        return new FlexColumnCentered(SizeFull)
        {
            new FlexColumn(Gap(24), Color(Gray600))
            {
                new a(Border(1,solid,Gray400), Padding(16), BorderRadius(4), TextAlignCenter, Hover(BorderColor(Gray600)))
                {
                    href = Routes.VisualDesigner,
                    text = "Visual Designer"
                },

                new a(Border(1,solid,Gray400), Padding(16), BorderRadius(4), TextAlignCenter, Hover(BorderColor(Gray600)))
                {
                    href   = InternalRoutes.MainWindow,
                    text   = "Internet Branch Types Exporter"
                }
            }
        };
    }
}
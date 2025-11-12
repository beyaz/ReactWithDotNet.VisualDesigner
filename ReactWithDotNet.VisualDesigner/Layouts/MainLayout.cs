using Microsoft.AspNetCore.Http.Extensions;

namespace ReactWithDotNet.VisualDesigner;

sealed class MainLayout : PureComponent, IPageLayout
{
    public string ContainerDomElementId => "app";

    public string InitialScript =>
        $$"""
          import {ReactWithDotNet} from '{{IndexJsFilePath}}';

          ReactWithDotNet.StrictMode = false;

          ReactWithDotNet.RequestHandlerPath = '{{RequestHandlerPath}}';

          ReactWithDotNet.RenderComponentIn({
            idOfContainerHtmlElement: '{{ContainerDomElementId}}',
            renderInfo: {{RenderInfo.ToJsonString()}}
          });
          
          window.addEventListener("unload", function ()
          {
              navigator.sendBeacon("/shutdown");
          });
          
          """;

    public ComponentRenderInfo RenderInfo { get; set; }

    protected override Element render()
    {
        return new html
        {
            Lang("tr"),
            DirLtr,

            // Global Styles
            Margin(0),
            Color(Theme.text_primary),
            WebkitFontSmoothingAntialiased,
            MozOsxFontSmoothingGrayScale,
            FontWeight400,
            FontSize(1 * rem),
            LineHeight(1.5 * CssUnit.em),

            new head
            {
                new meta { charset = "utf-8" },
                new meta { name    = "viewport", content = "width=device-width, initial-scale=1" },
                new title { "React Visual Designer" },
                new link { rel = "icon", href = $"{Context.wwwroot}/favicon.ico" },
                
                new link
                {
                    rel  = "stylesheet",
                    type = "text/css",
                    href = IndexCssFilePath
                },

                new style
                {
                    """

                    * {
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }

                    """
                },

                new script { src = "https://cdn.tailwindcss.com" },

                ElementIndicators,
                
                HomePageUnload,
                
                // M a t e r i a l   I c o n s
                new link { href = "https://fonts.googleapis.com/icon?family=Material+Icons", rel          = "stylesheet" },
                new link { href = "https://fonts.googleapis.com/icon?family=Material+Icons+Outlined", rel = "stylesheet" },
                new link { href = "https://fonts.googleapis.com/icon?family=Material+Icons+Round", rel    = "stylesheet" },
                new link { href = "https://fonts.googleapis.com/icon?family=Material+Icons+Sharp", rel    = "stylesheet" },
                new link { href = "https://fonts.googleapis.com/icon?family=Material+Icons+Two+Tone", rel    = "stylesheet" }
            },
            new body(Margin(0), Height100vh)
            {
                new div(Id(ContainerDomElementId), SizeFull)
            }
        };
        
        Element ElementIndicators()
        {
            var isPreviewPage = Context.HttpContext.Request.GetDisplayUrl().EndsWith(Routes.VisualDesignerPreview, StringComparison.OrdinalIgnoreCase);
            if (isPreviewPage)
            {
                const string HelperJsFileResourceName = "ReactWithDotNet.UIDesigner.Resources.ComponentIndicator.js";
                return new script
                {
                    src = HelperJsFileResourceName
                };
            }

            return null;
        }
        
        Element HomePageUnload()
        {
            var isHomePage = Context.HttpContext.Request.GetDisplayUrl().EndsWith(Routes.VisualDesigner, StringComparison.OrdinalIgnoreCase);
            if (isHomePage)
            {
                return new script
                {
                    """
                    window.addEventListener("unload", function ()
                    {
                        navigator.sendBeacon("/shutdown");
                    });
                    """
                };
            }

            return null;
        }
    }
}
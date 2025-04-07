using System.IO;
using System.Text;

namespace ReactWithDotNet.VisualDesigner.Views;

sealed class ApplicationPreview : Component
{
    public Task Refresh()
    {
        return Task.CompletedTask;
    }

    protected override Element componentDidCatch(Exception exceptionOccurredInRender)
    {
        return new div(Background(Gray100))
        {
            exceptionOccurredInRender.ToString()
        };
    }
    
    

    protected override Task constructor()
    {
        Client.ListenEvent("RefreshComponentPreview", Refresh);

        return Task.CompletedTask;
    }

    [StopPropagation]
    Task OnItemClick(MouseEvent e)
    {
        var visualElementTreeItemPath = e.target.id;

        var sb = new StringBuilder();

        sb.AppendLine("var parentWindow = window.parent;");
        sb.AppendLine("if(parentWindow)");
        sb.AppendLine("{");
        sb.AppendLine("  var reactWithDotNet = parentWindow.ReactWithDotNet;");
        sb.AppendLine("  if(reactWithDotNet)");
        sb.AppendLine("  {");
        sb.AppendLine($"    reactWithDotNet.DispatchEvent('Change_VisualElementTreeItemPath', ['{visualElementTreeItemPath}']);");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        
        Client.RunJavascript(sb.ToString());
        
        return Task.CompletedTask;
    }
    
    protected override Element render()
    {
        var userName = Environment.UserName; // future: get userName from cookie or url

        var appState = GetUserLastState(userName);

        if (appState is null)
        {
            return new div(Size(200), Background(Gray100))
            {
                "Has no state"
            };
        }
        
        var rootElement = appState.ComponentRootElement;
        if (rootElement is null)
        {
            return null;
        }

        VisualElementModel highlightedElement = null;
        {
            var selection = appState.Selection;
            if (selection.VisualElementTreeItemPathHover.HasValue())
            {
                highlightedElement = FindTreeNodeByTreePath(rootElement, selection.VisualElementTreeItemPathHover);
            }
            else if (selection.VisualElementTreeItemPath.HasValue())
            {
                highlightedElement = FindTreeNodeByTreePath(rootElement, selection.VisualElementTreeItemPath);
            }
        }
        

        return renderElement(rootElement,"0");

        Element renderElement(VisualElementModel model, string path)
        {
            HtmlElement element = new div();

            if (model.Tag == "i")
            {
                element = new i();
            }
            else if (model.Tag == "img")
            {
                element = new img();
            }

            element.style.Add(UserSelect(none));

            element.Add(Hover(Outline($"1px {dashed} {Blue300}")));
            
            element.id = $"{path}";
            
            element.onClick = OnItemClick;
            
            if (model.Text.HasValue())
            {
                element.text = model.Text;
            }
            
            foreach (var property in model.Properties)
            {
                var (success, name, value) = TryParsePropertyValue(property);
                if (success)
                {
                    if (name == "class")
                    {
                        element.AddClass(value);
                    }

                    if (element is img elementAsImage)
                    {
                        var isValueDouble = double.TryParse(value, out var valueAsDouble);
                        
                        if (name.Equals("h", StringComparison.OrdinalIgnoreCase) || name.Equals("height", StringComparison.OrdinalIgnoreCase))
                        {
                            if (isValueDouble)
                            {
                                elementAsImage.height = valueAsDouble+ "px";    
                            }
                            else
                            {
                                elementAsImage.height = value;
                            }
                        }
                        
                        if (name.Equals("w", StringComparison.OrdinalIgnoreCase) || name.Equals("width", StringComparison.OrdinalIgnoreCase))
                        {
                            if (isValueDouble)
                            {
                                elementAsImage.width = valueAsDouble + "px";    
                            }
                            else
                            {
                                elementAsImage.width = value;
                            }
                        }
                        
                        if (name.Equals("src", StringComparison.OrdinalIgnoreCase))
                        {
                            elementAsImage.src = Path.Combine(Context.wwwroot, value);
                        }
                    }
                    
                }
            }

            if (highlightedElement == model)
            {
                element.Add(Outline($"1px {dashed} {Blue300}"));
            }

            foreach (var styleGroup in model.StyleGroups ?? [])
            {
                foreach (var styleAttribute in styleGroup.Items ?? [])
                {
                    // try process from plugin
                    {
                        var style = TryProcessStyleAttributeByProjectConfig(styleAttribute);
                        if (style is not null)
                        {
                            element.Add(style);
                            continue;
                        }
                    }
                    
                    switch (styleAttribute)
                    {
                        case "w-full":
                        {
                            element.Add(Width("100%"));
                            continue;
                        }
                        case "w-fit":
                        {
                            element.Add(WidthFitContent);
                            continue;
                        }
                        case "h-fit":
                        {
                            element.Add(HeightFitContent);
                            continue;
                        }
                        case "size-fit":
                        {
                            element.Add(WidthFitContent);
                            element.Add(HeightFitContent);
                            continue;
                        }
                        
                        case "flex-row-centered":
                        {
                            element.Add(DisplayFlexRowCentered);
                            continue;
                        }
                        case "flex-col-centered":
                        {
                            element.Add(DisplayFlexColumnCentered);
                            continue;
                        }
                    }
                    
                    var (success, name, value) = TryParsePropertyValue(styleAttribute);
                    if (!success)
                    {
                        continue;
                    }
                    
                    var isValueDouble = double.TryParse(value, out var valueAsDouble);

                    switch (name)
                    {
                        case "top":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Top(valueAsDouble));
                                continue;
                            }
                            
                            element.Add(Top(value));
                            continue;
                        }
                        case "bottom":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Bottom(valueAsDouble));
                                continue;
                            }
                            
                            element.Add(Bottom(value));
                            continue;
                        }
                        case "left":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Left(valueAsDouble));
                                continue;
                            }
                            
                            element.Add(Left(value));
                            continue;
                        }
                        case "right":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Right(valueAsDouble));
                                continue;
                            }
                            
                            element.Add(Right(value));
                            continue;
                        }
                        
                        case "border":
                        {
                            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 3)
                            {
                                for (int i = 0; i < parts.Length; i++)
                                {
                                    if (Project.Colors.TryGetValue(parts[i], out var color))
                                    {
                                        parts[i] = color;
                                    }
                                    
                                }

                                value = string.Join(" ", parts);
                            }
                            
                            
                            element.Add(Border(value));
                            continue;
                        }
                        
                        
                        case "justify-items":
                        {
                            element.Add(JustifyItems(value));
                            continue;
                        }
                        case "justify-content":
                        {
                            element.Add(JustifyContent(value));
                            continue;
                        }
                            
                        case "align-items":
                        {
                            element.Add(AlignItems(value));
                            continue;
                        }
                        
                        case "display":
                        {
                            element.Add(Display(value));
                            continue;
                        }

                        case "background":
                        case "bg":
                        {
                            if (Project.Colors.TryGetValue(value, out var realColor))
                            {
                                value = realColor;
                            }
                            
                            element.Add(Background(value));
                            continue;
                        }
                        
                        case "font-size":
                        {
                            if (isValueDouble)
                            {
                                element.Add(FontSize(valueAsDouble));
                                continue;
                            }

                            element.Add(FontSize(value));
                            continue;
                        }
                        
                        case "w":
                        case "width":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Width(valueAsDouble));
                                continue;
                            }

                            element.Add(Width(value));
                            continue;
                        }

                        case "h":
                        case "height":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Height(valueAsDouble));
                                continue;
                            }

                            element.Add(Height(value));
                            continue;
                        }

                        case "border-radius":
                        {
                            if (isValueDouble)
                            {
                                element.Add(BorderRadius(valueAsDouble));
                                continue;
                            }

                            element.Add(BorderRadius(value));
                            continue;
                        }

                        case "gap":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Gap(valueAsDouble));
                                continue;
                            }

                            element.Add(Gap(value));
                            continue;
                        }
                        case "flex-grow":
                        {
                            if (isValueDouble)
                            {
                                element.Add(FlexGrow(valueAsDouble));
                                continue;
                            }

                            element.Add(FlexGrow(value));
                            continue;
                        }

                        case "p":
                        case "padding":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Padding(valueAsDouble));
                                continue;
                            }

                            element.Add(Padding(value));
                            continue;
                        }
                       
                        
                        case "size":
                        {
                            if (isValueDouble)
                            {
                                element.Add(Size(valueAsDouble));
                                continue;
                            }

                            element.Add(Size(value));
                            continue;
                        }
                        
                        case "color":
                        {
                            if (Project.Colors.TryGetValue(value, out var realColor))
                            {
                                value = realColor;
                            }
                            element.Add(Color(value));
                            continue;
                        }
                        
                        case "px":
                        {
                            if (isValueDouble)
                            {
                                element.Add(PaddingLeftRight(valueAsDouble));
                                continue;
                            }

                            element.Add(PaddingLeftRight(value));
                            continue;
                        }
                        case "py":
                        {
                            if (isValueDouble)
                            {
                                element.Add(PaddingTopBottom(valueAsDouble));
                                continue;
                            }

                            element.Add(PaddingTopBottom(value));
                            continue;
                        }
                        
                        case "pl":
                        case "padding-left":
                        {
                            if (isValueDouble)
                            {
                                element.Add(PaddingLeft(valueAsDouble));
                                continue;
                            }

                            element.Add(PaddingLeft(value));
                            continue;
                        }
                        
                        case "pr":
                        case "padding-right":
                        {
                            if (isValueDouble)
                            {
                                element.Add(PaddingRight(valueAsDouble));
                                continue;
                            }

                            element.Add(PaddingRight(value));
                            continue;
                        }
                        
                        case "pt":
                        case "padding-top":
                        {
                            if (isValueDouble)
                            {
                                element.Add(PaddingTop(valueAsDouble));
                                continue;
                            }

                            element.Add(PaddingTop(value));
                            continue;
                        }
                        
                        case "pb":
                        case "padding-bottom":
                        {
                            if (isValueDouble)
                            {
                                element.Add(PaddingBottom(valueAsDouble));
                                continue;
                            }

                            element.Add(PaddingBottom(value));
                            continue;
                        }
                        
                        case "ml":
                        case "margin-left":
                        {
                            if (isValueDouble)
                            {
                                element.Add(MarginLeft(valueAsDouble));
                                continue;
                            }

                            element.Add(MarginLeft(value));
                            continue;
                        }
                        
                        case "mr":
                        case "margin-right":
                        {
                            if (isValueDouble)
                            {
                                element.Add(MarginRight(valueAsDouble));
                                continue;
                            }

                            element.Add(MarginRight(value));
                            continue;
                        }
                        
                        case "mt":
                        case "margin-top":
                        {
                            if (isValueDouble)
                            {
                                element.Add(MarginTop(valueAsDouble));
                                continue;
                            }

                            element.Add(MarginTop(value));
                            continue;
                        }
                        
                        case "mb":
                        case "margin-bottom":
                        {
                            if (isValueDouble)
                            {
                                element.Add(MarginBottom(valueAsDouble));
                                continue;
                            }

                            element.Add(MarginBottom(value));
                            continue;
                        }
                        
                        case "flex-direction":
                        {
                            element.Add(FlexDirection(value));
                            continue;
                        }
                        case "z-index":
                        {
                            element.Add(ZIndex(value));
                            continue;
                        }
                        case "position":
                        {
                            element.Add(Position(value));
                            continue;
                        }
                        case "max-width":
                        {
                            if (isValueDouble)
                            {
                                element.Add(MaxWidth(valueAsDouble));
                                continue;
                            }

                            element.Add(MaxWidth(value));
                            continue;
                        }
                        case "max-height":
                        {
                            if (isValueDouble)
                            {
                                element.Add(MaxHeight(valueAsDouble));
                                continue;
                            }

                            element.Add(MaxHeight(value));
                            continue;
                        }
                        case  "border-top-left-radius":
                            {
                                element.Add(BorderTopLeftRadius(valueAsDouble));
                                continue;
                            }
                        case "border-top-right-radius":
                            {
                                element.Add(BorderTopRightRadius(valueAsDouble));
                                continue;
                            }
                        case "border-bottom-left-radius":
                            {
                                element.Add(BorderBottomLeftRadius(valueAsDouble));
                                continue;
                            }
                        case "border-bottom-right-radius":
                            {
                                element.Add(BorderBottomRightRadius(valueAsDouble));
                                continue;
                            }
                            
                        case "overflow-y":
                        {
                            element.Add(OverflowY(value));
                            continue;
                        }
                        case "overflow-x":
                        {
                            element.Add(OverflowX(value));
                            continue;
                        }
                        case "border-bottom-width":
                        {
                            if (isValueDouble)
                            {
                                element.Add(BorderBottomWidth(valueAsDouble+"px"));
                                continue;
                            }

                            element.Add(BorderBottomWidth(value));
                            continue;
                        }
                        
                    }
                }
            }

            if (model.HasNoChild())
            {
                return element;
            }

            for (var i = 0; i < model.Children.Count; i++)
            {
                var childElement = renderElement(model.Children[i], $"{path},{i}");

                element.children.Add(childElement);    
            }

            return element;
        }
    }
}
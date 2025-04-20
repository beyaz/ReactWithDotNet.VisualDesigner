using System.IO;
using System.Text;
using ReactWithDotNet.ThirdPartyLibraries.HeroUI;

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

    protected override async Task<Element> renderAsync()
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

        appState = CloneByUsingJson(appState);

        var projectId = appState.ProjectId;

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

        Element finalElement;
        {
            var renderContext = new RenderContext
            {
                ProjectId          = projectId,
                UserName           = userName,
                OnTreeItemClicked  = OnItemClick,
                ReactContext       = Context,
                HighlightedElement = highlightedElement,
                ParentModel        = null
            };
            var result = await renderElement(renderContext, rootElement, "0");
            if (result.HasError)
            {
                return new div(Size(200), Background(Gray100))
                {
                    result.Error.ToString()
                };
            }

            finalElement = result.Value;
        }

        var scaleStyle = TransformOrigin("0 0") + Transform($"scale({appState.Preview.Scale / 100})");

        return finalElement + scaleStyle;

        static async Task<Result<Element>> renderElement(RenderContext context, VisualElementModel model, string path)
        {
            HtmlElement element = new div();

            if (model.Tag == "i")
            {
                element = new i();
            }
            else if (model.Tag == "img")
            {
                element = new img { src = DummySrc(500) };
            }
            else if (model.Tag == "input")
            {
                element = new input();
            }
            else if (model.Tag == "heroui/Checkbox")
            {
                return new Checkbox();
            }

            if (model.Tag.Length > 3)
            {
                ComponentEntity component;
                {
                    var result = await GetComponenUserOrMainVersionAsync(context.ProjectId, model.Tag, context.UserName);
                    if (result.HasError)
                    {
                        return result.Error;
                    }

                    component = result.Value;
                }

                if (component is not null)
                {
                    var root = DeserializeFromJson<VisualElementModel>(component.RootElementAsJson);

                    root.Children.AddRange(model.Children);

                    return await renderElement(context with { Parent = context, ParentModel = model }, root, path);
                }
            }

            element.style.Add(UserSelect(none));

            element.Add(Hover(Outline($"1px {dashed} {Blue300}")));

            element.id = $"{path}";

            element.onClick = context.OnTreeItemClicked;

            if (model.Text.HasValue())
            {
                element.text = model.Text;

                if (context.ParentModel is not null)
                {
                    foreach (var property in model.Properties)
                    {
                        string bindPropertyValue;
                        {
                            var (success, name, value) = TryParsePropertyValue(property);
                            if (!success)
                            {
                                continue;
                            }

                            if (name != "-bind")
                            {
                                continue;
                            }

                            bindPropertyValue = value;
                        }

                        foreach (var componentProperty in context.ParentModel.Properties)
                        {
                            string name, value;
                            {
                                var result = TryParsePropertyValue(componentProperty);
                                if (!result.success)
                                {
                                    continue;
                                }

                                name  = result.name;
                                value = result.value;
                            }

                            if (ClearConnectedValue(bindPropertyValue) == $"props.{name}")
                            {
                                if (ClearConnectedValue(value).StartsWith("'"))
                                {
                                    element.text = ClearConnectedValue(value).RemoveFromStart("'").RemoveFromEnd("'");
                                }
                            }
                        }
                    }
                }
            }

            foreach (var property in model.Properties)
            {
                var (success, name, value) = TryParsePropertyValue(property);
                if (success)
                {
                    if (name == "-items-source-design-time-count")
                    {
                        var designTimeChildrenCount = double.Parse(value);

                        for (var i = 0; i < designTimeChildrenCount - 1; i++)
                        {
                            model.Children.Add(CloneByUsingJson(model.Children[0]));
                        }

                        continue;
                    }

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
                                elementAsImage.height = valueAsDouble + "px";
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

                        if (name.Equals("src", StringComparison.OrdinalIgnoreCase) && !IsConnectedValue(value))
                        {
                            if (value.StartsWith("/assets/"))
                            {
                                value = value.RemoveFromStart("/");
                            }

                            elementAsImage.src = Path.Combine(context.ReactContext.wwwroot, value);

                            continue;
                        }
                    }

                    if (element is input elementAsInput)
                    {
                        if (name.Equals("type", StringComparison.OrdinalIgnoreCase))
                        {
                            elementAsInput.type = value;
                        }
                    }
                }
            }

            if (context.HighlightedElement == model)
            {
                element.Add(Outline($"1px {dashed} {Blue300}"));
            }

            foreach (var styleGroup in model.StyleGroups ?? [])
            {
                foreach (var styleAttribute in styleGroup.Items ?? [])
                {
                    ProcessStyleAttribute(styleAttribute, element);
                }
            }

            if (model.HasNoChild())
            {
                return element;
            }

            for (var i = 0; i < model.Children.Count; i++)
            {
                Element childElement;
                {
                    var childPath = $"{path},{i}";
                    if (context.Parent is not null)
                    {
                        childPath = path;
                    }

                    var childModel = model.Children[i];
                    
                    var result = await renderElement(context, childModel, childPath);
                    if (result.HasError)
                    {
                        return result;
                    }

                    childElement = result.Value;

                    if (childModel.HideInDesigner)
                    {
                        childElement.Add(DisplayNone);
                    }
                }

                element.children.Add(childElement);
            }

            return element;
        }
    }

    static void ProcessStyleAttribute(string styleAttribute, HtmlElement element)
    {
        // try process from plugin
        {
            var style = TryProcessStyleAttributeByProjectConfig(styleAttribute);
            if (style is not null)
            {
                element.Add(style);
                return;
            }
        }

        switch (styleAttribute)
        {
            case "w-full":
            {
                element.Add(Width("100%"));
                return;
            }
            case "w-fit":
            {
                element.Add(WidthFitContent);
                return;
            }
            case "h-fit":
            {
                element.Add(HeightFitContent);
                return;
            }
            case "size-fit":
            {
                element.Add(WidthFitContent);
                element.Add(HeightFitContent);
                return;
            }

            case "flex-row-centered":
            {
                element.Add(DisplayFlexRowCentered);
                return;
            }
            case "flex-col-centered":
            {
                element.Add(DisplayFlexColumnCentered);
                return;
            }
            case "col":
            {
                element.Add(DisplayFlexColumn);
                return;
            }
            case "row":
            {
                element.Add(DisplayFlexRow);
                return;
            }
        }

        var (success, name, value) = TryParsePropertyValue(styleAttribute);
        if (!success)
        {
            return;
        }

        var isValueDouble = double.TryParse(value, out var valueAsDouble);

        switch (name)
        {
            case "min-width":
            {
                if (isValueDouble)
                {
                    element.Add(MinWidth(valueAsDouble));
                    return;
                }

                element.Add(MinWidth(value));
                return;
            }

            case "top":
            {
                if (isValueDouble)
                {
                    element.Add(Top(valueAsDouble));
                    return;
                }

                element.Add(Top(value));
                return;
            }
            case "bottom":
            {
                if (isValueDouble)
                {
                    element.Add(Bottom(valueAsDouble));
                    return;
                }

                element.Add(Bottom(value));
                return;
            }
            case "left":
            {
                if (isValueDouble)
                {
                    element.Add(Left(valueAsDouble));
                    return;
                }

                element.Add(Left(value));
                return;
            }
            case "right":
            {
                if (isValueDouble)
                {
                    element.Add(Right(valueAsDouble));
                    return;
                }

                element.Add(Right(value));
                return;
            }

            case "border":
            {
                var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (Project.Colors.TryGetValue(parts[i], out var color))
                        {
                            parts[i] = color;
                        }
                    }

                    value = string.Join(" ", parts);
                }

                element.Add(Border(value));
                return;
            }

            case "justify-items":
            {
                element.Add(JustifyItems(value));
                return;
            }
            case "justify-content":
            {
                element.Add(JustifyContent(value));
                return;
            }

            case "align-items":
            {
                element.Add(AlignItems(value));
                return;
            }

            case "display":
            {
                element.Add(Display(value));
                return;
            }

            case "background":
            case "bg":
            {
                if (Project.Colors.TryGetValue(value, out var realColor))
                {
                    value = realColor;
                }

                element.Add(Background(value));
                return;
            }

            case "font-size":
            {
                if (isValueDouble)
                {
                    element.Add(FontSize(valueAsDouble));
                    return;
                }

                element.Add(FontSize(value));
                return;
            }

            case "font-weight":
            {
                element.Add(FontWeight(value));
                return;
            }

            case "text-align":
            {
                element.Add(TextAlign(value));
                return;
            }

            case "w":
            case "width":
            {
                if (isValueDouble)
                {
                    element.Add(Width(valueAsDouble));
                    return;
                }

                element.Add(Width(value));
                return;
            }

            case "outline":
            {
                element.Add(Outline(value));
                return;
            }

            case "text-decoration":
            {
                element.Add(TextDecoration(value));
                return;
            }

            case "h":
            case "height":
            {
                if (isValueDouble)
                {
                    element.Add(Height(valueAsDouble));
                    return;
                }

                element.Add(Height(value));
                return;
            }

            case "border-radius":
            {
                if (isValueDouble)
                {
                    element.Add(BorderRadius(valueAsDouble));
                    return;
                }

                element.Add(BorderRadius(value));
                return;
            }

            case "gap":
            {
                if (isValueDouble)
                {
                    element.Add(Gap(valueAsDouble));
                    return;
                }

                element.Add(Gap(value));
                return;
            }
            case "flex-grow":
            {
                if (isValueDouble)
                {
                    element.Add(FlexGrow(valueAsDouble));
                    return;
                }

                element.Add(FlexGrow(value));
                return;
            }

            case "p":
            case "padding":
            {
                if (isValueDouble)
                {
                    element.Add(Padding(valueAsDouble));
                    return;
                }

                element.Add(Padding(value));
                return;
            }

            case "size":
            {
                if (isValueDouble)
                {
                    element.Add(Size(valueAsDouble));
                    return;
                }

                element.Add(Size(value));
                return;
            }

            case "color":
            {
                if (Project.Colors.TryGetValue(value, out var realColor))
                {
                    value = realColor;
                }

                element.Add(Color(value));
                return;
            }

            case "px":
            {
                if (isValueDouble)
                {
                    element.Add(PaddingLeftRight(valueAsDouble));
                    return;
                }

                element.Add(PaddingLeftRight(value));
                return;
            }
            case "py":
            {
                if (isValueDouble)
                {
                    element.Add(PaddingTopBottom(valueAsDouble));
                    return;
                }

                element.Add(PaddingTopBottom(value));
                return;
            }

            case "pl":
            case "padding-left":
            {
                if (isValueDouble)
                {
                    element.Add(PaddingLeft(valueAsDouble));
                    return;
                }

                element.Add(PaddingLeft(value));
                return;
            }

            case "pr":
            case "padding-right":
            {
                if (isValueDouble)
                {
                    element.Add(PaddingRight(valueAsDouble));
                    return;
                }

                element.Add(PaddingRight(value));
                return;
            }

            case "pt":
            case "padding-top":
            {
                if (isValueDouble)
                {
                    element.Add(PaddingTop(valueAsDouble));
                    return;
                }

                element.Add(PaddingTop(value));
                return;
            }

            case "pb":
            case "padding-bottom":
            {
                if (isValueDouble)
                {
                    element.Add(PaddingBottom(valueAsDouble));
                    return;
                }

                element.Add(PaddingBottom(value));
                return;
            }

            case "ml":
            case "margin-left":
            {
                if (isValueDouble)
                {
                    element.Add(MarginLeft(valueAsDouble));
                    return;
                }

                element.Add(MarginLeft(value));
                return;
            }

            case "mr":
            case "margin-right":
            {
                if (isValueDouble)
                {
                    element.Add(MarginRight(valueAsDouble));
                    return;
                }

                element.Add(MarginRight(value));
                return;
            }

            case "mt":
            case "margin-top":
            {
                if (isValueDouble)
                {
                    element.Add(MarginTop(valueAsDouble));
                    return;
                }

                element.Add(MarginTop(value));
                return;
            }

            case "mb":
            case "margin-bottom":
            {
                if (isValueDouble)
                {
                    element.Add(MarginBottom(valueAsDouble));
                    return;
                }

                element.Add(MarginBottom(value));
                return;
            }

            case "flex-direction":
            {
                element.Add(FlexDirection(value));
                return;
            }
            case "z-index":
            {
                element.Add(ZIndex(value));
                return;
            }
            case "position":
            {
                element.Add(Position(value));
                return;
            }
            case "max-width":
            {
                if (isValueDouble)
                {
                    element.Add(MaxWidth(valueAsDouble));
                    return;
                }

                element.Add(MaxWidth(value));
                return;
            }
            case "max-height":
            {
                if (isValueDouble)
                {
                    element.Add(MaxHeight(valueAsDouble));
                    return;
                }

                element.Add(MaxHeight(value));
                return;
            }
            case "border-top-left-radius":
            {
                element.Add(BorderTopLeftRadius(valueAsDouble));
                return;
            }
            case "border-top-right-radius":
            {
                element.Add(BorderTopRightRadius(valueAsDouble));
                return;
            }
            case "border-bottom-left-radius":
            {
                element.Add(BorderBottomLeftRadius(valueAsDouble));
                return;
            }
            case "border-bottom-right-radius":
            {
                element.Add(BorderBottomRightRadius(valueAsDouble));
                return;
            }

            case "overflow-y":
            {
                element.Add(OverflowY(value));
                return;
            }
            case "overflow-x":
            {
                element.Add(OverflowX(value));
                return;
            }
            case "border-bottom-width":
            {
                if (isValueDouble)
                {
                    element.Add(BorderBottomWidth(valueAsDouble + "px"));
                    return;
                }

                element.Add(BorderBottomWidth(value));
                return;
            }
        }
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

    record RenderContext
    {
        public required VisualElementModel HighlightedElement { get; init; }

        public required MouseEventHandler OnTreeItemClicked { get; init; }

        public RenderContext Parent { get; init; }

        public required VisualElementModel ParentModel { get; init; }

        public required int ProjectId { get; init; }

        public required ReactContext ReactContext { get; init; }

        public required string UserName { get; init; }
    }
}
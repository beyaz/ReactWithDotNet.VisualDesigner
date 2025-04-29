using System.IO;
using System.Text;
using ReactWithDotNet.ThirdPartyLibraries.HeroUI;

namespace ReactWithDotNet.VisualDesigner.Views;

sealed class ApplicationPreview : Component
{
    static readonly Dictionary<string, Func<StyleModifier[], StyleModifier>> ConditionMap = new()
    {
        { "hover", Hover },
        { "Focus", Focus },
        { "SM", SM },
        { "MD", MD },
        { "LG", LG },
        { "XL", XL },
        { "XXL", XXL }
    };

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
            else if (model.Tag == "svg")
            {
                element = new svg();
            }
            else if (model.Tag == "rect")
            {
                element = new rect();
            }
            else if (model.Tag == "circle")
            {
                element = new circle();
            }
            else if (model.Tag == "heroui/Checkbox")
            {
                return new Checkbox();
            }

            if (model.Tag.Length > 5)
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
                            var parseResult = TryParsePropertyValue(property);
                            if (!parseResult.success)
                            {
                                continue;
                            }

                            var name = parseResult.name;
                            var value = parseResult.value;
                            
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
                var parseResult = TryParsePropertyValue(property);
                if (parseResult.success && parseResult.value is not null)
                {
                    var name = parseResult.name;
                    var value = parseResult.value;
                    
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
                        continue;
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

                            continue;
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

                            continue;
                        }

                        if (name.Equals("src", StringComparison.OrdinalIgnoreCase))
                        {
                            if (IsConnectedValue(value))
                            {
                                continue;
                            }

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
                            continue;
                        }
                    }

                    {
                        var propertyInfo = element.GetType().GetProperty(name);
                        if (propertyInfo is not null)
                        {
                            if (propertyInfo.PropertyType == typeof(string))
                            {
                                propertyInfo.SetValue(element, value);
                                continue;
                            }

                            if (propertyInfo.PropertyType == typeof(UnionProp<string, double>))
                            {
                                propertyInfo.SetValue(element, (UnionProp<string, double>)value);
                            }
                        }
                    }
                }
            }

            foreach (var styleGroup in model.StyleGroups ?? [])
            {
                foreach (var styleAttribute in styleGroup.Items ?? [])
                {
                    var styleModifier = ConvertToStyleModifier(styleAttribute);
                    if (styleModifier is null)
                    {
                        continue;
                    }

                    if (styleGroup.Condition.HasNoValue() || styleGroup.Condition == "*")
                    {
                        element.Add(styleModifier);
                        continue;
                    }

                    if (ConditionMap.TryGetValue(styleGroup.Condition, out var fn))
                    {
                        element.Add(fn([styleModifier]));
                        continue;
                    }

                    return new Exception($"{styleGroup.Condition} not implemented yet");
                }
            }

            if (context.HighlightedElement == model)
            {
                if (element.style.outline is null)
                {
                    element.Add(Outline($"1px {dashed} {Blue300}"));
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

    static StyleModifier ConvertToStyleModifier(string styleAttribute)
    {
        // try process from plugin
        {
            var style = TryProcessStyleAttributeByProjectConfig(styleAttribute);
            if (style is not null)
            {
                return style;
            }
        }

        switch (styleAttribute)
        {
            case "w-full":
            {
                return Width("100%");
            }
            case "w-fit":
            {
                return WidthFitContent;
            }
            case "h-fit":
            {
                return HeightFitContent;
            }
            case "size-fit":
            {
                return WidthFitContent + HeightFitContent;
            }

            case "flex-row-centered":
            {
                return DisplayFlexRowCentered;
            }
            case "flex-col-centered":
            {
                return DisplayFlexColumnCentered;
            }
            case "col":
            {
                return DisplayFlexColumn;
            }
            case "row":
            {
                return DisplayFlexRow;
            }
        }

        var parseResult = TryParsePropertyValue(styleAttribute);
        if (!parseResult.success || parseResult.value is null)
        {
            return null;
        }
        
        var name = parseResult.name;
        var value = parseResult.value;

        var isValueDouble = double.TryParse(value, out var valueAsDouble);

        switch (name)
        {
            case "transform":
            {
                if (isValueDouble)
                {
                    return Transform(valueAsDouble + "deg");
                }

                return Transform(value);
            }
            case "min-width":
            {
                if (isValueDouble)
                {
                    return MinWidth(valueAsDouble);
                }

                return MinWidth(value);
            }

            case "top":
            {
                if (isValueDouble)
                {
                    return Top(valueAsDouble);
                }

                return Top(value);
            }
            case "bottom":
            {
                if (isValueDouble)
                {
                    return Bottom(valueAsDouble);
                }

                return Bottom(value);
            }
            case "left":
            {
                if (isValueDouble)
                {
                    return Left(valueAsDouble);
                }

                return Left(value);
            }
            case "right":
            {
                if (isValueDouble)
                {
                    return Right(valueAsDouble);
                }

                return Right(value);
            }

            case "border-top":
            case "border-bottom":
            case "border-left":
            case "border-right":
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

                switch (name)
                {
                    case "border-top":
                        return BorderTop(value);
                    case "border-bottom":
                        return BorderBottom(value);
                    case "border-left":
                        return BorderLeft(value);
                    case "border-right":
                        return BorderRight(value);
                    default:
                        return Border(value);
                }
            }

            case "justify-items":
            {
                return JustifyItems(value);
            }
            case "justify-content":
            {
                return JustifyContent(value);
            }

            case "align-items":
            {
                return AlignItems(value);
            }

            case "display":
            {
                return Display(value);
            }

            case "background":
            case "bg":
            {
                if (Project.Colors.TryGetValue(value, out var realColor))
                {
                    value = realColor;
                }

                return Background(value);
            }

            case "font-size":
            {
                if (isValueDouble)
                {
                    return FontSize(valueAsDouble);
                }

                return FontSize(value);
            }

            case "font-weight":
            {
                return FontWeight(value);
            }

            case "text-align":
            {
                return TextAlign(value);
            }

            case "w":
            case "width":
            {
                if (isValueDouble)
                {
                    return Width(valueAsDouble);
                }

                return Width(value);
            }

            case "outline":
            {
                return Outline(value);
            }

            case "text-decoration":
            {
                return TextDecoration(value);
            }

            case "h":
            case "height":
            {
                if (isValueDouble)
                {
                    return Height(valueAsDouble);
                }

                return Height(value);
            }

            case "border-radius":
            {
                if (isValueDouble)
                {
                    return BorderRadius(valueAsDouble);
                }

                return BorderRadius(value);
            }

            case "gap":
            {
                if (isValueDouble)
                {
                    return Gap(valueAsDouble);
                }

                return Gap(value);
            }
            case "flex-grow":
            {
                if (isValueDouble)
                {
                    return FlexGrow(valueAsDouble);
                }

                return FlexGrow(value);
            }

            case "p":
            case "padding":
            {
                if (isValueDouble)
                {
                    return Padding(valueAsDouble);
                }

                return Padding(value);
            }

            case "size":
            {
                if (isValueDouble)
                {
                    return Size(valueAsDouble);
                }

                return Size(value);
            }

            case "color":
            {
                if (Project.Colors.TryGetValue(value, out var realColor))
                {
                    value = realColor;
                }

                return Color(value);
            }

            case "px":
            {
                if (isValueDouble)
                {
                    return PaddingLeftRight(valueAsDouble);
                }

                return PaddingLeftRight(value);
            }
            case "py":
            {
                if (isValueDouble)
                {
                    return PaddingTopBottom(valueAsDouble);
                }

                return PaddingTopBottom(value);
            }

            case "pl":
            case "padding-left":
            {
                if (isValueDouble)
                {
                    return PaddingLeft(valueAsDouble);
                }

                return PaddingLeft(value);
            }

            case "pr":
            case "padding-right":
            {
                if (isValueDouble)
                {
                    return PaddingRight(valueAsDouble);
                }

                return PaddingRight(value);
            }

            case "pt":
            case "padding-top":
            {
                if (isValueDouble)
                {
                    return PaddingTop(valueAsDouble);
                }

                return PaddingTop(value);
            }

            case "pb":
            case "padding-bottom":
            {
                if (isValueDouble)
                {
                    return PaddingBottom(valueAsDouble);
                }

                return PaddingBottom(value);
            }

            case "ml":
            case "margin-left":
            {
                if (isValueDouble)
                {
                    return MarginLeft(valueAsDouble);
                }

                return MarginLeft(value);
            }

            case "mr":
            case "margin-right":
            {
                if (isValueDouble)
                {
                    return MarginRight(valueAsDouble);
                }

                return MarginRight(value);
            }

            case "mt":
            case "margin-top":
            {
                if (isValueDouble)
                {
                    return MarginTop(valueAsDouble);
                }

                return MarginTop(value);
            }

            case "mb":
            case "margin-bottom":
            {
                if (isValueDouble)
                {
                    return MarginBottom(valueAsDouble);
                }

                return MarginBottom(value);
            }

            case "flex-direction":
            {
                return FlexDirection(value);
            }
            case "z-index":
            {
                return ZIndex(value);
            }
            case "position":
            {
                return Position(value);
            }
            case "max-width":
            {
                if (isValueDouble)
                {
                    return MaxWidth(valueAsDouble);
                }

                return MaxWidth(value);
            }
            case "max-height":
            {
                if (isValueDouble)
                {
                    return MaxHeight(valueAsDouble);
                }

                return MaxHeight(value);
            }
            case "border-top-left-radius":
            {
                return BorderTopLeftRadius(valueAsDouble);
            }
            case "border-top-right-radius":
            {
                return BorderTopRightRadius(valueAsDouble);
            }
            case "border-bottom-left-radius":
            {
                return BorderBottomLeftRadius(valueAsDouble);
            }
            case "border-bottom-right-radius":
            {
                return BorderBottomRightRadius(valueAsDouble);
            }

            case "overflow-y":
            {
                return OverflowY(value);
            }
            case "overflow-x":
            {
                return OverflowX(value);
            }
            case "border-bottom-width":
            {
                if (isValueDouble)
                {
                    return BorderBottomWidth(valueAsDouble + "px");
                }

                return BorderBottomWidth(value);
            }
        }

        return null;
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
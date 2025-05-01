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


        return CssHelper.ConvertToStyleModifier(name, value).Value;
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
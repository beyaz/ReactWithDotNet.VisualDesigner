using System.IO;
using System.Reflection;
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
            else if (model.Tag == "path")
            {
                element = new path();
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
                    var root = component.RootElementAsJson.AsVisualElementModel();

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
                element.Add(model.Text);

                tryGetPropValueFromCaller(context, model, "-bind").HasValue(text => element.text = text);
            }

            foreach (var property in model.Properties)
            {
                var parseResult = TryParsePropertyValue(property);
                if (parseResult.HasValue && parseResult.Value is not null)
                {
                    var name = parseResult.Name;
                    var value = parseResult.Value;

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

                            value = TryClearStringValue(value);

                            if (value.StartsWith("https://"))
                            {
                                elementAsImage.src = value;
                                continue;
                            }

                            if (value.StartsWith("/"))
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
                        var propertyInfo = element.GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
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

            {
                var result = model.Styles
                    .Select(CreateDesignerStyleItemFromText)
                    .ConvertAll(designerItem => designerItem.ToStyleModifier())
                    .Then(styleModifiers => element.Add(styleModifiers.ToArray()));

                if (result.HasError)
                {
                    return result.Error;
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
                    if (childModel.HideInDesigner)
                    {
                        continue;
                    }

                    // check -show/hide-if
                    {
                        var hideIf = tryGetPropValueFromCaller(context, childModel, "-hide-if");
                        if (hideIf.HasValue && hideIf.Value == "true")
                        {
                            continue;
                        }

                        var showIf = tryGetPropValueFromCaller(context, childModel, "-show-if");
                        if (showIf.HasValue && showIf.Value == "false")
                        {
                            continue;
                        }
                    }

                    var result = await renderElement(context, childModel, childPath);
                    if (result.HasError)
                    {
                        return result;
                    }

                    childElement = result.Value;
                }

                element.children.Add(childElement);
            }

            return element;

            static bool hasProperty(VisualElementModel model, string propertyName)
            {
                return tryGetProperty(model, propertyName).HasValue;
            }

            static Maybe<(string propertyName, string propertyValue)> tryGetProperty(VisualElementModel model, string propertyName)
            {
                foreach (var property in model.Properties)
                {
                    var parseResult = TryParsePropertyValue(property);
                    if (!parseResult.HasValue)
                    {
                        continue;
                    }

                    var name = parseResult.Name;
                    var value = parseResult.Value;

                    if (name != propertyName)
                    {
                        continue;
                    }

                    return (name, value);
                }

                return None;
            }

            static Maybe<string> tryGetPropValueFromCaller(RenderContext context, VisualElementModel model, string propertyName)
            {
                if (context.ParentModel is null)
                {
                    return None;
                }

                string propertyValue;
                {
                    var maybe = tryGetProperty(model, propertyName);
                    if (maybe.HasNoValue)
                    {
                        return None;
                    }

                    propertyValue = maybe.Value.propertyValue;
                }

                foreach (var caller in context.ParentModel.Properties)
                {
                    string callerPropertyName, callerPropertyValue;
                    {
                        var result = TryParsePropertyValue(caller);
                        if (!result.HasValue)
                        {
                            continue;
                        }

                        callerPropertyName  = result.Name;
                        callerPropertyValue = result.Value;
                    }

                    if (ClearConnectedValue(propertyValue) == $"props.{callerPropertyName}")
                    {
                        if (ClearConnectedValue(callerPropertyValue).StartsWith("'"))
                        {
                            return ClearConnectedValue(callerPropertyValue).RemoveFromStart("'").RemoveFromEnd("'");
                        }

                        if (callerPropertyValue == "true" || callerPropertyValue == "false")
                        {
                            return callerPropertyValue;
                        }
                    }
                }

                return None;
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
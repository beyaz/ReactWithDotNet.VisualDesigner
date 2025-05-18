using System.IO;
using System.Reflection;
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

        appState = CloneByUsingYaml(appState);

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

        return new Fragment
        {
            new style
            {
                GetProjectConfig(projectId).GlobalCss
            },
            // phosphor
            new link
            {
                rel = "stylesheet", type = "text/css", href = "https://cdn.jsdelivr.net/npm/@phosphor-icons/web@2.1.1/src/regular/style.css"
            },
            new link
            {
                rel = "stylesheet", type = "text/css", href = "https://cdn.jsdelivr.net/npm/@phosphor-icons/web@2.1.1/src/fill/style.css"
            },
            finalElement + scaleStyle
        };

        static async Task<Result<Element>> renderElement(RenderContext context, VisualElementModel model, string path)
        {
            HtmlElement element = null;
            {
                TryGetHtmlElementTypeByTagName(model.Tag).HasValue(elementType => { element = (HtmlElement)Activator.CreateInstance(elementType); });
            }

            if (element is null)
            {
                if (int.TryParse(model.Tag, out var componentId))
                {
                    VisualElementModel componentRootElementModel;
                    {
                        var result = await GetComponenUserOrMainVersionAsync(componentId, context.UserName);
                        if (result.HasError)
                        {
                            return result.Error;
                        }

                        componentRootElementModel = result.Value;
                    }

                    if (componentRootElementModel is not null)
                    {
                        componentRootElementModel.Children.AddRange(model.Children);

                        return await renderElement(context with { Parent = context, ParentModel = model }, componentRootElementModel, path);
                    }
                }
            }

            if (element is null)
            {
                return new ArgumentException($"{model.Tag} is not resolved.");
            }

            element.style.Add(UserSelect(none));

            element.Add(Hover(Outline($"1px {dashed} {Blue300}")));

            element.id = $"{path}";

            element.onClick = context.OnTreeItemClicked;

            // make clever
            if (model.HasText() || model.GetDesignText().HasValue())
            {
                element.Add(TryClearStringValue(model.GetDesignText() ?? model.GetText()));

                tryGetPropValueFromCaller(context, model, Design.Text).HasValue(text => element.text = text);
            }

            foreach (var (name, value) in from p in model.Properties from x in TryParseProperty(p) where x.Name.NotIn(Design.Text, Design.DesignText) select x)
            {
                if (name == "-items-source-design-time-count")
                {
                    var firstChild = model.Children.FirstOrDefault();
                    if (firstChild is not null)
                    {
                        var designTimeChildrenCount = double.Parse(value);

                        for (var i = 0; i < designTimeChildrenCount - 1; i++)
                        {
                            model.Children.Add(CloneByUsingYaml(firstChild));
                        }
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
                        // try initialize dummy src
                        {
                            foreach (var width in model.Properties.TryGetPropertyValue("width", "w"))
                            {
                                if (int.TryParse(width, out var widthAsNumber))
                                {
                                    foreach (var height in model.Properties.TryGetPropertyValue("height", "h"))
                                    {
                                        if (int.TryParse(height, out var heightAsNumber))
                                        {
                                            elementAsImage.src = DummySrc(widthAsNumber, heightAsNumber);
                                        }
                                    }
                                }
                            }

                            if (elementAsImage.src.HasNoValue())
                            {
                                foreach (var size in model.Properties.TryGetPropertyValue("size"))
                                {
                                    if (int.TryParse(size, out var sizeAsNumber))
                                    {
                                        elementAsImage.src = DummySrc(sizeAsNumber);
                                    }
                                }
                            }
                        }

                        if (IsConnectedValue(value))
                        {
                            continue;
                        }

                        calculateSrcFromValue(context, model, value).HasValue(src => { elementAsImage.src = src; });
                        continue;

                        static Maybe<string> calculateSrcFromValue(RenderContext context, VisualElementModel model, string value)
                        {
                            var src = TryClearStringValue(value);

                            if (src.StartsWith("https://"))
                            {
                                return src;
                            }

                            if (src.StartsWith("/"))
                            {
                                var srcUnderWwwRoot = "/wwwroot" + src;

                                foreach (var localFilePath in TryFindFilePathFromWebRequestPath(srcUnderWwwRoot))
                                {
                                    if (File.Exists(localFilePath))
                                    {
                                        return srcUnderWwwRoot;
                                    }
                                }

                                return None;
                            }

                            // try find value from caller
                            foreach (var callerValue in tryGetPropValueFromCaller(context, model, "src"))
                            {
                                return calculateSrcFromValue(context, model, callerValue);
                            }

                            return None;
                        }
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
                            propertyInfo.SetValue(element, TryClearStringValue(value));
                            continue;
                        }

                        if (propertyInfo.PropertyType == typeof(UnionProp<string, double>))
                        {
                            propertyInfo.SetValue(element, (UnionProp<string, double>)value);
                        }
                    }
                }
            }

            {
                var result = model.Styles
                    .Select(x => CreateDesignerStyleItemFromText(context.ProjectId, x))
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
                        return new Exception($"Path: {childPath}", result.Error);
                    }

                    childElement = result.Value;
                }

                element.children.Add(childElement);
            }

            return element;

            static Maybe<(string propertyName, string propertyValue)> tryGetProperty(VisualElementModel model, string propertyName)
            {
                foreach (var (name, value) in from p in model.Properties from v in TryParseProperty(p) where v.Name == propertyName select v)
                {
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

                foreach (var (callerPropertyName, callerPropertyValue) in from p in context.ParentModel.Properties from v in TryParseProperty(p) select v)
                {
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
﻿using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        var highlightedElementPath = string.Empty;
        {
            if (appState.Selection.VisualElementTreeItemPathHover.HasValue())
            {
                highlightedElementPath = appState.Selection.VisualElementTreeItemPathHover;
            }

            if (appState.Selection.VisualElementTreeItemPath.HasValue())
            {
                highlightedElementPath = appState.Selection.VisualElementTreeItemPath;
            }
        }

        Element finalElement;
        {
            var renderContext = new RenderPreviewScope
            {
                ProjectId              = projectId,
                Project                = GetProjectConfig(projectId),
                UserName               = userName,
                OnTreeItemClicked      = OnItemClick,
                ReactContext           = Context,
                Client                 = Client,
                ParentModel            = null,
                HighlightedElementPath = highlightedElementPath
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
            finalElement + scaleStyle
        };

        static async Task<Result<Element>> renderElement(RenderPreviewScope scope, VisualElementModel model, string path)
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
                        var result = await GetComponentUserOrMainVersionAsync(componentId, scope.UserName);
                        if (result.HasError)
                        {
                            return result.Error;
                        }

                        componentRootElementModel = result.Value;
                    }

                    if (componentRootElementModel is not null)
                    {
                        var component = await renderElement(scope with { Parent = scope, ParentModel = model, ParentPath = path }, componentRootElementModel, path);
                        if (component.Success)
                        {
                            Plugin.BeforeComponentPreview(scope, model, component.Value);
                        }

                        return component;
                    }
                }
            }

            if (element is null)
            {
                if (model.Tag == TextNode.Tag)
                {
                    foreach (var text in tryCalculateText(scope, model))
                    {
                        return (HtmlTextNode)text;
                    }

                    return (HtmlTextNode)string.Empty;
                }
            }

            if (element is null)
            {
                return new ArgumentException($"{model.Tag} is not resolved.");
            }

            element.style.Add(UserSelect(none));

            element.id = $"{path}";

            element.onClick = scope.OnTreeItemClicked;

            foreach (var text in tryCalculateText(scope, model))
            {
                element.text = text;
            }

            model = model with
            {
                Properties = ListFrom(from p in model.Properties
                                      from x in TryParseProperty(p)
                                      where x.Name.NotIn(Design.Text, Design.TextPreview, Design.Src)
                                      select p)
            };

            while (model.Properties.Count > 0)
            {
                var propText = model.Properties[0];

                var prop = TryParseProperty(propText).Value;

                var propertyProcessScope = new PropertyProcessScope
                {
                    scope     = scope,
                    element   = element,
                    model     = model,
                    propName  = prop.Name,
                    propValue = prop.Value
                };
                var result = await processProp(propertyProcessScope);
                if (result.HasError)
                {
                    return result.Error;
                }

                model = result.Value.model;

                model = model with
                {
                    Properties = model.Properties.Where(p => p != propText).ToList()
                };
            }

            {
                var result = model.Styles
                                  .Select(x => CreateDesignerStyleItemFromText(scope.Project, x))
                                  .ConvertAll(designerItem => designerItem.ToStyleModifier())
                                  .Then(styleModifiers => element.Add(styleModifiers.ToArray()));

                if (result.HasError)
                {
                    return result.Error;
                }
            }

            // try to highlight
            if (scope.HighlightedElementPath == path)
            {
                element.id ??= Guid.NewGuid().ToString("N");

                scope.Client.RunJavascript(getJsCodeToHighlightElement(element.id ??= Guid.NewGuid().ToString("N")));
            }

            if (model.HasNoChild())
            {
                return element;
            }

            for (var i = 0; i < model.Children.Count; i++)
            {
                Element childElement;
                {
                    var childModel = model.Children[i];
                    if (childModel.HideInDesigner)
                    {
                        continue;
                    }

                    var childScope = scope;

                    var childPath = $"{path},{i}";
                    {
                        // children: props.children
                        if (childModel.Properties.Any(x => x == Design.IsImportedChild))
                        {
                            childModel = childModel with { Properties = childModel.Properties.Remove(Design.IsImportedChild) };

                            childPath = $"{scope.ParentPath},{i}";

                            childScope = scope.Parent;
                        }
                        else if (scope.Parent is not null)
                        {
                            // when any child element clicked in component we need to trigger only component
                            childPath = path;
                        }
                    }

                    // check -show/hide-if
                    {
                        var hideIf = tryGetPropValueFromCaller(scope, childModel, Design.HideIf);
                        if (hideIf.HasValue && hideIf.Value == "true")
                        {
                            continue;
                        }

                        var showIf = tryGetPropValueFromCaller(scope, childModel, Design.ShowIf);
                        if (showIf.HasValue && showIf.Value == "false")
                        {
                            continue;
                        }
                    }

                    var result = await renderElement(childScope, childModel, childPath);
                    if (result.HasError)
                    {
                        return new Exception($"Path: {childPath}", result.Error);
                    }

                    childElement = result.Value;
                }

                element.children.Add(childElement);
            }

            return element;

            static Maybe<string> tryCalculateText(RenderPreviewScope scope, VisualElementModel model)
            {
                {
                    var text = model.GetText();
                    if (IsStringValue(text))
                    {
                        return TryClearStringValue(text);
                    }
                }

                if (model.HasText() || model.GetDesignText().HasValue())
                {
                    foreach (var item in tryGetPropValueFromCaller(scope, model, Design.Text))
                    {
                        return item;
                    }

                    var text = TryClearStringValue(model.GetDesignText() ?? model.GetText());
                    if (!isUnknownValue(text))
                    {
                        return text;
                    }
                }

                return None;
            }

            static string getJsCodeToHighlightElement(string id)
            {
                var jsCode = new StringBuilder();

                jsCode.AppendLine("""

                                  function scrollIfNeededThenCall(element, callback) 
                                  {
                                    const rect = element.getBoundingClientRect();

                                    const isVisible =
                                      rect.top >= 0 &&
                                      rect.left >= 0 &&
                                      rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
                                      rect.right <= (window.innerWidth || document.documentElement.clientWidth);

                                    if (isVisible) 
                                    {
                                      callback();
                                      
                                      return;
                                    }

                                    let timeoutId;
                                    let lastScrollTop = window.scrollY;

                                    function checkScrollStopped() 
                                    {
                                      if (window.scrollY !== lastScrollTop) 
                                      {
                                        lastScrollTop = window.scrollY;
                                        clearTimeout(timeoutId);
                                        timeoutId = setTimeout(() => 
                                        {
                                          callback();
                                          window.removeEventListener("scroll", checkScrollStopped);
                                        }, 100);
                                      }
                                    }

                                    window.addEventListener("scroll", checkScrollStopped);
                                    
                                    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                                  }
                                                
                                  """);

                jsCode.AppendLine("ReactWithDotNet.OnDocumentReady(() => {");
                jsCode.AppendLine($"  const element = document.getElementById('{id}');");
                jsCode.AppendLine("  if(element)");
                jsCode.AppendLine("  {");
                jsCode.AppendLine("     scrollIfNeededThenCall(element, () => ReactWithDotNetHighlightElement(element));");
                jsCode.AppendLine("  }");
                jsCode.AppendLine("});");

                return jsCode.ToString();
            }

            static bool isUnknownValue(string value)
            {
                if (value.StartsWith("props.", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }

            static async Task<Result<PropertyProcessScope>> processProp(PropertyProcessScope data)
            {
                foreach (var propRealValue in tryGetPropValueFromCaller(data.scope, data.model, data.propName))
                {
                    data = data with { propValue = propRealValue };
                }

                data = await RunWhile(data, x => !x.IsProcessed,
                [
                    tryImportChildrenFromParentScope,
                    itemSourceDesignTimeCount,
                    tryAddClass,
                    tryProcessImage,
                    processInputType,
                    tryProcessCommonHtmlProperties
                ]);

                if (data.IsProcessed)
                {
                    return data;
                }

                if (isKnownProp(data.propName))
                {
                    return data;
                }

                if (isUnknownValue(data.propValue))
                {
                    return data;
                }

                return new Exception($"Property '{data.propName}' with value '{data.propValue}' is not processed for element '{data.model.Tag}'.");

                static bool isKnownProp(string name)
                {
                    if (name is "ref" or "key" or Design.ItemsSource or "size" or "onClick" or "onInput" or Design.ShowIf or Design.HideIf)
                    {
                        return true;
                    }

                    return false;
                }

                static PropertyProcessScope tryImportChildrenFromParentScope(PropertyProcessScope data)
                {
                    var propName = data.propName;
                    var propValue = data.propValue;
                    var scope = data.scope;

                    if (propName == "children" && propValue == "props.children" && scope.ParentModel is not null)
                    {
                        // mark children as imported
                        data = data with
                        {
                            model = data.model with
                            {
                                Children = data.model.Children.AddRange(scope.ParentModel.Children.Select(item => item with
                                {
                                    Properties = item.Properties.Add(Design.IsImportedChild)
                                }))
                            }
                        };

                        return data with { IsProcessed = true };
                    }

                    return data;
                }

                static PropertyProcessScope itemSourceDesignTimeCount(PropertyProcessScope data)
                {
                    var propName = data.propName;
                    var propValue = data.propValue;
                    var model = data.model;

                    if (propName == Design.ItemsSource)
                    {
                        var firstChild = model.Children.FirstOrDefault();
                        if (firstChild is not null)
                        {
                            var result = Try(() => JsonSerializer.Deserialize<JsonNode[]>(data.propValue));
                            if (result.HasError)
                            {
                                return data with { IsProcessed = true, model = model };
                            }

                            var arr = result.Value;

                            var designTimeChildrenCount = arr.Length;

                            model = model with
                            {
                                Children = ListFrom(Enumerable.Range(0, designTimeChildrenCount).Select(i =>
                                {
                                    var childModel = CloneByUsingYaml(firstChild);

                                    childModel = ModifyElements(childModel, _ => true, m => modifyElement(m, arr, i));

                                    return childModel;
                                })),

                                Properties = ListFrom(data.model.Properties.Where(p =>
                                {
                                    foreach (var prop in TryParseProperty(p))
                                    {
                                        if (prop.Name == Design.ItemsSourceDesignTimeCount)
                                        {
                                            return false;
                                        }
                                    }

                                    return true;
                                }))
                            };
                        }

                        return data with { IsProcessed = true, model = model };

                        static VisualElementModel modifyElement(VisualElementModel m, JsonNode[] arr, int index)
                        {
                            return m with
                            {
                                Properties = ListFrom(m.Properties.Select(p =>
                                {
                                    foreach (var (name, value) in TryParseProperty(p))
                                    {
                                        if (value.StartsWith("_item.", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var _item = arr[index];

                                            var path = value.RemoveFromStart("_item.");

                                            foreach (var realValue in JsonHelper.ReadValueAtPathFromJsonObject(_item, path))
                                            {
                                                return $"{name}: \"{realValue}\"";
                                            }
                                        }
                                    }

                                    return p;
                                }))
                            };
                        }
                    }

                    if (propName == Design.ItemsSourceDesignTimeCount)
                    {
                        var firstChild = model.Children.FirstOrDefault();
                        if (firstChild is not null)
                        {
                            var designTimeChildrenCount = double.Parse(propValue);

                            for (var i = 0; i < designTimeChildrenCount - 1; i++)
                            {
                                model = model with { Children = model.Children.Add(CloneByUsingYaml(firstChild)) };
                            }
                        }

                        return data with { IsProcessed = true, model = model };
                    }

                    return data;
                }

                static PropertyProcessScope tryAddClass(PropertyProcessScope data)
                {
                    var propName = data.propName;
                    var propValue = data.propValue;

                    if (propName == "class")
                    {
                        data.element.AddClass(propValue);
                        return data with { IsProcessed = true };
                    }

                    return data;
                }

                static async Task<PropertyProcessScope> tryProcessImage(PropertyProcessScope data)
                {
                    var propName = data.propName;
                    var propValue = data.propValue;
                    var scope = data.scope;
                    var model = data.model;
                    var element = data.element;

                    if (element is not img elementAsImage)
                    {
                        return data;
                    }

                    var isValueDouble = double.TryParse(propValue, out var valueAsDouble);

                    if (propName.Equals("h", StringComparison.OrdinalIgnoreCase) ||
                        propName.Equals("height", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isValueDouble)
                        {
                            elementAsImage.height = valueAsDouble + "px";
                        }
                        else
                        {
                            elementAsImage.height = propValue;
                        }

                        return data with { IsProcessed = true };
                    }

                    if (propName.Equals("w", StringComparison.OrdinalIgnoreCase) ||
                        propName.Equals("width", StringComparison.OrdinalIgnoreCase))
                    {
                        if (isValueDouble)
                        {
                            elementAsImage.width = valueAsDouble + "px";
                        }
                        else
                        {
                            elementAsImage.width = propValue;
                        }

                        return data with { IsProcessed = true };
                    }

                    if (propName.Equals("src", StringComparison.OrdinalIgnoreCase))
                    {
                        // try to assign value
                        {
                            foreach (var srcValue in await calculateSrcFromValue(scope, model, propValue))
                            {
                                elementAsImage.src = srcValue;
                                return data with { IsProcessed = true };
                            }
                        }

                        // try to load from design time src
                        {
                            string designTimeSrc = null;
                            {
                                foreach (var d_src in model.Properties.TryGetPropertyValue("d-src"))
                                {
                                    designTimeSrc = d_src;
                                }
                            }

                            if (designTimeSrc.HasValue())
                            {
                                foreach (var srcValue in await calculateSrcFromValue(scope, model, designTimeSrc))
                                {
                                    elementAsImage.src = srcValue;
                                    return data with { IsProcessed = true };
                                }
                            }
                        }

                        // try to initialize dummy src
                        {
                            string dummySrc = null;
                            {
                                foreach (var width in model.Properties.TryGetPropertyValue("width", "w"))
                                {
                                    if (int.TryParse(width, out var widthAsNumber))
                                    {
                                        foreach (var height in model.Properties.TryGetPropertyValue("height", "h"))
                                        {
                                            if (int.TryParse(height, out var heightAsNumber))
                                            {
                                                dummySrc = DummySrc(widthAsNumber, heightAsNumber);
                                            }
                                        }
                                    }
                                }

                                if (dummySrc.HasNoValue())
                                {
                                    foreach (var size in model.Properties.TryGetPropertyValue("size"))
                                    {
                                        if (int.TryParse(size, out var sizeAsNumber))
                                        {
                                            dummySrc = DummySrc(sizeAsNumber);
                                        }
                                    }
                                }
                            }

                            if (dummySrc.HasValue())
                            {
                                elementAsImage.src = dummySrc;

                                return data with { IsProcessed = true };
                            }
                        }

                        elementAsImage.src = DummySrc(500);

                        return data with { IsProcessed = true };

                        static async Task<Maybe<string>> calculateSrcFromValue(RenderPreviewScope scope, VisualElementModel model, string value)
                        {
                            var src = TryClearStringValue(value);

                            if (src.StartsWith("https://"))
                            {
                                return src;
                            }

                            if (src.StartsWith("/"))
                            {
                                var srcUnderWwwRoot = "/wwwroot" + src;

                                foreach (var localFilePath in await TryFindFilePathFromWebRequestPath(srcUnderWwwRoot))
                                {
                                    if (File.Exists(localFilePath))
                                    {
                                        return srcUnderWwwRoot;
                                    }
                                }

                                return None;
                            }

                            // try to find value from caller
                            foreach (var callerValue in tryGetPropValueFromCaller(scope, model, "src"))
                            {
                                return await calculateSrcFromValue(scope, model, callerValue);
                            }

                            return None;
                        }
                    }

                    return data;
                }

                static PropertyProcessScope processInputType(PropertyProcessScope data)
                {
                    var propName = data.propName;
                    var propValue = data.propValue;
                    var element = data.element;

                    if (element is input elementAsInput)
                    {
                        if (propName.Equals("type", StringComparison.OrdinalIgnoreCase))
                        {
                            elementAsInput.type = TryClearStringValue(propValue);
                            return data with { IsProcessed = true };
                        }
                    }

                    return data;
                }

                static PropertyProcessScope tryProcessCommonHtmlProperties(PropertyProcessScope data)
                {
                    var propName = data.propName;
                    var propValue = data.propValue;
                    var element = data.element;

                    var propertyInfo = element.GetType().GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo is null)
                    {
                        return data;
                    }

                    if (isUnknownValue(propValue))
                    {
                        return data with { IsProcessed = true };
                    }

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        propertyInfo.SetValue(element, TryClearStringValue(propValue));
                        return data with { IsProcessed = true };
                    }

                    if (propertyInfo.PropertyType == typeof(dangerouslySetInnerHTML))
                    {
                        propertyInfo.SetValue(element, new dangerouslySetInnerHTML(TryClearStringValue(propValue)));
                        return data with { IsProcessed = true };
                    }

                    if (propertyInfo.PropertyType == typeof(UnionProp<string, double>))
                    {
                        propertyInfo.SetValue(element, (UnionProp<string, double>)propValue);
                        return data with { IsProcessed = true };
                    }

                    return data;
                }
            }

            static Maybe<string> tryGetPropValueFromCaller(RenderPreviewScope scope, VisualElementModel model, string propertyName)
            {
                if (scope.ParentModel is null)
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

                foreach (var (callerPropertyName, callerPropertyValue) in from p in scope.ParentModel.Properties from v in TryParseProperty(p) select v)
                {
                    if (ClearConnectedValue(propertyValue) == $"props.{callerPropertyName}")
                    {
                        if (IsStringValue(ClearConnectedValue(callerPropertyValue)))
                        {
                            return TryClearStringValue(ClearConnectedValue(callerPropertyValue));
                        }

                        if (IsRawStringValue(ClearConnectedValue(callerPropertyValue)))
                        {
                            return TryClearRawStringValue(ClearConnectedValue(callerPropertyValue));
                        }

                        if (callerPropertyValue == "true" || callerPropertyValue == "false")
                        {
                            return callerPropertyValue;
                        }

                        if (IsJsonArray(callerPropertyValue))
                        {
                            return callerPropertyValue;
                        }

                        foreach (var _ in TryParseDouble(callerPropertyValue))
                        {
                            return callerPropertyValue;
                        }
                    }
                }

                return None;

                static Maybe<(string propertyName, string propertyValue)> tryGetProperty(VisualElementModel model, string propertyName)
                {
                    foreach (var (name, value) in from p in model.Properties from v in TryParseProperty(p) where v.Name == propertyName select v)
                    {
                        return (name, value);
                    }

                    return None;
                }
            }
        }
    }

    [StopPropagation]
    [SkipRender]
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
}

sealed record RenderPreviewScope
{
    public Client Client { get; init; }

    public required MouseEventHandler OnTreeItemClicked { get; init; }

    public RenderPreviewScope Parent { get; init; }

    public required VisualElementModel ParentModel { get; init; }

    public string ParentPath { get; init; }

    public ProjectConfig Project { get; init; }

    public required int ProjectId { get; init; }

    public required ReactContext ReactContext { get; init; }

    public required string UserName { get; init; }

    public string HighlightedElementPath { get; init; }
}

sealed record PropertyProcessScope
{
    public HtmlElement element { get; init; }
    public VisualElementModel model { get; init; }
    public string propName { get; init; }
    public string propValue { get; init; }
    public RenderPreviewScope scope { get; init; }

    public bool IsProcessed { get; init; }
}

static class JsonHelper
{
    public static Maybe<object> ReadValueAtPathFromJsonObject(JsonNode obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
        {
            return None;
        }

        var pathSegments = propertyPath.Split('.');
        var currentNode = obj;

        foreach (var segment in pathSegments)
        {
            if (currentNode is JsonObject jsonObject && jsonObject.TryGetPropertyValue(segment, out var value))
            {
                currentNode = value;
            }
            else
            {
                return None;
            }
        }

        return currentNode;
    }
}
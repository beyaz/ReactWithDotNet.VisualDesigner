using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Dapper;
using Google.Protobuf.Reflection;
using Microsoft.Data.SqlClient;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Configuration;
using ReactWithDotNet.VisualDesigner.Exporters;

namespace ReactWithDotNet.VisualDesigner;

sealed record PropSuggestionScope
{
    public ComponentEntity Component { get; init; }

    public Maybe<ComponentEntity> SelectedComponent { get; init; }
    public string TagName { get; init; }
}

static class Plugin
{
    const string BOA_MessagingByGroupName = "BOA.MessagingByGroupName";

    public static ConfigModel AfterReadConfig(ConfigModel config)
    {
        if (Environment.MachineName.StartsWith("BTARC", StringComparison.OrdinalIgnoreCase))
        {
            return config with
            {
                Database = new()
                {
                    IsSQLServer      = true,
                    SchemaName       = "RVD",
                    ConnectionString = @"Data Source=srvdev\atlas;Initial Catalog=boa;Min Pool Size=10; Max Pool Size=100;Application Name=Thriller;Integrated Security=true; TrustServerCertificate=true;"
                }
            };
        }

        return config;
    }

    public static ReactNode AnalyzeNode(ReactNode node)
    {
        foreach (var analyzeFunc in from x in Components.AllTypes where x.analyzeReactNode is not null select x.analyzeReactNode)
        {
            node = analyzeFunc(node);
        }

        return node;
    }

    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }

    public static async Task<IReadOnlyList<string>> GetPropSuggestions(PropSuggestionScope scope)
    {
        if (scope.TagName.HasNoValue())
        {
            return [];
        }

        return await Cache.AccessValue($"{nameof(Plugin)}-{scope.TagName}", () => calculate(scope));

        static async Task<IReadOnlyList<string>> calculate(PropSuggestionScope scope)
        {
            var collectionSuggestions = new List<string>();
            
            var stringSuggestions = new List<string>();
            {
                if (scope.Component.GetConfig().TryGetValue(BOA_MessagingByGroupName, out var messagingGroupName))
                {
                    foreach (var item in await GetMessagingByGroupName(messagingGroupName))
                    {
                        stringSuggestions.Add(item.Description);
                        stringSuggestions.Add($"${item.PropertyName}$ {item.Description}");
                    }
                }
            }

            List<string> numberSuggestions =
            [
                "2",
                "4",
                "8",
                "12",
                "16",
                "24"
            ];

            List<string> dateSuggestions =
            [
                "new Date().getDate()"
            ];

            List<string> booleanSuggestions =
            [
                "true",
                "false"
            ];

            List<string> remoteApiMethodNames = [];
            
            foreach (var (key, value) in scope.Component.GetConfig())
            {
                const string dotNetVariable = "DotNetVariable.";

                if (!key.StartsWith(dotNetVariable, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var variableName = key.RemoveFromStart(dotNetVariable);

                var dotnetTypeFullName = value;

                var assemblyFilePath = getAssemblyFilePathByFullTypeName(dotnetTypeFullName);
                if (assemblyFilePath is null)
                {
                    continue;
                }

                stringSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsString));
                numberSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsNumber));
                dateSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsDateTime));
                booleanSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsBoolean));
                collectionSuggestions.AddRange(CecilHelper.GetPropertyPathList(assemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsCollection));

                remoteApiMethodNames.AddRange(CecilHelper.GetRemoteApiMethodNames(assemblyFilePath, dotnetTypeFullName));
            }

            List<string> returnList = [];
            
            List<(string name, string value)> distinctSuggestions = [];

            foreach (var prop in from m in Components.GetAllTypesMetadata() where m.TagName == scope.TagName from p in m.Props select p)
            {
                switch (prop.ValueType)
                {
                    case JsType.String:
                    {
                        foreach (var item in stringSuggestions)
                        {
                            if (item.StartsWith("request."))
                            {
                                addSuggestion(prop.Name, ConvertDotNetPathToJsPath(item));
                                continue;
                            }

                            addSuggestion(prop.Name, '"'+item+'"');
                        }

                        break;
                    }
                    case JsType.Number:
                    {
                        foreach (var item in numberSuggestions)
                        {
                            addSuggestion(prop.Name, item);
                        }

                        break;
                    }
                    case JsType.Date:
                    {
                        foreach (var item in dateSuggestions)
                        {
                            addSuggestion(prop.Name, item);
                        }

                        break;
                    }
                    case JsType.Boolean:
                    {
                        foreach (var item in booleanSuggestions)
                        {
                            addSuggestion(prop.Name, item);
                        }

                        break;
                    }
                    case JsType.Array:
                    {
                        foreach (var item in collectionSuggestions)
                        {
                            addSuggestion(prop.Name, item);
                        }

                        break;
                    }
                    
                    case JsType.Function:
                    {
                        foreach (var remoteApiMethodName in remoteApiMethodNames)
                        {
                            addSuggestion(prop.Name, $"callRemoteApi('{remoteApiMethodName}')");
                        }

                        break;
                    }
                }
            }

            foreach (var item in stringSuggestions)
            {
                addSuggestion("d-text", '"' + item + '"');
            }

            foreach (var (name, value) in Components.GetPropSuggestions(scope.TagName))
            {
                addSuggestion(name, value);
            }

            returnList.InsertRange(0, distinctSuggestions.Select(x => x.name + ": " + x.value));
            
            return returnList;

            void addSuggestion(string name, string value)
            {
                if (!distinctSuggestions.Any(x => name.Equals(x.name, StringComparison.OrdinalIgnoreCase)))
                {
                    distinctSuggestions.Add((name, value));
                    return;
                }

                returnList.Add($"{name}: {value}");
            }

            static string getAssemblyFilePathByFullTypeName(string fullTypeName)
            {
                if (fullTypeName.StartsWith("BOA.InternetBanking.Payments.API", StringComparison.OrdinalIgnoreCase))
                {
                    const string projectBinDirectoryPath = @"D:\work\BOA.BusinessModules\Dev\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\";

                    return projectBinDirectoryPath + "BOA.InternetBanking.Payments.API.dll";
                }

                if (fullTypeName.StartsWith("BOA.POSPortal.MobilePos.API.", StringComparison.OrdinalIgnoreCase))
                {
                    const string projectBinDirectoryPath = @"D:\work\BOA.BusinessModules\Dev\BOA.MobilePos\API\BOA.POSPortal.MobilePos.API\bin\Debug\net8.0\";

                    return projectBinDirectoryPath + "BOA.POSPortal.MobilePos.API.dll";
                }

                return null;
            }
        }
    }

    public static IReadOnlyList<string> GetTagSuggestions()
    {
        return Components.AllTypes.Select(x => x.type.Name).ToList();
    }

    public static bool IsImage(object component)
    {
        return component is Components.Image;
    }

    public static Element TryCreateElementForPreview(string tag, string id, MouseEventHandler onMouseClick)
    {
        var type = Components.AllTypes.Select(x => x.type).FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return null;
        }

        var component = (Element)Activator.CreateInstance(type);

        if (component is PluginComponentBase componentBase)
        {
            componentBase.id           = id;
            componentBase.onMouseClick = onMouseClick;
        }

        return component;
    }

    public static Element TryGetIconForElementTreeNode(VisualElementModel node)
    {
        if (node.Tag == nameof(Components.BDigitalGroupView))
        {
            return new Icons.Panel();
        }

        if (node.Tag == nameof(Components.Link))
        {
            return new IconLink();
        }

        if (node.Tag == nameof(Components.Image) || node.Tag == nameof(Components.BIcon))
        {
            return new IconImage();
        }

        if (node.Tag is nameof(Components.BDigitalGrid))
        {
            foreach (var p in node.Properties)
            {
                foreach (var (name, value) in TryParseProperty(p))
                {
                    if (name == "direction")
                    {
                        if (TryClearStringValue(value).Contains("column", StringComparison.OrdinalIgnoreCase))
                        {
                            return new IconFlexColumn();
                        }

                        if (TryClearStringValue(value).Contains("row", StringComparison.OrdinalIgnoreCase))
                        {
                            return new IconFlexRow();
                        }
                    }
                }
            }

            return new IconFlexRow();
        }

        return null;
    }

    static string ConvertDotNetPathToJsPath(string dotNetPath)
    {
        if (string.IsNullOrEmpty(dotNetPath))
        {
            return dotNetPath;
        }

        var camelCase = new StringBuilder();
        var capitalizeNext = false;

        foreach (var c in dotNetPath)
        {
            if (c == '.')
            {
                capitalizeNext = true;
                camelCase.Append('.');
            }
            else
            {
                if (capitalizeNext)
                {
                    camelCase.Append(char.ToLower(c, CultureInfo.InvariantCulture));
                    capitalizeNext = false;
                }
                else
                {
                    camelCase.Append(c);
                }
            }
        }

        return camelCase.ToString();
    }

    static Task<IReadOnlyList<MessagingInfo>> GetMessagingByGroupName(string messagingGroupName)
    {
        var cacheKey = $"{nameof(GetMessagingByGroupName)} :: {messagingGroupName}";

        return Cache.AccessValue(cacheKey, async () => await getMessagingByGroupName(messagingGroupName));

        static async Task<IReadOnlyList<MessagingInfo>> getMessagingByGroupName(string messagingGroupName)
        {
            var returnList = new List<MessagingInfo>();

            const string connectionString = @"Data Source=srvdev\atlas;Initial Catalog=boa;Min Pool Size=10; Max Pool Size=100;Application Name=Thriller;Integrated Security=true; TrustServerCertificate=true;";

            using IDbConnection connection = new SqlConnection(connectionString);

            const string sql =
                """
                    SELECT m.PropertyName, Description
                      FROM COR.MessagingDetail AS d WITH(NOLOCK)
                INNER JOIN COR.Messaging       AS m WITH(NOLOCK) ON d.Code = m.Code
                INNER JOIN COR.MessagingGroup  AS g WITH(NOLOCK) ON g.MessagingGroupId = m.MessagingGroupId
                     WHERE g.Name = @messagingGroupName
                       AND d.LanguageId = 1
                """;

            var reader = await connection.ExecuteReaderAsync(sql, new { messagingGroupName });

            while (reader.Read())
            {
                var propertyName = reader["PropertyName"].ToString();
                var description = reader["Description"].ToString();

                returnList.Add(new() { PropertyName = propertyName, Description = description });
            }

            reader.Close();

            return returnList;
        }
    }

    static class Components
    {
        public static readonly IReadOnlyList<(Type type, Func<IReadOnlyList<(string name, string value)>> propSuggestions, Func<ReactNode, ReactNode> analyzeReactNode)> AllTypes =
        [
            // NextJsSupport
            (typeof(Image), Image.GetPropSuggestions, null),
            (typeof(Link), Link.GetPropSuggestions, null),

            (typeof(BTypography), BTypography.GetPropSuggestions, null),
            (typeof(BDigitalGrid), BDigitalGrid.GetPropSuggestions, null),
            (typeof(BasePage), BasePage.GetPropSuggestions, null),
            (typeof(TransactionWizardPage), TransactionWizardPage.GetPropSuggestions, null),
            (typeof(BRadioButtonGroup), BRadioButtonGroup.GetPropSuggestions, null),
            (typeof(BDigitalGroupView), BDigitalGroupView.GetPropSuggestions, null),
            (typeof(BDigitalBox), BDigitalBox.GetPropSuggestions, null),
            (typeof(BAlert), BAlert.GetPropSuggestions, null),
            (typeof(BIcon), BIcon.GetPropSuggestions, null),
            (typeof(BDigitalMoneyInput), BDigitalMoneyInput.GetPropSuggestions, null),
            (typeof(BComboBox), BComboBox.GetPropSuggestions, BComboBox.AnalyzeReactNode),
            (typeof(BDigitalDatepicker), BDigitalDatepicker.GetPropSuggestions, null),
            (typeof(BInput), BInput.GetPropSuggestions, BInput.AnalyzeReactNode),
            (typeof(BInputMaskExtended), BInputMaskExtended.GetPropSuggestions, BInputMaskExtended.AnalyzeReactNode),
            (typeof(BPlateNumber), BPlateNumber.GetPropSuggestions, null),
            (typeof(BCheckBox), BCheckBox.GetPropSuggestions, BCheckBox.AnalyzeReactNode),
            (typeof(BButton), BButton.GetPropSuggestions, null),
            (typeof(BDigitalPlateNumber), BDigitalPlateNumber.GetPropSuggestions, null),
            (typeof(BDigitalDialog), BDigitalDialog.GetPropSuggestions, null),
            (typeof(BDigitalTabNavigator), BDigitalTabNavigator.GetPropSuggestions, null)
        ];

        public static IReadOnlyList<ComponentMeta> GetAllTypesMetadata()
        {
            return AllTypes.Select(x => x.type).Select(createFrom).ToList();

            static ComponentMeta createFrom(Type type)
            {
                return new()
                {
                    TagName = type.Name,
                    Props   = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(createPropMetaFrom).ToList()
                };

                static PropMeta createPropMetaFrom(PropertyInfo propertyInfo)
                {
                    return new()
                    {
                        Name      = propertyInfo.Name,
                        ValueType = getValueType(propertyInfo)
                    };

                    static JsType getValueType(PropertyInfo propertyInfo )
                    {
                        var jsTypeInfoAttribute = propertyInfo.GetCustomAttribute<JsTypeInfoAttribute>();
                        if (jsTypeInfoAttribute is not null)
                        {
                            return jsTypeInfoAttribute.JsType;
                        }

                        
                        
                        
                        Type propertyType = propertyInfo.PropertyType;
                        if (propertyType == typeof(string))
                        {
                            return JsType.String;
                        }

                        if (propertyType.In(typeof(short), typeof(short?), typeof(int), typeof(int?), typeof(double), typeof(double?), typeof(long), typeof(long?)))
                        {
                            return JsType.Number;
                        }

                        if (propertyType.In(typeof(bool), typeof(bool?)))
                        {
                            return JsType.Boolean;
                        }

                        if (propertyType.In(typeof(DateTime), typeof(DateTime?)))
                        {
                            return JsType.Date;
                        }

                        if (propertyType == typeof(IEnumerable) || typeof(IEnumerable).IsSubclassOf(propertyType))
                        {
                            return JsType.Array;
                        }

                        throw new NotImplementedException(propertyType.FullName);
                    }
                }
            }
        }

        public static IReadOnlyList<(string name, string value)> GetPropSuggestions(string tag)
        {
            var type = AllTypes.Select(x => x.type).FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (type is null)
            {
                return [];
            }

            var methodInfo = type.GetMethod(nameof(GetPropSuggestions), BindingFlags.Static | BindingFlags.Public);
            if (methodInfo is null)
            {
                return [];
            }

            return (IReadOnlyList<(string name, string value)>)methodInfo.Invoke(null, []);
        }

        public sealed class BDigitalGrid : PluginComponentBase
        {
            public string alignItems { get; set; }
            public bool? container { get; set; }

            public string direction { get; set; }

            public bool? item { get; set; }

            public string justifyContent { get; set; }

            public int? lg { get; set; }

            public int? md { get; set; }

            public int? sm { get; set; }

            public int? spacing { get; set; }

            public int? xl { get; set; }

            public int? xs { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                    (nameof(container), "true"),
                    (nameof(item), "true"),
                    (nameof(direction),'"' +"column" + '"'),
                    (nameof(direction),'"' +"row" + '"'),
                    (nameof(justifyContent),'"' +"flex-start" + '"'),
                    (nameof(justifyContent),'"' +"center" + '"'),
                    (nameof(justifyContent),'"' +"flex-end" + '"'),
                    (nameof(justifyContent),'"' +"space-between" + '"'),
                    (nameof(justifyContent),'"' +"space-around" + '"'),
                    (nameof(justifyContent),'"' +"space-evenly" + '"'),
                    (nameof(alignItems),'"' +"flex-start" + '"'),
                    (nameof(alignItems),'"' +"stretch" + '"'),
                    (nameof(alignItems),'"' +"flex-end" + '"'),
                    (nameof(alignItems),'"' +"center" + '"'),
                    (nameof(alignItems),'"' +"baseline" + '"'),
                   
                    (nameof(spacing),"1"),
                    (nameof(spacing),"2"),
                    (nameof(spacing),"3"),
                    (nameof(spacing),"4"),
                    (nameof(spacing),"5"),  
                    (nameof(spacing),"6")
                ];
            }

            protected override Element render()
            {
                return new Grid
                {
                    children = { children },

                    container      = container,
                    item           = item,
                    direction      = direction,
                    justifyContent = justifyContent,
                    alignItems     = alignItems,
                    spacing        = spacing,
                    xs             = xs,
                    sm             = sm,
                    md             = md,
                    lg             = lg,
                    xl             = xl,

                    id      = id,
                    onClick = onMouseClick
                };
            }
        }

        public sealed class BDigitalGroupView : PluginComponentBase
        {
            public string title { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                
                ];
            }

            protected override Element render()
            {
                return new FlexColumn(Background(White), BorderRadius(8), Border(1, solid, Gray200), Padding(16), Id(id), OnClick(onMouseClick))
                {
                    children =
                    {
                        title is null ? null : new div { title },
                        children
                    }
                };
            }
        }

        public sealed class BIcon : PluginComponentBase
        {
            public string name { get; set; }

            public string size { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                    (nameof(name),"'TimerRounded'"),
                    (nameof(name),"'content_copy'")
                ];
            }

            protected override Element render()
            {
                return new FlexRowCentered(Size(GetSize()), Id(id), OnClick(onMouseClick))
                {
                    createSvg
                };
            }

            Element createSvg()
            {
                if (name == "TimerRounded")
                {
                    return new svg(ViewBox(0, 0, 24, 24), Fill("currentColor"), svg.Size(GetSize()))
                    {
                        new path
                        {
                            d = "M15.07 1H8.93c-.52 0-.93.41-.93.93s.41.93.93.93h6.14c.52 0 .93-.41.93-.93S15.59 1 15.07 1Zm-2.15 9.45V7.5c0-.28-.22-.5-.5-.5s-.5.22-.5.5v3.5c0 .13.05.26.15.35l2.5 2.5c.2.2.51.2.71 0 .2-.2.2-.51 0-.71l-2.36-2.36ZM12 4C7.59 4 4 7.59 4 12s3.59 8 8 8 8-3.59 8-8c0-1.9-.66-3.63-1.76-5.01l1.29-1.29c.2-.2.2-.51 0-.71s-.51-.2-.71 0l-1.3 1.3C16.63 5.21 14.39 4 12 4Zm0 14c-3.31 0-6-2.69-6-6s2.69-6 6-6 6 2.69 6 6-2.69 6-6 6Z"
                        }
                    };
                }

                if (name == "content_copy")
                {
                    return new svg(Fill("currentColor"), ViewBox(0, 0, 24, 24), svg.Size(GetSize()))
                    {
                        new path
                        {
                            d = "M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12V1Zm3 4H8c-1.1 0-2 .9-2 2v16h14c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2Zm0 18H8V7h11v16Z"
                        }
                    };
                }

                return name;
            }

            double GetSize()
            {
                if (size.HasValue())
                {
                    if (double.TryParse(size, out var d))
                    {
                        return d;
                    }
                }

                return 24;
            }
        }

        public sealed class Image : PluginComponentBase
        {
            public string alt { get; set; }

            public string className { get; set; }

            public bool? fill { get; set; }

            public string height { get; set; }
            public string src { get; set; }

            public string width { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                return new img
                {
                    src       = src,
                    alt       = alt,
                    width     = width,
                    height    = height,
                    className = className
                } + When(fill is true, SizeFull);
            }
        }

        public sealed class Link : PluginComponentBase
        {
            public string className { get; set; }
            public string href { get; set; }
            public string target { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                return new a
                {
                    href      = href,
                    target    = target,
                    className = className
                };
            }
        }

        sealed class BAlert : PluginComponentBase
        {
            public string severity { get; set; }
            public string variant { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                    (nameof(variant), "'standard'"),
                    (nameof(severity), "'info'")
                ];
            }

            protected override Element render()
            {
                return new Alert
                {
                    id      = id,
                    onClick = onMouseClick,

                    severity = severity,
                    variant  = variant,

                    children = { children }
                };
            }
        }

        sealed class BasePage : PluginComponentBase
        {
            public string pageTitle { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
              
                ];
            }

            protected override Element render()
            {
                return new FlexColumn(WidthFull, Padding(16), Background("#fafafa"))
                {
                    children =
                    {
                        new h6(FontWeight500, FontSize20, PaddingTop(32), PaddingBottom(24)) { pageTitle },
                        children
                    }
                } + Id(id) + OnClick(onMouseClick);
            }
        }

        sealed class TransactionWizardPage : PluginComponentBase
        {
            public bool? isWide { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                return new FlexColumn(WidthFull, Padding(16), Background("#fafafa"))
                {
                    children =
                    {
                        children
                    }
                } + Id(id) + OnClick(onMouseClick);
            }
        }

        sealed class BRadioButtonGroup : PluginComponentBase
        {
            public string items { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                if (items.HasNoValue())
                {
                    return null;
                }

                var itemList = JsonSerializer.Deserialize<ItemModel[]>(items);

                return new FlexRow(Gap(24))
                {
                    itemList.Select(x => new FlexRowCentered(Gap(12), WidthFitContent)
                    {
                        new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Fill(rgb(22, 160, 133)))
                        {
                            new path { d = "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zm0-5C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8z" }
                        },

                        new label
                        {
                            FontSize16, FontWeight400, LineHeight(1.5), FontFamily("Roboto, sans-serif"),
                            x.label
                        }
                    })
                } + Id(id) + OnClick(onMouseClick);
            }

            class ItemModel
            {
                public string label { get; set; }
            }
        }

        sealed class BDigitalTabNavigator : PluginComponentBase
        {
            public string items { get; set; }
            public string mainResource { get; set; }

            public int? selectedTab { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                if (items.HasNoValue())
                {
                    return null;
                }

                var itemList = JsonSerializer.Deserialize<ItemModel[]>(items);

                return new FlexRow(BorderBottom(1, solid, rgba(0, 0, 0, 0.12)), Color(rgb(22, 160, 133)))
                {
                    new FlexRow(Gap(24))
                    {
                        itemList.Select(x => new FlexRowCentered(Padding(24), WidthFitContent, AlignItemsCenter)
                        {
                            BorderBottom(2, solid, rgb(22, 160, 133)),

                            new label
                            {
                                FontSize16, FontWeight400, LineHeight(1.5), FontFamily("Roboto, sans-serif"),
                                x.label
                            }
                        })
                    }
                } + Id(id) + OnClick(onMouseClick);
            }

            class ItemModel
            {
                public string label { get; set; }
            }
        }

        sealed class BCheckBox : PluginComponentBase
        {
            [JsTypeInfo(JsType.Boolean)]
            public string @checked { get; set; }

            [JsTypeInfo(JsType.String)]
            public string label { get; set; }
            
            [JsTypeInfo(JsType.Function)]
            public string onCheck { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {

                var svgForIsCheckedFalse = new svg(ViewBox(0,0,24,24), Fill(rgb(22, 160, 133)))
                {
                    new path { d = "M19 5v14H5V5h14m0-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z" }
                };
                
                var svgForIsCheckedTrue = new svg(ViewBox(0,0,24,24), Fill(rgb(22, 160, 133)))
                {
                    new path { d = "M19 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.11 0 2-.9 2-2V5c0-1.1-.89-2-2-2zm-9 14l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" }
                };
                
                return new FlexRowCentered(Gap(12), WidthFitContent)
                {
                    new FlexRowCentered(Size(24))
                    {
                        @checked == "true" ? svgForIsCheckedTrue : svgForIsCheckedFalse
                    },
                    new div { label ?? "?" },
                    
                    Id(id),
                    OnClick(onMouseClick)
                };
            }
            
            public static ReactNode AnalyzeReactNode(ReactNode node)
            {
                if (node.Tag == nameof(BCheckBox))
                {
                    var checkedProp = node.Properties.FirstOrDefault(x => x.Name == nameof(@checked));
                    var onCheckProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onCheck));

                    if (checkedProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(e: any, checked: boolean) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{checkedProp.Value.RemoveFromStart("request.")} = checked; }});"
                        ];

                        if (onCheckProp is not null)
                        {
                            if (IsAlphaNumeric(onCheckProp.Value))
                            {
                                lines.Add(onCheckProp.Value + "(e, checked);");
                            }
                            else
                            {
                                lines.Add(onCheckProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (onCheckProp is not null)
                        {
                            onCheckProp = onCheckProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == onCheckProp.Name), onCheckProp);
                        }
                        else
                        {
                            properties = properties.Add(new ReactProperty
                            {
                                Name  = "onCheck",
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }

                    node = AddContextProp(node);
                }

                return node with { Children = node.Children.Select(AnalyzeReactNode).ToImmutableList() };
            }
        }

        static ReactNode AddContextProp(ReactNode node)
        {
            if (node.Properties.Any(p=>p.Name == "context"))
            {
                return node;
            }
            
            return node with
            {
                Properties = node.Properties.Add(new ReactProperty
                {
                    Name  = "context",
                    Value = "context"
                })
            };
        }

        sealed class BButton : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string text { get; set; }
            
            [JsTypeInfo(JsType.Function)]
            public string onClick { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                return new FlexRowCentered(WidthFitContent, Background("Blue"), BorderRadius(10), Padding(5, 15), BorderColor(rgb(230, 245, 243)), Color(White))
                {
                    new div { text ?? "?" },
                    
                    Id(id),
                    OnClick(onMouseClick)
                };
            }
        }

        sealed class BDigitalDatepicker : PluginComponentBase
        {
            public string bind { get; set; }

            public IEnumerable dataSource { get; set; }

            public bool? hiddenClearButton { get; set; }

            public string hintText { get; set; }
            public string labelText { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                var textContent = string.Empty;
                if (labelText.HasValue())
                {
                    textContent = labelText;
                }

                if (bind.HasValue())
                {
                    textContent += " | " + bind;
                }

                return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
                {
                    Id(id), OnClick(onMouseClick),

                    new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                        {
                            textContent
                        },
                        new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Color(rgb(117, 117, 117)))
                        {
                            new path
                            {
                                d    = "M7 10l5 5 5-5z",
                                fill = "#757575"
                            }
                        }
                    },
                    new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
                    {
                        //new div{ helperText},
                        //new div{ maxLength }
                    }
                };
            }
        }

        sealed class BDigitalBox : PluginComponentBase
        {
            public string styleContext { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                    (nameof(styleContext), "'noMargin'")
                ];
            }

            protected override Element render()
            {
                return new Grid
                {
                    children = { children }
                };
            }
        }

        sealed class BDigitalDialog : PluginComponentBase
        {
            public string actions { get; set; }
            public string content { get; set; }

            public bool? open { get; set; }

            public string title { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                if (actions.HasNoValue())
                {
                    return null;
                }

                var actionList = JsonSerializer.Deserialize<ItemModel[]>(actions);

                return new div(Background(rgba(0, 0, 0, 0.5)), Padding(24), BorderRadius(8))
                {
                    new div(Background("white"), BorderRadius(8), Padding(16), FontFamily("Roboto, sans-serif"))
                    {
                        new FlexRow(JustifyContentSpaceBetween, AlignItemsCenter, PaddingY(16))
                        {
                            new div(FontSize20) { title },
                            new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24))
                            {
                                new path
                                {
                                    d = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"
                                }
                            }
                        },

                        new Alert
                        {
                            content
                        },
                        SpaceY(12),
                        new FlexRow(JustifyContentFlexEnd, Color(rgb(22, 160, 133)), FontWeightBold, FontSize14, Gap(24))
                        {
                            actionList.Select(x => new div
                            {
                                x.label
                            })
                        }
                    }
                };
            }

            class ItemModel
            {
                public string label { get; set; }
            }
        }

        sealed class BDigitalMoneyInput : PluginComponentBase
        {
            public string bind { get; set; }

            public bool? currencyVisible { get; set; }
            public string fec { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                var textContent = "Tutar";

                if (bind.HasValue())
                {
                    textContent += " | " + bind;
                }

                return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
                {
                    new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                        {
                            textContent
                        },

                        new div { fec ?? "TL" },
                    },
                    new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
                    {
                        //new div{ helperText},
                        //new div{ maxLength }
                    },

                    Id(id), OnClick(onMouseClick)
                };
            }
        }

        sealed class BDigitalPlateNumber : PluginComponentBase
        {
            public string bind { get; set; }
            public string label { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                return new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                {
                    new div { bind ?? "?" }
                };
            }
        }

        sealed class BInput : PluginComponentBase
        {
            [JsTypeInfo(JsType.Boolean)]
            public string isAutoComplete { get; set; }

            [JsTypeInfo(JsType.String)]
            public string floatingLabelText { get; set; }

            [JsTypeInfo(JsType.String)]
            public string helperText { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string maxLength { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onChange { get; set; }
            
            [JsTypeInfo(JsType.Boolean)]
            public string isRequired { get; set; }

            [JsTypeInfo(JsType.String)]
            public string value { get; set; }

            public static ReactNode AnalyzeReactNode(ReactNode node)
            {
                if (node.Tag == nameof(BInput))
                {
                    var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
                    var onChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onChange));
                    var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
                    var isAutoCompleteProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isAutoComplete));

                    if (valueProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(e: any, value: any) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = value; }});"
                        ];

                        if (onChangeProp is not null)
                        {
                            if (IsAlphaNumeric(onChangeProp.Value))
                            {
                                lines.Add(onChangeProp.Value + "(e, value);");
                            }
                            else
                            {
                                lines.Add(onChangeProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (onChangeProp is not null)
                        {
                            onChangeProp = onChangeProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == onChangeProp.Name), onChangeProp);
                        }
                        else
                        {
                            properties = properties.Add(new ReactProperty
                            {
                                Name  = "onChange",
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }

                    if (isRequiredProp is not null && isAutoCompleteProp is not null)
                    {
                        var autoCompleteFinalValue = string.Empty;
                        {
                            if ("true".Equals(isAutoCompleteProp.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                autoCompleteFinalValue = "'on'";
                            }
                            else if ("false".Equals(isAutoCompleteProp.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                autoCompleteFinalValue = "'off'";
                            }
                            else
                            {
                                autoCompleteFinalValue = $"{ConvertDotNetPathToJsPath(isAutoCompleteProp.Value)} ? \"on\" : \"off\" }}";
                            }
                        }
                       
                        
                        node = node with
                        {
                            Properties = node.Properties.Remove(isRequiredProp).Remove(isAutoCompleteProp).Add(new ReactProperty
                            {
                                Name  = "valueConstraint",
                                Value = $"{{ required: {ConvertDotNetPathToJsPath(isRequiredProp.Value)}, autoComplete: {autoCompleteFinalValue} }}"
                            })
                        };
                    }

                    node = AddContextProp(node);
                }

                return node with { Children = node.Children.Select(AnalyzeReactNode).ToImmutableList() };
            }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                 
                ];
            }

            protected override Element render()
            {
                var textContent = string.Empty;
                if (floatingLabelText.HasValue())
                {
                    textContent = floatingLabelText;
                }

                if (value.HasValue())
                {
                    textContent += " | " + value;
                }

                return new div(PaddingTop(16), PaddingBottom(8))
                {
                    new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif")) { textContent },

                        Id(id), OnClick(onMouseClick)
                    },
                    new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
                    {
                        new div { helperText },
                        new div { maxLength }
                    }
                };
            }
        }

        sealed class BComboBox : PluginComponentBase
        {

            [JsTypeInfo(JsType.Array)]
            public string dataSource { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string  hiddenClearButton { get; set; }

            [JsTypeInfo(JsType.String)]
            public string hintText { get; set; }
            
            [JsTypeInfo(JsType.String)]
            public string labelText { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onSelect { get; set; }
            
            [JsTypeInfo(JsType.String)]
            public string value { get; set; }

            public static ReactNode AnalyzeReactNode(ReactNode node)
            {
                if (node.Tag == nameof(BComboBox))
                {
                    var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
                    var onSelectProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onSelect));

                    if (valueProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(selectedIndexes: [number], selectedItems: [TextValuePair], selectedValues: [string]) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = selectedValues[0]; }});"
                        ];

                        if (onSelectProp is not null)
                        {
                            if (IsAlphaNumeric(onSelectProp.Value))
                            {
                                lines.Add(onSelectProp.Value + "(selectedIndexes, selectedItems, selectedValues);");
                            }
                            else
                            {
                                lines.Add(onSelectProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (onSelectProp is not null)
                        {
                            onSelectProp = onSelectProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == onSelectProp.Name), onSelectProp);
                        }
                        else
                        {
                            properties = properties.Add(new ReactProperty
                            {
                                Name  = "onSelectProp",
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }
                }

                return node with { Children = node.Children.Select(AnalyzeReactNode).ToImmutableList() };
            }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                var textContent = string.Empty;
                if (labelText.HasValue())
                {
                    textContent = labelText;
                }

                if (value.HasValue())
                {
                    textContent += " | " + value;
                }

                return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
                {
                    Id(id), OnClick(onMouseClick),

                    new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                        {
                            textContent
                        },
                        new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Color(rgb(117, 117, 117)))
                        {
                            new path
                            {
                                d    = "M7 10l5 5 5-5z",
                                fill = "#757575"
                            }
                        }
                    },
                    new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
                    {
                        //new div{ helperText},
                        //new div{ maxLength }
                    }
                };
            }
        }

        sealed class BInputMaskExtended : PluginComponentBase
        {
            
            [JsTypeInfo(JsType.Array)]
            public string mask { get; set; }
            
            [JsTypeInfo(JsType.Boolean)]
            public string isAutoComplete { get; set; }
            
            [JsTypeInfo(JsType.Boolean)]
            public string isReadonly { get; set; }

            [JsTypeInfo(JsType.String)]
            public string value { get; set; }
            
            [JsTypeInfo(JsType.String)]
            public string floatingLabelText { get; set; }

            [JsTypeInfo(JsType.String)]
            public string helperText { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string maxLength { get; set; }
            
            [JsTypeInfo(JsType.Function)]
            public string onChange { get; set; }
            
            [JsTypeInfo(JsType.Boolean)]
            public string isRequired { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
              
                ];
            }

            protected override Element render()
            {
                var textContent = string.Empty;
                if (floatingLabelText.HasValue())
                {
                    textContent = floatingLabelText;
                }

                if (value.HasValue())
                {
                    textContent += " | " + value;
                }

                return new div(PaddingTop(16), PaddingBottom(8))
                {
                    new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif")) { textContent },

                        Id(id), OnClick(onMouseClick)
                    },
                    new FlexRow(JustifyContentSpaceBetween, FontSize12, PaddingLeftRight(14), Color(rgb(158, 158, 158)), LineHeight15)
                    {
                        new div { helperText },
                        new div { maxLength }
                    }
                };
            }
            
            public static ReactNode AnalyzeReactNode(ReactNode node)
            {
                if (node.Tag == nameof(BInputMaskExtended))
                {
                    var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
                    var onChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onChange));
                    var isRequiredProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isRequired));
                    var isAutoCompleteProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isAutoComplete));

                    if (valueProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(e: any, value: any) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = value; }});"
                        ];

                        if (onChangeProp is not null)
                        {
                            if (IsAlphaNumeric(onChangeProp.Value))
                            {
                                lines.Add(onChangeProp.Value + "(e, value);");
                            }
                            else
                            {
                                lines.Add(onChangeProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (onChangeProp is not null)
                        {
                            onChangeProp = onChangeProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == onChangeProp.Name), onChangeProp);
                        }
                        else
                        {
                            properties = properties.Add(new ReactProperty
                            {
                                Name  = "onChange",
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }

                    if (isRequiredProp is not null && isAutoCompleteProp is not null)
                    {
                        var autoCompleteFinalValue = string.Empty;
                        {
                            if ("true".Equals(isAutoCompleteProp.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                autoCompleteFinalValue = "'on'";
                            }
                            else if ("false".Equals(isAutoCompleteProp.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                autoCompleteFinalValue = "'off'";
                            }
                            else
                            {
                                autoCompleteFinalValue = $"{ConvertDotNetPathToJsPath(isAutoCompleteProp.Value)} ? \"on\" : \"off\" }}";
                            }
                        }
                       
                        
                        node = node with
                        {
                            Properties = node.Properties.Remove(isRequiredProp).Remove(isAutoCompleteProp).Add(new ReactProperty
                            {
                                Name  = "valueConstraint",
                                Value = $"{{ required: {ConvertDotNetPathToJsPath(isRequiredProp.Value)}, autoComplete: {autoCompleteFinalValue} }}"
                            })
                        };
                    }

                    node = AddContextProp(node);
                }

                return node with { Children = node.Children.Select(AnalyzeReactNode).ToImmutableList() };
            }
        }

        sealed class BPlateNumber : PluginComponentBase
        {
            public string bind { get; set; }

            public string label { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                ];
            }

            protected override Element render()
            {
                var textContent = string.Empty;
                if (label.HasValue())
                {
                    textContent = label;
                }

                if (bind.HasValue())
                {
                    textContent += " | " + bind;
                }

                return new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                {
                    new div { textContent },

                    Id(id), OnClick(onMouseClick)
                };
            }
        }

        sealed class BTypography : PluginComponentBase
        {
            public string dangerouslySetInnerHTML { get; set; }
            public string variant { get; set; }

            public static IReadOnlyList<(string name, string value)> GetPropSuggestions()
            {
                return
                [
                    (nameof(variant), '"'+"h1"+'"'),
                    (nameof(variant), '"'+"h2"+'"'),
                    (nameof(variant), '"'+"h3"+'"'),
                    (nameof(variant), '"'+"h4"+'"'),
                    (nameof(variant), '"'+"h5"+'"'),
                    (nameof(variant), '"'+"h6"+'"'),
                    
                    (nameof(variant), '"'+"body0"+'"'),
                    (nameof(variant), '"'+"body1"+'"')
                ];
            }

            protected override Element render()
            {
                return new Typography
                {
                    children = { children },
                    variant  = variant,

                    style =
                    {
                        FontFamily("Roboto, sans-serif"), FontWeight400, LineHeight27
                    },

                    id      = id,
                    onClick = onMouseClick,
                    //TODO: Open: dangerouslySetInnerHTML = dangerouslySetInnerHTML
                };
            }
        }
    }

    static class Icons
    {
        public sealed class Panel : PureComponent
        {
            protected override Element render()
            {
                return new svg(ViewBox(0, 0, 16, 16), Fill(none), svg.Size(16))
                {
                    new rect
                    {
                        x              = 1,
                        y              = 2,
                        width          = 14,
                        height         = 12,
                        rx             = 1,
                        stroke         = "currentColor",
                        strokeWidth    = 1,
                        strokeLinecap  = "round",
                        strokeLinejoin = "round"
                    },
                    new line
                    {
                        x1             = 1,
                        y1             = 4.5,
                        x2             = 15,
                        y2             = 4.5,
                        stroke         = "currentColor",
                        strokeWidth    = 1,
                        strokeLinecap  = "round",
                        strokeLinejoin = "round"
                    },
                    new rect
                    {
                        x      = 3,
                        y      = 7,
                        width  = 10,
                        height = 1,
                        fill   = "currentColor"
                    },
                    new rect
                    {
                        x      = 3,
                        y      = 9,
                        width  = 6,
                        height = 1,
                        fill   = "currentColor"
                    }
                };
            }
        }
    }

    record ComponentMeta
    {
        public IReadOnlyList<PropMeta> Props { get; init; }

        public string TagName { get; init; }
    }

    record PropMeta
    {
        public string Name { get; init; }

        public JsType ValueType { get; init; }
    }

    record MessagingInfo
    {
        public string Description { get; init; }
        public string PropertyName { get; init; }
    }
}
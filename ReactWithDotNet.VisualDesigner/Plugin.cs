using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
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
    public static string AnalyzeExportFilePath(string exportFilePathForComponent)
    {
        var names = exportFilePathForComponent.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 2 && names[0].StartsWith("BOA."))
        {
            // project folder is d:\work\
            // we need to calculate rest of path

            // sample: /BOA.InternetBanking.MoneyTransfers/x-form.tsx

            var solutionName = names[0];

            var pagesFolderPath = string.Empty;
            {
                pagesFolderPath = $@"D:\work\BOA.BusinessModules\Dev\{solutionName}\OBAWeb\OBA.Web.{solutionName.RemoveFromStart("BOA.")}\ClientApp\pages\";
                if (solutionName == "BOA.MobilePos")
                {
                    pagesFolderPath = @"D:\work\BOA.BusinessModules\Dev\BOA.MobilePos\OBAWeb\OBA.Web.POSPortal.MobilePos\ClientApp\pages\";
                }
            }

            var fileName = names[1];

            if (Directory.Exists(pagesFolderPath))
            {
                return Path.Combine(pagesFolderPath, fileName);
            }
        }

        return exportFilePathForComponent;
    }

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

    public static IEnumerable<Type> GetAllCustomComponents()
    {
        return
            from t in typeof(Plugin).Assembly.GetTypes()
            where t.GetCustomAttribute<CustomComponentAttribute>() is not null
            select t;
    }

    static IEnumerable<MethodInfo> GetAnalyzeMethods(Type type)
    {
        return from m in type.GetMethods(BindingFlags.Static | BindingFlags.Public) select m;
    }

    public static ReactNode AnalyzeNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
    {
        foreach (var analyzeMethodInfo in
                 from t in GetAllCustomComponents()
                 from m in GetAnalyzeMethods(t)
                 select m)
        {
            node = (ReactNode)analyzeMethodInfo.Invoke(null, [node, componentConfig]);
        }

        return node;
    }

    public static IEnumerable<string> CalculateImportLines(ReactNode node)
    {
        var lines = new List<string>();

        foreach (var type in GetAllCustomComponents())
        {
            lines.AddRange(tryGetImportLines(type, node));
        }

        foreach (var child in node.Children)
        {
            lines.AddRange(CalculateImportLines(child));
        }

        return lines.Distinct();

        static IEnumerable<string> tryGetImportLines(Type type, ReactNode node)
        {
            if (type.Name == node.Tag)
            {
                return from a in type.GetCustomAttributes<ImportAttribute>()
                    select $"import {{ {a.Name} }} from \"{a.Package}\";";
            }

            return [];
        }
    }

    public static Element BeforeComponentPreview(RenderPreviewScope scope, VisualElementModel visualElementModel, Element component)
    {
        return component;
    }

    static IEnumerable<(string variableName, string dotNetAssemblyFilePath, string dotnetTypeFullName)> GetDotNetVariables(ComponentEntity componentEntity)
    {
        return GetDotNetVariables(componentEntity.GetConfig());
    }

    static IEnumerable<(string variableName, string dotNetAssemblyFilePath, string dotnetTypeFullName)> GetDotNetVariables(IReadOnlyDictionary<string, string> componentConfig)
    {
        foreach (var (key, value) in componentConfig)
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

            yield return (variableName, assemblyFilePath, dotnetTypeFullName);
        }

        yield break;

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

            foreach (var (variableName, dotNetAssemblyFilePath, dotnetTypeFullName) in GetDotNetVariables(scope.Component))
            {
                stringSuggestions.AddRange(CecilHelper.GetPropertyPathList(dotNetAssemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsString));
                numberSuggestions.AddRange(CecilHelper.GetPropertyPathList(dotNetAssemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsNumber));
                dateSuggestions.AddRange(CecilHelper.GetPropertyPathList(dotNetAssemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsDateTime));
                booleanSuggestions.AddRange(CecilHelper.GetPropertyPathList(dotNetAssemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsBoolean));
                collectionSuggestions.AddRange(CecilHelper.GetPropertyPathList(dotNetAssemblyFilePath, dotnetTypeFullName, $"{variableName}.", CecilHelper.IsCollection));

                remoteApiMethodNames.AddRange(CecilHelper.GetRemoteApiMethodNames(dotNetAssemblyFilePath, dotnetTypeFullName));
            }

            List<string> returnList = [];

            List<(string name, string value)> distinctSuggestions = [];

            foreach (var (name, value) in Components.GetPropSuggestions(scope.TagName))
            {
                addSuggestion(name, value);
            }

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

                            addSuggestion(prop.Name, '"' + item + '"');
                        }

                        break;
                    }
                    case JsType.Number:
                    {
                        foreach (var item in numberSuggestions)
                        {
                            if (item.StartsWith("request."))
                            {
                                addSuggestion(prop.Name, ConvertDotNetPathToJsPath(item));
                                continue;
                            }

                            addSuggestion(prop.Name, item);
                        }

                        break;
                    }
                    case JsType.Date:
                    {
                        foreach (var item in dateSuggestions)
                        {
                            if (item.StartsWith("request."))
                            {
                                addSuggestion(prop.Name, ConvertDotNetPathToJsPath(item));
                                continue;
                            }

                            addSuggestion(prop.Name, item);
                        }

                        break;
                    }
                    case JsType.Boolean:
                    {
                        foreach (var item in booleanSuggestions)
                        {
                            if (item.StartsWith("request."))
                            {
                                addSuggestion(prop.Name, ConvertDotNetPathToJsPath(item));
                                continue;
                            }

                            addSuggestion(prop.Name, item);
                        }

                        break;
                    }
                    case JsType.Array:
                    {
                        foreach (var item in collectionSuggestions)
                        {
                            if (item.StartsWith("request."))
                            {
                                addSuggestion(prop.Name, ConvertDotNetPathToJsPath(item));
                                continue;
                            }

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
                addSuggestion(Design.Text, '"' + item + '"');
            }

            foreach (var item in booleanSuggestions)
            {
                if (item.StartsWith("request."))
                {
                    addSuggestion(Design.ShowIf, ConvertDotNetPathToJsPath(item));
                    addSuggestion(Design.HideIf, ConvertDotNetPathToJsPath(item));
                    continue;
                }

                addSuggestion(Design.ShowIf, item);
                addSuggestion(Design.HideIf, item);
            }

            foreach (var item in collectionSuggestions)
            {
                if (item.StartsWith("request."))
                {
                    addSuggestion(Design.ItemsSource, ConvertDotNetPathToJsPath(item));
                    continue;
                }

                addSuggestion(Design.ItemsSource, item);
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
        }
    }

    public static IReadOnlyList<string> GetTagSuggestions()
    {
        return GetAllCustomComponents().Select(x => x.Name).ToList();
    }

    public static bool IsImage(object component)
    {
        return component is Components.Image;
    }

    public static Element TryCreateElementForPreview(string tag, string id, MouseEventHandler onMouseClick)
    {
        var type = GetAllCustomComponents().FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
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
                foreach (var property in ParseProperty(p))
                {
                    if (property.Name == "direction")
                    {
                        if (TryClearStringValue(property.Value).Contains("column", StringComparison.OrdinalIgnoreCase))
                        {
                            return new IconFlexColumn();
                        }

                        if (TryClearStringValue(property.Value).Contains("row", StringComparison.OrdinalIgnoreCase))
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
        public static IReadOnlyList<ComponentMeta> GetAllTypesMetadata()
        {
            return GetAllCustomComponents().Select(createFrom).ToList();

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

                    static JsType getValueType(PropertyInfo propertyInfo)
                    {
                        var jsTypeInfoAttribute = propertyInfo.GetCustomAttribute<JsTypeInfoAttribute>();
                        if (jsTypeInfoAttribute is not null)
                        {
                            return jsTypeInfoAttribute.JsType;
                        }

                        var propertyType = propertyInfo.PropertyType;
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

        public static IEnumerable<(string name, string value)> GetPropSuggestions(string tag)
        {
            var type = GetAllCustomComponents().FirstOrDefault(t => t.Name.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (type is null)
            {
                yield break;
            }

            foreach (var item in
                     from p in type.GetProperties()
                     from a in p.GetCustomAttributes<SuggestionsAttribute>()
                     from s in a.Suggestions
                     select (p.Name, s))
            {
                yield return item;
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalGrid", Package = "b-digital-grid")]
        public sealed class BDigitalGrid : PluginComponentBase
        {
            [Suggestions("flex-start , flex-end , stretch , center , baseline")]
            [JsTypeInfo(JsType.String)]
            public string alignItems { get; set; }

            [Suggestions("true")]
            [JsTypeInfo(JsType.Boolean)]
            public string container { get; set; }

            [Suggestions("column , row")]
            [JsTypeInfo(JsType.String)]
            public string direction { get; set; }

            [Suggestions("true")]
            [JsTypeInfo(JsType.Boolean)]
            public string item { get; set; }

            [Suggestions("flex-start , center , flex-end , space-between , space-around , space-evenly")]
            [JsTypeInfo(JsType.String)]
            public string justifyContent { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string lg { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string md { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string sm { get; set; }

            [Suggestions("1 , 2 , 3 , 4 , 5 , 6")]
            [JsTypeInfo(JsType.Number)]
            public string spacing { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string xl { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string xs { get; set; }

            protected override Element render()
            {
                return new Grid
                {
                    children = { children },

                    container      = container == null ? null : Convert.ToBoolean(container),
                    item           = item == null ? null : Convert.ToBoolean(item),
                    direction      = direction,
                    justifyContent = justifyContent,
                    alignItems     = alignItems,
                    spacing        = spacing == null ? null : Convert.ToDouble(spacing),
                    xs             = xs == null ? null : Convert.ToInt32(xs),
                    sm             = sm == null ? null : Convert.ToInt32(sm),
                    md             = md == null ? null : Convert.ToInt32(md),
                    lg             = lg == null ? null : Convert.ToInt32(lg),
                    xl             = xl == null ? null : Convert.ToInt32(xl),

                    id      = id,
                    onClick = onMouseClick
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalGroupView", Package = "b-digital-group-view")]
        public sealed class BDigitalGroupView : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string title { get; set; }

            protected override Element render()
            {
                return new FlexColumn(Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(24), Id(id), OnClick(onMouseClick))
                {
                    children =
                    {
                        title is null ? null : new div { title },
                        children
                    }
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalTransactionConfirm", Package = "b-digital-transaction-confirm")]
        public sealed class BDigitalTransactionConfirm : PluginComponentBase
        {
            // @formatter:on

            protected override Element render()
            {
                return new Fragment
                {
                    new div(Id(id), OnClick(onMouseClick))
                    {
                        children
                    }
                };

                //var leftFlow = new FlexColumnCentered(Width(24), Gap(12))
                //{
                //    new div { Background(rgb(22, 160, 133)), Padding(6), BorderRadius(8) },
                //    new div { Width(0), Border(1, dashed, rgba(0, 0, 0, 0.12)), Height(80) },
                //    new DynamicMuiIcon { name = "ArrowDownwardOutlined", fontSize = "small" },
                //    new div { Width(0), Border(1, dashed, rgba(0, 0, 0, 0.12)), Height(80) },
                //    new div { Background(rgb(22, 160, 133)), Padding(6), BorderRadius(8) },
                //};

                //var partContent = new FlexColumn(JustifyContentSpaceBetween)
                //{
                //    // TOP
                //    new FlexColumn
                //    {
                //        children.Count > 0 ? children[0] : null,

                //        new FlexRow(AlignItemsCenter, Gap(4))
                //        {
                //            children.Count > 0 ? children[1] : null,

                //            new BTypography
                //            {
                //                children ={sender_item1_text + (sender_item1_value.HasValue() ? ":" : null)},
                //                variant  = sender_item1_text_variant,
                //                color    = sender_item1_text_color
                //            },
                //            new BTypography
                //            {
                //                children ={sender_item1_value},
                //                variant  = sender_item1_value_variant,
                //                color    = sender_item1_value_color
                //            }
                //        }
                //    },

                //    // CENTER
                //    new div{ "Amount"},

                //    // BOTTOM
                //    new FlexColumn
                //    {
                //        new BTypography
                //        {
                //            children ={receiver_title},
                //            variant  = receiver_title_variant,
                //            color    = receiver_title_color
                //        },

                //        new FlexRow
                //        {
                //            new BTypography
                //            {
                //                children ={sender_item1_text},
                //                variant  = sender_item1_text_variant,
                //                color    = sender_item1_text_color
                //            },
                //            new BTypography
                //            {
                //                children ={sender_item1_value},
                //                variant  = sender_item1_value_variant,
                //                color    = sender_item1_value_color
                //            }
                //        }
                //    }
                //};

                //return new FlexColumn(Background(White), BorderRadius(10), Border(1, solid, rgba(0, 0, 0, 0.12)), Padding(24), Id(id), OnClick(onMouseClick))
                //{
                //    new FlexRow(Gap(32), Height(368))
                //    {
                //        leftFlow,  partContent
                //    },

                //    new FlexRow(BorderTop(1, solid, rgba(0, 0, 0, 0.12)))
                //    {

                //    }
                //};
                /* <BDigitalTransactionConfirm
                                              senderData={{
                                                  titleText: getMessage("MoneyTransferSenderLabel"),
                                                  item1: { value: getMessage("GeneralKuveytTurkBankNameLabel"), valueVariant: "body1" },
                                                  item2: { text: getMessage("GeneralAccountNo2Label"), textVariant: "body2", value: model.selectedAccount.fullAccountNumber, valueVariant: "body1" },
                                                  item3: { text: `${getMessage("GeneralBalanceLabel")}:`, textVariant: "body2", value: `${BLocalization.formatMoney(model.selectedAccount.balance)} ${model.selectedAccount.fecCode}`, valueVariant: "body1" }
                                              }}
                                              receiverData={{
                                                  titleText: getMessage("MoneyTransferReceiverLabel"),
                                                  item1: { value: getMessage("FindeksLabel"), valueVariant: "body1" },
                                                  item2: { text: `${getMessage("GeneralTransactionLabel")}:`, textVariant: "body2", value: getMessage("FindeksRiskReport"), valueVariant: "body1" }
                                              }}
                                              transferAmount={`${BLocalization.formatMoney(model.amount)} ${model.fecCode}`}
                                              transferAmountVariant="h6"
                                              transactionDetailList={[
                                                  {
                                                      text: getMessage("GeneralTransactionDate2Label"),
                                                      textVariant: "body2",
                                                      value: BLocalization.formatDateTime(model.transactionDate, Constant.Format.DateCommon),
                                                      valueVariant: "body1",
                                                  }
                                              ]}
                                          />
                                      </BDigitalBox>*/
            }
        }

        static ReactNode AddContextProp(ReactNode node)
        {
            if (node.Properties.Any(p => p.Name == "context"))
            {
                return node;
            }

            return node with
            {
                Properties = node.Properties.Add(new()
                {
                    Name  = "context",
                    Value = "context"
                })
            };
        }

        [CustomComponent]
        [Import(Name = "BIconExtended as BIcon", Package = "../utils/FormAssistant")]
        public sealed class BIcon : PluginComponentBase
        {
            [Suggestions("TimerRounded , content_copy")]
            [JsTypeInfo(JsType.String)]
            public string name { get; set; }

            [JsTypeInfo(JsType.String)]
            public string size { get; set; }

            protected override Element render()
            {
                return new FlexRowCentered(Size(GetSize()), Id(id), OnClick(onMouseClick))
                {
                    createSvg
                };
            }

            Element createSvg()
            {
                return new DynamicMuiIcon
                {
                    name     = name,
                    fontSize = "medium"
                };
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

        [CustomComponent]
        public sealed class Image : PluginComponentBase
        {
            public string alt { get; set; }

            public string className { get; set; }

            public bool? fill { get; set; }

            public string height { get; set; }
            public string src { get; set; }

            public string width { get; set; }

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

        [CustomComponent]
        public sealed class Link : PluginComponentBase
        {
            public string className { get; set; }
            public string href { get; set; }
            public string target { get; set; }

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

        [CustomComponent]
        [Import(Name = "BAlert", Package = "b-core-alert")]
        sealed class BAlert : PluginComponentBase
        {
            [Suggestions("success , info , warning , error")]
            [JsTypeInfo(JsType.String)]
            public string severity { get; set; }

            [Suggestions("standard , outlined , filled")]
            [JsTypeInfo(JsType.String)]
            public string variant { get; set; }

            protected override Element render()
            {
                return new div
                {
                    Id(id), OnClick(onMouseClick),

                    new Alert
                    {
                        severity = severity,
                        variant  = variant,

                        children = { children }
                    }
                };
            }
        }

        [CustomComponent]
        sealed class BasePage : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string pageTitle { get; set; }

            protected override Element render()
            {
                return new FlexColumn(FontFamily("Roboto"), WidthFull, Padding(16), Background("#fafafa"))
                {
                    children =
                    {
                        new h6(FontWeight500, FontSize20, PaddingTop(32), PaddingBottom(24)) { pageTitle },
                        children
                    }
                } + Id(id) + OnClick(onMouseClick);
            }
        }

        [CustomComponent]
        [Import(Name = "BButton", Package = "b-button")]
        sealed class BButton : PluginComponentBase
        {
            [JsTypeInfo(JsType.Function)]
            public string onClick { get; set; }

            [JsTypeInfo(JsType.String)]
            public string text { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BButton))
                {
                    var onClickProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onClick));

                    if (onClickProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "() =>",
                            "{"
                        ];

                        if (IsAlphaNumeric(onClickProp.Value))
                        {
                            lines.Add(onClickProp.Value + "();");
                        }
                        else
                        {
                            lines.Add(onClickProp.Value);
                        }

                        lines.Add("}");

                        onClickProp = onClickProp with
                        {
                            Value = string.Join(Environment.NewLine, lines)
                        };

                        properties = properties.SetItem(properties.FindIndex(x => x.Name == onClickProp.Name), onClickProp);

                        node = node with { Properties = properties };
                    }
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
            }

            protected override Element render()
            {
                return new FlexRowCentered(Background(rgba(22, 160, 133, 1)), BorderRadius(10), PaddingY(8), PaddingX(64), MinWidth(160))
                {
                    FontSize(15),
                    LineHeight26,
                    LetterSpacing("0.46px"),
                    Color(rgba(248, 249, 250, 1)),

                    new div { text ?? "?" },

                    Id(id),
                    OnClick(onMouseClick)
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BCheckBox", Package = "b-check-box")]
        sealed class BCheckBox : PluginComponentBase
        {
            [JsTypeInfo(JsType.Boolean)]
            public string @checked { get; set; }

            [JsTypeInfo(JsType.String)]
            public new string id { get; set; }

            [JsTypeInfo(JsType.String)]
            public string label { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onCheck { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BCheckBox))
                {
                    var checkedProp = node.Properties.FirstOrDefault(x => x.Name == nameof(@checked));
                    var onCheckProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onCheck));

                    if (checkedProp is not null)
                    {
                        var properties = node.Properties;

                        var requestAssignmentLine = string.Empty;
                        if (checkedProp.Value.StartsWith("request.", StringComparison.OrdinalIgnoreCase))
                        {
                            requestAssignmentLine = $"  updateRequest(r => {{ r.{checkedProp.Value.RemoveFromStart("request.")} = checked; }});";
                        }

                        List<string> lines =
                        [
                            "(e: any, checked: boolean) =>",
                            "{",
                            requestAssignmentLine
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
                            properties = properties.Add(new()
                            {
                                Name  = "onCheck",
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }

                    node = AddContextProp(node);
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
            }

            protected override Element render()
            {
                var svgForIsCheckedFalse = new svg(ViewBox(0, 0, 24, 24), Fill(rgb(22, 160, 133)))
                {
                    new path { d = "M19 5v14H5V5h14m0-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z" }
                };

                var svgForIsCheckedTrue = new svg(ViewBox(0, 0, 24, 24), Fill(rgb(22, 160, 133)))
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
        }

        [CustomComponent]
        [Import(Name = "BChip", Package = "b-chip")]
        sealed class BChip : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string color { get; set; }

            [JsTypeInfo(JsType.String)]
            public string label { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onClick { get; set; }

            [Suggestions("default, filled , outlined")]
            [JsTypeInfo(JsType.String)]
            public string variant { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BChip))
                {
                    var onClickProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onClick));
                    if (onClickProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "() =>",
                            "{"
                        ];

                        if (IsAlphaNumeric(onClickProp.Value))
                        {
                            lines.Add(onClickProp.Value + "();");
                        }
                        else
                        {
                            lines.Add(onClickProp.Value);
                        }

                        lines.Add("}");

                        onClickProp = onClickProp with
                        {
                            Value = string.Join(Environment.NewLine, lines)
                        };

                        properties = properties.SetItem(properties.FindIndex(x => x.Name == onClickProp.Name), onClickProp);

                        node = node with { Properties = properties };
                    }

                    node = AddContextProp(node);
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
            }

            protected override Element render()
            {
                return new div(BorderRadius(16))
                {
                    new Chip
                    {
                        color   = color,
                        label   = label,
                        variant = variant
                    },
                    Id(id), OnClick(onMouseClick)
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BComboBox", Package = "b-combo-box")]
        sealed class BComboBox : PluginComponentBase
        {
            [JsTypeInfo(JsType.Array)]
            public string dataSource { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string hiddenClearButton { get; set; }

            [JsTypeInfo(JsType.String)]
            public string hintText { get; set; }

            [JsTypeInfo(JsType.String)]
            public string labelText { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onSelect { get; set; }

            [JsTypeInfo(JsType.String)]
            public string value { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BComboBox))
                {
                    var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
                    var onSelectProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onSelect));

                    if (valueProp is not null)
                    {
                        var properties = node.Properties;

                        var isCollection = IsPropertyPathProvidedByCollection(componentConfig, valueProp.Value);

                        List<string> lines =
                        [
                            "(selectedIndexes: [number], selectedItems: [TextValuePair], selectedValues: [string]) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = selectedValues{(isCollection ? string.Empty : "[0]")}; }});"
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
                            properties = properties.Add(new()
                            {
                                Name  = nameof(onSelect),
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        if (!isCollection)
                        {
                            properties = properties.SetItem(properties.IndexOf(valueProp), valueProp with
                            {
                                Value = $"[{valueProp.Value}]"
                            });
                        }

                        node = node with { Properties = properties };
                    }
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
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

            static bool IsPropertyPathProvidedByCollection(IReadOnlyDictionary<string, string> componentConfig, string propertyPathWithVariableName)
            {
                foreach (var (variableName, dotNetAssemblyFilePath, dotnetTypeFullName) in GetDotNetVariables(componentConfig))
                {
                    if (!propertyPathWithVariableName.StartsWith(variableName + ".", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var propertyPath = propertyPathWithVariableName.RemoveFromStart(variableName + ".");

                    return CecilHelper.IsPropertyPathProvidedByCollection(dotNetAssemblyFilePath, dotnetTypeFullName, propertyPath);
                }

                return false;
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalAccountView", Package = "b-digital-account-view")]
        sealed class BDigitalAccountView : PluginComponentBase
        {
            [JsTypeInfo(JsType.Array)]
            public string accounts { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onSelectedAccountIndexChange { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string selectedAccountIndex { get; set; }

            [JsTypeInfo(JsType.String)]
            public string title { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalAccountView))
                {
                    var selectedAccountIndexProp = node.Properties.FirstOrDefault(x => x.Name == nameof(selectedAccountIndex));
                    var onSelectedAccountIndexChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onSelectedAccountIndexChange));

                    if (selectedAccountIndexProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(selectedAccountIndex: number) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{selectedAccountIndexProp.Value.RemoveFromStart("request.")} = selectedAccountIndex; }});"
                        ];

                        if (onSelectedAccountIndexChangeProp is not null)
                        {
                            if (IsAlphaNumeric(onSelectedAccountIndexChangeProp.Value))
                            {
                                lines.Add(onSelectedAccountIndexChangeProp.Value + "(selectedAccountIndex);");
                            }
                            else
                            {
                                lines.Add(onSelectedAccountIndexChangeProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (onSelectedAccountIndexChangeProp is not null)
                        {
                            onSelectedAccountIndexChangeProp = onSelectedAccountIndexChangeProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == onSelectedAccountIndexChangeProp.Name), onSelectedAccountIndexChangeProp);
                        }
                        else
                        {
                            properties = properties.Add(new()
                            {
                                Name  = nameof(onSelectedAccountIndexChange),
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
            }

            protected override Element render()
            {
                var textContent = string.Empty;
                if (title.HasValue())
                {
                    textContent = title;
                }

                if (accounts.HasValue())
                {
                    textContent += " | " + accounts;
                }

                return new div
                {
                    Id(id), OnClick(onMouseClick),
                    new FlexRow(AlignItemsCenter, Padding(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(83), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400) { textContent },

                        new FlexRow(AlignItemsCenter, TextAlignRight, Gap(8))
                        {
                            new FlexColumn
                            {
                                new div(FontWeight700) { "73.148,00 TL" },
                                new div(Color("rgb(0 0 0 / 60%)")) { "Cari Hesap" }
                            },

                            new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24), Color(rgb(117, 117, 117)))
                            {
                                new path
                                {
                                    d    = "M7 10l5 5 5-5z",
                                    fill = "#757575"
                                }
                            }
                        }
                    }
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalBox", Package = "b-digital-box")]
        sealed class BDigitalBox : PluginComponentBase
        {
            [Suggestions("noMargin, primary")]
            [JsTypeInfo(JsType.String)]
            public string styleContext { get; set; }

            protected override Element render()
            {
                var style = new Style();

                if (styleContext == "primary")
                {
                    style = new()
                    {
                        Background(rgb(255, 255, 255)),
                        Border(1, solid, rgba(0, 0, 0, 0.12)),
                        BorderRadius(8)
                    };
                }

                if (styleContext == "noMargin")
                {
                    style = new()
                    {
                        Margin(0)
                    };
                }

                return new Grid
                {
                    children = { children },
                    style    = { style }
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalDatepicker", Package = "b-digital-datepicker")]
        sealed class BDigitalDatepicker : PluginComponentBase
        {
            [JsTypeInfo(JsType.Boolean)]
            public string disabled { get; set; }

            [JsTypeInfo(JsType.String)]
            public string format { get; set; }

            [JsTypeInfo(JsType.String)]
            public string labelText { get; set; }

            [JsTypeInfo(JsType.Date)]
            public string maxDate { get; set; }

            [JsTypeInfo(JsType.Date)]
            public string minDate { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onDateChange { get; set; }

            [JsTypeInfo(JsType.String)]
            public string placeholder { get; set; }

            [JsTypeInfo(JsType.Date)]
            public string value { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalDatepicker))
                {
                    var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
                    var onDateChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onDateChange));
                    if (valueProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(value: Date) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = value; }});"
                        ];

                        if (onDateChangeProp is not null)
                        {
                            if (IsAlphaNumeric(onDateChangeProp.Value))
                            {
                                lines.Add(onDateChangeProp.Value + "(value);");
                            }
                            else
                            {
                                lines.Add(onDateChangeProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (onDateChangeProp is not null)
                        {
                            onDateChangeProp = onDateChangeProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == onDateChangeProp.Name), onDateChangeProp);
                        }
                        else
                        {
                            properties = properties.Add(new()
                            {
                                Name  = nameof(onDateChange),
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }

                    var placeholderProp = node.Properties.FirstOrDefault(x => x.Name == nameof(placeholder));
                    if (placeholderProp is not null)
                    {
                        var placeholderFinalValue = string.Empty;
                        {
                            if (IsStringValue(placeholderProp.Value))
                            {
                                placeholderFinalValue = placeholderProp.Value;
                            }
                            else
                            {
                                placeholderFinalValue = $"{ConvertDotNetPathToJsPath(placeholderProp.Value)}";
                            }
                        }

                        node = node with
                        {
                            Properties = node.Properties.Remove(placeholderProp).Add(new()
                            {
                                Name  = "inputProps",
                                Value = $"{{ placeholder: {placeholderFinalValue} }}"
                            })
                        };
                    }
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
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
                        new DynamicMuiIcon { name = "CalendarMonthOutlined", fontSize = "medium" }
                    }
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalDialog", Package = "b-digital-dialog")]
        sealed class BDigitalDialog : PluginComponentBase
        {
            [JsTypeInfo(JsType.Boolean)]
            public string displayCloseIcon { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string displayOkButton { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string fullScreen { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string open { get; set; }

            [JsTypeInfo(JsType.String)]
            public string title { get; set; }

            [Suggestions("error , warning , info , success")]
            [JsTypeInfo(JsType.String)]
            public string type { get; set; }

            protected override Element render()
            {
                return new div(Background(rgba(0, 0, 0, 0.5)), Padding(24), BorderRadius(8))
                {
                    Id(id), OnClick(onMouseClick),
                    new div(Background("white"), BorderRadius(8), Padding(16))
                    {
                        // TOP BAR
                        new FlexRow(JustifyContentSpaceBetween, AlignItemsCenter, PaddingY(16))
                        {
                            new div(FontSize20, FontWeight400, LineHeight("160%"), LetterSpacing("0.15px")) { title },

                            displayCloseIcon == "false" || displayOkButton == "false" ? null :
                                new svg(ViewBox(0, 0, 24, 24), svg.Width(24), svg.Height(24))
                                {
                                    new path
                                    {
                                        d = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"
                                    }
                                }
                        },

                        SpaceY(12),

                        children
                    }
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalFilterView", Package = "b-digital-filter-view")]
        sealed class BDigitalFilterView : PluginComponentBase
        {
            [JsTypeInfo(JsType.Date)]
            public string beginDate { get; set; }

            [JsTypeInfo(JsType.String)]
            public string beginDateLabel { get; set; }

            [JsTypeInfo(JsType.Date)]
            public string endDate { get; set; }

            [JsTypeInfo(JsType.String)]
            public string endDateLabel { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onFilterDetailClear { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string setFilter { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalFilterView))
                {
                    var beginDateProp = node.Properties.FirstOrDefault(x => x.Name == nameof(beginDate));
                    var endDateProp = node.Properties.FirstOrDefault(x => x.Name == nameof(endDate));
                    var setFilterProp = node.Properties.FirstOrDefault(x => x.Name == nameof(setFilter));

                    if (beginDateProp is not null && endDateProp is not null && setFilterProp is not null)
                    {
                        var properties = node.Properties;

                        properties = properties.Add(new()
                        {
                            Name  = "beginDateDefault",
                            Value = beginDateProp.Value
                        });

                        properties = properties.Add(new()
                        {
                            Name  = "setBeginDate",
                            Value = $"(value: Date) => {{ updateRequest(r => r.{beginDateProp.Value.RemoveFromStart("request.")} = value) }}"
                        });

                        properties = properties.Add(new()
                        {
                            Name  = "endDateDefault",
                            Value = endDateProp.Value
                        });
                        properties = properties.Add(new()
                        {
                            Name  = "setEndDate",
                            Value = $"(value: Date) => {{ updateRequest(r => r.{beginDateProp.Value.RemoveFromStart("request.")} = value) }}"
                        });

                        // setFilter
                        {
                            List<string> lines =
                            [
                                "() =>",
                                "{"
                            ];

                            if (IsAlphaNumeric(setFilterProp.Value))
                            {
                                lines.Add(setFilterProp.Value + "();");
                            }
                            else
                            {
                                lines.Add(setFilterProp.Value);
                            }

                            lines.Add("}");

                            setFilterProp = setFilterProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == setFilterProp.Name), setFilterProp);
                        }

                        properties = properties.Remove(beginDateProp).Remove(endDateProp);

                        node = node with { Properties = properties };
                    }
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
            }

            protected override Element render()
            {
                return new FlexColumn(PaddingTop(16))
                {
                    Id(id), OnClick(onMouseClick),

                    new FlexColumn(PaddingY(16), FontSize18)
                    {
                        "Filtrele"
                    },
                    new div(WidthFull, PaddingTop(16), PaddingBottom(8))
                    {
                        new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                        {
                            new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                            {
                                beginDateLabel + " | " + beginDate
                            },
                            new DynamicMuiIcon { name = "CalendarMonthOutlined", fontSize = "medium" }
                        }
                    },

                    new div(WidthFull, PaddingTop(16), PaddingBottom(8))
                    {
                        new FlexRow(AlignItemsCenter, PaddingLeft(16), PaddingRight(12), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                        {
                            new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                            {
                                endDateLabel + " | " + endDate
                            },
                            new DynamicMuiIcon { name = "CalendarMonthOutlined", fontSize = "medium" }
                        }
                    },

                    children,

                    new BButton
                    {
                        text = "Sonuçları Göster"
                    }
                };
            }
        }

        [CustomComponent]
        [Import(Name = "BDigitalMoneyInput", Package = "b-digital-money-input")]
        sealed class BDigitalMoneyInput : PluginComponentBase
        {
            [JsTypeInfo(JsType.Boolean)]
            public string currencyVisible { get; set; }

            [JsTypeInfo(JsType.String)]
            public string fec { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string handleMoneyInputChange { get; set; }

            [JsTypeInfo(JsType.String)]
            public string label { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string value { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalMoneyInput))
                {
                    var valueProp = node.Properties.FirstOrDefault(x => x.Name == nameof(value));
                    var handleMoneyInputChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(handleMoneyInputChange));
                    if (valueProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(value: number) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{valueProp.Value.RemoveFromStart("request.")} = value; }});"
                        ];

                        if (handleMoneyInputChangeProp is not null)
                        {
                            if (IsAlphaNumeric(handleMoneyInputChangeProp.Value))
                            {
                                lines.Add(handleMoneyInputChangeProp.Value + "(value);");
                            }
                            else
                            {
                                lines.Add(handleMoneyInputChangeProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (handleMoneyInputChangeProp is not null)
                        {
                            handleMoneyInputChangeProp = handleMoneyInputChangeProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == handleMoneyInputChangeProp.Name), handleMoneyInputChangeProp);
                        }
                        else
                        {
                            properties = properties.Add(new()
                            {
                                Name  = nameof(handleMoneyInputChange),
                                Value = string.Join(Environment.NewLine, lines)
                            });
                        }

                        node = node with { Properties = properties };
                    }
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
            }

            protected override Element render()
            {
                var textContent = label ?? "Tutar";

                if (value.HasValue())
                {
                    textContent += " | " + value;
                }

                return new div(WidthFull, PaddingTop(16), PaddingBottom(8))
                {
                    new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif"))
                        {
                            textContent
                        },

                        new div { fec ?? "TL" }
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

        [CustomComponent]
        sealed class BDigitalPlateNumber : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string label { get; set; }

            [JsTypeInfo(JsType.String)]
            public string value { get; set; }

            protected override Element render()
            {
                return new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                {
                    new div { value ?? "?" }
                };
            }
        }

        [CustomComponent]
        sealed class BDigitalTabNavigator : PluginComponentBase
        {
            [JsTypeInfo(JsType.Array)]
            public string items { get; set; }

            [JsTypeInfo(JsType.String)]
            public string mainResource { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string selectedTab { get; set; }

            protected override Element render()
            {
                if (items.HasNoValue())
                {
                    return null;
                }

                var itemList = JsonConvert.DeserializeObject<ItemModel[]>(items);

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

        [CustomComponent]
        [Import(Name = "BInput", Package = "b-input")]
        sealed class BInput : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string floatingLabelText { get; set; }

            [JsTypeInfo(JsType.String)]
            public string helperText { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string isAutoComplete { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string isRequired { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string maxLength { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onChange { get; set; }

            [JsTypeInfo(JsType.String)]
            public string value { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
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
                            properties = properties.Add(new()
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
                            Properties = node.Properties.Remove(isRequiredProp).Remove(isAutoCompleteProp).Add(new()
                            {
                                Name  = "valueConstraint",
                                Value = $"{{ required: {ConvertDotNetPathToJsPath(isRequiredProp.Value)}, autoComplete: {autoCompleteFinalValue} }}"
                            })
                        };
                    }

                    node = AddContextProp(node);
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
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

        [CustomComponent]
        [Import(Name = "BInputMaskExtended", Package = "b-input-mask-extended")]
        sealed class BInputMaskExtended : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string floatingLabelText { get; set; }

            [JsTypeInfo(JsType.String)]
            public string helperText { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string isAutoComplete { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string isReadonly { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string isRequired { get; set; }

            [JsTypeInfo(JsType.Array)]
            public string mask { get; set; }

            [JsTypeInfo(JsType.Number)]
            public string maxLength { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onChange { get; set; }

            [JsTypeInfo(JsType.String)]
            public string value { get; set; }

            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
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
                            properties = properties.Add(new()
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
                            Properties = node.Properties.Remove(isRequiredProp).Remove(isAutoCompleteProp).Add(new()
                            {
                                Name  = "valueConstraint",
                                Value = $"{{ required: {ConvertDotNetPathToJsPath(isRequiredProp.Value)}, autoComplete: {autoCompleteFinalValue} }}"
                            })
                        };
                    }

                    node = AddContextProp(node);
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
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

        [CustomComponent]
        sealed class BPlateNumber : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string label { get; set; }

            [JsTypeInfo(JsType.String)]
            public string value { get; set; }

            protected override Element render()
            {
                var textContent = string.Empty;
                if (label.HasValue())
                {
                    textContent = label;
                }

                if (value.HasValue())
                {
                    textContent += " | " + value;
                }

                return new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                {
                    new div { textContent },

                    Id(id), OnClick(onMouseClick)
                };
            }
        }

        [CustomComponent]
        sealed class BRadioButtonGroup : PluginComponentBase
        {
            [JsTypeInfo(JsType.Array)]
            public string items { get; set; }

            protected override Element render()
            {
                if (items.HasNoValue())
                {
                    return null;
                }

                var itemList = JsonConvert.DeserializeObject<ItemModel[]>(items);

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

        [CustomComponent]
        [Import(Name = "BTypography", Package = "b-core-typography")]
        sealed class BTypography : PluginComponentBase
        {
            static readonly Dictionary<string, StyleModifier> ColorMap = new()
            {
                { "primary", new Style { Color("#16A085") } },
                { "secondary", new Style { Color("#FF9500") } }
            };

            static readonly Dictionary<string, StyleModifier> VariantMap = new()
            {
                { "body0", new Style { FontSize18, FontWeight400, LineHeight(1.5) } },
                { "body1", new Style { FontSize16, FontWeight400, LineHeight(1.5) } },
                { "body2", new Style { FontSize14, FontWeight400, LineHeight(1.43) } },
                { "body2b", new Style { FontSize14, FontWeight600, LineHeight(1.5) } },
                { "body2m", new Style { FontSize14, FontWeight500, LineHeight(1.5) } },

                { "h1", new Style { FontSize("6rem"), FontWeight300, LineHeight(1.167) } },
                { "h2", new Style { FontSize("3.75rem"), FontWeight300, LineHeight(1.2) } },
                { "h3", new Style { FontSize("3rem"), FontWeight400, LineHeight(1.167) } }
            };

            [Suggestions("primary, secondary")]
            [JsTypeInfo(JsType.String)]
            public string color { get; set; }

            public string dangerouslySetInnerHTML { get; set; }

            [Suggestions("h1, h2 , h3 , h4 , h5 , h6 , body0 , body1 , body2, body2m")]
            [JsTypeInfo(JsType.String)]
            public string variant { get; set; }

            protected override Element render()
            {
                var styleOverride = new Style();

                if (variant.HasValue())
                {
                    if (VariantMap.TryGetValue(variant, out var value))
                    {
                        styleOverride += value;
                    }
                }

                if (color.HasValue())
                {
                    if (ColorMap.TryGetValue(color, out var value))
                    {
                        styleOverride += value;
                    }
                }

                return new Typography
                {
                    children = { children },
                    variant  = variant,
                    color    = color,
                    style    = { styleOverride },

                    id      = id,
                    onClick = onMouseClick
                    //TODO: Open: dangerouslySetInnerHTML = dangerouslySetInnerHTML
                };
            }
        }

        [CustomComponent]
        sealed class TransactionWizardPage : PluginComponentBase
        {
            [JsTypeInfo(JsType.Boolean)]
            public string isWide { get; set; }

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

sealed class SuggestionsAttribute : Attribute
{
    public SuggestionsAttribute(string[] suggestions)
    {
        Suggestions = suggestions;
    }

    public SuggestionsAttribute(string suggestions)
    {
        Suggestions = suggestions.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    public IReadOnlyList<string> Suggestions { get; }
}

sealed class CustomComponentAttribute : Attribute
{
}

sealed class ImportAttribute : Attribute
{
    public string Name { get; init; }

    public string Package { get; init; }
}

sealed class NodeAnalyzerAttribute : Attribute
{
}
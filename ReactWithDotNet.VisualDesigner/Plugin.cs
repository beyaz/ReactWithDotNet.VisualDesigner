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
                pagesFolderPath = $@"D:\work\BOA.BusinessModules\Test\{solutionName}\OBAWeb\OBA.Web.{solutionName.RemoveFromStart("BOA.")}\ClientApp\pages\";
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
                const string projectBinDirectoryPath = @"D:\work\BOA.BusinessModules\Test\BOA.InternetBanking.Payments\API\BOA.InternetBanking.Payments.API\bin\Debug\net8.0\";

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
                return new FlexColumn(MarginBottom(24),MarginTop(8))
                {
                    title is null ? null : new div(FontSize18, FontWeight600, LineHeight32, Color(rgba(0, 0, 0, 0.87))) { title },
                    
                    new FlexColumn( Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(24), Id(id), OnClick(onMouseClick))
                    {
                        children
                    }
                };
            }
        }

        [CustomComponent]
        [Import(Name = nameof(BDigitalTransactionConfirm), Package = "b-digital-transaction-confirm")]
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
            
            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalTransactionConfirm))
                {

                    List<ReactProperty> finalProps = [];
                    
                    

                   
                   
                        

                    // sender
                    {
                        List<string> lines = [];
                        
                        var sender = new
                        {
                            title = tryGetValueAt(node, [0, 1, 0, 0]),

                            item1Text  = tryGetValueAt(node, [0, 1, 0, 1, 0]),
                            item1Value = tryGetValueAt(node, [0, 1, 0, 1, 1]),
                        
                            item2Text  = tryGetValueAt(node, [0, 1, 0, 2, 0]),
                            item2Value = tryGetValueAt(node, [0, 1, 0, 2, 1]),
                        
                            item3Text  = tryGetValueAt(node, [0, 1, 0, 3, 0]),
                            item3Value = tryGetValueAt(node, [0, 1, 0, 3, 1])
                        };
                        
                        if (sender.title is not null)
                        {
                            lines.Add($"titleText: {sender.title.Children[0].Properties[0].Value}");
                        }

                        var item1 = getItem(sender.item1Text, sender.item1Value);
                        if (item1 is not null)
                        {
                            lines.Add("item1: " + item1);
                        }
                    
                        var item2 = getItem(sender.item2Text, sender.item2Value);
                        if (item2 is not null)
                        {
                            lines.Add("item2: " + item2);
                        }
                    
                        var item3 = getItem(sender.item3Text, sender.item3Value);
                        if (item3 is not null)
                        {
                            lines.Add("item3: " + item3);
                        }

                        if (lines.Count > 0)
                        {
                            var prop = new ReactProperty
                            {
                                Name  = "senderData",
                                Value = '{' + string.Join(",", lines) + '}'
                            };

                            finalProps.Add(prop);
                        }
                    }

                    // amount
                    {
                        var transferAmount = tryGetValueAt(node, [1, 1]);
                        if (transferAmount is not null)
                        {
                            finalProps.Add(new ReactProperty
                            {
                                Name  = "transferAmount",
                                Value =  string.Join(",", transferAmount.Children[0].Children[0].Text)
                            });

                            finalProps.Add(new ReactProperty
                            {
                                Name  = "transferAmountVariant",
                                Value = string.Join(",", transferAmount.Properties[0].Value)
                            });
                        }
                    }
                    
                    
                    // receiver
                    {
                        List<string> lines = [];
                        
                        var receiver = new
                        {
                            title = tryGetValueAt(node, [2, 1, 0, 0]),

                            item1Text  = tryGetValueAt(node, [2, 1, 0, 1, 0]),
                            item1Value = tryGetValueAt(node, [2, 1, 0, 1, 1]),
                        
                            item2Text  = tryGetValueAt(node, [2, 1, 0, 2, 0]),
                            item2Value = tryGetValueAt(node, [2, 1, 0, 2, 1]),
                        
                            item3Text  = tryGetValueAt(node, [2, 1, 0, 3, 0]),
                            item3Value = tryGetValueAt(node, [2, 1, 0, 3, 1])
                        };
                        
                        if (receiver.title is not null)
                        {
                            lines.Add($"titleText: {receiver.title.Children[0].Properties[0].Value}");
                        }

                        var item1 = getItem(receiver.item1Text, receiver.item1Value);
                        if (item1 is not null)
                        {
                            lines.Add("item1: " + item1);
                        }
                    
                        var item2 = getItem(receiver.item2Text, receiver.item2Value);
                        if (item2 is not null)
                        {
                            lines.Add("item2: " + item2);
                        }
                    
                        var item3 = getItem(receiver.item3Text, receiver.item3Value);
                        if (item3 is not null)
                        {
                            lines.Add("item3: " + item3);
                        }

                        if (lines.Count > 0)
                        {
                            var prop = new ReactProperty
                            {
                                Name  = "receiverData",
                                Value = '{' + string.Join(",", lines) + '}'
                            };

                            finalProps.Add(prop);
                        }
                    }
                    
                    
                    // transactionDetailLines
                    {
                        List<string> lines = [];
                        
                        var items = new
                        {
                            item1Text  = tryGetValueAt(node, [4, 0, 0]),
                            item1Value = tryGetValueAt(node, [4, 0, 1]),
                        
                            item2Text  = tryGetValueAt(node, [4, 1, 0]),
                            item2Value = tryGetValueAt(node, [4, 1, 1]),
                        
                            item3Text  = tryGetValueAt(node, [4, 2, 0]),
                            item3Value = tryGetValueAt(node, [4, 2, 1])
                        };
                        
                       

                        var item1 = getItem(items.item1Text, items.item1Value);
                        if (item1 is not null)
                        {
                            lines.Add(item1);
                        }
                    
                        var item2 = getItem(items.item2Text, items.item2Value);
                        if (item2 is not null)
                        {
                            lines.Add(item2);
                        }
                    
                        var item3 = getItem(items.item3Text, items.item3Value);
                        if (item3 is not null)
                        {
                            lines.Add(item3);
                        }

                        finalProps.Add(new ReactProperty
                        {
                            Name  = "transactionDetailList",
                            Value = '[' + string.Join(",", lines) + ']'
                        });
                    }

                 
                    
                    node = node with
                    {
                        Children = [],
                        Properties = finalProps.ToImmutableList()
                    };
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };

                static string getItem(ReactNode textNode, ReactNode valueNode)
                {
                    if (valueNode is null)
                    {
                        if (textNode is null)
                        {
                            return null;
                        }

                        var value = textNode.Children[0].Properties[0].Value;

                        var valueVariant = textNode.Properties[0].Value;

                        return '{' + "value:" + value + ", valueVariant: " + valueVariant + '}';
                    }

                    {
                        var text = textNode.Children[0].Properties[0].Value;

                        var textVariant = textNode.Properties[0].Value;
                            
                        var value = valueNode.Children[0].Properties[0].Value;

                        var valueVariant = valueNode.Properties[0].Value;

                        return '{' + "text: " + text + ", textVariant: "+ textVariant +  ", value:" + value + ", valueVariant: " + valueVariant + '}';
                    }
                }
                
                static ReactNode tryGetValueAt(ReactNode node, int[] location)
                {
                    foreach (var childIndex in location)
                    {
                        if (node is null)
                        {
                            return null;
                        }

                        if (!(node.Children.Count > childIndex))
                        {
                            return null;
                        }
                        
                        node = node.Children[childIndex];
                    }

                    return node;
                }
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
        [Import(Name = "BDigitalPhone", Package = "b-digital-phone")]
        sealed class BDigitalPhone : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string label { get; set; }
            
            [JsTypeInfo(JsType.String)]
            public string hintText { get; set; }
            
            [JsTypeInfo(JsType.String)]
            public string phoneNumber { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string handlePhoneChange { get; set; }

             [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalPhone))
                {
                    var phoneNumberProp = node.Properties.FirstOrDefault(x => x.Name == nameof(phoneNumber));
                    var handlePhoneChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(handlePhoneChange));
                   
                    if (phoneNumberProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(value: string, formattedValue: string, areaCode: string) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{phoneNumberProp.Value.RemoveFromStart("request.")} = value; }});"
                        ];

                        if (handlePhoneChangeProp is not null)
                        {
                            if (IsAlphaNumeric(handlePhoneChangeProp.Value))
                            {
                                lines.Add(handlePhoneChangeProp.Value + "(value, formattedValue, areaCode);");
                            }
                            else
                            {
                                lines.Add(handlePhoneChangeProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (handlePhoneChangeProp is not null)
                        {
                            handlePhoneChangeProp = handlePhoneChangeProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == handlePhoneChangeProp.Name), handlePhoneChangeProp);
                        }
                        else
                        {
                            properties = properties.Add(new()
                            {
                                Name  = "handlePhoneChange",
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
                if (hintText.HasValue())
                {
                    textContent = hintText;
                }

                if (phoneNumber.HasValue())
                {
                    textContent += " | " + phoneNumber;
                }

                return new div(PaddingTop(16), PaddingBottom(8))
                {
                    new FlexRow(AlignItemsCenter, PaddingLeftRight(16), Border(1, solid, "#c0c0c0"), BorderRadius(10), Height(58), JustifyContentSpaceBetween)
                    {
                        new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400, FontFamily("Roboto, sans-serif")) { textContent },

                        Id(id), OnClick(onMouseClick)
                    }
                };
            }
        }
        
        
        
        
        [CustomComponent]
        [Import(Name = nameof(BDigitalSecureConfirmAgreement), Package = "b-digital-secure-confirm-agreement")]
        sealed class BDigitalSecureConfirmAgreement : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string messageInfo { get; set; }

            [JsTypeInfo(JsType.String)]
            public string description { get; set; }

            [JsTypeInfo(JsType.Boolean)]
            public string isRefresh { get; set; }
            
            [JsTypeInfo(JsType.String)]
            public string approveText { get; set; }
       

            protected override Element render()
            {
                return new FlexColumn(MarginBottom(24),MarginTop(8))
                {
                    Id(id), OnClick(onMouseClick),
                    
                    new div(FontSize18, FontWeight600, Color(rgba(0, 0, 0, 0.87))) { "Belge Onayı" },
                    SpaceY(8),
                    new FlexColumn(Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(24))
                    {
                        new FlexRow(Gap(8), AlignItemsCenter)
                        {
                            new svg(ViewBox(0, 0, 24, 24), svg.Size(24), Fill(rgb(22, 160, 133)))
                            {
                                new path { d = "M0 0h24v24H0z", fill = none},
                                new path { d = "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z"}
                            },
                            
                            new div
                            {
                                "Bağış Tahsilat Aydınlatma Metni"
                            },
                            
                            new div(Opacity(0.5),Border(1,solid, rgb(22, 160, 133)), Color(rgb(22, 160, 133)), PaddingX(8), PaddingY(4), BorderRadius(10))
                            {
                                "Onayla"
                            }
                        },
                        SpaceY(16),
                        new FlexRowCentered
                        {
                            "İşlemi tamamlayabilmeniz için belgeleri Kuveyt Türk Mobil'de Belgelerim ekranından onaylamanız gerekmektedir. Belgeleri Onayla butonu ile Kuveyt Türk Mobil'e yönlendirileceksiniz."
                        }
                    }
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
        [Import(Name = "TextValuePair", Package = "b-digital-internet-banking")]
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
        [Import(Name = nameof(BDigitalAccountCardView), Package = "b-digital-account-card-view")]
        sealed class BDigitalAccountCardView : PluginComponentBase
        {
            [JsTypeInfo(JsType.Array)]
            public string accounts { get; set; }
            
            [JsTypeInfo(JsType.Array)]
            public string cards { get; set; }
            
            [JsTypeInfo(JsType.Number)]
            public string selectedIndex { get; set; }
            
            [JsTypeInfo(JsType.Number)]
            public string isCardSelected { get; set; }

            [JsTypeInfo(JsType.Function)]
            public string onSelectedIndexChange { get; set; }
            
            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalAccountCardView))
                {
                    var selectedIndexProp = node.Properties.FirstOrDefault(x => x.Name == nameof(selectedIndex));
                    var isCardSelectedProp = node.Properties.FirstOrDefault(x => x.Name == nameof(isCardSelected));
                    var onSelectedIndexChangeProp = node.Properties.FirstOrDefault(x => x.Name == nameof(onSelectedIndexChange));

                    if (selectedIndexProp is not null && isCardSelectedProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(selectedAccountCardIndex: number, isCardSelected: boolean) =>",
                            "{",
                            "  updateRequest(r => {",
                            $"       r.{selectedIndexProp.Value.RemoveFromStart("request.")} = selectedAccountCardIndex;",
                            $"       r.{isCardSelectedProp.Value.RemoveFromStart("request.")} = isCardSelected;",
                            "  });"
                        ];

                        if (onSelectedIndexChangeProp is not null)
                        {
                            if (IsAlphaNumeric(onSelectedIndexChangeProp.Value))
                            {
                                lines.Add(onSelectedIndexChangeProp.Value + "(selectedAccountCardIndex, isCardSelected);");
                            }
                            else
                            {
                                lines.Add(onSelectedIndexChangeProp.Value);
                            }
                        }

                        lines.Add("}");

                        if (onSelectedIndexChangeProp is not null)
                        {
                            onSelectedIndexChangeProp = onSelectedIndexChangeProp with
                            {
                                Value = string.Join(Environment.NewLine, lines)
                            };

                            properties = properties.SetItem(properties.FindIndex(x => x.Name == onSelectedIndexChangeProp.Name), onSelectedIndexChangeProp);
                        }
                        else
                        {
                            properties = properties.Add(new()
                            {
                                Name  = nameof(onSelectedIndexChange),
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
                if (cards.HasValue())
                {
                    textContent = cards;
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
                        PositionRelative,
                        new div(Color("rgb(0 0 0 / 60%)"))
                        {
                            "Hesap / Kart Seçimi",
                            
                            PositionAbsolute,
                            Top(-16),
                            Left(8),
                            Transform("scale(0.942723)"),
                            
                            WhiteSpaceNoWrap,
                            
                            Background(White),
                            PaddingX(8)
                        },
                        
                        
                        new FlexColumn
                        {
                            new div(Color(rgba(0, 0, 0, 0.54)), FontSize16, FontWeight400)
                            {
                                textContent
                            },
                            
                            new div(Color(rgba(0, 0, 0, 0.87)), FontSize16, FontWeight400)
                            {
                                "0000000-1"
                            }
                            
                        },

                        new FlexRow(AlignItemsCenter, TextAlignRight, Gap(8))
                        {
                            new FlexColumn
                            {
                                new div(FontWeight700) { "73.148,00 TL" },
                                new div(FontWeight400,FontSize16, Color(rgba(0, 0, 0, 0.6))) { "Cari Hesap" }
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
        
        [CustomComponent]
        [Import(Name = nameof(BDigitalSecureConfirm), Package = "b-digital-secure-confirm")]
        sealed class BDigitalSecureConfirm : PluginComponentBase
        {
            [JsTypeInfo(JsType.String)]
            public string smsPassword { get; set; }

            [JsTypeInfo(JsType.String)]
            public string messageInfo { get; set; }
            
            [NodeAnalyzer]
            public static ReactNode AnalyzeReactNode(ReactNode node, IReadOnlyDictionary<string, string> componentConfig)
            {
                if (node.Tag == nameof(BDigitalSecureConfirm))
                {
                    var smsPasswordProp = node.Properties.FirstOrDefault(x => x.Name == nameof(smsPassword));

                    if (smsPasswordProp is not null)
                    {
                        var properties = node.Properties;

                        List<string> lines =
                        [
                            "(value: string) =>",
                            "{",
                            $"  updateRequest(r => {{ r.{smsPasswordProp.Value.RemoveFromStart("request.")} = value; }});",
                            "}"
                        ];

                        properties = properties.Remove(smsPasswordProp).Add(new()
                        {
                            Name  = "handleSmsPasswordSend",
                            Value = string.Join(Environment.NewLine, lines)
                        });

                        node = node with { Properties = properties };
                    }
                }

                return node with { Children = node.Children.Select(x => AnalyzeReactNode(x, componentConfig)).ToImmutableList() };
            }

            protected override Element render()
            {
                return new FlexColumn(MarginBottom(24),MarginTop(8))
                {
                    new div(FontSize18, FontWeight600, LineHeight32, Color(rgba(0, 0, 0, 0.87))) { "Mobil Onay" },
                    
                    new FlexRow(Gap(16) ,Background(White), BorderRadius(10), Border(1, solid, "#E0E0E0"), Padding(24), Id(id), OnClick(onMouseClick))
                    {
                        new FlexColumn(WidthFull)
                        {
                            new div
                            {
                                "Mobil Onay Bekleniyor...",
                                Color(rgba(2, 136, 209, 1))
                            },
                        
                            new div(FontWeight400)
                            {
                                "Ödeme işleminizi gerçekleştirmek için cihazınıza gelen Mobil Onay bildirimine belirtilen süre içerisinde onay vermeniz gerekmektedir."
                            }
                        },
                        
                        new FlexRowCentered(Size(76))
                        {
                            
                            new ellipse
                            {
                                Size(72),
                                Border(4,solid,Red)
                            }
                            //new svg(svg.Size(72), svg.ViewBox(0,0,72,72))
                            //{
                            //    new path
                            //    {
                            //        d="M2.00001 38C2.00001 29.905 4.72833 22.0462 9.74444 15.6925C14.7605 9.33893 21.7717 4.86135 29.6457 2.98278C37.5198 1.10422 45.7972 1.93432 53.1414 5.33905C60.4856 8.74378 66.468 14.5244 70.1225 21.7476C73.7771 28.9707 74.8906 37.2148 73.2831 45.1486C71.6756 53.0825 67.4411 60.243 61.2632 65.474C55.0854 70.705 47.3247 73.7012 39.2345 73.9788C31.1442 74.2564 23.1964 71.7992 16.6746 67.0039",
                            //        stroke="#ECEFF1",
                            //        strokeWidth = 4,
                                    
                            //        strokeLinecap = "round"
                                    
                            //    },
                                
                            //    new path
                            //    {
                            //        d             ="M38 2.00001C46.095 2.00001 53.9538 4.72833 60.3074 9.74443C66.661 14.7605 71.1386 21.7716 73.0172 29.6457C74.8958 37.5197 74.0657 45.7971 70.6609 53.1414C67.2562 60.4856 61.4756 66.4679 54.2524 70.1225C47.0293 73.7771 38.7852 74.8905 30.8514 73.2831C22.9175 71.6756 15.757 67.4411 10.526 61.2632C5.29496 55.0853 2.29876 47.3247 2.02118 39.2344C1.7436 31.1442 4.20083 23.1964 8.99612 16.6745",
                            //        stroke        ="#0288D1",
                            //        strokeWidth   = 4,
                            //        fill          ="white",
                            //        strokeLinecap = "round"
                                    
                            //    },
                                
                            //    new path
                            //    {
                            //        d             ="M36.1953 46.5146V50.25H19.6602V47.0625L27.4795 38.6787C28.2653 37.8044 28.8851 37.0352 29.3389 36.3711C29.7926 35.696 30.1191 35.0928 30.3184 34.5615C30.5286 34.0192 30.6338 33.5046 30.6338 33.0176C30.6338 32.2871 30.512 31.6618 30.2686 31.1416C30.0251 30.6104 29.6654 30.2008 29.1895 29.9131C28.7246 29.6253 28.1491 29.4814 27.4629 29.4814C26.7324 29.4814 26.1016 29.6585 25.5703 30.0127C25.0501 30.3669 24.6517 30.8594 24.375 31.4902C24.1094 32.1211 23.9766 32.835 23.9766 33.6318H19.1787C19.1787 32.193 19.5218 30.876 20.208 29.6807C20.8942 28.4743 21.8626 27.5169 23.1133 26.8086C24.3639 26.0892 25.847 25.7295 27.5625 25.7295C29.2559 25.7295 30.6836 26.0062 31.8457 26.5596C33.0189 27.1019 33.9043 27.8877 34.502 28.917C35.1107 29.9352 35.415 31.1527 35.415 32.5693C35.415 33.3662 35.2878 34.1465 35.0332 34.9102C34.7786 35.6628 34.4134 36.4154 33.9375 37.168C33.4727 37.9095 32.9082 38.6621 32.2441 39.4258C31.5801 40.1895 30.8441 40.9808 30.0361 41.7998L25.8359 46.5146H36.1953ZM42.8203 46.6143H43.1357C44.4639 46.6143 45.6038 46.4538 46.5557 46.1328C47.5186 45.8008 48.3099 45.3249 48.9297 44.7051C49.5495 44.0853 50.0088 43.3271 50.3076 42.4307C50.6064 41.5231 50.7559 40.4938 50.7559 39.3428V34.7607C50.7559 33.8864 50.6673 33.1172 50.4902 32.4531C50.3242 31.7891 50.0807 31.2412 49.7598 30.8096C49.4499 30.3669 49.0846 30.0348 48.6641 29.8135C48.2546 29.5921 47.8008 29.4814 47.3027 29.4814C46.7715 29.4814 46.3011 29.6143 45.8916 29.8799C45.4821 30.1344 45.1335 30.4831 44.8457 30.9258C44.569 31.3685 44.3532 31.8776 44.1982 32.4531C44.0544 33.0176 43.9824 33.6097 43.9824 34.2295C43.9824 34.8493 44.0544 35.4414 44.1982 36.0059C44.3421 36.5592 44.5579 37.0518 44.8457 37.4834C45.1335 37.904 45.4932 38.2415 45.9248 38.4961C46.3564 38.7396 46.8656 38.8613 47.4521 38.8613C48.0166 38.8613 48.5202 38.7562 48.9629 38.5459C49.4167 38.3245 49.7985 38.0368 50.1084 37.6826C50.4294 37.3285 50.6729 36.9355 50.8389 36.5039C51.016 36.0723 51.1045 35.6351 51.1045 35.1924L52.6816 36.0557C52.6816 36.8304 52.5156 37.5941 52.1836 38.3467C51.8516 39.0993 51.3867 39.7855 50.7891 40.4053C50.2025 41.014 49.5218 41.501 48.7471 41.8662C47.9723 42.2314 47.1423 42.4141 46.2568 42.4141C45.1279 42.4141 44.1263 42.2093 43.252 41.7998C42.3776 41.3792 41.6361 40.8037 41.0273 40.0732C40.4297 39.3317 39.9759 38.4684 39.666 37.4834C39.3561 36.4984 39.2012 35.4303 39.2012 34.2793C39.2012 33.1061 39.3893 32.0049 39.7656 30.9756C40.153 29.9463 40.7008 29.0387 41.4092 28.2529C42.1286 27.4671 42.9863 26.8529 43.9824 26.4102C44.9896 25.9564 46.1074 25.7295 47.3359 25.7295C48.5645 25.7295 49.6823 25.9674 50.6895 26.4434C51.6966 26.9193 52.5599 27.5944 53.2793 28.4688C53.9987 29.332 54.5521 30.3669 54.9395 31.5732C55.3379 32.7796 55.5371 34.1188 55.5371 35.5908V37.251C55.5371 38.8226 55.3656 40.2835 55.0225 41.6338C54.6904 42.984 54.1868 44.207 53.5117 45.3027C52.8477 46.3874 52.0176 47.3171 51.0215 48.0918C50.0365 48.8665 48.891 49.4587 47.585 49.8682C46.279 50.2777 44.818 50.4824 43.2021 50.4824H42.8203V46.6143Z",
                                  
                            //        fill = "#0288D1"
                                    
                            //    }
                            //}
                        }
                    }
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

[AttributeUsage(AttributeTargets.Property)]
sealed class SuggestionsAttribute : Attribute
{
    public SuggestionsAttribute(string[] suggestions)
    {
        Suggestions = suggestions;
    }

    public SuggestionsAttribute(string suggestions)
    {
        Suggestions = suggestions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).ToList();
    }

    public IReadOnlyList<string> Suggestions { get; }
}

[AttributeUsage(AttributeTargets.Class)]
sealed class CustomComponentAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
sealed class ImportAttribute : Attribute
{
    public string Name { get; init; }

    public string Package { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
sealed class NodeAnalyzerAttribute : Attribute
{
}
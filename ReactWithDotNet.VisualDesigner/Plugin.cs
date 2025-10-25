using Dapper;
using Microsoft.Data.SqlClient;
using Mono.Cecil;
using Newtonsoft.Json;
using ReactWithDotNet.ThirdPartyLibraries.MUI.Material;
using ReactWithDotNet.VisualDesigner.Configuration;
using ReactWithDotNet.VisualDesigner.Exporters;
using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace ReactWithDotNet.VisualDesigner;


[AttributeUsage(AttributeTargets.Method)]
public sealed class TryGetIconForElementTreeNodeAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class IsImageAttribute : Attribute
{
}

public sealed record TryCreateElementForPreviewInput
{
    public string Tag { get; init; }
    
    public string Id { get; init; }
    
    public MouseEventHandler OnMouseClick { get; init; }
}

public delegate Element TryCreateElementForPreview(TryCreateElementForPreviewInput input);

[AttributeUsage(AttributeTargets.Method)]
public sealed class TryCreateElementForPreviewAttribute : Attribute
{
}


sealed record PropSuggestionScope
{
    public ComponentEntity Component { get; init; }

    public Maybe<ComponentEntity> SelectedComponent { get; init; }
    
    public string TagName { get; init; }
}

interface IPlugin
{
    string AnalyzeExportFilePath(string exportFilePathForComponent);
    Element TryCreateElementForPreview(TryCreateElementForPreviewInput input);
}

abstract class PluginBase:IPlugin
{
    public virtual string AnalyzeExportFilePath(string exportFilePathForComponent)
    {
        return exportFilePathForComponent;
    }

    public virtual Element TryCreateElementForPreview(TryCreateElementForPreviewInput input)
    {
        return null;
    }
}


class Plugin: PluginBase
{
    public static readonly ScopeKey<VisualElementModel> VisualElementModel = new() { Key = nameof(VisualElementModel) };
    
    public static readonly ScopeKey<Element> IconForElementTreeNode = new() { Key = nameof(IconForElementTreeNode) };
    
    public static readonly ScopeKey<Element> CurrentElementInstanceInPreview = new() { Key = nameof(CurrentElementInstanceInPreview) };

    public static readonly ScopeKey<object> IsImageKey = new() { Key = nameof(IsImageKey) };

    
    public static string GetUpdateStateLine(string jsVariableName)
    {
        var propertyPath = jsVariableName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (propertyPath.Length == 2)
        {
            var stateName = propertyPath[0];

            return $"  set{Char.ToUpper(stateName[0]) + stateName[1..]}({{ ...{stateName} }});";
        }

        return null;
    }

    public override  string AnalyzeExportFilePath(string exportFilePathForComponent)
    {
        var names = exportFilePathForComponent.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (names[0].StartsWith("BOA."))
        {
            // project folder is d:\work\
            // we need to calculate rest of path

            // sample: /BOA.InternetBanking.MoneyTransfers/x-form.tsx

            var solutionName = names[0];

            string clientAppFolderPath;
            {
                clientAppFolderPath = $@"D:\work\BOA.BusinessModules\Dev\{solutionName}\OBAWeb\OBA.Web.{solutionName.RemoveFromStart("BOA.")}\ClientApp\";
                if (solutionName == "BOA.MobilePos")
                {
                    clientAppFolderPath = @"D:\work\BOA.BusinessModules\Dev\BOA.MobilePos\OBAWeb\OBA.Web.POSPortal.MobilePos\ClientApp\";
                }
            }

            if (Directory.Exists(clientAppFolderPath))
            {
                return Path.Combine(clientAppFolderPath, Path.Combine(names.Skip(1).ToArray()));
            }
        }

        return exportFilePathForComponent;
    }

    const string BOA_MessagingByGroupName = "BOA.MessagingByGroupName";

    public static ScopeKey<ConfigModel> Config = new() { Key = nameof(Config) };
    
        
    
        
    

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

public    static IEnumerable<(string variableName, string dotNetAssemblyFilePath, string dotnetTypeFullName)> GetDotNetVariables(ComponentEntity componentEntity)
    {
        return GetDotNetVariables(componentEntity.GetConfig());
    }

    public static IEnumerable<(string variableName, string dotNetAssemblyFilePath, string dotnetTypeFullName)> GetDotNetVariables(IReadOnlyDictionary<string, string> componentConfig)
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

    public static async Task<Result<IReadOnlyList<string>>> GetPropSuggestions(PropSuggestionScope scope)
    {
        if (scope.TagName.HasNoValue())
        {
            return Result.From((IReadOnlyList<string>)[]);
        }

        return await Cache.AccessValue($"{nameof(Plugin)}-{scope.TagName}", () => calculate(scope));

        static async Task<Result<IReadOnlyList<string>>> calculate(PropSuggestionScope scope)
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

            foreach (var (variableName, dotNetAssemblyFilePath, dotnetTypeFullName) in GetDotNetVariables(scope.Component))
            {
                List<(List<string> list, Func<PropertyDefinition, bool> matchFunc)> map =
                [
                    (stringSuggestions, CecilHelper.IsString),
                    (numberSuggestions, CecilHelper.IsNumber),
                    (dateSuggestions, CecilHelper.IsDateTime),
                    (booleanSuggestions, CecilHelper.IsBoolean),
                    (collectionSuggestions, CecilHelper.IsCollection)
                ];
                
                foreach (var (list, fn) in map)
                {
                    var result = CecilHelper.GetPropertyPathList(dotNetAssemblyFilePath, dotnetTypeFullName, $"{variableName}.", fn);
                    if (result.HasError)
                    {
                     continue;   
                    }

                    list.AddRange(result.Value);
                }
                
               
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
                            if (item.StartsWith("model."))
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

    

    public override Element TryCreateElementForPreview(TryCreateElementForPreviewInput input)
    {
        var type = GetAllCustomComponents().FirstOrDefault(t => t.Name.Equals(input.Tag, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return null;
        }

        var component = (Element)Activator.CreateInstance(type);

        if (component is PluginComponentBase componentBase)
        {
            componentBase.id           = input.Id;
            componentBase.onMouseClick = input.OnMouseClick;
        }

        return component;
    }

    static IReadOnlyList<PluginMethod> TryGetIconForElementTreeNodes
    {
        get
        {
            return field ??= GetPluginMethods<TryGetIconForElementTreeNodeAttribute>();
        }
    }
    
  
    

    public static Element TryGetIconForElementTreeNode(VisualElementModel node)
    {
        
            var scope = Scope.Create(new()
            {
                { VisualElementModel, node}
            });

            return RunPluginMethods(TryGetIconForElementTreeNodes, scope, IconForElementTreeNode);

    }

    public static string ConvertDotNetPathToJsPath(string dotNetPath)
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

        

      

      
    }

    public static ReactNode AddContextProp(ReactNode node)
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

    static IReadOnlyList<PluginMethod> IsImageList
    {
        get
        {
            return field ??= GetPluginMethods<IsImageAttribute>();
        }
    }
    public static bool IsImage(Element element)
    {
        var scope = Scope.Create(new()
        {
            { CurrentElementInstanceInPreview, element }
        });

       return (bool?) RunPluginMethods(IsImageList, scope, IsImageKey) ?? false;
    }
    
    
    static IReadOnlyList<PluginMethod> AfterReadConfigs
    {
        get
        {
            return field ??= GetPluginMethods<AfterReadConfigAttribute>();
        }
    }

    static IReadOnlyList<PluginMethod> GetPluginMethods<AttributeType>()
    {
        var items =
            from assembly in Plugins
            from type in assembly.GetTypes()
            from methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            where methodInfo.GetCustomAttribute(typeof(AttributeType)) is not null
            select (PluginMethod)Delegate.CreateDelegate(typeof(PluginMethod), methodInfo);

        return items.ToList();
    }
    
    public static ConfigModel AfterReadConfig(ConfigModel config)
    {
        var scope = Scope.Create(new()
        {
            { Config, config }
        });
        
        return RunPluginMethods(AfterReadConfigs, scope, Config) ?? config;
    }
    
    static T RunPluginMethods<T>(IReadOnlyList<PluginMethod> pluginMethods, Scope scope, ScopeKey<T> returnKey) where T:class
    {
        foreach (var method in pluginMethods)
        {
            var response = method(scope);
            if (response.Has(returnKey))
            {
                return returnKey[response];
            }
        }

        return null;
    }
    
    
    static readonly IEnumerable<Assembly> Plugins =
    [
        typeof(Plugin).Assembly
    ];

}

public delegate Scope PluginMethod(Scope scope);

[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterReadConfigAttribute : Attribute
{
}

class BComponents
{
    [AfterReadConfig]
    public static Scope AfterReadConfig(Scope scope)
    {
        var config = Plugin.Config[scope];
        
        if (Environment.MachineName.StartsWith("BTARC", StringComparison.OrdinalIgnoreCase))
        {
            config = config with
            {
                Database = new()
                {
                    //IsSQLite = true,
                    //ConnectionString = @"Data Source=D:\work\git\ReactWithDotNet.VisualDesigner\app.db"

                    IsSQLServer      = true,
                    SchemaName       = "RVD",
                    ConnectionString = @"Data Source=srvdev\atlas;Initial Catalog=boa;Min Pool Size=10; Max Pool Size=100;Application Name=Thriller;Integrated Security=true; TrustServerCertificate=true;"
                }
            };
        }

        return Scope.Create(new()
        {
            { Plugin.Config, config }
        });
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

public sealed class TsLineCollection: List<string>
{

    public void Add(IEnumerable<string> lines)
    {
        AddRange(lines);
    }

    public string ToTsCode()
    {
        return string.Join(Environment.NewLine, from line in this where line is not null select line);
    }
}
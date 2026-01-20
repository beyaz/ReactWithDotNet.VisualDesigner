global using static ReactWithDotNet.VisualDesigner.Plugins.b_digital.Mixin;
global using NodeAnalyzeOutput = System.Threading.Tasks.Task<Toolbox.Result<(ReactWithDotNet.VisualDesigner.Exporters.ReactNode Node, ReactWithDotNet.VisualDesigner.TsImportCollection TsImportCollection)>>;
using System.Collections.Immutable;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

record MessagingRecord
{
    public string Description { get; init; }
    public string PropertyName { get; init; }
}

static class Mixin
{
    public static ReactNode TransformIfHasProperty(
        this ReactNode node,
        string name,
        Func<ReactNode, ReactProperty, ReactNode> then)
    {
        var prop = node.Properties.FirstOrDefault(p => p.Name == name);

        return prop is null
            ? node
            : then(node, prop);
    }
    
    public static StyleModifier IsNotMobile(params StyleModifier[] styleModifiers)
    {
        return WhenMediaMinWidth(600, styleModifiers);
    }
    public static StyleModifier IsMobile(params StyleModifier[] styleModifiers)
    {
        return WhenMediaMaxWidth(600, styleModifiers);
    }
    
    public static StyleModifier IsTablet(params StyleModifier[] styleModifiers)
    {
        return MediaQuery("(min-width: 600px) and (max-width: 959.95px)", styleModifiers);
    }
    
    public static StyleModifier isDesktop(params StyleModifier[] styleModifiers)
    {
        return MediaQuery("(min-width: 960px)", styleModifiers);
    }
    
    internal static int ProjectId = 1;
    
    public static string TryResolveColorInProject(string maybeNamedColor)
    {
        foreach (var realColor in ReactWithDotNet.VisualDesigner.Extensions.TryResolveColorInProject(ProjectId, maybeNamedColor))
        {
            return realColor;
        }

        return maybeNamedColor;
    }
    
   
    
    public const string textSecondary = "rgba(0, 0, 0, 0.6)";
    
    public static async NodeAnalyzeOutput With(this NodeAnalyzeOutput task, (string name, string package) tsImport)
    {
        var output = await task;
        if (!output.HasError)
        {
            output.Value.TsImportCollection.Add(tsImport.name, tsImport.package);
        }

        return output;
    }
    public static async NodeAnalyzeOutput With(this NodeAnalyzeOutput task, TsImportCollection tsImportCollection)
    {
        var output = await task;
        if (!output.HasError)
        {
            output.Value.TsImportCollection.Add(tsImportCollection);
        }

        return output;
    }
    
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
                    //ConnectionString = @"Data Source=D:\workgit\ReactWithDotNet.VisualDesigner\app.db"

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

    public static async NodeAnalyzeOutput AnalyzeChildren(NodeAnalyzeInput input, Func<NodeAnalyzeInput, NodeAnalyzeOutput> analyzeMethod)
    {
        var children = new List<ReactNode>();
        
        TsImportCollection tsImportCollection = new();

        foreach (var child in input.Node.Children)
        {
            var response = await analyzeMethod(input with { Node = child });
            if (response.HasError)
            {
                return response.Error;
            }

            children.Add(response.Value.Node);
            
            tsImportCollection.Add(response.Value.TsImportCollection);
        }

        var node = input.Node with
        {
            Children = children.ToImmutableList()
        };
        
        return (node, tsImportCollection);
    }

    public static NodeAnalyzeInput ApplyTranslateOperationOnProps(NodeAnalyzeInput input, params string[] propNames)
    {
        return input = input with
        {
            Node = ApplyTranslateOperationOnProps(input.Node, input.ComponentConfig, propNames)
        };
    }
    
    public static ReactNode ApplyTranslateOperationOnProps(ReactNode node, ComponentConfig componentConfig, params string[] propNames)
    {
        return node with
        {
            Properties = node.Properties.Select(x => AnalyzeTranslate(x, componentConfig, propNames)).ToImmutableList()
        };

        static ReactProperty AnalyzeTranslate(ReactProperty property, ComponentConfig componentConfig, IReadOnlyList<string> propNames)
        {
            if (!propNames.Contains(property.Name))
            {
                return property;
            }

            var (hasAnyChange, value) = ApplyTranslateOperation(componentConfig.Translate, property.Value);
            if (!hasAnyChange)
            {
                return property;
            }

            return property with
            {
                Value = value
            };
        }

        
    }
    
    public static (bool hasAnyChange, string value) ApplyTranslateOperation(string translate, string label)
    {
        var messagingRecords = GetMessagingByGroupName(translate).GetAwaiter().GetResult();

        var labelRawValue = TryClearStringValue(label);

        var propertyName = FirstOrDefaultOf(from m in messagingRecords where m.Description.Trim() == labelRawValue.Trim() select m.PropertyName);
        if (propertyName is null)
        {
            return (false, label);
        }

        return (true, $"getMessage(\"{propertyName}\")");
    }
    
    public static IReadOnlyList<string> GetUpdateStateLines(string jsVariableName, string jsValueName)
    {

        var result = CalculateUpdateStateLines(jsVariableName);
        if (result.isUpdateContainerState)
        {
            var stateName = result.stateName;

            return
            [
                $"  {jsVariableName} = {jsValueName};",
                $"  set{char.ToUpper(stateName[0]) + stateName[1..]}({{ ...{stateName} }});"
            ];
        }
        
        if (result.isUpdateState)
        {
            var stateName = result.stateName;

            return
            [
                $"  set{char.ToUpper(stateName[0]) + stateName[1..]}({jsValueName});"
            ];
        }

        if (result.noNeedToUpdateAnything)
        {
            return null;
        }

        return
        [
            $"  {jsVariableName} = {jsValueName};"
        ];
    }
    
    public static IReadOnlyList<string> GetUpdateStateLines(string jsVariableName1, string jsValueName1, 
                                                            string jsVariableName2, string jsValueName2)
    {

        var result1 = CalculateUpdateStateLines(jsVariableName1);
        var result2 = CalculateUpdateStateLines(jsVariableName2);
        
        if (result1.isUpdateContainerState && result2.isUpdateContainerState)
        {
            if (result1.stateName == result2.stateName)
            {
                var stateName = result1.stateName;

                return
                [
                    $"  {jsVariableName1} = {jsValueName1};",
                    $"  {jsVariableName2} = {jsValueName2};",
                    $"  set{char.ToUpper(stateName[0]) + stateName[1..]}({{ ...{stateName} }});"
                ];
            }
           
        }

        var list = new List<string>();
        
        if (result1.isUpdateState)
        {
            var stateName = result1.stateName;

            list.Add($"  set{char.ToUpper(stateName[0]) + stateName[1..]}({jsValueName1});");
        }
        else
        {
            list.Add(  $"  {jsVariableName1} = {jsValueName1};");
        }
        
        if (result2.isUpdateState)
        {
            var stateName = result2.stateName;

            list.Add($"  set{char.ToUpper(stateName[0]) + stateName[1..]}({jsValueName2});");
        }
        else
        {
            list.Add(  $"  {jsVariableName2} = {jsValueName2};");
        }

        return list;
    }
    
    static (
        bool isUpdateContainerState,
        bool isUpdateState, 
        string stateName,
        bool noNeedToUpdateAnything) 
        CalculateUpdateStateLines(string jsVariableName)
    {
        var propertyPath = jsVariableName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (propertyPath.Length == 2)
        {
            var stateName = propertyPath[0];

            return (isUpdateContainerState: true, isUpdateState: false,  stateName, noNeedToUpdateAnything: false);
        }
        
        if (propertyPath.Length == 1)
        {
            if (IsStringValue(propertyPath[0]))
            {
                return (isUpdateContainerState: false, isUpdateState: false,  null, noNeedToUpdateAnything: true);
            }
            
            var stateName = propertyPath[0];

            return (isUpdateContainerState: false, isUpdateState: true,  stateName, noNeedToUpdateAnything: false);
        }

        return (isUpdateContainerState: false, isUpdateState: false,  null, noNeedToUpdateAnything: false);
    }
    

    internal static Task<IReadOnlyList<MessagingRecord>> GetMessagingByGroupName(string messagingGroupName)
    {
        var cacheKey = $"{nameof(GetMessagingByGroupName)} :: {messagingGroupName}";

        return Cache.AccessValue(cacheKey, async () => await getMessagingByGroupName(messagingGroupName));

        static async Task<IReadOnlyList<MessagingRecord>> getMessagingByGroupName(string messagingGroupName)
        {
            var returnList = new List<MessagingRecord>();

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
                var propertyName = (string)reader["PropertyName"];
                var description = (string)reader["Description"];

                returnList.Add(new() { PropertyName = propertyName, Description = description });
            }

            reader.Close();

            return returnList;
        }
    }
    
    public static ReactNode UpdateProp(this ReactNode node, string propName, TsLineCollection lines)
    {
        var prop = node.Properties.FirstOrDefault(x => x.Name == propName);

        if (prop is null)
        {
            return node with
            {
                Properties = node.Properties.Add(new()
                {
                    Name  = propName,
                    Value = lines.ToTsCode()
                })
            };
        }

                
        prop = prop with
        {
            Value = lines.ToTsCode()
        };

        return node with
        {
            Properties = node.Properties.SetItem(node.Properties.FindIndex(x => x.Name == prop.Name), prop)
        };
                

    }

    internal static bool HasFunctionAssignment(this IEnumerable<ReactProperty> props, string propName)
    {
        return 
            (
                from property in props
                where property.Name == propName && property.Value.Contains(" => ")
                select property
            )
            .Any();
    }
    
}

delegate ReactNode TransformNode(ReactNode node);
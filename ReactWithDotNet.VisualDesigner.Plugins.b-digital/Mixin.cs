global using static ReactWithDotNet.VisualDesigner.Plugins.b_digital.Mixin;

global using NodeAnalyzeOutput= System.Threading.Tasks.Task<Toolbox.Result<ReactWithDotNet.VisualDesigner.Exporters.ReactNode>>;

using System.Collections.Immutable;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ReactWithDotNet.VisualDesigner.DbModels;
using ReactWithDotNet.VisualDesigner.Exporters;



namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

record MessagingRecord
{
    public string Description { get; init; }
    public string PropertyName { get; init; }
} 

static class Mixin
{
    public static ReactNode AnalyzeChildren(NodeAnalyzeInput input, Func<NodeAnalyzeInput, ReactNode> analyzeMethod)
    {
        return input.Node with
        {
            Children = input.Node.Children.Select(x => analyzeMethod(input with{Node = x})).ToImmutableList()
        };
    }
    
    public static async NodeAnalyzeOutput AnalyzeChildren(NodeAnalyzeInput input, Func<NodeAnalyzeInput, NodeAnalyzeOutput> analyzeMethod)
    {
        var chilren = new List<ReactNode>();

        foreach (var child in input.Node.Children)
        {
            var response = await analyzeMethod(input with { Node = child });
            if (response.HasError)
            {
                return response.Error;
            }
            
            chilren.Add(response.Value);
        }

        return input.Node with
        {
            Children = chilren.ToImmutableList()
        };
    }
    
    public const string textSecondary = "rgba(0, 0, 0, 0.6)";
    
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
        
        static (bool hasAnyChange, string value) ApplyTranslateOperation(string translate, string label)
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
    }
    
    
    [GetStringSuggestions]
    public static async Task<Result<IReadOnlyList<string>>> GetStringSuggestions(PropSuggestionScope scope)
    {
        var stringSuggestions = new List<string>();

        var messagingGroupName = scope.Component.Config.Translate;
        
        if (messagingGroupName.HasValue())
        {
            foreach (var item in await GetMessagingByGroupName(messagingGroupName))
            {
                stringSuggestions.Add(item.Description);
                stringSuggestions.Add($"${item.PropertyName}$ {item.Description}");
            }
        }

        return stringSuggestions;
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
    
 
    public static string GetUpdateStateLine(string jsVariableName)
    {
        var propertyPath = jsVariableName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (propertyPath.Length == 2)
        {
            var stateName = propertyPath[0];

            return $"  set{char.ToUpper(stateName[0]) + stateName[1..]}({{ ...{stateName} }});";
        }

        return null;
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
}
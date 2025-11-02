global using static ReactWithDotNet.VisualDesigner.Plugins.b_digital.Mixin;
using System.Data;
using System.IO;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

static class Mixin
{
    
    [TryFindAssemblyPath]
    public static string TryFindAssemblyPath(IReadOnlyDictionary<string, string> componentConfig, string dotNetFullTypeName)
    {
        if (componentConfig.TryGetValue("SolutionDirectory", out var solutionDirectory))
        {
            var solutionName = Path.GetFileName(solutionDirectory);

            return solutionDirectory + $@"\API\{solutionName}.API\bin\Debug\net8.0\{solutionName}.API.dll";
        }

        return null;
    }
    
    
    const string BOA_MessagingByGroupName = "BOA.MessagingByGroupName";
    
    [GetStringSuggestions]
    public static async Task<Result<IReadOnlyList<string>>> GetStringSuggestions(PropSuggestionScope scope)
    {
        var stringSuggestions = new List<string>();
        
        if (scope.Component.GetConfig().TryGetValue(BOA_MessagingByGroupName, out var messagingGroupName))
        {
            foreach (var item in await GetMessagingByGroupName(messagingGroupName))
            {
                stringSuggestions.Add(item.Description);
                stringSuggestions.Add($"${item.PropertyName}$ {item.Description}");
            }
        }

        return stringSuggestions;
    }
    
    record MessagingInfo
    {
        public string Description { get; init; }
        public string PropertyName { get; init; }
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
    
    [AnalyzeExportFilePath]
    public static  Scope AnalyzeExportFilePath(Scope scope)
    {
        var exportFilePathForComponent = Plugin.ExportFilePathForComponent[scope];

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
                return Scope.Create(new()
                {
                    { Plugin.ExportFilePathForComponent, Path.Combine(clientAppFolderPath, Path.Combine(names.Skip(1).ToArray())) }
                });
                 
            }
        }

        return Scope.Empty;
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
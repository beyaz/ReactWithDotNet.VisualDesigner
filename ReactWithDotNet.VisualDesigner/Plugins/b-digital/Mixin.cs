global using static ReactWithDotNet.VisualDesigner.Plugins.b_digital.Mixin;
using System.IO;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

static class Mixin
{
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
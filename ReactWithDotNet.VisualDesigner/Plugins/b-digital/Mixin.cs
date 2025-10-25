global using static ReactWithDotNet.VisualDesigner.Plugins.b_digital.Mixin;

namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

static class Mixin
{
    
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
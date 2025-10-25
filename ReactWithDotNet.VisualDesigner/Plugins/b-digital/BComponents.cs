namespace ReactWithDotNet.VisualDesigner.Plugins.b_digital;

static class BComponents
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
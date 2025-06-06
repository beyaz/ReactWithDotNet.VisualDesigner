using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

static class SyncHelper
{
    public static void Run()
    {
        Transfer_From_SQLite_to_SqlServer("Data Source=C:\\github\\ReactWithDotNet.VisualDesigner\\app.db",
                                                     "Server=tcp:beyaz.database.windows.net,1433;Initial Catalog=ReactVisualDesigner;Persist Security Info=False;User ID=beyaz;Password=t5U7*n_5fHJ_r-yU;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        
    }
    public static async Task Transfer_From_SQLite_to_SqlServer(string sqliteConnection, string sqlConnection)
    {
        IDbConnection source = new SqliteConnection(sqliteConnection);

        IDbConnection target = new SqlConnection(sqlConnection);

        var projects = await source.GetAllAsync<ProjectEntity>();

        var components = await source.GetAllAsync<ComponentEntity>();

        var users = await source.GetAllAsync<UserEntity>();

        var componentHistories = await source.GetAllAsync<ComponentHistoryEntity>();

        var workspaces = await source.GetAllAsync<ComponentWorkspace>();

        DommelMapper.SetTableNameResolver(new TableNameResolverForSqlServer());
        try
        {
            
            await target.InsertAllAsync(projects);

            await target.InsertAllAsync(components);

            await target.InsertAllAsync(users);

            await target.InsertAllAsync(componentHistories);

            await target.InsertAllAsync(workspaces);
        }
        catch (Exception e)
        {
            e.ToString();

        }
    }

    class TableNameResolverForSqlServer : ITableNameResolver
    {
        public string ResolveTableName(Type type)
        {
            
            var tableAttribute = type.GetCustomAttribute<TableAttribute>();
        
            var tableName = tableAttribute?.Name ?? type.Name;
            
            
            return "RVD." + tableName;
        }
    }
}
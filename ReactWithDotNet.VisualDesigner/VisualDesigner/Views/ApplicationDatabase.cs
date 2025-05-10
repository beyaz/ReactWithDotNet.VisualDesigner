using System.Data;
using Dommel;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationDatabase
{
    public static readonly string AppDirectory = @"C:\github\ReactWithDotNet.VisualDesigner\";

    static string ConnectionString => $"Data Source={AppDirectory}app.db";

    static IReadOnlyList<ProjectEntity> Projects { get; set; }

    public static void DbOperation(Action<IDbConnection> operation)
    {
        using IDbConnection connection = new SqliteConnection(ConnectionString);

        operation(connection);
    }

    public static T DbOperation<T>(Func<IDbConnection, T> operation)
    {
        using IDbConnection connection = new SqliteConnection(ConnectionString);

        return operation(connection);
    }
    
    public static async Task DbOperation(Func<IDbConnection, Task> operation)
    {
        using IDbConnection connection = new SqliteConnection(ConnectionString);

        await operation(connection);
    }
    
    

    public static async Task<T> DbOperation<T>(Func<IDbConnection, Task<T>> operation)
    {
        using IDbConnection connection = new SqliteConnection(ConnectionString);

        return await operation(connection);
    }

    public static IReadOnlyList<ProjectEntity> GetAllProjects()
    {
        if (Projects is null)
        {
            const string query = "SELECT * FROM Project";

            DbOperation(connection => { Projects = connection.QueryAsync<ProjectEntity>(query).GetAwaiter().GetResult().ToList(); });
        }

        return Projects;
    }
    
    public static async Task<IReadOnlyList<UserEntity>> GetLastUsageInfoByUserName(string userName)
    {
        return (await DbOperation(async db =>  await db.SelectAsync<UserEntity>(x=>x.UserName == userName))).ToList();
    }
    
    public static async Task<ComponentEntity> GetFirstComponentInProject(int projectId)
    {
        const string query = $"select * from Component WHERE ProjectId = @{nameof(projectId)} LIMIT 1";

        return await DbOperation(async db =>  await db.QueryFirstOrDefaultAsync<ComponentEntity>(query, new{ projectId}));
    }
    
    public static async Task<int?> GetFirstProjectId()
    {
        const string query = "select * from Project LIMIT 1";

        return (await DbOperation(async db =>  await db.QueryFirstOrDefaultAsync<ProjectEntity>(query)))?.Id;
    }
    
    public static Task<IEnumerable<ComponentEntity>> GetAllComponentsInProject(int projectId)
    {
        const string query = $"SELECT * FROM Component WHERE {nameof(ComponentEntity.ProjectId)} = @{nameof(projectId)}";

        return DbOperation(db => db.QueryAsync<ComponentEntity>(query, new{projectId}));
    }
}
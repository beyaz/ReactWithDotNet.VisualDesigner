using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationDatabase
{
    static readonly string CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ReactVisualDesigner") +
                                            Path.DirectorySeparatorChar;

    static string ConnectionString => $"Data Source={CacheDirectory}app.db";

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
    
    public static async Task<IReadOnlyList<LastUsageInfoEntity>> GetLastUsageInfoByUserName(string userName)
    {
        const string query = $"SELECT * FROM LastUsageInfo WHERE UserName = @{nameof(userName)} Order BY {nameof(LastUsageInfoEntity.LastAccessTime)} DESC";

        return (await DbOperation(async db =>  await db.QueryAsync<LastUsageInfoEntity>(query, new{ userName}))).ToList();
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
}
using System.Data;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationDatabase
{
    static string ConnectionString => Config.ConnectionString;

  

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

    public static Task<IEnumerable<ComponentEntity>> GetAllComponentsInProject(int projectId)
    {
        return DbOperation(db => db.SelectAsync<ComponentEntity>(x => x.ProjectId == projectId));
    }

    public static IReadOnlyList<ProjectEntity> GetAllProjects()
    {
        return Cache.AccessValue(nameof(GetAllProjects), () => { return DbOperation(connection => connection.GetAll<ProjectEntity>().ToList()); });
    }

    public static async Task<ComponentEntity> GetFirstComponentInProject(int projectId)
    {
        return await DbOperation(async db => await db.FirstOrDefaultAsync<ComponentEntity>(x => x.ProjectId == projectId));
    }

    public static async Task<int?> GetFirstProjectId()
    {
        return (await DbOperation(async db => await db.FirstOrDefaultAsync<ProjectEntity>(x => true)))?.Id;
    }

    public static async Task<IReadOnlyList<UserEntity>> GetLastUsageInfoByUserName(string userName)
    {
        return (await DbOperation(async db => await db.SelectAsync<UserEntity>(x => x.UserName == userName))).ToList();
    }
}
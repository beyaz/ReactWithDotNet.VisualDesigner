using System.Data;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationDatabase
{
    static string ConnectionString => Config.ConnectionString;
    

    public static async Task<T> DbOperation<T>(Func<IDbConnection, Task<T>> operation)
    {
        using IDbConnection connection = new SqliteConnection(ConnectionString);

        return await operation(connection);
    }

    

    public static IReadOnlyList<ProjectEntity> GetAllProjects()
    {
        return Cache.AccessValue(nameof(GetAllProjects),
                                 () => Store.GetAllProjects().GetAwaiter().GetResult().ToList());
    }

   

    public static async Task<IReadOnlyList<UserEntity>> GetLastUsageInfoByUserName(string userName)
    {
        return (await DbOperation(async db => await db.SelectAsync<UserEntity>(x => x.UserName == userName))).ToList();
    }
}
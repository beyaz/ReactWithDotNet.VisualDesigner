using System.Data;
using Dommel;
using Microsoft.Data.Sqlite;

namespace ReactWithDotNet.VisualDesigner.Views;

static class ApplicationDatabase
{
    public static readonly string AppDirectory = @"C:\github\ReactWithDotNet.VisualDesigner\";

    static string ConnectionString => $"Data Source={AppDirectory}app.db";


    public static UserEntity GetUser(int projectId, string userName)
    {
        return DbOperation(db => db.FirstOrDefault<UserEntity>(x => x.ProjectId == projectId && x.UserName == userName));
    }

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
            DbOperation(connection => { Projects = connection.GetAll<ProjectEntity>().ToList(); });
        }

        return Projects;
    }
    
    public static async Task<IReadOnlyList<UserEntity>> GetLastUsageInfoByUserName(string userName)
    {
        return (await DbOperation(async db =>  await db.SelectAsync<UserEntity>(x=>x.UserName == userName))).ToList();
    }
    
    public static async Task<ComponentEntity> GetFirstComponentInProject(int projectId)
    {
        return await DbOperation(async db =>  await db.FirstOrDefaultAsync<ComponentEntity>(x=>x.ProjectId == projectId));
    }
    
   
    
    public static async Task<int?> GetFirstProjectId()
    {
        return (await DbOperation(async db =>  await db.FirstOrDefaultAsync<ProjectEntity>(x=>true)))?.Id;
    }
    
    public static Task<IEnumerable<ComponentEntity>> GetAllComponentsInProject(int projectId)
    {
        return DbOperation(db => db.SelectAsync<ComponentEntity>(x=>x.ProjectId == projectId));
    }
}
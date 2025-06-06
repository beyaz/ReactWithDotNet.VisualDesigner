using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace ReactWithDotNet.VisualDesigner.DataAccess;

static class Store
{
    static Store()
    {
        DommelMapper.SetTableNameResolver(new TableNameResolver());
    }
    public static Task Delete(ComponentEntity entity)
    {
        return DbOperation(db => db.DeleteAsync(entity));
    }

    public static Task Delete(ComponentWorkspace entity)
    {
        return DbOperation(db => db.DeleteAsync(entity));
    }

    public static Task<IEnumerable<ComponentEntity>> GetAllComponentsInProject(int projectId)
    {
        return DbOperation(db => db.SelectAsync<ComponentEntity>(x => x.ProjectId == projectId));
    }

    public static Task<IEnumerable<ProjectEntity>> GetAllProjects()
    {
        return DbOperation(db => db.GetAllAsync<ProjectEntity>());
    }

    public static Task<IEnumerable<ComponentWorkspace>> GetComponentWorkspaces(int componentId)
    {
        return DbOperation(db => db.SelectAsync<ComponentWorkspace>(x => x.ComponentId == componentId));
    }

    public static async Task<ComponentEntity> GetFirstComponentInProject(int projectId)
    {
        return await DbOperation(async db => await db.FirstOrDefaultAsync<ComponentEntity>(x => x.ProjectId == projectId));
    }

    public static async Task<int?> GetFirstProjectId()
    {
        return (await DbOperation(async db => await db.FirstOrDefaultAsync<ProjectEntity>(x => true)))?.Id;
    }

    public static Task<IEnumerable<UserEntity>> GetUserByUserName(string userName)
    {
        return DbOperation(db => db.SelectAsync<UserEntity>(x => x.UserName == userName));
    }

    public static async Task<long> Insert(UserEntity entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }

    public static async Task<long> Insert(ComponentEntity entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }

    public static Task Insert(ComponentHistoryEntity entity)
    {
        return DbOperation(db => db.InsertAsync(entity));
    }

    public static async Task<long> Insert(ComponentWorkspace entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }

    public static async Task<ComponentEntity> TryGetComponent(int componentId)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == componentId));
    }

    public static async Task<ComponentWorkspace> TryGetComponentWorkspace(int componentId, string userName)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ComponentWorkspace>(x => x.ComponentId == componentId && x.UserName == userName));
    }

    public static async Task<ProjectEntity> TryGetProject(int projectId)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ProjectEntity>(x => x.Id == projectId));
    }

    public static async Task<UserEntity> TryGetUser(int projectId, string userName)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<UserEntity>(x => x.ProjectId == projectId && x.UserName == userName));
    }

    public static async Task<bool> Update(ComponentWorkspace entity)
    {
        return await DbOperation(db => db.UpdateAsync(entity));
    }

    public static async Task<bool> Update(ProjectEntity entity)
    {
        return await DbOperation(db => db.UpdateAsync(entity));
    }

    public static async Task<bool> Update(UserEntity entity)
    {
        return await DbOperation(db => db.UpdateAsync(entity));
    }

    public static async Task<bool> Update(ComponentEntity entity)
    {
        return await DbOperation(db => db.UpdateAsync(entity));
    }

    static async Task<T> DbOperation<T>(Func<IDbConnection, Task<T>> operation)
    {
        if (Config.Database.IsSQLite)
        {
            using IDbConnection connection = new SqliteConnection(Config.Database.ConnectionString);

            return await operation(connection);    
        }
        
        if (Config.Database.IsSQLServer)
        {
            using IDbConnection connection = new SqlConnection(Config.Database.ConnectionString);

            return await operation(connection);    
        }
        
        if (Config.Database.IsMySQL)
        {
            using IDbConnection connection = new MySqlConnection(Config.Database.ConnectionString);

            return await operation(connection);    
        }
        
        throw new NotSupportedException("Database type is not supported.");

    }
}
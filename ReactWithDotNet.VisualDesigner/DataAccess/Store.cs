namespace ReactWithDotNet.VisualDesigner.DataAccess;

static class Store
{
    public static Task<IEnumerable<ComponentEntity>> GetAllComponentsInProject(int projectId)
    {
        return DbOperation(db => db.SelectAsync<ComponentEntity>(x => x.ProjectId == projectId));
    }
    
    public static async Task<long> Insert(UserEntity entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }
    
    public static Task Delete(ComponentEntity entity)
    {
        return DbOperation(db => db.DeleteAsync(entity));
    }
    public static Task Delete(ComponentWorkspace entity)
    {
        return DbOperation(db => db.DeleteAsync(entity));
    }
    public static Task Insert(ComponentHistoryEntity entity)
    {
        return DbOperation(db => db.InsertAsync(entity));
    }
    
    public static async Task<long> Insert(ComponentWorkspace entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }
    
    public static async Task<bool> Update(ComponentWorkspace entity)
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
    
    public static async Task<UserEntity> TryGetUser(int projectId, string userName)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<UserEntity>(x => x.ProjectId == projectId && x.UserName == userName));
    }
    
    public static async Task<ComponentWorkspace> TryGetComponentWorkspace(int componentId, string userName)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ComponentWorkspace>(x => x.ComponentId == componentId && x.UserName == userName));
    }
    
    public static Task<IEnumerable<ComponentWorkspace>> GetComponentWorkspaces(int componentId)
    {
        return DbOperation(db => db.SelectAsync<ComponentWorkspace>(x => x.ComponentId == componentId));
    }
    
    public static async Task<ComponentEntity> TryGetComponent(int componentId)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == componentId));
    }
    
    public static async Task<ProjectEntity> TryGetProject(int projectId)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ProjectEntity>(x => x.Id == projectId));
    }
    
    public static Task<IEnumerable<ProjectEntity>> GetAllProjects()
    {
        return DbOperation(connection => connection.GetAllAsync<ProjectEntity>());
    }
}
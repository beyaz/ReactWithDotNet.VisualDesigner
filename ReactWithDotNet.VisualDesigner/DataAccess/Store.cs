namespace ReactWithDotNet.VisualDesigner.DataAccess;

sealed record Store
{
    public async Task<long> Insert(UserEntity entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }
    
    public async Task<long> Insert(ComponentWorkspace entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }
    
    public async Task<bool> Update(ComponentWorkspace entity)
    {
        return await DbOperation(db => db.UpdateAsync(entity));
    }
    
    public async Task<bool> Update(UserEntity entity)
    {
        return await DbOperation(db => db.UpdateAsync(entity));
    }
    
    public async Task<UserEntity> TryGetUser(int projectId, string userName)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<UserEntity>(x => x.ProjectId == projectId && x.UserName == userName));
    }
    
    public async Task<ComponentWorkspace> TryGetComponentWorkspace(int componentId, string userName)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ComponentWorkspace>(x => x.ComponentId == componentId && x.UserName == userName));
    }
    
    public async Task<ComponentEntity> TryGetComponent(int componentId)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<ComponentEntity>(x => x.Id == componentId));
    }
}
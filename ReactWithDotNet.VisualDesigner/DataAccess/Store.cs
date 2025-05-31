namespace ReactWithDotNet.VisualDesigner.DataAccess;

sealed record Store
{
    public async Task<long> Insert(UserEntity entity)
    {
        return (long)await DbOperation(db => db.InsertAsync(entity));
    }
    
    public async Task<bool> Update(UserEntity entity)
    {
        return await DbOperation(db => db.UpdateAsync(entity));
    }
    
    public async Task<UserEntity> TryGetUser(int projectId, string userName)
    {
        return await DbOperation(db => db.FirstOrDefaultAsync<UserEntity>(x => x.ProjectId == projectId && x.UserName == userName));
    }
}
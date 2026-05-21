namespace Base.Contracts;

public interface IBaseService<TBllEntity>
    where TBllEntity : class, IBaseEntity
{
    public Task<IEnumerable<TBllEntity>> AllAsync();
    public Task<TBllEntity?> FindAsync(Guid id);
    public TBllEntity Add(TBllEntity entity);
    public TBllEntity Update(TBllEntity entity);
    public void Remove(TBllEntity entity);
    public Task<bool> ExistsAsync(Guid id);
}

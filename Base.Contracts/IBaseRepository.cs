namespace Base.Contracts;

public interface IBaseRepository<TEntity>
    where TEntity : class, IBaseEntity
{
    public Task<IEnumerable<TEntity>> AllAsync();
    public Task<TEntity?> FindAsync(Guid id);
    public TEntity Add(TEntity entity);
    public TEntity Update(TEntity entity);
    public void Remove(TEntity entity);
    public Task<bool> ExistsAsync(Guid id);
}

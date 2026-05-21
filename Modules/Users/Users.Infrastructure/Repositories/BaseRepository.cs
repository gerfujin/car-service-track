using Base.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Users.Infrastructure.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class, IBaseEntity
{
    protected readonly UsersDbContext _context;
    protected readonly DbSet<TEntity> DbSet;

    public BaseRepository(UsersDbContext context)
    {
        _context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<IEnumerable<TEntity>> AllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<TEntity?> FindAsync(Guid id)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    public virtual TEntity Add(TEntity entity)
    {
        return DbSet.Add(entity).Entity;
    }

    public virtual TEntity Update(TEntity entity)
    {
        return DbSet.Update(entity).Entity;
    }

    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(e => e.Id == id);
    }
}

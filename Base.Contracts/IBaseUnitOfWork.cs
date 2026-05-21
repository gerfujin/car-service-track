namespace Base.Contracts;

public interface IBaseUnitOfWork
{
    public Task<int> SaveChangesAsync();
}

using App.DAL.Contracts;
using App.Domain;

namespace App.DAL.EF.Repositories;

public class ServiceRepository : BaseRepository<Service>, IServiceRepository
{
    public ServiceRepository(AppDbContext context) : base(context)
    {
    }
}

using Workshops.Contracts.Repositories;
using Workshops.Domain;

namespace Workshops.Infrastructure.Repositories;

public class ServiceRepository : BaseRepository<Service>, IServiceRepository
{
    public ServiceRepository(WorkshopsDbContext context) : base(context)
    {
    }
}

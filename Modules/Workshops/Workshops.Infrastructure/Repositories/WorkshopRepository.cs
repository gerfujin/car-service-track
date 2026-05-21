using Workshops.Contracts.Repositories;
using Workshops.Domain;

namespace Workshops.Infrastructure.Repositories;

public class WorkshopRepository : BaseRepository<Workshop>, IWorkshopRepository
{
    public WorkshopRepository(WorkshopsDbContext context) : base(context)
    {
    }
}

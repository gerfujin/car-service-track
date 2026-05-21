using Workshops.Contracts.Repositories;
using Workshops.Domain;

namespace Workshops.Infrastructure.Repositories;

public class MechanicInWorkshopRepository : BaseRepository<MechanicInWorkshop>, IMechanicInWorkshopRepository
{
    public MechanicInWorkshopRepository(WorkshopsDbContext context) : base(context)
    {
    }
}

using Workshops.Contracts.Repositories;
using Workshops.Domain;

namespace Workshops.Infrastructure.Repositories;

public class MechanicRepository : BaseRepository<Mechanic>, IMechanicRepository
{
    public MechanicRepository(WorkshopsDbContext context) : base(context)
    {
    }
}

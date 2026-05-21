using Workshops.Contracts.Repositories;
using Workshops.Domain;

namespace Workshops.Infrastructure.Repositories;

public class SparePartRepository : BaseRepository<SparePart>, ISparePartRepository
{
    public SparePartRepository(WorkshopsDbContext context) : base(context)
    {
    }
}

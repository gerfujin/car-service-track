using App.DAL.Contracts;
using App.Domain;

namespace App.DAL.EF.Repositories;

public class SparePartRepository : BaseRepository<SparePart>, ISparePartRepository
{
    public SparePartRepository(AppDbContext context) : base(context)
    {
    }
}

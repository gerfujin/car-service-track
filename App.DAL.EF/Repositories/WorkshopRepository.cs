using App.DAL.Contracts;
using App.Domain;

namespace App.DAL.EF.Repositories;

public class WorkshopRepository : BaseRepository<Workshop>, IWorkshopRepository
{
    public WorkshopRepository(AppDbContext context) : base(context)
    {
    }
}

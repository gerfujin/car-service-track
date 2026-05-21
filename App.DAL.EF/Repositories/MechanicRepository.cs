using App.DAL.Contracts;
using App.Domain;

namespace App.DAL.EF.Repositories;

public class MechanicRepository : BaseRepository<Mechanic>, IMechanicRepository
{
    public MechanicRepository(AppDbContext context) : base(context)
    {
    }
}

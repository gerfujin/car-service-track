using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

// The controller only needs standard CRUD, so the generic base contract is sufficient.
public interface IWorkshopRepository : IBaseRepository<Workshop>
{
}

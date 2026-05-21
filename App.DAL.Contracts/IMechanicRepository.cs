using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

// The API controller only needs standard read operations; the generic base contract is sufficient.
public interface IMechanicRepository : IBaseRepository<Mechanic>
{
}

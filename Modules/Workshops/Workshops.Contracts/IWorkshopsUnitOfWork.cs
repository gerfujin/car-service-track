using Base.Contracts;
using Workshops.Contracts.Repositories;

namespace Workshops.Contracts;

public interface IWorkshopsUnitOfWork : IBaseUnitOfWork
{
    IWorkshopRepository Workshops { get; }
    IMechanicRepository Mechanics { get; }
    IMechanicInWorkshopRepository MechanicsInWorkshops { get; }
    IServiceRepository Services { get; }
    ISparePartRepository SpareParts { get; }
}

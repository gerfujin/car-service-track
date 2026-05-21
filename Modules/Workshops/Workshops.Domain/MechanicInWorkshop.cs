using Base.Domain;

namespace Workshops.Domain;

public class MechanicInWorkshop : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Guid MechanicId { get; set; }
    public Mechanic? Mechanic { get; set; }

    public Guid WorkshopId { get; set; }
    public Workshop? Workshop { get; set; }
}

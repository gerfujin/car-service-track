using Base.Domain;

namespace App.Domain;

public class MechanicInWorkshop : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // FK
    public Guid MechanicId { get; set; }
    public Mechanic? Mechanic { get; set; }

    public Guid WorkshopId { get; set; }
    public Workshop? Workshop { get; set; }
}

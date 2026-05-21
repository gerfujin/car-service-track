using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class ListItemMapper
{
    public static BllListItem? ToBll(ListItem? entity)
    {
        if (entity == null) return null;
        return new BllListItem
        {
            Id = entity.Id,
            ItemDescription = entity.ItemDescription,
            Summary = entity.Summary,
            IsDone = entity.IsDone,
            AppUserId = entity.AppUserId,
            AppUserEmail = entity.AppUser?.Email
        };
    }

    public static ListItem? ToDomain(BllListItem? bll)
    {
        if (bll == null) return null;
        return new ListItem
        {
            Id = bll.Id,
            ItemDescription = bll.ItemDescription,
            Summary = bll.Summary,
            IsDone = bll.IsDone,
            AppUserId = bll.AppUserId
        };
    }
}

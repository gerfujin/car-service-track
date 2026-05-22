using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Helpers;

public static class SelectListMappingExtensions
{
    public static List<SelectListItem> ToSelectListItems(this IEnumerable<Orders.Application.DTO.BllSelectListItem> items)
    {
        return items.Select(x => new SelectListItem
        {
            Value = x.Value,
            Text = x.Text,
            Selected = x.Selected
        }).ToList();
    }
}

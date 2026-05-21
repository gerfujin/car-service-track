using System.Security.Claims;
using App.BLL;
using App.BLL.DTO;
using Base.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    [Authorize]
    public class ListItemsController : Controller
    {
        private readonly IAppBll _bll;

        public ListItemsController(IAppBll bll)
        {
            _bll = bll;
        }

        private Guid GetUserId()
        {
            var userIdString = User.Claims.First(c =>
                c.Type == ClaimTypes.NameIdentifier).Value;
            return Guid.Parse(userIdString);
        }

        // GET: ListItems
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var items = await _bll.ListItems.AllByUserAsync(userId);

            var vm = items.Select(ToIndexItemVm).ToList();
            return View(vm);
        }

        // GET: ListItems/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var userId = GetUserId();
            var item = await _bll.ListItems.FindByUserAsync(id.Value, userId);
            if (item == null) return NotFound();

            return View(ToDetailsVm(item));
        }

        // GET: ListItems/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ListItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListItemCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var bllItem = new BllListItem
            {
                ItemDescription = vm.ItemDescription,
                Summary = new LangStr(vm.Summary),
                IsDone = vm.IsDone,
                AppUserId = GetUserId()
            };

            _bll.ListItems.Add(bllItem);
            await _bll.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: ListItems/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var userId = GetUserId();
            var item = await _bll.ListItems.FindByUserAsync(id.Value, userId);
            if (item == null) return NotFound();

            var vm = new ListItemEditViewModel
            {
                Id = item.Id,
                ItemDescription = item.ItemDescription,
                Summary = item.Summary.ToString(),
                IsDone = item.IsDone
            };

            return View(vm);
        }

        // POST: ListItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ListItemEditViewModel vm)
        {
            if (id != vm.Id) return NotFound();
            if (!ModelState.IsValid) return View(vm);

            var existing = await _bll.ListItems.FindAsync(id);
            if (existing == null) return NotFound();

            existing.ItemDescription = vm.ItemDescription;
            existing.IsDone = vm.IsDone;
            existing.Summary = new LangStr(vm.Summary);

            await _bll.ListItems.UpdateAsync(existing);
            await _bll.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: ListItems/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var userId = GetUserId();
            var item = await _bll.ListItems.FindByUserAsync(id.Value, userId);
            if (item == null) return NotFound();

            return View(ToDeleteVm(item));
        }

        // POST: ListItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var userId = GetUserId();
            var item = await _bll.ListItems.FindByUserAsync(id, userId);

            if (item != null)
            {
                _bll.ListItems.Remove(item);
                await _bll.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ---- Mapping helpers ----

        private static ListItemIndexItemViewModel ToIndexItemVm(BllListItem item) => new()
        {
            Id = item.Id,
            ItemDescription = item.ItemDescription,
            Summary = item.Summary.ToString(),
            IsDone = item.IsDone,
            AppUserEmail = item.AppUserEmail
        };

        private static ListItemDetailsViewModel ToDetailsVm(BllListItem item) => new()
        {
            Id = item.Id,
            ItemDescription = item.ItemDescription,
            Summary = item.Summary.ToString(),
            IsDone = item.IsDone,
            AppUserEmail = item.AppUserEmail
        };

        private static ListItemDeleteViewModel ToDeleteVm(BllListItem item) => new()
        {
            Id = item.Id,
            ItemDescription = item.ItemDescription,
            Summary = item.Summary.ToString(),
            IsDone = item.IsDone,
            AppUserEmail = item.AppUserEmail
        };
    }
}
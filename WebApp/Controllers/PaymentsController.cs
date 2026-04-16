using App.DAL.EF;
using App.Domain.Enums;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels.Client;

namespace WebApp.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    // GET: /Payments — all roles
    public async Task<IActionResult> Index()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

        var query = _context.Payments
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .AsQueryable();

        if (!IsAdmin() && !IsMechanic())
        {
            // Client: own payments only
            var owner = await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);
            if (owner == null)
                return View(new PaymentClientListViewModel());
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentClientViewModel
            {
                Id = p.Id,
                Amount = p.Amount,
                Status = p.Status,
                PaidAt = p.PaidAt,
                PaymentMethod = p.PaymentMethod,
                Notes = p.Notes,
                ServiceOrderId = p.ServiceOrderId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return View(new PaymentClientListViewModel { Payments = payments });
    }
}

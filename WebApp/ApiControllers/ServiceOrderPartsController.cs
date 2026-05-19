using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.ServiceOrderPart;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ServiceOrderPartsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServiceOrderPartsController(AppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private async Task<Owner?> GetCurrentOwnerAsync()
    {
        var userId = GetCurrentUserId();
        return await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    [HttpGet]
    [ProducesResponseType<IEnumerable<ServiceOrderPartDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ServiceOrderPartDto>>> GetServiceOrderParts([FromQuery] Guid? serviceOrderId)
    {
        var query = _context.ServiceOrderParts
            .Include(sop => sop.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Include(sop => sop.SparePart)
            .AsQueryable();

        if (serviceOrderId.HasValue)
        {
            query = query.Where(sop => sop.ServiceOrderId == serviceOrderId.Value);
        }

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return Ok(new List<ServiceOrderPartDto>());

            query = query.Where(sop => sop.ServiceOrder != null && sop.ServiceOrder.Vehicle != null && sop.ServiceOrder.Vehicle.OwnerId == owner.Id);
        }

        var parts = await query
            .Select(sop => new ServiceOrderPartDto
            {
                Id = sop.Id,
                ServiceOrderId = sop.ServiceOrderId,
                SparePartId = sop.SparePartId,
                SparePartName = sop.SparePart != null ? (sop.SparePart.Name.Translate() ?? sop.SparePart.Name.ToString()) : null,
                SparePartPartNumber = sop.SparePart != null ? sop.SparePart.PartNumber : null,
                Quantity = sop.Quantity,
                Price = sop.UnitPrice,
                LineTotal = sop.Quantity * sop.UnitPrice
            })
            .ToListAsync();

        return Ok(parts);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServiceOrderPartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceOrderPartDto>> GetServiceOrderPart(Guid id)
    {
        var sop = await _context.ServiceOrderParts
            .Include(x => x.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Include(x => x.SparePart)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (sop == null) return NotFound();

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null || sop.ServiceOrder?.Vehicle?.OwnerId != owner.Id)
            {
                return NotFound();
            }
        }

        return Ok(new ServiceOrderPartDto
        {
            Id = sop.Id,
            ServiceOrderId = sop.ServiceOrderId,
            SparePartId = sop.SparePartId,
            SparePartName = sop.SparePart != null ? (sop.SparePart.Name.Translate() ?? sop.SparePart.Name.ToString()) : null,
            SparePartPartNumber = sop.SparePart != null ? sop.SparePart.PartNumber : null,
            Quantity = sop.Quantity,
            Price = sop.UnitPrice,
            LineTotal = sop.Quantity * sop.UnitPrice
        });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType<ServiceOrderPartDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceOrderPartDto>> CreateServiceOrderPart([FromBody] ServiceOrderPartCreateDto dto)
    {
        var order = await _context.ServiceOrders.FindAsync(dto.ServiceOrderId);
        if (order == null)
        {
            return BadRequest(new RestApiErrorResponse { Status = System.Net.HttpStatusCode.BadRequest, Error = "Service order not found." });
        }

        var sparePart = await _context.SpareParts.FindAsync(dto.SparePartId);
        if (sparePart == null)
        {
            return BadRequest(new RestApiErrorResponse { Status = System.Net.HttpStatusCode.BadRequest, Error = "Spare part not found." });
        }

        if (dto.Quantity <= 0)
        {
            return BadRequest(new RestApiErrorResponse { Status = System.Net.HttpStatusCode.BadRequest, Error = "Quantity must be greater than 0." });
        }

        var effectivePrice = dto.Price <= 0 ? sparePart.UnitPrice : dto.Price;
        var entity = new ServiceOrderPart
        {
            ServiceOrderId = dto.ServiceOrderId,
            SparePartId = dto.SparePartId,
            Quantity = dto.Quantity,
            UnitPrice = effectivePrice
        };

        _context.ServiceOrderParts.Add(entity);
        await _context.SaveChangesAsync();
        await RecalculateOrderTotalAsync(order.Id);

        var result = new ServiceOrderPartDto
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            SparePartName = sparePart.Name.Translate() ?? sparePart.Name.ToString(),
            SparePartPartNumber = sparePart.PartNumber,
            Quantity = entity.Quantity,
            Price = entity.UnitPrice,
            LineTotal = entity.Quantity * entity.UnitPrice
        };

        return CreatedAtAction(nameof(GetServiceOrderPart), new { id = entity.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateServiceOrderPart(Guid id, [FromBody] ServiceOrderPartUpdateDto dto)
    {
        var entity = await _context.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        if (dto.Price < 0)
        {
            return BadRequest(new RestApiErrorResponse { Status = System.Net.HttpStatusCode.BadRequest, Error = "Price must be greater than or equal to 0." });
        }

        var order = await _context.ServiceOrders.FindAsync(dto.ServiceOrderId);
        if (order == null)
        {
            return BadRequest(new RestApiErrorResponse { Status = System.Net.HttpStatusCode.BadRequest, Error = "Service order not found." });
        }

        var sparePart = await _context.SpareParts.FindAsync(dto.SparePartId);
        if (sparePart == null)
        {
            return BadRequest(new RestApiErrorResponse { Status = System.Net.HttpStatusCode.BadRequest, Error = "Spare part not found." });
        }

        if (dto.Quantity <= 0)
        {
            return BadRequest(new RestApiErrorResponse { Status = System.Net.HttpStatusCode.BadRequest, Error = "Quantity must be greater than 0." });
        }

        entity.ServiceOrderId = dto.ServiceOrderId;
        entity.SparePartId = dto.SparePartId;
        entity.Quantity = dto.Quantity;
        entity.UnitPrice = dto.Price <= 0 ? sparePart.UnitPrice : dto.Price;

        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        await RecalculateOrderTotalAsync(entity.ServiceOrderId);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteServiceOrderPart(Guid id)
    {
        var entity = await _context.ServiceOrderParts.FindAsync(id);
        if (entity == null) return NotFound();

        var orderId = entity.ServiceOrderId;
        _context.ServiceOrderParts.Remove(entity);
        await _context.SaveChangesAsync();
        await RecalculateOrderTotalAsync(orderId);

        return NoContent();
    }

    private async Task RecalculateOrderTotalAsync(Guid serviceOrderId)
    {
        var order = await _context.ServiceOrders
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .FirstOrDefaultAsync(so => so.Id == serviceOrderId);

        if (order == null) return;

        var itemsTotal = order.ServiceOrderItems?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
        var partsTotal = order.ServiceOrderParts?.Sum(p => p.Quantity * p.UnitPrice) ?? 0;
        order.FinalPrice = itemsTotal + partsTotal;
        order.UpdatedAt = DateTime.UtcNow;

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}

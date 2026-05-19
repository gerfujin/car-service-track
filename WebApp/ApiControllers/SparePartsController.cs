using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.SparePart;
using Asp.Versioning;
using Base.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SparePartsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SparePartsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Get all spare parts (admin only)</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<SparePartDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SparePartDto>>> GetSpareParts()
    {
        var entities = await _context.SpareParts.ToListAsync();
        var parts = entities.Select(sp => new SparePartDto
        {
            Id = sp.Id,
            Name = sp.Name.Translate() ?? sp.Name.ToString() ?? "",
            PartNumber = sp.PartNumber,
            Manufacturer = sp.PartNumber,
            Price = sp.UnitPrice,
            Country = sp.Country,
            StockQuantity = sp.StockQuantity
        }).ToList();

        return Ok(parts);
    }

    /// <summary>Get a specific spare part (admin only)</summary>
    [HttpGet("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [Produces("application/json")]
    [ProducesResponseType<SparePartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SparePartDto>> GetSparePart(Guid id)
    {
        var entity = await _context.SpareParts
            .Where(sp => sp.Id == id)
            .FirstOrDefaultAsync();

        if (entity == null) return NotFound();

        var part = new SparePartDto
        {
            Id = entity.Id,
            Name = entity.Name.Translate() ?? entity.Name.ToString() ?? "",
            PartNumber = entity.PartNumber,
            Manufacturer = entity.PartNumber,
            Price = entity.UnitPrice,
            Country = entity.Country,
            StockQuantity = entity.StockQuantity
        };

        return Ok(part);
    }

    /// <summary>Create a spare part (admin only)</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<SparePartDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SparePartDto>> CreateSparePart([FromBody] SparePartCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Name is required."
            });
        }

        var part = new SparePart
        {
            Name = new LangStr(dto.Name),
            PartNumber = dto.Manufacturer,
            Country = dto.Country,
            UnitPrice = dto.Price,
            StockQuantity = dto.StockQuantity
        };

        _context.SpareParts.Add(part);
        await _context.SaveChangesAsync();

        var result = new SparePartDto
        {
            Id = part.Id,
            Name = part.Name.Translate() ?? part.Name.ToString() ?? "",
            Manufacturer = part.PartNumber,
            Price = part.UnitPrice,
            Country = dto.Country,
            StockQuantity = part.StockQuantity
        };

        return CreatedAtAction(nameof(GetSparePart), new { id = part.Id }, result);
    }

    /// <summary>Update a spare part (admin only)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSparePart(Guid id, [FromBody] SparePartUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "Name is required."
            });
        }

        var part = await _context.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        part.Name = new LangStr(dto.Name);
        part.PartNumber = dto.Manufacturer;
        part.Country = dto.Country;
        part.UnitPrice = dto.Price;
        part.StockQuantity = dto.StockQuantity;
        part.UpdatedAt = DateTime.UtcNow;

        _context.Entry(part).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Delete a spare part (admin only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSparePart(Guid id)
    {
        var part = await _context.SpareParts.FindAsync(id);
        if (part == null) return NotFound();

        _context.SpareParts.Remove(part);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

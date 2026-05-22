using App.DTO.v1;
using App.DTO.v1.SparePart;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Workshops.Application.Services;
using Workshops.Contracts;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SparePartsController : ControllerBase
{
    private readonly ISparePartService _spareParts;
    private readonly IWorkshopsUnitOfWork _workshopsUow;

    public SparePartsController(ISparePartService spareParts, IWorkshopsUnitOfWork workshopsUow)
    {
        _spareParts = spareParts;
        _workshopsUow = workshopsUow;
    }

    /// <summary>Get all spare parts (admin only)</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<SparePartDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SparePartDto>>> GetSpareParts()
    {
        var parts = (await _spareParts.AllAsync())
            .Select(SparePartApiMapper.ToApiDto)
            .ToList();

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
        var part = await _spareParts.FindAsync(id);
        if (part == null) return NotFound();

        return Ok(SparePartApiMapper.ToApiDto(part));
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

        var created = _spareParts.Add(SparePartApiMapper.ToBll(dto));
        await _workshopsUow.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSparePart), new { id = created.Id }, SparePartApiMapper.ToCreatedApiDto(created));
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

        var updated = await _spareParts.UpdateAsync(SparePartApiMapper.ToBll(dto, id));
        if (updated == null) return NotFound();

        await _workshopsUow.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Delete a spare part (admin only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSparePart(Guid id)
    {
        var part = await _spareParts.FindAsync(id);
        if (part == null) return NotFound();

        _spareParts.Remove(part);
        await _workshopsUow.SaveChangesAsync();

        return NoContent();
    }
}

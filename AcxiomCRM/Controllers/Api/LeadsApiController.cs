using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserName => User.Identity?.Name ?? "API User";
    private string CurrentUserRole
    {
        get
        {
            if (User.IsInRole(RoleConstants.Admin)) return RoleConstants.Admin;
            if (User.IsInRole(RoleConstants.Manager)) return RoleConstants.Manager;
            return RoleConstants.SalesExecutive;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] LeadStatus? status = null,
        [FromQuery] LeadPriority? priority = null,
        [FromQuery] LeadSource? source = null,
        [FromQuery] string? assigneeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _leadService.GetLeadsAsync(
            CurrentUserId,
            CurrentUserRole,
            search,
            status,
            priority,
            source,
            assigneeId,
            "DateDesc",
            page,
            Math.Clamp(pageSize, 1, 100));

        var dtos = result.Items.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source.ToString(),
            Status = l.Status.ToString(),
            Priority = l.Priority.ToString(),
            ExpectedValue = l.ExpectedValue,
            Notes = l.Notes,
            AssignedTo = l.AssignedTo,
            AssignedUserName = l.AssignedUser?.FullName,
            CustomerId = l.CustomerId,
            CreatedDate = l.CreatedDate
        }).ToList();

        return Ok(new
        {
            total = result.TotalCount,
            page = result.PageNumber,
            pageSize = result.PageSize,
            totalPages = result.TotalPages,
            data = dtos
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null)
        {
            return NotFound(new { message = $"Lead with ID {id} not found or you lack permission to view it." });
        }

        return Ok(new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source.ToString(),
            Status = lead.Status.ToString(),
            Priority = lead.Priority.ToString(),
            ExpectedValue = lead.ExpectedValue,
            Notes = lead.Notes,
            AssignedTo = lead.AssignedTo,
            AssignedUserName = lead.AssignedUser?.FullName,
            CustomerId = lead.CustomerId,
            CreatedDate = lead.CreatedDate
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var lead = new Lead
        {
            LeadName = dto.LeadName.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo ?? CurrentUserId
        };

        var (success, message, created) = await _leadService.CreateLeadAsync(
            lead,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            return BadRequest(new { message });
        }

        var resDto = new LeadDto
        {
            LeadId = created!.LeadId,
            LeadCode = created.LeadCode,
            LeadName = created.LeadName,
            Email = created.Email,
            Phone = created.Phone,
            CompanyName = created.CompanyName,
            Source = created.Source.ToString(),
            Status = created.Status.ToString(),
            Priority = created.Priority.ToString(),
            ExpectedValue = created.ExpectedValue,
            Notes = created.Notes,
            AssignedTo = created.AssignedTo,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetById), new { id = resDto.LeadId }, resDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var lead = new Lead
        {
            LeadId = id,
            LeadName = dto.LeadName.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo ?? CurrentUserId
        };

        var (success, message) = await _leadService.UpdateLeadAsync(
            lead,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            if (message.Contains("not have permission")) return StatusCode(StatusCodes.Status403Forbidden, new { message });
            return BadRequest(new { message });
        }

        return Ok(new { message = "Lead updated successfully." });
    }
}


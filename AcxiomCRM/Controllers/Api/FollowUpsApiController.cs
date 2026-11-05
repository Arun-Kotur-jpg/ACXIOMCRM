using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
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
        [FromQuery] string scope = "All",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _followUpService.GetFollowUpsAsync(
            CurrentUserId,
            CurrentUserRole,
            search,
            null,
            null,
            null,
            scope,
            page,
            Math.Clamp(pageSize, 1, 100));

        var dtos = result.Items.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity?.OpportunityName,
            FollowUpDate = f.FollowUpDate,
            Subject = f.Subject,
            FollowUpType = f.FollowUpType.ToString(),
            Status = f.Status.ToString(),
            Remarks = f.Remarks,
            AssignedTo = f.AssignedTo,
            AssignedUserName = f.AssignedUser?.FullName,
            CreatedDate = f.CreatedDate
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            Subject = dto.Subject.Trim(),
            FollowUpType = dto.FollowUpType,
            Status = dto.Status,
            Remarks = dto.Remarks,
            AssignedTo = dto.AssignedTo ?? CurrentUserId
        };

        var (success, message, created) = await _followUpService.CreateFollowUpAsync(
            followUp,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            return BadRequest(new { message });
        }

        var resDto = new FollowUpDto
        {
            FollowUpId = created!.FollowUpId,
            CustomerId = created.CustomerId,
            LeadId = created.LeadId,
            OpportunityId = created.OpportunityId,
            FollowUpDate = created.FollowUpDate,
            Subject = created.Subject,
            FollowUpType = created.FollowUpType.ToString(),
            Status = created.Status.ToString(),
            Remarks = created.Remarks,
            AssignedTo = created.AssignedTo,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetAll), new { id = resDto.FollowUpId }, resDto);
    }
}


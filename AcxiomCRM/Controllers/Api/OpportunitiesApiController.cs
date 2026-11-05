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
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
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
        [FromQuery] OpportunityStage? stage = null,
        [FromQuery] OpportunityStatus? status = null,
        [FromQuery] string? assigneeId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _opportunityService.GetOpportunitiesAsync(
            CurrentUserId,
            CurrentUserRole,
            search,
            stage,
            status,
            assigneeId,
            "DateDesc",
            page,
            Math.Clamp(pageSize, 1, 100));

        var dtos = result.Items.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage.ToString(),
            Probability = o.Probability,
            WeightedPipeline = o.WeightedPipeline,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status.ToString(),
            Source = o.Source,
            Notes = o.Notes,
            AssignedTo = o.AssignedTo,
            AssignedUserName = o.AssignedUser?.FullName,
            CreatedDate = o.CreatedDate
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
        var opportunity = await _opportunityService.GetOpportunityByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (opportunity == null)
        {
            return NotFound(new { message = $"Opportunity with ID {id} not found or you lack permission to view it." });
        }

        return Ok(new OpportunityDto
        {
            OpportunityId = opportunity.OpportunityId,
            OpportunityName = opportunity.OpportunityName,
            CustomerId = opportunity.CustomerId,
            CustomerName = opportunity.Customer?.CustomerName,
            LeadId = opportunity.LeadId,
            Amount = opportunity.Amount,
            Stage = opportunity.Stage.ToString(),
            Probability = opportunity.Probability,
            WeightedPipeline = opportunity.WeightedPipeline,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Status = opportunity.Status.ToString(),
            Source = opportunity.Source,
            Notes = opportunity.Notes,
            AssignedTo = opportunity.AssignedTo,
            AssignedUserName = opportunity.AssignedUser?.FullName,
            CreatedDate = opportunity.CreatedDate
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var opportunity = new Opportunity
        {
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            Source = dto.Source,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo ?? CurrentUserId
        };

        var (success, message, created) = await _opportunityService.CreateOpportunityAsync(
            opportunity,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            return BadRequest(new { message });
        }

        var resDto = new OpportunityDto
        {
            OpportunityId = created!.OpportunityId,
            OpportunityName = created.OpportunityName,
            CustomerId = created.CustomerId,
            LeadId = created.LeadId,
            Amount = created.Amount,
            Stage = created.Stage.ToString(),
            Probability = created.Probability,
            WeightedPipeline = created.WeightedPipeline,
            ExpectedCloseDate = created.ExpectedCloseDate,
            Status = created.Status.ToString(),
            Source = created.Source,
            Notes = created.Notes,
            AssignedTo = created.AssignedTo,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetById), new { id = resDto.OpportunityId }, resDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var opportunity = new Opportunity
        {
            OpportunityId = id,
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            Source = dto.Source,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo ?? CurrentUserId
        };

        var (success, message) = await _opportunityService.UpdateOpportunityAsync(
            opportunity,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            if (message.Contains("not have permission")) return StatusCode(StatusCodes.Status403Forbidden, new { message });
            return BadRequest(new { message });
        }

        return Ok(new { message = "Opportunity updated successfully." });
    }
}


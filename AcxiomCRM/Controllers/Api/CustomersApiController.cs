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
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
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
        [FromQuery] CustomerStatus? status = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _customerService.GetCustomersAsync(
            CurrentUserId,
            CurrentUserRole,
            search,
            status,
            ownerId,
            "DateDesc",
            page,
            Math.Clamp(pageSize, 1, 100));

        var dtos = result.Items.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            City = c.City,
            State = c.State,
            Status = c.Status.ToString(),
            OwnerId = c.OwnerId,
            OwnerName = c.Owner?.FullName,
            CreatedDate = c.CreatedDate
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
        var customer = await _customerService.GetCustomerByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found or you lack permission to view it." });
        }

        return Ok(new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            City = customer.City,
            State = customer.State,
            Status = customer.Status.ToString(),
            OwnerId = customer.OwnerId,
            OwnerName = customer.Owner?.FullName,
            CreatedDate = customer.CreatedDate
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var customer = new Customer
        {
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status,
            OwnerId = dto.OwnerId ?? CurrentUserId
        };

        var (success, message, created) = await _customerService.CreateCustomerAsync(
            customer,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            if (message.Contains("already exists"))
            {
                return Conflict(new { message });
            }
            return BadRequest(new { message });
        }

        var resDto = new CustomerDto
        {
            CustomerId = created!.CustomerId,
            CustomerCode = created.CustomerCode,
            CustomerName = created.CustomerName,
            Email = created.Email,
            Phone = created.Phone,
            CompanyName = created.CompanyName,
            City = created.City,
            State = created.State,
            Status = created.Status.ToString(),
            OwnerId = created.OwnerId,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetById), new { id = resDto.CustomerId }, resDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var customer = new Customer
        {
            CustomerId = id,
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status,
            OwnerId = dto.OwnerId ?? CurrentUserId
        };

        var (success, message) = await _customerService.UpdateCustomerAsync(
            customer,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            if (message.Contains("not have permission")) return StatusCode(StatusCodes.Status403Forbidden, new { message });
            if (message.Contains("already exists")) return Conflict(new { message });
            return BadRequest(new { message });
        }

        return Ok(new { message = "Customer updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _customerService.DeactivateCustomerAsync(
            id,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            if (message.Contains("not found")) return NotFound(new { message });
            if (message.Contains("not have permission")) return StatusCode(StatusCodes.Status403Forbidden, new { message });
            return BadRequest(new { message });
        }

        return NoContent();
    }
}


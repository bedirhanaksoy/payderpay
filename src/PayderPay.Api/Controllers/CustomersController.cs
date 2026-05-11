using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayderPay.Api.Extensions;
using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Services;
using PayderPay.Application.Dtos.Customers;

namespace PayderPay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ISummaryService _summaryService;

    public CustomersController(ICustomerService customerService, ISummaryService summaryService)
    {
        _customerService = customerService;
        _summaryService = summaryService;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetAllPagedAsync(new PageRequest(page, pageSize), cancellationToken);
        Response.AddPaginationHeaders(result);
        return Ok(result.Items);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/dashboard")]
    public async Task<IActionResult> GetDashboard(Guid id, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await _summaryService.GetDashboardAsync(id, year, month, cancellationToken);
        return Ok(result);
    }
}

using BudgetAllocator.Api.Interfaces;
using BudgetAllocator.Api.Models.DTO.Request;
using BudgetAllocator.Api.Models.DTO.Response;
using BudgetAllocator.Api.Models.DTO.Response.Generic;
using BudgetAllocator.Api.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetAllocator.Api.Controllers;

[ApiController]
[Route("api/v1/income-profiles")]
[Authorize]
public class IncomeProfilesController: ControllerBase
{
    private readonly IIncomeProfileService _incomeProfileService;

    public IncomeProfilesController(IIncomeProfileService incomeProfileService)
    {
        _incomeProfileService = incomeProfileService;
    }

    [HttpPost]
    [EndpointSummary("Create a new income profile")]
    [EndpointDescription("Creates a monthly income profile and calculates monetary allocations for each category. Total category percentage cannot exceed 100%.")]
    [ProducesResponseType(typeof(BaseResponse<IncomeProfileResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponse<IncomeProfileResponse>>> Create(
        [FromBody] CreateIncomeProfileRequest request)
    {
        var result = await _incomeProfileService.CreateAsync(request);

        if (!result.Success)
        {
            return BadRequest(result); //HTTP 400
        }
        
        // HTTP 201 Created with Location header pointing to GetById
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }
    
    [HttpGet]
    [EndpointSummary("Get all income profiles")]
    [EndpointDescription("Returns a list of all saved income profiles ordered from newest to oldest, including their budget categories.")]
    [ProducesResponseType(typeof(BaseResponse<List<IncomeProfileResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<List<IncomeProfileResponse>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _incomeProfileService.GetAllAsync(pageNumber, pageSize);
        return Ok(result); // HTTP 200
    }
    
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get an income profile by ID")]
    [ProducesResponseType(typeof(BaseResponse<IncomeProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponse<IncomeProfileResponse>>> GetById(Guid id)
    {
        var result = await _incomeProfileService.GetByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result); // HTTP 404
        }

        return Ok(result); // HTTP 200
    }
    
    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete an income profile (Admin only)")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = nameof(RoleEnum.Admin))]
    public async Task<ActionResult<BaseResponse<bool>>> Delete(Guid id)
    {
        var result = await _incomeProfileService.DeleteAsync(id);
        if (!result.Success)
        {
            return NotFound(result); // HTTP 404
        }

        return Ok(result); // HTTP 200
    }

    [HttpGet("{id:guid}/convert")]
    [EndpointSummary("Convert an income profile to another currency")]
    [EndpointDescription("Fetches real-time exchange rates and converts the monthly net income and all category allocations into the target currency.")]
    [ProducesResponseType(typeof(BaseResponse<ConvertedIncomeProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponse<ConvertedIncomeProfileResponse>>> GetConverted(
        Guid id,
        [FromQuery] CurrencyEnum targetCurrency = CurrencyEnum.USD)
    {
        var result = await _incomeProfileService.GetConvertedByIdAsync(id, targetCurrency);

        if (!result.Success)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
}
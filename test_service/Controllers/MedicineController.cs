using Microsoft.AspNetCore.Mvc;
using SharedKernel.CQRS;
using SharedKernel.Commands.Medicine;
using SharedKernel.Queries.Medicine;
using SharedKernel.DTOs.Medicine;

namespace test_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicineController : ControllerBase
{
    private readonly ICommandHandler<CreateMedicineCommand, SharedKernel.Common.Result<MedicineResponse>> _createHandler;
    private readonly ICommandHandler<UpdateMedicineCommand, SharedKernel.Common.Result<MedicineResponse>> _updateHandler;
    private readonly ICommandHandler<DeleteMedicineCommand, SharedKernel.Common.Result> _deleteHandler;
    private readonly IQueryHandler<GetMedicineByIdQuery, SharedKernel.Common.Result<MedicineResponse>> _getByIdHandler;
    private readonly IQueryHandler<GetAllMedicinesQuery, SharedKernel.Common.Result<List<MedicineResponse>>> _getAllHandler;
    private readonly IQueryHandler<SearchMedicinesQuery, SharedKernel.Common.Result<List<MedicineResponse>>> _searchHandler;
    private readonly IQueryHandler<GetMedicinesByCategoryQuery, SharedKernel.Common.Result<List<MedicineResponse>>> _getByCategoryHandler;
    private readonly IQueryHandler<GetMedicinesByManufacturerQuery, SharedKernel.Common.Result<List<MedicineResponse>>> _getByManufacturerHandler;
    private readonly ILogger<MedicineController> _logger;

    public MedicineController(
        ICommandHandler<CreateMedicineCommand, SharedKernel.Common.Result<MedicineResponse>> createHandler,
        ICommandHandler<UpdateMedicineCommand, SharedKernel.Common.Result<MedicineResponse>> updateHandler,
        ICommandHandler<DeleteMedicineCommand, SharedKernel.Common.Result> deleteHandler,
        IQueryHandler<GetMedicineByIdQuery, SharedKernel.Common.Result<MedicineResponse>> getByIdHandler,
        IQueryHandler<GetAllMedicinesQuery, SharedKernel.Common.Result<List<MedicineResponse>>> getAllHandler,
        IQueryHandler<SearchMedicinesQuery, SharedKernel.Common.Result<List<MedicineResponse>>> searchHandler,
        IQueryHandler<GetMedicinesByCategoryQuery, SharedKernel.Common.Result<List<MedicineResponse>>> getByCategoryHandler,
        IQueryHandler<GetMedicinesByManufacturerQuery, SharedKernel.Common.Result<List<MedicineResponse>>> getByManufacturerHandler,
        ILogger<MedicineController> logger)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getByIdHandler = getByIdHandler;
        _getAllHandler = getAllHandler;
        _searchHandler = searchHandler;
        _getByCategoryHandler = getByCategoryHandler;
        _getByManufacturerHandler = getByManufacturerHandler;
        _logger = logger;
    }

    /// <summary>
    /// Get all medicines
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllMedicines()
    {
        var query = new GetAllMedicinesQuery();
        var result = await _getAllHandler.HandleAsync(query);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get medicine by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMedicine(string id)
    {
        var query = new GetMedicineByIdQuery(id);
        var result = await _getByIdHandler.HandleAsync(query);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Search medicines by name, generic name, or description
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMedicines([FromQuery] string query)
    {
        var searchQuery = new SearchMedicinesQuery(query);
        var result = await _searchHandler.HandleAsync(searchQuery);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get medicines by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetMedicinesByCategory(string category)
    {
        var query = new GetMedicinesByCategoryQuery(category);
        var result = await _getByCategoryHandler.HandleAsync(query);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get medicines by manufacturer
    /// </summary>
    [HttpGet("manufacturer/{manufacturer}")]
    public async Task<IActionResult> GetMedicinesByManufacturer(string manufacturer)
    {
        var query = new GetMedicinesByManufacturerQuery(manufacturer);
        var result = await _getByManufacturerHandler.HandleAsync(query);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new medicine
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineCommand command)
    {
        var result = await _createHandler.HandleAsync(command);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetMedicine), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing medicine
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMedicine(string id, [FromBody] UpdateMedicineCommand command)
    {
        if (id != command.Id)
            return BadRequest(new { error = "ID mismatch" });

        var result = await _updateHandler.HandleAsync(command);

        if (result.IsFailure)
            return result.Error == "Medicine not found" ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a medicine
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedicine(string id)
    {
        var command = new DeleteMedicineCommand(id);
        var result = await _deleteHandler.HandleAsync(command);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using SharedKernel.Messaging;
using SharedKernel.Commands.Medicine;
using Microsoft.Extensions.Logging;

namespace test_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicineController : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MedicineController> _logger;

    public MedicineController(IMessageBus messageBus, ILogger<MedicineController> logger)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new medicine (async - sends command to message bus)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineCommand command)
    {
        try
        {
            _logger.LogInformation("?? Publishing CreateMedicineCommand to message bus: {Name}", command.Name);

            await _messageBus.SendAsync(command);

            _logger.LogInformation("? Command published successfully: {Name}", command.Name);

            return Accepted(new
            {
                message = "Medicine creation command accepted and queued for processing",
                commandType = nameof(CreateMedicineCommand),
                medicineName = command.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error publishing command to message bus");
            return StatusCode(500, new { error = "Failed to queue medicine creation command" });
        }
    }
}

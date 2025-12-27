using Microsoft.AspNetCore.Mvc;
using SharedKernel.Messaging;
using test_service.Models;

namespace test_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageBus messageBus, ILogger<MessagesController> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a message to a queue
    /// </summary>
    [HttpPost("{queueName}")]
    public async Task<IActionResult> PublishMessage(string queueName, [FromBody] MessageDto message)
    {
        try
        {
            await _messageBus.PublishAsync(queueName, message);
            return Ok(new { success = true, message = "Message published successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Sample endpoint to start subscribing to a queue
    /// Note: In production, subscriptions should typically be started on application startup
    /// </summary>
    [HttpPost("subscribe/{queueName}")]
    public async Task<IActionResult> Subscribe(string queueName)
    {
        try
        {
            // This is just a demo - in production, setup subscriptions in Program.cs or a BackgroundService
            await _messageBus.SubscribeAsync<MessageDto>(queueName, async (msg) =>
            {
                _logger.LogInformation("Received message: {Content}", msg.Content);
                // Process the message here
                await Task.CompletedTask;
            });

            return Ok(new { success = true, message = $"Subscribed to queue: {queueName}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to queue");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

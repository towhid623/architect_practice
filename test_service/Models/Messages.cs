namespace test_service.Models;

/// <summary>
/// Sample message DTO for general messages
/// </summary>
public record MessageDto
{
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Order message for order processing
/// </summary>
public record OrderMessage
{
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime OrderDate { get; init; }
    public List<string> Items { get; init; } = new();
}

/// <summary>
/// Notification message for sending notifications
/// </summary>
public record NotificationMessage
{
    public string Type { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
  public string Body { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

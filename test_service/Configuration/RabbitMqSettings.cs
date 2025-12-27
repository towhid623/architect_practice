namespace test_service.Configuration;

/// <summary>
/// RabbitMQ configuration settings
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// Connection string for RabbitMQ (AMQP URI)
  /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}

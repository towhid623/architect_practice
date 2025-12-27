using SharedKernel.Configuration;

namespace test_service.Configuration;

/// <summary>
/// Re-export RabbitMqSettings from SharedKernel for backward compatibility
/// </summary>
public class RabbitMqSettings : SharedKernel.Configuration.RabbitMqSettings
{
}

namespace SharedKernel.Configuration;

/// <summary>
/// RabbitMQ configuration settings with comprehensive options
/// </summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// Connection string for RabbitMQ (AMQP URI format)
    /// Example: amqps://username:password@hostname:port/virtualhost
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// RabbitMQ hostname (alternative to connection string)
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port (default: 5672 for AMQP, 5671 for AMQPS)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ virtual host (default: /)
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// RabbitMQ username
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Enable SSL/TLS connection
 /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// SSL server name (for certificate validation)
    /// </summary>
    public string? SslServerName { get; set; }

    /// <summary>
/// Connection timeout in milliseconds (default: 30000 = 30 seconds)
    /// </summary>
    public int RequestedConnectionTimeout { get; set; } = 30000;

    /// <summary>
    /// Socket read timeout in milliseconds (default: 30000 = 30 seconds)
    /// </summary>
    public int SocketReadTimeout { get; set; } = 30000;

    /// <summary>
    /// Socket write timeout in milliseconds (default: 30000 = 30 seconds)
    /// </summary>
    public int SocketWriteTimeout { get; set; } = 30000;

    /// <summary>
    /// Enable automatic connection recovery (default: true)
 /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
  /// Enable topology recovery (queues, exchanges, bindings) (default: true)
    /// </summary>
    public bool TopologyRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Number of retry attempts for initial connection (default: 3)
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds (default: 5000 = 5 seconds)
  /// </summary>
    public int RetryDelayMs { get; set; } = 5000;

    /// <summary>
    /// Validates if the configuration is valid
    /// </summary>
    public bool IsValid()
    {
        // If connection string is provided, it takes precedence
      if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
 return true;
        }

        // Otherwise, validate individual settings
        return !string.IsNullOrWhiteSpace(HostName) &&
Port > 0 &&
    !string.IsNullOrWhiteSpace(UserName) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    /// <summary>
    /// Parses connection string and extracts individual settings
    /// Format: amqp(s)://username:password@hostname:port/virtualhost
    /// </summary>
    public void ParseConnectionString()
    {
     if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

     try
        {
        var uri = new Uri(ConnectionString);

  // Parse SSL
    UseSsl = uri.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase);

        // Parse hostname
            HostName = uri.Host;

     // Parse port (use default based on scheme if not specified)
    Port = uri.Port > 0 ? uri.Port : (UseSsl ? 5671 : 5672);

 // Parse virtual host
    if (uri.AbsolutePath.Length > 1)
    {
         VirtualHost = Uri.UnescapeDataString(uri.AbsolutePath.Substring(1));
            }

        // Parse username and password
if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
         var userInfo = uri.UserInfo.Split(':');
 if (userInfo.Length >= 1)
            {
       UserName = Uri.UnescapeDataString(userInfo[0]);
         }
      if (userInfo.Length >= 2)
    {
           Password = Uri.UnescapeDataString(userInfo[1]);
           }
            }

  if (UseSsl && string.IsNullOrWhiteSpace(SslServerName))
   {
        SslServerName = HostName;
            }
 }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse RabbitMQ connection string: {ex.Message}", ex);
        }
    }
}

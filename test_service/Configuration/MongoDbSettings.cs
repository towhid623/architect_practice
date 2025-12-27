namespace test_service.Configuration;

/// <summary>
/// MongoDB configuration settings
/// </summary>
public class MongoDbSettings
{
 public const string SectionName = "MongoDB";

    /// <summary>
    /// Connection string for MongoDB
  /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

  /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;
}

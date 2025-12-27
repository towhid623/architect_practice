namespace SharedKernel.CQRS;

/// <summary>
/// Marker interface for commands (write operations)
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Command with response
/// </summary>
public interface ICommand<out TResponse> : ICommand
{
}

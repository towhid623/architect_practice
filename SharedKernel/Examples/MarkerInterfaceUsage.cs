/*
 * ========================================
 * MARKER INTERFACE USAGE EXAMPLES
 * ========================================
 * 
 * ALL COMMANDS AND QUERIES IMPLEMENT MARKER INTERFACES
 * =====================================================
 * 
 * Commands implement: ICommand<TResponse> : ICommand
 * Queries implement:  IQuery<TResponse>
 * 
 * This allows for generic constraints and middleware patterns.
 * 
 * 
 * CURRENT IMPLEMENTATION:
 * =======================
 * 
 * All command classes derive from ICommand<TResponse>:
 *   - CreateMedicineCommand : ICommand<Result<MedicineResponse>>
 *   - UpdateMedicineCommand : ICommand<Result<MedicineResponse>>
 *   - DeleteMedicineCommand : ICommand<Result>
 * 
 * All query classes derive from IQuery<TResponse>:
 *   - GetMedicineByIdQuery : IQuery<Result<MedicineResponse>>
 *   - GetAllMedicinesQuery : IQuery<Result<List<MedicineResponse>>>
 *   - SearchMedicinesQuery : IQuery<Result<List<MedicineResponse>>>
 *   - GetMedicinesByCategoryQuery : IQuery<Result<List<MedicineResponse>>>
 *   - GetMedicinesByManufacturerQuery : IQuery<Result<List<MedicineResponse>>>
 * 
 * 
 * EXAMPLE 1: Generic Command Sender/Mediator
 * ===========================================
 * 
 * public interface ICommandSender
 * {
 *     Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
 * }
 * 
 * public class CommandSender : ICommandSender
 * {
 *     private readonly IServiceProvider _serviceProvider;
 * 
 *     public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
 *     {
 *         var commandType = command.GetType();
 *         var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));
 *   var handler = _serviceProvider.GetService(handlerType);
 *         // ... invoke handler
 *     }
 * }
 * 
 * USAGE:
 * var command = new CreateMedicineCommand(...);
 * var result = await commandSender.SendAsync(command);
 * 
 * 
 * EXAMPLE 2: Generic Query Sender/Mediator
 * =========================================
 * 
 * public interface IQuerySender
 * {
 *     Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
 * }
 * 
 * public class QuerySender : IQuerySender
 * {
 *     private readonly IServiceProvider _serviceProvider;
 * 
 *     public async Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
 *     {
 *         var queryType = query.GetType();
 *         var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));
 *         var handler = _serviceProvider.GetService(handlerType);
 * 
 *         // ... invoke handler
 *     }
 * }
 * 
 * USAGE:
 * var query = new GetMedicineByIdQuery("someId");
 * var result = await querySender.SendAsync(query);
 * 
 * 
 * EXAMPLE 3: Generic Validation Pipeline
 * =======================================
 * 
 * public interface ICommandValidator<TCommand> where TCommand : ICommand
 * {
 *     Task<Result> ValidateAsync(TCommand command);
 * }
 * 
 * public class CreateMedicineValidator : ICommandValidator<CreateMedicineCommand>
 * {
 *   public Task<Result> ValidateAsync(CreateMedicineCommand command)
 *     {
 *         if (string.IsNullOrWhiteSpace(command.Name))
 *             return Task.FromResult(Result.Failure("Name is required"));
 *      return Task.FromResult(Result.Success());
 *     }
 * }
 * 
 * 
 * EXAMPLE 4: Generic Logging Decorator
 * =====================================
 * 
 * public class LoggingCommandHandlerDecorator<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
 *     where TCommand : ICommand<TResponse>
 * {
 *     private readonly ICommandHandler<TCommand, TResponse> _innerHandler;
 *     private readonly ILogger _logger;
 * 
 *     public async Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
 *{
 *         _logger.LogInformation("Executing command {CommandType}", typeof(TCommand).Name);
 *         var result = await _innerHandler.HandleAsync(command, cancellationToken);
 *         _logger.LogInformation("Successfully executed command {CommandType}", typeof(TCommand).Name);
 *         return result;
 *     }
 * }
 * 
 * 
 * EXAMPLE 5: Generic Caching for Queries
 * =======================================
 * 
 * public class CachingQueryHandlerDecorator<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
 *     where TQuery : IQuery<TResponse>
 * {
 *     private readonly IQueryHandler<TQuery, TResponse> _innerHandler;
 *   private readonly IMemoryCache _cache;
 * 
 *     public async Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
 *     {
 *   var cacheKey = $"{typeof(TQuery).Name}_{query.GetHashCode()}";
 *         if (_cache.TryGetValue(cacheKey, out TResponse cachedResult))
 *             return cachedResult;
 * 
 *         var result = await _innerHandler.HandleAsync(query, cancellationToken);
 *         _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
 *         return result;
 *     }
 * }
 * 
 * 
 * EXAMPLE 6: Generic Authorization Pipeline
 * ==========================================
 * 
 * public interface IAuthorizer<TCommand> where TCommand : ICommand
 * {
 *     Task<bool> IsAuthorizedAsync(TCommand command, string userId);
 * }
 * 
 * public class DeleteMedicineAuthorizer : IAuthorizer<DeleteMedicineCommand>
 * {
 *     public Task<bool> IsAuthorizedAsync(DeleteMedicineCommand command, string userId)
 *     {
 *         // Check if user has permission to delete medicine
 *    return Task.FromResult(true);
 *     }
 * }
 * 
 * 
 * EXAMPLE 7: Generic Mediator Pattern Implementation
 * ==================================================
 * 
 * public interface IMediator
 * {
 *     Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
 *     Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
 * }
 * 
 * public class Mediator : IMediator
 * {
 *     private readonly IServiceProvider _serviceProvider;
 * 
 *   public async Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
 *     {
 *      var commandType = command.GetType();
 *    var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResponse));
 *         dynamic handler = _serviceProvider.GetService(handlerType);
 *       return await handler.HandleAsync((dynamic)command, cancellationToken);
 *   }
 * 
 *     public async Task<TResponse> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
 *     {
 *  var queryType = query.GetType();
 *         var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));
 *       dynamic handler = _serviceProvider.GetService(handlerType);
 *   return await handler.HandleAsync((dynamic)query, cancellationToken);
 *     }
 * }
 * 
 * REGISTRATION IN PROGRAM.CS:
 * builder.Services.AddSingleton<IMediator, Mediator>();
 * 
 * USAGE IN CONTROLLERS:
 * 
 * public class MedicineController : ControllerBase
 * {
 *   private readonly IMediator _mediator;
 * 
 *     public MedicineController(IMediator mediator)
 *  {
 *         _mediator = mediator;
 *     }
 * 
 *     [HttpPost]
 *     public async Task<IActionResult> Create([FromBody] CreateMedicineCommand command)
 *     {
 *         var result = await _mediator.SendAsync(command);
 *         return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
 *     }
 * 
 *     [HttpGet("{id}")]
 *public async Task<IActionResult> GetById(string id)
 *     {
 *         var query = new GetMedicineByIdQuery(id);
 *  var result = await _mediator.SendAsync(query);
 *         return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
 *     }
 * }
 * 
 * 
 * BENEFITS OF MARKER INTERFACES:
 * ===============================
 * ? All commands implement ICommand<TResponse> (marker interface)
 * ? All queries implement IQuery<TResponse> (marker interface)
 * ? Can use generic constraints for middleware/decorators
 * ? Can implement Mediator pattern (like MediatR)
 * ? Can add cross-cutting concerns (logging, validation, caching, authorization)
 * ? Follows CQRS principles with type safety
 * ? Easy to add new commands/queries without modifying existing code
 * ? Enables dependency injection of generic handlers
 * ? Supports decorator pattern for pipeline behaviors
 * ? Allows for convention-based handler discovery
 * 
 * 
 * PRACTICAL USE CASES:
 * ====================
 * 1. Mediator Pattern - Single entry point for all commands/queries
 * 2. Validation Pipeline - Validate all commands before execution
 * 3. Logging Pipeline - Log all command/query execution
 * 4. Authorization Pipeline - Check permissions before execution
 * 5. Caching Pipeline - Cache query results automatically
 * 6. Transaction Pipeline - Wrap commands in database transactions
 * 7. Retry Pipeline - Automatically retry failed commands
 * 8. Performance Monitoring - Track execution time of all operations
 * 9. Event Sourcing - Record all commands as events
 * 10. Audit Trail - Log who executed which commands when
 */

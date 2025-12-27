/*
 * ========================================
 * CLEAN ARCHITECTURE OVERVIEW
 * ========================================
 * 
 * This solution implements Clean Architecture with CQRS pattern and
 * Infrastructure separation.
 * 
 * STRUCTURE:
 * ----------
 * 
 * SharedKernel (Class Library) - Core contracts and domain logic
 * ??? CQRS/
 * ?   ??? ICommand.cs                  - Base command interface
 * ?   ??? ICommand<TResponse>.cs          - Command with response
 * ?   ??? IQuery<TResponse>.cs     - Query interface
 * ? ??? ICommandHandler<T>.cs           - Command handler interface
 * ?   ??? IQueryHandler<T, R>.cs          - Query handler interface
 * ?
 * ??? Common/
 * ?   ??? Result.cs  - Result wrapper for success/failure
 * ?   ??? Result<T>.cs      - Result wrapper with value
 * ?
 * ??? Messaging/
 * ?   ??? IMessageBus.cs        - Message bus interface
 * ?
 * ??? DTOs/
 * ?   ??? Medicine/
 * ?       ??? MedicineResponse.cs    - Shared response DTO
 * ?
 * ??? Commands/
 * ?   ??? Medicine/
 * ?       ??? CreateMedicineCommand.cs    - Command to create medicine
 * ?       ??? UpdateMedicineCommand.cs    - Command to update medicine
 * ?       ??? DeleteMedicineCommand.cs    - Command to delete medicine
 * ?
 * ??? Queries/
 *     ??? Medicine/
 *      ??? GetMedicineByIdQuery.cs             - Query to get medicine by ID
 *  ??? GetAllMedicinesQuery.cs             - Query to get all medicines
 *         ??? SearchMedicinesQuery.cs             - Query to search medicines
 * ??? GetMedicinesByCategoryQuery.cs      - Query to filter by category
 *         ??? GetMedicinesByManufacturerQuery.cs  - Query to filter by manufacturer
 * 
 * 
 * Infrastructure (Class Library) - External service implementations
 * ??? Messaging/
 *     ??? RabbitMqMessageBus.cs - RabbitMQ implementation of IMessageBus
 * 
 * 
 * test_service (Web API) - Application layer and presentation
 * ??? Handlers/
 * ?   ??? Medicine/
 * ?       ??? CreateMedicineCommandHandler.cs             - Handles create command
 * ?   ??? UpdateMedicineCommandHandler.cs             - Handles update command
 * ?       ??? DeleteMedicineCommandHandler.cs             - Handles delete command
 * ???? GetMedicineByIdQueryHandler.cs              - Handles get by ID query
 * ? ??? GetAllMedicinesQueryHandler.cs         - Handles get all query
 * ?   ??? SearchMedicinesQueryHandler.cs              - Handles search query
 * ?     ??? GetMedicinesByCategoryQueryHandler.cs       - Handles category filter
 * ???? GetMedicinesByManufacturerQueryHandler.cs   - Handles manufacturer filter
 * ?
 * ??? Controllers/
 * ?   ??? MedicineController.cs           - Medicine API endpoints (uses handlers)
 * ?   ??? MessagesController.cs         - Message bus API endpoints
 * ?
 * ??? Services/
 * ?   ??? IMedicineService.cs         - Service interface
 * ? ??? MedicineService.cs              - Business logic implementation
 * ?
 * ??? Repositories/
 * ?   ??? IMedicineRepository.cs          - Repository interface
 * ?   ??? MedicineRepository.cs  - Data access implementation
 * ?
 * ??? Data/
 * ?   ??? ApplicationDbContext.cs    - EF Core DbContext for MongoDB
 * ?
 * ??? Models/
 * ?   ??? Entities.cs   - Database entities
 * ?
 * ??? BackgroundServices/
 * ??? MessageConsumerService.cs       - Background service for message consumption
 * 
 * 
 * KEY PRINCIPLES:
 * ---------------
 * 1. SharedKernel - Contains ONLY contracts (interfaces, commands, queries, DTOs)
 * 2. Infrastructure - Contains implementations of external services (RabbitMQ, etc.)
 * 3. test_service - Contains business logic, handlers, controllers
 * 4. Commands = Write operations (Create, Update, Delete)
 * 5. Queries = Read operations (Get, Search, Filter)
 * 6. Handlers = Implementation of command/query processing
 * 7. Result pattern = Consistent success/failure handling
 * 8. Dependency Inversion = Application depends on abstractions, not implementations
 * 
 * 
 * DEPENDENCY FLOW:
 * ----------------
 * test_service ? Infrastructure ? SharedKernel
 * test_service ? SharedKernel
 * 
 * (Infrastructure references SharedKernel for interfaces)
 * (test_service references both for contracts and implementations)
 * 
 * 
 * BENEFITS:
 * ---------
 * ? Clean separation of concerns
 * ? Infrastructure is isolated and can be swapped
 * ? Commands and queries are reusable across projects
 * ? Easy to test handlers and infrastructure independently
 * ? Consistent error handling with Result pattern
 * ? Single responsibility - each handler/service does one thing
 * ? Open for extension, closed for modification
 * ? Technology-agnostic core (SharedKernel has no external dependencies)
 * 
 * 
 * INFRASTRUCTURE EXAMPLES:
 * ------------------------
 * 
 * // IMessageBus is in SharedKernel
 * public interface IMessageBus
 * {
 *     Task PublishAsync<T>(string queueName, T message);
 *     Task SubscribeAsync<T>(string queueName, Func<T, Task> handler);
 * }
 * 
 * // RabbitMqMessageBus is in Infrastructure
 * public class RabbitMqMessageBus : IMessageBus
 * {
 *  // RabbitMQ-specific implementation
 * }
 * 
 * // Application registers Infrastructure implementation
 * builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
 * 
 * 
 * USAGE EXAMPLE:
 * --------------
 * 
 * // In Controller - Inject handler
 * private readonly ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>> _handler;
 * 
 * // Create command object
 * var command = new CreateMedicineCommand(
 *     Name: "Aspirin",
 *     GenericName: "Acetylsalicylic Acid",
 *     // ... other properties
 * );
 * 
 * // Execute command
 * var result = await _handler.HandleAsync(command);
 * 
 * // Check result
 * if (result.IsSuccess)
 *     return Ok(result.Value);
 * else
 *     return BadRequest(new { error = result.Error });
 * 
 * 
 * DEPENDENCY INJECTION:
 * ---------------------
 * All handlers and infrastructure services are registered in Program.cs:
 * 
 * // Infrastructure
 * builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
 * 
 * // Handlers
 * builder.Services.AddScoped<ICommandHandler<CreateMedicineCommand, Result<MedicineResponse>>, 
 *     CreateMedicineCommandHandler>();
 * 
 * 
 * SOLUTION STRUCTURE:
 * -------------------
 * ArchitectPractice.sln
 * ??? SharedKernel.csproj          (Class Library - .NET 9)
 * ??? Infrastructure.csproj        (Class Library - .NET 9)
 * ?   ??? References: SharedKernel
 * ??? test_service.csproj          (Web API - .NET 9)
 *     ??? References: SharedKernel, Infrastructure
 * 
 * 
 * TESTING STRATEGY:
 * -----------------
 * 1. Unit Tests - Test handlers with mocked services/repositories
 * 2. Integration Tests - Test Infrastructure implementations
 * 3. API Tests - Test controllers end-to-end
 * 
 * 
 * FUTURE ENHANCEMENTS:
 * --------------------
 * - Add Domain project for domain entities and business rules
 * - Add Application project for use cases and orchestration
 * - Separate Persistence project for EF Core and repositories
 * - Add Events project for domain events
 * - Add API versioning
 * - Add authentication and authorization
 * - Add health checks
 * - Add distributed tracing
 */

using SharedKernel.CQRS;
using SharedKernel.Common;

namespace SharedKernel.Commands.Medicine;

public record DeleteMedicineCommand(string Id) : ICommand<Result>;

using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Infrastructure.Repositories;

public class OperationRepository(ApplicationDbContext applicationDbContext, ILogger<OperationRepository> logger)
    : IOperationRepository
{
    public async Task CreateOperationAsync(Operation operation)
    {
        await applicationDbContext.Operations.AddAsync(operation);
        await applicationDbContext.SaveChangesAsync();
        logger.LogInformation($"Operation {operation.Id} has been created");
    }
}
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Infrastructure.Interfaces;

public interface IOperationRepository
{
    public Task CreateOperationAsync(Operation operation);
}
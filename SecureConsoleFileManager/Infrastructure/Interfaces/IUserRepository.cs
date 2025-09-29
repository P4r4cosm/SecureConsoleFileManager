using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Infrastructure.Interfaces;

public interface IUserRepository
{
    public Task CreateUserAsync(User user, CancellationToken cancellationToken);

    public Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);

    public Task<User?> GetUserByLoginAsync(string login, CancellationToken cancellationToken);
}
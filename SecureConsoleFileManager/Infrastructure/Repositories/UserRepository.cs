using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Infrastructure.Repositories;

public class UserRepository(ILogger<UserRepository> logger, ApplicationDbContext dbContext) : IUserRepository
{
    public async Task CreateUserAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(Guid guid, CancellationToken cancellationToken)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == guid, cancellationToken);
    }

    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken cancellationToken)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Login == login, cancellationToken);
    }
}
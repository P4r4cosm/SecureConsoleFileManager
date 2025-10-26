using MediatR;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;
using Serilog;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;

namespace SecureConsoleFileManager.Feature.Users.CreateUser;

public class CreateUserHandler(
    IUserRepository userRepository,
    ICryptoService cryptoService,
    ApplicationDbContext dbContext,
    ILogger<CreateUserHandler> logger)
    : IRequestHandler<CreateUserCommand, MbResult>
{
    public async Task<MbResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var login = request.Login;
        var password = request.Password;

        // Checking if a login is taken
        var user = await userRepository.GetUserByLoginAsync(login, cancellationToken);
        if (user is not null)
            return MbResult.Failure($"This login is already taken.");

        var hashPassword = cryptoService.HashPassword(password);
        var newUser = new User { Login = login, Password = hashPassword };
        try
        {
            await userRepository.CreateUserAsync(newUser, cancellationToken);
            
            var logEntity = new LogEntity(LogLevel.Info, Command.CreateUser, $"User {login} has been created.");
            
            logger.LogInformation($"User {login} has been created.");

            return MbResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError($"Database error: {ex.Message}");
            return MbResult.Failure("Database error");
        }
    }
}
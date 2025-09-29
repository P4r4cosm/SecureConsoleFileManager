using MediatR;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common.Exceptions;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;
using Serilog;

namespace SecureConsoleFileManager.Feature.Users.CreateUser;

public class CreateUserHandler(
    IUserRepository userRepository,
    ICryptoService cryptoService,
    ILogger<CreateUserHandler> logger)
    : IRequestHandler<CreateUserCommand, bool>
{
    public async Task<bool> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var login = request.Login;
        var password = request.Password;

        // Checking if a login is taken
        var user = await userRepository.GetUserByLoginAsync(login, cancellationToken);
        if (user is not null)
            // Throw Exception
            throw new UserAlreadyExistsException(login);

        var hashPassword = cryptoService.HashPassword(password);
        var newUser = new User { Login = login, Password = hashPassword };
        try
        {
            await userRepository.CreateUserAsync(newUser,cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Database error: {e.Message}");
            return false;
        }
    }
}
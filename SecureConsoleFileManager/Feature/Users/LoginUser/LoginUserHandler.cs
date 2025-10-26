using MediatR;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;

namespace SecureConsoleFileManager.Feature.Users.LoginUser;

public class LoginUserHandler(
    IUserRepository userRepository,
    ICryptoService cryptoService,
    ApplicationDbContext dbContext,
    ILogger<LoginUserHandler> logger)
    : IRequestHandler<LoginUserCommand, MbResult<User>>
{
    public async Task<MbResult<User>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var login = request.Login;
        var password = request.Password;

        // If not found -> throw ex
        var user = await userRepository.GetUserByLoginAsync(login, cancellationToken);

        if (user is null)
        {
            logger.LogInformation("The user {login} not found", login);
            return MbResult<User>.Failure("Incorrect password");
        }

        var verificationResult = cryptoService.VerifyUserByPassword(user, password);

        if (verificationResult)
        {
            try
            {
                dbContext.Logs.Add(new LogEntity(LogLevel.Info, Command.Login, $"{user.Login} successful logged in"));
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while saving log to database {ex.Message}");
            }

            logger.LogInformation("The user {user} has successfully logged in", user.Login);
            return MbResult<User>.Success(user);
        }
        
        logger.LogInformation("Incorrect user {user} password", user.Login);
        return MbResult<User>.Failure("Incorrect password");
    }
}
using MediatR;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common.Exceptions;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Services.Interfaces;

namespace SecureConsoleFileManager.Feature.Users.LoginUser;

public class LoginUserHandler(
    IUserRepository userRepository,
    ICryptoService cryptoService,
    ILogger<LoginUserHandler> logger)
    : IRequestHandler<LoginUserCommand, bool>
{
    public async Task<bool> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var login = request.Login;
        var password = request.Password;

        // If not found -> throw ex
        var user = await userRepository.GetUserByLoginAsync(login, cancellationToken);

        // TODO: Maybe Return False
        if (user is null)
            throw new UserNotFoundException(login);
        
        
        return cryptoService.VerifyUserByPassword(user, password);
    }
}
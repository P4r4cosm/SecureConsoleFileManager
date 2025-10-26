using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Users.LoginUser;

public record LoginUserCommand(string Login, string Password) : IRequest<MbResult<User>>;

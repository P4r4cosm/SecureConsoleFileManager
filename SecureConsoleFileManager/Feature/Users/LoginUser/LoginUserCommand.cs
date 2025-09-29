using MediatR;

namespace SecureConsoleFileManager.Feature.Users.LoginUser;

public record LoginUserCommand(string Login, string Password) : IRequest<bool>;

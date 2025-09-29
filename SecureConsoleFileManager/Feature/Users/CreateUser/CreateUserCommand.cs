using MediatR;

namespace SecureConsoleFileManager.Feature.Users.CreateUser;

public record CreateUserCommand(string Login, string Password) : IRequest<bool>;
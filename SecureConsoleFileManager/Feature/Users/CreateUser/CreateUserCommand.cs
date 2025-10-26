using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Users.CreateUser;

public record CreateUserCommand(string Login, string Password) : IRequest<MbResult>;
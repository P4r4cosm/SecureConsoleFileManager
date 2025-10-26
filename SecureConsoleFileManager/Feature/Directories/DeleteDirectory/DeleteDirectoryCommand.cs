using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Directories.DeleteDirectory;

public record class DeleteDirectoryCommand(string DirectoryName, bool Recursive) : IRequest<MbResult>;
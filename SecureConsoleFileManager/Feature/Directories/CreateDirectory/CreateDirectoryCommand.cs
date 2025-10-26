using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Directories.CreateDirectory;

public record CreateDirectoryCommand(string DirectoryName):  IRequest<MbResult>
{
    
}
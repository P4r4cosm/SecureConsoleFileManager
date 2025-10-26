using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Files.ReadFile;

public record ReadFileCommand(string FileArgument): IRequest<MbResult<string>>
{
    
}
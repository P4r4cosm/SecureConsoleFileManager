using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Files.DeleteFile;

public record DeleteFileCommand(string CommandArgument): IRequest<MbResult>
{
        
}

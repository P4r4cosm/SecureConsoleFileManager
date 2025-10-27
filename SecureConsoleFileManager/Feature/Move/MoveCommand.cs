using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Move;

public record MoveCommand(string SourcePath, string DestinationPath): IRequest<MbResult>
{
    
}
using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Files.WriteInFile;

public record WriteInFileCommand(string FilePath, string Info): IRequest<MbResult>;

using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Archives.CreateArchive;

public record class CreateArchiveCommand(
    string ArchiveName, 
    IEnumerable<string> SourcePaths) : IRequest<MbResult>;
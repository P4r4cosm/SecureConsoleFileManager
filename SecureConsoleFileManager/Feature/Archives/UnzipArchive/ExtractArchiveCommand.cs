using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Feature.Archives.UnzipArchive;

public record class ExtractArchiveCommand(
    string ArchivePath, 
    string DestinationDirectory) : IRequest<MbResult>;
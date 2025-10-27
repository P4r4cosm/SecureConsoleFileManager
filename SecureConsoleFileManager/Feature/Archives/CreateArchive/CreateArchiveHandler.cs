using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using File = SecureConsoleFileManager.Models.File;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;

namespace SecureConsoleFileManager.Feature.Archives.CreateArchive;

public class CreateArchiveHandler(
    ILogger<CreateArchiveHandler> logger,
    IArchiveService archiveService,
    ApplicationDbContext dbContext,
    ApplicationState state) : IRequestHandler<CreateArchiveCommand, MbResult>
{
    public async Task<MbResult> Handle(CreateArchiveCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogWarning("Unauthorized user attempted to create an archive.");
            return MbResult.Failure("Необходимо авторизоваться для создания архива.");
        }

        // Преобразуем пути, введенные пользователем, в полные относительные пути от корня
        var archivePath = Path.Combine(state.CurrentRelativePath, request.ArchiveName);
        var sourcePaths = request.SourcePaths
            .Select(p => Path.Combine(state.CurrentRelativePath, p))
            .ToList();

        logger.LogInformation("Attempting to create archive '{archivePath}' from {sourceCount} sources.",
            archivePath, sourcePaths.Count);

        var result = archiveService.SafeCreateArchive(sourcePaths, archivePath);

        if (result.IsSuccess)
        {
            var userId = state.CurrentUser!.Id;
            var fileInfo = result.Result;
            var file = new File
            {
                Name = fileInfo.Name,
                CreatedAt = fileInfo.CreationTimeUtc,
                Size = (uint)fileInfo.Length,
                Path = archivePath,
                UserId = userId
            };
            var operation = new Operation(OperationType.Creation, userId, file.Id);
            var entity = new LogEntity(LogLevel.Info, Command.CreateFile,
                $"Created archive {fileInfo.FullName}", operation);
            try
            {
                dbContext.Files.Add(file);
                dbContext.Operations.Add(operation);
                dbContext.Logs.Add(entity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while saving log to database {ex.Message}");
                return MbResult.Failure(
                    $"Физически архив создан, но произошла критическая ошибка при обновлении базы данных: {ex.Message}");
            }
            return MbResult.Success();
        }

        logger.LogError("Failed to create archive: {Error}", result.Error);
        return MbResult.Failure(result.Error);
    }
}
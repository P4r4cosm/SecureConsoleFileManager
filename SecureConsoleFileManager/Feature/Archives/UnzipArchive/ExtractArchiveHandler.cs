using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;
using File = SecureConsoleFileManager.Models.File;

namespace SecureConsoleFileManager.Feature.Archives.UnzipArchive;

public class ExtractArchiveHandler(
    ILogger<ExtractArchiveHandler> logger,
    IArchiveService archiveService,
    ApplicationDbContext dbContext,
    IOptions<FileSystemOptions> _options,
    ApplicationState state) : IRequestHandler<ExtractArchiveCommand, MbResult>
{
    public async Task<MbResult> Handle(ExtractArchiveCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogWarning("Unauthorized user attempted to extract an archive.");
            return MbResult.Failure("Необходимо авторизоваться для распаковки архива.");
        }

        var archivePath = Path.Combine(state.CurrentRelativePath, request.ArchivePath);
        var destinationPath = Path.Combine(state.CurrentRelativePath, request.DestinationDirectory);

        // 1. Выполняем физическую распаковку
        var result = archiveService.SafeExtract(archivePath, destinationPath);

        if (!result.IsSuccess)
        {
            logger.LogError("Failed to extract archive: {Error}", result.Error);
            return MbResult.Failure(result.Error!);
        }

        // Если архив был пустой, успешно выходим
        if (result.Result.Count == 0)
        {
            logger.LogInformation("Archive {archive} extracted to {dest}. No files were created.", archivePath,
                destinationPath);
            return MbResult.Success();
        }

        // 2. Синхронизируем базу 
        
        try
        {
            var filesToAdd = new List<File>();
            var operationsToAdd = new List<Operation>();
            var logsToAdd = new List<LogEntity>();
            var userId = state.CurrentUser.Id;

            foreach (var fileInfo in result.Result)
            {
                // Создаем новую сущность файла для нашей БД
                var newFile = new File
                {
                    Name = fileInfo.Name,
                    Path = Path.Combine(destinationPath, fileInfo.Name), // Преобразуем полный путь в относительный
                    Size = (uint)fileInfo.Length,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    UserId = userId
                };
                filesToAdd.Add(newFile);

                // Создаем связанные сущности
                var operation = new Operation(OperationType.Creation, userId, newFile.Id);
                operationsToAdd.Add(operation);

                var log = new LogEntity(LogLevel.Info, Command.CreateFile,
                    $"File created during archive extraction '{archivePath}'", operation);
                logsToAdd.Add(log);
            }

            dbContext.Files.AddRange(filesToAdd);
            dbContext.Operations.AddRange(operationsToAdd);
            dbContext.Logs.AddRange(logsToAdd);

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully synchronized DB after extracting archive. Added {count} file records.",
                filesToAdd.Count);

            return MbResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "CRITICAL: Failed to update database after extracting archive '{archivePath}'. DB is now out of sync.",
                archivePath);
            return MbResult.Failure(
                $"Файлы из архива извлечены, но произошла критическая ошибка при обновлении базы данных: {ex.Message}");
        }
    }
}
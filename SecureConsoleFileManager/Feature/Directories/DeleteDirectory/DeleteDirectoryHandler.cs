using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;

namespace SecureConsoleFileManager.Feature.Directories.DeleteDirectory;

public class DeleteDirectoryHandler(
    ILogger<DeleteDirectoryHandler> logger,
    ApplicationDbContext dbContext,
    IFileManagerService fileManagerService,
    IOptions<FileSystemOptions> options,
    ApplicationState state) : IRequestHandler<DeleteDirectoryCommand, MbResult>
{
    public async Task<MbResult> Handle(DeleteDirectoryCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogInformation("An unauthorized user attempted to delete a directory");
            return MbResult.Failure("Вам необходимо авторизоваться для удаления директории.");
        }

        var path = request.DirectoryName;
        if (!path.Contains(Path.DirectorySeparatorChar) && !path.Contains(Path.AltDirectorySeparatorChar))
        {
            path = Path.Combine(state.CurrentRelativePath, path);
        }

        var result = fileManagerService.DeleteDirectory(path, request.Recursive);

        if (result.IsSuccess)
        {
            
            logger.LogInformation($"Deleted directory {path}");
            
            if (result.Result.Count == 0)
            {
                logger.LogInformation("Directory {Path} deleted successfully. No files to remove from DB.", path);
                return MbResult.Success();
            }

            var logEntity = new LogEntity(LogLevel.Info,
                Command.DeleteDirectory, $"Deleted directory {path}");
            // Собираем в массив относительные пути до файлов для поиска
            var filePathes = result.Result.Select(x => options.Value.GetUserPath(x.FullName)).ToList();

            // На основе этих путей запрашиваем из базы файлы
            var files = dbContext.Files.Where(file => filePathes.Contains(file.Path)).ToList();
            var operationList = new List<Operation>();
            var logEntitiesList = new List<LogEntity>();
            var userId = state.CurrentUser!.Id;
            foreach (var file in files)
            {
                var operation = new Operation(OperationType.Deletion, userId, file.Id);
                var entity = new LogEntity(LogLevel.Info, Command.DeleteFile,
                    $"Automatic file deletion (durifilesToRemoveng recursive delete of '{path}')", operation);
                operationList.Add(operation);
                logEntitiesList.Add(entity);
            }
            try
            {
                dbContext.Logs.Add(logEntity);
                dbContext.Operations.AddRange(operationList);
                dbContext.Logs.AddRange(logEntitiesList);
                dbContext.Files.RemoveRange(files);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Successfully synchronized DB after deleting directory {path}. Removed {Count} file records.", path, filePathes.Count);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while saving log to database {ex.Message}");
                return MbResult.Failure($"Физически директория удалена, но произошла критическая ошибка при обновлении базы данных: {ex.Message}");
            }

            return MbResult.Success();
        }

        logger.LogInformation($"Directory deletion failed: {result.Error}");
        return MbResult.Failure(result.Error!);
    }
}
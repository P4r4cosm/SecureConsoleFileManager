using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;
namespace SecureConsoleFileManager.Feature.Move;

public class MoveHandler(
    ILogger<MoveHandler> logger,
    ApplicationDbContext dbContext,
    IFileManagerService fileManagerService,
    IOptions<FileSystemOptions> options,
    ApplicationState state) : IRequestHandler<MoveCommand, MbResult>
{
    public async Task<MbResult> Handle(MoveCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogInformation("An unauthorized user attempted to move a file or directory");
            return MbResult.Failure("Вам необходимо авторизоваться для перемещения файлов или директорий.");
        }

        var sourcePath = request.SourcePath;
        if (!Path.IsPathRooted(sourcePath))
        {
            sourcePath = Path.Combine(state.CurrentRelativePath, sourcePath);
        }

        var destinationPath = request.DestinationPath;
        if (!Path.IsPathRooted(destinationPath))
        {
            destinationPath = Path.Combine(state.CurrentRelativePath, destinationPath);
        }

        var result = fileManagerService.Move(sourcePath, destinationPath);
        // Создание сущностей, изменение всех файлов и т.д.
        if (result.IsSuccess)
        {
            
            logger.LogInformation($"Moved {sourcePath} to {destinationPath}");
            
            if (result.Result.Count == 0)
            {
                logger.LogInformation("Directory {Path} moved successfully. No files to replace from DB.", sourcePath);
                return MbResult.Success();
            }

            var logEntity = new LogEntity(LogLevel.Info,
                Command.DeleteDirectory, $"Moved directory {sourcePath}");
            // Собираем в массив относительные пути до файлов для поиска
            var filePathes = result.Result.Select(x => options.Value.GetUserPath(x.FullName)).ToList();

            // На основе этих путей запрашиваем из базы файлы
            var files = dbContext.Files.Where(file => filePathes.Contains(file.Path)).ToList();
            var operationList = new List<Operation>();
            var logEntitiesList = new List<LogEntity>();
            var userId = state.CurrentUser!.Id;
            foreach (var file in files)
            {
                // TODO: пофиксить для одного файла
                file.Path=Path.Combine(destinationPath, file.Name);
                var operation = new Operation(OperationType.Move, userId, file.Id);
                var entity = new LogEntity(LogLevel.Info, Command.Move,
                    $"Automatic file move (with recursive '{sourcePath}')", operation);
                operationList.Add(operation);
                logEntitiesList.Add(entity);
            }
            try
            {
                dbContext.Operations.AddRange(operationList);
                dbContext.Logs.AddRange(logEntitiesList);
                dbContext.Files.UpdateRange(files);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Successfully synchronized DB after move directory {path}. Changed {Count} file records.", sourcePath, filePathes.Count);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while saving log to database {ex.Message}");
                return MbResult.Failure($"Физически перемещение произошло, но произошла критическая ошибка при обновлении базы данных: {ex.Message}");
            }

            return MbResult.Success();
        }

        logger.LogInformation($"Move operation failed: {result.Error}");
        return MbResult.Failure(result.Error!);
    }
}
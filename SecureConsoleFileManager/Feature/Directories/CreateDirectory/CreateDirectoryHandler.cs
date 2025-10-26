using MediatR;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;

namespace SecureConsoleFileManager.Feature.Directories.CreateDirectory;

public class CreateDirectoryHandler(
    ILogger<CreateDirectoryHandler> logger,
    ApplicationDbContext dbContext,
    IFileManagerService fileManagerService,
    ApplicationState state) : IRequestHandler<CreateDirectoryCommand, MbResult>
{
    public async Task<MbResult> Handle(CreateDirectoryCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogInformation("An unauthorized user attempted to create a directory");
            return MbResult.Failure("Вам необходимо авторизоваться для создания директории.");
        }

        var path = request.DirectoryName;
        // Если указано только имя, создаем директорию в текущей
        if (!path.Contains(Path.DirectorySeparatorChar) && !path.Contains(Path.AltDirectorySeparatorChar))
        {
            path = Path.Combine(state.CurrentRelativePath, path);
        }

        var result = fileManagerService.CreateDirectory(path);

        if (result.IsSuccess)
        {
            logger.LogInformation($"Created directory {path}");
            var logEntity = new LogEntity(LogLevel.Info,
                Command.CreateDirectory, $"Created directory {path}");
            try
            {
                dbContext.Logs.Add(logEntity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while saving log to database {ex.Message}");
                return MbResult.Failure($"Произошла ошибка при обновлении базы данных: {ex.Message}");
            }

            return MbResult.Success();
        }
        logger.LogInformation($"Directory creation failed: {result.Error}");
        return MbResult.Failure(result.Error!);
    }
}
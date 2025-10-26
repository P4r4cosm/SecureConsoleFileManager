using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Feature.Files.CreateFile;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;

namespace SecureConsoleFileManager.Feature.Files.WriteInFile;

public class WriteInFileHandler(
    ILogger<WriteInFileHandler> logger,
    ApplicationDbContext dbContext,
    IFileManagerService fileManagerService,
    ApplicationState state) : IRequestHandler<WriteInFileCommand, MbResult>
{
    public async Task<MbResult> Handle(WriteInFileCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogInformation("An unauthorized user attempted to delete a file");
            return MbResult.Failure("You need to log in to delete a file");
        }

        var param = request.FilePath;
        // Если путь не указан, то создаём файл в текущей папке, если указан, то пробуем создать по указанному пути
        if (!(param.Contains("/") || param.Contains("\\")))
            param = Path.Combine(state.CurrentRelativePath, param);
        var info = request.Info;
        var result = await fileManagerService.AppendToFileAsync(param, info);
        if (result.IsSuccess)
        {
            try
            {
                var fileInfo = result.Result;
                var file = await dbContext.Files.Where(x => x.Path == param).FirstOrDefaultAsync();
                if (file is null)
                    return MbResult.Failure("File not found in postgres");
                var userId = state.CurrentUser!.Id;
                file.Size = (uint)fileInfo.Length;
                var operation = new Operation
                {
                    Created = file.CreatedAt,
                    Type = OperationType.Modification,
                    UserId = userId,
                    FileId = file.Id
                };
                var logEntity = new LogEntity(SecureConsoleFileManager.Models.LogLevel.Info,
                    Command.WriteInFile, $"Write in file {file.Name}", operation);
                dbContext.Files.Update(file);
                dbContext.Operations.Add(operation);
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

        logger.LogInformation("Write in file error: {result.Error}");
        return MbResult.Failure(result.Error);
    }
}
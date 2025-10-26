using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Feature.Files.CreateFile;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;

namespace SecureConsoleFileManager.Feature.Files.DeleteFile;

public class DeleteFileHandler    (ILogger<DeleteFileHandler> logger,
    ApplicationDbContext dbContext,
IFileManagerService fileManagerService,
    ApplicationState state) : IRequestHandler<DeleteFileCommand, MbResult>
{
    public async Task<MbResult> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogInformation("An unauthorized user attempted to delete a file");
            return MbResult.Failure("You need to log in to delete a file");
        }
        
        var param = request.CommandArgument;
        // Если путь не указан, то создаём файл в текущей папке, если указан, то пробуем создать по указанному пути
        if (!(param.Contains("/") || param.Contains("\\")))
            param = Path.Combine(state.CurrentRelativePath, param);

        var result = fileManagerService.DeleteFile(param);
        if (result.IsSuccess)
        {
            try
            {
                var file = await dbContext.Files.Where(x => x.Path == param).FirstOrDefaultAsync();
                if (file is null)
                    return MbResult.Failure("File not found in postgres");
                var userId = state.CurrentUser!.Id;
                var operation = new Operation
                {
                    Created = file.CreatedAt,
                    Type = OperationType.Deletion,
                    UserId = userId,
                    FileId = file.Id
                };
                var logEntity = new LogEntity(SecureConsoleFileManager.Models.LogLevel.Info,
                    Command.DeleteFile, $"Deleted file {file.Name}", operation);
                dbContext.Files.Remove(file);
                dbContext.Operations.Add(operation);
                dbContext.Logs.Add(logEntity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while saving log to database {ex.Message}");
            }
            return MbResult.Success();
        }
        logger.LogInformation("File deleting failed: {result.Error}");
        return MbResult.Failure(result.Error);
    }
}
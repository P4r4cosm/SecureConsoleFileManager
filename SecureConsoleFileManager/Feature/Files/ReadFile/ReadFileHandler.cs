using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Feature.Files.WriteInFile;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;

namespace SecureConsoleFileManager.Feature.Files.ReadFile;

public class ReadFileHandler(
    ILogger<ReadFileHandler> logger,
    ApplicationDbContext dbContext,
    IFileManagerService fileManagerService,
    ApplicationState state) : IRequestHandler<ReadFileCommand, MbResult<string>>
{
    public async Task<MbResult<string>> Handle(ReadFileCommand request, CancellationToken cancellationToken)
    {
        if (state.CurrentUser is null)
        {
            logger.LogInformation("An unauthorized user attempted to create a file");
            return MbResult<string>.Failure("You need to log in to create a file");
        }

        var param = request.FileArgument;
        // Если путь не указан, то создаём файл в текущей папке, если указан, то пробуем создать по указанному пути
        if (!(param.Contains("/") || param.Contains("\\")))
            param = Path.Combine(state.CurrentRelativePath, param);
        var result = await fileManagerService.ReadFileAsync(param);
        if (result.IsSuccess)
        {
            try
            {
                var file = await dbContext.Files.Where(x => x.Path == param).FirstOrDefaultAsync();
                if (file is null)
                    return MbResult<string>.Failure("File not found in postgres");
                var userId = state.CurrentUser!.Id;
                var operation = new Operation
                {
                    Created = file.CreatedAt,
                    Type = OperationType.Read,
                    UserId = userId,
                    FileId = file.Id
                };
                var logEntity = new LogEntity(SecureConsoleFileManager.Models.LogLevel.Info,
                    Command.ReadFile, $"Read file {file.Name}", operation);
                dbContext.Operations.Add(operation);
                dbContext.Logs.Add(logEntity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error while saving log to database {ex.Message}");
            }

            return MbResult<string>.Success(result.Result);
        }

        logger.LogInformation("File reading failed: {result.Error}");
        return MbResult<string>.Failure(result.Error);
    }
}


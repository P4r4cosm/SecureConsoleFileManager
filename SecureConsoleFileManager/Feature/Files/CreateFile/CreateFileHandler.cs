using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using Serilog;
using File = SecureConsoleFileManager.Models.File;
using LogLevel = SecureConsoleFileManager.Models.LogLevel;

namespace SecureConsoleFileManager.Feature.Files.CreateFile
{
    public class CreateFileHandler(
        ILogger<CreateFileHandler> logger,
        ApplicationDbContext dbContext,
        IFileManagerService fileManagerService,
        ApplicationState state) : IRequestHandler<CreateFileCommand, MbResult>
    {
        public async Task<MbResult> Handle(CreateFileCommand request, CancellationToken cancellationToken)
        {
            if (state.CurrentUser is null)
            {
                logger.LogInformation("An unauthorized user attempted to create a file");
                return MbResult.Failure("You need to log in to create a file");
            }
            var param = request.CommandArgument;
            // Если путь не указан, то создаём файл в текущей папке, если указан, то пробуем создать по указанному пути
            if (!(param.Contains("/") || param.Contains("\\")))
                param = Path.Combine(state.CurrentRelativePath, param);
            var result = fileManagerService.CreateFile(param);
            if (result.IsSuccess)
            {
                var file = result.Result;
                var userId = state.CurrentUser!.Id;
                file.UserId = userId;
                logger.LogInformation($"Created file {file.Name}");
                var operation = new Operation
                {
                    Created = file.CreatedAt,
                    Type = OperationType.Creation,
                    UserId = userId,
                    FileId = file.Id
                };
                var logEntity = new LogEntity(SecureConsoleFileManager.Models.LogLevel.Info,
                    Command.CreateFile, $"Created file {file.Name}", operation);
                try
                {
                    dbContext.Files.Add(file);
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
            logger.LogInformation("File creation failed: {result.Error}");
            return MbResult.Failure(result.Error);
        }
    }
}
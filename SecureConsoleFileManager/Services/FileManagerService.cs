using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;
using System.IO;
using System.Threading.Tasks;
using File = System.IO.File;

namespace SecureConsoleFileManager.Services;

public interface IFileManagerService
{
    public MbResult CreateDirectory(string relativePath);
    public MbResult<string> ValidateAndGetFullPath(string relativePath);

    public MbResult DeleteDirectory(string relativePath, bool recursive);

    public MbResult Move(string sourceRelativePath, string destinationRelativePath);

    public MbResult<DirectoryInfo> GetDirectoryInfo(string currentDirectoryPath);
    public Task<MbResult<string>> ReadFileAsync(string relativePath);

    public MbResult<SecureConsoleFileManager.Models.File> CreateFile(string relativePath);
    public MbResult DeleteFile(string relativePath);
    public Task<MbResult<FileInfo>> AppendToFileAsync(string relativePath, string contentToAppend);
}

public class FileManagerService(
    IOptions<FileSystemOptions> fileSystemOptions,
    ILogger<FileManagerService> logger,
    ILockerService lockerService)
    : IFileManagerService
{
    private readonly FileSystemOptions _fileSystemOptions = fileSystemOptions.Value;

    // Этот метод остается публичным, так как он полезен и для других сервисов (например, ArchiveService)
    public MbResult<string> ValidateAndGetFullPath(string relativePath)
    {
        // if (string.IsNullOrWhiteSpace(relativePath))
        // {
        //     return MbResult<string>.Failure("Путь не может быть пустым.");
        // }

        if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return MbResult<string>.Failure("Путь содержит недопустимые символы.");
        }

        var rootPath = _fileSystemOptions.FullStartPath;
        // Path.Combine корректно обработает, если relativePath - это уже корень.
        var combinedPath = Path.Combine(rootPath, relativePath);
        var fullPath = Path.GetFullPath(combinedPath);

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Обнаружена попытка обхода каталога: '{relativePath}'", relativePath);
            return MbResult<string>.Failure("Обнаружена попытка обхода каталога (Path Traversal).");
        }

        return MbResult<string>.Success(fullPath);
    }

    // ================ Операции с Директориями ==================

    public MbResult CreateDirectory(string relativePath)
    {
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess) return MbResult.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;

        return lockerService.ExecuteLocked(fullPath, () =>
        {
            if (Directory.Exists(fullPath))
            {
                return MbResult.Failure($"Директория '{relativePath}' уже существует.");
            }

            if (File.Exists(fullPath))
            {
                return MbResult.Failure($"Объект с именем '{relativePath}' уже существует как файл.");
            }

            Directory.CreateDirectory(fullPath);
            logger.LogInformation("Директория создана: {path}", relativePath);
            return MbResult.Success();
        });
    }

    public MbResult DeleteDirectory(string relativePath, bool recursive = true)
    {
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess) return MbResult.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;

        return lockerService.ExecuteLocked(fullPath, () =>
        {
            if (!Directory.Exists(fullPath))
            {
                return MbResult.Failure($"Директория не найдена: {relativePath}");
            }

            Directory.Delete(fullPath, recursive);
            logger.LogInformation("Директория удалена: {path}", relativePath);
            return MbResult.Success();
        });
    }

    public MbResult<DirectoryInfo> GetDirectoryInfo(string relativePath)
    {
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess)
            return MbResult<DirectoryInfo>.Failure(validationResult.Error!);

        var validatedFullPath = validationResult.Result;
        if (Directory.Exists(validatedFullPath))
            return MbResult<DirectoryInfo>.Success(new DirectoryInfo(validatedFullPath));

        return MbResult<DirectoryInfo>.Failure($"Директория не найдена: {relativePath}");
    }

    // ================ Операции с Файлами ==================

    public MbResult<SecureConsoleFileManager.Models.File> CreateFile(string relativePath)
    {
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess)
            return MbResult<SecureConsoleFileManager.Models.File>.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;

        var result = lockerService.ExecuteLocked(fullPath, () =>
        {
            if (File.Exists(fullPath))
            {
                return MbResult.Failure($"Файл '{relativePath}' уже существует.");
            }

            if (Directory.Exists(fullPath))
            {
                return MbResult.Failure($"Объект с именем '{relativePath}' уже существует как директория.");
            }

            // Создаем директорию, если ее нет
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, string.Empty);
            logger.LogInformation("Файл создан: {path}", relativePath);
            return MbResult.Success();
        });
        if (result.IsSuccess)
        {
            var fileInfo = new FileInfo(fullPath);
            var file = new SecureConsoleFileManager.Models.File
            {
                Name = fileInfo.Name,
                Path = relativePath,
                CreatedAt = fileInfo.CreationTimeUtc,
                Size = (uint)fileInfo.Length
            };
            return MbResult<SecureConsoleFileManager.Models.File>.Success(file);
        }

        return MbResult<SecureConsoleFileManager.Models.File>.Failure(result.Error!);
    }

    /// <summary>
    /// Return relativePath
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns> Return relativePath</returns>
    public MbResult DeleteFile(string relativePath)
    {
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess)
            return MbResult.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;

        return lockerService.ExecuteLocked(fullPath, () =>
        {
            if (!File.Exists(fullPath))
                return MbResult.Failure($"Файл не найден: {relativePath}");

            File.Delete(fullPath);
            logger.LogInformation("Файл удален: {path}", relativePath);
            return MbResult.Success();
        });
    }

    public async Task<MbResult<FileInfo>> AppendToFileAsync(string relativePath, string contentToAppend)
    {
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess) return MbResult<FileInfo>.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;

        var res =  lockerService.ExecuteLocked(fullPath,  () =>
        {
            if (!File.Exists(fullPath))
                return MbResult.Failure($"Файл не найден: {relativePath}");

             File.AppendAllText(fullPath, contentToAppend);
            logger.LogInformation("Данные дописаны в файл: {path}", relativePath);
            return MbResult.Success();
        });
        if (res.IsSuccess)
        {
            var fileInfo = new FileInfo(fullPath);
            return MbResult<FileInfo>.Success(fileInfo);
        }

        return MbResult<FileInfo>.Failure(res.Error!);
    }

    public async Task<MbResult<string>> ReadFileAsync(string relativePath)
    {
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess) return MbResult<string>.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;
        try
        {
            if (!File.Exists(fullPath))
            {
                return MbResult<string>.Failure($"Файл не найден: {relativePath}");
            }

            string text = await File.ReadAllTextAsync(fullPath);
            logger.LogInformation("Файл прочитан: {path}", relativePath);
            return MbResult<string>.Success(text);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при чтении файла: {path}", fullPath);
            return MbResult<string>.Failure($"Ошибка при чтении файла: {e.Message}");
        }
    }

    // ================ Общие операции ==================

    public MbResult Move(string sourceRelativePath, string destinationRelativePath)
    {
        var sourceValidation = ValidateAndGetFullPath(sourceRelativePath);
        if (!sourceValidation.IsSuccess) return MbResult.Failure($"Неверный исходный путь: {sourceValidation.Error}");
        var fullSourcePath = sourceValidation.Result;

        var destValidation = ValidateAndGetFullPath(destinationRelativePath);
        if (!destValidation.IsSuccess) return MbResult.Failure($"Неверный целевой путь: {destValidation.Error}");
        var fullDestPath = destValidation.Result;

        return lockerService.ExecuteLocked(fullSourcePath, fullDestPath, () =>
        {
            if (!File.Exists(fullSourcePath) && !Directory.Exists(fullSourcePath))
            {
                return MbResult.Failure($"Исходный путь не найден: {sourceRelativePath}");
            }

            if (File.Exists(fullDestPath) || Directory.Exists(fullDestPath))
            {
                return MbResult.Failure($"Целевой путь уже занят: {destinationRelativePath}");
            }

            // Создаем директорию, если ее нет
            Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath));

            Directory.Move(fullSourcePath, fullDestPath);
            logger.LogInformation("Ресурс перемещен из {source} в {dest}", sourceRelativePath, destinationRelativePath);
            return MbResult.Success();
        });
    }
}
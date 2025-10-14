using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;
using File = System.IO.File;

namespace SecureConsoleFileManager.Services;

// ПОД CurrentDirectoryPath имеется ввиду относительный путь пользователя
public class FileManagerService(IOptions<FileSystemOptions> fileSystemOptions, ILogger<FileManagerService> logger)
    : IFileManagerService
{
    private readonly FileSystemOptions _fileSystemOptions = fileSystemOptions.Value;


    // ================ Директории ==================


    // Он либо вернет ошибку, либо безопасный полный путь
    public MbResult<string> ValidateAndGetFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return MbResult<string>.Failure("Путь не может быть пустым.");
        }

        // Проверяем на недопустимые символы в самом начале
        if (relativePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return MbResult<string>.Failure("Путь содержит недопустимые символы.");
        }

        var rootPath = _fileSystemOptions.FullStartPath;
        var combinedPath = Path.Combine(rootPath, relativePath);
        var fullPath = Path.GetFullPath(combinedPath);

        // Основная проверка безопасности
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Обнаружена попытка обхода каталога: '{relativePath}'", relativePath);
            return MbResult<string>.Failure("Обнаружена попытка обхода каталога (Path Traversal).");
        }

        return MbResult<string>.Success(fullPath);
    }

    public MbResult CreateDirectory(string directoryName, string currentDirectoryPath)
    {
        var relativePath = Path.Combine(_fileSystemOptions.GetUserPath(currentDirectoryPath), directoryName);
        var validationResult = ValidateAndGetFullPath(relativePath);

        if (!validationResult.IsSuccess)
        {
            return MbResult.Failure(validationResult.Error!);
        }

        var fullPath = validationResult.Result;

        try
        {
            if (Directory.Exists(fullPath))
            {
                return MbResult.Failure($"Директория '{directoryName}' уже существует.");
            }

            Directory.CreateDirectory(fullPath);
            logger.LogInformation("Директория создана: {userPath}", relativePath);
            return MbResult.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при создании директории: {fullPath}", fullPath);
            return MbResult.Failure($"Внутренняя ошибка при создании директории: {e.Message}");
        }
    }

    public MbResult DeleteDirectory(string currentDirectoryPath)
    {
        var validationResult = ValidateAndGetFullPath(currentDirectoryPath);

        if (!validationResult.IsSuccess)
        {
            return MbResult.Failure(validationResult.Error!);
        }

        var validatedFullPath = validationResult.Result;

        try
        {
            if (!Directory.Exists(validatedFullPath))
            {
                return MbResult.Failure($"Директория не найдена: {currentDirectoryPath}");
            }

            // Выполняем рекурсивное удаление
            Directory.Delete(validatedFullPath, true);
            logger.LogInformation("Директория удалена: {path}", validatedFullPath);
            return MbResult.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении директории: {path}", validatedFullPath);
            return MbResult.Failure($"Ошибка при удалении: {e.Message}");
        }
    }

    public MbResult ChangeDirectoryName(string newDirectoryName, string currentDirectoryPath)
    {
        // 1. Валидируем исходный путь
        var validationResult = ValidateAndGetFullPath(currentDirectoryPath);
        if (!validationResult.IsSuccess)
        {
            return MbResult.Failure(validationResult.Error!);
        }

        var validatedFullPath = validationResult.Result;

        // 2. Проверяем, что новое имя - это просто имя, а не попытка атаки
        if (newDirectoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return MbResult.Failure("Новое имя директории содержит недопустимые символы.");
        }

        try
        {
            if (!Directory.Exists(validatedFullPath))
            {
                return MbResult.Failure($"Исходная директория не найдена: {currentDirectoryPath}");
            }

            var directoryInfo = new DirectoryInfo(validatedFullPath);
            // Строим полный путь назначения
            var destinationPath = Path.Combine(directoryInfo.Parent!.FullName, newDirectoryName);

            if (Directory.Exists(destinationPath))
            {
                return MbResult.Failure($"Директория с именем '{newDirectoryName}' уже существует.");
            }

            // Используем правильные пути
            Directory.Move(validatedFullPath, destinationPath);
            logger.LogInformation("Директория {source} переименована в {dest}", validatedFullPath, destinationPath);
            return MbResult.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при переименовании директории {path}", validatedFullPath);
            return MbResult.Failure($"Ошибка при переименовании: {e.Message}");
        }
    }

    public MbResult<DirectoryInfo> GetDirectoryInfo(string currentDirectoryPath)
    {
        var validationResult = ValidateAndGetFullPath(currentDirectoryPath);
        if (!validationResult.IsSuccess)

            return MbResult<DirectoryInfo>.Failure(validationResult.Error!);
        var  validatedFullPath = validationResult.Result;
        if (Directory.Exists(validatedFullPath))
            return MbResult<DirectoryInfo>.Success(new DirectoryInfo(validatedFullPath));
        return MbResult<DirectoryInfo>.Failure("Директория не найдена");
    }

    // Перегруженные методы теперь просто вызывают основные
    public MbResult DeleteDirectory(string directoryName, string currentDirectoryPath)
    {
        var path = Path.Combine(currentDirectoryPath, directoryName);
        return DeleteDirectory(path);
    }

    public MbResult ChangeDirectoryName(string newDirectoryName, string directoryName, string currentDirectoryPath)
    {
        var path = Path.Combine(currentDirectoryPath, directoryName);
        return ChangeDirectoryName(newDirectoryName, path);
    }

    // ================ Файлы ==================


    // TODO: 1. Обо всех операциях с файлами нужно делать уведомления
    // TODO: 2? Вынести операции с файлами в отдельный сервис, а этот переименовать под директории
    public MbResult CreateFile(string fileName, string currentDirectoryPath)
    {
        var relativeFilePath = Path.Combine(currentDirectoryPath, fileName);
        var validationResult = ValidateAndGetFullPath(relativeFilePath);

        if (!validationResult.IsSuccess) return MbResult.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;

        try
        {
            if (File.Exists(fullPath))
            {
                return MbResult.Failure($"Файл '{fileName}' уже существует.");
            }

            File.WriteAllText(fullPath, string.Empty);
            logger.LogInformation("Файл создан: {path}", relativeFilePath);


            return MbResult.Success();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при создании файла: {path}", fullPath);
            return MbResult.Failure($"Внутренняя ошибка при создании файла: {exception.Message}");
        }
    }

    public MbResult DeleteFile(string fullFilePath)
    {
        var relativePath = _fileSystemOptions.GetUserPath(fullFilePath);
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess) return MbResult.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;
        try
        {
            if (!File.Exists(fullPath))
                return MbResult.Failure($"Файл не найден: {relativePath}");
            File.Delete(fullPath);
            logger.LogInformation("Файл удален: {path}", relativePath);
            return MbResult.Success();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при удалении файла: {path}", fullPath);
            return MbResult.Failure($"Ошибка при удалении файла: {exception.Message}");
        }
    }

    public async Task<MbResult> AppendToFileAsync(string fullFilePath, string contentToAppend)
    {
        var relativePath = _fileSystemOptions.GetUserPath(fullFilePath);
        var validationResult = ValidateAndGetFullPath(relativePath);
        if (!validationResult.IsSuccess) return MbResult.Failure(validationResult.Error!);

        var fullPath = validationResult.Result;
        try
        {
            if (!File.Exists(fullPath))
                return MbResult.Failure($"Файл не найден: {relativePath}");

            await File.AppendAllTextAsync(fullPath, contentToAppend);
            logger.LogInformation("Данные дописаны в файл: {path}", relativePath);

            return MbResult.Success();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при дописывании в файл: {path}", fullPath);
            return MbResult.Failure($"Ошибка при записи в файл: {e.Message}");
        }
    }

    public async Task<MbResult<string>> ReadFileAsync(string fullFilePath)
    {
        var validationResult = ValidateAndGetFullPath(fullFilePath);
        if (!validationResult.IsSuccess) return validationResult;
        
        var fullPath = validationResult.Result;
        try
        {
            if (!File.Exists(fullPath))
            {
                return MbResult<string>.Failure($"Файл не найден: {fullFilePath}");
            }
            
            string text = await File.ReadAllTextAsync(fullPath);
            logger.LogInformation("Файл прочитан: {path}", fullFilePath);
            return MbResult<string>.Success(text);
        }
        catch(Exception e)
        {
            logger.LogError(e, "Ошибка при чтении файла: {path}", fullPath);
            return MbResult<string>.Failure($"Ошибка при чтении файла: {e.Message}");
        }
    }
}
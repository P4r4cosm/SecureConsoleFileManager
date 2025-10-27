using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;
using File = System.IO.File;

namespace SecureConsoleFileManager.Services;

public interface IArchiveService
{
    public MbResult<FileInfo> SafeCreateArchive(IEnumerable<string> filePaths, string zipLocation);


    /// <summary>
    /// Безопасно извлекает файлы из архива и записывает в destinationDirectory
    /// </summary>
    /// <param name="zipLocation">относительный юзера путь к архиву</param>
    /// <param name="destinationDirectory">относительный путь юзера</param>
    /// <returns></returns>
    public MbResult<List<FileInfo>> SafeExtract(string zipLocation, string destinationDirectory);
}

public class ArchiveService(
    IOptions<FileSystemOptions> fileSystemOptions,
    ILogger<ArchiveService> logger,
    ILockerService lockerService,
    IFileManagerService fileManagerService) : IArchiveService
{
    // Максимальный размер одного распакованного файла (например, 100 МБ)
    private const long MaxFileSize = 100 * 1024 * 1024;

    // Максимальный общий размер всех распакованных файлов (например, 1 ГБ)
    private const long MaxTotalSize = 1024 * 1024 * 1024;

    // Максимальное количество файлов в архиве
    private const int MaxFileCount = 1000;

    // Максимальная глубина вложенности архивов
    private const int MaxRecursionDepth = 5;

    // Максимальный суммарный размер архивируемых файлов
    private const long MaxSourceTotalSize = 2L * 1024 * 1024 * 1024; // 2 GB

    // Максимальное число архивируемых файлов
    private const int MaxSourceFileCount = 10000;
    
    // Максимальный коэффициент сжатия (uncompressed / compressed)
    private const double MaxCompressionRatio = 100.0;

    /// <summary>
    /// Рекурсивно сканирует путь (файл или директорию), проверяя соответствие политикам безопасности.
    /// </summary>
    private MbResult ScanAndValidatePathRecursive(string fullPath, ref long totalSize, ref int fileCount,
        HashSet<string> processedPaths)
    {
        // Защита от дублирования и циклических ссылок
        if (!processedPaths.Add(fullPath))
        {
            return MbResult.Success(); // Уже обработано, просто пропускаем
        }

        // --- ОБРАБОТКА ФАЙЛА ---
        if (File.Exists(fullPath))
        {
            // Защита от символических ссылок
            if (File.ResolveLinkTarget(fullPath, true) is not null)
            {
                logger.LogWarning("Обнаружена символическая ссылка: {file}", fullPath);
                return MbResult.Failure($"Обнаружена символическая ссылка, операция прервана: {fullPath}");
            }

            fileCount++;
            if (fileCount > MaxSourceFileCount)
            {
                return MbResult.Failure($"Количество файлов для архивации превышает лимит ({MaxSourceFileCount}).");
            }

            totalSize += new FileInfo(fullPath).Length;
            if (totalSize > MaxSourceTotalSize)
            {
                return MbResult.Failure(
                    $"Общий размер файлов для архивации превышает лимит ({MaxSourceTotalSize} байт).");
            }

            return MbResult.Success();
        }

        // --- ОБРАБОТКА ДИРЕКТОРИИ ---
        if (Directory.Exists(fullPath))
        {
            // Рекурсивно обходим все содержимое директории
            foreach (var entry in Directory.EnumerateFileSystemEntries(fullPath))
            {
                var result = ScanAndValidatePathRecursive(entry, ref totalSize, ref fileCount, processedPaths);
                if (!result.IsSuccess)
                {
                    return result; // Немедленно выходим при первой же ошибке
                }
            }

            return MbResult.Success();
        }

        return MbResult.Failure($"Путь не найден: {fullPath}");
    }

    /// <summary>
    /// Безопасно создает ZIP-архив из переданного списка файлов и директорий.
    /// </summary>
    /// <param name="relativePaths">Перечисление относительных путей к файлам и папкам.</param>
    /// <param name="zipLocationRelative">Относительный путь для сохранения ZIP-архива.</param>
    public MbResult<FileInfo> SafeCreateArchive(IEnumerable<string> relativePaths, string zipLocationRelative)
    {
        var zipValidation = fileManagerService.ValidateAndGetFullPath(zipLocationRelative);

        if (!zipValidation.IsSuccess)
            return MbResult<FileInfo>.Failure($"Неверный путь для сохранения архива: {zipValidation.Error}");
        string fullZipPath = zipValidation.Result;

        var fullSourcePaths = new List<string>();
        foreach (var relativePath in relativePaths)
        {
            var fullPathValidation = fileManagerService.ValidateAndGetFullPath(relativePath);
            if (!fullPathValidation.IsSuccess)
                return MbResult<FileInfo>.Failure($"Неверный путь в списке источников: {fullPathValidation.Error}");
            fullSourcePaths.Add(fullPathValidation.Result);
        }

        long totalSize = 0;
        int fileCount = 0;
        var processedPaths = new HashSet<string>();
        foreach (var fullPath in fullSourcePaths)
        {
            var scanResult = ScanAndValidatePathRecursive(fullPath, ref totalSize, ref fileCount, processedPaths);
            if (!scanResult.IsSuccess)
                MbResult<FileInfo>.Failure(scanResult.Error!);
        }

        logger.LogInformation(
            "Проверка перед архивацией пройдена. Файлов: {fileCount}, общий размер: {totalSize} байт.", fileCount,
            totalSize);
        FileInfo fileInfo = null;
        var result = lockerService.ExecuteLocked(fullZipPath, () =>
        {
            try
            {
                using (var fileStream = new FileStream(fullZipPath, FileMode.Create, FileAccess.Write))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                {
                    var basePath = fileSystemOptions.Value.FullStartPath;
                    foreach (var path in processedPaths.Where(p => File.Exists(p)))
                    {
                        // Дополнительная проверка на симлинк для защиты от TOCTOU
                        if (File.ResolveLinkTarget(path, true) is not null)
                        {
                            logger.LogWarning("Пропуск символической ссылки во время архивации: {path}", path);
                            continue;
                        }

                        string entryName = Path.GetRelativePath(basePath, path);
                        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        using (var sourceStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            sourceStream.CopyTo(entryStream);
                        }
                    }
                }

                fileInfo = new FileInfo(fullZipPath);
                logger.LogInformation("Архив '{zipLocationRelative}' успешно создан.", zipLocationRelative);
                return MbResult.Success();
            }
            catch (Exception ex)
            {
                // Очистка делегируется внешнему try-catch в LockerService, но мы можем ее продублировать для надежности
                if (File.Exists(fullZipPath)) File.Delete(fullZipPath);
                // Логирование уже происходит в LockerService, но можно добавить специфичное для архива
                logger.LogError(ex, "Критическая ошибка при записи в ZIP-файл '{zipLocationRelative}'.",
                    zipLocationRelative);
                return MbResult.Failure("Внутренняя ошибка при создании архива.");
            }
        });
        return result.IsSuccess
            ? MbResult<FileInfo>.Success(fileInfo!)
            : MbResult<FileInfo>.Failure(result.Error!);
    }

    /// <summary>
    /// Безопасно извлекает файлы из архива и записывает в destinationDirectory
    /// </summary>
    /// <param name="zipLocation">относительный юзера путь к архиву</param>
    /// <param name="destinationDirectory">относительный путь юзера (если директории нет, она будет создана)</param>
    /// <returns></returns>
    public MbResult<List<FileInfo>> SafeExtract(string zipLocation, string destinationDirectory)
{
    var zipValidationResult = fileManagerService.ValidateAndGetFullPath(zipLocation);
    if (!zipValidationResult.IsSuccess)
        return MbResult<List<FileInfo>>.Failure($"Неверный путь к архиву: {zipValidationResult.Error}");
    
    string fullZipPath = zipValidationResult.Result;

    var destValidationResult = fileManagerService.ValidateAndGetFullPath(destinationDirectory);
    if (!destValidationResult.IsSuccess)
        return MbResult<List<FileInfo>>.Failure($"Неверный путь к папке назначения: {destValidationResult.Error}");
    
    string fullDestinationPath = destValidationResult.Result;
    string tempDirectory = Path.Combine(fileSystemOptions.Value.RootPath,"unpack_" + Guid.NewGuid().ToString());

    // 1. Объявляем переменную для результата ЗА ПРЕДЕЛАМИ лямбды
    List<FileInfo> extractedFiles = null;

    try
    {
        // 2. Выполняем блокировку. `lockerService` вернет обычный `MbResult`.
        var lockResult = lockerService.ExecuteLocked(fullDestinationPath, () =>
        {
            Directory.CreateDirectory(tempDirectory);
            long currentTotalSize = 0;
            int fileCount = 0;
            using (FileStream stream = File.OpenRead(fullZipPath))
            {
                var extractResult = SafeExtractRecursive(stream, tempDirectory, 0, ref currentTotalSize, ref fileCount);
                if (!extractResult.IsSuccess)
                {
                    // Лямбда возвращает обычный MbResult.Failure
                    return extractResult;
                }
            }

            if (Directory.Exists(fullDestinationPath))
            {
                logger.LogWarning("{destPath} уже существует, она будет удалена и заменена.", destinationDirectory);
                Directory.Delete(fullDestinationPath, true);
            }

            Directory.Move(tempDirectory, fullDestinationPath);

            // 3. В случае успеха, ПРИСВАИВАЕМ ЗНАЧЕНИЕ внешней переменной
            var destinationDirInfo = new DirectoryInfo(fullDestinationPath);
            extractedFiles = destinationDirInfo.GetFiles("*", SearchOption.AllDirectories).ToList();

            logger.LogInformation("Архив '{zipLocation}' успешно распакован в '{destinationDirectory}'. Найдено {count} файлов.", 
                zipLocation, destinationDirectory, extractedFiles.Count);
            
            // 4. Лямбда возвращает обычный MbResult.Success
            return MbResult.Success();
        });

        // 5. Анализируем результат работы lockerService
        if (!lockResult.IsSuccess)
        {
            // Если блокировка или операция внутри не удались, возвращаем ошибку
            return MbResult<List<FileInfo>>.Failure(lockResult.Error!);
        }

        // Если все прошло успешно, `extractedFiles` теперь содержит данные.
        // Возвращаем их в дженерик-результате.
        return MbResult<List<FileInfo>>.Success(extractedFiles);
    }
    finally
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}

    private MbResult SafeExtractRecursive(Stream zipStream, string destinationDirectory, int currentDepth,
        ref long totalSize, ref int fileCount)
    {
        if (currentDepth > MaxRecursionDepth)
        {
            logger.LogWarning("Превышена максимальная глубина вложенности архивов: {MaxRecursionDepth}",
                MaxRecursionDepth);
            return MbResult.Failure("Превышена максимальная вложенность архивов!!!");
        }

        using (ZipArchive zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            if (zipArchive.Entries.Count >= MaxFileCount)
            {
                return MbResult.Failure("Слишком много файлов в архиве!!!");
            }

            fileCount += zipArchive.Entries.Count;

            if (fileCount > MaxFileCount)
            {
                return MbResult.Failure("Превышено общее количество файлов во всех вложенных архивах.");
            }

            // Получаем полный, канонический путь к папке назначения для сравнения
            string destinationDirectoryFullPath = Path.GetFullPath(destinationDirectory + Path.DirectorySeparatorChar);

            foreach (var entry in zipArchive.Entries)
            {
                // проверка на zip slip (path traversal)
                string entryFullPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));

                if (!entryFullPath.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Обнаружена попытка обхода каталога (Zip Slip): '{entryFullName}'",
                        entry.FullName);
                    return MbResult.Failure("Обнаружена попытка обхода каталога (Zip Slip).");
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(entryFullPath);
                    continue;
                }


                if (entry.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string nestedDirectory = Path.Combine(destinationDirectory,
                        Path.GetFileNameWithoutExtension(entry.FullName));
                    Directory.CreateDirectory(nestedDirectory);

                    using (Stream nestedEntryStream = entry.Open())
                    {
                        // Копируем в MemoryStream, так как поток из ZipArchiveEntry может не поддерживать поиск (seeking),
                        // что необходимо для конструктора ZipArchive.
                        using (var memoryStream = new MemoryStream())
                        {
                            nestedEntryStream.CopyTo(memoryStream);
                            memoryStream.Position = 0; // Возвращаем указатель в начало потока

                            var recursiveResult = SafeExtractRecursive(memoryStream, nestedDirectory, currentDepth + 1,
                                ref totalSize, ref fileCount);

                            // Если на любом уровне вложенности произошла ошибка, немедленно прерываем всю операцию.
                            if (!recursiveResult.IsSuccess)
                            {
                                return recursiveResult;
                            }
                        }
                    }
                }
                // Если это обычный файл - распаковываем с проверкой размеров (защита от ZIP-бомб)
                else
                {
                    // проверка кэффа сжатия
                    if (entry.CompressedLength > 0) // Избегаем деления на ноль
                    {
                        double compressionRatio = (double)entry.Length / entry.CompressedLength;
                        if (compressionRatio > MaxCompressionRatio)
                        {
                            logger.LogWarning("Обнаружен подозрительно высокий коэффициент сжатия ({ratio}) для файла '{file}'. Операция прервана.", 
                                compressionRatio, entry.FullName);
                            return MbResult.Failure($"Файл '{entry.FullName}' имеет слишком высокий коэффициент сжатия. Возможно, это zip-бомба.");
                        }
                    }
                    // Также проверяем случай, когда сжатый размер 0, а распакованный - нет (бесконечный коэф.)
                    else if (entry.Length > 0)
                    {
                        logger.LogWarning("Обнаружен файл '{file}' с нулевым сжатым размером, но ненулевым размером после распаковки. Операция прервана.", entry.FullName);
                        return MbResult.Failure($"Файл '{entry.FullName}' имеет некорректные размеры (попытка распаковки из нуля байт).");
                    }
                    
                    // Убедимся, что директория для файла существует
                    Directory.CreateDirectory(Path.GetDirectoryName(entryFullPath));

                    using (Stream entryStream = entry.Open())
                    using (FileStream fileStream = new FileStream(entryFullPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[8192]; // Буфер 8KB
                        int bytesRead;
                        long currentFileSize = 0;

                        while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            currentFileSize += bytesRead;
                            totalSize += bytesRead;

                            // 4. ПРОВЕРКА НА МАКСИМАЛЬНЫЙ РАЗМЕР ФАЙЛА
                            if (currentFileSize > MaxFileSize)
                            {
                                return MbResult.Failure(
                                    $"Файл '{entry.FullName}' превышает максимальный размер ({MaxFileSize} байт).");
                            }

                            // 5. ПРОВЕРКА НА ОБЩИЙ РАЗМЕР РАСПАКОВКИ
                            if (totalSize > MaxTotalSize)
                            {
                                return MbResult.Failure(
                                    $"Общий размер распакованных данных превышает лимит ({MaxTotalSize} байт).");
                            }

                            fileStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }

        return MbResult.Success();
    }
}
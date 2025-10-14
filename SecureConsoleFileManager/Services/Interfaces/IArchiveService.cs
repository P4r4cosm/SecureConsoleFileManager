using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Services.Interfaces;

public interface IArchiveService
{
    
    
    public MbResult SafeCreateArchive(IEnumerable<string> filePaths, string zipLocation);
    
    
    /// <summary>
    /// Безопасно извлекает файлы из архива и записывает в destinationDirectory
    /// </summary>
    /// <param name="zipLocation">относительный юзера путь к архиву</param>
    /// <param name="destinationDirectory">относительный путь юзера</param>
    /// <returns></returns>
    public MbResult SafeExtract(string zipLocation, string destinationDirectory);
}
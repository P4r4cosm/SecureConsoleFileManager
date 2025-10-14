using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Services.Interfaces;

public interface IFileManagerService
{

    public MbResult CreateDirectory(string directoryName, string currentDirectoryPath);
    public MbResult<string> ValidateAndGetFullPath(string relativePath);

    public MbResult DeleteDirectory(string currentDirectoryPath);
    public MbResult DeleteDirectory(string directoryName, string currentDirectoryPath);

    public MbResult ChangeDirectoryName(string newDirectoryName, string currentDirectoryPath);
    
    public MbResult ChangeDirectoryName(string newDirectoryName, string directoryName, string currentDirectoryPath);
    public MbResult<DirectoryInfo> GetDirectoryInfo(string currentDirectoryPath);
    
}
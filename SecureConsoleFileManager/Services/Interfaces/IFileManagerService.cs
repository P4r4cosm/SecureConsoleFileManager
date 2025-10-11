using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Services.Interfaces;

public interface IFileManagerService
{

    public MbResult CreateDirectory(string directoryName, string currentDirectoryPath);
    
    
    public MbResult DeleteDirectory(string directoryName, string currentDirectoryPath);
    
    public MbResult DeleteDirectory(string fullDirectoryPath);
    
    public MbResult ChangeDirectoryName(string newDirectoryName, string fullDirectoryPath);
    
    public MbResult ChangeDirectoryName(string newDirectoryName, string directoryName, string currentDirectoryPath);
}
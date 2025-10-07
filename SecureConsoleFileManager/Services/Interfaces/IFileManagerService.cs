namespace SecureConsoleFileManager.Services.Interfaces;

public interface IFileManagerService
{ 
    public bool IsPathSave(string relativePath);
    
    public bool CreateDirectory(string directoryName, string CurrentDirectoryPath);
}
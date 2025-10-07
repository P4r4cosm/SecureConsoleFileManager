using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Services.Interfaces;
using Serilog;

namespace SecureConsoleFileManager.Services;

public class FileManagerService(IOptions<FileSystemOptions> fileSystemOptions) : IFileManagerService
{
    private readonly FileSystemOptions _fileSystemOptions = fileSystemOptions.Value;

    public bool IsPathSave(string relativePath)
    {
        // get full path
        var rootPath = _fileSystemOptions.FullStartPath;

        // combine paths
        string combinedPath = Path.Combine(rootPath, relativePath);


        string fullPath = Path.GetFullPath(combinedPath);
        // check fullPath contain rootPath
        if (fullPath.StartsWith(rootPath))
            return true;

        return false;
    }

    // Create a Directory in currentDirectoryName
    public bool CreateDirectory(string directoryName, string currentDirectoryPath)
    {
        // check path
        var path = Path.Combine(currentDirectoryPath, directoryName);
        if (!IsPathSave(path))
        {
            Log.Error("The user attempted to exploit the Path Traversal vulnerability when creating a directory.");
            return false;
        }
        var userPath=_fileSystemOptions.GetUserPath(path);
        Log.Information("Directory {directoryName} created in {userPath}", directoryName, userPath);
        Directory.CreateDirectory(path);
        return true;
    }
}
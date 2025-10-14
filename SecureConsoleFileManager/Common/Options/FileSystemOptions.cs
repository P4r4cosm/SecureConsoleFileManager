namespace SecureConsoleFileManager.Common.Options;

public class FileSystemOptions
{
    // appsetting.json config name
    public const string FileSystemConfig = "FileSystemConfig"; // Константа для ключа секции
    public string StartPath { get; set; } = "default_path";


    // RootPath = user Directory (/home/<username>)
    public string RootPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);


    // path without /home/<username>
    public string GetUserPath(string currentDirectoryPath) => currentDirectoryPath.Remove(0, RootPath.Length);

    public string FullStartPath =>
        Path.Combine(RootPath, StartPath);
    
    
}
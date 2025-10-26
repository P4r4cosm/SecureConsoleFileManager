using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Common;

public class ApplicationState
{
    /// <summary>
    /// Относительный путь к текущей рабочей директории пользователя.
    /// Например: "", "Documents", "Documents/Projects"
    /// </summary>
    public string CurrentRelativePath { get; set; } = ""; // Начинаем с корня


    /// <summary>
    /// Хранит объект авторизованного через login пользователя 
    /// </summary>
    public User? CurrentUser { get; set; } = null;  
}
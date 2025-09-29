using System.ComponentModel.DataAnnotations;

namespace SecureConsoleFileManager.Models;

public class User
{
    public Guid Id { get; set; }
    
    [MaxLength (30)]
    public required string Login  { get; set; }
    
    // TODO : Добавить хэширование
    public required string Password { get; set; }
}
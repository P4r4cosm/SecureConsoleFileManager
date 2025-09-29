using System.ComponentModel.DataAnnotations;

namespace SecureConsoleFileManager.Models;

public class File
{
    public Guid Id { get; set; }
    
    [MaxLength (150)]
    public required string Name { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public uint Size { get; set; }
    
    [MaxLength (1500)]
    public required string Path { get; set; }

    public Guid UserId { get; set; }
}
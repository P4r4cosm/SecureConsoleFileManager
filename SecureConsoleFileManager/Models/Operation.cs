namespace SecureConsoleFileManager.Models;

public class Operation
{
    public Guid Id { get; set; }
    
    public DateTime Created { get; set; }
    
    public OperationType Type { get; set; }
    
    public Guid UserId { get; set; }
    
    public Guid FileId { get; set; }

    public Operation() { }

    public Operation(OperationType type, Guid userId, Guid fileId)
    {
        Created = DateTime.UtcNow;
        UserId = userId;
        FileId = fileId;
        Type = type;
    }
}
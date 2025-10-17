using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Services.Interfaces;

public interface ILockerService
{
    public MbResult ExecuteLocked(string fullPath, Func<MbResult> operation);
    
    Task<MbResult> ExecuteLockedAsync(string fullPath, Func<Task<MbResult>> asyncOperation);
    MbResult ExecuteLocked(string fullPath1, string fullPath2, Func<MbResult> operation);
}
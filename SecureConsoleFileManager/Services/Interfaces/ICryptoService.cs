using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Services.Interfaces;

public interface ICryptoService
{
    /// <summary>
    /// Hashes the password using Argon2
    /// </summary>
    /// <param name="password"></param>
    /// <returns>hashed password</returns>
    public string HashPassword(string password);

    /// <summary>
    /// Check is password is correct
    /// </summary>
    /// <param name="user"></param>
    /// <param name="password"></param>
    /// <returns>bool</returns>
    public bool VerifyUserByPassword(User user, string password);
}
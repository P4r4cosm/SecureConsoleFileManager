using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;

namespace SecureConsoleFileManager.Services;

public class CryptoService(ILogger<CryptoService> logger): ICryptoService
{
    /// <summary>
    /// Hashes the password using Argon2
    /// </summary>
    /// <param name="password"></param>
    /// <returns>hashed password</returns>
    public string HashPassword(string password)
    {
        var hash = Argon2.Hash(password);
        return hash;
    }


    /// <summary>
    /// Check is password is correct
    /// </summary>
    /// <param name="user"></param>
    /// <param name="password"></param>
    /// <returns>bool</returns>
    public bool VerifyUserByPassword(User user, string password)
    {
        return Argon2.Verify(user.Password, password);
    }
}
namespace SecureConsoleFileManager.Common.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string login) : base($"login {login} not found")
    {
    }
}
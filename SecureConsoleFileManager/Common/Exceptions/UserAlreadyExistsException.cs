namespace SecureConsoleFileManager.Common.Exceptions;

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string login) : base($"login {login} is already registered")
    {
    }
}
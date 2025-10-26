namespace SecureConsoleFileManager.Models;

public class MbResult<T>
{
    public T Result { get; }

    public string Error { get; } = string.Empty;
    
    public bool IsSuccess => Error == string.Empty || Error == null;
    
    // Конструкторы для удобства (рекомендую добавить)
    private MbResult(T result)
    {
        Result = result;
        Error = string.Empty;
    }

    private MbResult(string error)
    {
        Result = default(T); // Важно инициализировать значением по умолчанию
        Error = error;
    }

    // Статические фабричные методы - это лучший способ создания экземпляров
    public static MbResult<T> Success(T result) => new MbResult<T>(result);
    public static MbResult<T> Failure(string error) => new MbResult<T>(error);
}
public class MbResult
{
    public string? Error { get; }
    public bool IsSuccess => Error == null;

    protected MbResult(string? error = null)
    {
        Error = error;
    }

    public static MbResult Success() => new();
    public static MbResult Failure(string error) => new(error);
}
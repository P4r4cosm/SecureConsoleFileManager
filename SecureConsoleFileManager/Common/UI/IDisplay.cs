namespace SecureConsoleFileManager.Common.UI;

public interface IDisplay
{
    public void PrintMessageResult(string message);
    public string ReadInput();

    public void Clear();
}

public class ConsoleDisplay : IDisplay
{
   

    public void PrintMessageResult(string message)
    {
        Console.WriteLine(message);
    }

    public string? ReadInput()
    {
        return Console.ReadLine();
    }

    public void Clear()
    {
        Console.Clear();
    }
} 
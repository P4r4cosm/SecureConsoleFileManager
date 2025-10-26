using SecureConsoleFileManager.Feature.Disks;

namespace SecureConsoleFileManager.Common.UI;

public interface IDisplay
{
    public void PrintMessage(string message);
    public string ReadInput();
    public void DisplayDiskInfos(List<DriveInfo> driveInfos);
    public string ReadSecretMessage();

    public void Clear();
}

public class ConsoleDisplay : IDisplay
{


    public void PrintMessage(string message)
    {
        Console.WriteLine(message);
    }

    public string? ReadInput()
    {
        return Console.ReadLine();
    }
    public string ReadSecretMessage()
    {
        string secret = "";
        ConsoleKeyInfo keyInfo;

        do
        {
            // Console.ReadKey(true) считывает клавишу, но не отображает её.
            keyInfo = Console.ReadKey(true);

            // Обрабатываем только "обычные" символы, игнорируем служебные клавиши (Shift, Ctrl и т.д.)
            if (!char.IsControl(keyInfo.KeyChar))
            {
                secret += keyInfo.KeyChar;
            }
            // Реализуем удаление символа по нажатию Backspace
            else if (keyInfo.Key == ConsoleKey.Backspace && secret.Length > 0)
            {
                secret = secret.Substring(0, secret.Length - 1);
            }
        }
        // Цикл продолжается, пока не будет нажат Enter
        while (keyInfo.Key != ConsoleKey.Enter);

        return secret;
    }
    public void DisplayDiskInfos(List<DriveInfo> driveInfos)
    {
        foreach (var drive in driveInfos)
        {
            Console.WriteLine("Диск {0}", drive.Name);
            Console.WriteLine("  Тип диска: {0}", drive.DriveType);
            if (drive.IsReady == true)
            {
                try
                {
                    Console.WriteLine("  Метка тома: {0}", drive.VolumeLabel);
                    Console.WriteLine("  Файловая система: {0}", drive.DriveFormat);
                    Console.WriteLine("  Доступно места для текущего пользователя:{0, 15} байт", drive.AvailableFreeSpace);
                    Console.WriteLine("  Всего доступно места на диске:           {0, 15} байт", drive.TotalFreeSpace);
                    Console.WriteLine("  Общий размер диска:                     {0, 15} байт", drive.TotalSize);
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Сообщаем, что доступ к этому конкретному диску запрещен
                    Console.WriteLine("  Ошибка: Доступ к информации о диске запрещен. {0}", ex.Message);
                }
                catch (IOException ex)
                {
                    // Сообщаем о других ошибках ввода-вывода
                    Console.WriteLine("  Ошибка: Не удалось прочитать информацию о диске. {0}", ex.Message);
                }
            }
            else
            {
                // Сообщаем, что диск не готов (например, нет диска в CD-ROM)
                Console.WriteLine("  Диск не готов к использованию.");
            }
            Console.WriteLine("-----------------------------------");
        }
    }

    public void Clear()
    {
        Console.Clear();
    }
}
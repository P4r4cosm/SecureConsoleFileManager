using System.CommandLine;

using SecureConsoleFileManager.Common.UI;
using SecureConsoleFileManager.Services;
namespace SecureConsoleFileManager.Common;

public static class CommandBuilder
{
    public static ParseResult Build(IFileManagerService fileManagerService, ApplicationState applicationState,
        IDisplay display, string commandLine)
    {
        // ======= LS =======
        var lsCommand = new Command("ls", "Показывает список файлов и директорий");
        lsCommand.SetAction((_) =>
        {
            var result = fileManagerService.GetDirectoryInfo(applicationState.CurrentRelativePath);
            if (!result.IsSuccess)
            {
                display.PrintMessageResult($"Ошибка: {result.Error}");
                return;
            }

            foreach (var dir in result.Result.GetDirectories())
            {
                display.PrintMessageResult($"[DIR]  {dir.Name}");
            }

            foreach (var file in result.Result.GetFiles())
            {
                display.PrintMessageResult($"\t{file.Name,-30} {file.Length,10} bytes");
            }
        });

        // ======= CD =======
        var pathArgument = new Argument<string>("path")
        {
            Description = "Путь для перехода. '..' для перехода вверх."
        };
        var cdCommand = new Command("cd", "Меняет текущую директорию.");
        cdCommand.Arguments.Add(pathArgument);

        cdCommand.SetAction((pathResult) =>
        {
            var path = pathResult.GetValue(pathArgument);
            string newPath;
            if (path == "..")
            {
                var parent = Path.GetDirectoryName(applicationState.CurrentRelativePath);
                newPath = string.IsNullOrEmpty(parent) ? string.Empty : parent;
            }
            else
            {
                newPath = Path.Combine(applicationState.CurrentRelativePath, path);
            }

            var result = fileManagerService.GetDirectoryInfo(newPath);
            if (result.IsSuccess)
            {
                applicationState.CurrentRelativePath = newPath;
            }
            else
            {
                display.PrintMessageResult($"Ошибка: {result.Error}");
            }
        });

        // ======= clear =======
        var clearCommand = new Command("clear", "Очищает дисплей");
        clearCommand.SetAction((_) => { display.Clear(); });


        var rootCommand = new RootCommand("Secure File Manager")
        {
            lsCommand,
            cdCommand,
            clearCommand
        };
        return rootCommand.Parse(commandLine);
    }
}
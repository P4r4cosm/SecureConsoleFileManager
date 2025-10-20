using System.CommandLine;
using SecureConsoleFileManager.Common.UI;
using SecureConsoleFileManager.Services;

namespace SecureConsoleFileManager.Common;

public class Application(IFileManagerService fileManagerService, ApplicationState applicationState, IDisplay display)
{
    public async Task RunAsync()
    {
        display.PrintMessageResult("Secure File Manager. Введите 'exit' для выхода.");
        while (true)
        {
            display.PrintMessageResult($"{applicationState.CurrentRelativePath}> ");
            var commandLine = display.ReadInput();
            if (string.IsNullOrWhiteSpace(commandLine))
                continue;

            if (commandLine.ToLower() == "exit")
            {
                break;
            }

            var parser = CommandBuilder.Build(fileManagerService, applicationState, display, commandLine);
            
            
            await parser.InvokeAsync();
        }
    }
}
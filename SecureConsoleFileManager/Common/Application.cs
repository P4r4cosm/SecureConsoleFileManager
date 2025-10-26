using System.CommandLine;
using MediatR;
using SecureConsoleFileManager.Common.UI;
using SecureConsoleFileManager.Services;

namespace SecureConsoleFileManager.Common;

public class Application(IFileManagerService fileManagerService, ApplicationState applicationState, IDisplay display, IMediator mediator)
{
    public async Task RunAsync()
    {
        display.PrintMessage("Secure File Manager. Введите 'exit' для выхода.");
        while (true)
        {
            display.PrintMessage($"{applicationState.CurrentRelativePath}> ");
            var commandLine = display.ReadInput();
            if (string.IsNullOrWhiteSpace(commandLine))
                continue;

            if (commandLine.ToLower() == "exit")
            {
                break;
            }

            var parser = CommandBuilder.Build(fileManagerService, mediator, applicationState, display, commandLine);           
            await parser.InvokeAsync();
        }
    }
}
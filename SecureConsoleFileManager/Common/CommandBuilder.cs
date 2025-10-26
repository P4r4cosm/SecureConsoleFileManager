using System.CommandLine;
using MediatR;
using SecureConsoleFileManager.Common.UI;
using SecureConsoleFileManager.Feature.Disks.GetDisksInfo;
using SecureConsoleFileManager.Feature.Files.CreateFile;
using SecureConsoleFileManager.Feature.Users.CreateUser;
using SecureConsoleFileManager.Feature.Users.LoginUser;
using SecureConsoleFileManager.Services;

namespace SecureConsoleFileManager.Common;

public static class CommandBuilder
{
    public static ParseResult Build(IFileManagerService fileManagerService, IMediator mediator,
        ApplicationState applicationState,
        IDisplay display, string commandLine)
    {
        // ======= LS =======
        var lsCommand = new Command("ls", "Показывает список файлов и директорий");
        lsCommand.SetAction((parseResult) =>
        {
            var result = fileManagerService.GetDirectoryInfo(applicationState.CurrentRelativePath);
            if (!result.IsSuccess)
            {
                display.PrintMessage($"Ошибка: {result.Error}");
                return;
            }

            foreach (var dir in result.Result.GetDirectories())
            {
                display.PrintMessage($"[DIR]  {dir.Name}");
            }

            foreach (var file in result.Result.GetFiles())
            {
                display.PrintMessage($"\t{file.Name,-30} {file.Length,10} bytes");
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
                display.PrintMessage($"Ошибка: {result.Error}");
            }
        });

        // ======= clear =======
        var clearCommand = new Command("clear", "Очищает дисплей");
        clearCommand.SetAction((_) => { display.Clear(); });


        // ======= login =======
        var loginCommand = new Command("login", "Авторизует в системе");
        loginCommand.SetAction(async (pathResult) =>
        {
            display.PrintMessage("Введите login:");
            var login = display.ReadInput();

            display.PrintMessage("Введите password:");
            var password = display.ReadSecretMessage();

            // создаём команду
            var loginUserCommand = new LoginUserCommand(login, password);
            var result = await mediator.Send(loginUserCommand);
            // если результат логина успешный, то сохраняем пользователя в состоянии приложения
            if (result.IsSuccess)
                applicationState.CurrentUser = result.Result;
            else
                display.PrintMessage(result.Error);
        });
        // ======== register ========
        var createUserCommand = new Command("register", "Регистрирует пользователя в базе данных");
        createUserCommand.SetAction(async (parseResult) =>
        {
            display.PrintMessage("Введите login:");
            var login = display.ReadInput();

            display.PrintMessage("Введите password:");
            var password = display.ReadSecretMessage();
            display.PrintMessage("Введите password снова: ");
            var password2 = display.ReadSecretMessage();
            if (password != password2)
            {
                display.PrintMessage("Пароли не совпадают");
                return;
            }


            // создаём команду
            var userCommand = new CreateUserCommand(login, password);
            var result = await mediator.Send(userCommand);
            // если результат логина успешный, то сохраняем пользователя в состоянии приложения
            // TODO: запись о логине нужно сделать в LoginUserCommand
            display.PrintMessage(result.IsSuccess ? $"User {login} has been created." : result.Error!);
        });

        // ======= pwd =======
        var pwdCommand = new Command("pwd", "Выводит путь к текущей директории");
        pwdCommand.SetAction((pathResult) =>
        {
            if (applicationState.CurrentRelativePath == string.Empty || applicationState.CurrentRelativePath == "")
                display.PrintMessage("/");
            else
                display.PrintMessage(applicationState.CurrentRelativePath);
        });

        // ======= disk =======
        var diskCommand = new Command("disk", "Выводит информацию о всех дисках в системе");
        diskCommand.SetAction(async (pathResult) =>
        {
            var getDiskInfoCommand = new GetDisksInfoCommand();
            var result = await mediator.Send(getDiskInfoCommand);
            display.DisplayDiskInfos(result);
        });

        // ======= touch =======
        var touchCommand = new Command("touch", "Создаём файл с указанным названием");
        var touchArgument = new Argument<string>("name")
        {
            Description = "Название файла или путь для создания файла",
        };
        touchCommand.Arguments.Add(touchArgument);
        touchCommand.SetAction(async (pathResult) =>
        {
            var argument = pathResult.GetValue(touchArgument);
            var createFileCommand = new CreateFileCommand(argument);
            var result = await mediator.Send(createFileCommand);
            display.PrintMessage(result.IsSuccess
                ? $"{argument} файл успешно создаан"
                : $"Ошибка при создании файла: {result.Error}");
        });

        var rootCommand = new RootCommand("Secure File Manager")
        {
            lsCommand,
            cdCommand,
            clearCommand,
            loginCommand,
            createUserCommand,
            pwdCommand,
            diskCommand,
            touchCommand
        };
        return rootCommand.Parse(commandLine);
    }
}
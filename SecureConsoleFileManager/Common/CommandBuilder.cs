using System.CommandLine;
using MediatR;
using SecureConsoleFileManager.Common.UI;
using SecureConsoleFileManager.Feature.Archives.CreateArchive;
using SecureConsoleFileManager.Feature.Archives.UnzipArchive;
using SecureConsoleFileManager.Feature.Directories.CreateDirectory;
using SecureConsoleFileManager.Feature.Directories.DeleteDirectory;
using SecureConsoleFileManager.Feature.Disks.GetDisksInfo;
using SecureConsoleFileManager.Feature.Files.CreateFile;
using SecureConsoleFileManager.Feature.Files.DeleteFile;
using SecureConsoleFileManager.Feature.Files.ReadFile;
using SecureConsoleFileManager.Feature.Files.WriteInFile;
using SecureConsoleFileManager.Feature.Move;
using SecureConsoleFileManager.Feature.Users.CreateUser;
using SecureConsoleFileManager.Feature.Users.LoginUser;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using Command = System.CommandLine.Command;

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
                ? $"{argument} файл успешно создан"
                : $"Ошибка при создании файла: {result.Error}");
        });

        // ========== rm ===========
        var rmCommand = new Command("rm", "Удаляет файл");
        var rmArgument = new Argument<string>("name")
        {
            Description = "Название файла или путь для создания файла",
        };
        rmCommand.Arguments.Add(rmArgument);
        rmCommand.SetAction(async (pathResult) =>
        {
            var argument = pathResult.GetValue(rmArgument);
            var deleteFileCommand = new DeleteFileCommand(argument);
            var result = await mediator.Send(deleteFileCommand);
            display.PrintMessage(result.IsSuccess
                ? $"{argument} файл успешно удалён"
                : $"Ошибка при удалении файла: {result.Error}");
        });


        // ========== wr ==========
        var wrCommand = new Command("wr", "Дозаписывает информацию в файл");
        var wrFileArgument = new Argument<string>("name")
        {
            Description = "Название файла или путь для создания файла"
        };
        var wrInfoArgument = new Argument<string>("info")
        {
            Description = "Информация для записи в файл"
        };
        wrCommand.Arguments.Add(wrFileArgument);
        wrCommand.Arguments.Add(wrInfoArgument);
        wrCommand.SetAction(async (ParseResult) =>
        {
            var fileArgument = ParseResult.GetValue(wrFileArgument);
            var infoArgument = ParseResult.GetValue(wrInfoArgument);
            var writeInFileCommand = new WriteInFileCommand(fileArgument, infoArgument);
            var result = await mediator.Send(writeInFileCommand);
            display.PrintMessage(result.IsSuccess
                ? $"{fileArgument} файл успешно дозаписан"
                : $"Ошибка при записи в файл: {result.Error}");
        });

        // ========== cat ============
        var catCommand = new Command("cat", "Выводит содержимое файла на экран");
        var catArgument = new Argument<string>("name")
        {
            Description = "Название файла или путь для создания файла"
        };
        catCommand.Arguments.Add(catArgument);
        catCommand.SetAction(async (ParseResult) =>
        {
            var fileArgument = ParseResult.GetValue(catArgument);
            var readFileCommand = new ReadFileCommand(fileArgument);
            var result = await mediator.Send(readFileCommand);
            display.PrintMessage(result.IsSuccess
                ? result.Result
                : $"Ошибка при чтении файла: {result.Error}");
        });

        // ======= mkdir =======
        var mkdirCommand = new Command("mkdir", "Создаёт новую директорию");
        var mkdirArgument = new Argument<string>("directoryName")
        {
            Description = "Название создаваемой директории",
        };
        mkdirCommand.Arguments.Add(mkdirArgument);
        mkdirCommand.SetAction(async (pathResult) =>
        {
            var argument = pathResult.GetValue(mkdirArgument);
            var createDirectoryCommand = new CreateDirectoryCommand(argument);
            var result = await mediator.Send(createDirectoryCommand);
            display.PrintMessage(result.IsSuccess
                ? $"Директория '{argument}' успешно создана"
                : $"Ошибка при создании директории: {result.Error}");
        });


        // ========== rmdir ===========
        var rmdirCommand = new Command("rmdir", "Удаляет директорию");
        var rmdirArgument = new Argument<string>("directoryName")
        {
            Description = "Название удаляемой директории",
        };
        var recursiveOption = new Option<bool>("recursive",
            aliases: new[] { "-r", "--recursive" });
        rmdirCommand.Arguments.Add(rmdirArgument);
        rmdirCommand.Options.Add(recursiveOption);
        rmdirCommand.SetAction(async (pathResult) =>
        {
            var argument = pathResult.GetValue(rmdirArgument);
            var isRecursive = pathResult.GetValue(recursiveOption);
            var deleteDirectoryCommand = new DeleteDirectoryCommand(argument, isRecursive);
            var result = await mediator.Send(deleteDirectoryCommand);
            display.PrintMessage(result.IsSuccess
                ? $"Директория '{argument}' успешно удалена"
                : $"Ошибка при удалении директории: {result.Error}");
        });
        
        // ========== mv ===========
        var mvCommand = new Command("mv", "Перемещает файл или директорию");
        var mvSourceArgument = new Argument<string>("source")
        {
            Description = "Исходный путь к файлу или директории"
        };
        var mvDestinationArgument = new Argument<string>("destination")
        {
            Description = "Целевой путь"
        };
        mvCommand.Arguments.Add(mvSourceArgument);
        mvCommand.Arguments.Add(mvDestinationArgument);
        mvCommand.SetAction(async (parseResult) =>
        {
            var sourceArgument = parseResult.GetValue(mvSourceArgument);
            var destinationArgument = parseResult.GetValue(mvDestinationArgument);
            var moveCommand = new MoveCommand(sourceArgument, destinationArgument);
            var result = await mediator.Send(moveCommand);
            display.PrintMessage(result.IsSuccess
                ? $"'{sourceArgument}' успешно перемещен в '{destinationArgument}'"
                : $"Ошибка при перемещении: {result.Error}");
        });
        
        // ========== zip ===========
        var zipCommand = new Command("zip", "Создаёт ZIP-архив из указанных файлов и директорий.");
        var zipArchiveArgument = new Argument<string>("archive-name")
        {
            Description = "Имя создаваемого архива (например, 'my-archive.zip')."
        };
        var zipSourcesArgument = new Argument<string[]>("sources")
        {
            Description = "Один или несколько файлов/директорий для добавления в архив.",
            Arity = ArgumentArity.OneOrMore // Указываем, что источников должен быть хотя бы один
        };
        zipCommand.Arguments.Add(zipArchiveArgument);
        zipCommand.Arguments.Add(zipSourcesArgument);

        zipCommand.SetAction(async (parseResult) =>
        {
            var archiveName = parseResult.GetValue(zipArchiveArgument);
            var sources = parseResult.GetValue(zipSourcesArgument);

            var createArchiveCommand = new CreateArchiveCommand(archiveName, sources);
            var result = await mediator.Send(createArchiveCommand);

            display.PrintMessage(result.IsSuccess
                ? $"Архив '{archiveName}' успешно создан."
                : $"Ошибка при создании архива: {result.Error}");
        });
        
        
        // ========== unzip ===========
        var unzipCommand = new Command("unzip", "Распаковывает ZIP-архив в указанную директорию.");
        var unzipArchiveArgument = new Argument<string>("archive-path")
        {
            Description = "Путь к архиву, который нужно распаковать."
        };
        var unzipDestinationArgument = new Argument<string>("destination-directory")
        {
            Description = "Папка, в которую будут извлечены файлы."
        };
        unzipCommand.Arguments.Add(unzipArchiveArgument);
        unzipCommand.Arguments.Add(unzipDestinationArgument);

        unzipCommand.SetAction(async (parseResult) =>
        {
            var archivePath = parseResult.GetValue(unzipArchiveArgument);
            var destination = parseResult.GetValue(unzipDestinationArgument);

            var extractArchiveCommand = new ExtractArchiveCommand(archivePath, destination);
            var result = await mediator.Send(extractArchiveCommand);

            display.PrintMessage(result.IsSuccess
                ? $"Архив '{archivePath}' успешно распакован в '{destination}'."
                : $"Ошибка при распаковке архива: {result.Error}");
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
            touchCommand,
            rmCommand,
            wrCommand,
            catCommand,
            mkdirCommand,
            rmdirCommand,
            mvCommand,
            zipCommand,
            unzipCommand
            
        };
        return rootCommand.Parse(commandLine);
    }
}
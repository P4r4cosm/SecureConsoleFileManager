using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Common;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Common.UI;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Infrastructure.Repositories;
using SecureConsoleFileManager.Services;
using SecureConsoleFileManager.Services.Interfaces;
using Serilog;

var builder = new ConfigurationBuilder();

BuildConfiguration(builder);

IConfiguration config = builder.Build();

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();


try
{
    Log.Information("Application starting up ...");

    Log.Information("Initialize file manager directory... ");

    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddOptions();

            services.Configure<FileSystemOptions>(config.GetSection(FileSystemOptions.FileSystemConfig));

            var connection = context.Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connection)
                    .UseLoggerFactory(LoggerFactory.Create(b => b.AddSerilog()));
            });
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            // Services
            services.AddSingleton<ApplicationState>();
            services.AddSingleton<IDisplay, ConsoleDisplay>();
            services.AddSingleton<ILockerService, LockerService>();
            services.AddSingleton<IArchiveService, ArchiveService>();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<IFileManagerService, FileManagerService>();

            // Application (основной цикл принимающий команды)
            services.AddSingleton<Application>();

            // Mediatr
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }).UseSerilog().Build();
    
    
    var app = host.Services.GetRequiredService<Application>();
    await app.RunAsync();
}
catch (Exception e)
{
    Log.Error($"Ошибка, {e.Message}");
}


static void BuildConfiguration(IConfigurationBuilder builder)
{
    var executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    builder.SetBasePath(executableLocation)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile(
            $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true,
            reloadOnChange: true)
        .AddEnvironmentVariables();
}
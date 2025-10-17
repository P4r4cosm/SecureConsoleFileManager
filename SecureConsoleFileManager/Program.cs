


using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureConsoleFileManager.Common.Options;
using SecureConsoleFileManager.Feature.Disks.GetDisksInfo;
using SecureConsoleFileManager.Feature.Users.LoginUser;
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
            services.AddSingleton<ILockerService, LockerService>();
            services.AddSingleton<IArchiveService, ArchiveService>();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<IFileManagerService, FileManagerService>();
            
            // Mediatr
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        }).UseSerilog().Build();
    
    // Receiving service from DI
    // var cryptoService = host.Services.GetService<ICryptoService>();
    
    
    var mediatr = host.Services.GetService<IMediator>();

    // Create Account
    // var createUserCommand = new CreateUserCommand("admin", "admin");
    // var guid = await mediatr!.Send(createUserCommand);

    // Login Account 

    var loginUserCommand = new LoginUserCommand("admin", "admin");
    var result = await mediatr!.Send(loginUserCommand);

    // GetDisksInfo
    var getDiskInfocommand = new GetDisksInfoCommand();
    var result2 = await mediatr!.Send(getDiskInfocommand);
}
catch (Exception e)
{
    Log.Error($"Ошибка, {e.Message}");
}


static void BuildConfiguration(IConfigurationBuilder builder)
{
    builder.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile(
            $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true,
            reloadOnChange: true)
        .AddEnvironmentVariables();
}
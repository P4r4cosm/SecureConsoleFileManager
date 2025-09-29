// See https://aka.ms/new-console-template for more information


using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Feature.Users.CreateUser;
using SecureConsoleFileManager.Feature.Users.LoginUser;
using SecureConsoleFileManager.Infrastructure;
using SecureConsoleFileManager.Infrastructure.Interfaces;
using SecureConsoleFileManager.Infrastructure.Repositories;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services;
using SecureConsoleFileManager.Services.Interfaces;
using Serilog;

var builder = new ConfigurationBuilder();

BuildConfiguration(builder);

IConfiguration conifg = builder.Build();

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(conifg).CreateLogger();


try
{
    Log.Information("Запуск приложения ...");
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            var connection = context.Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connection)
                    .UseLoggerFactory(LoggerFactory.Create(b => b.AddSerilog()));
            });
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            // Services
            services.AddSingleton<ICryptoService, CryptoService>();
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
    Console.WriteLine(result);
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
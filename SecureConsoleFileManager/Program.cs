// See https://aka.ms/new-console-template for more information


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Infrastructure;
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
        }).UseSerilog().Build();
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
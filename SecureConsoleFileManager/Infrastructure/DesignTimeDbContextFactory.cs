
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
namespace SecureConsoleFileManager.Infrastructure
{
    public class DesignTimeDbContextFactory: IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            
            // Эта фабрика предназначена для того, чтобы инструменты EF Core
            // могли создавать миграции, не запуская основное приложение.

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Укажите здесь строку подключения.
            // Она может быть такой же, как в вашем appsettings.json,
            // но здесь она задается напрямую. Это нормально, так как используется
            // только для создания миграций.
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=account_db;User Id=user;Password=password;");
        

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
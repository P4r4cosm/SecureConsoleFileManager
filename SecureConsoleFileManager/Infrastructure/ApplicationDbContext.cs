using Microsoft.EntityFrameworkCore;
using SecureConsoleFileManager.Models;
using File = SecureConsoleFileManager.Models.File;


namespace SecureConsoleFileManager.Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Operation> Operations { get; set; }
    public DbSet<File> Files { get; set; }
}
using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Cli;

public static class UserCreator
{
    public static async Task<int> RunAsync(string[] args, IConfiguration config)
    {
        // args expected: --create-user <username> <password>
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: dotnet run -- --create-user <username> <password>");
            return 1;
        }

        var username = args[1].Trim();
        var password = args[2];

        if (string.IsNullOrWhiteSpace(username))
        {
            Console.Error.WriteLine("Error: username cannot be blank.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("Error: password cannot be blank.");
            return 1;
        }

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new AppDbContext(dbOptions);
        await db.Database.MigrateAsync();

        var exists = await db.Users.AnyAsync(u => u.Username == username);
        if (exists)
        {
            Console.Error.WriteLine($"Error: username '{username}' is already taken.");
            return 1;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        db.Users.Add(new User { Username = username, PasswordHash = passwordHash });
        await db.SaveChangesAsync();

        Console.WriteLine($"User '{username}' created successfully.");
        return 0;
    }
}

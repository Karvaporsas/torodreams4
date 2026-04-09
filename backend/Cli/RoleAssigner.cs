using Microsoft.EntityFrameworkCore;
using ToroFitDreaming4.Data;
using ToroFitDreaming4.Models;

namespace ToroFitDreaming4.Cli;

public static class RoleAssigner
{
    private static readonly HashSet<string> ValidRoles = ["Admin", "User"];

    public static async Task<int> RunAsync(string[] args, IConfiguration config)
    {
        // args expected: --assign-role <username> <role>
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: dotnet run -- --assign-role <username> <role>");
            Console.Error.WriteLine($"Valid roles: {string.Join(", ", ValidRoles)}");
            return 1;
        }

        var username = args[1].Trim();
        var role = args[2].Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            Console.Error.WriteLine("Error: username cannot be blank.");
            return 1;
        }

        if (!ValidRoles.Contains(role))
        {
            Console.Error.WriteLine($"Error: '{role}' is not a valid role. Valid roles: {string.Join(", ", ValidRoles)}");
            return 1;
        }

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new AppDbContext(dbOptions);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            Console.Error.WriteLine($"Error: user '{username}' does not exist.");
            return 1;
        }

        var existing = await db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.Role == role);

        if (existing is not null)
        {
            Console.WriteLine($"User '{username}' already has the '{role}' role.");
            return 0;
        }

        db.UserRoles.Add(new UserRole { UserId = user.Id, Role = role });
        await db.SaveChangesAsync();

        Console.WriteLine($"Role '{role}' assigned to user '{username}' successfully.");
        return 0;
    }
}

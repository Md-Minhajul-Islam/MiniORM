// ════════════════════════════════════════════════════════════════════
//  MiniOrm.Migrations - CLI entry point
//
//  Usage:
//    dotnet run -- migrations add <Name>    Generate a migration file
//    dotnet run -- migrations apply         Run all pending migrations
//    dotnet run -- migrations list          Show applied/pending status
//    dotnet run -- migrations rollback      Revert last applied migration
//
//  Connection string comes from the MINIORM_CONN environment variable.
//  Migration .sql files are stored in a Migrations/ sub-folder relative
//  to the current working directory.
// ════════════════════════════════════════════════════════════════════

using MiniOrm.Configuration;
using MiniOrm.Migrations.Commands;

EnvConfig.Load();
var connStr = EnvConfig.GetConnectionString();

var migrationsDir = Path.Combine(Directory.GetCurrentDirectory(), "Migrations");
var runner        = new MigrationRunner(connStr, migrationsDir);

if (args.Length < 2 || !string.Equals(args[0], "migrations", StringComparison.OrdinalIgnoreCase))
{
    PrintUsage();
    return 1;
}

switch (args[1].ToLowerInvariant())
{
    case "add" when args.Length >= 3:
        runner.Add(args[2]);
        break;

    case "add":
        Console.Error.WriteLine("Error: 'migrations add' requires a name, e.g. migrations add InitialCreate");
        return 1;

    case "apply":
        runner.Apply();
        break;

    case "list":
        runner.List();
        break;

    case "rollback":
        runner.Rollback();
        break;

    default:
        Console.Error.WriteLine($"Unknown command: {args[1]}");
        PrintUsage();
        return 1;
}

return 0;

static void PrintUsage()
{
    Console.WriteLine("""
        MiniOrm Migration CLI
        Usage:
          dotnet run -- migrations add <Name>    Generate a timestamped .sql migration file
          dotnet run -- migrations apply         Apply all pending migrations to the database
          dotnet run -- migrations list          List all migrations with [applied] / [pending] status
          dotnet run -- migrations rollback      Revert the last applied migration
        """);
}

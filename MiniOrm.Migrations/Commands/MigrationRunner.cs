using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;
using MiniOrm.Attributes;
using MiniOrm.Data;

namespace MiniOrm.Migrations.Commands;

/// <summary>
/// Implements all four migration commands against SQL Server.
///
///  add &lt;Name&gt;  - Diffs registered entities vs the live DB schema and
///                generates a timestamped .sql file with -- up / -- down sections.
///                Scope: CREATE TABLE (new tables), ALTER TABLE ADD COLUMN (new columns).
///
///  apply       - Runs every pending migration's -- up block in order and
///                records it in the __migrations tracking table.
///
///  list        - Prints every .sql file as [applied] or [pending].
///
///  rollback    - Reverts the last applied migration using its -- down block.
/// </summary>
public class MigrationRunner
{
    private readonly string _connStr;
    private readonly string _migrationsDir;

    public MigrationRunner(string connStr, string migrationsDir)
    {
        _connStr       = connStr;
        _migrationsDir = migrationsDir;
        Directory.CreateDirectory(migrationsDir);
    }

    // add

    public void Add(string name)
    {
        using var conn = OpenConnection();
        EnsureMigrationsTable(conn);

        var entityTypes    = GetEntityTypes();
        var existingTables = GetExistingTables(conn);
        var existingCols   = GetExistingColumns(conn);

        var up   = new StringBuilder();
        var down = new StringBuilder();

        foreach (var type in entityTypes)
        {
            var meta = TypeMapper.BuildMetadata(type);

            if (!existingTables.Contains(meta.TableName))
            {
                up.AppendLine(GenerateCreateTable(meta));
                up.AppendLine();
                down.AppendLine($"DROP TABLE IF EXISTS {meta.TableName};");
            }
            else
            {
                var existing = existingCols.GetValueOrDefault(
                    meta.TableName.ToLowerInvariant(), new HashSet<string>());

                foreach (var col in meta.Columns.Where(c => !c.IsPrimaryKey))
                {
                    if (!existing.Contains(col.ColumnName.ToLowerInvariant()))
                    {
                        string sqlType = TypeMapper.GetSqlType(col);
                        up.AppendLine(
                            $"ALTER TABLE {meta.TableName} ADD {col.ColumnName} {sqlType};");
                        down.AppendLine(
                            $"ALTER TABLE {meta.TableName} DROP COLUMN {col.ColumnName};");
                    }
                }
            }
        }

        if (up.Length == 0)
        {
            Console.WriteLine("No schema changes detected - migration file not created.");
            return;
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName  = $"{timestamp}_{name}.sql";
        var filePath  = Path.Combine(_migrationsDir, fileName);

        File.WriteAllText(filePath,
            $"-- up\n{up.ToString().TrimEnd()}\n\n-- down\n{down.ToString().TrimEnd()}\n");

        Console.WriteLine($"Created: {filePath}");
    }

    // apply

    public void Apply()
    {
        using var conn = OpenConnection();
        EnsureMigrationsTable(conn);

        var applied = GetAppliedMigrations(conn);
        var files   = GetMigrationFiles();
        int count   = 0;

        foreach (var file in files)
        {
            var migName = Path.GetFileNameWithoutExtension(file);
            if (applied.Contains(migName)) continue;

            var upSql = ExtractSection(File.ReadAllText(file), "up");
            Console.Write($"  Applying {migName}... ");

            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var stmt in SplitStatements(upSql))
                    new SqlCommand(stmt, conn, tx).ExecuteNonQuery();

                var rec = new SqlCommand(
                    "INSERT INTO __migrations (name) VALUES (@n)", conn, tx);
                rec.Parameters.AddWithValue("@n", migName);
                rec.ExecuteNonQuery();

                tx.Commit();
                count++;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Console.WriteLine($"FAILED\n  → {ex.Message}");
                Console.WriteLine("  Remaining migrations skipped.");
                break;
            }
        }

        Console.WriteLine(count == 0
            ? "No pending migrations."
            : $"{count} migration(s) applied.");
    }

    // list

    public void List()
    {
        using var conn = OpenConnection();
        EnsureMigrationsTable(conn);

        var applied = GetAppliedMigrations(conn);
        var files   = GetMigrationFiles();

        if (!files.Any())
        {
            Console.WriteLine($"No .sql files found in: {_migrationsDir}");
            return;
        }

        foreach (var file in files)
        {
            var migName = Path.GetFileNameWithoutExtension(file);
            var status  = applied.Contains(migName) ? "[applied]" : "[pending]";
            Console.WriteLine($"  {status,-10} {migName}");
        }
    }

    // rollback

    public void Rollback()
    {
        using var conn = OpenConnection();
        EnsureMigrationsTable(conn);

        var applied = GetAppliedMigrations(conn);
        if (!applied.Any())
        {
            Console.WriteLine("No applied migrations to roll back.");
            return;
        }

        var last = applied.Last();
        var file = GetMigrationFiles()
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == last);

        if (file is null)
        {
            Console.WriteLine($"Migration file not found for: {last}");
            return;
        }

        var downSql = ExtractSection(File.ReadAllText(file), "down");
        Console.Write($"  Rolling back {last}... ");

        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var stmt in SplitStatements(downSql))
                new SqlCommand(stmt, conn, tx).ExecuteNonQuery();

            var del = new SqlCommand(
                "DELETE FROM __migrations WHERE name = @n", conn, tx);
            del.Parameters.AddWithValue("@n", last);
            del.ExecuteNonQuery();

            tx.Commit();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            Console.WriteLine($"FAILED\n  → {ex.Message}");
        }
    }

    // Private helpers 

    private SqlConnection OpenConnection()
    {
        var conn = new SqlConnection(_connStr);
        conn.Open();
        return conn;
    }

    private static void EnsureMigrationsTable(SqlConnection conn)
    {
        const string sql = """
            IF NOT EXISTS (
                SELECT 1 FROM sys.tables WHERE name = '__migrations'
            )
            CREATE TABLE __migrations (
                id         INT IDENTITY(1,1) PRIMARY KEY,
                name       NVARCHAR(255) NOT NULL,
                applied_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
            """;
        new SqlCommand(sql, conn).ExecuteNonQuery();
    }

    private static IEnumerable<Type> GetEntityTypes()
        => typeof(TableAttribute).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<TableAttribute>() != null);

    private static HashSet<string> GetExistingTables(SqlConnection conn)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new SqlCommand(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'",
            conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) set.Add(r.GetString(0));
        return set;
    }

    private static Dictionary<string, HashSet<string>> GetExistingColumns(SqlConnection conn)
    {
        var dict = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new SqlCommand(
            "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var tbl = r.GetString(0).ToLowerInvariant();
            var col = r.GetString(1).ToLowerInvariant();
            if (!dict.ContainsKey(tbl)) dict[tbl] = new HashSet<string>();
            dict[tbl].Add(col);
        }
        return dict;
    }

    private static string GenerateCreateTable(EntityMetadata meta)
    {
        var colDefs = meta.Columns.Select(c =>
        {
            string sqlType = TypeMapper.GetSqlType(c);
            string pk      = c.IsPrimaryKey ? " PRIMARY KEY" : "";
            return $"    {c.ColumnName} {sqlType}{pk}";
        });

        return $"CREATE TABLE {meta.TableName} (\n{string.Join(",\n", colDefs)}\n);";
    }

    private static HashSet<string> GetAppliedMigrations(SqlConnection conn)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        using var cmd = new SqlCommand(
            "SELECT name FROM __migrations ORDER BY applied_at, id", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) set.Add(r.GetString(0));
        return set;
    }

    private List<string> GetMigrationFiles()
        => Directory.GetFiles(_migrationsDir, "*.sql")
                    .OrderBy(f => f)
                    .ToList();


    private static string ExtractSection(string content, string section)
    {
        var sb      = new StringBuilder();
        bool inside = false;

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed == $"-- {section}") { inside = true;  continue; }
            if (trimmed.StartsWith("-- ") && inside) break;

            if (inside) sb.AppendLine(line);
        }
        return sb.ToString();
    }

    private static IEnumerable<string> SplitStatements(string sql)
        => sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Where(s => !string.IsNullOrWhiteSpace(s));
}

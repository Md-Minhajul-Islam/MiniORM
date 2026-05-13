using Microsoft.Data.SqlClient;

namespace MiniOrm.Data;

public class DbSet<T> where T : class, new()
{
    private readonly DbContext _context;
    private readonly EntityMetadata _metadata;

    public DbSet(DbContext context)
    {
        _context = context;
        _metadata = TypeMapper.BuildMetadata<T>();
    }

    // INSERT
    public int Insert(T entity)
    {
        var cols = _metadata.NonPkColumns.ToList();
        var colNames = string.Join(", ", cols.Select(c => c.ColumnName));
        var paramNames = string.Join(", ", cols.Select((_, i) => $"@p{i}"));

        var sql = $@"
            INSERT INTO {_metadata.TableName} ({colNames})
            VALUES ({paramNames});
            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

        using var cmd = new SqlCommand(sql, _context.GetConnection());
        BindParameters(cmd, cols, entity);

        var result = cmd.ExecuteScalar()
            ?? throw new InvalidOperationException("INSERT returned no identity value.");

        int newId = Convert.ToInt32(result);
        _metadata.PrimaryKey.Property.SetValue(entity, newId);

        return newId;
    }


    // FindById
    public T? FindById(int id)
    {
        var sql = $"SELECT * FROM {_metadata.TableName} WHERE {_metadata.PrimaryKey.ColumnName} = @id";

        using var cmd = new SqlCommand(sql, _context.GetConnection());
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? 
    }


    private static void BindParameters(SqlCommand cmd, List<ColumnMetadata> cols, T entity)
    {
        for(int i = 0; i < cols.Count; i++)
        {
            var rawValue = cols[i].Property.GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", rawValue ?? DBNull.Value);
        }
    }

    private T MapRow(SqlDataReader reader)
    {
        var entity = new T();
        for(int i = 0; i < reader.FieldCount; i++)
        {
            
        }
    }
}
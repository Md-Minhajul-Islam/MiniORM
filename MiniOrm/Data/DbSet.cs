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
        return reader.Read() ? MapRow(reader) : null;
    }


    // GET All
    public IEnumerable<T> GetAll()
    {
        var sql = $"SELECT * FROM {_metadata.TableName}";
        using var cmd = new SqlCommand(sql, _context.GetConnection());
        using var reader = cmd.ExecuteReader();

        var list = new List<T>();
        while(reader.Read()) list.Add(MapRow(reader));
        return list;
    }


    // Update
    public void Update(T entity)
    {
        var cols = _metadata.NonPkColumns.ToList();
        var setClauses = string.Join(", ", cols.Select((c, i) => $"{c.ColumnName} = @p{i}"));
        var pk = _metadata.PrimaryKey;

        var sql = $"UPDATE {_metadata.TableName} SET {setClauses} WHERE {pk.ColumnName} = @pk";
        using var cmd = new SqlCommand(sql, _context.GetConnection());
        BindParameters(cmd, cols, entity);
        cmd.Parameters.AddWithValue("@pk", pk.Property.GetValue(entity));
        cmd.ExecuteNonQuery();
    }


    // Delete
    public void Delete(int id)
    {
        var sql = $"DELETE FROM {_metadata.TableName} WHERE {_metadata.PrimaryKey.ColumnName} = @id";
        using var cmd = new SqlCommand(sql, _context.GetConnection());
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }




    // Private Helpers
    private static void BindParameters(SqlCommand cmd, List<ColumnMetadata> cols, T entity)
    {
        for(int i = 0; i < cols.Count; i++)
        {
            var rawValue = cols[i].Property.GetValue(entity);
            cmd.Parameters.AddWithValue($"@p{i}", rawValue ?? DBNull.Value);
        }
    }


    // Converts database row into a c# object
    private T MapRow(SqlDataReader reader)
    {
        var entity = new T();
        for(int i = 0; i < reader.FieldCount; i++)
        {
            var colName = reader.GetName(i);

            // Find matching metadata column
            var col = _metadata.Columns.FirstOrDefault(c =>
            c.ColumnName.Equals(colName, StringComparison.OrdinalIgnoreCase));

            if(col is null) continue;

            if(reader.IsDBNull(i))
            {
                col.Property.SetValue(entity, null);
                continue;
            }

            object raw = reader.GetValue(i);

            // Resolve nullable types
            // Convert.ChangeType() cannot convert directly to Nullable<int>
            Type targetType = Nullable.GetUnderlyingType(col.Property.PropertyType)
                                ?? col.Property.PropertyType;

            // Convert.ChangeType() does NOT properly handle Guid
            object converted = targetType == typeof(Guid)
                ? reader.GetGuid(i)
                : Convert.ChangeType(raw, targetType);

            col.Property.SetValue(entity, converted);
        }
        return entity;
    }
}
using System.Reflection;
using MiniOrm.Attributes;

namespace MiniOrm.Data;

public static class TypeMapper
{
    public static EntityMetadata BuildMetadata<T>()
    {
        return BuildMetadata(typeof(T));
    }

    public static EntityMetadata BuildMetadata(Type type)
    {
        var tableAttr = type.GetCustomAttribute<TableAttribute>();

        var columns = new List<ColumnMetadata>();

        foreach(var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var pkAttr = prop.GetCustomAttribute<PrimaryKeyAttribute>();

            // Skip properties without [Column] or [PrimaryKey] 
            if(colAttr == null && pkAttr == null) continue;


            columns.Add(new ColumnMetadata
            {
                ColumnName = colAttr?.Name ?? prop.Name.ToLowerInvariant(),
                Property = prop,
                IsPrimaryKey = pkAttr != null,
                IsNullable = ResolveNullability(prop)
            });
        }

        return new EntityMetadata {TableName = tableAttr!.Name, Columns = columns};
    }

    public static string GetSqlType(ColumnMetadata col)
    {
        var type = Nullable.GetUnderlyingType(col.Property.PropertyType)
                   ?? col.Property.PropertyType;

        var sqlType = type switch
        {
            _ when type == typeof(int) && col.IsPrimaryKey => "INT IDENTITY(1,1)",
            _ when type == typeof(int)     => "INT",
            _ when type == typeof(long)    => "BIGINT",
            _ when type == typeof(float)   => "REAL",
            _ when type == typeof(double)  => "FLOAT",
            _ when type == typeof(decimal) => "DECIMAL(18,4)",
            _ when type == typeof(bool)    => "BIT",
            _ when type == typeof(DateTime) => "DATETIME2",
            _ when type == typeof(Guid)    => "UNIQUEIDENTIFIER",
            _ when type == typeof(string)  => "NVARCHAR(MAX)",
            _ => throw new NotSupportedException($"Type '{type.Name}' is not supported for SQL mapping.")
        };

        if (col.IsPrimaryKey)
            return sqlType;

        return col.IsNullable ? $"{sqlType} NULL" : $"{sqlType} NOT NULL";
    }

    private static bool ResolveNullability(PropertyInfo prop)
    {
        var type = prop.PropertyType;

        // Nullable.GetUnderlyingType(type)
        // - returns underlying Type if it's nullable
        return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
    }

}


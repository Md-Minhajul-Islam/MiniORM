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

        return new EntityMetadata {TableName = tableAttr.Name, Columns = columns};
    }



    private static bool ResolveNullability(PropertyInfo prop)
    {
        var type = prop.PropertyType;

        // Nullable.GetUnderlyingType(type)
        // - returns underlying Type if it's nullable
        return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
    }

}


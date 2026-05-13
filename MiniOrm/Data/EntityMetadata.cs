namespace MiniOrm.Data;
using System.Reflection;

public class ColumnMetadata
{
    public required string ColumnName {get; init;}
    public required PropertyInfo Property {get; init;}
    public bool IsPrimaryKey {get; init;}
    public bool IsNullable {get; init;}

}


public class EntityMetadata
{
    public required string TableName {get; init;}
    public required List<ColumnMetadata> Columns {get; init;}
    public ColumnMetadata PrimaryKey 
        => Columns.First(c => c.IsPrimaryKey);
    public IEnumerable<ColumnMetadata> NonPkColumns
        => Columns.Where(c => !c.IsPrimaryKey);
}

namespace MiniOrm.Data;

public class AppDbContext : DbContext
{
    
    public AppDbContext(string connStr) : base(connStr)
    {
        
    }
}
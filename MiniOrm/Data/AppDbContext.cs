using MiniOrm.Models;

namespace MiniOrm.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products {get; set;} = null!;
    public DbSet<Order> Orders {get; set;} = null!;
    
    public AppDbContext(string connStr) : base(connStr)
    {
        Products = new DbSet<Product>(this);
        Orders   = new DbSet<Order>(this);
    }
}
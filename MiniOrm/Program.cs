
using MiniOrm.Data;
namespace  MiniOrm;

public class Program
{
    static void Main(string[] args)
    {
        string? connStr = Environment.GetEnvironmentVariable("MINIORM_CONN");

        AppDbContext appDbContext = new AppDbContext(connStr);
    
        Console.WriteLine(appDbContext.GetConnection().ClientConnectionId);
    
    }
}



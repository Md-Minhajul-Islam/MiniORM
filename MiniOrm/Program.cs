// ════════════════════════════════════════════════════════════════════
//  MiniOrm
//  Demonstrates every CRUD operation against a live SQL Server database.
//
//  Pre-requisite: run migrations first
//    cd MiniOrm.Migrations
//    dotnet run -- migrations add InitialCreate
//    dotnet run -- migrations apply
//
//  Set the connection string in your shell before running:
//    $Env:MINIORM_CONN = "Server=localhost;Database=miniorm_db;Trusted_Connection=True;TrustServerCertificate=True"
// ════════════════════════════════════════════════════════════════════

using MiniOrm.Configuration;
using MiniOrm.Data;
using MiniOrm.Models;

Console.WriteLine("----------------------------------------");
Console.WriteLine("                 MiniOrm                ");
Console.WriteLine("----------------------------------------");

EnvConfig.Load();
var connStr = EnvConfig.GetConnectionString();

AppDbContext db = new AppDbContext(connStr);

Console.WriteLine($"Connection opened: {db.GetConnection().ClientConnectionId}");


// Insert Products
Console.WriteLine("Inserting products...");

var keyboard = new Product { Name = "Keyboard", Price = 89.99m, Discount = null, InStock = true };
int kbId = db.Products.Insert(keyboard);
Console.WriteLine($"Inserted  Id={kbId}, Name={keyboard.Name}\n");

var mouse = new Product { Name = "Mouse", Price = 29.99m, Discount = 5.00m, InStock = true };
db.Products.Insert(mouse);
Console.WriteLine($"Inserted  Id={mouse.Id}, Name={mouse.Name}\n");

// Query, update, delete
Console.WriteLine("FindById...");
var found = db.Products.FindById(kbId);
Console.WriteLine($"Found -> Name={found?.Name}, Price={found?.Price}, " + $"Discount={(found?.Discount?.ToString() ?? "NULL")}");

Console.WriteLine("\nUpdating price and adding discount...");
found!.Price    = 79.99m;
found.Discount  = 5.00m;
db.Products.Update(found);
Console.WriteLine($"Updated -> Price={found.Price}, Discount={found.Discount}");

Console.WriteLine("\nGetAll...");
var all = db.Products.GetAll().ToList();
Console.WriteLine($"{all.Count} product(s) in table:");
foreach (var p in all)
{
    Console.WriteLine($"Id={p.Id}  Name={p.Name} " + $"Price={p.Price}  Discount={(p.Discount?.ToString() ?? "NULL")}  InStock={p.InStock}");
}

Console.WriteLine("\nDelete ...");
db.Products.Delete(kbId);
int remaining = db.Products.GetAll().Count();
Console.WriteLine($"Deleted Id={kbId} - {remaining} product(s) remaining.");

Console.WriteLine("\n-------------------------------------------------\n");

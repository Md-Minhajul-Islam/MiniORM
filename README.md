# MiniOrm — Scratch ORM with ADO.NET (SQL Server edition)

A simplified Entity Framework–style ORM built on raw ADO.NET + **Microsoft.Data.SqlClient**,
demonstrating how production ORMs work under the hood.

---

## Project structure

```
MiniOrm.sln
├── MiniOrm/                   # Library + demo (Console Application)
│   ├── Attributes/
│   │   ├── TableAttribute.cs
│   │   ├── ColumnAttribute.cs
│   │   └── PrimaryKeyAttribute.cs
│   ├── Models/
│   │   ├── Product.cs
│   │   └── Order.cs
│   ├── Data/
│   │   ├── DbContext.cs
│   │   ├── DbSet.cs
│   │   ├── TypeMapper.cs
│   │   └── EntityMetadata.cs
│   └── Program.cs             # 5-step coding demo
└── MiniOrm.Migrations/        # Migration CLI (Console Application)
    ├── Commands/
    │   └── MigrationRunner.cs
    └── Program.cs
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| SQL Server | 2019+ (or SQL Server Express / LocalDB / Azure SQL) |

---

## 1 — Set up the database

Create a database in SQL Server Management Studio or `sqlcmd`:

```sql
CREATE DATABASE miniorm_db;
```

---

## 2 — Set the connection string (`MINIORM_CONN`)

The connection string is **never hard-coded**. It is read from `MINIORM_CONN`.

### Option A — `.env` file (recommended)

Copy the example file at the repo root and edit it for your machine:

```bash
cp .env.example .env
```

Example `.env`:

```env
MINIORM_CONN=Server=.;Database=MiniORMDB;Trusted_Connection=True;TrustServerCertificate=True
```

Both `MiniOrm` and `MiniOrm.Migrations` load `.env` automatically when you run
`dotnet run` (they search the current folder and parent directories). You do
**not** need to set `$Env:MINIORM_CONN` in the terminal each time.

`.env` is git-ignored; never commit real credentials.

### Option B — shell environment variable

### Windows (PowerShell)
```powershell
$Env:MINIORM_CONN = "Server=localhost;Database=miniorm_db;Trusted_Connection=True;TrustServerCertificate=True"
```

### Windows (Command Prompt)
```cmd
set MINIORM_CONN=Server=localhost;Database=miniorm_db;Trusted_Connection=True;TrustServerCertificate=True
```

### macOS / Linux
```bash
export MINIORM_CONN="Server=localhost;Database=miniorm_db;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

### SQL Server Express / LocalDB
```
Server=(localdb)\MSSQLLocalDB;Database=miniorm_db;Trusted_Connection=True;
```

---

## 3 — Run migrations

```bash
cd MiniOrm.Migrations

# Generate the initial migration (diffs entities vs live DB)
dotnet run -- migrations add InitialCreate

# Apply all pending migrations
dotnet run -- migrations apply

# List migrations with status
dotnet run -- migrations list

# Roll back the last migration
dotnet run -- migrations rollback
```

Migration `.sql` files are saved to a `Migrations/` folder and look like:

```sql
-- up
CREATE TABLE products (
    id       INT IDENTITY(1,1) PRIMARY KEY,
    name     NVARCHAR(MAX) NOT NULL,
    price    DECIMAL(18,4) NOT NULL,
    discount DECIMAL(18,4) NULL,
    in_stock BIT NOT NULL
);

-- down
DROP TABLE IF EXISTS products;
```

---

## 4 — Run the demo

```bash
cd MiniOrm
dotnet run
```

Expected output:

```
Step 4 ▸ Inserting products …
        ✓ Inserted  Id=1, Name=Keyboard, Discount=NULL
        ✓ Inserted  Id=2, Name=Mouse, Discount=5.0000

Step 5 ▸ FindById …
        ✓ Found → Name=Keyboard, Price=89.9900, Discount=NULL
        Updating price and adding discount …
        ✓ Updated → Price=79.9900, Discount=5.0000
        ...
        ✓ Deleted Id=1 — 1 product(s) remaining.
```

---

## Type mapping

| C# type | SQL Server type | Nullability |
|---------|-----------------|-------------|
| `int` (PrimaryKey) | `INT IDENTITY(1,1)` | PRIMARY KEY |
| `int` | `INT` | NOT NULL |
| `long` | `BIGINT` | NOT NULL |
| `float` | `REAL` | NOT NULL |
| `double` | `FLOAT` | NOT NULL |
| `decimal` | `DECIMAL(18,4)` | NOT NULL |
| `bool` | `BIT` | NOT NULL |
| `DateTime` | `DATETIME2` | NOT NULL |
| `Guid` | `UNIQUEIDENTIFIER` | NOT NULL |
| `string` | `NVARCHAR(MAX)` | NOT NULL |
| `T?` (any above) | same SQL type | NULL |

Nullable value types (`int?`, `decimal?` …) are detected via `Nullable.GetUnderlyingType`.
Nullable reference types (`string?`) are detected via `NullabilityInfoContext` (.NET 6+).

Properties **without** `[Column]` or `[PrimaryKey]` are silently skipped — navigation
properties and computed fields are never mapped.

---

## Attribute filtering

| Attribute | Purpose |
|-----------|---------|
| `[Table("name")]` | Maps class → SQL Server table |
| `[Column("name")]` | Maps property → column name |
| `[PrimaryKey]` | Marks the `INT IDENTITY` primary key |

---

## Restrictions

- Only `Microsoft.Data.SqlClient` is used as a third-party package.
- No Dapper, EF Core, or any other ORM/data-access library.
- All SQL is parameterised — no string concatenation of values.

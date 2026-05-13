using System.Text;
using Microsoft.Data.SqlClient;

namespace MiniOrm.Data;
public class DbContext : IDisposable
{
    private readonly string _connectionString;
    private SqlConnection? _connection;
    protected DbContext(string connStr)
    {
        string[] connStrArr = connStr.Split(';');

        StringBuilder tempDatabaseName = new StringBuilder();
        StringBuilder tempConnStringWithoutDb = new StringBuilder();

        foreach(string str in connStrArr)
        {
            string[] strArr = str.Split('=', ' ');
            
            if(strArr.Contains("database", StringComparer.OrdinalIgnoreCase))
            {
                foreach(string s in strArr)
                {
                    if(!s.Equals("database", StringComparison.OrdinalIgnoreCase))
                    {
                        tempDatabaseName.Append(s);    
                    }
                }
            }
            else
            {
                tempConnStringWithoutDb.Append(str);
                tempConnStringWithoutDb.Append(';');
            }
        }

        string databaseName = tempDatabaseName.ToString();
        string connStringWithoutDb = tempConnStringWithoutDb.ToString();

        if(string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(connStringWithoutDb))
        {
            throw new Exception("Something went wrong");
        }

        string masterConn = $"Database=master;{connStringWithoutDb}";

        string createDb = $@"
        IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{databaseName}')
        BEGIN
            CREATE DATABASE [{databaseName}]
        END
        ";

        // If There is no database named MiniORMDB, it will create one.
        // ADO.NET won't automatically create database if it isn't exist. 
        using(SqlConnection connection = new SqlConnection(masterConn))
        {
            connection.Open();
            using var cmd = new SqlCommand(createDb, connection);
            cmd.ExecuteNonQuery();
        }

        _connectionString = $"Database={databaseName};{connStringWithoutDb}";


    }

    public SqlConnection GetConnection()
    {
        if(_connection is null)
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}

using Microsoft.Data.Sqlite;

namespace Barista.Data;

// Creates or upgrades the SQLite file and opens a connection for each database operation.
public sealed class BaristaDatabase
{
    private readonly string connectionString;

    public BaristaDatabase(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            DefaultTimeout = 5,
            ForeignKeys = true
        }.ToString();

        using var connection = Open();
        using var command = connection.CreateCommand();
        // WAL lets reads continue while an order or account is being saved.
        command.CommandText = "PRAGMA journal_mode = WAL;";
        command.ExecuteNonQuery();

        using var transaction = connection.BeginTransaction(deferred: false);
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS staff_account (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                username TEXT NOT NULL,
                password_hash TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS client_account (
                id INTEGER PRIMARY KEY,
                username TEXT NOT NULL,
                username_key TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS menu_availability (
                item_key TEXT PRIMARY KEY
            );

            CREATE TABLE IF NOT EXISTS order_ticket (
                number INTEGER PRIMARY KEY AUTOINCREMENT,
                request_id TEXT NOT NULL UNIQUE,
                item TEXT NOT NULL,
                receipt TEXT NOT NULL,
                ready INTEGER NOT NULL DEFAULT 0 CHECK (ready IN (0, 1))
            );
            """;
        command.ExecuteNonQuery();
        // Add the client and pickup columns to existing databases once.
        command.CommandText = "PRAGMA user_version;";
        if ((long)command.ExecuteScalar()! == 0)
        {
            command.CommandText = """
                ALTER TABLE order_ticket ADD COLUMN client_id INTEGER REFERENCES client_account(id);
                ALTER TABLE order_ticket ADD COLUMN created_at INTEGER;
                ALTER TABLE order_ticket ADD COLUMN completed INTEGER NOT NULL DEFAULT 0;
                CREATE INDEX IF NOT EXISTS order_ticket_client ON order_ticket (client_id, number);
                PRAGMA user_version = 1;
                """;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
